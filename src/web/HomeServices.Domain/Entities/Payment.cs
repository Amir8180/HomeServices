using HomeServices.Domain.Common;
using HomeServices.Domain.Enums;

namespace HomeServices.Domain.Entities;

/// <summary>
/// A payment attempt/transaction for an order.
/// </summary>
public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Online;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Internal transaction id from our payment service.</summary>
    public string? TransactionId { get; set; }

    /// <summary>Reference returned by the payment gateway.</summary>
    public string? GatewayReference { get; set; }
    public DateTime? PaidAt { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}
