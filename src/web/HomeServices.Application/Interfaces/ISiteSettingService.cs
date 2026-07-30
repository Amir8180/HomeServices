using HomeServices.Application.Dtos;

namespace HomeServices.Application.Interfaces;

/// <summary>
/// Application service contract for site settings (branding, theme, text).
/// </summary>
public interface ISiteSettingService
{
    Task<IReadOnlyList<SiteSettingDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string?>> GetAllAsDictionaryAsync(CancellationToken cancellationToken = default);
    Task<SiteSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<SiteSettingDto> UpsertAsync(UpsertSiteSettingDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
