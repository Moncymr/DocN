using Microsoft.EntityFrameworkCore;
using DocN.Data.Models;

namespace DocN.Data;

/// <summary>
/// Main database context for DocN application
/// </summary>
public class DocNDbContext : DbContext
{
    public DocNDbContext(DbContextOptions<DocNDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentChunk> DocumentChunks { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Conversation> Conversations { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => new { e.OwnerId, e.ActualCategory, e.UploadedAt });
            entity.HasIndex(e => new { e.DepartmentId, e.Visibility });
            entity.HasIndex(e => e.UploadedAt);

            entity.HasMany(e => e.Chunks)
                .WithOne(e => e.Document)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DocumentChunk configuration
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex });
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.ParentCategoryId);

            entity.HasOne(e => e.ParentCategory)
                .WithMany(e => e.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Conversation configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsActive, e.LastMessageAt });

            entity.HasMany(e => e.Messages)
                .WithOne(e => e.Conversation)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => new { e.ConversationId, e.Timestamp });
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.Timestamp);
        });

        // Seed initial categories
        SeedCategories(modelBuilder);
    }

    private void SeedCategories(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Contratti", Description = "Documenti contrattuali", Color = "#2196F3", Icon = "description", CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Fatture", Description = "Fatture e documenti fiscali", Color = "#4CAF50", Icon = "receipt", CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Name = "Report", Description = "Report e analisi", Color = "#FF9800", Icon = "assessment", CreatedAt = DateTime.UtcNow },
            new Category { Id = 4, Name = "Manuali", Description = "Manuali e documentazione tecnica", Color = "#9C27B0", Icon = "menu_book", CreatedAt = DateTime.UtcNow },
            new Category { Id = 5, Name = "Policy", Description = "Policy aziendali", Color = "#F44336", Icon = "policy", CreatedAt = DateTime.UtcNow },
            new Category { Id = 6, Name = "Corrispondenza", Description = "Email e corrispondenza", Color = "#00BCD4", Icon = "mail", CreatedAt = DateTime.UtcNow },
            new Category { Id = 7, Name = "Legale", Description = "Documenti legali", Color = "#795548", Icon = "gavel", CreatedAt = DateTime.UtcNow },
            new Category { Id = 8, Name = "HR", Description = "Risorse umane", Color = "#E91E63", Icon = "people", CreatedAt = DateTime.UtcNow },
            new Category { Id = 9, Name = "Altro", Description = "Documenti non categorizzati", Color = "#607D8B", Icon = "folder", CreatedAt = DateTime.UtcNow }
        );
    }
}
