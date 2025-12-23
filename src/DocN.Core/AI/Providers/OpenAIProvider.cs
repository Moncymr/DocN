using DocN.Core.AI.Configuration;
using DocN.Core.AI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;
using System.ClientModel;
using System.Text.Json;

namespace DocN.Core.AI.Providers;

/// <summary>
/// Provider per OpenAI
/// </summary>
public class OpenAIProvider : BaseAIProvider
{
    private readonly OpenAIConfiguration _config;
    private readonly OpenAIClient _client;

    public override AIProviderType ProviderType => AIProviderType.OpenAI;
    public override string ProviderName => "OpenAI";

    public OpenAIProvider(
        IOptions<AIProviderConfiguration> configuration,
        ILogger<OpenAIProvider> logger) : base(logger)
    {
        _config = configuration.Value.OpenAI 
            ?? throw new InvalidOperationException("OpenAI configuration not found");

        if (string.IsNullOrEmpty(_config.ApiKey))
        {
            throw new InvalidOperationException("OpenAI ApiKey is required");
        }

        var options = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(_config.OrganizationId))
        {
            options.OrganizationId = _config.OrganizationId;
        }

        _client = new OpenAIClient(new ApiKeyCredential(_config.ApiKey), options);
    }

    public override async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating embedding with OpenAI");

        var embeddingClient = _client.GetEmbeddingClient(_config.EmbeddingModel);
        var response = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        
        return response.Value.ToFloats().ToArray();
    }

    public override async Task<List<CategorySuggestion>> SuggestCategoriesAsync(
        string documentText, 
        List<string> availableCategories, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Suggesting categories with OpenAI");

        var chatClient = _client.GetChatClient(_config.ChatModel);
        var prompt = BuildCategorySuggestionPrompt(documentText, availableCategories);

        var chatMessages = new List<ChatMessage>
        {
            new SystemChatMessage("Sei un assistente che analizza documenti e suggerisce categorie appropriate."),
            new UserChatMessage(prompt)
        };

        var response = await chatClient.CompleteChatAsync(chatMessages, cancellationToken: cancellationToken);
        var content = response.Value.Content[0].Text;

        return ParseCategorySuggestions(content);
    }

    private List<CategorySuggestion> ParseCategorySuggestions(string jsonResponse)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonResponse);
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
            _logger.LogError(ex, "Error parsing category suggestions");
            return new List<CategorySuggestion>();
        }
    }
}
