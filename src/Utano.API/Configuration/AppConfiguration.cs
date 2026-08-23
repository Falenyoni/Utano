using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using System.Globalization;
using System.Threading.RateLimiting;
using Utano.API.Filters;
using Utano.API.Infrastructure.Services;
using Utano.Module.Appointments.Configuration;
using Utano.Module.Billing.Configuration;
using Utano.Module.ClinicalNotes.Configuration;
using Utano.Module.Core.Authorization;
using Utano.Module.Core.Services;
using Utano.Module.Identity.Configuration;
using Utano.Module.Files.Configuration;
using Utano.Module.Inventory.Configuration;
using Utano.Module.Notifications.Configuration;
using Utano.Module.Patients.Configuration;

namespace Utano.API.Configuration;

public static class AppConfiguration
{
    public static WebApplicationBuilder ConfigureBuilder(this WebApplicationBuilder builder, string[]? args = null)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        // "signup": public self-service registration has no auth of its own to lean on, so it
        // gets its own rate limit - keyed per client IP, 5 attempts/hour. A genuine user retrying
        // a typo'd field a couple of times is fine; a script spinning up fake trial practices
        // isn't.
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Baseline applied to every request regardless of endpoint - until now, only the 3
            // named policies below had any throttling at all, meaning every other endpoint
            // (patients, billing, inventory, appointments...) had none. This doesn't replace those
            // tighter policies, it stacks under them: a request still has to clear both.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy("signup", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                    }));

            // "login": second layer alongside per-account lockout (User.RecordFailedLogin) -
            // catches a distributed attack trying many different email addresses from one IP,
            // which per-account lockout alone wouldn't. Deliberately more generous than the
            // account lockout threshold so a shared office IP with several legitimate users isn't
            // penalized for normal daily login traffic.
            options.AddPolicy("login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                    }));

            // Public, unauthenticated, and sends a real email per request - without this, it's an
            // easy way to spam a victim's inbox or burn through Resend's send quota.
            options.AddPolicy("forgot-password", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                    }));

            // Same shape as forgot-password - public, unauthenticated, sends a real email.
            options.AddPolicy("resend-verification", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                    }));
        });

        // Open-generic registrations - apply to every request across every module's own
        // AddMediatR() call, since they all resolve out of this same DI container.
        // PermissionAuthorizationBehavior only checks requests implementing IRequirePermission.
        // SubscriptionTierBehavior checks every request against its owning module's declared Plan.
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PermissionAuthorizationBehavior<,>));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(SubscriptionTierBehavior<,>));

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

        builder.Services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(
                builder.Configuration.GetConnectionString("UtanoDb"))));
        builder.Services.AddHangfireServer();

        builder.Services.AddIdentityModule(builder.Configuration);
        builder.Services.AddPatientsModule(builder.Configuration);
        builder.Services.AddAppointmentsModule(builder.Configuration);
        builder.Services.AddClinicalNotesModule(builder.Configuration);
        builder.Services.AddInventoryModule(builder.Configuration);
        builder.Services.AddBillingModule(builder.Configuration);
        builder.Services.AddFilesModule(builder.Configuration);
        builder.Services.AddNotificationsModule(builder.Configuration);

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
        app.UseExceptionHandler();

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
        app.UseRateLimiter();

        // The app's auth is JWT-in-header (SPA pattern), which a directly browser-navigated
        // dashboard can't carry - there's no cookie session to check a role against. Rather than
        // fake an "Admin-only" check that doesn't actually hold, restrict this to local requests
        // in Development only. Exposing it beyond that would need a real cookie-based admin
        // session, which is out of scope here.
        if (app.Environment.IsDevelopment())
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new LocalRequestsOnlyAuthorizationFilter()],
            });
        }

        app.ConfigureIdentityModule();
        app.UseAuthorization();
        app.UseMiddleware<SubscriptionMiddleware>();
        app.MapControllers();
        app.ConfigurePatientsModule();
        app.ConfigureAppointmentsModule();
        app.ConfigureClinicalNotesModule();
        app.ConfigureInventoryModule();
        app.ConfigureBillingModule();
        app.ConfigureFilesModule();
        app.ConfigureNotificationsModule();

        return app;
    }
}