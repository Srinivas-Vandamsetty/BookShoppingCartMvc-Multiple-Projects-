using System.Data;
using System.ComponentModel.DataAnnotations;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using BookStoreCore.Pricing;
using BookStoreCore.Security;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCart.Data.Repositories;

public class CartRepository(ApplicationDbContext db, ICurrentUser currentUser) : ICartRepository
{
    // Serialize mutations for one user's cart across processes, without locking other users.
    // Stock availability is enforced separately with atomic conditional updates.
    private async Task LockCart(string userId)
    {
        if (db.Database.IsSqlServer())
        {
            var resource = "cart:" + userId;
            await db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive',
    @LockOwner='Transaction', @LockTimeout=10000;
IF @result < 0 THROW 51000, 'Cart is busy. Please retry.', 1;");
        }
    }

    public async Task<int> AddItem(int bookId, int qty)
    {
        if (bookId <= 0 || qty is < 1 or > CartPricing.MaxQuantity)
            throw new ArgumentException("Invalid book or quantity (maximum 1000).");
        var userId = currentUser.UserId;
        await using var tx = await db.Database.BeginTransactionAsync();
        await LockCart(userId);
        var book = await db.Books.AsNoTracking().SingleOrDefaultAsync(b => b.Id == bookId && !b.IsArchived)
            ?? throw new KeyNotFoundException("Book not found.");
        var available = await db.Stocks.Where(s => s.BookId == bookId).Select(s => s.Quantity).SingleOrDefaultAsync();
        var cart = await GetCart(userId);
        if (cart is null)
        {
            cart = new ShoppingCart { UserId = userId };
            db.ShoppingCarts.Add(cart);
        }
        var line = cart.CartDetails.SingleOrDefault(d => d.BookId == bookId);
        var quantity = checked((line?.Quantity ?? 0) + qty);
        if (quantity > CartPricing.MaxQuantity || quantity > available)
            throw new InvalidOperationException("Requested quantity is not available.");
        if (line is null && cart.CartDetails.Count >= 100) throw new InvalidOperationException("A cart can contain at most 100 different books.");
        if (line is null)
            cart.CartDetails.Add(new CartDetail { BookId = bookId, Quantity = quantity, UnitPrice = book.Price });
        else
        {
            line.Quantity = quantity;
            line.UnitPrice = book.Price;
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return await GetCartItemCount();
    }

    public async Task<int> SetQuantity(int bookId, int quantity)
    {
        if (bookId <= 0 || quantity is < 0 or > CartPricing.MaxQuantity) throw new ArgumentException("Invalid quantity.");
        var owner = currentUser.UserId;
        await using var tx = await db.Database.BeginTransactionAsync();
        await LockCart(owner);
        var cart = await GetCart(owner);
        var line = cart?.CartDetails.SingleOrDefault(d => d.BookId == bookId);
        if (line is null && quantity > 0) throw new KeyNotFoundException("Cart item not found.");
        if (line is not null)
        {
            if (quantity == 0) db.CartDetails.Remove(line);
            else
            {
                var book = await db.Books.AsNoTracking().SingleOrDefaultAsync(b => b.Id == bookId && !b.IsArchived)
                    ?? throw new KeyNotFoundException("Book not found.");
                var available = await db.Stocks.Where(s => s.BookId == bookId).Select(s => s.Quantity).SingleOrDefaultAsync();
                if (quantity > available) throw new InvalidOperationException("Requested quantity is not available.");
                line.Quantity = quantity; line.UnitPrice = book.Price;
            }
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        return await GetCartItemCount();
    }

    public async Task<int> RemoveItem(int bookId)
    {
        if (bookId <= 0) throw new ArgumentException("Invalid book.");
        var userId = currentUser.UserId;
        await using var tx = await db.Database.BeginTransactionAsync();
        await LockCart(userId);
        var cart = await GetCart(userId);
        var line = cart?.CartDetails.SingleOrDefault(d => d.BookId == bookId);
        if (line is not null)
        {
            if (line.Quantity <= 1) db.CartDetails.Remove(line);
            else line.Quantity--;
            await db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        return await GetCartItemCount();
    }

    public async Task<ShoppingCart> GetUserCart()
    {
        var userId = currentUser.UserId;
        return await db.ShoppingCarts.AsNoTracking()
            .Include(c => c.CartDetails).ThenInclude(d => d.Book).ThenInclude(b => b.Stock)
            .Include(c => c.CartDetails).ThenInclude(d => d.Book).ThenInclude(b => b.Genre)
            .AsSplitQuery().SingleOrDefaultAsync(c => c.UserId == userId)
            ?? new ShoppingCart { UserId = userId };
    }

    public Task<ShoppingCart?> GetCart(string userId)
    {
        if (userId != currentUser.UserId) throw new UnauthorizedAccessException();
        return db.ShoppingCarts.Include(c => c.CartDetails).SingleOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<int> GetCartItemCount(string userId = "")
    {
        var owner = currentUser.UserId;
        if (!string.IsNullOrEmpty(userId) && userId != owner) throw new UnauthorizedAccessException();
        return await db.CartDetails.Where(d => d.ShoppingCart.UserId == owner).SumAsync(d => (int?)d.Quantity) ?? 0;
    }

    public async Task<bool> DoCheckout(CheckoutModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Validator.ValidateObject(model, new ValidationContext(model), true);
        if (!string.Equals(model.PaymentMethod, "cash", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException("Electronic payments are unavailable until a verified payment provider is configured.");
        if (model.CheckoutKey == Guid.Empty) throw new ArgumentException("Checkout key is required.");
        var userId = currentUser.UserId;
        await using var tx = await db.Database.BeginTransactionAsync();
        await LockCart(userId);
        // A replay returns the existing committed outcome even after the cart has been emptied.
        if (await db.Orders.AnyAsync(o => o.UserId == userId && o.CheckoutKey == model.CheckoutKey))
        {
            await tx.CommitAsync();
            return true;
        }
        var cart = await GetCart(userId);
        if (cart is null || cart.CartDetails.Count == 0)
            throw new InvalidOperationException("Your cart is empty.");
        var lines = cart.CartDetails.OrderBy(d => d.BookId).ToList();
        if (lines.Count > 100) throw new InvalidOperationException("A cart can contain at most 100 different books.");
        if (lines.Any(d => d.Quantity is < 1 or > CartPricing.MaxQuantity))
            throw new InvalidOperationException("Invalid cart quantity.");
        var ids = lines.Select(d => d.BookId).ToArray();
        var books = await db.Books.AsNoTracking().Include(b => b.Genre)
            .Where(b => ids.Contains(b.Id) && !b.IsArchived).ToDictionaryAsync(b => b.Id);
        if (books.Count != lines.Count) throw new InvalidOperationException("A book in your cart is no longer available.");
        if (model.ExpectedCartFingerprint is not null &&
            model.ExpectedCartFingerprint != CartPricing.Fingerprint(lines.Select(d =>
                (d.BookId, d.Quantity, books[d.BookId].Price))))
            throw new InvalidOperationException("Cart prices changed. Please review the updated total.");
        var quote = CartPricing.Quote(lines.Sum(d => books[d.BookId].Price * d.Quantity), model.CouponCode);
        var pending = await db.OrderStatuses.SingleOrDefaultAsync(s => s.StatusName == "Pending")
            ?? throw new InvalidOperationException("Order statuses are not initialized.");
        var order = new Order
        {
            UserId = userId, CheckoutKey = model.CheckoutKey, Name = model.Name.Trim(),
            Email = model.Email.Trim(), MobileNumber = model.MobileNumber.Trim(), Address = model.Address.Trim(),
            PaymentMethod = "cash", IsPaid = false, OrderStatusId = pending.Id,
            Subtotal = quote.Subtotal, Discount = quote.Discount, Shipping = quote.Shipping,
            Total = quote.Total, CouponCode = model.CouponCode?.Trim().ToUpperInvariant()
        };
        foreach (var line in lines)
        {
            var changed = await db.Stocks.Where(s => s.BookId == line.BookId && s.Quantity >= line.Quantity)
                .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.Quantity, s => s.Quantity - line.Quantity));
            if (changed != 1) throw new InvalidOperationException("Stock changed. Please review your cart.");
            var book = books[line.BookId];
            order.OrderDetail.Add(new OrderDetail
            {
                BookId = book.Id, Quantity = line.Quantity, UnitPrice = book.Price,
                BookName = book.BookName, AuthorName = book.AuthorName, GenreName = book.Genre.GenreName
            });
        }
        db.Orders.Add(order);
        db.CartDetails.RemoveRange(lines);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }
}
