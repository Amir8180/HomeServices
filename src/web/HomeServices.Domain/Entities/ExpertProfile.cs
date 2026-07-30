using HomeServices.Domain.Common;

namespace HomeServices.Domain.Entities;

/// <summary>
/// The professional profile of an expert, kept in the main service (not the
/// Identity service). Mirrors the Angi pro-profile: business name, logo, bio,
/// service area, ratings and a portfolio of past work. The UserId links back to
/// the Identity service via a Guid.
/// </summary>
public class ExpertProfile : BaseEntity
{
    /// <summary>Expert user id (Guid) from the Identity service.</summary>
    public Guid UserId { get; set; }

    public string BusinessName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? ServiceArea { get; set; }
    public string? City { get; set; }

    /// <summary>Free-text business hours, e.g. "Sat-Thu 8:00-20:00".</summary>
    public string? BusinessHours { get; set; }

    public bool IsVerified { get; set; }
    public bool IsApproved { get; set; }

    /// <summary>Aggregated average rating (1-5). Updated when reviews are approved.</summary>
    public double RatingAverage { get; set; }

    public int ReviewCount { get; set; }
    public int JobsCompleted { get; set; }

    /// <summary>Average response time in minutes, for display on the pro card.</summary>
    public int? ResponseTimeMinutes { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<ExpertCategory> ExpertCategories { get; set; } = new List<ExpertCategory>();
    public ICollection<ExpertService> ExpertServices { get; set; } = new List<ExpertService>();
    public ICollection<ExpertPortfolioImage> PortfolioImages { get; set; } = new List<ExpertPortfolioImage>();
    public ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
}
