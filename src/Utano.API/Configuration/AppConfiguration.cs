using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.OpenApi;
using System.Globalization;
using Utano.API.Filters;
using Utano.API.Infrastructure.Services;
using Utano.Module.Appointments.Configuration;
using Utano.Module.Billing.Configuration;
using Utano.Module.ClinicalNotes.Configuration;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Configuration;
using Utano.Module.Inventory.Configuration;
using Utano.Module.Patients.Configuration;

namespace Utano.API.Configuration;

public static class AppConfiguration
{
    public static WebApplicationBuilder ConfigureBuilder(this WebApplicationBuilder builder, string[]? args = null)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("UtanoPolicy", policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        builder.Services.AddIdentityModule(builder.Configuration);
        builder.Services.AddPatientsModule(builder.Configuration);
        builder.Services.AddAppointmentsModule(builder.Configuration);
        builder.Services.AddClinicalNotesModule(builder.Configuration);
        builder.Services.AddInventoryModule(builder.Configuration);
        builder.Services.AddBillingModule(builder.Configuration);

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(
                    new System.Text.Json.Serialization.JsonStringEnumConverter()));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "Utano API", Version = "v1" });
            options.EnableAnnotations();
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Description = "Enter: Bearer {your_token}"
            });
            options.DocumentFilter<BearerSecurityDocumentFilter>();
        });

        return builder;
    }

    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            KnownNetworks = { },
            KnownProxies = { }
        });
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        var supportedCultures = new[] { new CultureInfo("en") };
        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture("en"),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures,
            RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()]
        });

        app.UseCors("UtanoPolicy");
        app.UseMiddleware<CancellationMiddleware>();
        app.UseMiddleware<ApiKeyMiddleware>();
        app.UseRouting();
        app.ConfigureIdentityModule();
        app.UseAuthorization();
        app.MapControllers();
        app.ConfigurePatientsModule();
        app.ConfigureAppointmentsModule();
        app.ConfigureClinicalNotesModule();
        app.ConfigureInventoryModule();
        app.ConfigureBillingModule();

        return app;
    }
}