using Microsoft.Extensions.Logging;

namespace DocN.Data.Utilities;

/// <summary>
/// Helper class for validating embedding dimensions and providing helpful error messages
/// </summary>
public static class EmbeddingValidationHelper
{
    // Constants for embedding dimensions
    public const int GeminiEmbeddingDimension = 768;
    public const int OpenAIEmbeddingDimension = 1536;

    /// <summary>
    /// Validates embedding dimensions to ensure compatibility with database
    /// </summary>
    /// <param name="embeddingVector">The embedding vector to validate</param>
    /// <param name="logger">Optional logger for recording validation errors</param>
    /// <exception cref="InvalidOperationException">Thrown when embedding dimensions are invalid</exception>
    public static void ValidateEmbeddingDimensions(float[]? embeddingVector, ILogger? logger = null)
    {
        if (embeddingVector != null && embeddingVector.Length > 0)
        {
            var embeddingDimension = embeddingVector.Length;
            
            // Check if dimension is valid (768 for Gemini, 1536 for OpenAI/Azure)
            if (embeddingDimension != GeminiEmbeddingDimension && embeddingDimension != OpenAIEmbeddingDimension)
            {
                logger?.LogError("Invalid embedding dimension: {Dimension}. Expected {Gemini} (Gemini) or {OpenAI} (OpenAI/Azure)", 
                    embeddingDimension, GeminiEmbeddingDimension, OpenAIEmbeddingDimension);
                
                throw new InvalidOperationException(
                    $"Invalid embedding dimension: {embeddingDimension}. " +
                    $"Expected {GeminiEmbeddingDimension} (Gemini) or {OpenAIEmbeddingDimension} (OpenAI/Azure OpenAI). " +
                    "Please check your AI provider configuration.");
            }
        }
    }
    
    /// <summary>
    /// Checks if an exception message indicates a vector dimension mismatch error
    /// </summary>
    /// <param name="errorMessage">The error message to check</param>
    /// <returns>True if the error appears to be a dimension mismatch, false otherwise</returns>
    public static bool IsVectorDimensionMismatchError(string errorMessage)
    {
        return errorMessage.Contains("dimensioni del vettore", StringComparison.OrdinalIgnoreCase) || 
               errorMessage.Contains("vector", StringComparison.OrdinalIgnoreCase) || 
               errorMessage.Contains("1536") || 
               errorMessage.Contains("768");
    }
    
    /// <summary>
    /// Creates a detailed error message for dimension mismatch errors with actionable solutions
    /// </summary>
    /// <param name="embeddingDimension">The actual embedding dimension that was generated (0 if unknown)</param>
    /// <param name="originalError">The original error message from the database</param>
    /// <returns>A formatted error message with troubleshooting steps</returns>
    public static string CreateDimensionMismatchErrorMessage(int embeddingDimension, string originalError)
    {
        var dimensionInfo = embeddingDimension > 0 
            ? $"📊 Generated embedding dimensions: {embeddingDimension}\n"
            : "";
            
        return $"DATABASE DIMENSION MISMATCH ERROR:\n\n" +
               dimensionInfo +
               $"❌ Database vector configuration mismatch detected.\n\n" +
               $"SOLUTION:\n" +
               $"1. If you're using Gemini ({GeminiEmbeddingDimension} dimensions):\n" +
               $"   - Your database should be configured for VECTOR({GeminiEmbeddingDimension})\n" +
               $"   - Run: database/Update_Vector_1536_to_768.sql (if exists)\n\n" +
               $"2. If you're using OpenAI/Azure OpenAI ({OpenAIEmbeddingDimension} dimensions):\n" +
               $"   - Your database should be configured for VECTOR({OpenAIEmbeddingDimension})\n" +
               $"   - Run: database/Update_Vector_768_to_1536.sql\n\n" +
               $"3. Switch AI provider to match your database configuration:\n" +
               $"   - Go to AI Configuration page\n" +
               $"   - Select the appropriate embedding provider\n\n" +
               $"Original error: {originalError}";
    }
}
