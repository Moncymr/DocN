using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
    public DbSet<SimilarDocument> SimilarDocuments { get; set; }
    public DbSet<AIConfiguration> AIConfigurations { get; set; }
    
    // Conversazioni e messaggi per RAG
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    
    // Agent configuration and templates
    public DbSet<AgentConfiguration> AgentConfigurations { get; set; }
    public DbSet<AgentTemplate> AgentTemplates { get; set; }
    public DbSet<AgentUsageLog> AgentUsageLogs { get; set; }

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
            
            // Configure vector columns for SQL Server 2025 native VECTOR type
            // Two separate fields for different dimensions
            
            // 768-dimensional vector for Gemini and similar providers
            entity.Property(e => e.EmbeddingVector768)
                .HasColumnType("VECTOR(768)")
                .IsRequired(false);
            
            // 1536-dimensional vector for OpenAI and similar providers
            entity.Property(e => e.EmbeddingVector1536)
                .HasColumnType("VECTOR(1536)")
                .IsRequired(false);
            
            // Configure EmbeddingDimension to track which field is used
            entity.Property(e => e.EmbeddingDimension)
                .IsRequired(false);
            
            // Ignore the calculated property EmbeddingVector (it's not mapped to database)
            entity.Ignore(e => e.EmbeddingVector);
            
            // Index for performance with large number of documents
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.UploadedAt);
            entity.HasIndex(e => e.Visibility);
            entity.HasIndex(e => e.SuggestedCategory);
            entity.HasIndex(e => e.ActualCategory);
            entity.HasIndex(e => e.TenantId);
            
            // Relationship with owner (optional - documents can exist without owner)
            entity.HasOne(e => e.Owner)
                .WithMany(u => u.Documents)
                .HasForeignKey(e => e.OwnerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            
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
            // Must explicitly configure to prevent EF Core 10 from treating this as a primitive collection
            var referencedDocIdsConverter = new ValueConverter<List<int>, string>(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => string.IsNullOrEmpty(v) ? new List<int>() : System.Text.Json.JsonSerializer.Deserialize<List<int>>(v) ?? new List<int>()
            );
            
            var referencedDocIdsComparer = new ValueComparer<List<int>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? new List<int>() : c.ToList()
            );
            
            var property = entity.Property(e => e.ReferencedDocumentIds)
                .HasConversion(referencedDocIdsConverter)
                .HasColumnType("nvarchar(max)");
            
            property.Metadata.SetValueComparer(referencedDocIdsComparer);
            
            // Explicitly set the provider type to string to prevent EF Core 10 
            // from treating this as a primitive collection
            property.Metadata.SetProviderClrType(typeof(string));
            
            // Indici per performance
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.Timestamp);
        });

        // DocumentChunk configuration
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChunkText).IsRequired();
            
            // Configure vector columns for chunk embeddings
            // Two separate fields for different dimensions
            
            // 768-dimensional vector for Gemini and similar providers
            entity.Property(e => e.ChunkEmbedding768)
                .HasColumnType("VECTOR(768)")
                .IsRequired(false);
            
            // 1536-dimensional vector for OpenAI and similar providers
            entity.Property(e => e.ChunkEmbedding1536)
                .HasColumnType("VECTOR(1536)")
                .IsRequired(false);
            
            // Configure EmbeddingDimension to track which field is used
            entity.Property(e => e.EmbeddingDimension)
                .IsRequired(false);
            
            // Ignore the calculated property ChunkEmbedding (it's not mapped to database)
            entity.Ignore(e => e.ChunkEmbedding);
            
            // Relationship with Document
            entity.HasOne(e => e.Document)
                .WithMany()
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for performance
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex });
        });

        // SimilarDocument configuration
        modelBuilder.Entity<SimilarDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SimilarityScore).IsRequired();
            entity.Property(e => e.RelevantChunk).HasMaxLength(1000);
            entity.Property(e => e.Rank).IsRequired();
            
            // Relationship with source document
            entity.HasOne(e => e.SourceDocument)
                .WithMany()
                .HasForeignKey(e => e.SourceDocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relationship with similar document (no cascade to avoid cycles)
            entity.HasOne(e => e.SimilarDocumentRef)
                .WithMany()
                .HasForeignKey(e => e.SimilarDocumentId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes for performance
            entity.HasIndex(e => e.SourceDocumentId);
            entity.HasIndex(e => new { e.SourceDocumentId, e.Rank });
            entity.HasIndex(e => new { e.SourceDocumentId, e.SimilarityScore });
        });

        // AgentConfiguration configuration
        modelBuilder.Entity<AgentConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.SystemPrompt).IsRequired();
            entity.Property(e => e.CustomInstructions).HasMaxLength(2000);
            entity.Property(e => e.CategoryFilter).HasMaxLength(1000);
            entity.Property(e => e.TagFilter).HasMaxLength(1000);
            
            // Relationships
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Template)
                .WithMany(t => t.Agents)
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Indexes
            entity.HasIndex(e => e.OwnerId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.AgentType);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.TenantId, e.IsActive });
        });

        // AgentTemplate configuration
        modelBuilder.Entity<AgentTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Category).HasMaxLength(100);
            entity.Property(e => e.DefaultSystemPrompt).IsRequired();
            entity.Property(e => e.DefaultParametersJson).HasMaxLength(4000);
            
            // Relationships
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Indexes
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.AgentType);
            entity.HasIndex(e => e.IsBuiltIn);
            entity.HasIndex(e => e.IsActive);
        });

        // AgentUsageLog configuration
        modelBuilder.Entity<AgentUsageLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Query).IsRequired();
            entity.Property(e => e.Response).HasMaxLength(int.MaxValue);
            
            // Relationships
            entity.HasOne(e => e.AgentConfiguration)
                .WithMany()
                .HasForeignKey(e => e.AgentConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Indexes for analytics
            entity.HasIndex(e => e.AgentConfigurationId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.AgentConfigurationId, e.CreatedAt });
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
