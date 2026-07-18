using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utano.Module.Files.DatabaseMappings;
using Utano.Module.Files.Domain.Interfaces;
using Utano.Module.Files.Infrastructure.Repositories;
using Utano.Module.Files.Infrastructure.Services;

namespace Utano.Module.Files.Configuration;

public static class AppConfiguration
{
    public static IServiceCollection AddFilesModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<FilesDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UtanoDb")));

        services.Configure<FileStorageSettings>(
            configuration.GetSection("FileStorage"));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(AppConfiguration).Assembly));

        services.AddScoped<IFileAttachmentRepository, FileAttachmentRepository>();
        services.AddSingleton<IFileStorageService, R2FileStorageService>();

        return services;
    }

    public static WebApplication ConfigureFilesModule(this WebApplication app)
    {
        return app;
    }
}
