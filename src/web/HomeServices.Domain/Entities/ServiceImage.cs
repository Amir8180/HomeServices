using HomeServices.Domain.Common;

namespace HomeServices.Domain.Entities;

/// <summary>
/// An image attached to a service (gallery). One image can be marked as primary.
/// A thumbnail URL is generated automatically by the file service.
/// </summary>
public class ServiceImage : BaseEntity
{
    public int ServiceId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }

    // Navigation
    public Service Service { get; set; } = null!;
}
