using HomeServices.Domain.Common;
using HomeServices.Domain.Enums;

namespace HomeServices.Domain.Entities;

/// <summary>
/// A multi-dimensional review a customer leaves for an expert after an order is
/// completed. Mirrors Angi's grading model: an overall rating plus sub-grades for
/// Professionalism, Punctuality, Quality, Responsiveness and Value (each 1-5).
/// </summary>
public class Review : AuditableEntity
{
    public int OrderId { get; set; }
    public int RequestId { get; set; }

    /// <summary>Customer user id (Guid) from the Identity service.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Expert user id (Guid) from the Identity service.</summary>
    public Guid ExpertId { get; set; }

    /// <summary>Overall rating 1-5.</summary>
    public int Rating { get; set; }

    // Angi-style sub-grades (1-5 each)
    public int? Professionalism { get; set; }
    public int? Punctuality { get; set; }
    public int? Quality { get; set; }
    public int? Responsiveness { get; set; }
    public int? Value { get; set; }

    public string? Comment { get; set; }
    public bool IsVerified { get; set; }

    /// <summary>When the reviewed service actually took place.</summary>
    public DateTime? ServiceDate { get; set; }

    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    // Navigation
    public Order Order { get; set; } = null!;
    public ServiceRequest Request { get; set; } = null!;
}
