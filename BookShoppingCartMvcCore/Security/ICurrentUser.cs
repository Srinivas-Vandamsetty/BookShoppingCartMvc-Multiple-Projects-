namespace BookStoreCore.Security;
public interface ICurrentUser
{
    string UserId { get; }
    bool IsAdmin { get; }
    bool IsEmployee { get; }
    bool CanManageOrders { get; }
}
