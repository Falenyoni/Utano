using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utano.Module.ClinicalNotes.DatabaseMappings;
using Utano.Module.ClinicalNotes.Domain.Interfaces;
using Utano.Module.ClinicalNotes.Infrastructure;
using Utano.Module.ClinicalNotes.Infrastructure.Repositories;
using Utano.Module.Core.Services;

namespace Utano.Module.ClinicalNotes.Configuration;

public static class AppConfiguration
{
    public static IServiceCollection AddClinicalNotesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ClinicalNotesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UtanoDb")));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AppConfiguration).Assembly));

        services.AddValidatorsFromAssembly(typeof(AppConfiguration).Assembly);

        services.AddScoped<IVisitReadRepository, VisitReadRepository>();
        services.AddScoped<IVisitWriteRepository, VisitWriteRepository>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }

    public static WebApplication ConfigureClinicalNotesModule(this WebApplication app) => app;
}
