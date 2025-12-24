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
    public DbSet<DocumentChunk> DocumentChunks { get; set; }
    public DbSet<DocumentShare> DocumentShares { get; set; }
    public DbSet<DocumentTag> DocumentTags { get; set; }
    public DbSet<AIConfiguration> AIConfigurations { get; set; }
    
    // Conversazioni e messaggi per RAG
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }

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
                v => v == null ? null : string.Join(",", v.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))),
                v => v == null ? null : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture))
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

        // Conversation configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Tags).HasMaxLength(500);
            
            // Relazione con User
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indici per performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LastMessageAt);
            entity.HasIndex(e => new { e.UserId, e.IsArchived, e.LastMessageAt });
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Content).IsRequired();
            
            // Relazione con Conversation
            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Configura ReferencedDocumentIds come JSON
            entity.Property(e => e.ReferencedDocumentIds)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<int>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<int>())
                .HasColumnType("nvarchar(max)");
            
            // Indici per performance
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.Timestamp);
        });

        // DocumentChunk configuration
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChunkText).IsRequired();
            
            // Configure vector column for chunk embeddings
            var converter = new ValueConverter<float[]?, string?>(
                v => v == null ? null : string.Join(",", v.Select(f => f.ToString(System.Globalization.CultureInfo.InvariantCulture))),
                v => v == null ? null : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => float.Parse(s, System.Globalization.CultureInfo.InvariantCulture))
                    .ToArray()
            );
            
            entity.Property(e => e.ChunkEmbedding)
                .HasColumnType("nvarchar(max)")  // Temporarily use nvarchar(max) until database supports VECTOR
                .HasConversion(converter)
                .IsRequired(false);
            
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
