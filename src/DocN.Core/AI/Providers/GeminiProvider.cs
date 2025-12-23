using DocN.Core.AI.Configuration;
using DocN.Core.AI.Models;
using Mscc.GenerativeAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DocN.Core.AI.Providers;

/// <summary>
/// Provider per Google Gemini
/// </summary>
public class GeminiProvider : BaseAIProvider
{
    private readonly GeminiConfiguration _config;
    private readonly GoogleAI _client;

    public override AIProviderType ProviderType => AIProviderType.Gemini;
    public override string ProviderName => "Google Gemini";

    public GeminiProvider(
        IOptions<AIProviderConfiguration> configuration,
        ILogger<GeminiProvider> logger) : base(logger)
    {
        _config = configuration.Value.Gemini 
            ?? throw new InvalidOperationException("Gemini configuration not found");

        if (string.IsNullOrEmpty(_config.ApiKey))
        {
            throw new InvalidOperationException("Gemini ApiKey is required");
        }

        _client = new GoogleAI(_config.ApiKey);
    }

    public override async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating embedding with Gemini");

        try
        {
            var model = _client.GenerativeModel(model: _config.EmbeddingModel);
            var response = await model.EmbedContent(text);
            
            if (response?.Embedding?.Values != null)
            {
                return response.Embedding.Values.ToArray();
            }

            throw new InvalidOperationException("Failed to generate embedding with Gemini");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding with Gemini");
            throw;
        }
    }

    public override async Task<List<CategorySuggestion>> SuggestCategoriesAsync(
        string documentText, 
        List<string> availableCategories, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Suggesting categories with Gemini");

        var model = _client.GenerativeModel(model: _config.GenerationModel);
        var prompt = BuildCategorySuggestionPrompt(documentText, availableCategories);

        var response = await model.GenerateContent(prompt);
        
        if (response?.Text != null)
        {
            return ParseCategorySuggestions(response.Text);
        }

        return new List<CategorySuggestion>();
    }

    private List<CategorySuggestion> ParseCategorySuggestions(string jsonResponse)
    {
        try
        {
            // Gemini a volte include ```json markers, rimuoviamoli
            var cleanedResponse = jsonResponse.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Substring(7);
            }
            if (cleanedResponse.StartsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(3);
            }
            if (cleanedResponse.EndsWith("```"))
            {
                cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
            }
            cleanedResponse = cleanedResponse.Trim();

            var jsonDoc = JsonDocument.Parse(cleanedResponse);
            var suggestions = new List<CategorySuggestion>();

            if (jsonDoc.RootElement.TryGetProperty("suggestions", out var suggestionsArray))
            {
                foreach (var item in suggestionsArray.EnumerateArray())
                {
                    suggestions.Add(new CategorySuggestion
                    {
                        CategoryName = item.GetProperty("categoryName").GetString() ?? string.Empty,
                        Confidence = item.GetProperty("confidence").GetDouble(),
                        Reasoning = item.GetProperty("reasoning").GetString() ?? string.Empty
                    });
                }
            }

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing category suggestions from Gemini");
            return new List<CategorySuggestion>();
        }
    }
}
