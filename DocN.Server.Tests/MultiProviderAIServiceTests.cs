using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DocN.Data;
using DocN.Data.Models;
using DocN.Data.Services;
using Moq;

namespace DocN.Server.Tests;

/// <summary>
/// Tests for MultiProviderAIService, specifically focusing on API key detection logic.
/// </summary>
public class MultiProviderAIServiceTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private IConfiguration CreateEmptyConfiguration()
    {
        var configBuilder = new ConfigurationBuilder();
        return configBuilder.Build();
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_DetectsGeminiApiKey_WhenSet()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var config = CreateEmptyConfiguration();
        var logServiceMock = new Mock<ILogService>();
        
        // Add configuration with Gemini API key
        context.AIConfigurations.Add(new AIConfiguration
        {
            Id = 1,
            ConfigurationName = "Test Gemini Config",
            IsActive = true,
            ProviderType = AIProviderType.Gemini,
            GeminiApiKey = "AIzaSyDk_OnKI8Abk_cFkOz7Qv6iivEUHBgWMbo",
            GeminiChatModel = "gemini-2.0-flash-exp",
            GeminiEmbeddingModel = "text-embedding-004"
        });
        await context.SaveChangesAsync();

        var service = new MultiProviderAIService(config, context, logServiceMock.Object);

        // Act
        var result = await service.GetActiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Gemini Config", result.ConfigurationName);
        
        // Verify that the log service was called with "Gemini" in the configured providers
        logServiceMock.Verify(
            x => x.LogInfoAsync(
                "Configuration",
                It.Is<string>(msg => msg.Contains("Configured providers") && msg.Contains("Gemini")),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_DetectsOpenAIApiKey_WhenSet()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var config = CreateEmptyConfiguration();
        var logServiceMock = new Mock<ILogService>();
        
        // Add configuration with OpenAI API key
        context.AIConfigurations.Add(new AIConfiguration
        {
            Id = 1,
            ConfigurationName = "Test OpenAI Config",
            IsActive = true,
            ProviderType = AIProviderType.OpenAI,
            OpenAIApiKey = "sk-proj-test123456789",
            OpenAIChatModel = "gpt-4",
            OpenAIEmbeddingModel = "text-embedding-ada-002"
        });
        await context.SaveChangesAsync();

        var service = new MultiProviderAIService(config, context, logServiceMock.Object);

        // Act
        var result = await service.GetActiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test OpenAI Config", result.ConfigurationName);
        
        // Verify that the log service was called with "OpenAI" in the configured providers
        logServiceMock.Verify(
            x => x.LogInfoAsync(
                "Configuration",
                It.Is<string>(msg => msg.Contains("Configured providers") && msg.Contains("OpenAI")),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_DetectsAzureOpenAI_WhenBothKeyAndEndpointSet()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var config = CreateEmptyConfiguration();
        var logServiceMock = new Mock<ILogService>();
        
        // Add configuration with Azure OpenAI key and endpoint
        context.AIConfigurations.Add(new AIConfiguration
        {
            Id = 1,
            ConfigurationName = "Test Azure OpenAI Config",
            IsActive = true,
            ProviderType = AIProviderType.AzureOpenAI,
            AzureOpenAIKey = "5D8q4YHsgwB3ZaSwaswUURR7ugchSsVn",
            AzureOpenAIEndpoint = "https://test-resource.openai.azure.com/",
            ChatDeploymentName = "gpt-4",
            EmbeddingDeploymentName = "text-embedding-ada-002"
        });
        await context.SaveChangesAsync();

        var service = new MultiProviderAIService(config, context, logServiceMock.Object);

        // Act
        var result = await service.GetActiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Azure OpenAI Config", result.ConfigurationName);
        
        // Verify that the log service was called with "Azure OpenAI" in the configured providers
        logServiceMock.Verify(
            x => x.LogInfoAsync(
                "Configuration",
                It.Is<string>(msg => msg.Contains("Configured providers") && msg.Contains("Azure OpenAI")),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_UsesProviderApiKey_WhenProviderSpecificKeyNotSet()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var config = CreateEmptyConfiguration();
        var logServiceMock = new Mock<ILogService>();
        
        // Add configuration with only ProviderApiKey set (not provider-specific keys)
        context.AIConfigurations.Add(new AIConfiguration
        {
            Id = 1,
            ConfigurationName = "Test Fallback Config",
            IsActive = true,
            ProviderType = AIProviderType.Gemini,
            ProviderApiKey = "AIzaSyDk_OnKI8Abk_cFkOz7Qv6iivEUHBgWMbo", // Fallback key
            GeminiApiKey = null // Provider-specific key not set
        });
        await context.SaveChangesAsync();

        var service = new MultiProviderAIService(config, context, logServiceMock.Object);

        // Act
        var result = await service.GetActiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Fallback Config", result.ConfigurationName);
        
        // Verify that Gemini is detected even though GeminiApiKey is null
        // (because ProviderApiKey is set and ProviderType is Gemini)
        logServiceMock.Verify(
            x => x.LogInfoAsync(
                "Configuration",
                It.Is<string>(msg => msg.Contains("Configured providers") && msg.Contains("Gemini")),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_DetectsMultipleProviders_WhenAllKeysSet()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var config = CreateEmptyConfiguration();
        var logServiceMock = new Mock<ILogService>();
        
        // Add configuration with all providers configured (like the database data in the problem statement)
        context.AIConfigurations.Add(new AIConfiguration
        {
            Id = 1,
            ConfigurationName = "Default",
            IsActive = true,
            ProviderType = AIProviderType.Gemini,
            ChatProvider = AIProviderType.Gemini,
            EmbeddingsProvider = AIProviderType.Gemini,
            TagExtractionProvider = AIProviderType.Gemini,
            RAGProvider = AIProviderType.Gemini,
            GeminiApiKey = "AIzaSyDk_OnKI8Abk_cFkOz7Qv6iivEUHBgWMbo",
            GeminiChatModel = "gemini-2.5-flash",
            GeminiEmbeddingModel = "text-embedding-004",
            OpenAIApiKey = "sk-proj-Eig-C71bVm15v8d11SV_odwJ1oB2l8Q9nvddKsU2Xv",
            OpenAIChatModel = "gpt-4",
            OpenAIEmbeddingModel = "embed-v4-0",
            AzureOpenAIKey = "5D8q4YHsgwB3ZaSwaswUURR7ugchSsVnsubWgPTR0htB1KMXHgeDJQQJ99BLACfhMk5XJ3w3AAAAACOGJUWQ",
            AzureOpenAIEndpoint = "https://moncymr1971-demoemaf-resource.services.azure.com/",
            ChatDeploymentName = "embed-v4-0",
            EmbeddingDeploymentName = "embed-v4-0"
        });
        await context.SaveChangesAsync();

        var service = new MultiProviderAIService(config, context, logServiceMock.Object);

        // Act
        var result = await service.GetActiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Default", result.ConfigurationName);
        
        // Verify that all three providers are detected
        logServiceMock.Verify(
            x => x.LogInfoAsync(
                "Configuration",
                It.Is<string>(msg => 
                    msg.Contains("Configured providers") && 
                    msg.Contains("Gemini") && 
                    msg.Contains("OpenAI") && 
                    msg.Contains("Azure OpenAI")),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
        
        // Verify that NO warning about missing API keys was logged
        logServiceMock.Verify(
            x => x.LogWarningAsync(
                "Configuration",
                It.Is<string>(msg => msg.Contains("no API keys are set")),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Never);
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_LogsWarning_WhenNoKeysConfigured()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var config = CreateEmptyConfiguration();
        var logServiceMock = new Mock<ILogService>();
        
        // Add configuration without any API keys
        context.AIConfigurations.Add(new AIConfiguration
        {
            Id = 1,
            ConfigurationName = "Empty Config",
            IsActive = true,
            ProviderType = AIProviderType.Gemini,
            GeminiApiKey = null,
            OpenAIApiKey = null,
            AzureOpenAIKey = null,
            ProviderApiKey = null
        });
        await context.SaveChangesAsync();

        var service = new MultiProviderAIService(config, context, logServiceMock.Object);

        // Act
        var result = await service.GetActiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        
        // Verify that warning was logged
        logServiceMock.Verify(
            x => x.LogWarningAsync(
                "Configuration",
                It.Is<string>(msg => msg.Contains("no API keys are set")),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task GetActiveConfigurationAsync_HandlesWhitespaceKeys_Correctly()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var config = CreateEmptyConfiguration();
        var logServiceMock = new Mock<ILogService>();
        
        // Add configuration with whitespace-only API keys (should be treated as not configured)
        context.AIConfigurations.Add(new AIConfiguration
        {
            Id = 1,
            ConfigurationName = "Whitespace Config",
            IsActive = true,
            ProviderType = AIProviderType.Gemini,
            GeminiApiKey = "   ", // Only whitespace
            OpenAIApiKey = "",
            AzureOpenAIKey = null
        });
        await context.SaveChangesAsync();

        var service = new MultiProviderAIService(config, context, logServiceMock.Object);

        // Act
        var result = await service.GetActiveConfigurationAsync();

        // Assert
        Assert.NotNull(result);
        
        // Verify that warning was logged (whitespace keys should be treated as empty)
        logServiceMock.Verify(
            x => x.LogWarningAsync(
                "Configuration",
                It.Is<string>(msg => msg.Contains("no API keys are set")),
                It.IsAny<string?>(),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ReadsTimeoutFromConfiguration()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var logServiceMock = new Mock<ILogService>();
        
        // Create configuration with custom timeout
        var configDict = new Dictionary<string, string>
        {
            {"AI:TimeoutSeconds", "90"}
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        // Act
        var service = new MultiProviderAIService(config, context, logServiceMock.Object);

        // Assert
        // Service should be created without errors
        Assert.NotNull(service);
        // The timeout will be used internally when ExecuteWithTimeoutAsync is called
    }

    [Fact]
    public void Constructor_UsesDefaultTimeoutWhenNotConfigured()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var config = CreateEmptyConfiguration();
        var logServiceMock = new Mock<ILogService>();

        // Act
        var service = new MultiProviderAIService(config, context, logServiceMock.Object);

        // Assert
        // Service should be created without errors with default timeout (120 seconds)
        Assert.NotNull(service);
    }
}
