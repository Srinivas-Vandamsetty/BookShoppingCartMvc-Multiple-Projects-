using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCartMvcUI.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BookShoppingCart.Tests.Tests;
public class BoundaryAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logs, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logs, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier,"test-user"),
                new Claim(ClaimTypes.Name,"reader@example.com"), new Claim(ClaimTypes.Role,"Admin") }, "Boundary")), "Boundary")));
}
public class HttpBoundaryTests
{
    private static readonly string JwtKey=Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("boundary-test-signing-key-not-for-production-123456"));
    private static string AdminToken() => new BookShoppingCart.WebAPI.Security.JwtAccessTokenIssuer(
        Microsoft.Extensions.Options.Options.Create(new BookShoppingCart.WebAPI.Security.JwtOptions
        { Issuer="https://tests.local",Audience="bookstore-tests",SigningKey=JwtKey }))
        .Issue("test-admin","admin",["Admin"],Guid.NewGuid(),DateTime.UtcNow).Token;
    private static WebApplicationFactory<GetBooksEndpoint> Api(Mock<IGenreService> genres) =>
        new WebApplicationFactory<GetBooksEndpoint>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>
            {
                ["Jwt:SigningKey"]=JwtKey, ["Jwt:Issuer"]="https://tests.local", ["Jwt:Audience"]="bookstore-tests", ["Database:Initialize"]="false",
                ["ConnectionStrings:DefaultConnection"]="Server=(localdb)\\MSSQLLocalDB;Database=NotUsed;Trusted_Connection=True;Encrypt=False"
            }));
            builder.ConfigureServices(services=>
            {
                services.RemoveAll<IGenreService>(); services.AddSingleton(genres.Object);
                var authentication=new Mock<IApiAuthenticationService>();
                authentication.Setup(a=>a.ValidateAccessAsync(It.IsAny<ClaimsPrincipal>(),It.IsAny<CancellationToken>())).ReturnsAsync(true);
                services.RemoveAll<IApiAuthenticationService>(); services.AddSingleton(authentication.Object);
            });
        });
    [Theory] [InlineData("POST","/api/genre/AddGenre")] [InlineData("PUT","/api/genre/UpdateGenre")]
    [InlineData("DELETE","/api/genre/DeleteGenre/1")]
    public async Task AnonymousGenreWrites_AreRejected(string method,string url)
    {
        await using var app=Api(new Mock<IGenreService>());
        using var client=app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost"), AllowAutoRedirect=false });
        using var response=await client.SendAsync(new HttpRequestMessage(new HttpMethod(method),url)
        { Content=JsonContent.Create(new { id=1,genreName="Fiction" }) });
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }
    [Fact] public async Task ApiValidationAndScalarMapping_PreventGraphWrites()
    {
        var genres=new Mock<IGenreService>();
        genres.Setup(g=>g.AddGenre(It.IsAny<Genre>())).Callback<Genre>(g=>g.Id=42).Returns(Task.CompletedTask);
        await using var app=Api(genres);
        using var client=app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization=new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",AdminToken());
        using var bad=await client.PostAsJsonAsync("/api/genre/AddGenre",new { genreName="" });
        Assert.Equal(HttpStatusCode.BadRequest,bad.StatusCode);
        using var created=await client.PostAsJsonAsync("/api/genre/AddGenre",new { genreName="Fiction", books=new[] { new { bookName="Injected" } } });
        Assert.Equal(HttpStatusCode.Created,created.StatusCode);
        genres.Verify(g=>g.AddGenre(It.Is<Genre>(g=>g.GenreName=="Fiction" && g.Books.Count==0)),Times.Once);
    }
    private static WebApplicationFactory<BookController> Mvc(Mock<ICartService> cart, Action<IServiceCollection>? customize = null) =>
        new WebApplicationFactory<BookController>().WithWebHostBuilder(builder=>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>
            { ["Database:Initialize"]="false" }));
            builder.ConfigureServices(services=>
            {
                services.RemoveAll<ICartService>(); services.AddSingleton(cart.Object);
                services.AddAuthentication().AddScheme<AuthenticationSchemeOptions,BoundaryAuthentication>("Boundary",_=>{});
                services.PostConfigure<AuthenticationOptions>(options=>
                { options.DefaultAuthenticateScheme="Boundary"; options.DefaultChallengeScheme="Boundary"; });
                customize?.Invoke(services);
            });
        });
    [Theory] [InlineData("/Cart/AddItem?bookId=1")] [InlineData("/Cart/RemoveItem?bookId=1")]
    [InlineData("/Book/DeleteBook/1")] [InlineData("/Book/CloneBook/1")] [InlineData("/Genre/DeleteGenre/1")]
    [InlineData("/AdminOperations/TogglePaymentStatus?orderId=1")]
    public async Task MutatingGetRoutes_AreNotAllowed(string route)
    {
        await using var app=Mvc(new Mock<ICartService>());
        using var client=app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false });
        using var response=await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.MethodNotAllowed,response.StatusCode);
    }
    [Fact] public async Task CartPost_RequiresAntiforgery_AndWorksWithValidToken()
    {
        var cart=new Mock<ICartService>();
        cart.Setup(c=>c.GetUserCart()).ReturnsAsync(new ShoppingCart());
        cart.Setup(c=>c.AddItem(1,1)).ReturnsAsync(1);
        await using var app=Mvc(cart);
        using var client=app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost") });
        using var rejected=await client.PostAsync("/Cart/AddItem",new FormUrlEncodedContent(new Dictionary<string,string>{{"bookId","1"}}));
        Assert.Equal(HttpStatusCode.BadRequest,rejected.StatusCode);
        var html=await client.GetStringAsync("/Cart/GetUserCart");
        var match=Regex.Match(html,"name=\"csrf-token\" content=\"([^\"]+)\"");
        Assert.True(match.Success);
        using var request=new HttpRequestMessage(HttpMethod.Post,"/Cart/AddItem")
        { Content=new FormUrlEncodedContent(new Dictionary<string,string>{{"bookId","1"},{"qty","1"}}) };
        request.Headers.Add("RequestVerificationToken",WebUtility.HtmlDecode(match.Groups[1].Value));
        using var accepted=await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK,accepted.StatusCode);
        cart.Verify(c=>c.AddItem(1,1),Times.Once);
    }

    [Fact] public async Task ValidationAssets_AreAvailableBeforeFormScripts()
    {
        await using var app=Mvc(new Mock<ICartService>());
        using var client=app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost") });
        var html=await client.GetStringAsync("/Identity/Account/Register");
        Assert.Contains("/Identity/lib/jquery/dist/jquery.min.js",html);
        Assert.True(html.IndexOf("/Identity/lib/jquery/dist/jquery.min.js",StringComparison.Ordinal) <
            html.IndexOf("/Identity/lib/jquery-validation/dist/jquery.validate.min.js",StringComparison.Ordinal));
        using var response=await client.GetAsync("/Identity/lib/jquery/dist/jquery.min.js");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
    }
    [Fact] public async Task RegisterConfirmation_DoesNotRevealTokensOrAccountExistence()
    {
        await using var app=Mvc(new Mock<ICartService>());
        using var client=app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost") });
        var html=await client.GetStringAsync("/Identity/Account/RegisterConfirmation?email=admin@example.com");
        Assert.DoesNotContain("confirm-link",html);
        Assert.DoesNotContain("code=",html);
        Assert.Contains("confirmation link has been sent",html);
    }

    [Fact] public async Task InvalidGenreForm_RendersDtoValidationInsteadOfModelTypeFailure()
    {
        await using var app=Mvc(new Mock<ICartService>());
        using var client=app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost") });
        var html=await client.GetStringAsync("/Genre/AddGenre");
        var match=Regex.Match(html,"name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(match.Success);
        using var response=await client.PostAsync("/Genre/AddGenre",new FormUrlEncodedContent(new Dictionary<string,string>
        { ["GenreName"]="", ["__RequestVerificationToken"]=WebUtility.HtmlDecode(match.Groups[1].Value) }));
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.Contains("GenreName",await response.Content.ReadAsStringAsync());
    }
}
