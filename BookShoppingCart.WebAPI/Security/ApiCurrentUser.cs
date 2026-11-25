using System.Security.Claims;
using BookStoreCore.Security;
namespace BookShoppingCart.WebAPI.Security;
public sealed class ApiCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string UserId => accessor.HttpContext?.User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException("Please sign in.");
    public bool IsAdmin => accessor.HttpContext?.User.IsInRole("Admin") == true;
    public bool IsEmployee => accessor.HttpContext?.User.IsInRole("Employee") == true;
    public bool CanManageOrders => IsAdmin || IsEmployee;
}
