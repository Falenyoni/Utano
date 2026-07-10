using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utano.Module.Appointments.DatabaseMappings;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Appointments.Infrastructure.Repositories;

namespace Utano.Module.Appointments.Configuration;

public static class AppConfiguration
{
    public static IServiceCollection AddAppointmentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppointmentsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UtanoDb")));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AppConfiguration).Assembly));

        services.AddValidatorsFromAssembly(typeof(AppConfiguration).Assembly);

        services.AddScoped<IAppointmentReadRepository, AppointmentReadRepository>();
        services.AddScoped<IAppointmentWriteRepository, AppointmentWriteRepository>();

        return services;
    }

    public static WebApplication ConfigureAppointmentsModule(this WebApplication application)
        => application;
}
