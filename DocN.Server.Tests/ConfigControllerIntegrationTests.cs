using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Services;
using DocN.Server.Controllers;
using System.Collections.Generic;

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
        
        // Add configuration (needed by MultiProviderAIService)
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        services.AddSingleton<IConfiguration>(configuration);
        
        // Add the services like in Program.cs (minimal set needed for ConfigController)
        services.AddHttpClient();  // Required for IHttpClientFactory
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase("IntegrationTestDb"));
        services.AddLogging();
        
        // Add required services for ConfigController
        services.AddScoped<ILogService, LogService>();
        services.AddScoped<IMultiProviderAIService, MultiProviderAIService>();
        
        // Register the controller explicitly for testing
        services.AddScoped<ConfigController>();

        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert - This would throw InvalidOperationException if dependencies are missing
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
