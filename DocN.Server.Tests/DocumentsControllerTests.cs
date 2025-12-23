using Xunit;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Models;
using DocN.Server.Controllers;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Mvc;

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

    [Fact]
    public async Task GetDocuments_ReturnsAllDocuments_IncludingThoseWithoutVectors()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<DocumentsController>>();
        
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

        var controller = new DocumentsController(context, loggerMock.Object);

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
        var loggerMock = new Mock<ILogger<DocumentsController>>();
        var controller = new DocumentsController(context, loggerMock.Object);

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
        var loggerMock = new Mock<ILogger<DocumentsController>>();
        
        var testDocument = new Document
        {
            FileName = "test_document.pdf",
            EmbeddingVector = null, // NO VECTOR
            UploadedAt = DateTime.UtcNow
        };
        context.Documents.Add(testDocument);
        await context.SaveChangesAsync();

        var controller = new DocumentsController(context, loggerMock.Object);

        // Act
        var result = await controller.GetDocument(testDocument.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var document = Assert.IsType<Document>(okResult.Value);
        Assert.Equal(testDocument.Id, document.Id);
        Assert.Equal("test_document.pdf", document.FileName);
        Assert.Null(document.EmbeddingVector); // Confirm vector is null
    }
}
