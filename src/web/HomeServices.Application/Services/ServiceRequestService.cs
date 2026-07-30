using AutoMapper;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using HomeServices.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

/// <summary>
/// Application service for customer service requests. Handles the intake, listing,
/// filtering and status transitions that drive the request lifecycle. Creating a
/// request also keeps the request → proposal → order flow consistent.
/// </summary>
public class ServiceRequestService : IServiceRequestService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<ServiceRequestService> _logger;

    public ServiceRequestService(IUnitOfWork uow, IMapper mapper, ILogger<ServiceRequestService> logger)
    {
        _uow = uow; _mapper = mapper; _logger = logger;
    }

    public async Task<PagedResult<ServiceRequestDto>> GetPagedAsync(ServiceRequestFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _uow.Repository<ServiceRequest>().GetAllNoTracking()
            .Include(r => r.Category).Include(r => r.Service).Include(r => r.Images)
            .AsQueryable();

        if (filter.CategoryId.HasValue) query = query.Where(r => r.CategoryId == filter.CategoryId);
        if (filter.CustomerId.HasValue) query = query.Where(r => r.CustomerId == filter.CustomerId);
        if (filter.Status.HasValue) query = query.Where(r => r.Status == filter.Status);
        if (filter.Urgency.HasValue) query = query.Where(r => r.Urgency == filter.Urgency);
        if (!string.IsNullOrWhiteSpace(filter.City))
            query = query.Where(r => r.City != null && r.City.Contains(filter.City));
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(r => r.Title.Contains(term) || r.Description.Contains(term));
        }

        query = query.OrderByDescending(r => r.CreatedAt);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 12 : filter.PageSize;
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<ServiceRequestDto>
        {
            Items = _mapper.Map<List<ServiceRequestDto>>(items),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var list = await _uow.Repository<ServiceRequest>().GetAllNoTracking()
            .Include(r => r.Category).Include(r => r.Service).Include(r => r.Images)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<ServiceRequestDto>>(list);
    }

    public async Task<IReadOnlyList<ServiceRequestDto>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var list = await _uow.Repository<ServiceRequest>().GetAllNoTracking()
            .Include(r => r.Category).Include(r => r.Images)
            .Where(r => r.CategoryId == categoryId && r.Status == RequestStatus.Open)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<ServiceRequestDto>>(list);
    }

    public async Task<ServiceRequestDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<ServiceRequest>().GetAllNoTracking()
            .Include(r => r.Category).Include(r => r.Service).Include(r => r.Images)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return entity == null ? null : _mapper.Map<ServiceRequestDto>(entity);
    }

    public async Task<ServiceRequestDto> CreateAsync(CreateServiceRequestDto dto, Guid customerId, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<ServiceRequest>(dto);
        entity.CustomerId = customerId;
        entity.Status = RequestStatus.Open;
        entity.CreatedBy = customerId;

        await _uow.Repository<ServiceRequest>().AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ServiceRequest {Id} created by customer {Customer}.", entity.Id, customerId);

        // Reload with navigation for the returned DTO.
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ServiceRequestDto?> UpdateAsync(int id, UpdateServiceRequestDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<ServiceRequest>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;
        _mapper.Map(dto, entity);
        _uow.Repository<ServiceRequest>().Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> UpdateStatusAsync(int id, RequestStatus status, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<ServiceRequest>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        entity.Status = status;
        _uow.Repository<ServiceRequest>().Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("ServiceRequest {Id} status -> {Status}.", id, status);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<ServiceRequest>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        _uow.Repository<ServiceRequest>().SoftDelete(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AddImageAsync(int requestId, string imageUrl, string? thumbnailUrl, CancellationToken cancellationToken = default)
    {
        var exists = await _uow.Repository<ServiceRequest>().AnyAsync(r => r.Id == requestId, cancellationToken);
        if (!exists) return false;

        var image = new RequestImage
        {
            RequestId = requestId,
            ImageUrl = imageUrl,
            ThumbnailUrl = thumbnailUrl,
            DisplayOrder = 0,
        };

        await _uow.Repository<RequestImage>().AddAsync(image, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
