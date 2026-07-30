using HomeServices.Domain.Enums;

namespace HomeServices.Application.Dtos;

public class ReviewDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int RequestId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid ExpertId { get; set; }
    public string? ExpertBusinessName { get; set; }
    public int Rating { get; set; }
    public int? Professionalism { get; set; }
    public int? Punctuality { get; set; }
    public int? Quality { get; set; }
    public int? Responsiveness { get; set; }
    public int? Value { get; set; }
    public string? Comment { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? ServiceDate { get; set; }
    public ReviewStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReviewFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public Guid? ExpertId { get; set; }
    public Guid? CustomerId { get; set; }
    public ReviewStatus? Status { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
}

public class CreateReviewDto
{
    public int OrderId { get; set; }
    public int Rating { get; set; }
    public int? Professionalism { get; set; }
    public int? Punctuality { get; set; }
    public int? Quality { get; set; }
    public int? Responsiveness { get; set; }
    public int? Value { get; set; }
    public string? Comment { get; set; }
    public DateTime? ServiceDate { get; set; }
}
