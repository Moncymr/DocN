using DocN.Data.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocN.Server.Tests;

/// <summary>
/// Tests for EmbeddingValidationHelper with flexible vector dimensions
/// </summary>
public class EmbeddingValidationHelperTests
{
    private readonly Mock<ILogger> _mockLogger;

    public EmbeddingValidationHelperTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Theory]
    [InlineData(256)]   // Minimum dimension
    [InlineData(700)]   // Gemini custom
    [InlineData(768)]   // Gemini default
    [InlineData(1536)]  // OpenAI ada-002
    [InlineData(1583)]  // OpenAI custom
    [InlineData(3072)]  // OpenAI large
    [InlineData(4096)]  // Maximum dimension
    public void ValidateEmbeddingDimensions_ValidDimensions_ShouldNotThrow(int dimension)
    {
        // Arrange
        var embedding = new float[dimension];
        
        // Act & Assert
        var exception = Record.Exception(() => 
            EmbeddingValidationHelper.ValidateEmbeddingDimensions(embedding, _mockLogger.Object));
        
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(255)]   // Below minimum
    [InlineData(100)]   // Too small
    [InlineData(4097)]  // Above maximum
    [InlineData(5000)]  // Too large
    public void ValidateEmbeddingDimensions_InvalidDimensions_ShouldThrow(int dimension)
    {
        // Arrange
        var embedding = new float[dimension];
        
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            EmbeddingValidationHelper.ValidateEmbeddingDimensions(embedding, _mockLogger.Object));
        
        Assert.Contains("Invalid embedding dimension", exception.Message);
        Assert.Contains(dimension.ToString(), exception.Message);
    }

    [Fact]
    public void ValidateEmbeddingDimensions_NullEmbedding_ShouldNotThrow()
    {
        // Arrange
        float[]? embedding = null;
        
        // Act & Assert
        var exception = Record.Exception(() =>
            EmbeddingValidationHelper.ValidateEmbeddingDimensions(embedding, _mockLogger.Object));
        
        Assert.Null(exception);
    }

    [Fact]
    public void ValidateEmbeddingDimensions_EmptyEmbedding_ShouldNotThrow()
    {
        // Arrange
        var embedding = Array.Empty<float>();
        
        // Act & Assert
        var exception = Record.Exception(() =>
            EmbeddingValidationHelper.ValidateEmbeddingDimensions(embedding, _mockLogger.Object));
        
        Assert.Null(exception);
    }

    [Fact]
    public void IsVectorDimensionMismatchError_WithDimensionKeywords_ReturnsTrue()
    {
        // Arrange
        var errorMessages = new[]
        {
            "Le dimensioni del vettore non corrispondono",
            "vector dimension mismatch",
            "Vector dimensions do not match",
            "Dimension error occurred"
        };
        
        // Act & Assert
        foreach (var errorMessage in errorMessages)
        {
            Assert.True(
                EmbeddingValidationHelper.IsVectorDimensionMismatchError(errorMessage),
                $"Should detect dimension error in: {errorMessage}");
        }
    }

    [Fact]
    public void IsVectorDimensionMismatchError_WithoutDimensionKeywords_ReturnsFalse()
    {
        // Arrange
        var errorMessages = new[]
        {
            "Network timeout error",
            "Authentication failed",
            "Database connection error"
        };
        
        // Act & Assert
        foreach (var errorMessage in errorMessages)
        {
            Assert.False(
                EmbeddingValidationHelper.IsVectorDimensionMismatchError(errorMessage),
                $"Should not detect dimension error in: {errorMessage}");
        }
    }

    [Fact]
    public void CreateDimensionMismatchErrorMessage_WithDimension_IncludesDimensionInfo()
    {
        // Arrange
        var dimension = 700;
        var originalError = "Test error message";
        
        // Act
        var errorMessage = EmbeddingValidationHelper.CreateDimensionMismatchErrorMessage(dimension, originalError);
        
        // Assert
        Assert.Contains(dimension.ToString(), errorMessage);
        Assert.Contains("Gemini (custom): 700 dimensions", errorMessage);
        Assert.Contains("Gemini (default): 768 dimensions", errorMessage);
        Assert.Contains("OpenAI ada-002: 1536 dimensions", errorMessage);
        Assert.Contains("OpenAI (custom): 1583 dimensions", errorMessage);
        Assert.Contains("OpenAI large: 3072 dimensions", errorMessage);
        Assert.Contains(originalError, errorMessage);
    }

    [Fact]
    public void CreateDimensionMismatchErrorMessage_WithoutDimension_OmitsDimensionInfo()
    {
        // Arrange
        var dimension = 0;
        var originalError = "Test error message";
        
        // Act
        var errorMessage = EmbeddingValidationHelper.CreateDimensionMismatchErrorMessage(dimension, originalError);
        
        // Assert
        Assert.DoesNotContain("Generated embedding dimensions:", errorMessage);
        Assert.Contains(originalError, errorMessage);
    }

    [Fact]
    public void Constants_HaveCorrectValues()
    {
        // Assert
        Assert.Equal(768, EmbeddingValidationHelper.GeminiEmbeddingDimension);
        Assert.Equal(1536, EmbeddingValidationHelper.OpenAIEmbeddingDimension);
        Assert.Equal(256, EmbeddingValidationHelper.MinimumEmbeddingDimension);
        Assert.Equal(4096, EmbeddingValidationHelper.MaximumEmbeddingDimension);
    }

    [Theory]
    [InlineData(700, 1583)]  // Gemini custom vs OpenAI custom
    [InlineData(768, 1536)]  // Gemini default vs OpenAI ada-002
    [InlineData(768, 3072)]  // Gemini vs OpenAI large
    public void ValidateEmbeddingDimensions_DifferentValidDimensions_BothShouldPass(int dimension1, int dimension2)
    {
        // Arrange
        var embedding1 = new float[dimension1];
        var embedding2 = new float[dimension2];
        
        // Act & Assert
        var exception1 = Record.Exception(() =>
            EmbeddingValidationHelper.ValidateEmbeddingDimensions(embedding1, _mockLogger.Object));
        var exception2 = Record.Exception(() =>
            EmbeddingValidationHelper.ValidateEmbeddingDimensions(embedding2, _mockLogger.Object));
        
        Assert.Null(exception1);
        Assert.Null(exception2);
    }
}
