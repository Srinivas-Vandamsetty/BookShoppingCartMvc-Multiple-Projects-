using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    // Service interface for handling user order operations such as fetching orders,
    // changing status, toggling payment, and retrieving order details.
    public interface IUserOrderService
    {
        Task<IEnumerable<Order>> GetUserOrders(bool getAll = false);
        Task ChangeOrderStatus(UpdateOrderStatusModel data);
        Task TogglePaymentStatus(int orderId);
        Task<Order?> GetOrderById(int id);
        Task<IEnumerable<OrderStatus>> GetOrderStatuses();
    }
}
