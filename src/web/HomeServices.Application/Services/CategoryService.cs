using AutoMapper;
using HomeServices.Application.Common;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using HomeServices.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

/// <summary>
/// Application service for service categories. Wraps the generic repository, maps
/// entities to DTOs and applies a short-lived cache for the read-heavy catalogue
/// pages (homepage tile grid, navigation). Writes invalidate the category cache.
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private readonly ILogger<CategoryService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    public CategoryService(IUnitOfWork uow, IMapper mapper, ICacheService cache, ILogger<CategoryService> logger)
    {
        _uow = uow; _mapper = mapper; _cache = cache; _logger = logger;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Categories.All(activeOnly);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var query = _uow.Repository<Category>().GetAllNoTracking();
            if (activeOnly) query = query.Where(c => c.IsActive);
            var list = await query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(cancellationToken);
            return _mapper.Map<List<CategoryDto>>(list);
        }, CacheTtl, cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetByGroupAsync(CategoryGroup group, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Categories.ByGroup((int)group);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var list = await _uow.Repository<Category>().GetAllNoTracking()
                .Where(c => c.Group == group && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync(cancellationToken);
            return _mapper.Map<List<CategoryDto>>(list);
        }, CacheTtl, cancellationToken);
    }

    public async Task<CategoryDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Categories.ById(id);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var entity = await _uow.Repository<Category>().GetByIdAsync(id, cancellationToken);
            return entity == null ? null : _mapper.Map<CategoryDto>(entity);
        }, CacheTtl, cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryDto>> GetSubCategoriesAsync(int parentId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Categories.SubCategories(parentId);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var list = await _uow.Repository<Category>().GetAllNoTracking()
                .Where(c => c.ParentCategoryId == parentId && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync(cancellationToken);
            return _mapper.Map<List<CategoryDto>>(list);
        }, CacheTtl, cancellationToken);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<Category>(dto);
        await _uow.Repository<Category>().AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Categories.Prefix, cancellationToken);
        _logger.LogInformation("Category {Id} ({Name}) created.", entity.Id, entity.Name);
        return _mapper.Map<CategoryDto>(entity);
    }

    public async Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Category>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return null;

        _mapper.Map(dto, entity);
        _uow.Repository<Category>().Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Categories.Prefix, cancellationToken);
        return _mapper.Map<CategoryDto>(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<Category>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        _uow.Repository<Category>().SoftDelete(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Categories.Prefix, cancellationToken);
        return true;
    }
}
