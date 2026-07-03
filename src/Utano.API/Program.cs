using Utano.API.Configuration;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        var app = WebApplication
.CreateBuilder(args)
.ConfigureBuilder()
.Build()
.ConfigureApplication();
        await app.RunAsync();
    }
}

public partial class Program
{
}