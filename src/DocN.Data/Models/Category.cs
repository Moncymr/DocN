using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocN.Data.Models;

/// <summary>
/// Represents a document category with hierarchical support
/// </summary>
public class Category
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }

    [MaxLength(7)]
    public string? Color { get; set; } // Hex color like #FF5733

    [MaxLength(50)]
    public string? Icon { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ParentCategoryId))]
    public virtual Category? ParentCategory { get; set; }

    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
}
