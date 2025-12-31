using Microsoft.SemanticKernel;
using Microsoft.Extensions.Logging;

namespace DocN.Data.Services;

/// <summary>
/// Provides lazy-loaded access to a Semantic Kernel configured from database.
/// This wrapper allows services to get a Kernel that's configured from database
/// without blocking DI container initialization.
/// </summary>
public interface IKernelProvider
{
    /// <summary>
    /// Gets a Kernel instance configured from the database.
    /// The Kernel is created lazily on first access.
    /// </summary>
    Task<Kernel> GetKernelAsync();
}

/// <summary>
/// Implementation of IKernelProvider that lazily creates Kernel from database configuration.
/// </summary>
public class KernelProvider : IKernelProvider
{
    private readonly ISemanticKernelFactory _factory;
    private readonly ILogger<KernelProvider> _logger;
    private Task<Kernel>? _kernelTask;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public KernelProvider(
        ISemanticKernelFactory factory,
        ILogger<KernelProvider> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<Kernel> GetKernelAsync()
    {
        if (_kernelTask != null)
        {
            return await _kernelTask;
        }

        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_kernelTask != null)
            {
                return await _kernelTask;
            }

            _logger.LogInformation("Initializing Semantic Kernel from database configuration...");
            _kernelTask = _factory.CreateKernelAsync();
            var kernel = await _kernelTask;
            _logger.LogInformation("Semantic Kernel initialized successfully");
            
            return kernel;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
