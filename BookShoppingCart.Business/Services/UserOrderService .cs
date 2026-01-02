using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models;
using BookShoppingCart.Models.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookShoppingCart.Business.Services
{
    public class UserOrderService : IUserOrderService
    {
        // Injecting the repository to interact with order data
        private readonly IUserOrderRepository _userOrderRepository;

        public UserOrderService(IUserOrderRepository userOrderRepository)
        {
            _userOrderRepository = userOrderRepository;
        }

        // Get orders based on user or all users depending on the flag
        public async Task<IEnumerable<Order>> GetUserOrders(bool getAll = false)
        {
            return await _userOrderRepository.UserOrders(getAll);
        }

        // Change the current status of a specific order
        public async Task ChangeOrderStatus(UpdateOrderStatusModel data)
        {
            await _userOrderRepository.ChangeOrderStatus(data);
        }

        // Flip the payment status (paid/unpaid) for an order
        public async Task TogglePaymentStatus(int orderId)
        {
            await _userOrderRepository.TogglePaymentStatus(orderId);
        }

        // Get a specific order using its ID
        public async Task<Order?> GetOrderById(int id)
        {
            return await _userOrderRepository.GetOrderById(id);
        }

        // Get all available order statuses (e.g., pending, shipped)
        public async Task<IEnumerable<OrderStatus>> GetOrderStatuses()
        {
            return await _userOrderRepository.GetOrderStatuses();
        }
    }
}
