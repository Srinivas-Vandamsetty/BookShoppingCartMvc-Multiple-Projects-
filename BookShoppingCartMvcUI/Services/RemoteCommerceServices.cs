using System.Net;
using BookShoppingCart.Business.Services;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.AspNetCore.WebUtilities;

namespace BookShoppingCartMvcUI.Services;

public sealed class RemoteBooks(CommerceApiClient api, BookService localImages) : IBookService
{
    public async Task<IEnumerable<Book>> GetBooks(int page = 1) => (await api.Get<CommercePage<BookResponse>>($"admin/books?page={page}")).Items.Select(b => b.ToModel());
    public async Task<Book> GetBookById(int id) => (await api.Get<BookResponse>($"books/{id}")).ToModel();
    private static BookWriteRequest Map(Book book) => new() { BookName = book.BookName, AuthorName = book.AuthorName,
        Price = book.Price, GenreId = book.GenreId, Image = book.Image };
    public Task AddBook(Book book) => Write(async () =>
    { book.Id = (await api.Exchange<BookResponse>(HttpMethod.Post, "books", Map(book))).Id; });
    public Task UpdateBook(Book book) => Write(() => api.Send(HttpMethod.Put, $"books/{book.Id}", Map(book)));
    private static async Task Write(Func<Task> action)
    {
        try { await action(); }
        catch (CommerceApiException ex) when ((int)ex.Status >= 500) { throw new BookWriteUnconfirmedException(ex); }
    }
    public Task DeleteBook(int id) => api.Send(HttpMethod.Delete, $"books/{id}");
    public Task DeleteUnusedImage(string image) => localImages.DeleteUnusedImage(image);
}

public sealed class RemoteCart(CommerceApiClient api) : ICartService
{
    public Task<int> AddItem(int bookId, int qty) => api.Exchange<int>(HttpMethod.Post, "cart/items", new CartItemRequest { BookId = bookId, Quantity = qty });
    public Task<int> RemoveItem(int bookId) => api.Exchange<int>(HttpMethod.Post, $"cart/items/{bookId}/decrement");
    public async Task<ShoppingCart> GetUserCart()
    {
        var cart = await api.Get<CartResponse>("cart");
        return new ShoppingCart { CartDetails = cart.Items.Select(d => new CartDetail { BookId = d.BookId,
            Quantity = d.Quantity, UnitPrice = d.Book.Price, Book = d.Book.ToModel() }).ToList() };
    }
    public async Task<int> GetCartItemCount(string userId = "")
    {
        if (!string.IsNullOrEmpty(userId)) throw new ArgumentException("Cart is scoped to the signed-in customer.");
        return (await api.Get<CartResponse>("cart")).ItemCount;
    }
    public async Task<bool> DoCheckout(CheckoutModel model) { await api.Checkout(model); return true; }
}

public sealed class RemoteOrders(CommerceApiClient api) : IUserOrderService
{
    public async Task<IEnumerable<Order>> GetUserOrders(bool getAll = false, int page = 1) =>
        (await api.Get<CommercePage<OrderResponse>>($"{(getAll ? "admin/" : "")}orders?page={page}")).Items.Select(o => o.ToModel());
    public async Task<Order?> GetOrderById(int id)
    {
        try { return (await api.Get<OrderResponse>($"orders/{id}")).ToModel(); }
        catch (CommerceApiException ex) when (ex.Status == HttpStatusCode.NotFound) { return null; }
    }
    public async Task<IEnumerable<OrderStatus>> GetOrderStatuses() => await api.Get<List<OrderStatus>>("admin/order-statuses");
    public Task ChangeOrderStatus(UpdateOrderStatusModel data) => api.Send(HttpMethod.Patch, $"admin/orders/{data.OrderId}/status",
        new OrderStatusRequest { OperationId = data.OperationId, StatusId = data.OrderStatusId, ExpectedStatusId = data.ExpectedStatusId, Reason = data.Reason });
    public async Task TogglePaymentStatus(int orderId)
    {
        var order = await GetOrderById(orderId) ?? throw new KeyNotFoundException("Order not found.");
        await api.Send(HttpMethod.Put, $"admin/orders/{orderId}/cash-collection", new CashCollectionRequest
        { OperationId = Guid.NewGuid(), Collected = !order.IsPaid, ExpectedCollected = order.IsPaid, Reason = "Legacy cash status change" });
    }
}

public sealed class RemoteStock(CommerceApiClient api) : IStockService
{
    public async Task<IEnumerable<StockDisplayModel>> GetStocks(string sTerm = "", int page = 1) =>
        (await api.Get<CommercePage<StockDisplayModel>>(QueryHelpers.AddQueryString($"admin/inventory?page={page}", "search", sTerm))).Items;
    public async Task<Stock?> GetStockByBookId(int bookId)
    {
        var availability = await api.Get<AvailabilityResponse>($"books/{bookId}/availability");
        return new Stock { BookId = bookId, Quantity = availability.AvailableQuantity };
    }
    public Task ManageStock(StockDTO stock) => api.Send(HttpMethod.Post, $"admin/inventory/{stock.BookId}/adjustments",
        new InventoryAdjustmentRequest { OperationId = stock.OperationId, ExpectedQuantity = stock.ExpectedQuantity,
            Delta = checked(stock.Quantity - stock.ExpectedQuantity), Reason = stock.Reason });
}
