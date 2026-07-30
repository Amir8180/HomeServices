using HomeServices.Domain.Common;
using HomeServices.Domain.Enums;

namespace HomeServices.Domain.Entities;

/// <summary>
/// A customer's request for a home service. Captures the Angi-style intake fields:
/// category/service, free-text description, location (address/city/ZIP/geo),
/// urgency/timeline, optional budget range, home-ownership flag and optional photos.
/// </summary>
public class ServiceRequest : AuditableEntity
{
    /// <summary>Customer user id (Guid) from the Identity service.</summary>
    public Guid CustomerId { get; set; }

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

    public RequestStatus Status { get; set; } = RequestStatus.Open;

    /// <summary>The proposal the customer accepted, once one is chosen.</summary>
    public int? AcceptedProposalId { get; set; }
    public DateTime? ScheduledDate { get; set; }

    // Navigation
    public Category Category { get; set; } = null!;
    public Service? Service { get; set; }
    public ICollection<RequestImage> Images { get; set; } = new List<RequestImage>();
    public ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
    public Proposal? AcceptedProposal { get; set; }
    public Order? Order { get; set; }
    public Review? Review { get; set; }
}
