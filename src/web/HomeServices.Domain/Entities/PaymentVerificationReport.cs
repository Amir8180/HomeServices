using HomeServices.Domain.Common;
using HomeServices.Domain.Enums;

namespace HomeServices.Domain.Entities;

/// <summary>
/// A card-to-card payment report submitted by the customer from the order payment
/// page (sender name, amount, bank reference number and an optional note). The
/// site support team reviews the receipt (received in the support Telegram chat)
/// and verifies or rejects the record. Verification moves the order to Paid and
/// unblocks the expert to start the job.
/// </summary>
public class PaymentVerificationReport : AuditableEntity
{
    public int OrderId { get; set; }

    /// <summary>Customer user id (Guid) from the Identity service.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Transferred amount as claimed by the customer.</summary>
    public decimal Amount { get; set; }

    /// <summary>Name of the bank account holder that sent the transfer.</summary>
    public string SenderFullName { get; set; } = string.Empty;

    /// <summary>Bank tracking/reference number typed by the customer (optional).</summary>
    public string? BankRefNumber { get; set; }

    /// <summary>Free-text note from the customer (e.g. transfer time, bank name).</summary>
    public string? CustomerNote { get; set; }

    public PaymentVerificationStatus Status { get; set; } = PaymentVerificationStatus.PendingReview;

    /// <summary>Explanation written by the support agent when verifying/rejecting.</summary>
    public string? SupportNote { get; set; }

    /// <summary>Admin user id (Guid) that reviewed this report.</summary>
    public Guid? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    /// <summary>Payment record created automatically upon verification.</summary>
    public int? PaymentId { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
    public Payment? Payment { get; set; }
}
