using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocN.Data.Models;

/// <summary>
/// Represents a chunk of a document for better semantic search
/// </summary>
public class DocumentChunk
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int ChunkIndex { get; set; }

    [Required]
    [Column(TypeName = "NVARCHAR(MAX)")]
    public string ChunkText { get; set; } = string.Empty;

    /// <summary>
    /// Vector embedding for this chunk (1536 dimensions for OpenAI embeddings)
    /// </summary>
    [Column(TypeName = "NVARCHAR(MAX)")] // Will be migrated to VECTOR(1536) in SQL Server 2025
    public string? ChunkEmbedding { get; set; }

    public int? TokenCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;
}
