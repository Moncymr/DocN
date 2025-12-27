using Microsoft.Extensions.Configuration;
using Mscc.GenerativeAI;
using Azure.AI.OpenAI;
using Azure;
using DocN.Data.Models;
using OpenAI.Embeddings;
using OpenAI.Chat;
using System.IO;

namespace DocN.Data.Services;

public interface IMultiProviderAIService
{
    Task<float[]?> GenerateEmbeddingAsync(string text);
    Task<string> GenerateChatCompletionAsync(string systemPrompt, string userPrompt);
    Task<(string Category, string Reasoning)> SuggestCategoryAsync(string fileName, string extractedText);
    Task<List<string>> ExtractTagsAsync(string extractedText);
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
        
        // Log configuration for debugging
        Console.WriteLine("=== MultiProviderAIService Configuration ===");
        Console.WriteLine($"AI Provider: {_aiSettings.Provider}");
        Console.WriteLine($"AI EnableFallback: {_aiSettings.EnableFallback}");
        Console.WriteLine($"Gemini ApiKey: {(string.IsNullOrEmpty(_geminiSettings.ApiKey) ? "NOT SET" : $"SET ({_geminiSettings.ApiKey.Length} chars)")}");
        Console.WriteLine($"Embeddings Provider: {_embeddingsSettings.Provider}");
        Console.WriteLine($"Embeddings ApiKey: {(string.IsNullOrEmpty(_embeddingsSettings.ApiKey) ? "NOT SET" : $"SET ({_embeddingsSettings.ApiKey.Length} chars)")}");
        Console.WriteLine($"Embeddings Model: {_embeddingsSettings.Model}");
        Console.WriteLine("==========================================");
    }

    public async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        Console.WriteLine($"GenerateEmbeddingAsync called. Provider: {_embeddingsSettings.Provider}, Text length: {text?.Length ?? 0}");
        
        // Handle null or empty text
        if (string.IsNullOrEmpty(text))
        {
            Console.WriteLine("Text is null or empty, cannot generate embedding");
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        }
        
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
                Console.WriteLine("Using AzureOpenAI for embeddings");
                return await GenerateEmbeddingWithAzureOpenAIAsync(text);
            }
            else if (_embeddingsSettings.Provider == "OpenAI")
            {
                Console.WriteLine("Using OpenAI for embeddings");
                return await GenerateEmbeddingWithOpenAIAsync(text);
            }
            else if (_embeddingsSettings.Provider == "Gemini")
            {
                Console.WriteLine("Using Gemini for embeddings");
                return await GenerateEmbeddingWithGeminiAsync(text);
            }
            else
            {
                Console.WriteLine($"Unknown embeddings provider: {_embeddingsSettings.Provider}");
                throw new InvalidOperationException($"Unknown embeddings provider: {_embeddingsSettings.Provider}");
            }
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            Console.WriteLine($"Primary embedding provider ({_embeddingsSettings.Provider}) failed: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // If primary fails and fallback is enabled, try alternatives
            if (_aiSettings.EnableFallback)
            {
                var errors = new List<string> { $"{_embeddingsSettings.Provider}: {ex.Message}" };
                
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
                        errors.Add($"Gemini: {ex2.Message}");
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
                        errors.Add($"OpenAI: {ex3.Message}");
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
                        errors.Add($"AzureOpenAI: {ex4.Message}");
                    }
                }
                
                // All fallback attempts failed
                Console.WriteLine($"All embedding providers failed. Errors: {string.Join("; ", errors)}");
                throw new InvalidOperationException($"All embedding providers failed. Errors: {string.Join("; ", errors)}");
            }
            
            // Fallback is disabled, throw the original exception
            throw;
        }
    }

    private async Task<float[]?> GenerateEmbeddingWithAzureOpenAIAsync(string text)
    {
        if (string.IsNullOrEmpty(_embeddingsSettings.Endpoint) || string.IsNullOrEmpty(_embeddingsSettings.ApiKey))
        {
            Console.WriteLine($"AzureOpenAI embeddings not configured. Endpoint: {(string.IsNullOrEmpty(_embeddingsSettings.Endpoint) ? "MISSING" : "SET")}, ApiKey: {(string.IsNullOrEmpty(_embeddingsSettings.ApiKey) ? "MISSING" : "SET")}");
            throw new InvalidOperationException("AzureOpenAI embeddings not configured (missing Endpoint or ApiKey)");
        }

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
        {
            Console.WriteLine("OpenAI API key is not configured");
            throw new InvalidOperationException("OpenAI API key not configured");
        }

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
        catch (System.Net.Http.HttpRequestException ex) when (
            (ex.StatusCode.HasValue && ex.StatusCode.Value == System.Net.HttpStatusCode.Forbidden) || 
            ex.Message.Contains("Forbidden") || 
            ex.Message.Contains("403"))
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

        throw new InvalidOperationException("No chat provider configured.");
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
        catch (System.Net.Http.HttpRequestException ex) when (
            (ex.StatusCode.HasValue && ex.StatusCode.Value == System.Net.HttpStatusCode.Forbidden) || 
            ex.Message.Contains("Forbidden") || 
            ex.Message.Contains("403"))
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
                : "This is a new system. You MUST suggest an appropriate category based on the document content.";

            var systemPrompt = @"You are a document classification expert. Your task is to ALWAYS suggest a specific, meaningful category for documents.
NEVER return 'Uncategorized' or generic categories. Analyze the content and propose a descriptive category name.
Examples of good categories: 'Financial Reports', 'Legal Contracts', 'Technical Documentation', 'Marketing Materials', 'Meeting Minutes', etc.";
            
            var userPrompt = $@"Analyze this document and suggest the BEST POSSIBLE category for it. Also explain your reasoning.

File name: {fileName}
Content preview: {TruncateText(extractedText, 500)}

{categoriesHint}

IMPORTANT: You MUST suggest a specific category. If no existing category fits perfectly, propose a new descriptive category name based on the document content.
Do NOT use generic terms like 'Uncategorized', 'General', or 'Other'.

Respond in JSON format:
{{
    ""category"": ""specific category name"",
    ""reasoning"": ""detailed explanation of why this category fits, mentioning specific keywords, content type, or patterns you identified""
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
                Console.WriteLine($"Failed to parse JSON response: {response}");
            }
            
            // Validate that we got a meaningful category
            var category = result?.Category?.Trim() ?? string.Empty;
            var reasoning = result?.Reasoning?.Trim() ?? "No reasoning provided";
            
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
            
            return (category, reasoning);
        }
        catch (Exception ex)
        {
            // Even on error, try to return something meaningful instead of "Uncategorized"
            var inferredCategory = InferCategoryFromFileNameOrContent(fileName, extractedText);
            return (inferredCategory, $"Errore nell'analisi AI: {ex.Message}. Categoria inferita dal nome del file.");
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
                Console.WriteLine($"Failed to parse tags JSON response: {response}");
                Console.WriteLine($"JSON error: {jsonEx.Message}");
            }
            
            return result?.Tags ?? new List<string>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting tags: {ex.Message}");
            return new List<string>();
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

    private class TagsResponse
    {
        public List<string> Tags { get; set; } = new List<string>();
    }
}
