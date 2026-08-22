using HomeServices.Domain.Common;

namespace HomeServices.Domain.Entities;

/// <summary>
/// The wage paid to an expert for a completed order. Created automatically when
/// the support team approves the work-completion report. The order amount is
/// split between the site commission (10% by default, configurable via the
/// "Payment.CommissionRatePercent" site setting) and the expert's net wage.
/// Powers the expert's financial-management pages (income charts, payouts list
/// and the minimal invoice) and the site-revenue dashboard.
/// </summary>
public class ExpertPayout : AuditableEntity
{
    /// <summary>Human-friendly unique payout/invoice number (e.g. PO-100001).</summary>
    public string PayoutNumber { get; set; } = string.Empty;

    public int OrderId { get; set; }
    public int WorkCompletionReportId { get; set; }

    /// <summary>Expert user id (Guid) from the Identity service.</summary>
    public Guid ExpertId { get; set; }

    /// <summary>Customer user id (Guid) from the Identity service.</summary>
    public Guid CustomerId { get; set; }

    // -------------------- Amounts (snapshot at payment time) --------------------
    /// <summary>Full amount the customer paid for the order.</summary>
    public decimal GrossAmount { get; set; }

    /// <summary>Commission percent kept by the site (e.g. 10).</summary>
    public decimal CommissionPercent { get; set; }

    /// <summary>Commission amount = GrossAmount × CommissionPercent / 100 (site revenue).</summary>
    public decimal CommissionAmount { get; set; }

    /// <summary>Net wage actually paid to the expert (GrossAmount − CommissionAmount).</summary>
    public decimal NetAmount { get; set; }

    // -------------------- Display snapshots --------------------
    /// <summary>Order number snapshot (e.g. HS-100001) for the payouts list/invoice.</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Service/title snapshot of what was delivered.</summary>
    public string ServiceTitle { get; set; } = string.Empty;

    /// <summary>Admin user id (Guid) that released this payout (the reviewing supporter).</summary>
    public Guid? PaidBy { get; set; }

    public DateTime? PaidAt { get; set; }

    // Navigation
    public Order Order { get; set; } = null!;
}
