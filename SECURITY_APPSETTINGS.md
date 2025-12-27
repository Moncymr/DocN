# Security Notice - API Keys in Configuration Files

## ⚠️ IMPORTANT SECURITY INFORMATION

The `appsettings.json` files in this repository currently contain API keys for demonstration/development purposes. 

**For production deployments, you MUST:**

1. **Remove API keys from appsettings.json** before deploying to production
2. **Use secure configuration methods** instead:

### Recommended Secure Configuration Methods

#### Option 1: User Secrets (Development)
```bash
cd DocN.Server
dotnet user-secrets init
dotnet user-secrets set "Gemini:ApiKey" "YOUR_API_KEY_HERE"
dotnet user-secrets set "Embeddings:ApiKey" "YOUR_API_KEY_HERE"
```

#### Option 2: Environment Variables (Production)
```bash
# Linux/Mac
export Gemini__ApiKey="YOUR_API_KEY_HERE"
export Embeddings__ApiKey="YOUR_API_KEY_HERE"

# Windows
set Gemini__ApiKey=YOUR_API_KEY_HERE
set Embeddings__ApiKey=YOUR_API_KEY_HERE
```

#### Option 3: Azure Key Vault (Production - Recommended)
Configure Azure Key Vault in your `Program.cs`:
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

#### Option 4: appsettings.Production.json (Excluded from Git)
Create `appsettings.Production.json` (already in .gitignore):
```json
{
  "Gemini": {
    "ApiKey": "YOUR_PRODUCTION_API_KEY"
  },
  "Embeddings": {
    "ApiKey": "YOUR_PRODUCTION_API_KEY"
  }
}
```

## Current Configuration

The current `appsettings.json` files are configured with:
- **AI Provider**: Gemini
- **Embeddings Provider**: Gemini (text-embedding-004)
- **Fallback**: Enabled

## Best Practices

1. ✅ **Never commit API keys** to version control
2. ✅ **Use User Secrets** for local development
3. ✅ **Use Azure Key Vault** or similar for production
4. ✅ **Rotate API keys** regularly
5. ✅ **Use different keys** for development/staging/production
6. ✅ **Monitor API usage** to detect unauthorized access
7. ✅ **Restrict API key permissions** to minimum required

## If API Keys Are Compromised

If you accidentally commit API keys:
1. **Immediately revoke/regenerate** the keys in the Google Cloud Console
2. **Remove them from git history** using `git filter-branch` or BFG Repo-Cleaner
3. **Update all deployments** with new keys
4. **Review access logs** for unauthorized usage

## Additional Resources

- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Safe Storage of App Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault Configuration](https://docs.microsoft.com/en-us/azure/key-vault/general/overview)
