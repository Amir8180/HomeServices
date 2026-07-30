namespace HomeServices.Application.Dtos;

public class ExpertProfileDto
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? ServiceArea { get; set; }
    public string? City { get; set; }
    public string? BusinessHours { get; set; }
    public bool IsVerified { get; set; }
    public bool IsApproved { get; set; }
    public double RatingAverage { get; set; }
    public int ReviewCount { get; set; }
    public int JobsCompleted { get; set; }
    public int? ResponseTimeMinutes { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<int> CategoryIds { get; set; } = Array.Empty<int>();
    public IReadOnlyList<string> CategoryNames { get; set; } = Array.Empty<string>();
    public IReadOnlyList<ExpertPortfolioImageDto> PortfolioImages { get; set; } = Array.Empty<ExpertPortfolioImageDto>();
}

public class ExpertPortfolioImageDto
{
    public int Id { get; set; }
    public int ExpertProfileId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class ExpertProfileFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public string? City { get; set; }
    public bool? IsApproved { get; set; }
    public bool ActiveOnly { get; set; } = true;
}

public class CreateExpertProfileDto
{
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? ServiceArea { get; set; }
    public string? City { get; set; }
    public string? BusinessHours { get; set; }
    public int? ResponseTimeMinutes { get; set; }
    public List<int> CategoryIds { get; set; } = new();
}

public class UpdateExpertProfileDto
{
    public string BusinessName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? ServiceArea { get; set; }
    public string? City { get; set; }
    public string? BusinessHours { get; set; }
    public int? ResponseTimeMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public List<int> CategoryIds { get; set; } = new();
}
