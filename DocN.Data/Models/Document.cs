namespace DocN.Data.Models;

public class Document
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ExtractedText { get; set; } = string.Empty;
    
    // Category suggested by AI with reasoning
    public string? SuggestedCategory { get; set; }
    public string? CategoryReasoning { get; set; } // NEW: Explains why AI suggested this category
    public string? ActualCategory { get; set; }
    
    // AI Tag Analysis Results
    public string? AITagsJson { get; set; } // JSON array of AI-detected tags with confidence scores
    public DateTime? AIAnalysisDate { get; set; }
    
    // AI Extracted Metadata (invoices, contracts, etc.)
    public string? ExtractedMetadataJson { get; set; } // JSON object with extracted structured data
    
    // Document metadata from processing
    public int? PageCount { get; set; }
    public string? DetectedLanguage { get; set; }
    public string? ProcessingStatus { get; set; } // "Pending", "Processing", "Completed", "Failed"
    public string? ProcessingError { get; set; }
    
    // User notes
    public string? Notes { get; set; }
    
    // Visibility management
    public DocumentVisibility Visibility { get; set; } = DocumentVisibility.Private;
    
    // Vector embeddings for semantic search - separate fields for different dimensions
    // Using native SQL Server VECTOR type for optimal performance
    
    /// <summary>
    /// 768-dimensional embedding vector (for Gemini and similar providers)
    /// Stored as VECTOR(768) in SQL Server 2025
    /// </summary>
    public float[]? EmbeddingVector768 { get; set; }
    
    /// <summary>
    /// 1536-dimensional embedding vector (for OpenAI ada-002 and similar providers)
    /// Stored as VECTOR(1536) in SQL Server 2025
    /// </summary>
    public float[]? EmbeddingVector1536 { get; set; }
    
    /// <summary>
    /// The actual dimension of the embedding vector stored.
    /// Indicates which vector field is populated: 768 or 1536
    /// </summary>
    public int? EmbeddingDimension { get; set; }
    
    /// <summary>
    /// Unified property for backward compatibility - returns the populated vector field
    /// Gets/sets the appropriate field based on dimension
    /// </summary>
    public float[]? EmbeddingVector
    {
        get
        {
            return EmbeddingVector768 ?? EmbeddingVector1536;
        }
        set
        {
            if (value == null)
            {
                EmbeddingVector768 = null;
                EmbeddingVector1536 = null;
                EmbeddingDimension = null;
            }
            else if (value.Length == 768)
            {
                EmbeddingVector768 = value;
                EmbeddingVector1536 = null;
                EmbeddingDimension = 768;
            }
            else if (value.Length == 1536)
            {
                EmbeddingVector768 = null;
                EmbeddingVector1536 = value;
                EmbeddingDimension = 1536;
            }
            else
            {
                throw new ArgumentException($"Unsupported embedding dimension: {value.Length}. Expected 768 or 1536.");
            }
        }
    }
    
    // Metadata
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }
    public int AccessCount { get; set; } = 0;
    
    // Owner (nullable until authentication is fully implemented)
    public string? OwnerId { get; set; }
    public virtual ApplicationUser? Owner { get; set; }
    
    // Multi-tenant support
    public int? TenantId { get; set; }
    public virtual Tenant? Tenant { get; set; }
    
    // Document sharing
    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();
    
    // Tags
    public virtual ICollection<DocumentTag> Tags { get; set; } = new List<DocumentTag>();
}

public enum DocumentVisibility
{
    Private = 0,      // Only owner can see
    Shared = 1,       // Shared with specific users
    Organization = 2, // All organization members
    Public = 3        // Everyone can see
}
