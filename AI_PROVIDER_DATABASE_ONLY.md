# AI Provider Configuration - Database Only

## Overview

This document explains the changes made to ensure that **ALL AI provider configuration is loaded exclusively from the database**, not from `appsettings.json`.

## Problem Statement

Previously, the Semantic Kernel was being configured in `Program.cs` with hardcoded values from `appsettings.json`:

```csharp
// OLD CODE - Loading from appsettings.json
var azureOpenAIEndpoint = builder.Configuration["AzureOpenAI:Endpoint"];
var azureOpenAIKey = builder.Configuration["AzureOpenAI:ApiKey"];
// ... etc
```

This meant that AI providers could come from two sources:
1. Database (`AIConfigurations` table) - used by chat
2. `appsettings.json` - used by Semantic Kernel

This created confusion and inconsistency. The requirement was to ensure **all providers come from the database only**.

## Solution

The solution involves three new components:

### 1. SemanticKernelFactory

**File:** `DocN.Data/Services/SemanticKernelFactory.cs`

This factory creates Semantic Kernel instances from database AI configuration:

```csharp
public interface ISemanticKernelFactory
{
    Task<Kernel> CreateKernelAsync();
}
```

The factory:
- Loads active AI configuration from the database via `IMultiProviderAIService`
- Creates a Kernel configured with the database providers
- Caches the Kernel for 5 minutes for performance
- Supports Azure OpenAI, OpenAI, and Gemini providers

### 2. KernelProvider

**File:** `DocN.Data/Services/KernelProvider.cs`

This provider gives lazy-loaded access to the Kernel:

```csharp
public interface IKernelProvider
{
    Task<Kernel> GetKernelAsync();
}
```

The provider:
- Lazily creates the Kernel on first access
- Uses thread-safe initialization
- Works with DI container's synchronous requirements

### 3. Updated Program.cs

**File:** `DocN.Server/Program.cs`

The Semantic Kernel configuration was replaced:

```csharp
// NEW CODE - Loading from database
builder.Services.AddScoped<ISemanticKernelFactory, SemanticKernelFactory>();
builder.Services.AddSingleton<IKernelProvider, KernelProvider>();
```

### 4. Updated Health Check

**File:** `DocN.Server/Services/HealthChecks/SemanticKernelHealthCheck.cs`

The health check now uses `IKernelProvider` instead of direct Kernel injection:

```csharp
public SemanticKernelHealthCheck(
    IKernelProvider kernelProvider,
    ILogger<SemanticKernelHealthCheck> logger)
{
    _kernelProvider = kernelProvider;
    _logger = logger;
}

public async Task<HealthCheckResult> CheckHealthAsync(...)
{
    var kernel = await _kernelProvider.GetKernelAsync();
    // ... check logic
}
```

## Benefits

1. **Single Source of Truth**: All AI provider configuration comes from the database
2. **Centralized Management**: All providers managed through the application UI
3. **Consistency**: No more confusion about which configuration is being used
4. **Dynamic Updates**: Configuration changes in the database are picked up automatically
5. **Backward Compatible**: Existing services continue to work without changes

## How It Works

### Configuration Flow

```
┌─────────────────────────┐
│  Database               │
│  AIConfigurations Table │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│  MultiProviderAIService │
│  (loads from database)  │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│  SemanticKernelFactory  │
│  (creates Kernel)       │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│  KernelProvider         │
│  (lazy initialization)  │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│  Services using Kernel  │
│  (health checks, etc.)  │
└─────────────────────────┘
```

### For Chat Functionality

Chat already used database configuration:

```
┌─────────────────────────┐
│  Database               │
│  AIConfigurations Table │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│  MultiProviderAIService │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ MultiProviderSemanticRAG│
│ Service (chat)          │
└─────────────────────────┘
```

So chat was already correct - we just needed to fix the Semantic Kernel initialization.

## Usage

### For New Services That Need Kernel

If you're creating a new service that needs a Kernel:

```csharp
public class MyService
{
    private readonly IKernelProvider _kernelProvider;
    
    public MyService(IKernelProvider kernelProvider)
    {
        _kernelProvider = kernelProvider;
    }
    
    public async Task DoSomethingAsync()
    {
        var kernel = await _kernelProvider.GetKernelAsync();
        // Use the kernel configured from database
    }
}
```

### Configuring AI Providers

All configuration is done through the application UI:

1. Go to Settings (`/config`)
2. Configure your AI providers (Gemini, OpenAI, or Azure OpenAI)
3. Set which provider to use for Chat, Embeddings, Tag Extraction, and RAG
4. Save and activate the configuration

No need to modify `appsettings.json` anymore!

## Important Notes

1. **No More appsettings.json Configuration**: The hardcoded Semantic Kernel configuration from `appsettings.json` has been completely removed

2. **Database Required**: The application now requires the `AIConfigurations` table to be present and have at least one active configuration

3. **Caching**: Configurations are cached for 5 minutes for performance. Use `IMultiProviderAIService.ClearConfigurationCache()` to force a reload

4. **Fallback Behavior**: If no database configuration is found, an empty Kernel is created and services must handle this gracefully

## Migration Notes

### Before This Change
- Semantic Kernel: Configured from `appsettings.json`
- Chat: Configured from database
- Result: Two sources of configuration

### After This Change
- Semantic Kernel: Configured from database
- Chat: Configured from database
- Result: Single source of configuration

## Testing

To verify the changes:

1. Ensure database has active AI configuration
2. Start the application
3. Check logs for "Semantic Kernel created successfully from database configuration"
4. Use the chat feature - it should work with database configuration
5. Check health endpoint `/health` - Semantic Kernel health should be operational

## Troubleshooting

### "No active AI configuration found in database"

**Solution**: Go to Settings (`/config`) and configure at least one AI provider with an active configuration.

### "Semantic Kernel has no services configured"

**Solution**: The database configuration doesn't have valid API keys for Azure OpenAI or OpenAI. Add valid keys in Settings.

### Chat Not Working

**Solution**: 
1. Check that you have an active AI configuration in the database
2. Verify API keys are valid
3. Check logs for detailed error messages
4. Ensure the `ChatProvider` field is set in your AI configuration

## Related Files

- `DocN.Data/Services/SemanticKernelFactory.cs` - Factory for creating Kernel from database
- `DocN.Data/Services/KernelProvider.cs` - Lazy provider for Kernel access
- `DocN.Data/Services/MultiProviderAIService.cs` - Service that loads configuration from database
- `DocN.Server/Program.cs` - Application startup and service registration
- `DocN.Server/Services/HealthChecks/SemanticKernelHealthCheck.cs` - Health check using database config

## See Also

- `RAG_PROVIDER_INITIALIZATION_GUIDE.md` - Guide on RAG provider initialization
- `MULTI_PROVIDER_CONFIG.md` - Multi-provider configuration guide
- Database schema: `AIConfigurations` table
