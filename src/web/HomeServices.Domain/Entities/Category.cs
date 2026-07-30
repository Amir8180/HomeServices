using HomeServices.Domain.Common;
using HomeServices.Domain.Enums;

namespace HomeServices.Domain.Entities;

/// <summary>
/// A service category (e.g. Plumbing, Electrical, House Cleaning). Categories are
/// grouped Angi-style (Interior/Exterior/Lawn&amp;Garden/Other) and support a
/// self-referencing parent for sub-categories.
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CategoryGroup Group { get; set; }
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Parent category id for sub-categories; null for top-level categories.</summary>
    public int? ParentCategoryId { get; set; }

    // Navigation
    public Category? Parent { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Service> Services { get; set; } = new List<Service>();
}
