using HomeServices.Domain.Common;
using HomeServices.Domain.Enums;

namespace HomeServices.Domain.Entities;

/// <summary>
/// An expert's quote on a service request: a price, an estimated duration, a
/// short message and when the expert can start. The customer compares proposals
/// and accepts one, which turns it into an order.
/// </summary>
public class Proposal : AuditableEntity
{
    public int RequestId { get; set; }

    /// <summary>Expert user id (Guid) from the Identity service.</summary>
    public Guid ExpertId { get; set; }

    public decimal Price { get; set; }
    public int? EstimatedDurationHours { get; set; }
    public string? Message { get; set; }
    public DateTime? AvailableStartDate { get; set; }

    public ProposalStatus Status { get; set; } = ProposalStatus.Pending;

    // Navigation
    public ServiceRequest Request { get; set; } = null!;
    public Order? Order { get; set; }
}
