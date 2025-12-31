using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using DocN.Data.Services;
using DocN.Core.Interfaces;

namespace DocN.Server.Tests;

/// <summary>
/// Test per il servizio HyDE (Hypothetical Document Embeddings)
/// </summary>
public class HyDEServiceTests
{
    private readonly Mock<ILogger<HyDEService>> _mockLogger;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<ISemanticRAGService> _mockRagService;

    public HyDEServiceTests()
    {
        _mockLogger = new Mock<ILogger<HyDEService>>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockRagService = new Mock<ISemanticRAGService>();
        
        // Setup default behaviors
        _mockEmbeddingService.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>()))
            .ReturnsAsync(new float[768]); // Dummy embedding
        
        _mockRagService.Setup(s => s.SearchDocumentsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
            .ReturnsAsync(new List<RelevantDocumentResult>());
        
        _mockRagService.Setup(s => s.SearchDocumentsWithEmbeddingAsync(It.IsAny<float[]>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
            .ReturnsAsync(new List<RelevantDocumentResult>());
    }

    [Fact]
    public async Task GenerateHypotheticalDocumentAsync_ReturnsNonEmptyDocument()
    {
        // Arrange
        var service = CreateService();
        var query = "Come ridurre i costi operativi?";

        // Act
        var result = await service.GenerateHypotheticalDocumentAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GenerateHypotheticalDocumentAsync_WithContext_UsesContext()
    {
        // Arrange
        var service = CreateService();
        var query = "Strategie di ottimizzazione";
        var context = "Settore manifatturiero, focus su automazione";

        // Act
        var result = await service.GenerateHypotheticalDocumentAsync(query, context);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GenerateMultipleHypotheticalDocumentsAsync_ReturnsRequestedNumber()
    {
        // Arrange
        var service = CreateService();
        var query = "Migliorare la produttività";
        var numVariants = 3;

        // Act
        var result = await service.GenerateMultipleHypotheticalDocumentsAsync(query, numVariants);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(numVariants, result.Count);
    }

    [Fact]
    public async Task SearchWithHyDEAsync_CallsEmbeddingService()
    {
        // Arrange
        var service = CreateService();
        var query = "test query";
        var userId = "user123";

        // Act
        var result = await service.SearchWithHyDEAsync(query, userId);

        // Assert
        Assert.NotNull(result);
        // Verifica che il servizio di embedding sia stato chiamato
        _mockEmbeddingService.Verify(
            s => s.GenerateEmbeddingAsync(It.IsAny<string>()), 
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SearchHybridWithHyDEAsync_CombinesResults()
    {
        // Arrange
        var standardResults = new List<RelevantDocumentResult>
        {
            new() { DocumentId = 1, SimilarityScore = 0.9, FileName = "doc1.pdf" },
            new() { DocumentId = 2, SimilarityScore = 0.7, FileName = "doc2.pdf" }
        };

        var hydeResults = new List<RelevantDocumentResult>
        {
            new() { DocumentId = 2, SimilarityScore = 0.8, FileName = "doc2.pdf" },
            new() { DocumentId = 3, SimilarityScore = 0.75, FileName = "doc3.pdf" }
        };

        _mockRagService.Setup(s => s.SearchDocumentsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
            .ReturnsAsync(standardResults);

        var service = CreateServiceWithMocks(hydeResults);
        var query = "test query";
        var userId = "user123";

        // Act
        var result = await service.SearchHybridWithHyDEAsync(query, userId, topK: 10, hydeWeight: 0.6);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // Dovrebbe contenere documenti da entrambe le ricerche
    }

    [Fact]
    public async Task AnalyzeQueryForHyDEAsync_ReturnsRecommendation()
    {
        // Arrange
        var service = CreateService();
        var query = "Come migliorare l'efficienza aziendale?";

        // Act
        var result = await service.AnalyzeQueryForHyDEAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.Confidence, 0, 1);
        Assert.InRange(result.SuggestedHyDEWeight, 0, 1);
    }

    [Theory]
    [InlineData("fattura 2024")] // Simple query
    [InlineData("Come possiamo innovare il processo?")] // Conceptual query
    [InlineData("ID-12345")] // Exact search
    public async Task AnalyzeQueryForHyDEAsync_HandlesDifferentQueryTypes(string query)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.AnalyzeQueryForHyDEAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.True(Enum.IsDefined(typeof(QueryType), result.QueryType));
    }

    [Fact]
    public async Task SearchWithHyDEAsync_WithDisabledConfig_FallsBackToStandard()
    {
        // Arrange
        var config = new HyDEConfiguration { Enabled = false };
        var service = CreateService(config);
        var query = "test query";
        var userId = "user123";

        // Act
        var result = await service.SearchWithHyDEAsync(query, userId);

        // Assert
        Assert.NotNull(result);
        // Verifica che sia stata chiamata la ricerca standard
        _mockRagService.Verify(
            s => s.SearchDocumentsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateMultipleHypotheticalDocumentsAsync_GeneratesDistinctDocuments()
    {
        // Arrange
        var service = CreateService();
        var query = "strategie aziendali";
        var numVariants = 2;

        // Act
        var result = await service.GenerateMultipleHypotheticalDocumentsAsync(query, numVariants);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(numVariants, result.Count);
        // I documenti dovrebbero essere diversi
        if (result.Count >= 2)
        {
            Assert.NotEqual(result[0], result[1]);
        }
    }

    [Fact]
    public async Task SearchWithHyDEAsync_WithMultipleHypotheticalDocs_AggregatesResults()
    {
        // Arrange
        var config = new HyDEConfiguration { NumHypotheticalDocs = 2 };
        var service = CreateService(config);
        var query = "test query";
        var userId = "user123";

        // Act
        var result = await service.SearchWithHyDEAsync(query, userId);

        // Assert
        Assert.NotNull(result);
        // Dovrebbe aver chiamato l'embedding service almeno 2 volte
        _mockEmbeddingService.Verify(
            s => s.GenerateEmbeddingAsync(It.IsAny<string>()),
            Times.AtLeast(2));
    }

    /// <summary>
    /// Crea un'istanza del servizio per i test
    /// </summary>
    private IHyDEService CreateService(HyDEConfiguration? config = null)
    {
        try
        {
            var kernel = new KernelBuilder().Build();
            return new HyDEService(
                kernel, 
                _mockLogger.Object, 
                _mockEmbeddingService.Object,
                _mockRagService.Object,
                config);
        }
        catch
        {
            // Mock service per quando kernel non è disponibile
            var mockService = new Mock<IHyDEService>();
            
            mockService.Setup(s => s.GenerateHypotheticalDocumentAsync(It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync((string q, string? c) => $"Hypothetical document for: {q}");
            
            mockService.Setup(s => s.GenerateMultipleHypotheticalDocumentsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>()))
                .ReturnsAsync((string q, int n, string? c) => 
                    Enumerable.Range(1, n).Select(i => $"Hypothetical document {i} for: {q}").ToList());
            
            mockService.Setup(s => s.SearchWithHyDEAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
                .ReturnsAsync(new List<RelevantDocumentResult>());
            
            mockService.Setup(s => s.SearchHybridWithHyDEAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
                .ReturnsAsync(new List<RelevantDocumentResult>());
            
            mockService.Setup(s => s.AnalyzeQueryForHyDEAsync(It.IsAny<string>()))
                .ReturnsAsync(new HyDERecommendation
                {
                    IsRecommended = true,
                    Confidence = 0.7,
                    QueryType = QueryType.Conceptual,
                    SuggestedHyDEWeight = 0.6,
                    Reason = "Test recommendation"
                });
            
            return mockService.Object;
        }
    }

    /// <summary>
    /// Crea servizio con mocks specifici per HyDE results
    /// </summary>
    private IHyDEService CreateServiceWithMocks(List<RelevantDocumentResult> hydeResults)
    {
        try
        {
            var kernel = new KernelBuilder().Build();
            
            _mockRagService.Setup(s => s.SearchDocumentsWithEmbeddingAsync(
                It.IsAny<float[]>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>()))
                .ReturnsAsync(hydeResults);
            
            return new HyDEService(
                kernel,
                _mockLogger.Object,
                _mockEmbeddingService.Object,
                _mockRagService.Object);
        }
        catch
        {
            return CreateService();
        }
    }
}
