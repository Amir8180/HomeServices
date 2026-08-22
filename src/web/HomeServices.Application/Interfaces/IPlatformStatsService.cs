using HomeServices.Application.Dtos;

namespace HomeServices.Application.Interfaces;

/// <summary>
/// Aggregated platform statistics computed from real database data.
/// Cached briefly because the homepage is public and high-traffic.
/// </summary>
public interface IPlatformStatsService
{
    /// <summary>Returns up-to-date platform-wide statistics.</summary>
    Task<PlatformStatsDto> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Invalidates the cached statistics (call after impactful writes).</summary>
    Task InvalidateCacheAsync(CancellationToken cancellationToken = default);
}
