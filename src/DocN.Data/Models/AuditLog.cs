using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocN.Data.Models;

/// <summary>
/// Represents an audit log entry for compliance and security tracking
/// </summary>
public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // "VIEW", "DOWNLOAD", "SEARCH", "UPLOAD", "DELETE", "UPDATE"

    [Required]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty; // "Document", "Conversation", "Category"

    [Required]
    public int EntityId { get; set; }

    /// <summary>
    /// JSON object with additional details about the action
    /// </summary>
    [Column(TypeName = "NVARCHAR(MAX)")]
    public string? Details { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
