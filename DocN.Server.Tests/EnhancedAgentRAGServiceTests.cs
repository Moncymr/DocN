using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DocN.Core.Interfaces;
using DocN.Core.AI.Configuration;
using DocN.Data.Services;
using DocN.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DocN.Server.Tests;

public class EnhancedAgentRAGServiceTests
{
    private readonly Mock<ILogger<EnhancedAgentRAGService>> _mockLogger;
    private readonly Mock<IKernelProvider> _mockKernelProvider;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<IHyDEService> _mockHydeService;
    private readonly Mock<IReRankingService> _mockReRankingService;
    private readonly Mock<IContextualCompressionService> _mockCompressionService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly IOptions<EnhancedRAGConfiguration> _config;
    private readonly ApplicationDbContext _context;

    public EnhancedAgentRAGServiceTests()
    {
        _mockLogger = new Mock<ILogger<EnhancedAgentRAGService>>();
        _mockKernelProvider = new Mock<IKernelProvider>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockHydeService = new Mock<IHyDEService>();
        _mockReRankingService = new Mock<IReRankingService>();
        _mockCompressionService = new Mock<IContextualCompressionService>();
        _mockCacheService = new Mock<ICacheService>();

        // Setup configuration
        var config = new EnhancedRAGConfiguration
        {
            UseEnhancedAgentRAG = true,
            QueryAnalysis = new QueryAnalysisOptions
            {
                EnableHyDE = true,
                EnableQueryRewriting = true
            },
            Retrieval = new RetrievalOptions
            {
                DefaultTopK = 10,
                MinSimilarity = 0.5,
                CandidateMultiplier = 2
            },
            Reranking = new RerankingOptions
            {
                Enabled = true
            },
            Synthesis = new SynthesisOptions
            {
                EnableContextualCompression = true,
                MaxContextLength = 4000
            },
            Caching = new CachingOptions
            {
                EnableRetrievalCache = true,
                EnableQueryAnalysisCache = true,
                CacheExpirationHours = 1
            }
        };
        _config = Options.Create(config);

        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SearchDocumentsAsync_WithHyDEEnabled_UsesHyDE()
    {
        // Arrange
        var service = CreateService();
        var query = "test query";
        var userId = "user123";

        // Setup HyDE recommendation
        _mockHydeService
            .Setup(s => s.AnalyzeQueryForHyDEAsync(query))
            .ReturnsAsync(new HyDERecommendation
            {
                IsRecommended = true,
                Confidence = 0.9,
                QueryType = QueryType.Conceptual
            });

        _mockHydeService
            .Setup(s => s.SearchWithHyDEAsync(query, userId, It.IsAny<int>(), It.IsAny<double>()))
            .ReturnsAsync(new List<RelevantDocumentResult>
            {
                new RelevantDocumentResult
                {
                    DocumentId = 1,
                    FileName = "test.pdf",
                    SimilarityScore = 0.85
                }
            });

        // Act
        var results = await service.SearchDocumentsAsync(query, userId, 10, 0.7);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        _mockHydeService.Verify(s => s.SearchWithHyDEAsync(query, userId, It.IsAny<int>(), It.IsAny<double>()), Times.Once);
    }

    [Fact]
    public async Task SearchDocumentsAsync_WithHyDEDisabled_UsesStandardSearch()
    {
        // Arrange
        var config = new EnhancedRAGConfiguration
        {
            QueryAnalysis = new QueryAnalysisOptions { EnableHyDE = false }
        };
        var service = CreateService(Options.Create(config));

        var query = "test query";
        var userId = "user123";
        var embedding = new float[384];

        _mockEmbeddingService
            .Setup(s => s.GenerateEmbeddingAsync(query))
            .ReturnsAsync(embedding);

        // Act
        var results = await service.SearchDocumentsAsync(query, userId, 10, 0.7);

        // Assert
        Assert.NotNull(results);
        _mockHydeService.Verify(s => s.AnalyzeQueryForHyDEAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithCaching_UsesCachedResults()
    {
        // Arrange
        var service = CreateService();
        var query = "test query";
        var userId = "user123";

        // Setup cached query analysis
        _mockCacheService
            .Setup(s => s.GetCachedSearchResultsAsync<(string, string?)>(It.IsAny<string>()))
            .ReturnsAsync(new List<(string, string?)> { (query, null) });

        // Setup cached retrieval
        var cachedDocs = new List<RelevantDocumentResult>
        {
            new RelevantDocumentResult
            {
                DocumentId = 1,
                FileName = "cached.pdf",
                SimilarityScore = 0.9,
                RelevantChunk = "Cached content"
            }
        };
        _mockCacheService
            .Setup(s => s.GetCachedSearchResultsAsync<RelevantDocumentResult>(It.IsAny<string>()))
            .ReturnsAsync(cachedDocs);

        // Setup reranking
        _mockReRankingService
            .Setup(s => s.ReRankResultsAsync(query, cachedDocs, It.IsAny<int>()))
            .ReturnsAsync(cachedDocs);

        // Setup compression
        _mockCompressionService
            .Setup(s => s.CompressChunksAsync(query, It.IsAny<List<string>>(), It.IsAny<int>()))
            .ReturnsAsync(new List<CompressedChunk>
            {
                new CompressedChunk
                {
                    Content = "Compressed content",
                    TokenCount = 10,
                    RelevanceScore = 0.9
                }
            });

        // Act
        var response = await service.GenerateResponseAsync(query, userId);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Metadata.ContainsKey("query_analysis_cached"));
        Assert.True(response.Metadata.ContainsKey("retrieval_cached"));
    }

    [Fact]
    public async Task GenerateResponseAsync_WithReranking_AppliesReranking()
    {
        // Arrange
        var service = CreateService();
        var query = "test query";
        var userId = "user123";

        // Seed test data
        await SeedTestDocumentsAsync();

        // Setup mocks
        SetupDefaultMocks(query);

        var initialResults = new List<RelevantDocumentResult>
        {
            new RelevantDocumentResult { DocumentId = 1, SimilarityScore = 0.7 },
            new RelevantDocumentResult { DocumentId = 2, SimilarityScore = 0.6 }
        };

        var rerankedResults = new List<RelevantDocumentResult>
        {
            new RelevantDocumentResult { DocumentId = 2, SimilarityScore = 0.9 },
            new RelevantDocumentResult { DocumentId = 1, SimilarityScore = 0.8 }
        };

        _mockReRankingService
            .Setup(s => s.ReRankResultsAsync(query, It.IsAny<List<RelevantDocumentResult>>(), It.IsAny<int>()))
            .ReturnsAsync(rerankedResults);

        // Act
        var response = await service.GenerateResponseAsync(query, userId);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Metadata.ContainsKey("reranking_enabled"));
        _mockReRankingService.Verify(s => s.ReRankResultsAsync(query, It.IsAny<List<RelevantDocumentResult>>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GenerateResponseAsync_WithCompression_CompressesContext()
    {
        // Arrange
        var service = CreateService();
        var query = "test query";
        var userId = "user123";

        await SeedTestDocumentsAsync();
        SetupDefaultMocks(query);

        var compressedChunks = new List<CompressedChunk>
        {
            new CompressedChunk
            {
                Content = "Compressed text",
                TokenCount = 50,
                CompressionRatio = 0.5,
                RelevanceScore = 0.9
            }
        };

        _mockCompressionService
            .Setup(s => s.CompressChunksAsync(query, It.IsAny<List<string>>(), It.IsAny<int>()))
            .ReturnsAsync(compressedChunks);

        _mockCompressionService
            .Setup(s => s.EstimateTokenCount(It.IsAny<string>()))
            .Returns(50);

        // Act
        var response = await service.GenerateResponseAsync(query, userId);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Metadata.ContainsKey("compression_enabled"));
        Assert.True(response.Metadata.ContainsKey("compressed_tokens"));
        _mockCompressionService.Verify(s => s.CompressChunksAsync(query, It.IsAny<List<string>>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GenerateStreamingResponseAsync_YieldsProgressiveUpdates()
    {
        // Arrange
        var service = CreateService();
        var query = "test query";
        var userId = "user123";

        await SeedTestDocumentsAsync();
        SetupDefaultMocks(query);

        // Act
        var chunks = new List<string>();
        await foreach (var chunk in service.GenerateStreamingResponseAsync(query, userId))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.NotEmpty(chunks);
        Assert.Contains(chunks, c => c.Contains("Analyzing query"));
        Assert.Contains(chunks, c => c.Contains("Retrieving documents"));
        Assert.Contains(chunks, c => c.Contains("Re-ranking"));
        Assert.Contains(chunks, c => c.Contains("Compressing context"));
    }

    private EnhancedAgentRAGService CreateService(IOptions<EnhancedRAGConfiguration>? config = null)
    {
        return new EnhancedAgentRAGService(
            _context,
            _mockLogger.Object,
            _mockKernelProvider.Object,
            _mockEmbeddingService.Object,
            _mockHydeService.Object,
            _mockReRankingService.Object,
            _mockCompressionService.Object,
            _mockCacheService.Object,
            config ?? _config);
    }

    private void SetupDefaultMocks(string query)
    {
        // HyDE
        _mockHydeService
            .Setup(s => s.AnalyzeQueryForHyDEAsync(query))
            .ReturnsAsync(new HyDERecommendation
            {
                IsRecommended = false,
                QueryType = QueryType.Simple
            });

        // Embeddings
        _mockEmbeddingService
            .Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[384]);

        // ReRanking
        _mockReRankingService
            .Setup(s => s.ReRankResultsAsync(It.IsAny<string>(), It.IsAny<List<RelevantDocumentResult>>(), It.IsAny<int>()))
            .ReturnsAsync((string q, List<RelevantDocumentResult> docs, int k) => docs.Take(k).ToList());

        // Compression
        _mockCompressionService
            .Setup(s => s.CompressChunksAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<int>()))
            .ReturnsAsync((string q, List<string> chunks, int tokens) =>
                chunks.Select((c, i) => new CompressedChunk
                {
                    Content = c,
                    OriginalIndex = i,
                    TokenCount = 50,
                    RelevanceScore = 0.8
                }).ToList());

        _mockCompressionService
            .Setup(s => s.EstimateTokenCount(It.IsAny<string>()))
            .Returns(100);
    }

    private async Task SeedTestDocumentsAsync()
    {
        var user = new User { Id = "user123", UserName = "testuser" };
        _context.Users.Add(user);

        var doc1 = new Document
        {
            Id = 1,
            FileName = "test1.pdf",
            UserId = "user123",
            UploadDate = DateTime.UtcNow
        };

        var doc2 = new Document
        {
            Id = 2,
            FileName = "test2.pdf",
            UserId = "user123",
            UploadDate = DateTime.UtcNow
        };

        _context.Documents.AddRange(doc1, doc2);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
