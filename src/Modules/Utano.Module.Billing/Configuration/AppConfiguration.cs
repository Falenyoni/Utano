using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utano.Module.Billing.DatabaseMappings;
using Utano.Module.Billing.Infrastructure;
using Utano.Module.Core.Services;

namespace Utano.Module.Billing.Configuration;

public static class AppConfiguration
{
    public static IServiceCollection AddBillingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<BillingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UtanoDb")));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AppConfiguration).Assembly));

        services.AddValidatorsFromAssembly(typeof(AppConfiguration).Assembly);

        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<IFiscalDevice, NullFiscalDevice>();

        return services;
    }

    public static WebApplication ConfigureBillingModule(this WebApplication app) => app;
}
