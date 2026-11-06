using System.Security.Claims;
using BookStoreCore.Security;
namespace BookShoppingCartMvcUI.Services;
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string UserId => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("Please sign in.");
    public bool IsAdmin => accessor.HttpContext?.User.IsInRole("Admin") == true;
}
