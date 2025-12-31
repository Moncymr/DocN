using Xunit;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Models;
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

        // Act & Assert - Should not throw exception
        var controller = new ConfigController(
            context,
            loggerMock.Object,
            httpClientFactoryMock.Object);

        Assert.NotNull(controller);
    }

    [Fact]
    public async Task TestConfiguration_ReturnsNotFound_WhenNoActiveConfiguration()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<ConfigController>>();
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();

        var controller = new ConfigController(
            context,
            loggerMock.Object,
            httpClientFactoryMock.Object);

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

        var controller = new ConfigController(
            context,
            loggerMock.Object,
            httpClientFactoryMock.Object);

        // Act
        var result = await controller.TestConfiguration();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var testResult = Assert.IsType<ConfigurationTestResult>(okResult.Value);
        Assert.False(testResult.Success);
        Assert.Contains("Nessun provider configurato", testResult.Message);
        Assert.Empty(testResult.ProviderResults);
    }
}
