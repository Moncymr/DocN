using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;
using Azure.AI.OpenAI;
using Azure;
using DocN.Data.Models;
using OpenAI.Embeddings;
using OpenAI.Chat;

namespace DocN.Data.Services;

public interface IMultiProviderAIService
{
    Task<float[]?> GenerateEmbeddingAsync(string text);
    Task<string> GenerateChatCompletionAsync(string systemPrompt, string userPrompt);
    Task<(string Category, string Reasoning)> SuggestCategoryAsync(string fileName, string extractedText);
}

public class MultiProviderAIService : IMultiProviderAIService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly AISettings _aiSettings;
    private readonly EmbeddingsSettings _embeddingsSettings;
    private readonly GeminiSettings _geminiSettings;
    private readonly OpenAISettings _openAISettings;

    public MultiProviderAIService(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
        
        _aiSettings = new AISettings();
        _configuration.GetSection("AI").Bind(_aiSettings);
        
        _embeddingsSettings = new EmbeddingsSettings();
        _configuration.GetSection("Embeddings").Bind(_embeddingsSettings);
        
        _geminiSettings = new GeminiSettings();
        _configuration.GetSection("Gemini").Bind(_geminiSettings);
        
        _openAISettings = new OpenAISettings();
        _configuration.GetSection("OpenAI").Bind(_openAISettings);
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        // Truncate very long text to avoid API limits
        if (text.Length > 10000)
        {
            text = text.Substring(0, 10000);
        }

        // Try primary embedding provider
        try
        {
            if (_embeddingsSettings.Provider == "AzureOpenAI")
            {
                return await GenerateEmbeddingWithAzureOpenAIAsync(text);
            }
            else if (_embeddingsSettings.Provider == "OpenAI")
            {
                return await GenerateEmbeddingWithOpenAIAsync(text);
            }
            else if (_embeddingsSettings.Provider == "Gemini")
            {
                return await GenerateEmbeddingWithGeminiAsync(text);
            }
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"Primary embedding provider ({_embeddingsSettings.Provider}) failed: {ex.Message}");
            
            // If primary fails and fallback is enabled, try alternatives
            if (_aiSettings.EnableFallback)
            {
                // Try alternative providers, but skip the one that just failed
                if (_embeddingsSettings.Provider != "Gemini")
                {
                    try
                    {
                        Console.WriteLine("Trying Gemini as fallback...");
                        return await GenerateEmbeddingWithGeminiAsync(text);
                    }
                    catch (Exception ex2)
                    {
                        Console.WriteLine($"Gemini fallback failed: {ex2.Message}");
                    }
                }
                
                if (_embeddingsSettings.Provider != "OpenAI")
                {
                    try
                    {
                        Console.WriteLine("Trying OpenAI as fallback...");
                        return await GenerateEmbeddingWithOpenAIAsync(text);
                    }
                    catch (Exception ex3)
                    {
                        Console.WriteLine($"OpenAI fallback failed: {ex3.Message}");
                    }
                }
                
                if (_embeddingsSettings.Provider != "AzureOpenAI")
                {
                    try
                    {
                        Console.WriteLine("Trying AzureOpenAI as fallback...");
                        return await GenerateEmbeddingWithAzureOpenAIAsync(text);
                    }
                    catch (Exception ex4)
                    {
                        Console.WriteLine($"AzureOpenAI fallback failed: {ex4.Message}");
                    }
                }
            }
        }

        return null;
    }

    private async Task<float[]?> GenerateEmbeddingWithAzureOpenAIAsync(string text)
    {
        if (string.IsNullOrEmpty(_embeddingsSettings.Endpoint) || string.IsNullOrEmpty(_embeddingsSettings.ApiKey))
            return null;

        var azureClient = new AzureOpenAIClient(
            new Uri(_embeddingsSettings.Endpoint), 
            new AzureKeyCredential(_embeddingsSettings.ApiKey));
        
        var embeddingClient = azureClient.GetEmbeddingClient(_embeddingsSettings.DeploymentName);
        var response = await embeddingClient.GenerateEmbeddingAsync(text);
        
        return response.Value.ToFloats().ToArray();
    }

    private async Task<float[]?> GenerateEmbeddingWithOpenAIAsync(string text)
    {
        if (string.IsNullOrEmpty(_openAISettings.ApiKey))
            return null;

        var openAIClient = new OpenAI.OpenAIClient(_openAISettings.ApiKey);
        var embeddingClient = openAIClient.GetEmbeddingClient("text-embedding-ada-002");
        var response = await embeddingClient.GenerateEmbeddingAsync(text);
        
        return response.Value.ToFloats().ToArray();
    }

    private async Task<float[]?> GenerateEmbeddingWithGeminiAsync(string text)
    {
        if (string.IsNullOrEmpty(_geminiSettings.ApiKey))
        {
            Console.WriteLine("Gemini API key is not configured");
            throw new InvalidOperationException("Gemini API key not configured");
        }

        try
        {
            var gemini = new GoogleAI(_geminiSettings.ApiKey);
            var model = gemini.GenerativeModel(model: "text-embedding-004");
            
            var response = await model.EmbedContent(text);
            
            if (response?.Embedding?.Values != null)
            {
                var embedding = response.Embedding.Values.Select(v => (float)v).ToArray();
                Console.WriteLine($"Gemini embedding generated successfully: {embedding.Length} dimensions");
                return embedding;
            }
            else
            {
                Console.WriteLine("Gemini response was null or had no embedding values");
                throw new InvalidOperationException("Gemini returned no embedding values");
            }
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("Forbidden") || ex.Message.Contains("403"))
        {
            Console.WriteLine($"Gemini API key issue (possibly leaked or invalid): {ex.Message}");
            throw new InvalidOperationException("Gemini API key is invalid or has been reported as leaked. Please use a different API key.", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gemini embedding error: {ex.Message}");
            throw; // Re-throw to allow fallback logic to work
        }
    }

    public async Task<string> GenerateChatCompletionAsync(string systemPrompt, string userPrompt)
    {
        // Try primary provider (Gemini or OpenAI)
        try
        {
            if (_aiSettings.Provider == "Gemini")
            {
                return await GenerateChatWithGeminiAsync(systemPrompt, userPrompt);
            }
            else if (_aiSettings.Provider == "OpenAI")
            {
                return await GenerateChatWithOpenAIAsync(systemPrompt, userPrompt);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Primary chat provider ({_aiSettings.Provider}) failed: {ex.Message}");
            
            // If primary fails and fallback is enabled, try the alternative provider
            if (_aiSettings.EnableFallback)
            {
                try
                {
                    // Try the other provider as fallback (not the one that just failed)
                    if (_aiSettings.Provider == "Gemini")
                    {
                        Console.WriteLine("Trying OpenAI as fallback...");
                        return await GenerateChatWithOpenAIAsync(systemPrompt, userPrompt);
                    }
                    else if (_aiSettings.Provider == "OpenAI")
                    {
                        Console.WriteLine("Trying Gemini as fallback...");
                        return await GenerateChatWithGeminiAsync(systemPrompt, userPrompt);
                    }
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"Fallback chat provider failed: {ex2.Message}");
                    return $"Error: All AI providers failed. Primary: {ex.Message}, Fallback: {ex2.Message}";
                }
            }
            else
            {
                return $"Error: {ex.Message}";
            }
        }

        return "AI service not configured.";
    }

    private async Task<string> GenerateChatWithGeminiAsync(string systemPrompt, string userPrompt)
    {
        if (string.IsNullOrEmpty(_geminiSettings.ApiKey))
        {
            Console.WriteLine("Gemini API key is not configured");
            throw new InvalidOperationException("Gemini API key not configured");
        }

        try
        {
            var gemini = new GoogleAI(_geminiSettings.ApiKey);
            var model = gemini.GenerativeModel(model: "gemini-1.5-flash");
            
            var fullPrompt = $"{systemPrompt}\n\n{userPrompt}";
            var response = await model.GenerateContent(fullPrompt);
            
            if (response?.Text != null)
            {
                Console.WriteLine($"Gemini chat response generated successfully");
                return response.Text;
            }
            else
            {
                Console.WriteLine("Gemini chat response was null");
                throw new InvalidOperationException("No response from Gemini");
            }
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.Message.Contains("Forbidden") || ex.Message.Contains("403"))
        {
            Console.WriteLine($"Gemini API key issue (possibly leaked or invalid): {ex.Message}");
            throw new InvalidOperationException("Gemini API key is invalid or has been reported as leaked. Please use a different API key.", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Gemini chat error: {ex.Message}");
            throw;
        }
    }

    private async Task<string> GenerateChatWithOpenAIAsync(string systemPrompt, string userPrompt)
    {
        if (string.IsNullOrEmpty(_openAISettings.ApiKey))
            throw new InvalidOperationException("OpenAI API key not configured");

        var openAIClient = new OpenAI.OpenAIClient(_openAISettings.ApiKey);
        var chatClient = openAIClient.GetChatClient(_openAISettings.Model);
        
        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            OpenAI.Chat.ChatMessage.CreateSystemMessage(systemPrompt),
            OpenAI.Chat.ChatMessage.CreateUserMessage(userPrompt)
        };
        
        var completion = await chatClient.CompleteChatAsync(messages);
        return completion.Value.Content[0].Text;
    }

    public async Task<(string Category, string Reasoning)> SuggestCategoryAsync(string fileName, string extractedText)
    {
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
                : "This is a new system, suggest an appropriate category.";

            var systemPrompt = "You are a document classification expert. Analyze documents and suggest appropriate categories with clear reasoning.";
            
            var userPrompt = $@"Analyze this document and suggest the best category for it. Also explain your reasoning.

File name: {fileName}
Content preview: {TruncateText(extractedText, 500)}

{categoriesHint}

Respond in JSON format:
{{
    ""category"": ""suggested category name"",
    ""reasoning"": ""detailed explanation of why this category fits, mentioning specific keywords, content type, or patterns you identified""
}}";

            var response = await GenerateChatCompletionAsync(systemPrompt, userPrompt);
            
            // Parse JSON response
            var result = System.Text.Json.JsonSerializer.Deserialize<CategorySuggestion>(response);
            return (result?.Category ?? "Uncategorized", result?.Reasoning ?? "No reasoning provided");
        }
        catch (Exception ex)
        {
            return ("Uncategorized", $"Error: {ex.Message}");
        }
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }

    private class CategorySuggestion
    {
        public string Category { get; set; } = string.Empty;
        public string Reasoning { get; set; } = string.Empty;
    }
}
