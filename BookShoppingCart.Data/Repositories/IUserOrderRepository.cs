using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Data.Repositories
{
    // This interface defines methods for handling user orders,
    // including retrieving orders, updating status, and toggling payment.
    public interface IUserOrderRepository
    {
        Task<IEnumerable<Order>> UserOrders(bool getAll = false);
        Task ChangeOrderStatus(UpdateOrderStatusModel data);
        Task TogglePaymentStatus(int orderId);
        Task<Order?> GetOrderById(int id);
        Task<IEnumerable<OrderStatus>> GetOrderStatuses();
    }
}
