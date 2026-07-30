using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Areas.Admin.Controllers;

/// <summary>
/// Shared base for every admin-panel controller. Locks the whole area down to
/// the Admin role so each controller stays focused on its own actions.
/// </summary>
[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public abstract class AdminControllerBase : Controller
{
    protected void NotifySuccess(string message) => TempData["Success"] = message;
    protected void NotifyError(string message) => TempData["Error"] = message;
}
