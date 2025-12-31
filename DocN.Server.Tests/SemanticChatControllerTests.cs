using Xunit;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Server.Controllers;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Mvc;
using DocN.Core.Interfaces;
using DocN.Data.Models;

namespace DocN.Server.Tests;

public class SemanticChatControllerTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private SemanticChatController CreateController(
        ApplicationDbContext context,
        ISemanticRAGService? ragService = null)
    {
        var loggerMock = new Mock<ILogger<SemanticChatController>>();
        var ragServiceMock = ragService != null 
            ? Mock.Get(ragService) 
            : new Mock<ISemanticRAGService>();

        return new SemanticChatController(
            ragServiceMock.Object,
            context,
            loggerMock.Object);
    }

    [Fact]
    public async Task Query_WithNoAIProviderConfigured_ReturnsServiceUnavailable()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var ragServiceMock = new Mock<ISemanticRAGService>();
        ragServiceMock
            .Setup(s => s.GenerateResponseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<List<int>?>(),
                It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException(
                "Nessun provider AI ha una chiave API valida configurata. " +
                "Configura almeno un provider (Gemini, OpenAI, o Azure OpenAI) " +
                "tramite l'interfaccia utente o appsettings.json."));

        var controller = CreateController(context, ragServiceMock.Object);

        var request = new SemanticChatRequest
        {
            Message = "Test query",
            UserId = "test-user"
        };

        // Act
        var result = await controller.Query(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, objectResult.StatusCode);
        
        // Verify error response structure
        dynamic? value = objectResult.Value;
        Assert.NotNull(value);
        
        var errorProp = value.GetType().GetProperty("error");
        var errorCodeProp = value.GetType().GetProperty("errorCode");
        
        Assert.NotNull(errorProp);
        Assert.NotNull(errorCodeProp);
        
        var errorValue = errorProp.GetValue(value)?.ToString();
        var errorCodeValue = errorCodeProp.GetValue(value)?.ToString();
        
        Assert.Contains("AI provider not configured", errorValue);
        Assert.Equal("AI_PROVIDER_NOT_CONFIGURED", errorCodeValue);
    }

    [Fact]
    public async Task Query_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var ragServiceMock = new Mock<ISemanticRAGService>();
        ragServiceMock
            .Setup(s => s.GenerateResponseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<List<int>?>(),
                It.IsAny<int>()))
            .ReturnsAsync(new SemanticRAGResponse
            {
                Answer = "Test answer",
                ConversationId = 1,
                SourceDocuments = new List<RelevantDocumentResult>(),
                ResponseTimeMs = 100,
                FromCache = false,
                Metadata = new Dictionary<string, object>()
            });

        var controller = CreateController(context, ragServiceMock.Object);

        var request = new SemanticChatRequest
        {
            Message = "Test query",
            UserId = "test-user"
        };

        // Act
        var result = await controller.Query(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SemanticChatResponse>(okResult.Value);
        Assert.Equal("Test answer", response.Answer);
        Assert.Equal(1, response.ConversationId);
    }

    [Fact]
    public async Task Query_WithEmptyMessage_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var controller = CreateController(context);

        var request = new SemanticChatRequest
        {
            Message = "",
            UserId = "test-user"
        };

        // Act
        var result = await controller.Query(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Query_WithEmptyUserId_ReturnsBadRequest()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        var controller = CreateController(context);

        var request = new SemanticChatRequest
        {
            Message = "Test query",
            UserId = ""
        };

        // Act
        var result = await controller.Query(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Query_WithGenericException_ReturnsInternalServerError()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        var ragServiceMock = new Mock<ISemanticRAGService>();
        ragServiceMock
            .Setup(s => s.GenerateResponseAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<List<int>?>(),
                It.IsAny<int>()))
            .ThrowsAsync(new Exception("Generic error"));

        var controller = CreateController(context, ragServiceMock.Object);

        var request = new SemanticChatRequest
        {
            Message = "Test query",
            UserId = "test-user"
        };

        // Act
        var result = await controller.Query(request);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
    }
}
