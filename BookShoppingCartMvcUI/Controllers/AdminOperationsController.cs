using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers;

// Admin operations controller, accessible only to admin users
[Authorize(Roles = nameof(Roles.Admin))]
public class AdminOperationsController : Controller
{
    // Loads the admin dashboard view
    public IActionResult Dashboard()
    {
        return View();
    }
}
