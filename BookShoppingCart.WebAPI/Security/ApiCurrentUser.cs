using System.Security.Claims;
using BookStoreCore.Security;
namespace BookShoppingCart.WebAPI.Security;
public sealed class ApiCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string UserId => accessor.HttpContext?.User.FindFirstValue("sub") ?? throw new UnauthorizedAccessException("Please sign in.");
    public bool IsAdmin => accessor.HttpContext?.User.IsInRole("Admin") == true;
}
