using BookShoppingCart.Data.Data;
using BookShoppingCart.Models.Models;
using BookShoppingCartMvcUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookShoppingCartMvcUI.Controllers;

[Authorize(Roles = "Admin,Employee")]
public sealed class AdminCustomersController(ApplicationDbContext db, UserManager<IdentityUser> users) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var adminIds = (await users.GetUsersInRoleAsync(nameof(Roles.Admin)))
            .Select(user => user.Id)
            .ToHashSet(StringComparer.Ordinal);
        var normalizedSearch = search?.Trim();
        var query = users.Users.AsNoTracking().Where(user => !adminIds.Contains(user.Id));

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var term = $"%{normalizedSearch}%";
            query = query.Where(user => EF.Functions.Like(user.UserName!, term) ||
                                        EF.Functions.Like(user.Email!, term) ||
                                        EF.Functions.Like(user.PhoneNumber!, term));
        }

        var customerRows = await query
            .OrderBy(user => user.UserName)
            .Select(user => new { user.Id, user.UserName, user.Email, user.PhoneNumber, user.EmailConfirmed, user.LockoutEnd })
            .ToListAsync();
        var customerIds = customerRows.Select(customer => customer.Id).ToList();
        var rolesByUser = await (from assignment in db.UserRoles
                                 join role in db.Roles on assignment.RoleId equals role.Id
                                 where customerIds.Contains(assignment.UserId)
                                 select new { assignment.UserId, Role = role.Name! })
            .GroupBy(item => item.UserId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(item => item.Role).ToArray());
        var orderCounts = await db.Orders.AsNoTracking()
            .Where(order => customerIds.Contains(order.UserId) && !order.IsDeleted)
            .GroupBy(order => order.UserId)
            .Select(group => new { UserId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.UserId, item => item.Count);

        ViewBag.Search = normalizedSearch;
        var now = DateTimeOffset.UtcNow;
        return View(customerRows.Select(customer => new AdminCustomerListItem
        {
            Id = customer.Id,
            UserName = customer.UserName ?? "Unnamed customer",
            Email = customer.Email ?? "No email",
            PhoneNumber = customer.PhoneNumber,
            Role = rolesByUser.TryGetValue(customer.Id, out var assignedRoles)
                ? assignedRoles.FirstOrDefault(role => role is "Employee" or "Customer") ??
                  (assignedRoles.Contains("User", StringComparer.Ordinal) ? "Customer" : "Customer")
                : "Customer",
            EmailConfirmed = customer.EmailConfirmed,
            IsLockedOut = customer.LockoutEnd is { } lockoutEnd && lockoutEnd > now,
            OrderCount = orderCounts.GetValueOrDefault(customer.Id)
        }));
    }

    [Authorize(Roles = nameof(Roles.Admin))]
    public async Task<IActionResult> Edit(string id)
    {
        var customer = await GetCustomerAsync(id);
        if (customer is null) return NotFound();

        return View(new EditCustomerViewModel
        {
            UserName = customer.UserName ?? string.Empty,
            Email = customer.Email ?? string.Empty,
            PhoneNumber = customer.PhoneNumber
        });
    }

    [HttpPost]
    [Authorize(Roles = nameof(Roles.Admin))]
    public async Task<IActionResult> Edit(string id, EditCustomerViewModel model)
    {
        var customer = await GetCustomerAsync(id);
        if (customer is null) return NotFound();
        if (!ModelState.IsValid) return View(model);

        customer.UserName = model.UserName.Trim();
        customer.Email = model.Email.Trim();
        customer.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        var result = await users.UpdateAsync(customer);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        TempData["successMessage"] = "Customer details updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = nameof(Roles.Admin))]
    public async Task<IActionResult> Delete(string id)
    {
        var customer = await GetCustomerAsync(id);
        if (customer is null) return NotFound();
        if (customer.Id == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            TempData["errorMessage"] = "You cannot delete your own administrator account.";
            return RedirectToAction(nameof(Index));
        }

        var hasOrders = await db.Orders.AnyAsync(order => order.UserId == customer.Id && !order.IsDeleted);
        if (hasOrders)
        {
            TempData["errorMessage"] = "This customer cannot be deleted because they have order history.";
            return RedirectToAction(nameof(Index));
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        var carts = await db.ShoppingCarts.Where(cart => cart.UserId == customer.Id).ToListAsync();
        db.ShoppingCarts.RemoveRange(carts);
        await db.SaveChangesAsync();
        var result = await users.DeleteAsync(customer);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync();
            TempData["errorMessage"] = "The customer could not be deleted.";
            return RedirectToAction(nameof(Index));
        }

        await transaction.CommitAsync();
        TempData["successMessage"] = "Customer deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IdentityUser?> GetCustomerAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var user = await users.FindByIdAsync(id);
        return user is not null && !await users.IsInRoleAsync(user, nameof(Roles.Admin)) ? user : null;
    }
}
