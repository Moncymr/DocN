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
    Task<(string Category, string Reasoning, List<SimilarDocument> SimilarDocuments)> SuggestCategoryAsync(string fileName, string extractedText, float[]? embedding = null);
}

public class SimilarDocument
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
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
        catch (Exception)
        {
            // If primary fails and fallback is enabled, try alternatives
            if (_aiSettings.EnableFallback)
            {
                // Try Gemini as fallback
                try
                {
                    return await GenerateEmbeddingWithGeminiAsync(text);
                }
                catch
                {
                    // Try OpenAI as last resort
                    try
                    {
                        return await GenerateEmbeddingWithOpenAIAsync(text);
                    }
                    catch
                    {
                        return null;
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
            return null;

        try
        {
            var gemini = new GoogleAI(_geminiSettings.ApiKey);
            var model = gemini.GenerativeModel(model: "text-embedding-004");
            
            var response = await model.EmbedContent(text);
            
            if (response?.Embedding?.Values != null)
            {
                return response.Embedding.Values.Select(v => (float)v).ToArray();
            }
        }
        catch
        {
            // Gemini embedding not available, will fall back to other providers
        }

        return null;
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
        catch (Exception)
        {
            // If primary fails and fallback is enabled
            if (_aiSettings.EnableFallback)
            {
                try
                {
                    // Try the other provider as fallback
                    if (_aiSettings.Provider == "Gemini")
                    {
                        return await GenerateChatWithOpenAIAsync(systemPrompt, userPrompt);
                    }
                    else
                    {
                        return await GenerateChatWithGeminiAsync(systemPrompt, userPrompt);
                    }
                }
                catch (Exception ex)
                {
                    return $"Error: {ex.Message}";
                }
            }
        }

        return "AI service not configured.";
    }

    private async Task<string> GenerateChatWithGeminiAsync(string systemPrompt, string userPrompt)
    {
        if (string.IsNullOrEmpty(_geminiSettings.ApiKey))
            throw new InvalidOperationException("Gemini API key not configured");

        var gemini = new GoogleAI(_geminiSettings.ApiKey);
        var model = gemini.GenerativeModel(model: "gemini-1.5-flash");
        
        var fullPrompt = $"{systemPrompt}\n\n{userPrompt}";
        var response = await model.GenerateContent(fullPrompt);
        
        return response?.Text ?? "No response from Gemini";
    }

    private async Task<string> GenerateChatWithOpenAIAsync(string systemPrompt, string userPrompt)
    {
        if (string.IsNullOrEmpty(_openAISettings.ApiKey))
            throw new InvalidOperationException("OpenAI API key not configured");

        var openAIClient = new OpenAI.OpenAIClient(_openAISettings.ApiKey);
        var chatClient = openAIClient.GetChatClient(_openAISettings.Model);
        
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };
        
        var completion = await chatClient.CompleteChatAsync(messages);
        return completion.Value.Content[0].Text;
    }

    public async Task<(string Category, string Reasoning, List<SimilarDocument> SimilarDocuments)> SuggestCategoryAsync(string fileName, string extractedText, float[]? embedding = null)
    {
        try
        {
            var similarDocuments = new List<SimilarDocument>();
            
            // Generate embedding for the new document if not provided
            if (embedding == null)
            {
                embedding = await GenerateEmbeddingAsync(TruncateText(extractedText, 2000));
            }
            
            // Find similar documents using vector similarity if embedding is available
            if (embedding != null && embedding.Length > 0)
            {
                var documentsWithEmbeddings = await Task.Run(() =>
                    _context.Documents
                        .Where(d => d.EmbeddingVector != null && d.ActualCategory != null)
                        .Select(d => new 
                        { 
                            d.Id, 
                            d.FileName, 
                            d.ActualCategory, 
                            d.EmbeddingVector 
                        })
                        .ToList());

                foreach (var doc in documentsWithEmbeddings)
                {
                    try
                    {
                        var docEmbedding = System.Text.Json.JsonSerializer.Deserialize<float[]>(doc.EmbeddingVector!);
                        if (docEmbedding != null && docEmbedding.Length == embedding.Length)
                        {
                            var similarity = CalculateCosineSimilarity(embedding, docEmbedding);
                            
                            // Only include documents with similarity > 0.7 (highly similar)
                            if (similarity > 0.7)
                            {
                                similarDocuments.Add(new SimilarDocument
                                {
                                    Id = doc.Id,
                                    FileName = doc.FileName,
                                    Category = doc.ActualCategory!,
                                    SimilarityScore = similarity
                                });
                            }
                        }
                    }
                    catch { }
                }
                
                // Sort by similarity score descending
                similarDocuments = similarDocuments.OrderByDescending(d => d.SimilarityScore).Take(5).ToList();
            }

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

            // Add information about similar documents if found
            var similarDocsHint = "";
            if (similarDocuments.Any())
            {
                var categoryDistribution = similarDocuments
                    .GroupBy(d => d.Category)
                    .OrderByDescending(g => g.Count())
                    .ThenByDescending(g => g.Average(d => d.SimilarityScore))
                    .ToList();
                
                similarDocsHint = $@"

IMPORTANT: Vector similarity analysis found {similarDocuments.Count} highly similar documents:
{string.Join("\n", similarDocuments.Select(d => $"- {d.FileName} (Category: {d.Category}, Similarity: {d.SimilarityScore:P0})"))}

Category frequency in similar documents:
{string.Join("\n", categoryDistribution.Select(g => $"- {g.Key}: {g.Count()} documents (avg similarity: {g.Average(d => d.SimilarityScore):P0})"))}

Strongly consider using the most frequent category from similar documents, especially if similarity scores are high.";
            }

            var systemPrompt = "You are a document classification expert. Analyze documents using both content analysis AND vector similarity results to suggest appropriate categories with clear reasoning.";
            
            var userPrompt = $@"Analyze this document and suggest the best category for it. Also explain your reasoning in detail.

File name: {fileName}
Content preview: {TruncateText(extractedText, 500)}

{categoriesHint}
{similarDocsHint}

Respond in JSON format:
{{
    ""category"": ""suggested category name"",
    ""reasoning"": ""detailed explanation including: 1) What you identified from content analysis, 2) How vector similarity results influenced your decision (if applicable), 3) Why this specific category was chosen""
}}";

            var response = await GenerateChatCompletionAsync(systemPrompt, userPrompt);
            
            // Parse JSON response
            var result = System.Text.Json.JsonSerializer.Deserialize<CategorySuggestion>(response);
            return (
                result?.Category ?? "Uncategorized", 
                result?.Reasoning ?? "No reasoning provided",
                similarDocuments
            );
        }
        catch (Exception ex)
        {
            return ("Uncategorized", $"Error: {ex.Message}", new List<SimilarDocument>());
        }
    }
    
    private double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            return 0;
        
        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;
        
        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }
        
        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);
        
        if (magnitudeA == 0 || magnitudeB == 0)
            return 0;
        
        return dotProduct / (magnitudeA * magnitudeB);
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
