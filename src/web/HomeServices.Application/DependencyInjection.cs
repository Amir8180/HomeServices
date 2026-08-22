using FluentValidation;
using HomeServices.Application.Services;
using HomeServices.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace HomeServices.Application;

/// <summary>
/// DI registration for the Application layer. Registers AutoMapper, all application
/// services and FluentValidation validators. Call AddApplication() from the web host.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddAutoMapper(assembly);
        services.AddValidatorsFromAssembly(assembly);

        // Application services
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IServiceService, ServiceService>();
        services.AddScoped<IServiceRequestService, ServiceRequestService>();
        services.AddScoped<IProposalService, ProposalService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IExpertProfileService, ExpertProfileService>();
        services.AddScoped<ISiteSettingService, SiteSettingService>();
        services.AddScoped<IPlatformStatsService, PlatformStatsService>();
        services.AddScoped<IPaymentVerificationService, PaymentVerificationService>();
        services.AddScoped<IWorkCompletionService, WorkCompletionService>();
        services.AddScoped<IExpertPayoutService, ExpertPayoutService>();
        services.AddScoped<ISupportTicketService, SupportTicketService>();

        return services;
    }
}
