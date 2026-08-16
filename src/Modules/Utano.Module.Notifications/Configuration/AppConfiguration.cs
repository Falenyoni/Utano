using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Utano.Module.Core.Modules;
using Utano.Module.Core.Services;
using Utano.Module.Notifications.DatabaseMappings;
using Utano.Module.Notifications.Domain.Interfaces;
using Utano.Module.Notifications.Infrastructure.Repositories;
using Utano.Module.Notifications.Infrastructure.Services;

namespace Utano.Module.Notifications.Configuration;

public static class AppConfiguration
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UtanoDb")));

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = configuration["MEDIATR_LICENSE_KEY"];
            cfg.RegisterServicesFromAssembly(typeof(AppConfiguration).Assembly);
        });

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddSingleton<IModuleDescriptor, NotificationsModuleDescriptor>();

        services.Configure<ResendSettings>(configuration.GetSection("Resend"));
        services.AddHttpClient<IEmailSender, ResendEmailSender>((sp, client) =>
        {
            var resendSettings = sp.GetRequiredService<IOptions<ResendSettings>>().Value;
            client.BaseAddress = new Uri("https://api.resend.com/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", resendSettings.ApiKey);
        });

        return services;
    }

    public static WebApplication ConfigureNotificationsModule(this WebApplication app)
        => app;
}
