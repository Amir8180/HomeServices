using HomeServices.Domain.Common;

namespace HomeServices.Domain.Entities;

/// <summary>
/// A before/after portfolio image showcasing an expert's past work, displayed on
/// the expert's public profile page.
/// </summary>
public class ExpertPortfolioImage : BaseEntity
{
    public int ExpertProfileId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    // Navigation
    public ExpertProfile ExpertProfile { get; set; } = null!;
}
