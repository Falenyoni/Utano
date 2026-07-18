using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utano.Module.Appointments.DatabaseMappings;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Appointments.Infrastructure.Repositories;
using Utano.Module.Appointments.Infrastructure.Services;
using Utano.Module.Core.Services;

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
        services.AddScoped<IAppointmentLinker, AppointmentLinker>();
        services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();

        return services;
    }

    public static WebApplication ConfigureAppointmentsModule(this WebApplication application)
        => application;
}
