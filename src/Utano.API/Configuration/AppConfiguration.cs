using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Utano.API.Infrastructure.Services;
using Utano.Module.Core.Services;
using Utano.Module.Patients.Configuration;

namespace Utano.API.Configuration;

public static class AppConfiguration
{
    public static WebApplicationBuilder ConfigureBuilder(this WebApplicationBuilder builder, string[]? args = null)
    {
        builder.Services
            .AddScoped<ICurrentUserService, CurrentUserService>()
            .AddPatientsModule(builder.Configuration);
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        return builder;
    }

    public static WebApplication ConfigureApplication(this WebApplication web)
    {
        if (web.Environment.IsDevelopment())
        {
            web.UseSwagger();
            web.UseSwaggerUI();
        }

        web.UseHttpsRedirection();
        web.UseAuthorization();

        var supportedCultures = new[] { new CultureInfo("en") };

        web.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture("en"),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures,
            RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()]
        })
        .UseRouting()
        .UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });

        web.ConfigurePatientsModule();

        return web;
    }
}