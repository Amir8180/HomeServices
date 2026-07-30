using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HomeServices.Application.Contracts;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Mvc.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SharedDtos = HomeServices.Shared.Dtos;
using SharedEnums = HomeServices.Shared.Enums;

namespace HomeServices.Mvc.Controllers;

public class AccountController : Controller
{
    private readonly IIdentityApiClient _identity;
    private readonly IExpertProfileService _experts;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IIdentityApiClient identity,
        IExpertProfileService experts,
        ILogger<AccountController> logger)
    {
        _identity = identity;
        _experts = experts;
        _logger = logger;
    }

    // -------------------- Login --------------------
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated ?? false) return RedirectToDashboard();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _identity.LoginAsync(model.Email, model.Password, HttpContext.RequestAborted);
        if (!result.Succeeded || result.Data?.User == null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "ایمیل یا رمز عبور اشتباه است.");
            return View(model);
        }

        await SignInAsync(result.Data.AccessToken, result.Data.User!, model.RememberMe);
        _logger.LogInformation("User {Email} logged in.", model.Email);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToDashboard();
    }

    // -------------------- Register --------------------
    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated ?? false) return RedirectToDashboard();
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _identity.RegisterAsync(
            model.FullName, model.Email, model.PhoneNumber, model.Password,
            model.UserType, HttpContext.RequestAborted);

        if (!result.Succeeded || result.Data?.User == null)
        {
            var errors = result.Errors.Any()
                ? result.Errors
                : new List<string> { result.Message ?? "ثبت‌نام ناموفق بود." };
            foreach (var err in errors)
                ModelState.AddModelError(string.Empty, err);
            return View(model);
        }

        await SignInAsync(result.Data.AccessToken, result.Data.User!, false);
        _logger.LogInformation("User {Email} registered as {Type}.", model.Email, model.UserType);

        // Auto-provision an expert profile for experts so they can start quoting immediately.
        // The profile is created unapproved (IsApproved = false) until an admin verifies it.
        if (model.UserType == SharedEnums.UserType.Expert)
        {
            try
            {
                await _experts.CreateAsync(new CreateExpertProfileDto
                {
                    UserId = Guid.Parse(result.Data.User!.Id),
                    BusinessName = model.FullName,
                    CategoryIds = new List<int>(),
                }, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                // Non-fatal: the expert can complete their profile later. Log and continue.
                _logger.LogWarning(ex, "Failed to auto-create expert profile for {Email}.", model.Email);
            }
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToDashboard();
    }

    // -------------------- Profile --------------------
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!(User.Identity?.IsAuthenticated ?? false)) return RedirectToAction(nameof(Login));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return RedirectToAction(nameof(Login));

        var user = await _identity.GetUserByIdAsync(Guid.Parse(userId), HttpContext.RequestAborted);
        if (user == null) return NotFound();

        var model = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber ?? "",
            AvatarUrl = user.AvatarUrl,
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return RedirectToAction(nameof(Login));

        var ok = await _identity.UpdateProfileAsync(
            Guid.Parse(userId), model.FullName, model.AvatarUrl, model.PhoneNumber,
            HttpContext.RequestAborted);

        if (!ok)
        {
            ModelState.AddModelError(string.Empty, "به‌روزرسانی پروفایل ناموفق بود.");
            return View(model);
        }

        // Refresh the name claim in the existing cookie.
        await UpdateNameClaimAsync(model.FullName);
        TempData["Success"] = "پروفایل شما به‌روزرسانی شد.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return RedirectToAction(nameof(Login));

        var result = await _identity.ChangePasswordAsync(
            Guid.Parse(userId), model.CurrentPassword, model.NewPassword,
            HttpContext.RequestAborted);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "تغییر رمز عبور ناموفق بود.");
            return View(model);
        }

        TempData["Success"] = "رمز عبور با موفقیت تغییر کرد.";
        return RedirectToAction(nameof(Profile));
    }

    // -------------------- Logout --------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // -------------------- helpers --------------------
    private async Task SignInAsync(string accessToken, SharedDtos.UserDto user, bool isPersistent)
    {
        var principal = HomeServicesAuthExtensions.BuildPrincipal(accessToken, user);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = isPersistent, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
    }

    private async Task UpdateNameClaimAsync(string newFullName)
    {
        var identity = (ClaimsIdentity?)User.Identity;
        if (identity == null) return;
        var nameClaim = identity.FindFirst(ClaimTypes.Name);
        if (nameClaim != null) identity.RemoveClaim(nameClaim);
        identity.AddClaim(new Claim(ClaimTypes.Name, newFullName));
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }

    private IActionResult RedirectToDashboard()
    {
        if (User.IsInRole("Admin")) return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        if (User.IsInRole("Expert")) return RedirectToAction("Dashboard", "Expert");
        if (User.IsInRole("Customer")) return RedirectToAction("Dashboard", "Customer");
        return RedirectToAction("Index", "Home");
    }
}

// -------------------- View Models --------------------
public class LoginViewModel
{
    [Required(ErrorMessage = "ایمیل الزامی است.")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "رمز عبور الزامی است.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "نام و نام خانوادگی الزامی است.")]
    [StringLength(100, ErrorMessage = "نام نمی‌تواند بیش از ۱۰۰ کاراکتر باشد.")]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "ایمیل الزامی است.")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "شماره موبایل الزامی است.")]
    [Phone(ErrorMessage = "شماره موبایل معتبر نیست.")]
    public string PhoneNumber { get; set; } = "";
    [Required(ErrorMessage = "رمز عبور الزامی است.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "رمز عبور حداقل ۸ کاراکتر باشد.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
    [Required(ErrorMessage = "تکرار رمز عبور الزامی است.")]
    [Compare("Password", ErrorMessage = "رمز عبور و تکرار آن یکسان نیستند.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = "";
    public SharedEnums.UserType UserType { get; set; } = SharedEnums.UserType.Customer;
    public string? ReturnUrl { get; set; }
}

public class ProfileViewModel
{
    [Required] public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    [Phone] public string PhoneNumber { get; set; } = "";
    public string? AvatarUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [Required] [DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
    [Required] [StringLength(100, MinimumLength = 8)] [DataType(DataType.Password)] public string NewPassword { get; set; } = "";
    [Required] [Compare("NewPassword")] [DataType(DataType.Password)] public string ConfirmNewPassword { get; set; } = "";
}
