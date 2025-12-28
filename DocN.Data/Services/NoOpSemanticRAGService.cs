using DocN.Core.Interfaces;
using DocN.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocN.Data.Services;

/// <summary>
/// No-op implementation of ISemanticRAGService for when AI services are not configured
/// Provides similarity search capabilities using existing embeddings in the database
/// </summary>
public class NoOpSemanticRAGService : ISemanticRAGService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NoOpSemanticRAGService> _logger;

    public NoOpSemanticRAGService(
        ApplicationDbContext context,
        ILogger<NoOpSemanticRAGService> logger)
    {
        _context = context;
        _logger = logger;
    }
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

    public async Task<List<RelevantDocumentResult>> SearchDocumentsWithEmbeddingAsync(
        float[] queryEmbedding,
        string userId,
        int topK = 10,
        double minSimilarity = 0.7)
    {
        try
        {
            _logger.LogDebug("Searching documents with pre-generated embedding for user: {UserId} (NoOp mode)", userId);

            if (queryEmbedding == null || queryEmbedding.Length == 0)
            {
                _logger.LogWarning("Query embedding is null or empty");
                return new List<RelevantDocumentResult>();
            }

            // Get all documents with embeddings for the user
            var documents = await _context.Documents
                .Where(d => d.OwnerId == userId && d.EmbeddingVector != null)
                .ToListAsync();

            _logger.LogInformation("Found {Count} documents with embeddings for user {UserId}", documents.Count, userId);
            
            // Calculate similarity scores for documents
            var scoredDocs = new List<(Document doc, double score)>();
            foreach (var doc in documents)
            {
                if (doc.EmbeddingVector == null) continue;

                var similarity = CalculateCosineSimilarity(queryEmbedding, doc.EmbeddingVector);
                if (similarity >= minSimilarity)
                {
                    scoredDocs.Add((doc, similarity));
                }
            }

            _logger.LogInformation("Found {Count} documents above similarity threshold {Threshold:P0}", scoredDocs.Count, minSimilarity);

            // Get chunks for better precision
            var chunks = await _context.DocumentChunks
                .Include(c => c.Document)
                .Where(c => c.Document!.OwnerId == userId && c.ChunkEmbedding != null)
                .ToListAsync();

            var scoredChunks = new List<(DocumentChunk chunk, double score)>();
            foreach (var chunk in chunks)
            {
                if (chunk.ChunkEmbedding == null) continue;

                var similarity = CalculateCosineSimilarity(queryEmbedding, chunk.ChunkEmbedding);
                if (similarity >= minSimilarity)
                {
                    scoredChunks.Add((chunk, similarity));
                }
            }

            // Combine document-level and chunk-level results
            var results = new List<RelevantDocumentResult>();

            // Add chunk-based results (higher priority)
            foreach (var (chunk, score) in scoredChunks.OrderByDescending(x => x.score).Take(topK))
            {
                if (chunk.Document == null) continue;

                results.Add(new RelevantDocumentResult
                {
                    DocumentId = chunk.DocumentId,
                    FileName = chunk.Document.FileName,
                    Category = chunk.Document.ActualCategory,
                    SimilarityScore = score,
                    RelevantChunk = chunk.ChunkText,
                    ChunkIndex = chunk.ChunkIndex
                });
            }

            // Add document-level results if we don't have enough chunks
            if (results.Count < topK)
            {
                var remaining = topK - results.Count;
                var existingDocIds = new HashSet<int>(results.Select(r => r.DocumentId));
                
                foreach (var (doc, score) in scoredDocs.OrderByDescending(x => x.score).Take(remaining))
                {
                    // Avoid duplicates
                    if (existingDocIds.Contains(doc.Id))
                        continue;

                    results.Add(new RelevantDocumentResult
                    {
                        DocumentId = doc.Id,
                        FileName = doc.FileName,
                        Category = doc.ActualCategory,
                        SimilarityScore = score,
                        ExtractedText = doc.ExtractedText
                    });
                    existingDocIds.Add(doc.Id);
                }
            }

            _logger.LogDebug("Returning {Count} total results", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents with embedding for user: {UserId}", userId);
            return new List<RelevantDocumentResult>();
        }
    }

    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// </summary>
    private double CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            return 0;

        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }
}
