using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace BookShoppingCartMvcUI.Services;
public static class AdminProvisioning
{
    public static async Task Run(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email) || string.IsNullOrEmpty(password))
            throw new InvalidOperationException("Supply BootstrapAdmin__Email and BootstrapAdmin__Password for this one-time command.");
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        var roles = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (await users.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("The account already exists. Manage existing administrators through an authenticated administrative process.");
        if (!await roles.RoleExistsAsync("Admin")) Check(await roles.CreateAsync(new IdentityRole("Admin")));
        var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        Check(await users.CreateAsync(user, password));
        var role = await users.AddToRoleAsync(user,"Admin");
        if (!role.Succeeded)
        {
            await users.DeleteAsync(user);
            Check(role);
        }
    }
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e=>e.Description)));
    }
}
