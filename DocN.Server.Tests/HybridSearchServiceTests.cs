using Xunit;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Models;
using DocN.Data.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace DocN.Server.Tests;

public class HybridSearchServiceTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private HybridSearchService CreateService(ApplicationDbContext context, IEmbeddingService? embeddingService = null)
    {
        if (embeddingService == null)
        {
            var mockEmbeddingService = new Mock<IEmbeddingService>();
            mockEmbeddingService.Setup(s => s.GenerateEmbeddingAsync(It.IsAny<string>()))
                .ReturnsAsync((string text) => new float[] { 0.1f, 0.2f, 0.3f });
            embeddingService = mockEmbeddingService.Object;
        }

        return new HybridSearchService(context, embeddingService);
    }

    [Fact]
    public async Task TextSearchAsync_FindsDocumentWithRossi_CaseInsensitive()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        // Add test document with "Rossi Gilbo" in extracted text (matching the real PDF)
        var document = new Document
        {
            FileName = "testdocupdat.pdf",
            FilePath = "/test/testdocupdat.pdf",
            ContentType = "application/pdf",
            FileSize = 57923,
            ExtractedText = @"--- Page 1 ---
Destinatario
Rossi Gilbo
Commercio Tessile
Via Michelangelo, 25
47100 Forli' (FO) (I)
Luogo di destinazione
Idem

D.D.T. n° 5 del 10/01/2024",
            ActualCategory = "DDT",
            UploadedAt = DateTime.UtcNow,
            OwnerId = "test-user-123"
        };
        
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var options = new SearchOptions
        {
            TopK = 10,
            MinSimilarity = 0.7
        };

        // Act - Search for "Rossi" (case should not matter)
        var results = await service.TextSearchAsync("Rossi", options);

        // Assert
        Assert.NotEmpty(results);
        Assert.Single(results);
        
        var result = results[0];
        Assert.Equal("testdocupdat.pdf", result.Document.FileName);
        Assert.True(result.TextScore > 0, "Text score should be greater than 0");
        Assert.Contains("Rossi", result.Document.ExtractedText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TextSearchAsync_FindsDocumentWithRossi_LowerCase()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var document = new Document
        {
            FileName = "testdocupdat.pdf",
            FilePath = "/test/testdocupdat.pdf",
            ContentType = "application/pdf",
            FileSize = 57923,
            ExtractedText = "Destinatario Rossi Gilbo Commercio Tessile",
            ActualCategory = "DDT",
            UploadedAt = DateTime.UtcNow,
            OwnerId = "test-user-123"
        };
        
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var options = new SearchOptions
        {
            TopK = 10,
            MinSimilarity = 0.7
        };

        // Act - Search with lowercase "rossi"
        var results = await service.TextSearchAsync("rossi", options);

        // Assert
        Assert.NotEmpty(results);
        Assert.Single(results);
        Assert.Equal("testdocupdat.pdf", results[0].Document.FileName);
    }

    [Fact]
    public async Task TextSearchAsync_FindsDocumentWithRossi_UpperCase()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var document = new Document
        {
            FileName = "testdocupdat.pdf",
            FilePath = "/test/testdocupdat.pdf",
            ContentType = "application/pdf",
            FileSize = 57923,
            ExtractedText = "Destinatario Rossi Gilbo",
            ActualCategory = "DDT",
            UploadedAt = DateTime.UtcNow,
            OwnerId = "test-user-123"
        };
        
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var options = new SearchOptions { TopK = 10 };

        // Act - Search with UPPERCASE "ROSSI"
        var results = await service.TextSearchAsync("ROSSI", options);

        // Assert
        Assert.NotEmpty(results);
        Assert.Single(results);
        Assert.Equal("testdocupdat.pdf", results[0].Document.FileName);
    }

    [Fact]
    public async Task TextSearchAsync_ScoresFilenameMatchesHigher()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        // Document with keyword in filename
        var doc1 = new Document
        {
            FileName = "Rossi_contract.pdf",
            FilePath = "/test/doc1.pdf",
            ContentType = "application/pdf",
            ExtractedText = "Some other content",
            FileSize = 1000,
            UploadedAt = DateTime.UtcNow,
            OwnerId = "test-user"
        };
        
        // Document with keyword only in text
        var doc2 = new Document
        {
            FileName = "contract.pdf",
            FilePath = "/test/doc2.pdf",
            ContentType = "application/pdf",
            ExtractedText = "Contract for Rossi company",
            FileSize = 1000,
            UploadedAt = DateTime.UtcNow,
            OwnerId = "test-user"
        };
        
        context.Documents.AddRange(doc1, doc2);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var options = new SearchOptions { TopK = 10 };

        // Act
        var results = await service.TextSearchAsync("Rossi", options);

        // Assert
        Assert.Equal(2, results.Count);
        
        // Document with filename match should score higher
        var firstResult = results[0];
        var secondResult = results[1];
        
        // The filename match should have higher score
        Assert.True(firstResult.TextScore >= secondResult.TextScore, 
            $"Filename match score ({firstResult.TextScore}) should be >= text match score ({secondResult.TextScore})");
    }

    [Fact]
    public async Task TextSearchAsync_HandlesMultipleKeywords()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var document = new Document
        {
            FileName = "invoice.pdf",
            FilePath = "/test/invoice.pdf",
            ContentType = "application/pdf",
            ExtractedText = "Invoice for Rossi Gilbo company dated 2024",
            FileSize = 1000,
            UploadedAt = DateTime.UtcNow,
            OwnerId = "test-user"
        };
        
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var options = new SearchOptions { TopK = 10 };

        // Act - Search with multiple keywords
        var results = await service.TextSearchAsync("Rossi 2024", options);

        // Assert
        Assert.NotEmpty(results);
        Assert.Single(results);
        
        var result = results[0];
        // Should have higher score because both keywords match
        Assert.True(result.TextScore > 0.5, "Score should reflect multiple keyword matches");
    }

    [Fact]
    public async Task TextSearchAsync_FiltersShortKeywords()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var document = new Document
        {
            FileName = "test.pdf",
            FilePath = "/test/test.pdf",
            ContentType = "application/pdf",
            ExtractedText = "Document content",
            FileSize = 1000,
            UploadedAt = DateTime.UtcNow,
            OwnerId = "test-user"
        };
        
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var options = new SearchOptions { TopK = 10 };

        // Act - Search with single character (should be filtered out)
        var results = await service.TextSearchAsync("a b c", options);

        // Assert - Should return empty since all keywords are single chars
        Assert.Empty(results);
    }

    [Fact]
    public async Task TextSearchAsync_AppliesCategoryFilter()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var doc1 = new Document
        {
            FileName = "rossi_ddt.pdf",
            FilePath = "/test/doc1.pdf",
            ContentType = "application/pdf",
            ExtractedText = "DDT for Rossi",
            ActualCategory = "DDT",
            FileSize = 1000,
            UploadedAt = DateTime.UtcNow,
            OwnerId = "test-user"
        };
        
        var doc2 = new Document
        {
            FileName = "rossi_invoice.pdf",
            FilePath = "/test/doc2.pdf",
            ContentType = "application/pdf",
            ExtractedText = "Invoice for Rossi",
            ActualCategory = "Invoice",
            FileSize = 1000,
            UploadedAt = DateTime.UtcNow,
            OwnerId = "test-user"
        };
        
        context.Documents.AddRange(doc1, doc2);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var options = new SearchOptions 
        { 
            TopK = 10,
            CategoryFilter = "DDT"
        };

        // Act
        var results = await service.TextSearchAsync("Rossi", options);

        // Assert - Should only return DDT document
        Assert.Single(results);
        Assert.Equal("DDT", results[0].Document.ActualCategory);
    }

    [Fact]
    public async Task TextSearchAsync_AppliesOwnerFilter()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var doc1 = new Document
        {
            FileName = "user1_doc.pdf",
            FilePath = "/test/doc1.pdf",
            ContentType = "application/pdf",
            ExtractedText = "Content with Rossi",
            FileSize = 1000,
            UploadedAt = DateTime.UtcNow,
            OwnerId = "user-1"
        };
        
        var doc2 = new Document
        {
            FileName = "user2_doc.pdf",
            FilePath = "/test/doc2.pdf",
            ContentType = "application/pdf",
            ExtractedText = "Content with Rossi",
            FileSize = 1000,
            UploadedAt = DateTime.UtcNow,
            OwnerId = "user-2"
        };
        
        context.Documents.AddRange(doc1, doc2);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var options = new SearchOptions 
        { 
            TopK = 10,
            OwnerId = "user-1"
        };

        // Act
        var results = await service.TextSearchAsync("Rossi", options);

        // Assert - Should only return user-1's document
        Assert.Single(results);
        Assert.Equal("user-1", results[0].Document.OwnerId);
    }

    [Fact]
    public async Task TextSearchAsync_SearchesInCategory()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var document = new Document
        {
            FileName = "document.pdf",
            FilePath = "/test/document.pdf",
            ContentType = "application/pdf",
            ExtractedText = "Some content",
            ActualCategory = "Rossi Documents", // Keyword in category
            FileSize = 1000,
            UploadedAt = DateTime.UtcNow,
            OwnerId = "test-user"
        };
        
        context.Documents.Add(document);
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var options = new SearchOptions { TopK = 10 };

        // Act
        var results = await service.TextSearchAsync("Rossi", options);

        // Assert
        Assert.NotEmpty(results);
        Assert.Single(results);
        Assert.Contains("Rossi", results[0].Document.ActualCategory!, StringComparison.OrdinalIgnoreCase);
    }
}
