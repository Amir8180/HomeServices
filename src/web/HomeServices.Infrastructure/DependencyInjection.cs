using HomeServices.Application.Contracts;
using HomeServices.Application.Interfaces;
using HomeServices.Infrastructure.Caching;
using HomeServices.Infrastructure.Data;
using HomeServices.Infrastructure.Identity;
using HomeServices.Infrastructure.Persistence;
using HomeServices.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Infrastructure;

/// <summary>
/// DI registration for the Infrastructure layer. Wires up EF Core, the generic
/// repository + unit of work, the cache service, the file service and the typed
/// Identity API client. Redis is used when a connection string is supplied;
/// otherwise the in-memory cache is the default.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ----- EF Core -----
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // ----- Repository / UoW -----
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ----- Cache -----
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConn);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, CacheService>();

        // ----- File service -----
        services.AddScoped<IFileService, FileService>();

        // ----- Identity API client (typed HttpClient via IHttpClientFactory) -----
        services.AddHttpClient<IIdentityApiClient, IdentityApiClient>((sp, client) =>
        {
            var baseUri = configuration["IdentityApiSettings:BaseUrl"] ?? "https://localhost:5001";
            client.BaseAddress = new Uri(baseUri);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
