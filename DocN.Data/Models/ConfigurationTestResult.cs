namespace DocN.Data.Models;

/// <summary>
/// Result of testing AI configuration
/// </summary>
public class ConfigurationTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ProviderTestResult> ProviderResults { get; set; } = new();
}

/// <summary>
/// Result of testing a specific AI provider
/// </summary>
public class ProviderTestResult
{
    public string ProviderName { get; set; } = string.Empty;
    public AIProviderType ProviderType { get; set; }
    public bool IsConfigured { get; set; }
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ServiceTestResult> Services { get; set; } = new();
}

/// <summary>
/// Result of testing a specific service assignment
/// </summary>
public class ServiceTestResult
{
    public string ServiceName { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
}
