using HomeServices.Application.Contracts;
using Microsoft.AspNetCore.Mvc;
using SharedDtos = HomeServices.Shared.Dtos;
using SharedEnums = HomeServices.Shared.Enums;

namespace HomeServices.Mvc.Areas.Admin.Controllers;

/// <summary>
/// Admin user management: list every account (from the Identity microservice) and
/// activate/deactivate them. User data lives in Identity; this is a thin proxy.
/// </summary>
public class UsersController : AdminControllerBase
{
    private readonly IIdentityApiClient _identity;

    public UsersController(IIdentityApiClient identity)
    {
        _identity = identity;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _identity.GetAllUsersAsync(cancellationToken);
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(id, out var userId))
        {
            var ok = await _identity.ToggleUserStatusAsync(userId, cancellationToken);
            NotifySuccess(ok ? "وضعیت کاربر تغییر کرد." : "عملیات ناموفق بود.");
        }
        return RedirectToAction(nameof(Index));
    }
}
