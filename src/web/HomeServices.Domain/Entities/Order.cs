using HomeServices.Domain.Common;
using HomeServices.Domain.Enums;

namespace HomeServices.Domain.Entities;

/// <summary>
/// An order created when a customer accepts a proposal. Tracks the agreed amount,
/// scheduling and completion, and is the anchor for payments and reviews.
/// </summary>
public class Order : AuditableEntity
{
    public int RequestId { get; set; }
    public int ProposalId { get; set; }

    /// <summary>Customer user id (Guid) from the Identity service.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Expert user id (Guid) from the Identity service.</summary>
    public Guid ExpertId { get; set; }

    /// <summary>Human-friendly unique order number (e.g. HS-100001).</summary>
    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public decimal TotalAmount { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ServiceRequest Request { get; set; } = null!;
    public Proposal Proposal { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public Review? Review { get; set; }
}
