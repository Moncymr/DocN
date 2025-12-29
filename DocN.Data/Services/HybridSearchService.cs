using DocN.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DocN.Data.Services;

/// <summary>
/// Options for hybrid search
/// </summary>
public class SearchOptions
{
    public int TopK { get; set; } = 10;
    public double MinSimilarity { get; set; } = 0.7;
    public string? CategoryFilter { get; set; }
    public string? OwnerId { get; set; }
    public DocumentVisibility? VisibilityFilter { get; set; }
}

/// <summary>
/// Search result with relevance scores
/// </summary>
public class SearchResult
{
    public Document Document { get; set; } = null!;
    public double VectorScore { get; set; }
    public double TextScore { get; set; }
    public double CombinedScore { get; set; }
    public int? VectorRank { get; set; }
    public int? TextRank { get; set; }
}

/// <summary>
/// Service for hybrid search combining vector similarity and full-text search
/// </summary>
public interface IHybridSearchService
{
    /// <summary>
    /// Perform hybrid search combining vector and full-text search with Reciprocal Rank Fusion
    /// </summary>
    Task<List<SearchResult>> SearchAsync(string query, SearchOptions options);

    /// <summary>
    /// Perform vector-only search
    /// </summary>
    Task<List<SearchResult>> VectorSearchAsync(float[] queryEmbedding, SearchOptions options);

    /// <summary>
    /// Perform full-text search only
    /// </summary>
    Task<List<SearchResult>> TextSearchAsync(string query, SearchOptions options);
}

public class HybridSearchService : IHybridSearchService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmbeddingService _embeddingService;

    public HybridSearchService(ApplicationDbContext context, IEmbeddingService embeddingService)
    {
        _context = context;
        _embeddingService = embeddingService;
    }

    /// <summary>
    /// Perform hybrid search combining vector similarity and text search
    /// </summary>
    public async Task<List<SearchResult>> SearchAsync(string query, SearchOptions options)
    {
        // 1. Generate query embedding
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
        if (queryEmbedding == null)
        {
            // Fallback to text search only if embedding generation fails
            return await TextSearchAsync(query, options);
        }

        // 2. Perform vector search
        var vectorResults = await VectorSearchAsync(queryEmbedding, options);

        // 3. Perform full-text search
        var textResults = await TextSearchAsync(query, options);

        // 4. Merge results using Reciprocal Rank Fusion (RRF)
        var merged = MergeWithRRF(vectorResults, textResults, options.TopK);

        return merged;
    }

    /// <summary>
    /// Perform vector similarity search with database optimization
    /// </summary>
    public async Task<List<SearchResult>> VectorSearchAsync(float[] queryEmbedding, SearchOptions options)
    {
        // Check if using SQL Server for optimization
        var isSqlServer = _context.Database.IsSqlServer();
        
        // Build query with filters
        var documentsQuery = _context.Documents
            .Where(d => d.EmbeddingVector != null);

        // Apply filters
        if (!string.IsNullOrEmpty(options.CategoryFilter))
        {
            documentsQuery = documentsQuery.Where(d => d.ActualCategory == options.CategoryFilter);
        }

        if (!string.IsNullOrEmpty(options.OwnerId))
        {
            documentsQuery = documentsQuery.Where(d => d.OwnerId == options.OwnerId);
        }

        if (options.VisibilityFilter.HasValue)
        {
            documentsQuery = documentsQuery.Where(d => d.Visibility == options.VisibilityFilter.Value);
        }

        // Optimize: Limit candidates before loading into memory
        var candidateLimit = isSqlServer ? Math.Max(options.TopK * 10, 100) : int.MaxValue;
        documentsQuery = documentsQuery.OrderByDescending(d => d.UploadedAt).Take(candidateLimit);

        var documents = await documentsQuery.ToListAsync();

        // Calculate cosine similarity for each document
        var results = new List<SearchResult>();
        foreach (var doc in documents)
        {
            if (doc.EmbeddingVector == null) continue;

            var similarity = CosineSimilarity(queryEmbedding, doc.EmbeddingVector);
            
            if (similarity >= options.MinSimilarity)
            {
                results.Add(new SearchResult
                {
                    Document = doc,
                    VectorScore = similarity,
                    TextScore = 0,
                    CombinedScore = similarity
                });
            }
        }

        // Sort by similarity and add ranks
        results = results.OrderByDescending(r => r.VectorScore).ToList();
        for (int i = 0; i < results.Count; i++)
        {
            results[i].VectorRank = i + 1;
        }

        return results.Take(options.TopK * 2).ToList(); // Return 2x TopK for fusion
    }

    /// <summary>
    /// Perform full-text search
    /// </summary>
    public async Task<List<SearchResult>> TextSearchAsync(string query, SearchOptions options)
    {
        // Simple text search using LINQ (in production, use SQL Server Full-Text Search)
        var keywords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var documentsQuery = _context.Documents.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(options.CategoryFilter))
        {
            documentsQuery = documentsQuery.Where(d => d.ActualCategory == options.CategoryFilter);
        }

        if (!string.IsNullOrEmpty(options.OwnerId))
        {
            documentsQuery = documentsQuery.Where(d => d.OwnerId == options.OwnerId);
        }

        if (options.VisibilityFilter.HasValue)
        {
            documentsQuery = documentsQuery.Where(d => d.Visibility == options.VisibilityFilter.Value);
        }

        // Search in filename and extracted text
        var documents = await documentsQuery.ToListAsync();

        var results = new List<SearchResult>();
        foreach (var doc in documents)
        {
            var text = $"{doc.FileName} {doc.ExtractedText}".ToLower();
            var matchCount = keywords.Count(k => text.Contains(k));
            
            if (matchCount > 0)
            {
                // Simple scoring: ratio of matching keywords
                var score = (double)matchCount / keywords.Length;
                
                results.Add(new SearchResult
                {
                    Document = doc,
                    VectorScore = 0,
                    TextScore = score,
                    CombinedScore = score
                });
            }
        }

        // Sort by score and add ranks
        results = results.OrderByDescending(r => r.TextScore).ToList();
        for (int i = 0; i < results.Count; i++)
        {
            results[i].TextRank = i + 1;
        }

        return results.Take(options.TopK * 2).ToList(); // Return 2x TopK for fusion
    }

    /// <summary>
    /// Merge results using Reciprocal Rank Fusion (RRF)
    /// RRF formula: score = sum(1 / (k + rank)) for each ranking
    /// </summary>
    private List<SearchResult> MergeWithRRF(
        List<SearchResult> vectorResults,
        List<SearchResult> textResults,
        int topK,
        int k = 60)
    {
        var mergedScores = new Dictionary<int, SearchResult>();

        // Process vector results
        foreach (var result in vectorResults)
        {
            var docId = result.Document.Id;
            if (!mergedScores.ContainsKey(docId))
            {
                mergedScores[docId] = new SearchResult
                {
                    Document = result.Document,
                    VectorScore = result.VectorScore,
                    VectorRank = result.VectorRank,
                    TextScore = 0,
                    CombinedScore = 0
                };
            }
            
            // Add RRF score from vector ranking
            if (result.VectorRank.HasValue)
            {
                mergedScores[docId].CombinedScore += 1.0 / (k + result.VectorRank.Value);
            }
        }

        // Process text results
        foreach (var result in textResults)
        {
            var docId = result.Document.Id;
            if (!mergedScores.ContainsKey(docId))
            {
                mergedScores[docId] = new SearchResult
                {
                    Document = result.Document,
                    VectorScore = 0,
                    TextScore = result.TextScore,
                    TextRank = result.TextRank,
                    CombinedScore = 0
                };
            }
            else
            {
                mergedScores[docId].TextScore = result.TextScore;
                mergedScores[docId].TextRank = result.TextRank;
            }
            
            // Add RRF score from text ranking
            if (result.TextRank.HasValue)
            {
                mergedScores[docId].CombinedScore += 1.0 / (k + result.TextRank.Value);
            }
        }

        // Sort by combined RRF score and return top K
        return mergedScores.Values
            .OrderByDescending(r => r.CombinedScore)
            .Take(topK)
            .ToList();
    }

    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// </summary>
    private double CosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            throw new ArgumentException("Vectors must have the same length");

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
