using HomeServices.Domain.Enums;

namespace HomeServices.Application.Dtos;

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int RequestId { get; set; }
    public string? RequestTitle { get; set; }
    public int ProposalId { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid ExpertId { get; set; }
    public string? ExpertBusinessName { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public Guid? CustomerId { get; set; }
    public Guid? ExpertId { get; set; }
    public OrderStatus? Status { get; set; }
    public string? OrderNumber { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
