using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DocN.Data.Models;

namespace DocN.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentShare> DocumentShares { get; set; }
    public DbSet<DocumentTag> DocumentTags { get; set; }
    public DbSet<AIConfiguration> AIConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ExtractedText).HasMaxLength(int.MaxValue);
            entity.Property(e => e.CategoryReasoning).HasMaxLength(2000);
            
            // Configure vector column for SQL Server 2025 native vector support
            // Using VECTOR(1536, FLOAT32) for text-embedding-ada-002 embeddings
            // Note: We use a value converter to handle float[] <-> string conversion
            // until EF Core has native VECTOR type support
            var converter = new ValueConverter<float[]?, string?>(
                v => v == null ? null : string.Join(",", v),
                v => v == null ? null : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(float.Parse)
                    .ToArray()
            );
            
            entity.Property(e => e.EmbeddingVector)
                .HasColumnType("nvarchar(max)")  // Temporarily use nvarchar(max) until database supports VECTOR
                .HasConversion(converter)
                .IsRequired(false);
            
            // Index for performance with large number of documents
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.UploadedAt);
            entity.HasIndex(e => e.Visibility);
            entity.HasIndex(e => e.SuggestedCategory);
            entity.HasIndex(e => e.ActualCategory);
            
            // Relationship with owner
            entity.HasOne(e => e.Owner)
                .WithMany(u => u.Documents)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DocumentShare configuration
        modelBuilder.Entity<DocumentShare>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Document)
                .WithMany(d => d.Shares)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.SharedWithUser)
                .WithMany(u => u.SharedDocuments)
                .HasForeignKey(e => e.SharedWithUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Index for performance
            entity.HasIndex(e => e.SharedWithUserId);
        });

        // DocumentTag configuration
        modelBuilder.Entity<DocumentTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            
            entity.HasOne(e => e.Document)
                .WithMany(d => d.Tags)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Index for searching by tags
            entity.HasIndex(e => e.Name);
        });

        // AIConfiguration
        modelBuilder.Entity<AIConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConfigurationName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AzureOpenAIKey).HasMaxLength(500);
            entity.Property(e => e.SystemPrompt).HasMaxLength(2000);
        });
    }
}
