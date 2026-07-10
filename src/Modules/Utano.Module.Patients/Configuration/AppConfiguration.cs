using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utano.Module.Core.Services;
using Utano.Module.Patients.DatabaseMappings;
using Utano.Module.Patients.Domain.Interfaces;
using Utano.Module.Patients.Infrastructure.Repositories;
using Utano.Module.Patients.Infrastructure.Services;

namespace Utano.Module.Patients.Configuration;

public static class AppConfiguration
{
    public static IServiceCollection AddPatientsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PatientsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UtanoDb")));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AppConfiguration).Assembly));

        services.AddValidatorsFromAssembly(typeof(AppConfiguration).Assembly);

        services.AddScoped<IPatientReadRepository, PatientReadRepository>();
        services.AddScoped<IPatientWriteRepository, PatientWriteRepository>();
        services.AddScoped<IMedicalAidRepository, MedicalAidRepository>();
        services.AddScoped<IPatientStatusChecker, PatientStatusChecker>();

        return services;
    }

    public static WebApplication ConfigurePatientsModule(this WebApplication application)
    {
        return application;
    }
}