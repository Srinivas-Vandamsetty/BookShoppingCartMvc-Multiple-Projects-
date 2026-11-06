using System.Data.Common;
using BookShoppingCartMvcUI.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
namespace BookShoppingCart.Tests.Tests;
public class ExceptionMiddlewareTests
{
    private sealed class ConnectionFailure : DbException
    {
        public ConnectionFailure() : base("Private SQL connection diagnostics") { }
    }
    [Fact]
    public async Task WrappedDatabaseFailure_ReturnsSanitizedServiceUnavailable()
    {
        var context=new DefaultHttpContext();
        context.Request.Headers.Accept="application/json";
        context.Response.Body=new MemoryStream();
        var middleware=new ExceptionMiddleware(_=>throw new InvalidOperationException(
            "Consider EnableRetryOnFailure",new ConnectionFailure()),NullLogger<ExceptionMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        context.Response.Body.Position=0;
        var body=await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(503,context.Response.StatusCode);
        Assert.Contains("database is currently unavailable",body);
        Assert.DoesNotContain("Private SQL",body);
        Assert.DoesNotContain("EnableRetryOnFailure",body);
    }
    [Fact]
    public async Task BusinessConflict_PreservesActionableMessage()
    {
        var context=new DefaultHttpContext();
        context.Request.Headers.Accept="application/json";
        context.Response.Body=new MemoryStream();
        var middleware=new ExceptionMiddleware(_=>throw new InvalidOperationException("Insufficient stock."),
            NullLogger<ExceptionMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.Equal(409,context.Response.StatusCode);
    }
}
