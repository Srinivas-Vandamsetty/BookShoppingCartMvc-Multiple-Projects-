using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    public class UserOrderRepository : IUserOrderRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;

        // Constructor to inject dependencies: DbContext, UserManager, and HttpContextAccessor
        public UserOrderRepository(ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        // Change the order status for a specific order
        public async Task ChangeOrderStatus(UpdateOrderStatusModel data)
        {
            var order = await _db.Orders.FindAsync(data.OrderId);

            // If order doesn't exist, throw error
            if (order == null)
            {
                throw new InvalidOperationException($"order with id:{data.OrderId} does not found");
            }

            order.OrderStatusId = data.OrderStatusId;
            await _db.SaveChangesAsync();
        }

        // Get a specific order by ID
        public async Task<Order?> GetOrderById(int id)
        {
            return await _db.Orders.FindAsync(id);
        }

        // Get list of all order statuses
        public async Task<IEnumerable<OrderStatus>> GetOrderStatuses()
        {
            return await _db.OrderStatuses.ToListAsync();
        }

        // Toggle the payment status (paid/unpaid) of an order
        public async Task TogglePaymentStatus(int orderId)
        {
            var order = await _db.Orders.FindAsync(orderId);

            // If order doesn't exist, throw error
            if (order == null)
            {
                throw new InvalidOperationException($"order with id:{orderId} does not found");
            }

            // Switch the payment status
            order.IsPaid = !order.IsPaid;
            await _db.SaveChangesAsync();
        }

        // Get orders for the current user or all users if getAll is true
        public async Task<IEnumerable<Order>> UserOrders(bool getAll = false)
        {
            // Load related data: order status, order details, book, and genre
            var orders = _db.Orders
                           .Include(x => x.OrderStatus)
                           .Include(x => x.OrderDetail)
                           .ThenInclude(x => x.Book)
                           .ThenInclude(x => x.Genre)
                           .AsQueryable();

            // If only current user's orders are needed
            if (!getAll)
            {
                var userId = GetUserId();

                // If user is not logged in
                if (string.IsNullOrEmpty(userId))
                    throw new Exception("User is not logged-in");

                orders = orders.Where(a => a.UserId == userId);
                return await orders.ToListAsync();
            }

            // Return all orders
            return await orders.ToListAsync();
        }

        // Get current logged-in user's ID
        private string GetUserId()
        {
            var principal = _httpContextAccessor.HttpContext.User;
            string userId = _userManager.GetUserId(principal);
            return userId;
        }
    }
}
