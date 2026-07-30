using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utano.Module.Appointments.DatabaseMappings;
using Utano.Module.Core.Modules;
using Utano.Module.Appointments.Domain.Interfaces;
using Utano.Module.Appointments.Infrastructure.Jobs;
using Utano.Module.Appointments.Infrastructure.Repositories;
using Utano.Module.Appointments.Infrastructure.Services;
using Utano.Module.Core.Persistence;
using Utano.Module.Core.Services;

namespace Utano.Module.Appointments.Configuration;

public static class AppConfiguration
{
    public static IServiceCollection AddAppointmentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<DomainEventDispatchInterceptor>();

        services.AddDbContext<AppointmentsDbContext>((sp, options) =>
            options.UseNpgsql(configuration.GetConnectionString("UtanoDb"))
                .AddInterceptors(sp.GetRequiredService<DomainEventDispatchInterceptor>()));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AppConfiguration).Assembly));

        services.AddValidatorsFromAssembly(typeof(AppConfiguration).Assembly);

        services.AddScoped<IAppointmentReadRepository, AppointmentReadRepository>();
        services.AddScoped<IAppointmentWriteRepository, AppointmentWriteRepository>();
        services.AddScoped<IAppointmentLinker, AppointmentLinker>();
        services.AddScoped<IDoctorScheduleRepository, DoctorScheduleRepository>();
        services.AddSingleton<IModuleDescriptor, AppointmentsModuleDescriptor>();

        services.Configure<AppointmentReminderSettings>(configuration.GetSection("AppointmentReminders"));
        services.AddScoped<AppointmentReminderScanJob>();

        return services;
    }

    public static WebApplication ConfigureAppointmentsModule(this WebApplication application)
    {
        var recurringJobs = application.Services.GetRequiredService<IRecurringJobManager>();
        recurringJobs.AddOrUpdate<AppointmentReminderScanJob>(
            "appointment-reminder-scan",
            job => job.RunAsync(CancellationToken.None),
            Cron.MinuteInterval(15));

        return application;
    }
}
