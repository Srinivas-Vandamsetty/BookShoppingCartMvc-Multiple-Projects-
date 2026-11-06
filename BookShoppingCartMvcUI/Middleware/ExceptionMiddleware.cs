using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace BookShoppingCartMvcUI.Middleware;
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex) when (ex is not OperationCanceledException || !context.RequestAborted.IsCancellationRequested)
        {
            if (context.Response.HasStarted) throw;
            var databaseFailure = ContainsDatabaseFailure(ex);
            var status = databaseFailure ? StatusCodes.Status503ServiceUnavailable : ex switch
            {
                BookShoppingCartMvcUI.Services.CommerceApiException api => (int)api.Status,
                KeyNotFoundException => 404, UnauthorizedAccessException => 403,
                ArgumentException or ValidationException => 400,
                NotSupportedException => 422, InvalidOperationException or DbUpdateConcurrencyException => 409,
                DbUpdateException => 409, _ => 500
            };
            if (status >= 500) logger.LogError(ex, "Request failed {TraceId}", context.TraceIdentifier);
            else logger.LogWarning(ex, "Request rejected with {Status} {TraceId}", status, context.TraceIdentifier);
            context.Response.Clear();
            context.Response.StatusCode = status;
            var message = databaseFailure ? "The database is currently unavailable. Please try again later." :
                ex is BookShoppingCartMvcUI.Services.CommerceApiException ? ex.Message :
                status >= 500 ? "An unexpected error occurred. Please try again." :
                ex is DbUpdateException ? "The record changed or conflicts with existing data. Please refresh and retry." : ex.Message;
            if (context.Request.Headers.Accept.ToString().Contains("application/json") ||
                context.Request.Path.StartsWithSegments("/api"))
                await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
                { Status = status, Title = message, Extensions = { ["traceId"] = context.TraceIdentifier } });
            else
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync("<!doctype html><title>Request failed</title><h1>" +
                    System.Net.WebUtility.HtmlEncode(message) + "</h1><p>Please return to the previous page and try again.</p>");
            }
        }
    }

    private static bool ContainsDatabaseFailure(Exception exception)
    {
        // EF can wrap connection failures in InvalidOperationException. Do not expose
        // provider diagnostics or misclassify them as a business conflict (409).
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is DbException && exception is not DbUpdateException) return true;
        return false;
    }
}
