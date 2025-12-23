using Microsoft.EntityFrameworkCore;
using DocN.Data.Models;

namespace DocN.Data;

public class DocArcContext : DbContext
{
    public DocArcContext(DbContextOptions<DocArcContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FilePath).HasMaxLength(1000);
            entity.Property(e => e.SuggestedCategory).HasMaxLength(200);
            entity.Property(e => e.ActualCategory).HasMaxLength(200);
            entity.Property(e => e.EmbeddingVector).IsRequired(false); // Vector is optional
            entity.Property(e => e.UploadedAt).IsRequired();
        });
    }
}
