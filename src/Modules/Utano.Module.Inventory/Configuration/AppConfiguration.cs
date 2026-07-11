using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utano.Module.Core.Services;
using Utano.Module.Inventory.DatabaseMappings;
using Utano.Module.Inventory.Infrastructure;

namespace Utano.Module.Inventory.Configuration;

public static class AppConfiguration
{
    public static IServiceCollection AddInventoryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UtanoDb")));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AppConfiguration).Assembly));

        services.AddValidatorsFromAssembly(typeof(AppConfiguration).Assembly);

        services.AddScoped<IInventoryService, InventoryService>();

        return services;
    }

    public static WebApplication ConfigureInventoryModule(this WebApplication app) => app;
}
