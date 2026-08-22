using AutoMapper;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

public class PaymentVerificationService : IPaymentVerificationService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentVerificationService> _logger;

    public PaymentVerificationService(IUnitOfWork uow, IMapper mapper, ILogger<PaymentVerificationService> logger)
    {
        _uow = uow; _mapper = mapper; _logger = logger;
    }

    public async Task<PagedResult<PaymentVerificationReportDto>> GetPagedAsync(PaymentVerificationFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _uow.Repository<PaymentVerificationReport>().GetAllNoTracking()
            .Include(r => r.Order).AsQueryable();

        if (filter.Status.HasValue) query = query.Where(r => r.Status == filter.Status.Value);
        if (filter.CustomerId.HasValue) query = query.Where(r => r.CustomerId == filter.CustomerId.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = query.Where(r => r.Order.OrderNumber.Contains(filter.SearchTerm) || r.SenderFullName.Contains(filter.SearchTerm));
        if (filter.FromDate.HasValue) query = query.Where(r => r.CreatedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) query = query.Where(r => r.CreatedAt <= filter.ToDate.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 20 : filter.PageSize;
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<PaymentVerificationReportDto>
        {
            Items = _mapper.Map<List<PaymentVerificationReportDto>>(items),
            TotalCount = total, PageNumber = page, PageSize = pageSize,
        };
    }

    public async Task<PaymentVerificationReportDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<PaymentVerificationReport>().GetAllNoTracking()
            .Include(r => r.Order).Include(r => r.Payment)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return entity == null ? null : _mapper.Map<PaymentVerificationReportDto>(entity);
    }

    public async Task<PaymentVerificationReportDto> CreateAsync(CreatePaymentVerificationReportDto dto, Guid customerId, CancellationToken cancellationToken = default)
    {
        var order = await _uow.Repository<Order>().GetByIdAsync(dto.OrderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        if (order.CustomerId != customerId) throw new InvalidOperationException("Only the order owner can submit a payment report.");
        if (order.Status != OrderStatus.PendingPayment && order.Status != OrderStatus.WaitingPaymentVerification)
            throw new InvalidOperationException("This order is not awaiting payment verification.");

        var report = new PaymentVerificationReport
        {
            OrderId = dto.OrderId,
            CustomerId = customerId,
            Amount = dto.Amount,
            SenderFullName = dto.SenderFullName,
            BankRefNumber = dto.BankRefNumber,
            CustomerNote = dto.CustomerNote,
            Status = PaymentVerificationStatus.PendingReview,
            CreatedBy = customerId,
        };

        // Move order to WaitingPaymentVerification.
        order.Status = OrderStatus.WaitingPaymentVerification;
        _uow.Repository<Order>().Update(order);

        await _uow.Repository<PaymentVerificationReport>().AddAsync(report, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("PaymentVerificationReport {Id} submitted for order {Order}.", report.Id, dto.OrderId);
        return (await GetByIdAsync(report.Id, cancellationToken))!;
    }

    public async Task<bool> VerifyAsync(int id, string? supportNote, Guid adminUserId, CancellationToken cancellationToken = default)
    {
        var report = await _uow.Repository<PaymentVerificationReport>().GetByIdAsync(id, cancellationToken);
        if (report == null) return false;
        if (report.Status != PaymentVerificationStatus.PendingReview) return false;

        report.Status = PaymentVerificationStatus.Verified;
        report.SupportNote = supportNote;
        report.ReviewedBy = adminUserId;
        report.ReviewedAt = DateTime.UtcNow;
        report.UpdatedBy = adminUserId;
        _uow.Repository<PaymentVerificationReport>().Update(report);

        // Create Payment record and move order to Paid.
        var order = await _uow.Repository<Order>().GetByIdAsync(report.OrderId, cancellationToken);
        if (order != null)
        {
            var payment = new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                PaymentMethod = PaymentMethod.CardToCard,
                Status = PaymentStatus.Succeeded,
                TransactionId = report.BankRefNumber,
                PaidAt = DateTime.UtcNow,
            };
            await _uow.Repository<Payment>().AddAsync(payment, cancellationToken);

            // Link via navigation so EF fixes up the store-generated key after insert.
            report.Payment = payment;
            order.Status = OrderStatus.Paid;
            _uow.Repository<Order>().Update(order);

            _logger.LogInformation("Order {OrderId} marked Paid via card-to-card verification. PaymentId={PaymentId}", order.Id, payment.Id);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RejectAsync(int id, string supportNote, Guid adminUserId, CancellationToken cancellationToken = default)
    {
        var report = await _uow.Repository<PaymentVerificationReport>().GetByIdAsync(id, cancellationToken);
        if (report == null) return false;
        if (report.Status != PaymentVerificationStatus.PendingReview) return false;

        report.Status = PaymentVerificationStatus.Rejected;
        report.SupportNote = supportNote;
        report.ReviewedBy = adminUserId;
        report.ReviewedAt = DateTime.UtcNow;
        report.UpdatedBy = adminUserId;
        _uow.Repository<PaymentVerificationReport>().Update(report);

        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
