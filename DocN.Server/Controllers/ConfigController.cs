using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Models;
using Microsoft.Extensions.Logging;

namespace DocN.Server.Controllers;

/// <summary>
/// Endpoints per la gestione delle configurazioni AI e test di connettività
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConfigController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConfigController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ConfigController(
        ApplicationDbContext context,
        ILogger<ConfigController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Testa la configurazione dei provider AI
    /// </summary>
    /// <returns>Risultati del test per ogni provider configurato</returns>
    /// <response code="200">Test completato con successo</response>
    /// <response code="404">Nessuna configurazione attiva trovata</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost("test")]
    [ProducesResponseType(typeof(ConfigurationTestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ConfigurationTestResult>> TestConfiguration()
    {
        try
        {
            _logger.LogInformation("Starting configuration test");

            // Get active configuration
            var config = await _context.AIConfigurations
                .FirstOrDefaultAsync(c => c.IsActive);

            if (config == null)
            {
                return NotFound(new ConfigurationTestResult
                {
                    Success = false,
                    Message = "Nessuna configurazione attiva trovata",
                    ProviderResults = new List<ProviderTestResult>()
                });
            }

            var result = new ConfigurationTestResult
            {
                Success = true,
                Message = "Test della configurazione completato",
                ProviderResults = new List<ProviderTestResult>()
            };

            // Test Gemini provider if configured
            if (!string.IsNullOrEmpty(config.GeminiApiKey))
            {
                var geminiResult = await TestGeminiProvider(config);
                result.ProviderResults.Add(geminiResult);
            }

            // Test OpenAI provider if configured
            if (!string.IsNullOrEmpty(config.OpenAIApiKey))
            {
                var openAIResult = await TestOpenAIProvider(config);
                result.ProviderResults.Add(openAIResult);
            }

            // Test Azure OpenAI provider if configured
            if (!string.IsNullOrEmpty(config.AzureOpenAIKey) && !string.IsNullOrEmpty(config.AzureOpenAIEndpoint))
            {
                var azureResult = await TestAzureOpenAIProvider(config);
                result.ProviderResults.Add(azureResult);
            }

            // Determine overall success
            result.Success = result.ProviderResults.Any() && 
                           result.ProviderResults.Any(r => r.IsConfigured && r.IsValid);

            if (!result.ProviderResults.Any())
            {
                result.Message = "⚠️ Nessun provider configurato. Configura almeno un provider AI.";
                result.Success = false;
            }
            else if (!result.Success)
            {
                result.Message = "❌ Tutti i provider configurati hanno fallito il test di connessione.";
            }
            else
            {
                var successCount = result.ProviderResults.Count(r => r.IsConfigured && r.IsValid);
                var totalCount = result.ProviderResults.Count(r => r.IsConfigured);
                result.Message = $"✅ Test completato: {successCount}/{totalCount} provider funzionanti";
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing configuration");
            return StatusCode(500, new ConfigurationTestResult
            {
                Success = false,
                Message = $"Errore durante il test: {ex.Message}",
                ProviderResults = new List<ProviderTestResult>()
            });
        }
    }

    private async Task<ProviderTestResult> TestGeminiProvider(AIConfiguration config)
    {
        var result = new ProviderTestResult
        {
            ProviderName = "Google Gemini",
            ProviderType = AIProviderType.Gemini,
            IsConfigured = true,
            Services = new List<ServiceTestResult>()
        };

        try
        {
            // Test API key validity by making a simple request
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("x-goog-api-key", config.GeminiApiKey);

            // Use models endpoint without API key in URL for security
            var testUrl = "https://generativelanguage.googleapis.com/v1beta/models";
            var response = await httpClient.GetAsync(testUrl);

            if (response.IsSuccessStatusCode)
            {
                result.IsValid = true;
                result.Message = "✅ API Key valida";

                // Test Chat service
                if (config.ChatProvider == AIProviderType.Gemini)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "Chat",
                        Model = config.GeminiChatModel ?? "gemini-1.5-flash",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }

                // Test Embeddings service
                if (config.EmbeddingsProvider == AIProviderType.Gemini)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "Embeddings",
                        Model = config.GeminiEmbeddingModel ?? "text-embedding-004",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }

                // Test Tag Extraction service
                if (config.TagExtractionProvider == AIProviderType.Gemini)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "Tag Extraction",
                        Model = config.GeminiChatModel ?? "gemini-1.5-flash",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }

                // Test RAG service
                if (config.RAGProvider == AIProviderType.Gemini)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "RAG",
                        Model = config.GeminiChatModel ?? "gemini-1.5-flash",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }
            }
            else
            {
                result.IsValid = false;
                var errorContent = await response.Content.ReadAsStringAsync();
                result.Message = $"❌ API Key non valida: {response.StatusCode}";
                _logger.LogWarning("Gemini API test failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Message = $"❌ Errore di connessione: {ex.Message}";
            _logger.LogError(ex, "Error testing Gemini provider");
        }

        return result;
    }

    private async Task<ProviderTestResult> TestOpenAIProvider(AIConfiguration config)
    {
        var result = new ProviderTestResult
        {
            ProviderName = "OpenAI",
            ProviderType = AIProviderType.OpenAI,
            IsConfigured = true,
            Services = new List<ServiceTestResult>()
        };

        try
        {
            // Test API key validity by listing models
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.OpenAIApiKey}");

            var response = await httpClient.GetAsync("https://api.openai.com/v1/models");

            if (response.IsSuccessStatusCode)
            {
                result.IsValid = true;
                result.Message = "✅ API Key valida";

                // Test Chat service
                if (config.ChatProvider == AIProviderType.OpenAI)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "Chat",
                        Model = config.OpenAIChatModel ?? "gpt-4",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }

                // Test Embeddings service
                if (config.EmbeddingsProvider == AIProviderType.OpenAI)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "Embeddings",
                        Model = config.OpenAIEmbeddingModel ?? "text-embedding-ada-002",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }

                // Test Tag Extraction service
                if (config.TagExtractionProvider == AIProviderType.OpenAI)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "Tag Extraction",
                        Model = config.OpenAIChatModel ?? "gpt-4",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }

                // Test RAG service
                if (config.RAGProvider == AIProviderType.OpenAI)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "RAG",
                        Model = config.OpenAIChatModel ?? "gpt-4",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }
            }
            else
            {
                result.IsValid = false;
                var errorContent = await response.Content.ReadAsStringAsync();
                result.Message = $"❌ API Key non valida: {response.StatusCode}";
                _logger.LogWarning("OpenAI API test failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Message = $"❌ Errore di connessione: {ex.Message}";
            _logger.LogError(ex, "Error testing OpenAI provider");
        }

        return result;
    }

    private async Task<ProviderTestResult> TestAzureOpenAIProvider(AIConfiguration config)
    {
        var result = new ProviderTestResult
        {
            ProviderName = "Azure OpenAI",
            ProviderType = AIProviderType.AzureOpenAI,
            IsConfigured = true,
            Services = new List<ServiceTestResult>()
        };

        try
        {
            // Test API key and endpoint validity
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("api-key", config.AzureOpenAIKey);

            // Try to list deployments to verify connection
            var endpoint = config.AzureOpenAIEndpoint?.TrimEnd('/');
            var testUrl = $"{endpoint}/openai/deployments?api-version=2023-05-15";
            var response = await httpClient.GetAsync(testUrl);

            if (response.IsSuccessStatusCode)
            {
                result.IsValid = true;
                result.Message = "✅ API Key e Endpoint validi";

                // Test Chat service
                if (config.ChatProvider == AIProviderType.AzureOpenAI)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "Chat",
                        Model = config.ChatDeploymentName ?? config.AzureOpenAIChatModel ?? "gpt-4",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }

                // Test Embeddings service
                if (config.EmbeddingsProvider == AIProviderType.AzureOpenAI)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "Embeddings",
                        Model = config.EmbeddingDeploymentName ?? config.AzureOpenAIEmbeddingModel ?? "text-embedding-ada-002",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }

                // Test Tag Extraction service
                if (config.TagExtractionProvider == AIProviderType.AzureOpenAI)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "Tag Extraction",
                        Model = config.ChatDeploymentName ?? config.AzureOpenAIChatModel ?? "gpt-4",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }

                // Test RAG service
                if (config.RAGProvider == AIProviderType.AzureOpenAI)
                {
                    result.Services.Add(new ServiceTestResult
                    {
                        ServiceName = "RAG",
                        Model = config.ChatDeploymentName ?? config.AzureOpenAIChatModel ?? "gpt-4",
                        Status = "✅ Configurato",
                        IsHealthy = true
                    });
                }
            }
            else
            {
                result.IsValid = false;
                var errorContent = await response.Content.ReadAsStringAsync();
                result.Message = $"❌ Endpoint o API Key non validi: {response.StatusCode}";
                _logger.LogWarning("Azure OpenAI API test failed: {StatusCode} - {Error}", response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Message = $"❌ Errore di connessione: {ex.Message}";
            _logger.LogError(ex, "Error testing Azure OpenAI provider");
        }

        return result;
    }
}
