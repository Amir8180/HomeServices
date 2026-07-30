using HomeServices.Domain.Common;

namespace HomeServices.Domain.Entities;

/// <summary>
/// A concrete service offered under a category (e.g. "Water heater installation"
/// under Plumbing). Has a base price, an optional icon/thumbnail and an estimated
/// duration. Fixed-price services can be booked instantly; others go through the
/// request → proposal flow.
/// </summary>
public class Service : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public decimal? BasePrice { get; set; }
    public string? IconUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? EstimatedDurationMinutes { get; set; }

    /// <summary>If true the service can be booked instantly at BasePrice; otherwise quotes are required.</summary>
    public bool IsFixedPrice { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Category Category { get; set; } = null!;
    public ICollection<ServiceImage> Images { get; set; } = new List<ServiceImage>();
    public ICollection<ServiceRequest> Requests { get; set; } = new List<ServiceRequest>();
}
