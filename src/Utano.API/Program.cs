using Utano.API.Configuration;
using Utano.Module.Identity.Infrastructure.Services;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        var app = WebApplication
.CreateBuilder(args)
.ConfigureBuilder()
.Build()
.ConfigureApplication();

        // Additive-only reconciliation - keeps the Permissions catalog and every practice's
        // system-role permissions in sync with what the code currently declares. Runs once per
        // boot, before the app starts serving requests.
        await PermissionReconciler.ReconcileAsync(app.Services);

        await app.RunAsync();
    }
}

public partial class Program
{
}