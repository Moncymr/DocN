using DocN.Core.Interfaces;

namespace DocN.Data.Services;

/// <summary>
/// No-op implementation of ISemanticRAGService for when AI services are not configured
/// </summary>
public class NoOpSemanticRAGService : ISemanticRAGService
{
    public Task<SemanticRAGResponse> GenerateResponseAsync(
        string query, 
        string userId, 
        int? conversationId = null, 
        List<int>? specificDocumentIds = null, 
        int topK = 5)
    {
        return Task.FromResult(new SemanticRAGResponse
        {
            Answer = "AI services are not configured. Please configure Azure OpenAI or OpenAI in appsettings.json.",
            SourceDocuments = new List<RelevantDocumentResult>(),
            Metadata = new Dictionary<string, object>
            {
                { "error", "AI services not configured" }
            }
        });
    }

    public async IAsyncEnumerable<string> GenerateStreamingResponseAsync(
        string query, 
        string userId, 
        int? conversationId = null, 
        List<int>? specificDocumentIds = null)
    {
        yield return "AI services are not configured. Please configure Azure OpenAI or OpenAI in appsettings.json.";
        await Task.CompletedTask;
    }

    public Task<List<RelevantDocumentResult>> SearchDocumentsAsync(
        string query, 
        string userId, 
        int topK = 10, 
        double minSimilarity = 0.7)
    {
        // Return empty list when AI services are not configured
        return Task.FromResult(new List<RelevantDocumentResult>());
    }

    public Task<List<RelevantDocumentResult>> SearchDocumentsWithEmbeddingAsync(
        float[] queryEmbedding,
        string userId,
        int topK = 10,
        double minSimilarity = 0.7)
    {
        // Return empty list when AI services are not configured
        return Task.FromResult(new List<RelevantDocumentResult>());
    }
}
