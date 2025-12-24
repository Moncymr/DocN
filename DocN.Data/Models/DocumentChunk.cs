namespace DocN.Data.Models;

/// <summary>
/// Represents a chunk of a document for more granular vector search
/// Documents are split into smaller chunks to improve retrieval accuracy
/// </summary>
public class DocumentChunk
{
    /// <summary>
    /// Unique identifier for the chunk
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ID of the parent document
    /// </summary>
    public int DocumentId { get; set; }

    /// <summary>
    /// Parent document reference
    /// </summary>
    public virtual Document? Document { get; set; }

    /// <summary>
    /// Index of this chunk within the document (0-based)
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Text content of this chunk
    /// </summary>
    public string ChunkText { get; set; } = string.Empty;

    /// <summary>
    /// Vector embedding for this chunk (1536 dimensions for text-embedding-ada-002)
    /// </summary>
    public float[]? ChunkEmbedding { get; set; }

    /// <summary>
    /// Token count for this chunk (useful for staying within model limits)
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    /// When this chunk was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Start position of this chunk in the original document text
    /// </summary>
    public int StartPosition { get; set; }

    /// <summary>
    /// End position of this chunk in the original document text
    /// </summary>
    public int EndPosition { get; set; }
}
