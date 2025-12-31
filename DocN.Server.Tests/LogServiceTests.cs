using Xunit;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Services;
using DocN.Data.Models;

namespace DocN.Server.Tests;

public class LogServiceTests : IDisposable
{
    private readonly DocArcContext _context;
    private readonly LogService _logService;

    public LogServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<DocArcContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new DocArcContext(options);

        // Create service
        _logService = new LogService(_context);
    }

    [Fact]
    public async Task GetUploadLogsAsync_ReturnsAllLogsWhenUserIdIsNull()
    {
        // Arrange
        await _logService.LogInfoAsync("Upload", "Test upload 1", userId: "user1");
        await _logService.LogInfoAsync("Upload", "Test upload 2", userId: "user2");
        await _logService.LogInfoAsync("Embedding", "Test embedding", userId: null);

        // Act
        var logs = await _logService.GetUploadLogsAsync(userId: null);

        // Assert
        Assert.Equal(3, logs.Count);
    }

    [Fact]
    public async Task GetUploadLogsAsync_ReturnsAllLogsWhenUserIdIsEmptyString()
    {
        // Arrange - This is the bug scenario
        await _logService.LogInfoAsync("Upload", "Test upload 1", userId: "user1");
        await _logService.LogInfoAsync("Upload", "Test upload 2", userId: "user2");
        await _logService.LogInfoAsync("Category", "Test category", userId: null);

        // Act - When user is not authenticated, currentUserId is set to empty string
        var logs = await _logService.GetUploadLogsAsync(userId: string.Empty);

        // Assert - Should return all logs, not filter by empty string
        Assert.Equal(3, logs.Count);
    }

    [Fact]
    public async Task GetUploadLogsAsync_FiltersLogsBySpecificUserId()
    {
        // Arrange
        await _logService.LogInfoAsync("Upload", "Test upload 1", userId: "user1");
        await _logService.LogInfoAsync("Upload", "Test upload 2", userId: "user2");
        await _logService.LogInfoAsync("Tag", "Test tag", userId: "user1");

        // Act
        var logs = await _logService.GetUploadLogsAsync(userId: "user1");

        // Assert
        Assert.Equal(2, logs.Count);
        Assert.All(logs, log => Assert.Equal("user1", log.UserId));
    }

    [Fact]
    public async Task GetUploadLogsAsync_OnlyReturnsUploadRelatedCategories()
    {
        // Arrange
        await _logService.LogInfoAsync("Upload", "Test upload", userId: "user1");
        await _logService.LogInfoAsync("NotUploadCategory", "Should not be returned", userId: "user1");
        await _logService.LogInfoAsync("Embedding", "Test embedding", userId: "user1");

        // Act
        var logs = await _logService.GetUploadLogsAsync(userId: "user1");

        // Assert
        Assert.Equal(2, logs.Count);
        Assert.DoesNotContain(logs, log => log.Category == "NotUploadCategory");
    }

    [Fact]
    public async Task GetUploadLogsAsync_FiltersLogsByDateRange()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddDays(-2);
        var recentDate = DateTime.UtcNow.AddHours(-1);
        
        // Add old log manually
        _context.LogEntries.Add(new LogEntry
        {
            Timestamp = oldDate,
            Level = "Info",
            Category = "Upload",
            Message = "Old upload",
            UserId = "user1"
        });
        await _context.SaveChangesAsync();
        
        await _logService.LogInfoAsync("Upload", "Recent upload", userId: "user1");

        // Act - Get logs from last 24 hours
        var logs = await _logService.GetUploadLogsAsync(userId: "user1", fromDate: DateTime.UtcNow.AddHours(-24));

        // Assert - Should only return recent log
        Assert.Single(logs);
        Assert.Equal("Recent upload", logs[0].Message);
    }

    [Fact]
    public async Task GetLogsAsync_HandlesEmptyUserIdCorrectly()
    {
        // Arrange
        await _logService.LogInfoAsync("TestCategory", "Test message 1", userId: "user1");
        await _logService.LogInfoAsync("TestCategory", "Test message 2", userId: "user2");

        // Act - Empty string should not filter
        var logs = await _logService.GetLogsAsync(userId: string.Empty);

        // Assert
        Assert.Equal(2, logs.Count);
    }

    [Fact]
    public async Task GetLogsAsync_HandlesWhitespaceUserIdCorrectly()
    {
        // Arrange
        await _logService.LogInfoAsync("TestCategory", "Test message 1", userId: "user1");
        await _logService.LogInfoAsync("TestCategory", "Test message 2", userId: "user2");

        // Act - Whitespace should not filter
        var logs = await _logService.GetLogsAsync(userId: "   ");

        // Assert
        Assert.Equal(2, logs.Count);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
