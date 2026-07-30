using AutoMapper;
using HomeServices.Application.Contracts;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

/// <summary>
/// Application service for expert proposals (quotes). Experts create proposals on
/// open requests; customers compare and accept one. Accepting a proposal rejects
/// the others on the same request, marks the request as Booked, and is the trigger
/// the OrderService listens to (via CreateFromProposal) for order creation.
/// </summary>
public class ProposalService : IProposalService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<ProposalService> _logger;

    public ProposalService(IUnitOfWork uow, IMapper mapper, ILogger<ProposalService> logger)
    {
        _uow = uow; _mapper = mapper; _logger = logger;
    }

    public async Task<PagedResult<ProposalDto>> GetPagedAsync(ProposalFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _uow.Repository<Proposal>().GetAllNoTracking()
            .Include(p => p.Request)
            .AsQueryable();

        if (filter.RequestId.HasValue) query = query.Where(p => p.RequestId == filter.RequestId);
        if (filter.ExpertId.HasValue) query = query.Where(p => p.ExpertId == filter.ExpertId);
        if (filter.Status.HasValue) query = query.Where(p => p.Status == filter.Status);

        query = query.OrderByDescending(p => p.CreatedAt);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 12 : filter.PageSize;
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<ProposalDto>
        {
            Items = _mapper.Map<List<ProposalDto>>(items),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<IReadOnlyList<ProposalDto>> GetByRequestAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var list = await _uow.Repository<Proposal>().GetAllNoTracking()
            .Include(p => p.Request)
            .Where(p => p.RequestId == requestId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<ProposalDto>>(list);
    }

    public async Task<IReadOnlyList<ProposalDto>> GetByExpertAsync(Guid expertId, CancellationToken cancellationToken = default)
    {
        var list = await _uow.Repository<Proposal>().GetAllNoTracking()
            .Include(p => p.Request)
            .Where(p => p.ExpertId == expertId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<ProposalDto>>(list);
    }

    public async Task<ProposalDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Proposal>().GetAllNoTracking()
            .Include(p => p.Request)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return entity == null ? null : _mapper.Map<ProposalDto>(entity);
    }

    public async Task<ProposalDto> CreateAsync(CreateProposalDto dto, Guid expertId, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<Proposal>(dto);
        entity.ExpertId = expertId;
        entity.Status = ProposalStatus.Pending;
        entity.CreatedBy = expertId;

        await _uow.Repository<Proposal>().AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // Bump the request status to Quoted when at least one proposal exists.
        var request = await _uow.Repository<ServiceRequest>().GetByIdAsync(entity.RequestId, cancellationToken);
        if (request != null && request.Status == RequestStatus.Open)
        {
            request.Status = RequestStatus.Quoted;
            _uow.Repository<ServiceRequest>().Update(request);
            _ = await _uow.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Proposal {Id} created by expert {Expert} on request {Request}.", entity.Id, expertId, entity.RequestId);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ProposalDto?> UpdateAsync(int id, UpdateProposalDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Proposal>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;
        _mapper.Map(dto, entity);
        _uow.Repository<Proposal>().Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(int id, ProposalStatus status, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Proposal>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        entity.Status = status;
        _uow.Repository<Proposal>().Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Proposal>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        _uow.Repository<Proposal>().SoftDelete(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Marks the proposal Accepted, rejects the sibling proposals on the same request,
    /// sets the request's AcceptedProposalId and Booked status. Called atomically with
    /// order creation from the controller layer.
    /// </summary>
    public async Task<bool> AcceptAsync(int proposalId, Guid customerUserId, CancellationToken cancellationToken = default)
    {
        var proposal = await _uow.Repository<Proposal>().GetByIdAsync(proposalId, cancellationToken);
        if (proposal == null) return false;

        var request = await _uow.Repository<ServiceRequest>().GetByIdAsync(proposal.RequestId, cancellationToken);
        if (request == null || request.CustomerId != customerUserId) return false;
        if (request.AcceptedProposalId.HasValue) return false; // already accepted

        await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            // Reject siblings.
            var siblings = await _uow.Repository<Proposal>().FindAsync(
                p => p.RequestId == request.Id && p.Id != proposal.Id && p.Status == ProposalStatus.Pending, cancellationToken);
            foreach (var s in siblings)
            {
                s.Status = ProposalStatus.Rejected;
                _uow.Repository<Proposal>().Update(s);
            }

            proposal.Status = ProposalStatus.Accepted;
            _uow.Repository<Proposal>().Update(proposal);

            request.AcceptedProposalId = proposal.Id;
            request.Status = RequestStatus.Booked;
            _uow.Repository<ServiceRequest>().Update(request);

            await _uow.SaveChangesAsync(cancellationToken);
            await _uow.CommitTransactionAsync(cancellationToken);
            _logger.LogInformation("Proposal {Id} accepted on request {Request}.", proposalId, request.Id);
            return true;
        }
        catch
        {
            await _uow.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
