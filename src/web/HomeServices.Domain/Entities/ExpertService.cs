using HomeServices.Domain.Common;

namespace HomeServices.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between an expert profile and the
/// services they offer, carrying an optional custom price that overrides the
/// service's base price for that expert.
/// </summary>
public class ExpertService : BaseEntity
{
    public int ExpertProfileId { get; set; }
    public int ServiceId { get; set; }

    /// <summary>Expert-specific price; null means use the service base price.</summary>
    public decimal? CustomPrice { get; set; }

    // Navigation
    public ExpertProfile ExpertProfile { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
