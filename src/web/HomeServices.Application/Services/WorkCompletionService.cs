using AutoMapper;
using HomeServices.Application.Common;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

public class WorkCompletionService : IWorkCompletionService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ISiteSettingService _siteSettings;
    private readonly ILogger<WorkCompletionService> _logger;

    public WorkCompletionService(IUnitOfWork uow, IMapper mapper, ISiteSettingService siteSettings, ILogger<WorkCompletionService> logger)
    {
        _uow = uow; _mapper = mapper; _siteSettings = siteSettings; _logger = logger;
    }

    public async Task<PagedResult<WorkCompletionReportDto>> GetPagedAsync(CompletionReviewFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _uow.Repository<WorkCompletionReport>().GetAllNoTracking()
            .Include(r => r.Order).Include(r => r.Attachments).AsQueryable();

        if (filter.Status.HasValue) query = query.Where(r => r.Status == filter.Status.Value);
        if (filter.ExpertId.HasValue) query = query.Where(r => r.ExpertId == filter.ExpertId.Value);
        if (filter.CustomerId.HasValue) query = query.Where(r => r.CustomerId == filter.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(r => r.Order.OrderNumber.Contains(term));
        }
        if (filter.FromDate.HasValue) query = query.Where(r => r.CreatedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) query = query.Where(r => r.CreatedAt <= filter.ToDate.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 20 : filter.PageSize;
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<WorkCompletionReportDto>>(items);
        // Enrich with service title from the linked request.
        foreach (var dto in dtos)
        {
            var request = await _uow.Repository<ServiceRequest>().GetAllNoTracking()
                .FirstOrDefaultAsync(r => r.Id == dto.RequestId, cancellationToken);
            dto.ServiceTitle = request?.Title ?? dto.OrderNumber;
        }

        return new PagedResult<WorkCompletionReportDto>
        {
            Items = dtos, TotalCount = total, PageNumber = page, PageSize = pageSize,
        };
    }

    public async Task<WorkCompletionReportDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<WorkCompletionReport>().GetAllNoTracking()
            .Include(r => r.Order).Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity == null) return null;
        var dto = _mapper.Map<WorkCompletionReportDto>(entity);
        var request = await _uow.Repository<ServiceRequest>().GetAllNoTracking()
            .FirstOrDefaultAsync(r => r.Id == entity.RequestId, cancellationToken);
        dto.ServiceTitle = request?.Title ?? entity.Order.OrderNumber;
        return dto;
    }

    public async Task<WorkCompletionReportDto?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<WorkCompletionReport>().GetAllNoTracking()
            .Include(r => r.Order).Include(r => r.Attachments)
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);
        if (entity == null) return null;
        var dto = _mapper.Map<WorkCompletionReportDto>(entity);
        var request = await _uow.Repository<ServiceRequest>().GetAllNoTracking()
            .FirstOrDefaultAsync(r => r.Id == entity.RequestId, cancellationToken);
        dto.ServiceTitle = request?.Title ?? entity.Order.OrderNumber;
        return dto;
    }

    public async Task<WorkCompletionReportDto> DeclareCompletionAsync(CreateWorkCompletionDeclarationDto dto, Guid userId, AttachmentUploader uploader, CancellationToken cancellationToken = default)
    {
        var order = await _uow.Repository<Order>().GetAllNoTracking()
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        bool isExpert = order.ExpertId == userId;
        bool isCustomer = order.CustomerId == userId;
        if (!isExpert && !isCustomer) throw new InvalidOperationException("Only the expert or customer can declare completion.");
        // CompletionReview is allowed too: the FIRST declaration moves the order there,
        // and the OTHER side must still be able to add their confirmation (dual flow).
        if (order.Status != OrderStatus.InProgress && order.Status != OrderStatus.Scheduled
            && order.Status != OrderStatus.Paid && order.Status != OrderStatus.CompletionReview)
            throw new InvalidOperationException("Order status does not allow completion declaration.");

        // Find or create the completion report.
        var report = await _uow.Repository<WorkCompletionReport>().GetAllNoTracking()
            .FirstOrDefaultAsync(r => r.OrderId == dto.OrderId, cancellationToken);

        if (report == null)
        {
            report = new WorkCompletionReport
            {
                OrderId = dto.OrderId,
                RequestId = order.RequestId,
                CustomerId = order.CustomerId,
                ExpertId = order.ExpertId,
                CreatedBy = userId,
            };
            await _uow.Repository<WorkCompletionReport>().AddAsync(report, cancellationToken);
        }
        else
        {
            // Reload as tracked for updates.
            report = await _uow.Repository<WorkCompletionReport>().GetByIdAsync(report.Id, cancellationToken)!;
            report.UpdatedBy = userId;
        }

        // Set the appropriate side's confirmation.
        if (isExpert)
        {
            report.ExpertConfirmed = dto.Confirmed;
            report.ExpertConfirmedAt = dto.Confirmed ? DateTime.UtcNow : null;
            report.ExpertNote = dto.Note;
        }
        else
        {
            report.CustomerConfirmed = dto.Confirmed;
            report.CustomerConfirmedAt = dto.Confirmed ? DateTime.UtcNow : null;
            report.CustomerNote = dto.Note;
        }

        // Move order to CompletionReview immediately so support sees it.
        order = await _uow.Repository<Order>().GetByIdAsync(dto.OrderId, cancellationToken)!;
        order.Status = OrderStatus.CompletionReview;
        _uow.Repository<Order>().Update(order);

        await _uow.SaveChangesAsync(cancellationToken);

        // Save attachments after we have the report Id.
        if (dto.FileUrls != null)
        {
            for (int i = 0; i < dto.FileUrls.Count; i++)
            {
                var attachment = new WorkCompletionAttachment
                {
                    WorkCompletionReportId = report.Id,
                    Uploader = uploader,
                    FileUrl = dto.FileUrls[i],
                    ThumbnailUrl = dto.ThumbnailUrls?[i],
                    MediaType = dto.MediaTypes?.ElementAtOrDefault(i) ?? MediaType.Image,
                    Caption = dto.Captions?.ElementAtOrDefault(i),
                };
                await _uow.Repository<WorkCompletionAttachment>().AddAsync(attachment, cancellationToken);
            }
            await _uow.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("WorkCompletionReport {Id} updated: order {Order}, uploader={Uploader}, confirmed={Confirmed}",
            report.Id, dto.OrderId, uploader, dto.Confirmed);

        return (await GetByIdAsync(report.Id, cancellationToken))!;
    }

    public async Task<bool> ApproveAsync(int id, string? supportNote, Guid adminUserId, CancellationToken cancellationToken = default)
    {
        var report = await _uow.Repository<WorkCompletionReport>().GetByIdAsync(id, cancellationToken);
        if (report == null) return false;
        if (report.Status != CompletionReviewStatus.PendingReview) return false;

        report.Status = CompletionReviewStatus.Approved;
        report.SupportNote = supportNote;
        report.ReviewedBy = adminUserId;
        report.ReviewedAt = DateTime.UtcNow;
        report.UpdatedBy = adminUserId;
        _uow.Repository<WorkCompletionReport>().Update(report);

        // Complete the order.
        var order = await _uow.Repository<Order>().GetByIdAsync(report.OrderId, cancellationToken);
        if (order != null)
        {
            order.Status = OrderStatus.Completed;
            order.CompletedDate = DateTime.UtcNow;
            _uow.Repository<Order>().Update(order);

            // Keep request lifecycle in sync.
            var request = await _uow.Repository<ServiceRequest>().GetByIdAsync(order.RequestId, cancellationToken);
            if (request != null)
            {
                request.Status = RequestStatus.Completed;
                _uow.Repository<ServiceRequest>().Update(request);
            }

            // Bump expert job counter.
            var expert = await _uow.Repository<ExpertProfile>().GetAllNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == order.ExpertId, cancellationToken);
            if (expert != null)
            {
                expert.JobsCompleted++;
                _uow.Repository<ExpertProfile>().Update(expert);
            }

            // Create ExpertPayout (90% to expert, 10% commission to site).
            await CreatePayoutAsync(order, report, adminUserId, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("WorkCompletionReport {Id} approved → Order {OrderId} completed + payout created.", id, report.OrderId);
        return true;
    }

    public async Task<bool> RejectAsync(int id, string supportNote, Guid adminUserId, CancellationToken cancellationToken = default)
    {
        var report = await _uow.Repository<WorkCompletionReport>().GetByIdAsync(id, cancellationToken);
        if (report == null) return false;
        if (report.Status != CompletionReviewStatus.PendingReview) return false;

        report.Status = CompletionReviewStatus.Rejected;
        report.SupportNote = supportNote;
        report.ReviewedBy = adminUserId;
        report.ReviewedAt = DateTime.UtcNow;
        report.UpdatedBy = adminUserId;
        _uow.Repository<WorkCompletionReport>().Update(report);

        // Return order to InProgress for re-work.
        var order = await _uow.Repository<Order>().GetByIdAsync(report.OrderId, cancellationToken);
        if (order != null)
        {
            order.Status = OrderStatus.InProgress;
            _uow.Repository<Order>().Update(order);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("WorkCompletionReport {Id} rejected → Order {OrderId} returned to InProgress.", id, report.OrderId);
        return true;
    }

    private async Task CreatePayoutAsync(Order order, WorkCompletionReport report, Guid adminUserId, CancellationToken cancellationToken)
    {
        // Prevent duplicate payout.
        var existing = await _uow.Repository<ExpertPayout>().AnyAsync(p => p.OrderId == order.Id, cancellationToken);
        if (existing) return;

        var seq = 100000;
        var maxPayout = await _uow.Repository<ExpertPayout>().GetAllNoTracking()
            .Select(p => p.PayoutNumber).ToListAsync(cancellationToken);
        if (maxPayout.Count > 0)
        {
            var nums = maxPayout.Where(n => n.StartsWith("PO-")).Select(n => int.TryParse(n[3..], out var v) ? v : 0).ToList();
            if (nums.Count > 0) seq = nums.Max() + 1;
        }

        var gross = order.TotalAmount;
        var settings = await _siteSettings.GetAllAsDictionaryAsync(cancellationToken);
        var commissionPercent = CardToCardPaymentInfo.ResolveCommissionPercent(settings);
        var commissionAmount = Math.Round(gross * commissionPercent / 100, 2);
        var netAmount = gross - commissionAmount;

        var request = await _uow.Repository<ServiceRequest>().GetAllNoTracking()
            .FirstOrDefaultAsync(r => r.Id == order.RequestId, cancellationToken);

        var payout = new ExpertPayout
        {
            PayoutNumber = $"PO-{seq}",
            OrderId = order.Id,
            WorkCompletionReportId = report.Id,
            ExpertId = order.ExpertId,
            CustomerId = order.CustomerId,
            GrossAmount = gross,
            CommissionPercent = commissionPercent,
            CommissionAmount = commissionAmount,
            NetAmount = netAmount,
            OrderNumber = order.OrderNumber,
            ServiceTitle = request?.Title ?? order.OrderNumber,
            PaidBy = adminUserId,
            PaidAt = DateTime.UtcNow,
            CreatedBy = adminUserId,
        };

        await _uow.Repository<ExpertPayout>().AddAsync(payout, cancellationToken);
        _logger.LogInformation("ExpertPayout {Number} created: gross={Gross}, commission={Comm}, net={Net}",
            payout.PayoutNumber, gross, commissionAmount, netAmount);
    }
}
