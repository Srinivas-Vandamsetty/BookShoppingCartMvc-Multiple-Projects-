using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Services;

public sealed class CommerceApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

public interface ICommerceCartGateway
{
    Task<CheckoutQuoteResponse> Quote(string? coupon);
    Task<CheckoutResponse> Checkout(CheckoutModel model);
    Task<List<PaymentMethodResponse>> PaymentMethods();
    Task SetQuantity(int bookId, int quantity);
    Task RemoveLine(int bookId);
}

public sealed class CommerceApiClient(IHttpClientFactory clients, IHttpContextAccessor accessor) : ICommerceCartGateway
{
    public Task<T> Get<T>(string path) => Exchange<T>(HttpMethod.Get, path);
    public async Task<T> Exchange<T>(HttpMethod method, string path, object? body = null)
    {
        using var response = await SendCore(method, path, body);
        try
        {
            var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: Cancellation);
            return result ?? throw Unavailable();
        }
        catch (JsonException) { throw Unavailable(); }
    }
    public async Task Send(HttpMethod method, string path, object? body = null)
    { using var response = await SendCore(method, path, body); }

    private CancellationToken Cancellation => accessor.HttpContext?.RequestAborted ?? default;
    private static CommerceApiException Unavailable() => new(HttpStatusCode.ServiceUnavailable, "The shop service is temporarily unavailable. Please try again.");
    private async Task<HttpResponseMessage> SendCore(HttpMethod method, string path, object? body)
    {
        using var client = clients.CreateClient("Catalog");
        using var request = new HttpRequestMessage(method, path) { Content = body is null ? null : JsonContent.Create(body) };
        HttpResponseMessage response;
        try { response = await client.SendAsync(request, Cancellation); }
        catch (HttpRequestException) { throw Unavailable(); }
        catch (OperationCanceledException) when (!Cancellation.IsCancellationRequested) { throw Unavailable(); }
        if (response.IsSuccessStatusCode) return response;
        using (response)
        {
            if ((int)response.StatusCode >= 500) throw Unavailable();
            var message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Your session expired. Please sign in again.",
                HttpStatusCode.Forbidden => "You do not have permission to perform this operation.",
                HttpStatusCode.TooManyRequests => "The shop is busy. Please try again shortly.",
                HttpStatusCode.NotFound => "This item could not be found.",
                _ => "The request could not be completed. Review your entries and try again."
            };
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity)
            {
                try
                {
                    var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken: Cancellation);
                    message = problem?.Errors.Values.SelectMany(v => v).FirstOrDefault() ?? problem?.Title ?? message;
                }
                catch (JsonException) { }
                catch (NotSupportedException) { }
            }
            throw new CommerceApiException(response.StatusCode, message);
        }
    }

    public Task<CheckoutQuoteResponse> Quote(string? coupon) => Exchange<CheckoutQuoteResponse>(HttpMethod.Post, "cart/quote", new QuoteRequest { CouponCode = coupon });
    public Task<CheckoutResponse> Checkout(CheckoutModel model) => Exchange<CheckoutResponse>(HttpMethod.Post, "cart/checkout", model);
    public Task<List<PaymentMethodResponse>> PaymentMethods() => Get<List<PaymentMethodResponse>>("payment-methods");
    public Task SetQuantity(int bookId, int quantity) => Send(HttpMethod.Put, $"cart/items/{bookId}", new CartQuantityRequest { Quantity = quantity });
    public Task RemoveLine(int bookId) => Send(HttpMethod.Delete, $"cart/items/{bookId}");
}
