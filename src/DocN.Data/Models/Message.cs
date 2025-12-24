using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocN.Data.Models;

/// <summary>
/// Represents a message in a conversation
/// </summary>
public class Message
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ConversationId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = string.Empty; // "user" or "assistant"

    [Required]
    [Column(TypeName = "NVARCHAR(MAX)")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of document IDs that were referenced in generating this message
    /// </summary>
    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? ReferencedDocumentIds { get; set; }

    public int? TokensUsed { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(ConversationId))]
    public virtual Conversation Conversation { get; set; } = null!;
}
