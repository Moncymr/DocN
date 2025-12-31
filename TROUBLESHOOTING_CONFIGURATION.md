# Troubleshooting AI Configuration Issues

This guide helps diagnose and resolve common configuration issues with DocN's AI providers.

## Problem: "Nessun provider AI ha una chiave API valida configurata"

This error means no AI provider has a valid API key configured. The system needs at least one provider (Gemini, OpenAI, or Azure OpenAI) to be configured.

### Solution 1: Configure via Web UI (Recommended)

1. Navigate to **Settings** → **AI Configuration** (`/config`)
2. Fill in the API key for at least one provider:
   - **Gemini**: Get API key from [Google AI Studio](https://makersuite.google.com/app/apikey)
   - **OpenAI**: Get API key from [OpenAI Platform](https://platform.openai.com/api-keys)
   - **Azure OpenAI**: Get endpoint and key from [Azure Portal](https://portal.azure.com)
3. Set the configuration as **Active**
4. Click **Save Configuration**
5. Click **Test Connection** to verify

### Solution 2: Configure via appsettings.json (Development)

Edit `DocN.Server/appsettings.Development.json`:

```json
{
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY_HERE"
  },
  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY_HERE",
    "Model": "gpt-4"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "YOUR_AZURE_OPENAI_KEY_HERE",
    "ChatDeployment": "gpt-4",
    "EmbeddingDeployment": "text-embedding-ada-002"
  }
}
```

**Note**: Database configuration takes priority over appsettings.json. If you have an active configuration in the database with empty API keys, it will override appsettings.json.

## Problem: Configuration Returns Default Values

If `GetActiveConfigurationAsync()` returns default values instead of database values:

### Diagnostic Steps

1. **Check Database Connection**:
   - Verify connection string in `appsettings.json`
   - Ensure database server is accessible
   - Check database exists and has proper schema

2. **Check AIConfigurations Table**:
   ```sql
   -- Check if table exists
   SELECT * FROM INFORMATION_SCHEMA.TABLES 
   WHERE TABLE_NAME = 'AIConfigurations';
   
   -- Check active configuration
   SELECT * FROM AIConfigurations WHERE IsActive = 1;
   
   -- Check all configurations
   SELECT Id, ConfigurationName, IsActive, CreatedAt, UpdatedAt,
          CASE WHEN GeminiApiKey IS NOT NULL THEN 'Yes' ELSE 'No' END AS HasGeminiKey,
          CASE WHEN OpenAIApiKey IS NOT NULL THEN 'Yes' ELSE 'No' END AS HasOpenAIKey,
          CASE WHEN AzureOpenAIKey IS NOT NULL THEN 'Yes' ELSE 'No' END AS HasAzureKey
   FROM AIConfigurations
   ORDER BY IsActive DESC, UpdatedAt DESC;
   ```

3. **Check Application Logs**:
   Look for these log entries in `logs/docn-*.log`:
   - `"Fetching active configuration from database..."`
   - `"✅ Loaded active configuration from database: {ConfigurationName}"`
   - `"⚠️ No active configuration found in database. Falling back to appsettings.json"`
   - `"Configured providers: {providers}"`

4. **Clear Configuration Cache**:
   The configuration is cached for 5 minutes. To force reload:
   ```bash
   # Restart the application
   # OR wait 5 minutes for cache to expire
   ```

## Problem: HTTP Timeout (180 or 300 seconds)

If you see: "The request was canceled due to the configured HttpClient.Timeout of X seconds elapsing"

### Causes
- AI providers (especially Gemini) can take longer during high load
- Large document processing
- Complex RAG queries

### Solutions

1. **Already Implemented** (v2.1+):
   - Client timeout increased to 300 seconds (5 minutes)
   - Server AI HttpClient timeout set to 300 seconds
   - Named HttpClients configured for AI operations

2. **If Still Timing Out**:
   - Check network connectivity to AI provider
   - Verify API key is valid
   - Try with smaller documents/queries first
   - Check AI provider status page

## Problem: Chat AI Not Working

### Troubleshooting Steps

1. **Verify Configuration**:
   ```bash
   curl -X GET https://localhost:5211/api/config/active
   ```
   Should return an active configuration with provider details.

2. **Test Provider Connection**:
   ```bash
   curl -X POST https://localhost:5211/api/config/test
   ```
   Should show ✅ for configured providers.

3. **Check Provider Status**:
   - **Gemini**: [Google AI Status](https://status.cloud.google.com/)
   - **OpenAI**: [OpenAI Status](https://status.openai.com/)
   - **Azure**: [Azure Status](https://status.azure.com/)

4. **Verify API Key Format**:
   - **Gemini**: Should start with `AIza...`
   - **OpenAI**: Should start with `sk-...`
   - **Azure**: 32-character hexadecimal string

5. **Check Rate Limits**:
   - Free tier Gemini: 60 requests/minute
   - OpenAI: Varies by tier
   - Azure: Depends on deployment configuration

## Provider-Specific Issues

### Gemini Provider

**Error**: "API key non configurata" / "API key not configured" or "403 Forbidden"
- **Solution**: Ensure API key is valid and not flagged as compromised
- Get a new key from [Google AI Studio](https://makersuite.google.com/app/apikey)

**Error**: "404 Not Found" or "modello non trovato" / "model not found"
- **Solution**: Model name incorrect or deprecated
- Use current models: `gemini-2.0-flash-exp`, `text-embedding-004`

### OpenAI Provider

**Error**: "401 Unauthorized"
- **Solution**: API key invalid or expired
- Check key at [OpenAI Platform](https://platform.openai.com/api-keys)

**Error**: "429 Too Many Requests"
- **Solution**: Rate limit exceeded
- Wait a few minutes or upgrade plan

### Azure OpenAI Provider

**Error**: "Endpoint non configurati" / "Endpoint not configured"
- **Solution**: Verify both endpoint URL and API key are set
- Format: `https://{your-resource-name}.openai.azure.com/`

**Error**: "Deployment not found"
- **Solution**: Verify deployment names in Azure Portal
- Match `ChatDeploymentName` and `EmbeddingDeploymentName` exactly

## Configuration Priority

The system loads configuration in this order:

1. **Database** (Priority): Active configuration in `AIConfigurations` table
2. **Fallback**: `appsettings.json` if no database configuration exists

If you have an active database configuration with empty API keys, it will **override** appsettings.json values!

### To Use appsettings.json Only

Option A: Delete or deactivate all database configurations
```sql
UPDATE AIConfigurations SET IsActive = 0;
-- OR
DELETE FROM AIConfigurations;
```

Option B: Set API keys in active database configuration to match appsettings

## Helpful Commands

### View Current Configuration
```bash
curl https://localhost:5211/api/config/active
```

### Test All Providers
```bash
curl -X POST https://localhost:5211/api/config/test
```

### Check Logs
```bash
# Linux/Mac
tail -f logs/docn-$(date +%Y%m%d).log | grep -i "configuration\|provider"

# Windows PowerShell
Get-Content logs\docn-$(Get-Date -Format 'yyyyMMdd').log -Wait | Select-String "configuration|provider"
```

## Still Having Issues?

1. Enable detailed logging in `appsettings.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "DocN.Data.Services.MultiProviderAIService": "Debug"
       }
     }
   }
   ```

2. Check logs for detailed error messages

3. Verify database migrations are applied:
   ```bash
   dotnet ef database update --project DocN.Data --startup-project DocN.Server
   ```

4. Create an issue on GitHub with:
   - Log excerpts (with API keys redacted!)
   - Configuration approach used (UI or appsettings)
   - Provider being used
   - Error messages

## Quick Start Checklist

- [ ] Database connection is working
- [ ] At least one AI provider API key is configured
- [ ] Configuration is marked as Active in database
- [ ] Test connection shows ✅ for at least one provider
- [ ] Application logs show configuration loaded successfully
- [ ] No timeout errors in logs

---

**Last Updated**: 2025-12-31  
**Version**: 2.1
