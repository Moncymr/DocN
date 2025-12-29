using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;
using Azure.AI.OpenAI;
using Azure;
using DocN.Data.Models;
using OpenAI.Embeddings;
using OpenAI.Chat;
using System.IO;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DocN.Data.Services;

public interface IMultiProviderAIService
{
    Task<float[]?> GenerateEmbeddingAsync(string text);
    Task<string> GenerateChatCompletionAsync(string systemPrompt, string userPrompt);
    Task<(string Category, string Reasoning, string Provider)> SuggestCategoryAsync(string fileName, string extractedText);
    Task<List<string>> ExtractTagsAsync(string extractedText);
    Task<Dictionary<string, string>> ExtractMetadataAsync(string extractedText, string fileName = "");
    string GetCurrentChatProvider();
    string GetCurrentEmbeddingProvider();
    Task<AIConfiguration?> GetActiveConfigurationAsync();
}

public class MultiProviderAIService : IMultiProviderAIService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly ILogService _logService;
    private AIConfiguration? _cachedConfig;
    private DateTime _lastConfigCheck = DateTime.MinValue;
    private readonly TimeSpan _configCacheDuration = TimeSpan.FromMinutes(5);

    public MultiProviderAIService(IConfiguration configuration, ApplicationDbContext context, ILogService logService)
    {
        _configuration = configuration;
        _context = context;
        _logService = logService;
    }

    /// <summary>
    /// Ottiene la configurazione attiva dal database, con caching per evitare troppe query
    /// </summary>
    public async Task<AIConfiguration?> GetActiveConfigurationAsync()
    {
        // Check if cached configuration is still valid
        if (_cachedConfig != null && DateTime.UtcNow - _lastConfigCheck < _configCacheDuration)
        {
            return _cachedConfig;
        }

        // Fetch active configuration from database
        _cachedConfig = await _context.AIConfigurations
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .FirstOrDefaultAsync();

        _lastConfigCheck = DateTime.UtcNow;

        // If no database configuration exists, create a default one from appsettings
        if (_cachedConfig == null)
        {
            _cachedConfig = CreateDefaultConfigurationFromAppSettings();
        }

        return _cachedConfig;
    }

    private AIConfiguration CreateDefaultConfigurationFromAppSettings()
    {
        // Fallback to appsettings.json configuration for backward compatibility
        return new AIConfiguration
        {
            ConfigurationName = "Default (from appsettings.json)",
            GeminiApiKey = _configuration["Gemini:ApiKey"],
            GeminiChatModel = "gemini-2.0-flash-exp",
            GeminiEmbeddingModel = "text-embedding-004",
            OpenAIApiKey = _configuration["OpenAI:ApiKey"],
            OpenAIChatModel = _configuration["OpenAI:Model"] ?? "gpt-4",
            OpenAIEmbeddingModel = "text-embedding-ada-002",
            AzureOpenAIEndpoint = _configuration["AzureOpenAI:Endpoint"],
            AzureOpenAIKey = _configuration["AzureOpenAI:ApiKey"] ?? _configuration["Embeddings:ApiKey"],
            ChatDeploymentName = _configuration["AzureOpenAI:ChatDeployment"],
            EmbeddingDeploymentName = _configuration["AzureOpenAI:EmbeddingDeployment"] ?? _configuration["Embeddings:DeploymentName"],
            AzureOpenAIChatModel = "gpt-4",
            AzureOpenAIEmbeddingModel = "text-embedding-ada-002",
            // Determine provider from settings
            ChatProvider = GetProviderFromConfig(_configuration["AI:Provider"]),
            EmbeddingsProvider = GetProviderFromConfig(_configuration["Embeddings:Provider"]),
            TagExtractionProvider = GetProviderFromConfig(_configuration["AI:Provider"]),
            RAGProvider = GetProviderFromConfig(_configuration["AI:Provider"]),
            EnableFallback = _configuration.GetValue<bool>("AI:EnableFallback", true),
            EnableChunking = true,
            ChunkSize = 1000,
            ChunkOverlap = 200,
            IsActive = false // This is a fallback, not a real config
        };
    }

    private AIProviderType GetProviderFromConfig(string? providerString)
    {
        return providerString?.ToLower() switch
        {
            "gemini" => AIProviderType.Gemini,
            "openai" => AIProviderType.OpenAI,
            "azureopenai" => AIProviderType.AzureOpenAI,
            _ => AIProviderType.Gemini
        };
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        // Truncate very long text to avoid API limits
        if (text.Length > 10000)
        {
            text = text.Substring(0, 10000);
        }

        var config = await GetActiveConfigurationAsync();
        if (config == null)
        {
            throw new InvalidOperationException("Nessuna configurazione AI attiva trovata nel database o in appsettings.json");
        }

        // Determine which provider to use for embeddings
        var provider = config.EmbeddingsProvider ?? config.ProviderType;
        
        // Log which provider is being used for embeddings
        await _logService.LogInfoAsync("Embedding", $"Using {provider} for embedding generation");

        // Try primary embedding provider
        try
        {
            return provider switch
            {
                AIProviderType.AzureOpenAI => await GenerateEmbeddingWithAzureOpenAIAsync(text, config),
                AIProviderType.OpenAI => await GenerateEmbeddingWithOpenAIAsync(text, config),
                AIProviderType.Gemini => await GenerateEmbeddingWithGeminiAsync(text, config),
                _ => throw new InvalidOperationException($"Provider non supportato: {provider}")
            };
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            await _logService.LogErrorAsync("Embedding", $"Provider di embedding primario ({provider}) fallito", ex.Message, stackTrace: ex.StackTrace);
            
            // If primary fails and fallback is enabled, try alternatives
            if (config.EnableFallback)
            {
                var errors = new List<string> { $"{provider}: {ex.Message}" };
                
                // Try alternative providers, but skip the one that just failed
                if (provider != AIProviderType.Gemini && !string.IsNullOrEmpty(config.GeminiApiKey))
                {
                    try
                    {
                        await _logService.LogInfoAsync("Embedding", "Tentativo Gemini come fallback");
                        return await GenerateEmbeddingWithGeminiAsync(text, config);
                    }
                    catch (Exception ex2)
                    {
                        await _logService.LogWarningAsync("Embedding", "Fallback Gemini fallito", ex2.Message);
                        errors.Add($"Gemini: {ex2.Message}");
                    }
                }
                
                if (provider != AIProviderType.OpenAI && !string.IsNullOrEmpty(config.OpenAIApiKey))
                {
                    try
                    {
                        await _logService.LogInfoAsync("Embedding", "Tentativo OpenAI come fallback");
                        return await GenerateEmbeddingWithOpenAIAsync(text, config);
                    }
                    catch (Exception ex3)
                    {
                        await _logService.LogWarningAsync("Embedding", "Fallback OpenAI fallito", ex3.Message);
                        errors.Add($"OpenAI: {ex3.Message}");
                    }
                }
                
                if (provider != AIProviderType.AzureOpenAI && !string.IsNullOrEmpty(config.AzureOpenAIKey))
                {
                    try
                    {
                        await _logService.LogInfoAsync("Embedding", "Tentativo AzureOpenAI come fallback");
                        return await GenerateEmbeddingWithAzureOpenAIAsync(text, config);
                    }
                    catch (Exception ex4)
                    {
                        await _logService.LogWarningAsync("Embedding", "Fallback AzureOpenAI fallito", ex4.Message);
                        errors.Add($"AzureOpenAI: {ex4.Message}");
                    }
                }
                
                // All fallback attempts failed
                throw new InvalidOperationException($"Tutti i provider di embedding sono falliti. Errori: {string.Join("; ", errors)}");
            }
            
            // Fallback is disabled, throw the original exception
            throw;
        }
    }

    private async Task<float[]?> GenerateEmbeddingWithAzureOpenAIAsync(string text, AIConfiguration config)
    {
        var endpoint = config.AzureOpenAIEndpoint ?? config.ProviderEndpoint;
        var apiKey = config.AzureOpenAIKey ?? config.ProviderApiKey;
        var deploymentName = config.EmbeddingDeploymentName ?? config.AzureOpenAIEmbeddingModel ?? "text-embedding-ada-002";

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Endpoint e API key di Azure OpenAI non configurati");
        }

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint), 
            new AzureKeyCredential(apiKey));
        
        var embeddingClient = azureClient.GetEmbeddingClient(deploymentName);
        var response = await embeddingClient.GenerateEmbeddingAsync(text);
        
        var embedding = response.Value.ToFloats().ToArray();
        
        // Log embedding info for debugging
        await _logService.LogDebugAsync("Embedding", $"[AzureOpenAI] Generated embedding: {embedding.Length} dimensions", $"First 5 values: [{string.Join(", ", embedding.Take(5).Select(v => v.ToString("F6")))}]");
        
        return embedding;
    }

    private async Task<float[]?> GenerateEmbeddingWithOpenAIAsync(string text, AIConfiguration config)
    {
        var apiKey = config.OpenAIApiKey ?? config.ProviderApiKey;
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("API key di OpenAI non configurata");
        }

        var openAIClient = new OpenAI.OpenAIClient(apiKey);
        var modelName = config.OpenAIEmbeddingModel ?? "text-embedding-ada-002";
        var embeddingClient = openAIClient.GetEmbeddingClient(modelName);
        var response = await embeddingClient.GenerateEmbeddingAsync(text);
        
        var embedding = response.Value.ToFloats().ToArray();
        
        // Log embedding info for debugging
        await _logService.LogDebugAsync("Embedding", $"[OpenAI] Generated embedding: {embedding.Length} dimensions", $"First 5 values: [{string.Join(", ", embedding.Take(5).Select(v => v.ToString("F6")))}]");
        
        return embedding;
    }

    private async Task<float[]?> GenerateEmbeddingWithGeminiAsync(string text, AIConfiguration config)
    {
        var apiKey = config.GeminiApiKey ?? config.ProviderApiKey;
        
        if (string.IsNullOrEmpty(apiKey))
        {
            await _logService.LogErrorAsync("Embedding", "API key di Gemini non configurata");
            throw new InvalidOperationException("API key di Gemini non configurata");
        }

        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                var gemini = new GoogleAI(apiKey);
                // Ensure model name has proper format - add "models/" prefix if not present
                var modelName = config.GeminiEmbeddingModel ?? "text-embedding-004";
                if (!modelName.StartsWith("models/"))
                {
                    modelName = $"models/{modelName}";
                }
                var model = gemini.GenerativeModel(model: modelName);
                
                await _logService.LogDebugAsync("Embedding", $"[Gemini] Attempting to generate embedding with model: {modelName}");
                var response = await model.EmbedContent(text);
                
                if (response?.Embedding?.Values != null)
                {
                    var embedding = response.Embedding.Values.Select(v => (float)v).ToArray();
                    
                    // Log embedding info for debugging
                    await _logService.LogDebugAsync("Embedding", $"[Gemini] Generated embedding: {embedding.Length} dimensions", $"First 5 values: [{string.Join(", ", embedding.Take(5).Select(v => v.ToString("F6")))}]");
                    
                    return embedding;
                }
                else
                {
                    await _logService.LogErrorAsync("Embedding", "La risposta di Gemini era nulla o non conteneva valori di embedding");
                    throw new InvalidOperationException("Gemini non ha restituito valori di embedding");
                }
            }
            catch (System.Net.Http.HttpRequestException ex) when (
                (ex.StatusCode.HasValue && ex.StatusCode.Value == System.Net.HttpStatusCode.NotFound) || 
                ex.Message.Contains("NotFound") || 
                ex.Message.Contains("404") ||
                ex.Message.Contains("NOT_FOUND"))
            {
                var modelName = config.GeminiEmbeddingModel ?? "text-embedding-004";
                await _logService.LogErrorAsync("Embedding", $"Modello Gemini embedding non trovato: {modelName}", ex.Message, stackTrace: ex.StackTrace);
                throw new InvalidOperationException($"Il modello Gemini embedding '{modelName}' non è stato trovato o non è supportato. Verifica che il modello sia disponibile per la tua API key e utilizza modelli supportati come 'text-embedding-004'. Errore: {ex.Message}", ex);
            }
            catch (System.Net.Http.HttpRequestException ex) when (
                (ex.StatusCode.HasValue && ex.StatusCode.Value == System.Net.HttpStatusCode.Forbidden) || 
                ex.Message.Contains("Forbidden") || 
                ex.Message.Contains("403"))
            {
                await _logService.LogErrorAsync("Embedding", "Problema con l'API key di Gemini (potrebbe essere non valida o segnalata)", ex.Message, stackTrace: ex.StackTrace);
                throw new InvalidOperationException("L'API key di Gemini non è valida o è stata segnalata come compromessa. Utilizza una chiave API diversa.", ex);
            }
            catch (System.Net.Http.HttpRequestException ex) when (
                (ex.StatusCode.HasValue && ex.StatusCode.Value == System.Net.HttpStatusCode.BadRequest) || 
                ex.Message.Contains("BadRequest") || 
                ex.Message.Contains("400") ||
                ex.Message.Contains("INVALID_ARGUMENT"))
            {
                var modelName = config.GeminiEmbeddingModel ?? "text-embedding-004";
                await _logService.LogErrorAsync("Embedding", $"Errore nel formato del modello Gemini embedding. Modello richiesto: {modelName}", ex.Message, stackTrace: ex.StackTrace);
                throw new InvalidOperationException($"Il modello Gemini embedding '{modelName}' non è valido o non è disponibile. Verifica che il nome del modello sia corretto (es: 'text-embedding-004'). Errore: {ex.Message}", ex);
            }
            catch (System.Net.Http.HttpRequestException ex) when (
                ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                // Let the retry logic handle this
                throw;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("Embedding", "Errore embedding Gemini", ex.Message, stackTrace: ex.StackTrace);
                throw; // Re-throw to allow fallback logic to work
            }
        }, "Gemini Embedding");
    }

    public async Task<string> GenerateChatCompletionAsync(string systemPrompt, string userPrompt)
    {
        var config = await GetActiveConfigurationAsync();
        if (config == null)
        {
            throw new InvalidOperationException("Nessuna configurazione AI attiva trovata");
        }

        // Determine which provider to use for chat (use ChatProvider if specified, otherwise ProviderType)
        var provider = config.ChatProvider ?? config.ProviderType;

        // Try primary provider
        try
        {
            return provider switch
            {
                AIProviderType.Gemini => await GenerateChatWithGeminiAsync(systemPrompt, userPrompt, config),
                AIProviderType.OpenAI => await GenerateChatWithOpenAIAsync(systemPrompt, userPrompt, config),
                AIProviderType.AzureOpenAI => await GenerateChatWithAzureOpenAIAsync(systemPrompt, userPrompt, config),
                _ => throw new InvalidOperationException($"Provider non supportato: {provider}")
            };
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("AI", $"Provider di chat primario ({provider}) fallito", ex.Message, stackTrace: ex.StackTrace);
            
            // If primary fails and fallback is enabled, try the alternative provider
            if (config.EnableFallback)
            {
                try
                {
                    // Try other providers as fallback
                    if (provider != AIProviderType.Gemini && !string.IsNullOrEmpty(config.GeminiApiKey))
                    {
                        await _logService.LogInfoAsync("AI", "Tentativo Gemini come fallback");
                        return await GenerateChatWithGeminiAsync(systemPrompt, userPrompt, config);
                    }
                    else if (provider != AIProviderType.OpenAI && !string.IsNullOrEmpty(config.OpenAIApiKey))
                    {
                        await _logService.LogInfoAsync("AI", "Tentativo OpenAI come fallback");
                        return await GenerateChatWithOpenAIAsync(systemPrompt, userPrompt, config);
                    }
                    else if (provider != AIProviderType.AzureOpenAI && !string.IsNullOrEmpty(config.AzureOpenAIKey))
                    {
                        await _logService.LogInfoAsync("AI", "Tentativo AzureOpenAI come fallback");
                        return await GenerateChatWithAzureOpenAIAsync(systemPrompt, userPrompt, config);
                    }
                }
                catch (Exception ex2)
                {
                    await _logService.LogErrorAsync("AI", "Provider di chat di fallback fallito", ex2.Message, stackTrace: ex2.StackTrace);
                    return $"Errore: Tutti i provider AI sono falliti. Primario: {ex.Message}, Fallback: {ex2.Message}";
                }
            }
            else
            {
                return $"Errore: {ex.Message}";
            }
        }

        throw new InvalidOperationException("Nessun provider di chat configurato.");
    }

    private async Task<string> GenerateChatWithGeminiAsync(string systemPrompt, string userPrompt, AIConfiguration config)
    {
        var apiKey = config.GeminiApiKey ?? config.ProviderApiKey;
        
        if (string.IsNullOrEmpty(apiKey))
        {
            await _logService.LogErrorAsync("AI", "API key di Gemini non configurata");
            throw new InvalidOperationException("API key di Gemini non configurata");
        }

        return await ExecuteWithRetryAsync(async () =>
        {
            try
            {
                var gemini = new GoogleAI(apiKey);
                // Ensure model name has proper format - add "models/" prefix if not present
                var modelName = config.GeminiChatModel ?? "gemini-2.0-flash-exp";
                if (!modelName.StartsWith("models/"))
                {
                    modelName = $"models/{modelName}";
                }
                var model = gemini.GenerativeModel(model: modelName);
                
                var fullPrompt = $"{systemPrompt}\n\n{userPrompt}";
                await _logService.LogDebugAsync("AI", $"[Gemini] Attempting to generate chat with model: {modelName}");
                var response = await model.GenerateContent(fullPrompt);
                
                if (response?.Text != null)
                {
                    await _logService.LogDebugAsync("AI", "Risposta di chat Gemini generata con successo");
                    return response.Text;
                }
                else
                {
                    await _logService.LogErrorAsync("AI", "La risposta di chat Gemini era nulla");
                    throw new InvalidOperationException("Nessuna risposta da Gemini");
                }
            }
            catch (System.Net.Http.HttpRequestException ex) when (
                (ex.StatusCode.HasValue && ex.StatusCode.Value == System.Net.HttpStatusCode.NotFound) || 
                ex.Message.Contains("NotFound") || 
                ex.Message.Contains("404") ||
                ex.Message.Contains("NOT_FOUND"))
            {
                var modelName = config.GeminiChatModel ?? "gemini-2.0-flash-exp";
                await _logService.LogErrorAsync("AI", $"Modello Gemini non trovato: {modelName}", ex.Message, stackTrace: ex.StackTrace);
                throw new InvalidOperationException($"Il modello Gemini '{modelName}' non è stato trovato o non è supportato. I modelli più vecchi come 'gemini-1.5-flash' potrebbero non essere più disponibili. Prova a utilizzare modelli più recenti come 'gemini-2.0-flash-exp', 'gemini-2.5-flash', o 'gemini-3-flash'. Puoi anche verificare i modelli disponibili con l'API ListModels. Errore: {ex.Message}", ex);
            }
            catch (System.Net.Http.HttpRequestException ex) when (
                (ex.StatusCode.HasValue && ex.StatusCode.Value == System.Net.HttpStatusCode.Forbidden) || 
                ex.Message.Contains("Forbidden") || 
                ex.Message.Contains("403"))
            {
                await _logService.LogErrorAsync("AI", "Problema con l'API key di Gemini (potrebbe essere non valida o segnalata)", ex.Message, stackTrace: ex.StackTrace);
                throw new InvalidOperationException("L'API key di Gemini non è valida o è stata segnalata come compromessa. Utilizza una chiave API diversa.", ex);
            }
            catch (System.Net.Http.HttpRequestException ex) when (
                (ex.StatusCode.HasValue && ex.StatusCode.Value == System.Net.HttpStatusCode.BadRequest) || 
                ex.Message.Contains("BadRequest") || 
                ex.Message.Contains("400") ||
                ex.Message.Contains("INVALID_ARGUMENT"))
            {
                var modelName = config.GeminiChatModel ?? "gemini-2.0-flash-exp";
                await _logService.LogErrorAsync("AI", $"Errore nel formato del modello Gemini. Modello richiesto: {modelName}", ex.Message, stackTrace: ex.StackTrace);
                throw new InvalidOperationException($"Il modello Gemini '{modelName}' non è valido o non è disponibile. Verifica che il nome del modello sia corretto (es: 'gemini-2.0-flash-exp', 'gemini-2.5-flash', 'gemini-3-flash'). Errore: {ex.Message}", ex);
            }
            catch (System.Net.Http.HttpRequestException ex) when (
                ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                // Let the retry logic handle this
                throw;
            }
            catch (Exception ex)
            {
                await _logService.LogErrorAsync("AI", "Errore chat Gemini", ex.Message, stackTrace: ex.StackTrace);
                throw;
            }
        }, "Gemini Chat");
    }

    private async Task<string> GenerateChatWithOpenAIAsync(string systemPrompt, string userPrompt, AIConfiguration config)
    {
        var apiKey = config.OpenAIApiKey ?? config.ProviderApiKey;
        
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("API key di OpenAI non configurata");

        var openAIClient = new OpenAI.OpenAIClient(apiKey);
        var modelName = config.OpenAIChatModel ?? "gpt-4";
        var chatClient = openAIClient.GetChatClient(modelName);
        
        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            OpenAI.Chat.ChatMessage.CreateSystemMessage(systemPrompt),
            OpenAI.Chat.ChatMessage.CreateUserMessage(userPrompt)
        };
        
        var completion = await chatClient.CompleteChatAsync(messages);
        return completion.Value.Content[0].Text;
    }

    private async Task<string> GenerateChatWithAzureOpenAIAsync(string systemPrompt, string userPrompt, AIConfiguration config)
    {
        var endpoint = config.AzureOpenAIEndpoint ?? config.ProviderEndpoint;
        var apiKey = config.AzureOpenAIKey ?? config.ProviderApiKey;
        var deploymentName = config.ChatDeploymentName ?? config.AzureOpenAIChatModel ?? "gpt-4";

        if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Endpoint e API key di Azure OpenAI non configurati");
        }

        var azureClient = new AzureOpenAIClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));

        var chatClient = azureClient.GetChatClient(deploymentName);

        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            OpenAI.Chat.ChatMessage.CreateSystemMessage(systemPrompt),
            OpenAI.Chat.ChatMessage.CreateUserMessage(userPrompt)
        };

        var completion = await chatClient.CompleteChatAsync(messages);
        return completion.Value.Content[0].Text;
    }

    public async Task<(string Category, string Reasoning, string Provider)> SuggestCategoryAsync(string fileName, string extractedText)
    {
        var provider = GetCurrentChatProvider();
        try
        {
            // Get existing categories from database
            var existingCategories = await Task.Run(() =>
                _context.Documents
                    .Where(d => !string.IsNullOrEmpty(d.ActualCategory))
                    .Select(d => d.ActualCategory)
                    .Distinct()
                    .ToList());

            var categoriesHint = existingCategories.Any()
                ? $"Existing categories in the system: {string.Join(", ", existingCategories)}. You can suggest one of these or propose a new category if none fit."
                : "This is a new system. You MUST suggest an appropriate category based on the document content.";

            var systemPrompt = @"You are a document classification expert. Your task is to ALWAYS suggest a specific, meaningful category for documents.
NEVER return 'Uncategorized' or generic categories. Analyze the content and propose a descriptive category name.
Examples of good categories: 'Financial Reports', 'Legal Contracts', 'Technical Documentation', 'Marketing Materials', 'Meeting Minutes', etc.
Always respond in Italian.";
            
            var userPrompt = $@"Analizza questo documento e suggerisci la MIGLIORE categoria possibile. Spiega anche la tua motivazione.

Nome file: {fileName}
Anteprima contenuto: {TruncateText(extractedText, 500)}

{categoriesHint}

IMPORTANTE: DEVI suggerire una categoria specifica. Se nessuna categoria esistente si adatta perfettamente, proponi un nuovo nome di categoria descrittivo basato sul contenuto del documento.
NON usare termini generici come 'Uncategorized', 'General' o 'Other'.

Rispondi in formato JSON:
{{
    ""category"": ""nome categoria specifico"",
    ""reasoning"": ""spiegazione dettagliata del motivo per cui questa categoria si adatta, menzionando parole chiave specifiche, tipo di contenuto o pattern identificati""
}}";

            var response = await GenerateChatCompletionAsync(systemPrompt, userPrompt);
            
            // Clean up response - sometimes AI adds markdown code blocks
            response = response.Trim();
            if (response.StartsWith("```json"))
            {
                response = response.Substring(7);
                if (response.EndsWith("```"))
                    response = response.Substring(0, response.Length - 3);
            }
            else if (response.StartsWith("```"))
            {
                response = response.Substring(3);
                if (response.EndsWith("```"))
                    response = response.Substring(0, response.Length - 3);
            }
            response = response.Trim();
            
            // Parse JSON response
            CategorySuggestion? result = null;
            try
            {
                result = System.Text.Json.JsonSerializer.Deserialize<CategorySuggestion>(response, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (System.Text.Json.JsonException)
            {
                // If JSON parsing fails, try to extract category from response text
                await _logService.LogWarningAsync("Category", "Failed to parse JSON response", response);
            }
            
            // Validate that we got a meaningful category
            var category = result?.Category?.Trim() ?? string.Empty;
            var reasoning = result?.Reasoning?.Trim() ?? "Nessuna motivazione fornita";
            
            // If AI still returned generic/empty category, infer from filename or content
            if (string.IsNullOrEmpty(category) || 
                category.Equals("Uncategorized", StringComparison.OrdinalIgnoreCase) ||
                category.Equals("General", StringComparison.OrdinalIgnoreCase) ||
                category.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                // Try to infer from file extension or name
                category = InferCategoryFromFileNameOrContent(fileName, extractedText);
                reasoning = $"Categoria inferita dal nome file o contenuto. {reasoning}";
            }
            
            return (category, reasoning, provider);
        }
        catch (Exception ex)
        {
            // Even on error, try to return something meaningful instead of "Uncategorized"
            var inferredCategory = InferCategoryFromFileNameOrContent(fileName, extractedText);
            return (inferredCategory, $"Errore nell'analisi AI: {ex.Message}. Categoria inferita dal nome del file.", provider);
        }
    }
    
    private string InferCategoryFromFileNameOrContent(string fileName, string extractedText)
    {
        // Try to infer category from file name
        var lowerFileName = fileName.ToLowerInvariant();
        
        // Check for common document type patterns in filename
        if (lowerFileName.Contains("contract") || lowerFileName.Contains("contratto"))
            return "Legal Contracts";
        if (lowerFileName.Contains("invoice") || lowerFileName.Contains("fattura"))
            return "Financial Documents";
        if (lowerFileName.Contains("report") || lowerFileName.Contains("rapporto"))
            return "Reports";
        if (lowerFileName.Contains("meeting") || lowerFileName.Contains("minutes") || lowerFileName.Contains("verbale"))
            return "Meeting Minutes";
        if (lowerFileName.Contains("proposal") || lowerFileName.Contains("proposta"))
            return "Proposals";
        if (lowerFileName.Contains("manual") || lowerFileName.Contains("manuale") || lowerFileName.Contains("guide"))
            return "Documentation";
        if (lowerFileName.Contains("letter") || lowerFileName.Contains("lettera"))
            return "Correspondence";
        
        // Check file extension to infer document type
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".pdf")
            return "PDF Documents";
        if (extension == ".docx" || extension == ".doc")
            return "Word Documents";
        if (extension == ".xlsx" || extension == ".xls")
            return "Spreadsheets";
        if (extension == ".txt")
            return "Text Documents";
        
        // Try to infer from content if available
        if (!string.IsNullOrEmpty(extractedText))
        {
            var lowerContent = extractedText.ToLowerInvariant();
            if (lowerContent.Contains("contract") || lowerContent.Contains("agreement"))
                return "Legal Documents";
            if (lowerContent.Contains("invoice") || lowerContent.Contains("payment"))
                return "Financial Documents";
            if (lowerContent.Contains("meeting") || lowerContent.Contains("agenda"))
                return "Meeting Documents";
        }
        
        // Last resort - use file extension based category
        if (string.IsNullOrEmpty(extension) || extension == ".")
            return "Unknown Files";
        
        return $"{extension.TrimStart('.')} Files";
    }

    public async Task<List<string>> ExtractTagsAsync(string extractedText)
    {
        try
        {
            // Note: This method uses GenerateChatCompletionAsync which automatically
            // determines the correct provider based on TagExtractionProvider configuration
            var systemPrompt = "You are a tag extraction expert. Extract 5-10 relevant keywords or tags from documents.";
            
            var userPrompt = $@"Extract 5-10 relevant tags or keywords from this document.
Tags should be short, specific, and representative of the content.

Content: {TruncateText(extractedText, 1000)}

Respond in JSON format:
{{
    ""tags"": [""tag1"", ""tag2"", ""tag3"", ...]
}}";

            var response = await GenerateChatCompletionAsync(systemPrompt, userPrompt);
            
            // Clean up response - sometimes AI adds markdown code blocks
            response = response.Trim();
            if (response.StartsWith("```json"))
            {
                response = response.Substring(7);
                if (response.EndsWith("```"))
                    response = response.Substring(0, response.Length - 3);
            }
            else if (response.StartsWith("```"))
            {
                response = response.Substring(3);
                if (response.EndsWith("```"))
                    response = response.Substring(0, response.Length - 3);
            }
            response = response.Trim();
            
            // Parse JSON response
            TagsResponse? result = null;
            try
            {
                result = System.Text.Json.JsonSerializer.Deserialize<TagsResponse>(response, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                // If JSON parsing fails, log it
                await _logService.LogWarningAsync("Tag", "Failed to parse tags JSON response", $"Response: {response}\nJSON error: {jsonEx.Message}");
            }
            
            return result?.Tags ?? new List<string>();
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("Tag", "Error extracting tags", ex.Message, stackTrace: ex.StackTrace);
            return new List<string>();
        }
    }

    public async Task<Dictionary<string, string>> ExtractMetadataAsync(string extractedText, string fileName = "")
    {
        try
        {
            var systemPrompt = "You are a metadata extraction expert. Extract structured metadata from documents.";
            
            var userPrompt = $@"Extract structured metadata from this document. Analyze the content and identify key information.

File name: {fileName}

Content: {TruncateText(extractedText, 3000)}

Extract as many relevant metadata fields as possible, for example:
- For INVOICES: invoice_number, invoice_date, invoice_year, total_amount, currency, vendor_name, customer_name, tax_id, payment_terms
- For CONTRACTS: contract_number, contract_date, contract_year, parties, expiration_date, renewal_terms, contract_value
- For GENERAL DOCUMENTS: document_type, author, creation_date, title, subject, company_name, reference_number, language
- Other relevant metadata specific to the document type

Respond in JSON format with key-value pairs.
Use English field names in snake_case (e.g., invoice_number, creation_date).
If a field is not present, DO NOT include it in the result.

Example: {{""document_type"": ""invoice"", ""invoice_number"": ""INV-2024-001"", ""invoice_date"": ""2024-01-15"", ""total_amount"": ""1000.00"", ""currency"": ""EUR""}}

Respond ONLY with valid JSON, no other comments.";

            var response = await GenerateChatCompletionAsync(systemPrompt, userPrompt);
            
            // Clean up response
            response = response.Trim();
            if (response.StartsWith("```json"))
            {
                response = response.Substring(7);
                if (response.EndsWith("```"))
                    response = response.Substring(0, response.Length - 3);
            }
            else if (response.StartsWith("```"))
            {
                response = response.Substring(3);
                if (response.EndsWith("```"))
                    response = response.Substring(0, response.Length - 3);
            }
            response = response.Trim();
            
            // Parse JSON response
            try
            {
                var metadata = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(response, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                await _logService.LogDebugAsync("Metadata", $"Extracted {metadata?.Count ?? 0} metadata fields", fileName);
                return metadata ?? new Dictionary<string, string>();
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                await _logService.LogWarningAsync("Metadata", "Failed to parse metadata JSON response", $"Response: {response}\nJSON error: {jsonEx.Message}", fileName: fileName);
                return new Dictionary<string, string>();
            }
        }
        catch (Exception ex)
        {
            await _logService.LogErrorAsync("Metadata", "Error extracting metadata", ex.Message, fileName: fileName, stackTrace: ex.StackTrace);
            return new Dictionary<string, string>();
        }
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Extracts retry delay from Gemini API error response
    /// </summary>
    private TimeSpan? ExtractRetryDelayFromError(string errorMessage)
    {
        try
        {
            // Try to parse the JSON error response
            var startIndex = errorMessage.IndexOf("{");
            if (startIndex >= 0)
            {
                var jsonPart = errorMessage.Substring(startIndex);
                using (JsonDocument doc = JsonDocument.Parse(jsonPart))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("error", out var error) &&
                        error.TryGetProperty("details", out var details) &&
                        details.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var detail in details.EnumerateArray())
                        {
                            if (detail.TryGetProperty("@type", out var type) &&
                                type.GetString()?.Contains("RetryInfo") == true &&
                                detail.TryGetProperty("retryDelay", out var retryDelay))
                            {
                                var delayStr = retryDelay.GetString();
                                if (!string.IsNullOrEmpty(delayStr) && delayStr.EndsWith("s"))
                                {
                                    // Parse delay like "23.509312193s" to seconds
                                    var secondsStr = delayStr.TrimEnd('s');
                                    if (double.TryParse(secondsStr, System.Globalization.NumberStyles.Float, 
                                        System.Globalization.CultureInfo.InvariantCulture, out var seconds))
                                    {
                                        return TimeSpan.FromSeconds(Math.Ceiling(seconds));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, return null to use default retry delay
            // Don't log here as this is called in error handling path
        }

        return null;
    }

    /// <summary>
    /// Executes an async function with retry logic for rate limit (429) errors
    /// </summary>
    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        int maxRetries = 3)
    {
        int retryCount = 0;
        TimeSpan baseDelay = TimeSpan.FromSeconds(5);
        Exception? lastException = null;

        while (retryCount <= maxRetries)
        {
            try
            {
                return await operation();
            }
            catch (System.Net.Http.HttpRequestException ex) when (
                ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests && 
                retryCount < maxRetries)
            {
                retryCount++;
                lastException = ex;
                
                // Try to extract retry delay from API response
                TimeSpan delay = ExtractRetryDelayFromError(ex.Message) 
                    ?? TimeSpan.FromSeconds(baseDelay.TotalSeconds * Math.Pow(2, retryCount - 1));

                await _logService.LogWarningAsync(
                    operationName, 
                    $"Rate limit exceeded (429). Retry {retryCount}/{maxRetries}", 
                    $"Waiting {delay.TotalSeconds:F1} seconds before retry");

                await Task.Delay(delay);
                
                await _logService.LogInfoAsync(operationName, $"Retrying after rate limit... Attempt {retryCount + 1}");
            }
            catch (Exception)
            {
                // For non-retryable errors or when retries exhausted, throw immediately
                throw;
            }
        }

        // If we've exhausted retries, throw the last exception
        throw lastException ?? new InvalidOperationException($"Operation failed after {maxRetries} retries");
    }

    private class CategorySuggestion
    {
        public string Category { get; set; } = string.Empty;
        public string Reasoning { get; set; } = string.Empty;
    }

    private class TagsResponse
    {
        public List<string> Tags { get; set; } = new List<string>();
    }

    public async Task<string> GetCurrentChatProviderAsync()
    {
        var config = await GetActiveConfigurationAsync();
        if (config == null) return "Nessuno";
        
        var provider = config.ChatProvider ?? config.ProviderType;
        return provider.ToString();
    }

    public async Task<string> GetCurrentEmbeddingProviderAsync()
    {
        var config = await GetActiveConfigurationAsync();
        if (config == null) return "Nessuno";
        
        var provider = config.EmbeddingsProvider ?? config.ProviderType;
        return provider.ToString();
    }

    // Synchronous wrappers for backward compatibility
    public string GetCurrentChatProvider()
    {
        return GetCurrentChatProviderAsync().GetAwaiter().GetResult();
    }

    public string GetCurrentEmbeddingProvider()
    {
        return GetCurrentEmbeddingProviderAsync().GetAwaiter().GetResult();
    }
}
