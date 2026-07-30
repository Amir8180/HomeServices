namespace HomeServices.Application.Dtos;

public class ServiceDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal? BasePrice { get; set; }
    public string? IconUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsFixedPrice { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<ServiceImageDto> Images { get; set; } = Array.Empty<ServiceImageDto>();
}

public class ServiceImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
}

public class ServiceFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool ActiveOnly { get; set; } = true;
    public string? SortBy { get; set; }
}

public class CreateServiceDto
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public decimal? BasePrice { get; set; }
    public string? IconUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsFixedPrice { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateServiceDto : CreateServiceDto { }
