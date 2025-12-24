using Microsoft.AspNetCore.Mvc;
using DocN.Data.Services;
using DocN.Data.Models;

namespace DocN.Server.Controllers;

/// <summary>
/// API endpoints for document search functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly IHybridSearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(
        IHybridSearchService searchService,
        ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    /// <summary>
    /// Perform hybrid search combining vector similarity and full-text search
    /// </summary>
    /// <param name="request">Search request parameters</param>
    /// <returns>List of search results with relevance scores</returns>
    [HttpPost("hybrid")]
    [ProducesResponseType(typeof(SearchResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<SearchResponse>> HybridSearch([FromBody] SearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query cannot be empty" });
            }

            var startTime = DateTime.UtcNow;

            var options = new SearchOptions
            {
                TopK = request.TopK ?? 10,
                MinSimilarity = request.MinSimilarity ?? 0.7,
                CategoryFilter = request.CategoryFilter,
                OwnerId = request.UserId,
                VisibilityFilter = request.VisibilityFilter
            };

            var results = await _searchService.SearchAsync(request.Query, options);
            var elapsedTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "Hybrid search completed for query '{Query}' - Found {Count} results in {Time}ms",
                request.Query, results.Count, elapsedTime);

            return Ok(new SearchResponse
            {
                Query = request.Query,
                Results = results,
                TotalResults = results.Count,
                QueryTimeMs = elapsedTime,
                SearchType = "hybrid"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing hybrid search");
            return StatusCode(500, new { error = "An error occurred during search" });
        }
    }

    /// <summary>
    /// Perform vector-only search using semantic similarity
    /// </summary>
    [HttpPost("vector")]
    [ProducesResponseType(typeof(SearchResponse), 200)]
    public async Task<ActionResult<SearchResponse>> VectorSearch([FromBody] SearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query cannot be empty" });
            }

            var startTime = DateTime.UtcNow;

            var options = new SearchOptions
            {
                TopK = request.TopK ?? 10,
                MinSimilarity = request.MinSimilarity ?? 0.7,
                CategoryFilter = request.CategoryFilter,
                OwnerId = request.UserId
            };

            // Generate embedding first
            var embedding = await GetQueryEmbeddingAsync(request.Query);
            if (embedding == null)
            {
                return BadRequest(new { error = "Failed to generate query embedding" });
            }

            var results = await _searchService.VectorSearchAsync(embedding, options);
            var elapsedTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return Ok(new SearchResponse
            {
                Query = request.Query,
                Results = results,
                TotalResults = results.Count,
                QueryTimeMs = elapsedTime,
                SearchType = "vector"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing vector search");
            return StatusCode(500, new { error = "An error occurred during search" });
        }
    }

    /// <summary>
    /// Perform full-text search only
    /// </summary>
    [HttpPost("text")]
    [ProducesResponseType(typeof(SearchResponse), 200)]
    public async Task<ActionResult<SearchResponse>> TextSearch([FromBody] SearchRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
            {
                return BadRequest(new { error = "Query cannot be empty" });
            }

            var startTime = DateTime.UtcNow;

            var options = new SearchOptions
            {
                TopK = request.TopK ?? 10,
                CategoryFilter = request.CategoryFilter,
                OwnerId = request.UserId
            };

            var results = await _searchService.TextSearchAsync(request.Query, options);
            var elapsedTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return Ok(new SearchResponse
            {
                Query = request.Query,
                Results = results,
                TotalResults = results.Count,
                QueryTimeMs = elapsedTime,
                SearchType = "text"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing text search");
            return StatusCode(500, new { error = "An error occurred during search" });
        }
    }

    private async Task<float[]?> GetQueryEmbeddingAsync(string query)
    {
        // This is a simplified implementation
        // In production, inject IEmbeddingService and use it
        return null;
    }
}

/// <summary>
/// Request model for search operations
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// The search query text
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int? TopK { get; set; }

    /// <summary>
    /// Minimum similarity threshold (0-1)
    /// </summary>
    public double? MinSimilarity { get; set; }

    /// <summary>
    /// Filter by category
    /// </summary>
    public string? CategoryFilter { get; set; }

    /// <summary>
    /// Filter by user ID (for private documents)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Filter by visibility level
    /// </summary>
    public DocumentVisibility? VisibilityFilter { get; set; }
}

/// <summary>
/// Response model for search operations
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// The original query
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Search results with scores
    /// </summary>
    public List<SearchResult> Results { get; set; } = new();

    /// <summary>
    /// Total number of results found
    /// </summary>
    public int TotalResults { get; set; }

    /// <summary>
    /// Time taken for the query in milliseconds
    /// </summary>
    public double QueryTimeMs { get; set; }

    /// <summary>
    /// Type of search performed
    /// </summary>
    public string SearchType { get; set; } = string.Empty;
}
