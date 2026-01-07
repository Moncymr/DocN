using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using DocN.Core.Interfaces;
using DocN.Data.Services;

namespace DocN.Server.Tests;

public class ContextualCompressionServiceTests
{
    private readonly Mock<ILogger<ContextualCompressionService>> _mockLogger;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly ContextualCompressionService _service;

    public ContextualCompressionServiceTests()
    {
        _mockLogger = new Mock<ILogger<ContextualCompressionService>>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        
        _service = new ContextualCompressionService(
            _mockLogger.Object,
            _mockEmbeddingService.Object);
    }

    [Fact]
    public void EstimateTokenCount_EmptyString_ReturnsZero()
    {
        // Act
        var result = _service.EstimateTokenCount("");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void EstimateTokenCount_ValidText_ReturnsEstimate()
    {
        // Arrange
        var text = "This is a test sentence with approximately twenty words to check the token estimation.";

        // Act
        var result = _service.EstimateTokenCount(text);

        // Assert
        Assert.True(result > 0);
        Assert.True(result < text.Length); // Tokens should be less than character count
    }

    [Fact]
    public async Task CompressTextAsync_ShortText_ReturnsOriginal()
    {
        // Arrange
        var query = "test query";
        var text = "Short text";
        var maxTokens = 100;

        // Act
        var result = await _service.CompressTextAsync(query, text, maxTokens);

        // Assert
        Assert.Equal(text, result);
    }

    [Fact]
    public async Task CompressChunksAsync_EmptyList_ReturnsEmptyResult()
    {
        // Arrange
        var query = "test query";
        var chunks = new List<string>();
        var targetTokens = 100;

        // Act
        var result = await _service.CompressChunksAsync(query, chunks, targetTokens);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task CompressChunksAsync_WithChunks_ReturnsCompressed()
    {
        // Arrange
        var query = "What is machine learning?";
        var chunks = new List<string>
        {
            "Machine learning is a subset of artificial intelligence that enables systems to learn from data.",
            "Deep learning is a type of machine learning that uses neural networks with multiple layers.",
            "Supervised learning requires labeled training data to train models."
        };
        var targetTokens = 50;

        // Setup mock embeddings
        var mockEmbedding = new float[384]; // Typical embedding size
        for (int i = 0; i < mockEmbedding.Length; i++)
        {
            mockEmbedding[i] = 0.1f;
        }

        _mockEmbeddingService
            .Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(mockEmbedding);

        // Act
        var result = await _service.CompressChunksAsync(query, chunks, targetTokens);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.All(c => c.TokenCount <= targetTokens));
    }

    [Fact]
    public async Task DeduplicateChunksAsync_NoDuplicates_ReturnsSameCount()
    {
        // Arrange
        var chunks = new List<string>
        {
            "First unique chunk about topic A",
            "Second unique chunk about topic B",
            "Third unique chunk about topic C"
        };

        // Setup mock embeddings with different values
        _mockEmbeddingService
            .Setup(s => s.GenerateEmbeddingAsync(chunks[0]))
            .ReturnsAsync(GenerateRandomEmbedding(0.1f));
        _mockEmbeddingService
            .Setup(s => s.GenerateEmbeddingAsync(chunks[1]))
            .ReturnsAsync(GenerateRandomEmbedding(0.5f));
        _mockEmbeddingService
            .Setup(s => s.GenerateEmbeddingAsync(chunks[2]))
            .ReturnsAsync(GenerateRandomEmbedding(0.9f));

        // Act
        var result = await _service.DeduplicateChunksAsync(chunks, 0.85);

        // Assert
        Assert.Equal(chunks.Count, result.Count);
    }

    [Fact]
    public async Task ExtractRelevantSentencesAsync_MultipleSentences_ReturnsTopN()
    {
        // Arrange
        var query = "machine learning";
        var text = "Machine learning is important. Deep learning uses neural networks. " +
                   "Data science involves statistics. AI is the future.";
        var maxSentences = 2;

        var mockEmbedding = new float[384];
        for (int i = 0; i < mockEmbedding.Length; i++)
        {
            mockEmbedding[i] = 0.1f;
        }

        _mockEmbeddingService
            .Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(mockEmbedding);

        // Act
        var result = await _service.ExtractRelevantSentencesAsync(query, text, maxSentences);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count <= maxSentences);
    }

    private float[] GenerateRandomEmbedding(float baseValue)
    {
        var embedding = new float[384];
        var random = new Random();
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = baseValue + (float)(random.NextDouble() * 0.1);
        }
        return embedding;
    }
}
