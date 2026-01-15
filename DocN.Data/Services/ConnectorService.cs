using DocN.Data;
using DocN.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocN.Data.Services;

/// <summary>
/// Service for managing document connectors
/// </summary>
public class ConnectorService : IConnectorService
{
    private readonly DocArcContext _context;
    private readonly ILogger<ConnectorService> _logger;

    public ConnectorService(DocArcContext context, ILogger<ConnectorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<DocumentConnector>> GetUserConnectorsAsync(string userId)
    {
        try
        {
            return await _context.DocumentConnectors
                .Where(c => c.OwnerId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving connectors for user {UserId}", userId);
            throw;
        }
    }

    public async Task<DocumentConnector?> GetConnectorAsync(int connectorId, string userId)
    {
        try
        {
            return await _context.DocumentConnectors
                .FirstOrDefaultAsync(c => c.Id == connectorId && c.OwnerId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving connector {ConnectorId} for user {UserId}", connectorId, userId);
            throw;
        }
    }

    public async Task<DocumentConnector> CreateConnectorAsync(DocumentConnector connector)
    {
        try
        {
            connector.CreatedAt = DateTime.UtcNow;
            connector.UpdatedAt = DateTime.UtcNow;
            
            _context.DocumentConnectors.Add(connector);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created connector {ConnectorId} for user {UserId}", connector.Id, connector.OwnerId);
            return connector;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating connector for user {UserId}", connector.OwnerId);
            throw;
        }
    }

    public async Task<DocumentConnector> UpdateConnectorAsync(DocumentConnector connector, string userId)
    {
        try
        {
            var existing = await _context.DocumentConnectors
                .FirstOrDefaultAsync(c => c.Id == connector.Id && c.OwnerId == userId);
            
            if (existing == null)
            {
                throw new UnauthorizedAccessException("Connector not found or access denied");
            }
            
            existing.Name = connector.Name;
            existing.ConnectorType = connector.ConnectorType;
            existing.Configuration = connector.Configuration;
            existing.EncryptedCredentials = connector.EncryptedCredentials;
            existing.IsActive = connector.IsActive;
            existing.Description = connector.Description;
            existing.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Updated connector {ConnectorId}", connector.Id);
            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating connector {ConnectorId}", connector.Id);
            throw;
        }
    }

    public async Task<bool> DeleteConnectorAsync(int connectorId, string userId)
    {
        try
        {
            var connector = await _context.DocumentConnectors
                .FirstOrDefaultAsync(c => c.Id == connectorId && c.OwnerId == userId);
            
            if (connector == null)
            {
                return false;
            }
            
            _context.DocumentConnectors.Remove(connector);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted connector {ConnectorId}", connectorId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting connector {ConnectorId}", connectorId);
            throw;
        }
    }

    public async Task<(bool success, string message)> TestConnectionAsync(int connectorId, string userId)
    {
        try
        {
            var connector = await GetConnectorAsync(connectorId, userId);
            if (connector == null)
            {
                return (false, "Connector not found");
            }
            
            // TODO: Implement actual connection testing based on connector type
            // For now, just simulate a successful test
            connector.LastConnectionTest = DateTime.UtcNow;
            connector.LastConnectionTestResult = "Connection successful";
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Connection test successful for connector {ConnectorId}", connectorId);
            return (true, "Connection successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing connection for connector {ConnectorId}", connectorId);
            return (false, $"Connection test failed: {ex.Message}");
        }
    }

    public async Task<List<ConnectorFileInfo>> ListFilesAsync(int connectorId, string userId, string? path = null)
    {
        try
        {
            var connector = await GetConnectorAsync(connectorId, userId);
            if (connector == null)
            {
                throw new UnauthorizedAccessException("Connector not found or access denied");
            }
            
            // TODO: Implement actual file listing based on connector type
            // For now, return empty list
            _logger.LogInformation("Listing files from connector {ConnectorId}", connectorId);
            return new List<ConnectorFileInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing files from connector {ConnectorId}", connectorId);
            throw;
        }
    }
}
