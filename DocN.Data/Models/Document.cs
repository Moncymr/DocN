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
    
    // Vector embedding for semantic search (supports variable dimensions: 700, 768, 1536, 1583, etc.)
    public float[]? EmbeddingVector { get; set; }
    
    /// <summary>
    /// The actual dimension of the embedding vector stored.
    /// Tracks the dimension to support coexistence of different AI providers with different dimensions.
    /// Common values: 700 (Gemini custom), 768 (Gemini default), 1536 (OpenAI ada-002), 1583 (OpenAI custom), 3072 (OpenAI large)
    /// </summary>
    public int? EmbeddingDimension { get; set; }
    
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
