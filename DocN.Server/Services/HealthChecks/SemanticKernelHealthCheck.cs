using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.SemanticKernel;

namespace DocN.Server.Services.HealthChecks;

/// <summary>
/// Health check for Semantic Kernel orchestration
/// </summary>
public class SemanticKernelHealthCheck : IHealthCheck
{
    private readonly Kernel _kernel;
    private readonly ILogger<SemanticKernelHealthCheck> _logger;

    public SemanticKernelHealthCheck(
        Kernel kernel,
        ILogger<SemanticKernelHealthCheck> logger)
    {
        _kernel = kernel;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if kernel has services configured
            var hasServices = _kernel.Services != null;
            
            if (!hasServices)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Semantic Kernel has no services configured"));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "Semantic Kernel orchestration is operational"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Semantic Kernel health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Semantic Kernel health check failed",
                ex));
        }
    }
}
