using AutoMapper;
using HomeServices.Application.Common;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

/// <summary>
/// Application service for the service catalogue shown to customers. Supports
/// paged listing with search/price/sort filters, lookup by id/slug and CRUD for
/// the admin panel. Read paths are cached; writes invalidate the service cache.
/// </summary>
public class ServiceService : IServiceService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private readonly ILogger<ServiceService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

    public ServiceService(IUnitOfWork uow, IMapper mapper, ICacheService cache, ILogger<ServiceService> logger)
    {
        _uow = uow; _mapper = mapper; _cache = cache; _logger = logger;
    }

    public async Task<PagedResult<ServiceDto>> GetPagedAsync(ServiceFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _uow.Repository<Service>().GetAllNoTracking()
            .Include(s => s.Category)
            .Include(s => s.Images)
            .AsQueryable();

        if (filter.ActiveOnly) query = query.Where(s => s.IsActive);
        if (filter.CategoryId.HasValue) query = query.Where(s => s.CategoryId == filter.CategoryId);
        if (filter.MinPrice.HasValue) query = query.Where(s => s.BasePrice >= filter.MinPrice);
        if (filter.MaxPrice.HasValue) query = query.Where(s => s.BasePrice <= filter.MaxPrice);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(s => s.Title.Contains(term) || (s.Description != null && s.Description.Contains(term)));
        }

        query = (filter.SortBy?.ToLowerInvariant()) switch
        {
            "price" => query.OrderBy(s => s.BasePrice),
            "pricedesc" => query.OrderByDescending(s => s.BasePrice),
            "name" => query.OrderBy(s => s.Title),
            _ => query.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Title),
        };

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 12 : filter.PageSize;
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<ServiceDto>
        {
            Items = _mapper.Map<List<ServiceDto>>(items),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<IReadOnlyList<ServiceDto>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Services.ByCategory(categoryId);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var list = await _uow.Repository<Service>().GetAllNoTracking()
                .Include(s => s.Images)
                .Where(s => s.CategoryId == categoryId && s.IsActive)
                .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Title)
                .ToListAsync(cancellationToken);
            return _mapper.Map<List<ServiceDto>>(list);
        }, CacheTtl, cancellationToken);
    }

    public async Task<ServiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Services.ById(id);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var entity = await _uow.Repository<Service>().GetAllNoTracking()
                .Include(s => s.Category).Include(s => s.Images)
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
            return entity == null ? null : _mapper.Map<ServiceDto>(entity);
        }, CacheTtl, cancellationToken);
    }

    public async Task<ServiceDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Services.BySlug(slug);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var entity = await _uow.Repository<Service>().GetAllNoTracking()
                .Include(s => s.Category).Include(s => s.Images)
                .FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);
            return entity == null ? null : _mapper.Map<ServiceDto>(entity);
        }, CacheTtl, cancellationToken);
    }

    public async Task<ServiceDto> CreateAsync(CreateServiceDto dto, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<Service>(dto);
        if (string.IsNullOrWhiteSpace(entity.Slug))
            entity.Slug = Slugify(entity.Title);
        await _uow.Repository<Service>().AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Services.Prefix, cancellationToken);
        _logger.LogInformation("Service {Id} ({Title}) created.", entity.Id, entity.Title);
        return _mapper.Map<ServiceDto>(entity);
    }

    public async Task<ServiceDto?> UpdateAsync(int id, UpdateServiceDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Service>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;
        _mapper.Map(dto, entity);
        _uow.Repository<Service>().Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Services.Prefix, cancellationToken);
        return _mapper.Map<ServiceDto>(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Service>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        _uow.Repository<Service>().SoftDelete(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Services.Prefix, cancellationToken);
        return true;
    }

    private static string Slugify(string text)
    {
        var sb = new System.Text.StringBuilder(text.Length);
        bool lastWasDash = true;
        foreach (var ch in text.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) { sb.Append(ch); lastWasDash = false; }
            else if (!lastWasDash) { sb.Append('-'); lastWasDash = true; }
        }
        if (sb.Length > 0 && sb[^1] == '-') sb.Length--;
        return sb.ToString();
    }
}
