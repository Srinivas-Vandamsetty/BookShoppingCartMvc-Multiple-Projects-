using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCart.WebAPI.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace BookShoppingCart.Tests.Tests;

public sealed class AuthTestClock : TimeProvider
{
    public DateTimeOffset Now { get; set; }=DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => Now;
}
public sealed class AuthApiFactory(int limit=1000) : WebApplicationFactory<GetBooksEndpoint>
{
    private readonly SqliteConnection _connection=new("Data Source=:memory:");
    public AuthTestClock Clock { get; }=new();
    public static readonly string Key=Convert.ToBase64String(Encoding.UTF8.GetBytes("authentication-integration-test-key-only-1234567890123456"));
    public const string Password="Test-Password-123!";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_,config)=>config.AddInMemoryCollection(new Dictionary<string,string?>
        {
            ["Jwt:SigningKey"]=Key, ["Jwt:Issuer"]="https://authentication.tests", ["Jwt:Audience"]="bookstore-auth-tests",
            ["RateLimits:AuthenticationPerMinute"]=limit.ToString(), ["Logging:LogLevel:Default"]="Warning",
            ["ConnectionStrings:DefaultConnection"]="Server=unused;Database=unused;Integrated Security=True;Encrypt=False"
        }));
        builder.ConfigureServices(services=>
        {
            _connection.Open();
            _connection.CreateCollation("SQL_Latin1_General_CP1_CI_AS",(a,b)=>StringComparer.OrdinalIgnoreCase.Compare(a,b));
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(o=>o.UseSqlite(_connection));
            services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(Clock);
            services.Configure<PasswordHasherOptions>(o=>o.IterationCount=10000); // Test work factor only.
        });
    }
    public async Task<string> SeedUser(string email="reader@example.test",bool confirmed=true,bool twoFactor=false,string role="User")
    {
        using var scope=Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if(!await roles.RoleExistsAsync(role)) Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);
        var users=scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user=new IdentityUser { UserName=email,Email=email,EmailConfirmed=confirmed,TwoFactorEnabled=twoFactor };
        Assert.True((await users.CreateAsync(user,Password)).Succeeded);
        Assert.True((await users.AddToRoleAsync(user,role)).Succeeded);
        return user.Id;
    }
    public HttpClient Client(bool https=true) => CreateClient(new WebApplicationFactoryClientOptions
    { BaseAddress=new Uri(https ? "https://localhost" : "http://localhost"),AllowAutoRedirect=false });
    public static async Task<HttpResponseMessage> Login(HttpClient client,string identifier="reader@example.test",string password=Password)
    {
        using var request=new HttpRequestMessage(HttpMethod.Post,"/api/auth/login");
        request.Headers.Authorization=new AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.UTF8.GetBytes(identifier+":"+password)));
        return await client.SendAsync(request);
    }
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
public class AuthenticationApiTests
{
    [Fact]
    public async Task Swagger_ExposesAuthenticationEndpointsAndSecuritySchemes()
    {
        await using var app = new AuthApiFactory();
        using var client = app.Client();
        using var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/auth/login", out var login));
        Assert.True(paths.TryGetProperty("/api/auth/refresh", out _));
        Assert.True(paths.TryGetProperty("/api/auth/logout", out var logout));
        var schemes = document.RootElement.GetProperty("components").GetProperty("securitySchemes");
        Assert.Equal("basic", schemes.GetProperty("BasicLogin").GetProperty("scheme").GetString());
        Assert.Contains("BasicLogin", login.GetProperty("post").GetProperty("security").ToString());
        var bearerName = schemes.EnumerateObject().Single(p => p.Value.TryGetProperty("scheme", out var scheme)
            && scheme.GetString() == "bearer").Name;
        Assert.Contains(bearerName, logout.GetProperty("post").GetProperty("security").ToString());
        Assert.Contains(bearerName, paths.GetProperty("/api/genre/AddGenre").GetProperty("post").GetProperty("security").ToString());
    }

    [Fact] public async Task Login_IssuesValidatedJwtAndOpaqueRefresh_StoresOnlyHashes()
    {
        await using var app=new AuthApiFactory();
        var id=await app.SeedUser();
        using var client=app.Client();
        using var response=await AuthApiFactory.Login(client);
        Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        var tokens=(await response.Content.ReadFromJsonAsync<AuthTokens>())!;
        Assert.Equal("Bearer",tokens.TokenType);
        Assert.Equal(86,tokens.RefreshToken.Length);
        var options=new JwtOptions { Issuer="https://authentication.tests",Audience="bookstore-auth-tests",SigningKey=AuthApiFactory.Key };
        var principal=new JwtSecurityTokenHandler { MapInboundClaims=false }.ValidateToken(tokens.AccessToken,options.ValidationParameters(),out _);
        Assert.Equal(id,principal.FindFirstValue("sub"));
        Assert.Contains(principal.FindAll("role"),c=>c.Value=="User");
        Assert.NotNull(principal.FindFirstValue("jti"));
        Assert.InRange(tokens.AccessTokenExpiresAtUtc,DateTime.UtcNow.AddMinutes(14),DateTime.UtcNow.AddMinutes(16));
        using var scope=app.Services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stored=await db.ApiRefreshTokens.AsNoTracking().SingleAsync();
        Assert.Equal(BookShoppingCart.Business.Services.ApiAuthenticationService.Hash(tokens.RefreshToken),stored.TokenHash);
        Assert.NotEqual(tokens.RefreshToken,stored.TokenHash);
    }
    [Theory] [InlineData("missing@example.test","wrong")] [InlineData("reader@example.test","wrong")]
    public async Task WrongCredentials_AreGenericAndIssueNoTokens(string email,string password)
    {
        await using var app=new AuthApiFactory(); await app.SeedUser();
        using var client=app.Client(); using var response=await AuthApiFactory.Login(client,email,password);
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate,h=>h.Scheme=="Basic");
        Assert.DoesNotContain("accessToken",await response.Content.ReadAsStringAsync());
        using var scope=app.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ApiSessions.ToListAsync());
    }
    [Theory] [InlineData(false,false)] [InlineData(true,true)]
    public async Task UnconfirmedAndMfaAccounts_CannotBypassIdentity(bool confirmed,bool twoFactor)
    {
        await using var app=new AuthApiFactory(); await app.SeedUser(confirmed:confirmed,twoFactor:twoFactor);
        using var client=app.Client(); using var response=await AuthApiFactory.Login(client);
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }
    [Fact] public async Task PasswordFailures_ActivateIdentityLockout()
    {
        await using var app=new AuthApiFactory(); var id=await app.SeedUser();
        using var client=app.Client();
        for(var i=0;i<5;i++) { using var failed=await AuthApiFactory.Login(client,password:"wrong"); Assert.Equal(HttpStatusCode.Unauthorized,failed.StatusCode); }
        using var valid=await AuthApiFactory.Login(client);
        Assert.Equal(HttpStatusCode.Unauthorized,valid.StatusCode);
        using var scope=app.Services.CreateScope();
        Assert.True(await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().IsLockedOutAsync(
            (await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByIdAsync(id))!));
    }
    [Fact] public async Task Refresh_RotatesAndReplayRevokesTheEntireFamily()
    {
        await using var app=new AuthApiFactory(); await app.SeedUser();
        using var client=app.Client();
        using var login=await AuthApiFactory.Login(client);
        var first=(await login.Content.ReadFromJsonAsync<AuthTokens>())!;
        using var refresh=await client.PostAsJsonAsync("/api/auth/refresh",new { first.RefreshToken });
        Assert.Equal(HttpStatusCode.OK,refresh.StatusCode);
        var next=(await refresh.Content.ReadFromJsonAsync<AuthTokens>())!;
        Assert.NotEqual(first.RefreshToken,next.RefreshToken);
        using var replay=await client.PostAsJsonAsync("/api/auth/refresh",new { first.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized,replay.StatusCode);
        using var denied=await client.PostAsJsonAsync("/api/auth/refresh",new { next.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized,denied.StatusCode);
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",next.AccessToken);
        using var logout=await client.PostAsync("/api/auth/logout",null);
        Assert.Equal(HttpStatusCode.Unauthorized,logout.StatusCode);
    }
    [Fact] public async Task Logout_RevokesRefreshAndAccessImmediately_ButNotOtherSessions()
    {
        await using var app=new AuthApiFactory(); await app.SeedUser();
        using var client=app.Client();
        using var login=await AuthApiFactory.Login(client);
        var tokens=(await login.Content.ReadFromJsonAsync<AuthTokens>())!;
        using var another=await AuthApiFactory.Login(client);
        var other=(await another.Content.ReadFromJsonAsync<AuthTokens>())!;
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",tokens.AccessToken);
        using var logout=await client.PostAsync("/api/auth/logout",null);
        Assert.Equal(HttpStatusCode.NoContent,logout.StatusCode);
        using var refresh=await client.PostAsJsonAsync("/api/auth/refresh",new { tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized,refresh.StatusCode);
        using var again=await client.PostAsync("/api/auth/logout",null);
        Assert.Equal(HttpStatusCode.Unauthorized,again.StatusCode);
        using var unaffected=await client.PostAsJsonAsync("/api/auth/refresh",new { other.RefreshToken });
        Assert.Equal(HttpStatusCode.OK,unaffected.StatusCode);
    }
    [Fact] public async Task ExpiredRefreshAndSecurityStampChanges_AreRejected()
    {
        await using var app=new AuthApiFactory(); var id=await app.SeedUser();
        using var client=app.Client(); using var login=await AuthApiFactory.Login(client);
        var tokens=(await login.Content.ReadFromJsonAsync<AuthTokens>())!;
        using(var scope=app.Services.CreateScope())
        {
            var users=scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            await users.UpdateSecurityStampAsync((await users.FindByIdAsync(id))!);
        }
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",tokens.AccessToken);
        using var logout=await client.PostAsync("/api/auth/logout",null);
        Assert.Equal(HttpStatusCode.Unauthorized,logout.StatusCode);
        using var denied=await client.PostAsJsonAsync("/api/auth/refresh",new { tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized,denied.StatusCode);
        using var relogin=await AuthApiFactory.Login(client);
        var next=(await relogin.Content.ReadFromJsonAsync<AuthTokens>())!;
        app.Clock.Now=app.Clock.Now.AddDays(8);
        using var expired=await client.PostAsJsonAsync("/api/auth/refresh",new { next.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized,expired.StatusCode);
    }
    [Theory] [InlineData("Basic not-base64")] [InlineData("Basic YWJj")] [InlineData("Bearer invalid")]
    public async Task MalformedLoginHeaders_AreRejected(string header)
    {
        await using var app=new AuthApiFactory(); await app.SeedUser();
        using var client=app.Client();
        using var request=new HttpRequestMessage(HttpMethod.Post,"/api/auth/login");
        request.Headers.TryAddWithoutValidation("Authorization",header);
        using var response=await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }
    [Fact] public async Task HttpCredentials_AreRejectedWithoutRedirect()
    {
        await using var app=new AuthApiFactory();
        using var client=app.Client(false); using var response=await AuthApiFactory.Login(client);
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        Assert.Null(response.Headers.Location);
    }
    [Fact] public async Task LoginThrottle_RunsBeforeCredentialVerification()
    {
        await using var app=new AuthApiFactory(2);
        using var client=app.Client();
        for(var i=0;i<2;i++) { using var denied=await client.PostAsync("/api/auth/login",null); Assert.Equal(HttpStatusCode.Unauthorized,denied.StatusCode); }
        using var limited=await client.PostAsync("/api/auth/login",null);
        Assert.Equal(HttpStatusCode.TooManyRequests,limited.StatusCode);
    }
    [Theory] [InlineData("signature")] [InlineData("issuer")] [InlineData("audience")] [InlineData("expired")] [InlineData("type")] [InlineData("algorithm")] [InlineData("session")]
    public async Task InvalidAccessTokens_AreRejected(string mutation)
    {
        await using var app=new AuthApiFactory(); await app.SeedUser();
        using var client=app.Client(); using var login=await AuthApiFactory.Login(client);
        var tokens=(await login.Content.ReadFromJsonAsync<AuthTokens>())!;
        var source=new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        var now=DateTime.UtcNow;
        var signing=mutation=="signature" ? Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)) : AuthApiFactory.Key;
        var claims=source.Claims.Where(c=>c.Type is not ("aud" or "iss" or "nbf" or "exp") &&
            (mutation!="session" || c.Type!="sid"));
        var jwt=new JwtSecurityToken(mutation=="issuer" ? "https://wrong.example" : source.Issuer,
            mutation=="audience" ? "wrong" : "bookstore-auth-tests",claims,
            mutation=="expired" ? now.AddHours(-2) : now.AddSeconds(-1),
            mutation=="expired" ? now.AddHours(-1) : now.AddMinutes(15),
            new SigningCredentials(JwtOptions.Key(signing),mutation=="algorithm" ? SecurityAlgorithms.HmacSha384 : SecurityAlgorithms.HmacSha256));
        jwt.Header["typ"]=mutation=="type" ? "JWT" : "at+jwt";
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",new JwtSecurityTokenHandler().WriteToken(jwt));
        using var response=await client.PostAsync("/api/auth/logout",null);
        Assert.Equal(HttpStatusCode.Unauthorized,response.StatusCode);
    }
    [Fact] public async Task RoleChangesAreEnforced_AndBasicCannotAccessProtectedApis()
    {
        await using var app=new AuthApiFactory(); var id=await app.SeedUser(role:"Admin");
        using var client=app.Client(); using var login=await AuthApiFactory.Login(client);
        var tokens=(await login.Content.ReadFromJsonAsync<AuthTokens>())!;
        using(var scope=app.Services.CreateScope())
        {
            var users=scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            await users.RemoveFromRoleAsync((await users.FindByIdAsync(id))!,"Admin");
        }
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",tokens.AccessToken);
        using var stale=await client.PostAsJsonAsync("/api/genre/AddGenre",new { GenreName="Denied" });
        Assert.Equal(HttpStatusCode.Unauthorized,stale.StatusCode);
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Basic",Convert.ToBase64String(Encoding.UTF8.GetBytes("reader@example.test:"+AuthApiFactory.Password)));
        using var basic=await client.PostAsJsonAsync("/api/genre/AddGenre",new { GenreName="Denied" });
        Assert.Equal(HttpStatusCode.Unauthorized,basic.StatusCode);
    }

    [Theory] [InlineData("Admin",HttpStatusCode.Created)] [InlineData("User",HttpStatusCode.Forbidden)]
    public async Task BearerRoleAuthorization_ControlsCatalogWrites(string role,HttpStatusCode expected)
    {
        await using var app=new AuthApiFactory(); await app.SeedUser(role:role);
        using var client=app.Client(); using var login=await AuthApiFactory.Login(client);
        var tokens=(await login.Content.ReadFromJsonAsync<AuthTokens>())!;
        client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",tokens.AccessToken);
        using var response=await client.PostAsJsonAsync("/api/genre/AddGenre",new { GenreName="Authorized category" });
        Assert.Equal(expected,response.StatusCode);
    }
    [Fact] public async Task MalformedRefreshBody_ReturnsValidationErrorWithoutIssuingTokens()
    {
        await using var app=new AuthApiFactory();
        using var client=app.Client();
        using var response=await client.PostAsJsonAsync("/api/auth/refresh",new { RefreshToken="short" });
        Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
    }
}
