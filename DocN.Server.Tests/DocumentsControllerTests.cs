using Xunit;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Models;
using DocN.Server.Controllers;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Mvc;
using DocN.Data.Services;

namespace DocN.Server.Tests;

public class DocumentsControllerTests
{
    private DocArcContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<DocArcContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new DocArcContext(options);
    }

    private DocumentsController CreateController(DocArcContext context)
    {
        var loggerMock = new Mock<ILogger<DocumentsController>>();
        var chunkingServiceMock = new Mock<IChunkingService>();
        var batchProcessingServiceMock = new Mock<IBatchProcessingService>();
        var embeddingServiceMock = new Mock<IEmbeddingService>();

        // Setup default behavior for chunking service to return empty list
        chunkingServiceMock.Setup(s => s.ChunkDocument(It.IsAny<Document>()))
            .Returns(new List<DocumentChunk>());

        return new DocumentsController(
            context, 
            loggerMock.Object, 
            chunkingServiceMock.Object,
            batchProcessingServiceMock.Object,
            embeddingServiceMock.Object);
    }

    [Fact]
    public async Task GetDocuments_ReturnsAllDocuments_IncludingThoseWithoutVectors()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        // Add test documents - mix of with and without vectors
        context.Documents.AddRange(
            new Document
            {
                FileName = "doc_with_vector.pdf",
                EmbeddingVector = new float[] { 1, 2, 3 },
                UploadedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Document
            {
                FileName = "doc_without_vector_1.pdf",
                EmbeddingVector = null, // NO VECTOR
                UploadedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Document
            {
                FileName = "doc_without_vector_2.pdf",
                EmbeddingVector = null, // NO VECTOR
                UploadedAt = DateTime.UtcNow.AddDays(-3)
            }
        );
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        // Act
        var result = await controller.GetDocuments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var documents = Assert.IsAssignableFrom<IEnumerable<Document>>(okResult.Value);
        var documentList = documents.ToList();
        
        // CRITICAL: All 3 documents should be returned, including the 2 without vectors
        Assert.Equal(3, documentList.Count);
        
        // Verify that documents without vectors are included
        var docsWithoutVectors = documentList.Where(d => d.EmbeddingVector == null).ToList();
        Assert.Equal(2, docsWithoutVectors.Count);
        
        // Verify that documents with vectors are included
        var docsWithVectors = documentList.Where(d => d.EmbeddingVector != null).ToList();
        Assert.Equal(1, docsWithVectors.Count);
    }

    [Fact]
    public async Task GetDocuments_ReturnsEmptyList_WhenNoDocuments()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var controller = CreateController(context);

        // Act
        var result = await controller.GetDocuments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var documents = Assert.IsAssignableFrom<IEnumerable<Document>>(okResult.Value);
        Assert.Empty(documents);
    }

    [Fact]
    public async Task GetDocument_ReturnsDocument_EvenWithoutVector()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var testDocument = new Document
        {
            FileName = "test_document.pdf",
            EmbeddingVector = null, // NO VECTOR
            UploadedAt = DateTime.UtcNow
        };
        context.Documents.Add(testDocument);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        // Act
        var result = await controller.GetDocument(testDocument.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var document = Assert.IsType<Document>(okResult.Value);
        Assert.Equal(testDocument.Id, document.Id);
        Assert.Equal("test_document.pdf", document.FileName);
        Assert.Null(document.EmbeddingVector); // Confirm vector is null
    }

    [Fact]
    public async Task UpdateDocument_UpdatesDocumentSuccessfully()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var originalDocument = new Document
        {
            FileName = "original.pdf",
            ContentType = "application/pdf",
            FileSize = 1024,
            ExtractedText = "Original text",
            ActualCategory = "Original Category",
            UploadedAt = DateTime.UtcNow
        };
        context.Documents.Add(originalDocument);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var updatedDocument = new Document
        {
            Id = originalDocument.Id,
            FileName = "updated.pdf",
            ContentType = "application/pdf",
            FileSize = 2048,
            ExtractedText = "Updated text",
            ActualCategory = "Updated Category"
        };

        // Act
        var result = await controller.UpdateDocument(originalDocument.Id, updatedDocument);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var document = Assert.IsType<Document>(okResult.Value);
        Assert.Equal("updated.pdf", document.FileName);
        Assert.Equal(2048, document.FileSize);
        Assert.Equal("Updated text", document.ExtractedText);
        Assert.Equal("Updated Category", document.ActualCategory);
    }

    [Fact]
    public async Task UpdateDocument_ReturnsNotFound_WhenDocumentDoesNotExist()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var controller = CreateController(context);

        var updatedDocument = new Document
        {
            Id = 999,
            FileName = "nonexistent.pdf"
        };

        // Act
        var result = await controller.UpdateDocument(999, updatedDocument);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateDocument_ReturnsBadRequest_WhenIdMismatch()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var controller = CreateController(context);

        var document = new Document
        {
            Id = 1,
            FileName = "test.pdf"
        };

        // Act
        var result = await controller.UpdateDocument(2, document);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
