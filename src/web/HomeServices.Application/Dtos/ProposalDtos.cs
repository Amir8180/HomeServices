using HomeServices.Domain.Enums;

namespace HomeServices.Application.Dtos;

public class ProposalDto
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public string? RequestTitle { get; set; }
    public Guid ExpertId { get; set; }
    public string? ExpertBusinessName { get; set; }
    public string? ExpertLogoUrl { get; set; }
    public double? ExpertRating { get; set; }
    public int? ExpertReviewCount { get; set; }
    public int? ExpertJobsCompleted { get; set; }
    public decimal Price { get; set; }
    public int? EstimatedDurationHours { get; set; }
    public string? Message { get; set; }
    public DateTime? AvailableStartDate { get; set; }
    public ProposalStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProposalFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int? RequestId { get; set; }
    public Guid? ExpertId { get; set; }
    public ProposalStatus? Status { get; set; }
}

public class CreateProposalDto
{
    public int RequestId { get; set; }
    public decimal Price { get; set; }
    public int? EstimatedDurationHours { get; set; }
    public string? Message { get; set; }
    public DateTime? AvailableStartDate { get; set; }
}

public class UpdateProposalDto : CreateProposalDto { }
