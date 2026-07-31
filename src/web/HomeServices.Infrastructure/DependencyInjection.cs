using System;
using System.Net.Http;
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
using Microsoft.Extensions.Logging;

namespace HomeServices.Infrastructure;

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
        var baseUri = configuration["IdentityApiSettings:BaseUrl"] ?? "https://localhost:7047";
        var allowInsecure = configuration.GetValue<bool>("IdentityApiSettings:AllowInsecureCertificates", false);

        services.AddHttpClient<IIdentityApiClient, IdentityApiClient>((sp, client) =>
        {
            client.BaseAddress = new Uri(baseUri);
            client.Timeout = TimeSpan.FromSeconds(30);

            // Log effective BaseAddress and flag so you can verify runtime config
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var log = loggerFactory.CreateLogger("IdentityHttpClient");
            log.LogInformation("IdentityApi HttpClient configured. BaseAddress={BaseAddress} AllowInsecure={AllowInsecure}",
                client.BaseAddress, allowInsecure);
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            // Bypass system proxy for localhost microservice calls (prevents 503 on Windows).
            var handler = new HttpClientHandler
            {
                UseProxy = false,
            };
            if (allowInsecure)
            {
                // ONLY for local development debugging when certs are not trusted.
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            return handler;
        });

        return services;
    }
}