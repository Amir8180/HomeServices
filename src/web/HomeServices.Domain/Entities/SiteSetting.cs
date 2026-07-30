using HomeServices.Domain.Common;

namespace HomeServices.Domain.Entities;

/// <summary>
/// A key/value site setting editable from the admin panel — used for branding
/// (logo, favicon, banner URLs), theme colors and site-wide text (hero title,
/// contact info, etc.).
/// </summary>
public class SiteSetting : BaseEntity
{
    /// <summary>Stable key, e.g. "Site.LogoUrl", "Theme.PrimaryColor", "Hero.Title".</summary>
    public string Key { get; set; } = string.Empty;

    public string? Value { get; set; }

    /// <summary>Logical grouping for display in the admin UI (e.g. "Branding", "Theme", "Contact").</summary>
    public string? Group { get; set; }

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }
}
