using AutoMapper;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

/// <summary>
/// Application service for orders. An order is created when a customer accepts a
/// proposal (CreateFromProposal) and progresses through Paid → Scheduled →
/// InProgress → Completed. Completion bumps the related request and the expert's
/// jobs-completed counter.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;
    private static int _orderSequence = 100000; // in-memory seed; real sequence from DB max

    public OrderService(IUnitOfWork uow, IMapper mapper, ILogger<OrderService> logger)
    {
        _uow = uow; _mapper = mapper; _logger = logger;
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(OrderFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _uow.Repository<Order>().GetAllNoTracking()
            .Include(o => o.Request)
            .AsQueryable();

        if (filter.CustomerId.HasValue) query = query.Where(o => o.CustomerId == filter.CustomerId);
        if (filter.ExpertId.HasValue) query = query.Where(o => o.ExpertId == filter.ExpertId);
        if (filter.Status.HasValue) query = query.Where(o => o.Status == filter.Status);
        if (!string.IsNullOrWhiteSpace(filter.OrderNumber))
            query = query.Where(o => o.OrderNumber.Contains(filter.OrderNumber));
        if (filter.FromDate.HasValue) query = query.Where(o => o.CreatedAt >= filter.FromDate);
        if (filter.ToDate.HasValue) query = query.Where(o => o.CreatedAt <= filter.ToDate);

        query = query.OrderByDescending(o => o.CreatedAt);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 12 : filter.PageSize;
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<OrderDto>
        {
            Items = _mapper.Map<List<OrderDto>>(items),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<IReadOnlyList<OrderDto>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var list = await _uow.Repository<Order>().GetAllNoTracking()
            .Include(o => o.Request)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<OrderDto>>(list);
    }

    public async Task<IReadOnlyList<OrderDto>> GetByExpertAsync(Guid expertId, CancellationToken cancellationToken = default)
    {
        var list = await _uow.Repository<Order>().GetAllNoTracking()
            .Include(o => o.Request)
            .Where(o => o.ExpertId == expertId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<OrderDto>>(list);
    }

    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Order>().GetAllNoTracking()
            .Include(o => o.Request)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        return entity == null ? null : _mapper.Map<OrderDto>(entity);
    }

    public async Task<OrderDto?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Order>().GetAllNoTracking()
            .Include(o => o.Request)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
        return entity == null ? null : _mapper.Map<OrderDto>(entity);
    }

    public async Task<OrderDto> CreateFromProposalAsync(int proposalId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var proposal = await _uow.Repository<Proposal>().GetByIdAsync(proposalId, cancellationToken);
        if (proposal == null) throw new InvalidOperationException("Proposal not found.");

        var request = await _uow.Repository<ServiceRequest>().GetByIdAsync(proposal.RequestId, cancellationToken);
        if (request == null) throw new InvalidOperationException("Request not found.");
        if (request.CustomerId != customerId) throw new InvalidOperationException("Only the request owner can create an order.");

        // Avoid duplicate orders for the same accepted proposal.
        var existing = await _uow.Repository<Order>().FindAsync(o => o.ProposalId == proposalId, cancellationToken);
        if (existing.Count > 0) return (await GetByIdAsync(existing[0].Id, cancellationToken))!;

        var orderNumber = await GenerateOrderNumberAsync(cancellationToken);

        var order = new Order
        {
            RequestId = request.Id,
            ProposalId = proposal.Id,
            CustomerId = customerId,
            ExpertId = proposal.ExpertId,
            OrderNumber = orderNumber,
            Status = OrderStatus.PendingPayment,
            TotalAmount = proposal.Price,
            // Prefer the customer's requested date so the expert confirms the time
            // the customer actually wants; fall back to the proposal's availability.
            ScheduledDate = request.PreferredDate ?? proposal.AvailableStartDate,
            CreatedBy = customerId,
        };

        await _uow.Repository<Order>().AddAsync(order, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // Ensure the request reflects the booking.
        request.Status = RequestStatus.Booked;
        request.AcceptedProposalId = proposal.Id;
        _uow.Repository<ServiceRequest>().Update(request);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {Id} ({Number}) created from proposal {Proposal}.", order.Id, orderNumber, proposalId);
        return (await GetByIdAsync(order.Id, cancellationToken))!;
    }

    public async Task<OrderDto?> UpdateStatusAsync(int id, OrderStatus status, CancellationToken cancellationToken = default)
    {
        var order = await _uow.Repository<Order>().GetByIdAsync(id, cancellationToken);
        if (order == null) return null;

        order.Status = status;
        if (status == OrderStatus.Scheduled && !order.ScheduledDate.HasValue)
            order.ScheduledDate = DateTime.UtcNow;
        if (status == OrderStatus.Completed) order.CompletedDate = DateTime.UtcNow;
        _uow.Repository<Order>().Update(order);
        await _uow.SaveChangesAsync(cancellationToken);

        // Keep the request lifecycle in sync.
        var request = await _uow.Repository<ServiceRequest>().GetByIdAsync(order.RequestId, cancellationToken);
        if (request != null)
        {
            request.Status = status switch
            {
                OrderStatus.Paid => RequestStatus.Booked,
                OrderStatus.Scheduled => RequestStatus.Booked,
                OrderStatus.InProgress => RequestStatus.InProgress,
                OrderStatus.Completed => RequestStatus.Completed,
                OrderStatus.Cancelled => RequestStatus.Cancelled,
                _ => request.Status,
            };
            _uow.Repository<ServiceRequest>().Update(request);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        // On completion bump the expert's job counter.
        if (status == OrderStatus.Completed)
        {
            var expert = await _uow.Repository<ExpertProfile>()
                .GetAllNoTracking()
                .FirstOrDefaultAsync(e => e.UserId == order.ExpertId, cancellationToken);
            if (expert != null)
            {
                expert.JobsCompleted++;
                _uow.Repository<ExpertProfile>().Update(expert);
                await _uow.SaveChangesAsync(cancellationToken);
            }
        }

        _logger.LogInformation("Order {Id} status -> {Status}.", id, status);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Order>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        _uow.Repository<Order>().SoftDelete(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        // Seed from the current maximum to keep numbers monotonic across restarts.
        if (_orderSequence == 100000)
        {
            var max = await _uow.Repository<Order>().GetAllNoTracking()
                .Select(o => o.OrderNumber).ToListAsync(cancellationToken);
            foreach (var n in max)
            {
                if (n != null && n.StartsWith("HS-") && int.TryParse(n[3..], out var num) && num > _orderSequence)
                    _orderSequence = num;
            }
        }
        return $"HS-{Interlocked.Increment(ref _orderSequence)}";
    }
}
