using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using BookStoreCore.Security;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCart.Data.Repositories;

public static class OrderTransitions
{
    public static bool Allows(string from, string to) => from == to || (from, to) is
        ("Pending", "Shipped") or ("Pending", "Cancelled") or ("Shipped", "Delivered");
}

public sealed class CommerceOperations(ApplicationDbContext db, ICurrentUser user)
{
    public Task<Book?> FindBook(int id, CancellationToken ct) => db.Books.AsNoTracking().Include(b => b.Genre)
        .Include(b => b.Stock).SingleOrDefaultAsync(b => b.Id == id && !b.IsArchived, ct);
    public Task<Order?> FindOrder(int id, CancellationToken ct) => db.Orders.AsNoTracking()
        .Include(o => o.OrderStatus).Include(o => o.OrderDetail).ThenInclude(d => d.Book).ThenInclude(b => b.Genre)
        .SingleOrDefaultAsync(o => o.Id == id && !o.IsDeleted && (user.IsAdmin || o.UserId == user.UserId), ct);
    public Task<Order> FindCheckout(Guid key, CancellationToken ct) => db.Orders.AsNoTracking()
        .SingleAsync(o => o.UserId == user.UserId && o.CheckoutKey == key, ct);

    public async Task<List<Order>> SearchOrders(int page, bool admin, string? status, string? search, CancellationToken ct)
    {
        if (admin) RequireAdmin();
        var query = db.Orders.AsNoTracking().Where(o => !o.IsDeleted);
        if (!admin) { var owner = user.UserId; query = query.Where(o => o.UserId == owner); }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.OrderStatus.StatusName == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = int.TryParse(term, out var id) ? query.Where(o => o.Id == id) : query.Where(o => o.Name.Contains(term) || o.Email.Contains(term));
        }
        return await query.OrderByDescending(o => o.CreateDate).ThenByDescending(o => o.Id)
            .Skip((page - 1) * 50).Take(51).Include(o => o.OrderStatus).Include(o => o.OrderDetail).ThenInclude(d => d.Book).ThenInclude(b => b.Genre).AsSplitQuery().ToListAsync(ct);
    }

    public Task<List<StockDisplayModel>> Inventory(int page, string? search, int? threshold, CancellationToken ct)
    {
        RequireAdmin();
        var query = db.Books.AsNoTracking().Where(b => !b.IsArchived);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(b => b.BookName.Contains(search));
        if (threshold.HasValue) query = query.Where(b => (b.Stock == null ? 0 : b.Stock.Quantity) <= threshold.Value);
        return query.OrderBy(b => b.Id).Skip((page - 1) * 50).Take(51).Select(b => new StockDisplayModel
        { BookId = b.Id, BookName = b.BookName, Quantity = b.Stock == null ? 0 : b.Stock.Quantity }).ToListAsync(ct);
    }

    public Task AdjustStock(int id, InventoryAdjustmentRequest request, CancellationToken ct) => AuditAsync(
        "inventory", id, request.OperationId, request.Reason, request, async () =>
        {
            if (!await db.Books.AnyAsync(b => b.Id == id && !b.IsArchived, ct)) throw new KeyNotFoundException("Book not found.");
            var stock = await db.Stocks.AsNoTracking().SingleOrDefaultAsync(s => s.BookId == id, ct);
            var original = stock?.Quantity ?? 0;
            var next = (long)original + request.Delta;
            if (original != request.ExpectedQuantity) throw new InvalidOperationException("Stock changed. Reload the stock page before retrying.");
            if (next is < 0 or > int.MaxValue) throw new ArgumentException("Resulting stock quantity is outside the allowed range.");
            if (stock is null) db.Stocks.Add(new Stock { BookId = id, Quantity = (int)next });
            else if (await db.Stocks.Where(s => s.Id == stock.Id && s.Quantity == original)
                .ExecuteUpdateAsync(set => set.SetProperty(s => s.Quantity, (int)next), ct) != 1)
                throw new InvalidOperationException("Stock changed. Reload the stock page before retrying.");
            return (original.ToString(CultureInfo.InvariantCulture), next.ToString(CultureInfo.InvariantCulture));
        }, ct);

    public Task RecordCash(int id, CashCollectionRequest request, CancellationToken ct) => AuditAsync(
        "cash-collection", id, request.OperationId, request.Reason, request, async () =>
        {
            var order = await FindOrder(id, ct) ?? throw new KeyNotFoundException("Order not found.");
            if (order.PaymentMethod != "cash") throw new InvalidOperationException("Only cash orders can be marked collected here.");
            if (order.IsPaid != request.ExpectedCollected || (request.Collected && order.OrderStatus.StatusName == "Cancelled"))
                throw new InvalidOperationException("The order changed or cannot collect cash. Reload and retry.");
            if (await db.Orders.Where(o => o.Id == id && o.IsPaid == request.ExpectedCollected && o.OrderStatusId == order.OrderStatusId)
                .ExecuteUpdateAsync(set => set.SetProperty(o => o.IsPaid, request.Collected), ct) != 1)
                throw new InvalidOperationException("The order changed. Reload and retry.");
            return (order.IsPaid.ToString(), request.Collected.ToString());
        }, ct);

    public Task UpdateStatus(int id, OrderStatusRequest request, CancellationToken ct) => AuditAsync(
        "order-status", id, request.OperationId, request.Reason, request, async () =>
        {
            var order = await FindOrder(id, ct) ?? throw new KeyNotFoundException("Order not found.");
            var next = await db.OrderStatuses.AsNoTracking().SingleOrDefaultAsync(s => s.Id == request.StatusId, ct)
                ?? throw new ArgumentException("Unknown order status.");
            var before = order.OrderStatus.StatusName;
            if (order.OrderStatusId != request.ExpectedStatusId || !OrderTransitions.Allows(before, next.StatusName))
                throw new InvalidOperationException("This order status transition is not allowed. Reload the order and try again.");
            if (before != next.StatusName && next.StatusName == "Cancelled" && order.IsPaid)
                throw new InvalidOperationException("A paid order cannot be cancelled through fulfillment. Resolve its payment first.");
            if (await db.Orders.Where(o => o.Id == id && o.OrderStatusId == order.OrderStatusId && o.IsPaid == order.IsPaid)
                .ExecuteUpdateAsync(set => set.SetProperty(o => o.OrderStatusId, next.Id), ct) != 1)
                throw new InvalidOperationException("Order changed. Reload and retry.");
            if (before != next.StatusName && next.StatusName == "Cancelled")
            {
                foreach (var line in order.OrderDetail.OrderBy(d => d.BookId))
                {
                    await LockAsync("inventory:" + line.BookId, ct);
                    var stock = await db.Stocks.AsNoTracking().SingleOrDefaultAsync(s => s.BookId == line.BookId, ct);
                    if (stock is null) db.Stocks.Add(new Stock { BookId = line.BookId, Quantity = line.Quantity });
                    else if (await db.Stocks.Where(s => s.Id == stock.Id && s.Quantity <= int.MaxValue - line.Quantity)
                        .ExecuteUpdateAsync(set => set.SetProperty(s => s.Quantity, s => s.Quantity + line.Quantity), ct) != 1)
                        throw new InvalidOperationException("Stock could not be restored safely.");
                }
            }
            return (before, next.StatusName);
        }, ct);

    private void RequireAdmin()
    {
        if (!user.IsAdmin) throw new UnauthorizedAccessException("Administrator access required.");
    }

    private async Task AuditAsync<T>(string kind, int id, Guid operationId, string reason, T request,
        Func<Task<(string Before, string After)>> mutate, CancellationToken ct)
    {
        RequireAdmin();
        if (id <= 0 || operationId == Guid.Empty || string.IsNullOrWhiteSpace(reason) || reason.Length > 300)
            throw new ArgumentException("A valid target, operation ID, and reason are required.");
        var actor = user.UserId;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { kind, id, actor, request }))));
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await LockAsync("operation:" + operationId.ToString("N"), ct);
        var prior = await db.CommerceAuditEvents.AsNoTracking().SingleOrDefaultAsync(e => e.Id == operationId, ct);
        if (prior is not null)
        {
            if (prior.RequestHash != hash) throw new InvalidOperationException("This operation ID was already used for a different request.");
            await tx.CommitAsync(ct);
            return;
        }
        await LockAsync((kind == "inventory" ? "inventory:" : "order:") + id, ct);
        var change = await mutate();
        db.CommerceAuditEvents.Add(new CommerceAuditEvent { Id = operationId, EntityId = id, Kind = kind,
            ActorId = actor, Before = change.Before, After = change.After, Reason = reason.Trim(), RequestHash = hash });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task LockAsync(string resource, CancellationToken ct)
    {
        if (db.Database.IsSqlServer()) await db.Database.ExecuteSqlInterpolatedAsync($@"
DECLARE @result int;
EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
IF @result < 0 THROW 51012, 'Commerce operation is busy.', 1;", ct);
    }
}
