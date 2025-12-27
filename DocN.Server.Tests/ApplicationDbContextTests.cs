using Xunit;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace DocN.Server.Tests;

public class ApplicationDbContextTests
{
    private ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void ApplicationDbContext_CanBeInitialized_WithoutException()
    {
        // This test verifies that the DbContext can be created without
        // throwing a NullReferenceException during model initialization
        
        // Act & Assert - should not throw any exception
        using var context = CreateInMemoryContext();
        
        // Verify the context was created successfully
        Assert.NotNull(context);
        Assert.NotNull(context.Model);
    }

    [Fact]
    public async Task Message_WithReferencedDocumentIds_CanBeSavedAndRetrieved()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        // Create a test user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser@test.com",
            Email = "testuser@test.com",
            FirstName = "Test",
            LastName = "User"
        };
        context.Users.Add(user);
        
        // Create a conversation
        var conversation = new Conversation
        {
            UserId = user.Id,
            Title = "Test Conversation",
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        
        // Create a message with ReferencedDocumentIds
        var message = new Message
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = "Test message",
            ReferencedDocumentIds = new List<int> { 1, 5, 12 },
            Timestamp = DateTime.UtcNow
        };
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        // Act - retrieve the message
        var retrievedMessage = await context.Messages
            .FirstOrDefaultAsync(m => m.Id == message.Id);

        // Assert
        Assert.NotNull(retrievedMessage);
        Assert.NotNull(retrievedMessage.ReferencedDocumentIds);
        Assert.Equal(3, retrievedMessage.ReferencedDocumentIds.Count);
        Assert.Contains(1, retrievedMessage.ReferencedDocumentIds);
        Assert.Contains(5, retrievedMessage.ReferencedDocumentIds);
        Assert.Contains(12, retrievedMessage.ReferencedDocumentIds);
    }

    [Fact]
    public async Task Message_WithEmptyReferencedDocumentIds_CanBeSavedAndRetrieved()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        
        // Create a test user
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser2@test.com",
            Email = "testuser2@test.com",
            FirstName = "Test",
            LastName = "User"
        };
        context.Users.Add(user);
        
        // Create a conversation
        var conversation = new Conversation
        {
            UserId = user.Id,
            Title = "Test Conversation 2",
            CreatedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow
        };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync();
        
        // Create a message with empty ReferencedDocumentIds
        var message = new Message
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = "Test response",
            ReferencedDocumentIds = new List<int>(), // Empty list
            Timestamp = DateTime.UtcNow
        };
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        // Act - retrieve the message
        var retrievedMessage = await context.Messages
            .FirstOrDefaultAsync(m => m.Id == message.Id);

        // Assert
        Assert.NotNull(retrievedMessage);
        Assert.NotNull(retrievedMessage.ReferencedDocumentIds);
        Assert.Empty(retrievedMessage.ReferencedDocumentIds);
    }
}
