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

            // Performance optimization: Limit the number of candidates to evaluate
            // This prevents loading thousands of documents into memory when the user has many files
            const int MaxDocumentCandidates = 500;
            const int MaxChunkCandidates = 1000;
            
            // Get recent documents with embeddings for the user - limited to avoid performance issues
            // Query the actual mapped fields: EmbeddingVector768 or EmbeddingVector1536
            var embeddingDimension = queryEmbedding.Length;
            
            var documentsQuery = _context.Documents
                .Where(d => d.OwnerId == userId && (d.EmbeddingVector768 != null || d.EmbeddingVector1536 != null))
                .OrderByDescending(d => d.UploadedAt)
                .Take(MaxDocumentCandidates)
                .Select(d => new 
                {
                    d.Id,
                    d.FileName,
                    d.ActualCategory,
                    d.ExtractedText,
                    d.EmbeddingVector768,
                    d.EmbeddingVector1536
                });

            var documents = await documentsQuery.ToListAsync();

            _logger.LogInformation("NoOp search: Loaded {Count} document candidates (max: {Max}) for user {UserId}", 
                documents.Count, MaxDocumentCandidates, userId);
            
            // Calculate similarity scores for documents
            var scoredDocs = new List<(int id, string fileName, string? category, string? extractedText, double score)>();
            foreach (var doc in documents)
            {
                // Get the correct embedding based on dimension
                float[]? embedding = embeddingDimension == 768 ? doc.EmbeddingVector768 : doc.EmbeddingVector1536;
                if (embedding == null) continue;

                var similarity = CalculateCosineSimilarity(queryEmbedding, embedding);
                if (similarity >= minSimilarity)
                {
                    scoredDocs.Add((doc.Id, doc.FileName, doc.ActualCategory, doc.ExtractedText, similarity));
                }
            }

            _logger.LogInformation("Found {Count} documents above similarity threshold {Threshold:P0}", scoredDocs.Count, minSimilarity);

            // Get chunks for better precision - limited for performance
            // Query the actual mapped fields: ChunkEmbedding768 or ChunkEmbedding1536
            var chunksQuery = from chunk in _context.DocumentChunks
                              join doc in _context.Documents on chunk.DocumentId equals doc.Id
                              where doc.OwnerId == userId && 
                                    (chunk.ChunkEmbedding768 != null || chunk.ChunkEmbedding1536 != null)
                              orderby chunk.CreatedAt descending
                              select new
                              {
                                  chunk.Id,
                                  chunk.DocumentId,
                                  chunk.ChunkText,
                                  chunk.ChunkIndex,
                                  chunk.ChunkEmbedding768,
                                  chunk.ChunkEmbedding1536,
                                  DocumentFileName = doc.FileName,
                                  DocumentCategory = doc.ActualCategory
                              };

            var chunks = await chunksQuery.Take(MaxChunkCandidates).ToListAsync();

            _logger.LogInformation("NoOp search: Loaded {Count} chunk candidates (max: {Max}) for user {UserId}", 
                chunks.Count, MaxChunkCandidates, userId);

            var scoredChunks = new List<(int docId, string fileName, string? category, string chunkText, int chunkIndex, double score)>();
            foreach (var chunk in chunks)
            {
                // Get the correct embedding based on dimension
                float[]? embedding = embeddingDimension == 768 ? chunk.ChunkEmbedding768 : chunk.ChunkEmbedding1536;
                if (embedding == null) continue;

                var similarity = CalculateCosineSimilarity(queryEmbedding, embedding);
                if (similarity >= minSimilarity)
                {
                    scoredChunks.Add((chunk.DocumentId, chunk.DocumentFileName, chunk.DocumentCategory, 
                                     chunk.ChunkText, chunk.ChunkIndex, similarity));
                }
            }

            // Combine document-level and chunk-level results
            var results = new List<RelevantDocumentResult>();
            
            // Add chunk-based results (higher priority)
            var topChunks = scoredChunks.OrderByDescending(x => x.score).Take(topK).ToList();
            var existingDocIds = new HashSet<int>();
            
            foreach (var (docId, fileName, category, chunkText, chunkIndex, score) in topChunks)
            {
                results.Add(new RelevantDocumentResult
                {
                    DocumentId = docId,
                    FileName = fileName,
                    Category = category,
                    SimilarityScore = score,
                    RelevantChunk = chunkText,
                    ChunkIndex = chunkIndex
                });
                existingDocIds.Add(docId);
            }

            // Add document-level results if we don't have enough chunks
            if (results.Count < topK)
            {
                foreach (var (id, fileName, category, extractedText, score) in scoredDocs.OrderByDescending(x => x.score))
                {
                    // Stop if we've reached topK results
                    if (results.Count >= topK)
                        break;
                        
                    // Avoid duplicates
                    if (existingDocIds.Contains(id))
                        continue;

                    results.Add(new RelevantDocumentResult
                    {
                        DocumentId = id,
                        FileName = fileName,
                        Category = category,
                        SimilarityScore = score,
                        ExtractedText = extractedText
                    });
                    existingDocIds.Add(id);
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
        {
            _logger.LogWarning(
                "Vector dimension mismatch: vector1 has {Length1} dimensions, vector2 has {Length2} dimensions",
                vector1.Length, vector2.Length);
            return 0;
        }

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
