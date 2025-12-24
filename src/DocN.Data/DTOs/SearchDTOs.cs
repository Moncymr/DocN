namespace DocN.Data.DTOs;

/// <summary>
/// Response for semantic/hybrid search
/// </summary>
public class SearchResult
{
    public int DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public float SimilarityScore { get; set; }
    public string? Snippet { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
}

/// <summary>
/// Request for search operations
/// </summary>
public class SearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string? CategoryFilter { get; set; }
    public int? DepartmentId { get; set; }
    public int TopK { get; set; } = 10;
    public bool HybridSearch { get; set; } = true;
    public float MinSimilarity { get; set; } = 0.7f;
}

/// <summary>
/// Search response with results and metadata
/// </summary>
public class SearchResponse
{
    public List<SearchResult> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public long QueryTimeMs { get; set; }
    public string SearchMode { get; set; } = string.Empty; // "vector", "text", "hybrid"
}
