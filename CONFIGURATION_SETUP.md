# DocN Configuration Setup

## Quick Start

If you encounter the error **"alla vio si chiude subito"** (application closes immediately on startup), this means the configuration files are missing.

### Solution

Follow these steps to configure the application:

## 1. Create Configuration Files

### For DocN.Client

```bash
# Copy the example files to create your configuration
cd DocN.Client
copy appsettings.example.json appsettings.json
copy appsettings.Development.example.json appsettings.Development.json
```

On Linux/macOS:
```bash
cp appsettings.example.json appsettings.json
cp appsettings.Development.example.json appsettings.Development.json
```

### For DocN.Server

```bash
# Copy the example files to create your configuration
cd DocN.Server
copy appsettings.example.json appsettings.json
copy appsettings.Development.example.json appsettings.Development.json
```

On Linux/macOS:
```bash
cp appsettings.example.json appsettings.json
cp appsettings.Development.example.json appsettings.Development.json
```

## 2. Configure Database Connection

Edit both `appsettings.json` files and update the connection string with your SQL Server details:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=True"
  }
}
```

Replace `YOUR_SERVER` with your SQL Server instance name. Examples:
- `localhost` - Default local instance
- `localhost\\SQLEXPRESS` - Named instance
- `NTSPJ-060-02\\SQL2025` - Remote named instance

## 3. Initialize Database

Run the SQL scripts in the `Database/` folder to create the database schema:

```sql
-- Run these in order
1. Create the DocNDb database
2. Run the schema creation scripts
3. Run the initial data scripts
```

## 4. Configure AI Providers (Optional)

AI providers can be configured in two ways:

### Option 1: Via Web UI (Recommended)
1. Start the application
2. Navigate to Settings → AI Configuration
3. Enter your API keys
4. Save and test the connection

### Option 2: Via appsettings.json
Edit the configuration files and add your API keys:

```json
{
  "OpenAI": {
    "ApiKey": "sk-your-key-here",
    "Model": "gpt-4"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-azure-key-here",
    "ChatDeployment": "gpt-4",
    "EmbeddingDeployment": "text-embedding-ada-002"
  },
  "Gemini": {
    "ApiKey": "your-gemini-key-here",
    "Model": "gemini-pro"
  }
}
```

**Note**: Database configuration takes priority over appsettings.json for AI providers.

## 5. Start the Applications

Start both applications:

1. **Start the Server** (DocN.Server)
   - The server runs on `https://localhost:5211` and `http://localhost:5210`
   - Check logs at `/logs/docn-*.log`

2. **Start the Client** (DocN.Client)
   - The client runs on the configured port (default: 5036 HTTP, 7114 HTTPS)
   - Access the web interface at the configured URL

## Troubleshooting

### Application Closes Immediately

**Cause**: Missing configuration files or invalid database connection.

**Solution**:
1. Ensure `appsettings.json` exists in both DocN.Client and DocN.Server folders
2. Verify database connection string is correct
3. Check that SQL Server is running and accessible
4. Review application logs in the `/logs` folder

### Database Connection Errors

**Symptoms**: Application logs show database connection errors during startup.

**Solutions**:
1. Verify SQL Server is running
2. Check the connection string syntax
3. Ensure the database exists (run scripts from `Database/` folder)
4. Verify the user has appropriate permissions
5. If using Windows Authentication, ensure the app runs with the correct user

### AI Features Not Working

**Cause**: No AI provider configured or invalid API keys.

**Solution**:
1. AI features are optional - the app will work without them
2. Configure at least one AI provider via the web UI
3. Test the connection after configuration
4. Check logs for specific error messages

## Security Notes

⚠️ **Important**:
- Never commit `appsettings.json` or `appsettings.Development.json` to version control
- These files are in `.gitignore` to prevent accidental commits
- Store production credentials in secure key vaults
- Use environment variables for sensitive configuration in production

## Additional Resources

- Full documentation: See `RIEPILOGO_DOCUMENTAZIONE.md`
- Troubleshooting guide: See `TROUBLESHOOTING_CONFIGURATION.md`
- API documentation: Available at `/swagger` when server is running
- Testing instructions: See `TESTING_INSTRUCTIONS.md`
