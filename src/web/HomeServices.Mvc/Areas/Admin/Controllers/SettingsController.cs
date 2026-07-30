using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HomeServices.Mvc.Areas.Admin.Controllers;

/// <summary>
/// Admin site settings: manage key/value configuration (site name, contact info,
/// social links) that drives the public layout footer and header.
/// </summary>
public class SettingsController : AdminControllerBase
{
    private readonly ISiteSettingService _settings;

    public SettingsController(ISiteSettingService settings)
    {
        _settings = settings;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await _settings.GetAllAsync(cancellationToken);
        return View(settings);
    }

    [HttpGet]
    public IActionResult Create() => View(new SiteSettingViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SiteSettingViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        await _settings.UpsertAsync(new UpsertSiteSettingDto
        {
            Key = model.Key,
            Value = model.Value,
            Description = model.Description,
        }, cancellationToken);
        NotifySuccess("تنظیمات ذخیره شد.");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var all = await _settings.GetAllAsync(cancellationToken);
        var s = all.FirstOrDefault(x => x.Id == id);
        if (s == null) return NotFound();
        return View(new SiteSettingViewModel { Id = s.Id, Key = s.Key, Value = s.Value, Description = s.Description });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SiteSettingViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        await _settings.UpsertAsync(new UpsertSiteSettingDto
        {
            Key = model.Key,
            Value = model.Value,
            Description = model.Description,
        }, cancellationToken);
        NotifySuccess("تنظیمات به‌روزرسانی شد.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var ok = await _settings.DeleteAsync(id, cancellationToken);
        NotifySuccess(ok ? "تنظیمات حذف شد." : "حذف ناموفق بود.");
        return RedirectToAction(nameof(Index));
    }
}

public class SiteSettingViewModel
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Description { get; set; }
}
