using DocN.Data.Models;

namespace DocN.Core.Interfaces;

/// <summary>
/// Service for managing document connectors to external repositories
/// </summary>
public interface IConnectorService
{
    /// <summary>
    /// Gets all connectors for a user
    /// </summary>
    Task<List<DocumentConnector>> GetUserConnectorsAsync(string userId);
    
    /// <summary>
    /// Gets a specific connector by ID
    /// </summary>
    Task<DocumentConnector?> GetConnectorAsync(int connectorId, string userId);
    
    /// <summary>
    /// Creates a new connector
    /// </summary>
    Task<DocumentConnector> CreateConnectorAsync(DocumentConnector connector);
    
    /// <summary>
    /// Updates an existing connector
    /// </summary>
    Task<DocumentConnector> UpdateConnectorAsync(DocumentConnector connector, string userId);
    
    /// <summary>
    /// Deletes a connector
    /// </summary>
    Task<bool> DeleteConnectorAsync(int connectorId, string userId);
    
    /// <summary>
    /// Tests connection to external repository
    /// </summary>
    Task<(bool success, string message)> TestConnectionAsync(int connectorId, string userId);
    
    /// <summary>
    /// Lists files from the connector
    /// </summary>
    Task<List<ConnectorFileInfo>> ListFilesAsync(int connectorId, string userId, string? path = null);
}

/// <summary>
/// Represents file information from a connector
/// </summary>
public class ConnectorFileInfo
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsFolder { get; set; }
    public string? ContentType { get; set; }
}
