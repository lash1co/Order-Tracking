using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OrderTracking.IntegrationTests;

public sealed class OrderTrackingApiFactory : WebApplicationFactory<Program>
{
    public OrderTrackingApiFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__OrderTracking",
            "Server=(localdb)\\mssqllocaldb;Database=OrderTrackingTests;Trusted_Connection=True;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "OrderTracking.API");
        Environment.SetEnvironmentVariable("Jwt__Audience", "OrderTracking.UI");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "integration-test-signing-key-32-characters-minimum");
        Environment.SetEnvironmentVariable("Redis__Enabled", "false");
        Environment.SetEnvironmentVariable("RabbitMQ__Enabled", "false");
        Environment.SetEnvironmentVariable("DriverMovementSimulator__Enabled", "false");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            var keyDirectory = new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "data-protection-keys"));
            keyDirectory.Create();
            services
                .AddDataProtection()
                .PersistKeysToFileSystem(keyDirectory);
        });
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OrderTracking"] =
                    "Server=(localdb)\\mssqllocaldb;Database=OrderTrackingTests;Trusted_Connection=True;TrustServerCertificate=True",
                ["Jwt:Issuer"] = "OrderTracking.API",
                ["Jwt:Audience"] = "OrderTracking.UI",
                ["Jwt:SigningKey"] = "integration-test-signing-key-32-characters-minimum",
                ["Redis:Enabled"] = "false",
                ["RabbitMQ:Enabled"] = "false",
                ["DriverMovementSimulator:Enabled"] = "false"
            });
        });
    }
}
