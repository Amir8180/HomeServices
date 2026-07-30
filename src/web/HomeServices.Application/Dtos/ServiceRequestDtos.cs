using HomeServices.Domain.Enums;

namespace HomeServices.Application.Dtos;

public class ServiceRequestDto
{
    public int Id { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryIconUrl { get; set; }
    public int? ServiceId { get; set; }
    public string? ServiceTitle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public UrgencyLevel Urgency { get; set; }
    public DateTime? PreferredDate { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public bool IsHomeOwner { get; set; }
    public RequestStatus Status { get; set; }
    public int? AcceptedProposalId { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ProposalCount { get; set; }
    public IReadOnlyList<RequestImageDto> Images { get; set; } = Array.Empty<RequestImageDto>();
}

public class RequestImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
}

public class ServiceRequestFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public Guid? CustomerId { get; set; }
    public RequestStatus? Status { get; set; }
    public UrgencyLevel? Urgency { get; set; }
    public string? City { get; set; }
}

public class CreateServiceRequestDto
{
    public int CategoryId { get; set; }
    public int? ServiceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public UrgencyLevel Urgency { get; set; } = UrgencyLevel.Flexible;
    public DateTime? PreferredDate { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public bool IsHomeOwner { get; set; } = true;
}

public class UpdateServiceRequestDto : CreateServiceRequestDto { }
