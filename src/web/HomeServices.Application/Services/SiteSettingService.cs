using AutoMapper;
using HomeServices.Application.Common;
using HomeServices.Application.Dtos;
using HomeServices.Application.Interfaces;
using HomeServices.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HomeServices.Application.Services;

/// <summary>
/// Application service for site settings (branding, theme, hero text, contact).
/// The whole key/value set is cached as a dictionary so the layout can resolve
/// settings in a single lookup; writes invalidate the cache.
/// </summary>
public class SiteSettingService : ISiteSettingService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICacheService _cache;
    private readonly ILogger<SiteSettingService> _logger;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(2);

    public SiteSettingService(IUnitOfWork uow, IMapper mapper, ICacheService cache, ILogger<SiteSettingService> logger)
    {
        _uow = uow; _mapper = mapper; _cache = cache; _logger = logger;
    }

    public async Task<IReadOnlyList<SiteSettingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _uow.Repository<SiteSetting>().GetAllNoTracking()
            .OrderBy(s => s.Group).ThenBy(s => s.DisplayOrder)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<SiteSettingDto>>(list);
    }

    public async Task<IReadOnlyDictionary<string, string?>> GetAllAsDictionaryAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(CacheKeys.SiteSettings.Dictionary, async () =>
        {
            var list = await _uow.Repository<SiteSetting>().GetAllNoTracking().ToListAsync(cancellationToken);
            return (IReadOnlyDictionary<string, string?>)list.ToDictionary(s => s.Key, s => s.Value);
        }, CacheTtl, cancellationToken);
    }

    public async Task<SiteSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var dict = await GetAllAsDictionaryAsync(cancellationToken);
        var entity = await _uow.Repository<SiteSetting>().GetAllNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        return entity == null ? null : _mapper.Map<SiteSettingDto>(entity);
    }

    public async Task<SiteSettingDto> UpsertAsync(UpsertSiteSettingDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<SiteSetting>().GetAllNoTracking()
            .FirstOrDefaultAsync(s => s.Key == dto.Key, cancellationToken);

        if (entity == null)
        {
            entity = _mapper.Map<SiteSetting>(dto);
            await _uow.Repository<SiteSetting>().AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.Value = dto.Value;
            entity.Group = dto.Group;
            entity.Description = dto.Description;
            entity.DisplayOrder = dto.DisplayOrder;
            _uow.Repository<SiteSetting>().Update(entity);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(CacheKeys.SiteSettings.Dictionary, cancellationToken);
        return _mapper.Map<SiteSettingDto>(entity);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Repository<SiteSetting>().GetByIdAsync(id, cancellationToken);
        if (entity == null) return false;
        _uow.Repository<SiteSetting>().HardDelete(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync(CacheKeys.SiteSettings.Dictionary, cancellationToken);
        return true;
    }
}
