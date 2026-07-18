using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utano.Module.Notifications.DatabaseMappings;
using Utano.Module.Notifications.Domain.Interfaces;
using Utano.Module.Notifications.Infrastructure.Repositories;

namespace Utano.Module.Notifications.Configuration;

public static class AppConfiguration
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UtanoDb")));

        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }

    public static WebApplication ConfigureNotificationsModule(this WebApplication app)
        => app;
}
