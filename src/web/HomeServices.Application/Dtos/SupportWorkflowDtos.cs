using HomeServices.Domain.Enums;

namespace HomeServices.Application.Dtos;

// ===================== Payment Verification Report =====================

public class PaymentVerificationReportDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? ExpertName { get; set; }
    public string? ServiceTitle { get; set; }
    public decimal Amount { get; set; }
    public string SenderFullName { get; set; } = string.Empty;
    public string? BankRefNumber { get; set; }
    public string? CustomerNote { get; set; }
    public PaymentVerificationStatus Status { get; set; }
    public string? SupportNote { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePaymentVerificationReportDto
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string SenderFullName { get; set; } = string.Empty;
    public string? BankRefNumber { get; set; }
    public string? CustomerNote { get; set; }
}

public class PaymentVerificationFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public PaymentVerificationStatus? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

// ===================== Work Completion Report =====================

public class WorkCompletionReportDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int RequestId { get; set; }
    public string? ServiceTitle { get; set; }
    public Guid CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid ExpertId { get; set; }
    public string? ExpertBusinessName { get; set; }

    public bool ExpertConfirmed { get; set; }
    public DateTime? ExpertConfirmedAt { get; set; }
    public string? ExpertNote { get; set; }

    public bool CustomerConfirmed { get; set; }
    public DateTime? CustomerConfirmedAt { get; set; }
    public string? CustomerNote { get; set; }

    public CompletionReviewStatus Status { get; set; }
    public string? SupportNote { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public decimal OrderAmount { get; set; }
    public List<WorkCompletionAttachmentDto> Attachments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class WorkCompletionAttachmentDto
{
    public int Id { get; set; }
    public AttachmentUploader Uploader { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public MediaType MediaType { get; set; }
    public string? Caption { get; set; }
}

public class CreateWorkCompletionDeclarationDto
{
    public int OrderId { get; set; }
    public bool Confirmed { get; set; }
    public string? Note { get; set; }
    public List<string>? FileUrls { get; set; }
    public List<string>? ThumbnailUrls { get; set; }
    public List<MediaType>? MediaTypes { get; set; }
    public List<string>? Captions { get; set; }
}

public class CompletionReviewFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public CompletionReviewStatus? Status { get; set; }
    public Guid? ExpertId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? SearchTerm { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

// ===================== Expert Payout =====================

public class ExpertPayoutDto
{
    public int Id { get; set; }
    public string PayoutNumber { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int WorkCompletionReportId { get; set; }
    public Guid ExpertId { get; set; }
    public Guid CustomerId { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal CommissionPercent { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string ServiceTitle { get; set; } = string.Empty;
    public Guid? PaidBy { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExpertPayoutFilterDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? ExpertId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

// ===================== Financial Summaries =====================

public class ExpertIncomeSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TodayIncome { get; set; }
    public decimal ThisWeekIncome { get; set; }
    public decimal ThisMonthIncome { get; set; }
    public decimal ThisYearIncome { get; set; }
    public int TotalPayouts { get; set; }

    /// <summary>Chart data: each entry is a (label, amount) pair for the requested period.</summary>
    public List<IncomeChartPointDto> ChartData { get; set; } = new();
}

public class IncomeChartPointDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class SiteRevenueSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal ThisWeekRevenue { get; set; }
    public decimal ThisMonthRevenue { get; set; }
    public decimal ThisYearRevenue { get; set; }
    public int TotalPayouts { get; set; }

    public List<IncomeChartPointDto> ChartData { get; set; } = new();
}
