using Azure.AI.OpenAI;
using Azure;
using DocN.Data.Models;
using System.Text;
using OpenAI.Chat;
using System.ClientModel;

namespace DocN.Data.Services;

public interface IRAGService
{
    Task<string> GenerateResponseAsync(string query, List<Document> relevantDocuments);
}

public class RAGService : IRAGService
{
    private readonly ApplicationDbContext _context;
    private ChatClient? _client;

    public RAGService(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        InitializeClient();
    }

    private void InitializeClient()
    {
        var config = _context.AIConfigurations.FirstOrDefault(c => c.IsActive);
        if (config != null && !string.IsNullOrEmpty(config.AzureOpenAIEndpoint) && !string.IsNullOrEmpty(config.AzureOpenAIKey))
        {
            var azureClient = new AzureOpenAIClient(new Uri(config.AzureOpenAIEndpoint), new AzureKeyCredential(config.AzureOpenAIKey));
            _client = azureClient.GetChatClient(config.ChatDeploymentName ?? "gpt-4");
        }
    }

    public async Task<string> GenerateResponseAsync(string query, List<Document> relevantDocuments)
    {
        if (_client == null)
            return "AI service not configured.";

        try
        {
            var config = _context.AIConfigurations.FirstOrDefault(c => c.IsActive);
            var systemPrompt = config?.SystemPrompt ?? "You are a helpful assistant that answers questions based on provided documents.";

            // Build context from relevant documents
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Use the following documents to answer the question:");
            contextBuilder.AppendLine();

            foreach (var doc in relevantDocuments)
            {
                contextBuilder.AppendLine($"Document: {doc.FileName}");
                contextBuilder.AppendLine($"Category: {doc.ActualCategory ?? doc.SuggestedCategory}");
                contextBuilder.AppendLine($"Content: {TruncateText(doc.ExtractedText, 1000)}");
                contextBuilder.AppendLine();
            }

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(contextBuilder.ToString()),
                new UserChatMessage(query)
            };

            var response = await _client.CompleteChatAsync(messages);
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            return $"Error generating response: {ex.Message}";
        }
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }
}
