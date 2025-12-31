using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Server.Controllers;

namespace DocN.Server.Tests;

/// <summary>
/// Integration tests to verify ConfigController can be resolved from DI container
/// This test simulates the actual DI configuration from Program.cs
/// </summary>
public class ConfigControllerIntegrationTests
{
    [Fact]
    public void ConfigController_CanBeResolved_FromDIContainer()
    {
        // Arrange - Simulate Program.cs DI configuration
        var services = new ServiceCollection();
        
        // Add the services like in Program.cs (minimal set needed for ConfigController)
        services.AddHttpClient();  // This is our fix!
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("IntegrationTestDb"));
        services.AddLogging();
        
        // Register the controller explicitly for testing
        services.AddScoped<ConfigController>();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - This would throw InvalidOperationException before our fix
        var exception = Record.Exception(() => 
        {
            using var scope = serviceProvider.CreateScope();
            var controller = scope.ServiceProvider.GetRequiredService<ConfigController>();
            Assert.NotNull(controller);
        });

        // Should not throw any exception
        Assert.Null(exception);
    }
}
