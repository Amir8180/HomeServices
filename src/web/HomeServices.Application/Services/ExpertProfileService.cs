using AutoMapper;
using HomeServices.Application.Common;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using HomeServices.Shared.Common;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

/// <summary>
/// Application service for expert (professional) profiles. Supports the public
/// pro-directory listing, the top-rated homepage strip, category filtering and the
/// admin/expert profile management. Reads are cached; writes invalidate the cache.
/// </summary>
public class ExpertProfileService : IExpertProfileService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private readonly ILogger<ExpertProfileService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(20);

    public ExpertProfileService(IUnitOfWork uow, IMapper mapper, ICacheService cache, ILogger<ExpertProfileService> logger)
    {
        _uow = uow; _mapper = mapper; _cache = cache; _logger = logger;
    }

    public async Task<PagedResult<ExpertProfileDto>> GetPagedAsync(ExpertProfileFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _uow.Repository<ExpertProfile>().GetAllNoTracking()
            .Include(e => e.ExpertCategories).ThenInclude(ec => ec.Category)
            .Include(e => e.PortfolioImages)
            .AsQueryable();

        if (filter.ActiveOnly) query = query.Where(e => e.IsActive);
        if (filter.IsApproved.HasValue) query = query.Where(e => e.IsApproved == filter.IsApproved);
        if (filter.CategoryId.HasValue)
            query = query.Where(e => e.ExpertCategories.Any(ec => ec.CategoryId == filter.CategoryId));
        if (!string.IsNullOrWhiteSpace(filter.City))
            query = query.Where(e => e.City != null && e.City.Contains(filter.City));
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(e => e.BusinessName.Contains(term) || (e.Bio != null && e.Bio.Contains(term)));
        }

        query = query.OrderByDescending(e => e.RatingAverage).ThenByDescending(e => e.JobsCompleted);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 12 : filter.PageSize;
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<ExpertProfileDto>
        {
            Items = _mapper.Map<List<ExpertProfileDto>>(items),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<IReadOnlyList<ExpertProfileDto>> GetTopRatedAsync(int count = 6, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Experts.TopRated(count);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var list = await _uow.Repository<ExpertProfile>().GetAllNoTracking()
                .Include(e => e.ExpertCategories).ThenInclude(ec => ec.Category)
                .Where(e => e.IsActive && e.IsApproved)
                .OrderByDescending(e => e.RatingAverage).ThenByDescending(e => e.ReviewCount)
                .Take(count)
                .ToListAsync(cancellationToken);
            return _mapper.Map<List<ExpertProfileDto>>(list);
        }, CacheTtl, cancellationToken);
    }

    public async Task<IReadOnlyList<ExpertProfileDto>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Experts.ByCategory(categoryId);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var list = await _uow.Repository<ExpertProfile>().GetAllNoTracking()
                .Include(e => e.ExpertCategories).ThenInclude(ec => ec.Category)
                .Where(e => e.IsActive && e.IsApproved && e.ExpertCategories.Any(ec => ec.CategoryId == categoryId))
                .OrderByDescending(e => e.RatingAverage)
                .ToListAsync(cancellationToken);
            return _mapper.Map<List<ExpertProfileDto>>(list);
        }, CacheTtl, cancellationToken);
    }

    public async Task<ExpertProfileDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Experts.ById(id);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var entity = await _uow.Repository<ExpertProfile>().GetAllNoTracking()
                .Include(e => e.ExpertCategories).ThenInclude(ec => ec.Category)
                .Include(e => e.PortfolioImages)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
            return entity == null ? null : _mapper.Map<ExpertProfileDto>(entity);
        }, CacheTtl, cancellationToken);
    }

    public async Task<ExpertProfileDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.Experts.ByUserId(userId);
        return await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            var entity = await _uow.Repository<ExpertProfile>().GetAllNoTracking()
                .Include(e => e.ExpertCategories).ThenInclude(ec => ec.Category)
                .Include(e => e.PortfolioImages)
                .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
            return entity == null ? null : _mapper.Map<ExpertProfileDto>(entity);
        }, CacheTtl, cancellationToken);
    }

    public async Task<ExpertProfileDto> CreateAsync(CreateExpertProfileDto dto, CancellationToken cancellationToken = default)
    {
        // Ensure one profile per user.
        var exists = await _uow.Repository<ExpertProfile>().AnyAsync(e => e.UserId == dto.UserId, cancellationToken);
        if (exists) throw new InvalidOperationException("This user already has an expert profile.");

        var entity = _mapper.Map<ExpertProfile>(dto);

        foreach (var categoryId in dto.CategoryIds.Distinct())
            entity.ExpertCategories.Add(new ExpertCategory { CategoryId = categoryId });

        await _uow.Repository<ExpertProfile>().AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Experts.Prefix, cancellationToken);
        _logger.LogInformation("ExpertProfile {Id} created for user {User}.", entity.Id, dto.UserId);
        return _mapper.Map<ExpertProfileDto>(entity);
    }

    public async Task<ExpertProfileDto?> UpdateAsync(int id, UpdateExpertProfileDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<ExpertProfile>().GetAllNoTracking()
            .Include(e => e.ExpertCategories)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity == null) return null;

        entity.BusinessName = dto.BusinessName;
        entity.Bio = dto.Bio;
        entity.LogoUrl = dto.LogoUrl;
        entity.CoverImageUrl = dto.CoverImageUrl;
        entity.ServiceArea = dto.ServiceArea;
        entity.City = dto.City;
        entity.BusinessHours = dto.BusinessHours;
        entity.ResponseTimeMinutes = dto.ResponseTimeMinutes;
        entity.IsActive = dto.IsActive;

        // Sync category membership.
        var desired = dto.CategoryIds.Distinct().ToList();
        var toRemove = entity.ExpertCategories.Where(ec => !desired.Contains(ec.CategoryId)).ToList();
        foreach (var ec in toRemove) entity.ExpertCategories.Remove(ec);
        var existing = entity.ExpertCategories.Select(ec => ec.CategoryId).ToList();
        foreach (var categoryId in desired.Except(existing))
            entity.ExpertCategories.Add(new ExpertCategory { CategoryId = categoryId });

        _uow.Repository<ExpertProfile>().Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Experts.Prefix, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<ExpertProfile>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        entity.IsApproved = !entity.IsApproved;
        _uow.Repository<ExpertProfile>().Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Experts.Prefix, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<ExpertProfile>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        _uow.Repository<ExpertProfile>().SoftDelete(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Experts.Prefix, cancellationToken);
        return true;
    }

    public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => _uow.Repository<ExpertProfile>().AnyAsync(e => e.UserId == userId, cancellationToken);

    public async Task<bool> AddPortfolioImageAsync(Guid userId, string imageUrl, string? thumbnailUrl, string? title, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<ExpertProfile>().GetAllNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
        if (entity == null) return false;

        var image = new ExpertPortfolioImage
        {
            ExpertProfileId = entity.Id,
            ImageUrl = imageUrl,
            ThumbnailUrl = thumbnailUrl,
            Title = title,
            DisplayOrder = 0,
        };

        await _uow.Repository<ExpertPortfolioImage>().AddAsync(image, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Experts.Prefix, cancellationToken);
        _logger.LogInformation("Portfolio image {Id} added for expert {User}.", image.Id, userId);
        return true;
    }

    public async Task<bool> DeletePortfolioImageAsync(int portfolioImageId, Guid userId, CancellationToken cancellationToken = default)
    {
        var image = await _uow.Repository<ExpertPortfolioImage>().GetByIdAsync(portfolioImageId, cancellationToken);
        if (image == null) return false;

        var ownedByUser = await _uow.Repository<ExpertProfile>()
            .AnyAsync(e => e.Id == image.ExpertProfileId && e.UserId == userId, cancellationToken);
        if (!ownedByUser) return false;

        _uow.Repository<ExpertPortfolioImage>().SoftDelete(image);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveByPrefixAsync(CacheKeys.Experts.Prefix, cancellationToken);
        return true;
    }
}
