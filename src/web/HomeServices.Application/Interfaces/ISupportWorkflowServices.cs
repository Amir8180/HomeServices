using HomeServices.Application.Dtos;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;

namespace HomeServices.Application.Interfaces;

public interface IPaymentVerificationService
{
    Task<PagedResult<PaymentVerificationReportDto>> GetPagedAsync(PaymentVerificationFilterDto filter, CancellationToken cancellationToken = default);
    Task<PaymentVerificationReportDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PaymentVerificationReportDto> CreateAsync(CreatePaymentVerificationReportDto dto, Guid customerId, CancellationToken cancellationToken = default);
    Task<bool> VerifyAsync(int id, string? supportNote, Guid adminUserId, CancellationToken cancellationToken = default);
    Task<bool> RejectAsync(int id, string supportNote, Guid adminUserId, CancellationToken cancellationToken = default);
}

public interface IWorkCompletionService
{
    Task<PagedResult<WorkCompletionReportDto>> GetPagedAsync(CompletionReviewFilterDto filter, CancellationToken cancellationToken = default);
    Task<WorkCompletionReportDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkCompletionReportDto?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<WorkCompletionReportDto> DeclareCompletionAsync(CreateWorkCompletionDeclarationDto dto, Guid userId, AttachmentUploader uploader, CancellationToken cancellationToken = default);
    Task<bool> ApproveAsync(int id, string? supportNote, Guid adminUserId, CancellationToken cancellationToken = default);
    Task<bool> RejectAsync(int id, string supportNote, Guid adminUserId, CancellationToken cancellationToken = default);
}

public interface IExpertPayoutService
{
    Task<PagedResult<ExpertPayoutDto>> GetPagedAsync(ExpertPayoutFilterDto filter, CancellationToken cancellationToken = default);
    Task<ExpertPayoutDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ExpertIncomeSummaryDto> GetExpertIncomeSummaryAsync(Guid expertId, string period, CancellationToken cancellationToken = default);
    Task<SiteRevenueSummaryDto> GetSiteRevenueSummaryAsync(string period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Net monthly income of an expert over the last N calendar months (oldest → newest).
    /// Label is the Gregorian "yyyy-MM" month key; the UI maps it to Persian month names.
    /// </summary>
    Task<List<IncomeChartPointDto>> GetExpertMonthlyTrendAsync(Guid expertId, int months = 6, CancellationToken cancellationToken = default);
}

public interface ISupportTicketService
{
    Task<PagedResult<SupportTicketDto>> GetPagedAsync(SupportTicketFilterDto filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SupportTicketDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<SupportTicketDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SupportTicketDto> CreateAsync(CreateSupportTicketDto dto, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Adds a message to the ticket thread (from the user or the support team).</summary>
    Task<bool> ReplyAsync(int ticketId, string body, Guid senderId, bool isFromAdmin,
        List<string>? fileUrls = null, List<string?>? thumbnailUrls = null, List<MediaType>? mediaTypes = null,
        CancellationToken cancellationToken = default);

    /// <summary>Changes the ticket status (admin side). Resolved/Closed timestamps are maintained.</summary>
    Task<bool> UpdateStatusAsync(int ticketId, SupportTicketStatus status, Guid adminUserId, CancellationToken cancellationToken = default);
}
