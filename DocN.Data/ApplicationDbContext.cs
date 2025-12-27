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

    public DbSet<Tenant> Tenants { get; set; }
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

        // Tenant configuration
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.Name);
        });

        // ApplicationUser - Tenant relationship
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.TenantId);
        });

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ExtractedText).HasMaxLength(int.MaxValue);
            entity.Property(e => e.CategoryReasoning).HasMaxLength(2000);
            
            // AI Tags JSON field
            entity.Property(e => e.AITagsJson).HasColumnType("nvarchar(max)");
            
            // Configure vector column for SQL Server 2025 native VECTOR type
            // Note: Using varbinary(max) as intermediate type since EF Core doesn't support VECTOR natively
            // The actual column type in database should be VECTOR(1536)
            // We convert float[] to byte[] for storage
            var vectorConverter = new ValueConverter<float[]?, byte[]?>(
                v => v == null ? null : ConvertFloatArrayToBytes(v),
                v => v == null ? null : ConvertBytesToFloatArray(v)
            );
            
            entity.Property(e => e.EmbeddingVector)
                .HasColumnType("varbinary(max)")  // Use varbinary as EF Core compatible type
                .HasConversion(vectorConverter)
                .IsRequired(false);
            
            // Index for performance with large number of documents
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.UploadedAt);
            entity.HasIndex(e => e.Visibility);
            entity.HasIndex(e => e.SuggestedCategory);
            entity.HasIndex(e => e.ActualCategory);
            entity.HasIndex(e => e.TenantId);
            
            // Relationship with owner
            entity.HasOne(e => e.Owner)
                .WithMany(u => u.Documents)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relationship with tenant
            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Documents)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
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
            // Use explicit ValueConverter to avoid EF Core collection mapping issues
            var referencedDocIdsConverter = new ValueConverter<List<int>, string>(
                v => System.Text.Json.JsonSerializer.Serialize(v),
                v => System.Text.Json.JsonSerializer.Deserialize<List<int>>(v) ?? new List<int>()
            );
            
            entity.Property(e => e.ReferencedDocumentIds)
                .HasConversion(referencedDocIdsConverter)
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
            // Note: Using varbinary(max) as intermediate type since EF Core doesn't support VECTOR natively
            // The actual column type in database should be VECTOR(1536)
            var chunkVectorConverter = new ValueConverter<float[]?, byte[]?>(
                v => v == null ? null : ConvertFloatArrayToBytes(v),
                v => v == null ? null : ConvertBytesToFloatArray(v)
            );
            
            entity.Property(e => e.ChunkEmbedding)
                .HasColumnType("varbinary(max)")  // Use varbinary as EF Core compatible type
                .HasConversion(chunkVectorConverter)
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
    
    // Helper methods for VECTOR type conversion
    private static byte[] ConvertFloatArrayToBytes(float[] floats)
    {
        byte[] bytes = new byte[floats.Length * sizeof(float)];
        Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);
        return bytes;
    }
    
    private static float[] ConvertBytesToFloatArray(byte[] bytes)
    {
        float[] floats = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
        return floats;
    }
}
