using HomeServices.Domain.Enums;

namespace HomeServices.Application.Dtos;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CategoryGroup Group { get; set; }
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int? ParentCategoryId { get; set; }
    public string? ParentName { get; set; }
    public int SubCategoryCount { get; set; }
    public int ServiceCount { get; set; }
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CategoryGroup Group { get; set; }
    public string? IconUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ParentCategoryId { get; set; }
}

public class UpdateCategoryDto : CreateCategoryDto { }
