using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocN.Data.Models;

/// <summary>
/// Represents a document in the system with its metadata and embeddings
/// </summary>
public class Document
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? ExtractedText { get; set; }

    /// <summary>
    /// Vector embedding for semantic search (1536 dimensions for OpenAI embeddings)
    /// Note: SQL Server 2025 supports VECTOR type natively
    /// </summary>
    [Column(TypeName = "NVARCHAR(MAX)")] // Will be migrated to VECTOR(1536) in SQL Server 2025
    public string? EmbeddingVector { get; set; }

    [MaxLength(200)]
    public string? SuggestedCategory { get; set; }

    [MaxLength(200)]
    public string? ActualCategory { get; set; }

    /// <summary>
    /// Confidence score for category suggestion (0.0 to 1.0)
    /// </summary>
    public float? CategoryConfidence { get; set; }

    [Required]
    [MaxLength(450)]
    public string OwnerId { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// Visibility level: 1=Private, 2=Department, 3=Public
    /// </summary>
    public int Visibility { get; set; } = 1;

    public int? DepartmentId { get; set; }

    /// <summary>
    /// JSON array of tags
    /// </summary>
    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? Tags { get; set; }

    /// <summary>
    /// JSON object with additional metadata
    /// </summary>
    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? Metadata { get; set; }

    // Navigation properties
    public virtual ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
