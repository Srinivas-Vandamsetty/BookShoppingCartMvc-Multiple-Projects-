using BookShoppingCart.Data.Data;
using BookShoppingCartMvcUI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace BookShoppingCartMvcUI.Controllers;

[Authorize(Roles = nameof(Roles.Admin))]
public sealed class AdminUsersController(ApplicationDbContext db, UserManager<IdentityUser> users, RoleManager<IdentityRole> roles) : Controller
{
    private static readonly string[] ApplicationRoles = ["Admin", "Employee", "Customer"];

    public async Task<IActionResult> Index(string? search)
    {
        var normalizedSearch = search?.Trim();
        var query = users.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var term = $"%{normalizedSearch}%";
            query = query.Where(user => EF.Functions.Like(user.UserName!, term) || EF.Functions.Like(user.Email!, term));
        }
        var userRows = await query.OrderBy(user => user.UserName)
            .Select(user => new { user.Id, user.UserName, user.Email, user.EmailConfirmed }).ToListAsync();
        var ids = userRows.Select(user => user.Id).ToList();
        var roleRows = await (from assignment in db.UserRoles
                              join role in db.Roles on assignment.RoleId equals role.Id
                              where ids.Contains(assignment.UserId)
                              select new { assignment.UserId, Role = role.Name! }).ToListAsync();
        var rolesByUser = roleRows.GroupBy(row => row.UserId).ToDictionary(group => group.Key,
            group => (IReadOnlyCollection<string>)group.Select(row => row.Role).ToArray());
        ViewBag.Search = normalizedSearch;
        return View(userRows.Select(user => new AdminUserRoleListItem
        {
            Id = user.Id,
            UserName = user.UserName ?? "Unnamed user",
            Email = user.Email ?? "No email",
            EmailConfirmed = user.EmailConfirmed,
            Role = DisplayRole(rolesByUser.GetValueOrDefault(user.Id))
        }));
    }

    public async Task<IActionResult> EditRole(string id)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null) return NotFound();
        ViewBag.Roles = ApplicationRoles;
        return View(new UpdateUserRoleViewModel
        {
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = DisplayRole(await users.GetRolesAsync(user))
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditRole(UpdateUserRoleViewModel model)
    {
        var user = await users.FindByIdAsync(model.UserId);
        if (user is null) return NotFound();
        ViewBag.Roles = ApplicationRoles;
        model.UserName = user.UserName ?? string.Empty;
        model.Email = user.Email ?? string.Empty;
        if (!ApplicationRoles.Contains(model.Role, StringComparer.Ordinal)) ModelState.AddModelError(nameof(model.Role), "Select a valid application role.");
        if (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier)) ModelState.AddModelError(string.Empty, "You cannot change your own role.");
        if (!ModelState.IsValid) return View(model);

        await EnsureRolesAsync();
        var currentRoles = await users.GetRolesAsync(user);
        if (currentRoles.Contains("Admin", StringComparer.Ordinal) && model.Role != "Admin" && (await users.GetUsersInRoleAsync("Admin")).Count <= 1)
        {
            ModelState.AddModelError(string.Empty, "At least one administrator must remain assigned.");
            return View(model);
        }
        var remove = currentRoles.Where(role => ApplicationRoles.Contains(role, StringComparer.Ordinal) || role == "User").ToArray();
        if (remove.Length > 0)
        {
            var removeResult = await users.RemoveFromRolesAsync(user, remove);
            if (!removeResult.Succeeded) return RoleError(model, removeResult);
        }
        var addResult = await users.AddToRoleAsync(user, model.Role);
        if (!addResult.Succeeded) return RoleError(model, addResult);
        var stampResult = await users.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded) return RoleError(model, stampResult);
        TempData["successMessage"] = $"{user.UserName}'s role was updated to {model.Role}. They must sign in again.";
        return RedirectToAction(nameof(Index));
    }

    private static string DisplayRole(IEnumerable<string>? roleNames) => roleNames?.FirstOrDefault(role => ApplicationRoles.Contains(role, StringComparer.Ordinal))
        ?? (roleNames?.Contains("User", StringComparer.Ordinal) == true ? "Customer" : "Customer");

    private async Task EnsureRolesAsync()
    {
        foreach (var role in ApplicationRoles)
            if (!await roles.RoleExistsAsync(role))
            {
                var result = await roles.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded) throw new ValidationException("Unable to initialize application roles.");
            }
    }

    private IActionResult RoleError(UpdateUserRoleViewModel model, IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }
}
