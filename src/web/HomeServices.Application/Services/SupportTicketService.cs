using AutoMapper;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

/// <summary>
/// Help-desk ticketing service: users submit tickets (optionally tied to an order)
/// and converse with the support team through a message thread with attachments.
/// Status lifecycle: Open → InProgress → Resolved → Closed (user may close own ticket).
/// </summary>
public class SupportTicketService : ISupportTicketService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<SupportTicketService> _logger;

    public SupportTicketService(IUnitOfWork uow, IMapper mapper, ILogger<SupportTicketService> logger)
    {
        _uow = uow; _mapper = mapper; _logger = logger;
    }

    public async Task<PagedResult<SupportTicketDto>> GetPagedAsync(SupportTicketFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _uow.Repository<SupportTicket>().GetAllNoTracking()
            .Include(t => t.Order).AsQueryable();

        if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status.Value);
        if (filter.Category.HasValue) query = query.Where(t => t.Category == filter.Category.Value);
        if (filter.UserId.HasValue) query = query.Where(t => t.UserId == filter.UserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(t => t.TicketNumber.Contains(term)
                                  || t.Subject.Contains(term)
                                  || (t.Order != null && t.Order.OrderNumber.Contains(term)));
        }
        if (filter.FromDate.HasValue) query = query.Where(t => t.CreatedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) query = query.Where(t => t.CreatedAt <= filter.ToDate.Value);

        query = query.OrderByDescending(t => t.LastActivityAt);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 20 : filter.PageSize;
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<SupportTicketDto>
        {
            Items = _mapper.Map<List<SupportTicketDto>>(items),
            TotalCount = total, PageNumber = page, PageSize = pageSize,
        };
    }

    public async Task<IReadOnlyList<SupportTicketDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var list = await _uow.Repository<SupportTicket>().GetAllNoTracking()
            .Include(t => t.Order)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.LastActivityAt)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<SupportTicketDto>>(list);
    }

    public async Task<SupportTicketDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<SupportTicket>().GetAllNoTracking()
            .Include(t => t.Order)
            .Include(t => t.Messages)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity == null) return null;

        var dto = _mapper.Map<SupportTicketDto>(entity);
        dto.Messages = dto.Messages.OrderBy(m => m.CreatedAt).ToList();
        return dto;
    }

    public async Task<SupportTicketDto> CreateAsync(CreateSupportTicketDto dto, Guid userId, CancellationToken cancellationToken = default)
    {
        // A ticket may reference an order, but only one owned by the submitter.
        if (dto.OrderId.HasValue)
        {
            var order = await _uow.Repository<Order>().GetByIdAsync(dto.OrderId.Value, cancellationToken);
            if (order == null || order.CustomerId != userId)
                throw new InvalidOperationException("سفارش انتخاب‌شده متعلق به شما نیست.");
        }

        var seq = 100001;
        var existing = await _uow.Repository<SupportTicket>().GetAllNoTracking()
            .Select(t => t.TicketNumber).ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            var nums = existing.Where(n => n.StartsWith("TK-"))
                .Select(n => int.TryParse(n[3..], out var v) ? v : 0).ToList();
            if (nums.Count > 0) seq = nums.Max() + 1;
        }

        var ticket = new SupportTicket
        {
            TicketNumber = $"TK-{seq}",
            UserId = userId,
            OrderId = dto.OrderId,
            Subject = dto.Subject.Trim(),
            Category = dto.Category,
            Priority = dto.Priority,
            Description = dto.Description.Trim(),
            Status = SupportTicketStatus.Open,
            LastActivityAt = DateTime.UtcNow,
            CreatedBy = userId,
        };
        await _uow.Repository<SupportTicket>().AddAsync(ticket, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        if (dto.FileUrls != null)
        {
            for (int i = 0; i < dto.FileUrls.Count; i++)
            {
                await _uow.Repository<SupportTicketAttachment>().AddAsync(new SupportTicketAttachment
                {
                    TicketId = ticket.Id,
                    FileUrl = dto.FileUrls[i],
                    ThumbnailUrl = dto.ThumbnailUrls?.ElementAtOrDefault(i),
                    MediaType = dto.MediaTypes?.ElementAtOrDefault(i) ?? MediaType.Image,
                    Caption = dto.Captions?.ElementAtOrDefault(i),
                    UploadedBy = userId,
                }, cancellationToken);
            }
            await _uow.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("SupportTicket {Number} created by user {UserId}.", ticket.TicketNumber, userId);
        return (await GetByIdAsync(ticket.Id, cancellationToken))!;
    }

    public async Task<bool> ReplyAsync(int ticketId, string body, Guid senderId, bool isFromAdmin,
        List<string>? fileUrls = null, List<string?>? thumbnailUrls = null, List<MediaType>? mediaTypes = null,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _uow.Repository<SupportTicket>().GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null) return false;
        if (ticket.Status == SupportTicketStatus.Closed) return false;

        if (!isFromAdmin && ticket.UserId != senderId)
            return false; // only the owner may reply on the user side

        var message = new SupportTicketMessage
        {
            TicketId = ticketId,
            SenderId = senderId,
            IsFromAdmin = isFromAdmin,
            Body = body.Trim(),
            CreatedBy = senderId,
        };
        await _uow.Repository<SupportTicketMessage>().AddAsync(message, cancellationToken);

        ticket.LastActivityAt = DateTime.UtcNow;
        ticket.UpdatedBy = senderId;
        _uow.Repository<SupportTicket>().Update(ticket);

        // Admin reply → ticket under investigation; user reply on Resolved → reopened.
        if (isFromAdmin && ticket.Status == SupportTicketStatus.Open)
            ticket.Status = SupportTicketStatus.InProgress;
        else if (!isFromAdmin && ticket.Status == SupportTicketStatus.Resolved)
            ticket.Status = SupportTicketStatus.InProgress;

        await _uow.SaveChangesAsync(cancellationToken);

        if (fileUrls != null)
        {
            for (int i = 0; i < fileUrls.Count; i++)
            {
                await _uow.Repository<SupportTicketAttachment>().AddAsync(new SupportTicketAttachment
                {
                    TicketId = ticketId,
                    MessageId = message.Id,
                    FileUrl = fileUrls[i],
                    ThumbnailUrl = thumbnailUrls?.ElementAtOrDefault(i),
                    MediaType = mediaTypes?.ElementAtOrDefault(i) ?? MediaType.Image,
                    UploadedBy = senderId,
                }, cancellationToken);
            }
            await _uow.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("SupportTicket {Number} replied (admin={IsAdmin}).", ticket.TicketNumber, isFromAdmin);
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int ticketId, SupportTicketStatus status, Guid adminUserId, CancellationToken cancellationToken = default)
    {
        var ticket = await _uow.Repository<SupportTicket>().GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null) return false;

        ticket.Status = status;
        ticket.AssignedTo = status == SupportTicketStatus.Open ? null : adminUserId;
        ticket.ResolvedAt = status == SupportTicketStatus.Resolved ? DateTime.UtcNow : ticket.ResolvedAt;
        ticket.ClosedAt = status == SupportTicketStatus.Closed ? DateTime.UtcNow : null;
        ticket.LastActivityAt = DateTime.UtcNow;
        ticket.UpdatedBy = adminUserId;
        _uow.Repository<SupportTicket>().Update(ticket);
        await _uow.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SupportTicket {Number} status → {Status}.", ticket.TicketNumber, status);
        return true;
    }
}
