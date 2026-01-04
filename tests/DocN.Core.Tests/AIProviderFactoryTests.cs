using DocN.Core.AI.Configuration;
using DocN.Core.AI.Interfaces;
using DocN.Core.AI.Models;
using DocN.Core.AI.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace DocN.Core.Tests;

/// <summary>
/// Test per verificare la factory dei provider AI
/// </summary>
public class AIProviderFactoryTests
{
    [Fact]
    public void CreateProvider_WithAzureOpenAI_ReturnsAzureOpenAIProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration(AIProviderType.AzureOpenAI);
        
        services.AddSingleton(Options.Create(configuration));
        services.AddSingleton<ILogger<AzureOpenAIProvider>>(new LoggerFactory().CreateLogger<AzureOpenAIProvider>());
        services.AddTransient<AzureOpenAIProvider>();
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IAIProviderFactory>();
        
        // Act
        var provider = factory.CreateProvider(AIProviderType.AzureOpenAI);
        
        // Assert
        Assert.NotNull(provider);
        Assert.IsType<AzureOpenAIProvider>(provider);
        Assert.Equal(AIProviderType.AzureOpenAI, provider.ProviderType);
        Assert.Equal("Azure OpenAI", provider.ProviderName);
    }

    [Fact]
    public void CreateProvider_WithOpenAI_ReturnsOpenAIProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration(AIProviderType.OpenAI);
        
        services.AddSingleton(Options.Create(configuration));
        services.AddSingleton<ILogger<OpenAIProvider>>(new LoggerFactory().CreateLogger<OpenAIProvider>());
        services.AddTransient<OpenAIProvider>();
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IAIProviderFactory>();
        
        // Act
        var provider = factory.CreateProvider(AIProviderType.OpenAI);
        
        // Assert
        Assert.NotNull(provider);
        Assert.IsType<OpenAIProvider>(provider);
        Assert.Equal(AIProviderType.OpenAI, provider.ProviderType);
        Assert.Equal("OpenAI", provider.ProviderName);
    }

    [Fact]
    public void CreateProvider_WithGemini_ReturnsGeminiProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration(AIProviderType.Gemini);
        
        services.AddSingleton(Options.Create(configuration));
        services.AddSingleton<ILogger<GeminiProvider>>(new LoggerFactory().CreateLogger<GeminiProvider>());
        services.AddTransient<GeminiProvider>();
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IAIProviderFactory>();
        
        // Act
        var provider = factory.CreateProvider(AIProviderType.Gemini);
        
        // Assert
        Assert.NotNull(provider);
        Assert.IsType<GeminiProvider>(provider);
        Assert.Equal(AIProviderType.Gemini, provider.ProviderType);
        Assert.Equal("Google Gemini", provider.ProviderName);
    }

    [Fact]
    public void CreateProvider_WithOllama_ReturnsOllamaProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration(AIProviderType.Ollama);
        
        services.AddSingleton(Options.Create(configuration));
        services.AddSingleton<ILogger<OllamaProvider>>(new LoggerFactory().CreateLogger<OllamaProvider>());
        services.AddTransient<OllamaProvider>();
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IAIProviderFactory>();
        
        // Act
        var provider = factory.CreateProvider(AIProviderType.Ollama);
        
        // Assert
        Assert.NotNull(provider);
        Assert.IsType<OllamaProvider>(provider);
        Assert.Equal(AIProviderType.Ollama, provider.ProviderType);
        Assert.Equal("Ollama", provider.ProviderName);
    }

    [Fact]
    public void CreateProvider_WithGroq_ReturnsGroqProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration(AIProviderType.Groq);
        
        services.AddSingleton(Options.Create(configuration));
        services.AddSingleton<ILogger<GroqProvider>>(new LoggerFactory().CreateLogger<GroqProvider>());
        services.AddTransient<GroqProvider>();
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IAIProviderFactory>();
        
        // Act
        var provider = factory.CreateProvider(AIProviderType.Groq);
        
        // Assert
        Assert.NotNull(provider);
        Assert.IsType<GroqProvider>(provider);
        Assert.Equal(AIProviderType.Groq, provider.ProviderType);
        Assert.Equal("Groq", provider.ProviderName);
    }

    [Fact]
    public void GetDefaultProvider_ReturnsConfiguredDefaultProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateTestConfiguration(AIProviderType.OpenAI);
        
        services.AddSingleton(Options.Create(configuration));
        services.AddSingleton<ILogger<OpenAIProvider>>(new LoggerFactory().CreateLogger<OpenAIProvider>());
        services.AddTransient<OpenAIProvider>();
        services.AddSingleton<IAIProviderFactory, AIProviderFactory>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IAIProviderFactory>();
        
        // Act
        var provider = factory.GetDefaultProvider();
        
        // Assert
        Assert.NotNull(provider);
        Assert.Equal(AIProviderType.OpenAI, provider.ProviderType);
    }

    private AIProviderConfiguration CreateTestConfiguration(AIProviderType defaultProvider)
    {
        return new AIProviderConfiguration
        {
            DefaultProvider = defaultProvider,
            AzureOpenAI = new AzureOpenAIConfiguration
            {
                Endpoint = "https://test.openai.azure.com/",
                ApiKey = "test-azure-key",
                EmbeddingDeployment = "test-embedding",
                ChatDeployment = "test-chat",
                ApiVersion = "2024-02-15-preview"
            },
            OpenAI = new OpenAIConfiguration
            {
                ApiKey = "test-openai-key",
                EmbeddingModel = "text-embedding-3-small",
                ChatModel = "gpt-4-turbo"
            },
            Gemini = new GeminiConfiguration
            {
                ApiKey = "test-gemini-key",
                EmbeddingModel = "text-embedding-004",
                GenerationModel = "gemini-1.5-pro"
            },
            Ollama = new OllamaConfiguration
            {
                Endpoint = "http://localhost:11434",
                EmbeddingModel = "nomic-embed-text",
                ChatModel = "llama3"
            },
            Groq = new GroqConfiguration
            {
                ApiKey = "test-groq-key",
                ChatModel = "llama-3.1-8b-instant",
                Endpoint = "https://api.groq.com/openai/v1"
            }
        };
    }
}
