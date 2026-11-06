using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using BookStoreCore.Security;
using Microsoft.EntityFrameworkCore;
namespace BookShoppingCart.Data.Repositories;
public class UserOrderRepository(ApplicationDbContext db, ICurrentUser user) : IUserOrderRepository
{
    private void RequireAdmin()
    {
        if (!user.IsAdmin) throw new UnauthorizedAccessException("Administrator access required.");
    }
    public async Task ChangeOrderStatus(UpdateOrderStatusModel data)
    {
        RequireAdmin();
        if (data.OrderId <= 0 || data.OrderStatusId <= 0) throw new ArgumentException("Select a valid order and status.");
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == data.OrderId) ?? throw new KeyNotFoundException("Order not found.");
        if (!await db.OrderStatuses.AnyAsync(s => s.Id == data.OrderStatusId)) throw new ArgumentException("Invalid order status.");
        order.OrderStatusId = data.OrderStatusId;
        await db.SaveChangesAsync();
    }
    public async Task<Order?> GetOrderById(int id)
    {
        var owner = user.UserId;
        return await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id && (user.IsAdmin || o.UserId == owner));
    }
    public async Task<IEnumerable<OrderStatus>> GetOrderStatuses() =>
        await db.OrderStatuses.AsNoTracking().OrderBy(s => s.StatusId).ToListAsync();
    public async Task TogglePaymentStatus(int orderId)
    {
        RequireAdmin();
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == orderId) ?? throw new KeyNotFoundException("Order not found.");
        if (order.PaymentMethod != "cash") throw new InvalidOperationException("Electronic payment status must be verified by its provider.");
        var updated = await db.Orders.Where(o => o.Id == orderId && o.IsPaid == order.IsPaid)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsPaid, !order.IsPaid));
        if (updated != 1) throw new InvalidOperationException("Order changed. Refresh and retry.");
    }
    public async Task<IEnumerable<Order>> UserOrders(bool getAll = false, int page = 1)
    {
        if (getAll) RequireAdmin();
        if (page is < 1 or > 100000) throw new ArgumentException("Invalid page.");
        var owner = user.UserId;
        return await db.Orders.AsNoTracking().Where(o => !o.IsDeleted && (getAll || o.UserId == owner))
            .OrderByDescending(o => o.CreateDate).ThenByDescending(o => o.Id)
            .Skip((page - 1) * 50).Take(51)
            .Include(o => o.OrderStatus).Include(o => o.OrderDetail).ThenInclude(d => d.Book).ThenInclude(b => b.Genre)
            .AsSplitQuery().ToListAsync();
    }
}
