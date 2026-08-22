using HomeServices.Domain.Common;
using HomeServices.Domain.Enums;

namespace HomeServices.Domain.Entities;

/// <summary>
/// A user-submitted support ticket (help-desk). The ticket carries the initial
/// request (subject/category/priority/description + attachments) and a message
/// thread between the user and the support team, mirroring mainstream ticketing
/// systems. Optional link to the related order for order-specific issues.
/// </summary>
public class SupportTicket : AuditableEntity
{
    /// <summary>Human-friendly unique ticket number (e.g. TK-100001).</summary>
    public string TicketNumber { get; set; } = string.Empty;

    /// <summary>Submitter user id (Guid) from the Identity service.</summary>
    public Guid UserId { get; set; }

    /// <summary>Related order, when the ticket is about a specific order.</summary>
    public int? OrderId { get; set; }

    public string Subject { get; set; } = string.Empty;
    public SupportTicketCategory Category { get; set; } = SupportTicketCategory.Other;
    public SupportTicketPriority Priority { get; set; } = SupportTicketPriority.Normal;

    /// <summary>Initial request body typed by the user.</summary>
    public string Description { get; set; } = string.Empty;

    public SupportTicketStatus Status { get; set; } = SupportTicketStatus.Open;

    /// <summary>Admin user id currently handling the ticket.</summary>
    public Guid? AssignedTo { get; set; }

    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>Updated on every new message — powers "latest activity" sorting.</summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Order? Order { get; set; }
    public ICollection<SupportTicketMessage> Messages { get; set; } = new List<SupportTicketMessage>();
    public ICollection<SupportTicketAttachment> Attachments { get; set; } = new List<SupportTicketAttachment>();
}

/// <summary>
/// A single message in the ticket conversation — sent either by the user or by
/// the support team (IsFromAdmin).
/// </summary>
public class SupportTicketMessage : AuditableEntity
{
    public int TicketId { get; set; }
    public Guid SenderId { get; set; }
    public bool IsFromAdmin { get; set; }
    public string Body { get; set; } = string.Empty;

    // Navigation
    public SupportTicket Ticket { get; set; } = null!;
}

/// <summary>
/// A photo/video/document attached to a ticket by the user or the support agent.
/// </summary>
public class SupportTicketAttachment : BaseEntity
{
    public int TicketId { get; set; }

    /// <summary>Optional message this attachment belongs to (null = initial request).</summary>
    public int? MessageId { get; set; }

    /// <summary>Stored file URL under /uploads.</summary>
    public string FileUrl { get; set; } = string.Empty;

    public string? ThumbnailUrl { get; set; }
    public MediaType MediaType { get; set; } = MediaType.Image;

    /// <summary>Optional caption typed by the uploader.</summary>
    public string? Caption { get; set; }

    /// <summary>User id (Guid) from the Identity service who uploaded this file.</summary>
    public Guid? UploadedBy { get; set; }

    // Navigation
    public SupportTicket Ticket { get; set; } = null!;
}
