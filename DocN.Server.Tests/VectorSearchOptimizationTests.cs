using DocN.Data;
using DocN.Data.Models;
using DocN.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using IEmbeddingService = DocN.Data.Services.IEmbeddingService;

namespace DocN.Server.Tests;

/// <summary>
/// Tests for vector search optimization to ensure database-level filtering is working
/// </summary>
public class VectorSearchOptimizationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<HybridSearchService>> _mockLogger;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly HybridSearchService _service;

    public VectorSearchOptimizationTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<HybridSearchService>>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _service = new HybridSearchService(_context, _mockEmbeddingService.Object);
    }

    [Fact]
    public async Task VectorSearchAsync_LimitsResultsWithSqlServer_WhenUsingInMemoryDatabase()
    {
        // Arrange: Create test documents with embeddings
        var userId = "test-user";
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        
        // Add 150 documents to test candidate limiting
        for (int i = 0; i < 150; i++)
        {
            _context.Documents.Add(new Document
            {
                Id = i + 1,
                FileName = $"Document_{i}.pdf",
                FilePath = $"/path/to/doc_{i}.pdf",
                ContentType = "application/pdf",
                OwnerId = userId,
                EmbeddingVector = embedding,
                UploadedAt = DateTime.UtcNow.AddDays(-i) // Older documents first
            });
        }
        await _context.SaveChangesAsync();

        // Act: Search with small topK
        var queryEmbedding = new float[] { 0.15f, 0.25f, 0.35f };
        var options = new SearchOptions
        {
            TopK = 5,
            MinSimilarity = 0.5,
            OwnerId = userId
        };
        
        var results = await _service.VectorSearchAsync(queryEmbedding, options);

        // Assert: Should return results limited by topK * 2 (10 results)
        Assert.NotNull(results);
        Assert.True(results.Count <= options.TopK * 2, "Results should be limited by topK * 2");
        
        // Since we're using in-memory database, it should process all documents
        // but still return limited results
        Assert.True(results.Count > 0, "Should have at least some results");
    }

    [Fact]
    public async Task VectorSearchAsync_FiltersDocumentsByOwner()
    {
        // Arrange: Create documents for different users
        var user1 = "user1";
        var user2 = "user2";
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        
        _context.Documents.Add(new Document
        {
            FileName = "User1_Doc.pdf",
            FilePath = "/path/user1.pdf",
            ContentType = "application/pdf",
            OwnerId = user1,
            EmbeddingVector = embedding
        });
        
        _context.Documents.Add(new Document
        {
            FileName = "User2_Doc.pdf",
            FilePath = "/path/user2.pdf",
            ContentType = "application/pdf",
            OwnerId = user2,
            EmbeddingVector = embedding
        });
        
        await _context.SaveChangesAsync();

        // Act: Search for user1's documents
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var options = new SearchOptions
        {
            TopK = 10,
            MinSimilarity = 0.9, // High similarity so we should get results
            OwnerId = user1
        };
        
        var results = await _service.VectorSearchAsync(queryEmbedding, options);

        // Assert: Should only return user1's documents
        Assert.NotNull(results);
        Assert.All(results, r => Assert.Equal(user1, r.Document.OwnerId));
    }

    [Fact]
    public async Task VectorSearchAsync_ReturnsEmptyList_WhenNoDocumentsMatchSimilarityThreshold()
    {
        // Arrange: Create a document with very different embedding
        var userId = "test-user";
        _context.Documents.Add(new Document
        {
            FileName = "Document.pdf",
            FilePath = "/path/doc.pdf",
            ContentType = "application/pdf",
            OwnerId = userId,
            EmbeddingVector = new float[] { 1.0f, 0.0f, 0.0f } // Very different
        });
        await _context.SaveChangesAsync();

        // Act: Search with very different embedding
        var queryEmbedding = new float[] { 0.0f, 1.0f, 0.0f }; // Perpendicular vectors
        var options = new SearchOptions
        {
            TopK = 10,
            MinSimilarity = 0.9, // High threshold
            OwnerId = userId
        };
        
        var results = await _service.VectorSearchAsync(queryEmbedding, options);

        // Assert: Should return empty list since similarity is too low
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
