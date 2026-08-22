using HomeServices.Domain.Common;
using HomeServices.Domain.Enums;

namespace HomeServices.Domain.Entities;

/// <summary>
/// Dual work-completion declaration for an order. Created as soon as EITHER the
/// expert declares the job finished or the customer confirms completion — the
/// record immediately becomes visible to the site support team (process-monitoring
/// sheet), which mediates between the two sides. Each side can attach explanations
/// and photo/video evidence. Approval by support completes the order and releases
/// the expert payout; rejection returns the order to InProgress with the support
/// note shown to both parties for re-work/re-review.
/// </summary>
public class WorkCompletionReport : AuditableEntity
{
    public int OrderId { get; set; }
    public int RequestId { get; set; }

    /// <summary>Customer user id (Guid) from the Identity service.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Expert user id (Guid) from the Identity service.</summary>
    public Guid ExpertId { get; set; }

    // -------------------- Expert side --------------------
    public bool ExpertConfirmed { get; set; }
    public DateTime? ExpertConfirmedAt { get; set; }

    /// <summary>Expert's explanation of the finished work (or reasons of incompleteness).</summary>
    public string? ExpertNote { get; set; }

    // -------------------- Customer side --------------------
    public bool CustomerConfirmed { get; set; }
    public DateTime? CustomerConfirmedAt { get; set; }

    /// <summary>Customer's explanation (satisfaction or reasons of dissatisfaction).</summary>
    public string? CustomerNote { get; set; }

    // -------------------- Support review --------------------
    public CompletionReviewStatus Status { get; set; } = CompletionReviewStatus.PendingReview;

    /// <summary>Mediation decision explanation shown to both parties on approve/reject.</summary>
    public string? SupportNote { get; set; }

    /// <summary>Admin user id (Guid) that reviewed this report.</summary>
    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
    public ICollection<WorkCompletionAttachment> Attachments { get; set; } = new List<WorkCompletionAttachment>();
}

/// <summary>
/// A photo or video evidence file attached to a work-completion declaration by
/// either side, so the support agent can judge the dispute as a mediator.
/// </summary>
public class WorkCompletionAttachment : BaseEntity
{
    public int WorkCompletionReportId { get; set; }

    public AttachmentUploader Uploader { get; set; } = AttachmentUploader.Expert;

    /// <summary>Stored file URL under /uploads.</summary>
    public string FileUrl { get; set; } = string.Empty;

    public string? ThumbnailUrl { get; set; }

    public MediaType MediaType { get; set; } = MediaType.Image;

    /// <summary>Optional caption typed by the uploader.</summary>
    public string? Caption { get; set; }

    // Navigation
    public WorkCompletionReport Report { get; set; } = null!;
}
