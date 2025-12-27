using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DocN.Data.Models;

namespace DocN.Data;

public class DocArcContext : DbContext
{
    public DocArcContext(DbContextOptions<DocArcContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentChunk> DocumentChunks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.ContentType).IsRequired();
            entity.Property(e => e.FileSize).IsRequired();
            entity.Property(e => e.ExtractedText).IsRequired();
            
            // Category fields
            entity.Property(e => e.SuggestedCategory).HasMaxLength(200);
            entity.Property(e => e.CategoryReasoning).HasMaxLength(2000);
            entity.Property(e => e.ActualCategory).HasMaxLength(200);
            
            // AI Tag Analysis Results
            entity.Property(e => e.AITagsJson).HasColumnType("nvarchar(max)");
            entity.Property(e => e.AIAnalysisDate).IsRequired(false);
            
            // Document metadata from processing
            entity.Property(e => e.PageCount).IsRequired(false);
            entity.Property(e => e.DetectedLanguage).IsRequired(false);
            entity.Property(e => e.ProcessingStatus).IsRequired(false);
            entity.Property(e => e.ProcessingError).IsRequired(false);
            
            // User notes
            entity.Property(e => e.Notes).IsRequired(false);
            
            // Visibility management
            entity.Property(e => e.Visibility).IsRequired();
            
            // Vector embedding
            var vectorConverter = new ValueConverter<float[]?, string?>(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<float[]>(v) ?? Array.Empty<float>()
            );
            
            entity.Property(e => e.EmbeddingVector)
                .HasColumnType("nvarchar(max)")
                .HasConversion(vectorConverter)
                .IsRequired(false);
            
            // Metadata
            entity.Property(e => e.UploadedAt).IsRequired();
            entity.Property(e => e.LastAccessedAt).IsRequired(false);
            entity.Property(e => e.AccessCount).IsRequired();
            
            // Owner
            entity.Property(e => e.OwnerId).IsRequired(false);
            
            // Multi-tenant support
            entity.Property(e => e.TenantId).IsRequired(false);
        });

        // DocumentChunk configuration
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("DocumentChunks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChunkText).IsRequired();
            
            // Configure vector column for chunk embeddings
            var chunkVectorConverter = new ValueConverter<float[]?, string?>(
                v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v),
                v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<float[]>(v) ?? Array.Empty<float>()
            );
            
            entity.Property(e => e.ChunkEmbedding)
                .HasColumnType("nvarchar(max)")
                .HasConversion(chunkVectorConverter)
                .IsRequired(false);
            
            entity.Property(e => e.TokenCount).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.StartPosition).IsRequired();
            entity.Property(e => e.EndPosition).IsRequired();
            
            // Relationship with Document
            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for performance
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex });
        });
    }
}
