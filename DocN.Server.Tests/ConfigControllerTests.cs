using Xunit;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Models;
using DocN.Data.Services;
using DocN.Server.Controllers;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace DocN.Server.Tests;

public class ConfigControllerTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void ConfigController_CanBeConstructed_WithIHttpClientFactory()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<ConfigController>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var aiServiceMock = new Mock<IMultiProviderAIService>();

        // Act & Assert - Should not throw exception
        var controller = new ConfigController(
            context,
            loggerMock.Object,
            httpClientFactoryMock.Object,
            aiServiceMock.Object);

        Assert.NotNull(controller);
    }

    [Fact]
    public async Task TestConfiguration_ReturnsNotFound_WhenNoActiveConfiguration()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<ConfigController>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var aiServiceMock = new Mock<IMultiProviderAIService>();

        var controller = new ConfigController(
            context,
            loggerMock.Object,
            httpClientFactoryMock.Object,
            aiServiceMock.Object);

        // Act
        var result = await controller.TestConfiguration();

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var testResult = Assert.IsType<ConfigurationTestResult>(notFoundResult.Value);
        Assert.False(testResult.Success);
        Assert.Contains("Nessuna configurazione attiva trovata", testResult.Message);
    }

    [Fact]
    public async Task TestConfiguration_ReturnsWarning_WhenNoProviderConfigured()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        // Add an active configuration with no API keys configured (like the default seeded config)
        context.AIConfigurations.Add(new AIConfiguration
        {
            Id = 1,
            ConfigurationName = "Default Configuration",
            IsActive = true,
            ChatProvider = AIProviderType.Gemini,
            EmbeddingsProvider = AIProviderType.Gemini,
            TagExtractionProvider = AIProviderType.Gemini,
            RAGProvider = AIProviderType.Gemini,
            // No API keys configured - this simulates the default seeded configuration
            GeminiApiKey = null,
            OpenAIApiKey = null,
            AzureOpenAIKey = null
        });
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<ConfigController>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var aiServiceMock = new Mock<IMultiProviderAIService>();

        var controller = new ConfigController(
            context,
            loggerMock.Object,
            httpClientFactoryMock.Object,
            aiServiceMock.Object);

        // Act
        var result = await controller.TestConfiguration();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var testResult = Assert.IsType<ConfigurationTestResult>(okResult.Value);
        Assert.False(testResult.Success);
        Assert.Contains("Nessun provider configurato", testResult.Message);
        Assert.Empty(testResult.ProviderResults);
    }

    [Fact]
    public async Task GetConfigurationDiagnostics_ReturnsActualDatabaseIds()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        // Add configurations with specific IDs to test that actual IDs are returned
        // This tests the issue where ID=3 in DB should show as ID=3, not ID=1
        context.AIConfigurations.Add(new AIConfiguration
        {
            Id = 3, // Explicitly set ID to 3 to match the issue scenario
            ConfigurationName = "Test Configuration",
            IsActive = true,
            ChatProvider = AIProviderType.Gemini,
            EmbeddingsProvider = AIProviderType.Gemini,
            TagExtractionProvider = AIProviderType.Gemini,
            RAGProvider = AIProviderType.Gemini,
            GeminiApiKey = "test-key",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        
        context.AIConfigurations.Add(new AIConfiguration
        {
            Id = 5,
            ConfigurationName = "Another Configuration",
            IsActive = false,
            ChatProvider = AIProviderType.OpenAI,
            OpenAIApiKey = "test-openai-key",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        
        await context.SaveChangesAsync();

        var loggerMock = new Mock<ILogger<ConfigController>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var aiServiceMock = new Mock<IMultiProviderAIService>();

        var controller = new ConfigController(
            context,
            loggerMock.Object,
            httpClientFactoryMock.Object,
            aiServiceMock.Object);

        // Act
        var result = await controller.GetConfigurationDiagnostics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic? diagnostics = okResult.Value;
        
        Assert.NotNull(diagnostics);
        
        // Verify total configurations count
        Assert.Equal(2, (int)diagnostics.GetType().GetProperty("totalConfigurations")?.GetValue(diagnostics)!);
        
        // Verify active configuration has the correct ID from database (3, not 1)
        var activeConfig = diagnostics.GetType().GetProperty("activeConfiguration")?.GetValue(diagnostics);
        Assert.NotNull(activeConfig);
        
        var activeConfigId = (int)activeConfig.GetType().GetProperty("id")?.GetValue(activeConfig)!;
        Assert.Equal(3, activeConfigId); // This is the key assertion - ID should be 3 from DB
        
        var activeConfigName = (string)activeConfig.GetType().GetProperty("name")?.GetValue(activeConfig)!;
        Assert.Equal("Test Configuration", activeConfigName);
        
        // Verify all configurations include correct IDs
        var allConfigs = diagnostics.GetType().GetProperty("allConfigurations")?.GetValue(diagnostics) as System.Collections.IEnumerable;
        Assert.NotNull(allConfigs);
        
        var configList = allConfigs.Cast<object>().ToList();
        Assert.Equal(2, configList.Count);
        
        // First config should be the active one (ID=3)
        var firstConfig = configList[0];
        var firstConfigId = (int)firstConfig.GetType().GetProperty("Id")?.GetValue(firstConfig)!;
        Assert.Equal(3, firstConfigId);
        
        // Second config should have ID=5
        var secondConfig = configList[1];
        var secondConfigId = (int)secondConfig.GetType().GetProperty("Id")?.GetValue(secondConfig)!;
        Assert.Equal(5, secondConfigId);
    }

    [Fact]
    public async Task GetConfigurationDiagnostics_ReturnsEmptyWhenNoConfigurations()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var loggerMock = new Mock<ILogger<ConfigController>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var aiServiceMock = new Mock<IMultiProviderAIService>();

        var controller = new ConfigController(
            context,
            loggerMock.Object,
            httpClientFactoryMock.Object,
            aiServiceMock.Object);

        // Act
        var result = await controller.GetConfigurationDiagnostics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        dynamic? diagnostics = okResult.Value;
        
        Assert.NotNull(diagnostics);
        
        // Verify no configurations
        Assert.Equal(0, (int)diagnostics.GetType().GetProperty("totalConfigurations")?.GetValue(diagnostics)!);
        
        // Verify active configuration is null
        var activeConfig = diagnostics.GetType().GetProperty("activeConfiguration")?.GetValue(diagnostics);
        Assert.Null(activeConfig);
        
        // Verify recommendations include warning about no active configuration
        var recommendations = diagnostics.GetType().GetProperty("recommendations")?.GetValue(diagnostics) as System.Collections.IEnumerable;
        Assert.NotNull(recommendations);
        
        var recommendationList = recommendations.Cast<string>().ToList();
        Assert.Contains(recommendationList, r => r.Contains("No active configuration found"));
    }
}
