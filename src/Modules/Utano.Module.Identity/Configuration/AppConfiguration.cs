using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Utano.Module.Core.Modules;
using Utano.Module.Core.Services;
using Utano.Module.Identity.DatabaseMappings;
using Utano.Module.Identity.Domain.Interfaces;
using Utano.Module.Identity.Infrastructure.Repositories;
using Utano.Module.Identity.Infrastructure.Services;

namespace Utano.Module.Identity.Configuration;

public static class AppConfiguration
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()!;

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("UtanoDb")));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Without this, the JWT bearer handler silently remaps well-known short
                // claim names ("sub", "email") to their long ClaimTypes equivalents during
                // validation, regardless of what the token was issued with. TokenService
                // issues "sub"/"email" as short names (JwtRegisteredClaimNames), so reading
                // them back via those same short names in CurrentUserService requires this
                // to be off - otherwise ICurrentUserService.UserId/Email always resolve to
                // Guid.Empty/"" no matter who's authenticated.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = configuration["MEDIATR_LICENSE_KEY"];
            cfg.RegisterServicesFromAssembly(typeof(AppConfiguration).Assembly);
        });

        services.AddValidatorsFromAssembly(typeof(AppConfiguration).Assembly);

        services.AddScoped<IUserReadRepository, UserReadRepository>();
        services.AddScoped<IUserWriteRepository, UserWriteRepository>();
        services.AddScoped<IPracticeRepository, PracticeRepository>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IFeatureService, FeatureService>();
        services.AddScoped<IUserPracticeValidator, UserPracticeValidator>();
        services.AddSingleton<IModuleDescriptor, UtanoCoreModuleDescriptor>();
        services.AddSingleton<IModuleDescriptor, ReportsModuleDescriptor>();

        return services;
    }

    public static WebApplication ConfigureIdentityModule(this WebApplication app)
    {
        app.UseAuthentication();
        return app;
    }
}
