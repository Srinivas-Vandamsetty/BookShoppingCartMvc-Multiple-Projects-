using System.Net;
using System.Net.Http.Json;
using BookShoppingCart.Models.Models.DTOs;
using BookShoppingCartMvcUI.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
namespace BookShoppingCart.Tests.Tests;
public class CatalogClientTests
{
    private sealed class CatalogHandler : HttpMessageHandler
    {
        public System.Collections.Concurrent.ConcurrentBag<Uri> Requests { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests.Add(request.RequestUri!);
            var page = request.RequestUri!.AbsolutePath.EndsWith("GetBooksPage",StringComparison.Ordinal);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = page ? JsonContent.Create(new PagedBooks()) : JsonContent.Create(Array.Empty<GenreDTO>()) });
        }
    }
    [Theory] [InlineData("abc")] [InlineData("-1")] [InlineData("9999999999999")]
    public async Task Search_EncodesQueryAndNormalizesInvalidPage(string page)
    {
        using var handler = new CatalogHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://catalog.example/api/") };
        var clients = new Mock<IHttpClientFactory>();
        clients.Setup(c=>c.CreateClient("Catalog")).Returns(client);
        var controller = new HomeController(clients.Object,NullLogger<HomeController>.Instance);
        const string search = "C# & programming";
        var result = Assert.IsType<ViewResult>(await controller.Index(search,4,page));
        Assert.IsType<BookDisplayModel>(result.Model);
        var uri = Assert.Single(handler.Requests,u=>u.AbsolutePath.EndsWith("GetBooksPage",StringComparison.Ordinal));
        var values = QueryHelpers.ParseQuery(uri.Query);
        Assert.Equal(search,values["sTerm"].ToString());
        Assert.Equal("4",values["genreId"].ToString());
        Assert.Equal("1",values["page"].ToString());
    }
}
