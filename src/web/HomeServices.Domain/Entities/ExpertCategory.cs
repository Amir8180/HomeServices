using HomeServices.Domain.Common;

namespace HomeServices.Domain.Entities;

/// <summary>
/// Join entity for the many-to-many relationship between an expert profile and the
/// categories (trades) the expert works in.
/// </summary>
public class ExpertCategory : BaseEntity
{
    public int ExpertProfileId { get; set; }
    public int CategoryId { get; set; }

    // Navigation
    public ExpertProfile ExpertProfile { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
