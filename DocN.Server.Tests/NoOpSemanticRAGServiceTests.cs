using DocN.Data;
using DocN.Data.Models;
using DocN.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocN.Server.Tests;

public class NoOpSemanticRAGServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly NoOpSemanticRAGService _service;
    private readonly Mock<ILogger<NoOpSemanticRAGService>> _mockLogger;

    public NoOpSemanticRAGServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<NoOpSemanticRAGService>>();
        _service = new NoOpSemanticRAGService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task SearchDocumentsWithEmbeddingAsync_ReturnsEmptyList_WhenNoDocumentsExist()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var userId = "test-user";

        // Act
        var result = await _service.SearchDocumentsWithEmbeddingAsync(queryEmbedding, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchDocumentsWithEmbeddingAsync_ReturnsEmptyList_WhenQueryEmbeddingIsNull()
    {
        // Arrange
        float[]? queryEmbedding = null;
        var userId = "test-user";

        // Act
        var result = await _service.SearchDocumentsWithEmbeddingAsync(queryEmbedding!, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchDocumentsWithEmbeddingAsync_ReturnsMatchingDocuments_WhenSimilarityAboveThreshold()
    {
        // Arrange
        var userId = "test-user";
        var queryEmbedding = CreateNormalizedVector(new float[] { 1.0f, 0.0f, 0.0f });

        // Create a document with a similar embedding (cosine similarity = 1.0)
        var document = new Document
        {
            Id = 1,
            FileName = "test.pdf",
            OwnerId = userId,
            EmbeddingVector = CreateNormalizedVector(new float[] { 1.0f, 0.0f, 0.0f }),
            ExtractedText = "Test document content",
            ActualCategory = "Test"
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchDocumentsWithEmbeddingAsync(
            queryEmbedding, 
            userId, 
            topK: 10, 
            minSimilarity: 0.5);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("test.pdf", result[0].FileName);
        Assert.True(result[0].SimilarityScore >= 0.5);
    }

    [Fact]
    public async Task SearchDocumentsWithEmbeddingAsync_ExcludesDocuments_WhenSimilarityBelowThreshold()
    {
        // Arrange
        var userId = "test-user";
        var queryEmbedding = CreateNormalizedVector(new float[] { 1.0f, 0.0f, 0.0f });

        // Create a document with orthogonal embedding (cosine similarity = 0.0)
        var document = new Document
        {
            Id = 1,
            FileName = "test.pdf",
            OwnerId = userId,
            EmbeddingVector = CreateNormalizedVector(new float[] { 0.0f, 1.0f, 0.0f }),
            ExtractedText = "Test document content",
            ActualCategory = "Test"
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchDocumentsWithEmbeddingAsync(
            queryEmbedding, 
            userId, 
            topK: 10, 
            minSimilarity: 0.5);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchDocumentsWithEmbeddingAsync_RespectsUserIdFiltering()
    {
        // Arrange
        var userId1 = "user1";
        var userId2 = "user2";
        var queryEmbedding = CreateNormalizedVector(new float[] { 1.0f, 0.0f, 0.0f });

        // Create documents for different users
        var document1 = new Document
        {
            Id = 1,
            FileName = "user1-doc.pdf",
            OwnerId = userId1,
            EmbeddingVector = CreateNormalizedVector(new float[] { 1.0f, 0.0f, 0.0f }),
            ExtractedText = "User 1 document"
        };

        var document2 = new Document
        {
            Id = 2,
            FileName = "user2-doc.pdf",
            OwnerId = userId2,
            EmbeddingVector = CreateNormalizedVector(new float[] { 1.0f, 0.0f, 0.0f }),
            ExtractedText = "User 2 document"
        };

        _context.Documents.AddRange(document1, document2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchDocumentsWithEmbeddingAsync(
            queryEmbedding, 
            userId1, 
            topK: 10, 
            minSimilarity: 0.5);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("user1-doc.pdf", result[0].FileName);
    }

    [Fact]
    public async Task SearchDocumentsWithEmbeddingAsync_ReturnsTopKResults()
    {
        // Arrange
        var userId = "test-user";
        var queryEmbedding = CreateNormalizedVector(new float[] { 1.0f, 0.0f, 0.0f });

        // Create 5 similar documents
        for (int i = 0; i < 5; i++)
        {
            var document = new Document
            {
                Id = i + 1,
                FileName = $"doc{i}.pdf",
                OwnerId = userId,
                EmbeddingVector = CreateNormalizedVector(new float[] { 0.9f, 0.1f, 0.0f }),
                ExtractedText = $"Document {i}"
            };
            _context.Documents.Add(document);
        }

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.SearchDocumentsWithEmbeddingAsync(
            queryEmbedding, 
            userId, 
            topK: 3, 
            minSimilarity: 0.5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    /// <summary>
    /// Helper method to create a normalized vector for testing
    /// </summary>
    private float[] CreateNormalizedVector(float[] vector)
    {
        var magnitude = Math.Sqrt(vector.Sum(v => v * v));
        if (magnitude == 0) return vector;
        return vector.Select(v => (float)(v / magnitude)).ToArray();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
