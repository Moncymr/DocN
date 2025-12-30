using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using DocN.Data;
using DocN.Data.Services;
using DocN.Data.Models;
using System.Security.Claims;

namespace DocN.Server.Tests;

public class AuditServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<ILogger<AuditService>> _loggerMock;
    private readonly AuditService _auditService;

    public AuditServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup mocks
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<AuditService>>();

        // Create service
        _auditService = new AuditService(_context, _httpContextAccessorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task LogAsync_CreatesAuditLog()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers["User-Agent"] = "Test Agent";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        await _auditService.LogAsync("TestAction", "TestResource", "123", new { test = "data" });

        // Assert
        var logs = await _context.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("TestAction", logs[0].Action);
        Assert.Equal("TestResource", logs[0].ResourceType);
        Assert.Equal("123", logs[0].ResourceId);
        Assert.Equal("127.0.0.1", logs[0].IpAddress);
    }

    [Fact]
    public async Task LogAuthenticationAsync_CreatesAuthenticationLog()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        await _auditService.LogAuthenticationAsync("UserLogin", "user123", "testuser", true);

        // Assert
        var logs = await _context.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("UserLogin", logs[0].Action);
        Assert.Equal("Authentication", logs[0].ResourceType);
        Assert.Equal("user123", logs[0].UserId);
        Assert.Equal("testuser", logs[0].Username);
        Assert.True(logs[0].Success);
    }

    [Fact]
    public async Task GetAuditLogsAsync_FiltersCorrectly()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        await _auditService.LogAsync("Action1", "Document", "1");
        await _auditService.LogAsync("Action2", "Document", "2");
        await _auditService.LogAsync("Action1", "User", "3");

        // Act
        var logs = await _auditService.GetAuditLogsAsync(
            action: "Action1",
            resourceType: "Document");

        // Assert
        Assert.Single(logs);
        Assert.Equal("Action1", logs[0].Action);
        Assert.Equal("Document", logs[0].ResourceType);
    }

    [Fact]
    public async Task LogAsync_HandlesExceptionGracefully()
    {
        // Arrange - create a context that will fail
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext)null!);

        // Act - should not throw
        await _auditService.LogAsync("TestAction", "TestResource");

        // Assert - log should be created even without HTTP context
        var logs = await _context.AuditLogs.ToListAsync();
        Assert.Single(logs);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
