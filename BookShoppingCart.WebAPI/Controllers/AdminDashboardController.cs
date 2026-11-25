using BookShoppingCart.Data.Repositories;
using BookShoppingCart.Models.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCart.WebAPI.Controllers;

[ApiController, Authorize(Roles = "Admin"), Route("api/admin/dashboard"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminDashboardController(AdminDashboardService dashboard) : ControllerBase
{
    [HttpGet]
    public Task<AdminDashboardResponse> Get(CancellationToken cancellationToken) => dashboard.GetAsync(cancellationToken);
}
