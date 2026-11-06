using BookShoppingCart.Models.Models.DTOs;
using BookStoreCore.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
namespace BookShoppingCart.WebAPI.Controllers;

[ApiController, Authorize, Route("api/customers/me"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CustomersController(UserManager<IdentityUser> users, ICurrentUser current) : ControllerBase
{
    [HttpGet]
    public async Task<CustomerProfileResponse> Get() => Map(await Find());
    [HttpPatch]
    public async Task<CustomerProfileResponse> Update(CustomerProfileRequest request)
    {
        var user = await Find();
        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        if (phone != user.PhoneNumber)
        {
            // This contact field is unverified. Preserve the password security stamp and existing session.
            user.PhoneNumber = phone;
            user.PhoneNumberConfirmed = false;
            if (!(await users.UpdateAsync(user)).Succeeded) throw new InvalidOperationException("Profile changed. Reload and retry.");
        }
        return Map(user);
    }
    private async Task<IdentityUser> Find() => await users.FindByIdAsync(current.UserId) ?? throw new KeyNotFoundException("Customer not found.");
    private static CustomerProfileResponse Map(IdentityUser user) => new(user.Id, user.UserName ?? "", user.Email ?? "", user.PhoneNumber, user.EmailConfirmed);
}

[ApiController, Route("api/payment-methods")]
public sealed class PaymentMethodsController : ControllerBase
{
    [HttpGet, AllowAnonymous]
    public PaymentMethodResponse[] Get() => [new("cash", "Cash on delivery")];
}
