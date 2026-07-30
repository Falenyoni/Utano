using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utano.Module.ClinicalNotes.DatabaseMappings;
using Utano.Module.Core.Modules;
using Utano.Module.ClinicalNotes.Domain.Interfaces;
using Utano.Module.ClinicalNotes.Infrastructure;
using Utano.Module.ClinicalNotes.Infrastructure.Repositories;
using Utano.Module.Core.Persistence;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Configuration;

public static class AppConfiguration
{
    public static IServiceCollection AddClinicalNotesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<DomainEventDispatchInterceptor>();

        services.AddDbContext<ClinicalNotesDbContext>((sp, options) =>
            options.UseNpgsql(configuration.GetConnectionString("UtanoDb"))
                .AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>()));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AppConfiguration).Assembly));

        services.AddValidatorsFromAssembly(typeof(AppConfiguration).Assembly);

        services.AddScoped<IVisitReadRepository, VisitReadRepository>();
        services.AddScoped<IVisitWriteRepository, VisitWriteRepository>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IModuleDescriptor, ClinicalNotesModuleDescriptor>();

        return services;
    }

    public static WebApplication ConfigureClinicalNotesModule(this WebApplication app) => app;
}
