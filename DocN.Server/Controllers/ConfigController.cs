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

    /// <summary>
    /// Ottiene informazioni diagnostiche sulla configurazione AI
    /// </summary>
    /// <returns>Informazioni diagnostiche dettagliate</returns>
    /// <response code="200">Diagnostica completata con successo</response>
    /// <response code="500">Errore interno del server</response>
    [HttpGet("diagnostica")]
    [ProducesResponseType(typeof(DiagnosticResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DiagnosticResult>> GetDiagnostics()
    {
        try
        {
            _logger.LogInformation("Getting configuration diagnostics");

            var result = new DiagnosticResult
            {
                Timestamp = DateTime.UtcNow,
                Configurations = new List<ConfigurationInfo>()
            };

            // Get all configurations
            var allConfigs = await _context.AIConfigurations.ToListAsync();
            result.TotalConfigurations = allConfigs.Count;
            result.ActiveConfiguration = allConfigs.FirstOrDefault(c => c.IsActive);

            foreach (var config in allConfigs)
            {
                var configInfo = new ConfigurationInfo
                {
                    Id = config.Id,
                    Name = config.ConfigurationName,
                    IsActive = config.IsActive,
                    CreatedAt = config.CreatedAt,
                    UpdatedAt = config.UpdatedAt,
                    ProvidersConfigured = new List<string>()
                };

                // Check which providers are configured
                if (!string.IsNullOrEmpty(config.GeminiApiKey))
                    configInfo.ProvidersConfigured.Add("Gemini");
                if (!string.IsNullOrEmpty(config.OpenAIApiKey))
                    configInfo.ProvidersConfigured.Add("OpenAI");
                if (!string.IsNullOrEmpty(config.AzureOpenAIKey) && !string.IsNullOrEmpty(config.AzureOpenAIEndpoint))
                    configInfo.ProvidersConfigured.Add("Azure OpenAI");

                // Service assignments
                configInfo.ChatProvider = config.ChatProvider?.ToString() ?? "Not Set";
                configInfo.EmbeddingsProvider = config.EmbeddingsProvider?.ToString() ?? "Not Set";
                configInfo.TagExtractionProvider = config.TagExtractionProvider?.ToString() ?? "Not Set";
                configInfo.RAGProvider = config.RAGProvider?.ToString() ?? "Not Set";

                result.Configurations.Add(configInfo);
            }

            result.Success = true;
            result.Message = $"Diagnostica completata: {result.TotalConfigurations} configurazioni trovate";

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting diagnostics");
            return StatusCode(500, new DiagnosticResult
            {
                Success = false,
                Message = $"Errore durante la diagnostica: {ex.Message}",
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Resetta la configurazione attiva ai valori predefiniti
    /// </summary>
    /// <returns>Risultato dell'operazione di reset</returns>
    /// <response code="200">Reset completato con successo</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(ResetResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ResetResult>> ResetConfiguration()
    {
        try
        {
            _logger.LogInformation("Resetting configuration to defaults");

            // Get active configuration
            var activeConfig = await _context.AIConfigurations
                .FirstOrDefaultAsync(c => c.IsActive);

            if (activeConfig != null)
            {
                // Reset to default values
                activeConfig.ConfigurationName = "Configurazione Predefinita";
                activeConfig.ProviderType = AIProviderType.Gemini;
                activeConfig.MaxDocumentsToRetrieve = 5;
                activeConfig.SimilarityThreshold = 0.7;
                activeConfig.MaxTokensForContext = 4000;
                activeConfig.EmbeddingDimensions = 768;
                activeConfig.SystemPrompt = "Sei un assistente utile che risponde a domande basandoti sui documenti forniti. Cita sempre i documenti fonte quando fornisci informazioni.";
                activeConfig.EnableChunking = true;
                activeConfig.ChunkSize = 1000;
                activeConfig.ChunkOverlap = 200;
                activeConfig.EnableFallback = true;
                activeConfig.GeminiChatModel = "gemini-1.5-flash";
                activeConfig.GeminiEmbeddingModel = "text-embedding-004";
                activeConfig.OpenAIChatModel = "gpt-4";
                activeConfig.OpenAIEmbeddingModel = "text-embedding-ada-002";
                activeConfig.AzureOpenAIChatModel = "gpt-4";
                activeConfig.AzureOpenAIEmbeddingModel = "text-embedding-ada-002";
                activeConfig.UpdatedAt = DateTime.UtcNow;

                _context.AIConfigurations.Update(activeConfig);
                await _context.SaveChangesAsync();

                return Ok(new ResetResult
                {
                    Success = true,
                    Message = "✅ Configurazione resettata ai valori predefiniti",
                    ConfigurationId = activeConfig.Id
                });
            }
            else
            {
                // Create a new default configuration if none exists
                var newConfig = new AIConfiguration
                {
                    ConfigurationName = "Configurazione Predefinita",
                    ProviderType = AIProviderType.Gemini,
                    MaxDocumentsToRetrieve = 5,
                    SimilarityThreshold = 0.7,
                    MaxTokensForContext = 4000,
                    EmbeddingDimensions = 768,
                    EmbeddingModel = "text-embedding-004",
                    SystemPrompt = "Sei un assistente utile che risponde a domande basandoti sui documenti forniti. Cita sempre i documenti fonte quando fornisci informazioni.",
                    IsActive = true,
                    EnableChunking = true,
                    ChunkSize = 1000,
                    ChunkOverlap = 200,
                    EnableFallback = true,
                    GeminiChatModel = "gemini-1.5-flash",
                    GeminiEmbeddingModel = "text-embedding-004",
                    OpenAIChatModel = "gpt-4",
                    OpenAIEmbeddingModel = "text-embedding-ada-002",
                    AzureOpenAIChatModel = "gpt-4",
                    AzureOpenAIEmbeddingModel = "text-embedding-ada-002",
                    CreatedAt = DateTime.UtcNow
                };

                _context.AIConfigurations.Add(newConfig);
                await _context.SaveChangesAsync();

                return Ok(new ResetResult
                {
                    Success = true,
                    Message = "✅ Nuova configurazione predefinita creata",
                    ConfigurationId = newConfig.Id
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting configuration");
            return StatusCode(500, new ResetResult
            {
                Success = false,
                Message = $"❌ Errore durante il reset: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Imposta una configurazione come predefinita dall'utente
    /// </summary>
    /// <param name="configId">ID della configurazione da impostare come predefinita</param>
    /// <returns>Risultato dell'operazione</returns>
    /// <response code="200">Configurazione impostata come predefinita</response>
    /// <response code="404">Configurazione non trovata</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost("set-default/{configId}")]
    [ProducesResponseType(typeof(SetDefaultResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SetDefaultResult>> SetDefaultConfiguration(int configId)
    {
        try
        {
            _logger.LogInformation("Setting configuration {ConfigId} as default", configId);

            var config = await _context.AIConfigurations.FindAsync(configId);
            
            if (config == null)
            {
                return NotFound(new SetDefaultResult
                {
                    Success = false,
                    Message = "❌ Configurazione non trovata"
                });
            }

            // Deactivate all other configurations
            var allConfigs = await _context.AIConfigurations.ToListAsync();
            foreach (var c in allConfigs)
            {
                c.IsActive = false;
            }

            // Activate the selected configuration
            config.IsActive = true;
            config.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new SetDefaultResult
            {
                Success = true,
                Message = $"✅ Configurazione '{config.ConfigurationName}' impostata come predefinita",
                ConfigurationId = config.Id,
                ConfigurationName = config.ConfigurationName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default configuration");
            return StatusCode(500, new SetDefaultResult
            {
                Success = false,
                Message = $"❌ Errore durante l'impostazione: {ex.Message}"
            });
        }
    }
}
