# Testing Instructions for Startup Crash Fix

## What Was Fixed
The application was crashing during startup with an `AggregateException` when AI services were not properly configured. This has been fixed by making the `SemanticRAGService` initialization more resilient.

## How to Test

### Prerequisites
- .NET 10.0 SDK installed
- SQL Server 2025 or compatible version running
- Database connection string configured

### Test Scenario 1: Application Starts Without AI Configuration

1. **Remove or comment out AI configuration** in `appsettings.json`:
   ```json
   //"AzureOpenAI": {
   //  "Endpoint": "",
   //  "ApiKey": ""
   //},
   //"OpenAI": {
   //  "ApiKey": ""
   //}
   ```

2. **Start the Server**:
   ```bash
   cd DocN.Server
   dotnet run
   ```
   
   **Expected Result**: 
   - Server starts successfully ✅
   - You may see a warning: "Could not initialize SemanticRAGService during construction"
   - Server continues running and is accessible

3. **Start the Client** (in a new terminal):
   ```bash
   cd DocN.Client
   dotnet run
   ```
   
   **Expected Result**:
   - Client starts successfully ✅
   - Application is accessible at `https://localhost:7114`
   - You can log in and use basic features
   - AI/RAG features will show appropriate error messages

### Test Scenario 2: Application Starts With Valid AI Configuration

1. **Configure AI services** in `appsettings.json` or user secrets:
   ```bash
   cd DocN.Server
   dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
   ```

2. **Start both Server and Client** as described above

3. **Expected Result**:
   - Both start successfully ✅
   - No warnings about SemanticRAGService initialization
   - All AI/RAG features work correctly

### Test Scenario 3: Database Connection Issues

If you see database connection errors:

1. **Verify database server is running**:
   ```bash
   # Check SQL Server status (Windows)
   # Or verify your connection string
   ```

2. **Expected Behavior**:
   - Application should show clear error messages about database connectivity
   - Application should not crash immediately
   - In development mode, seeding errors are logged but don't prevent startup

### Verification Checklist

After testing, verify:

- [ ] Server starts without crashing (exit code should be 0 when stopped, not -1)
- [ ] Client starts without crashing (exit code should be 0 when stopped, not -1)
- [ ] No `AggregateException` errors in the debug output
- [ ] Applications can be accessed in the browser
- [ ] Login page is accessible
- [ ] Clear error messages if AI features are unavailable

### What to Look For in Debug Output

**Before the fix** (BAD):
```
Exception thrown: 'System.AggregateException' in Microsoft.Extensions.DependencyInjection.dll
The program '[xxxxx] DocN.Client.exe' has exited with code 4294967295 (0xffffffff).
```

**After the fix** (GOOD):
```
[Warning] Could not initialize SemanticRAGService during construction. Services will be initialized on first use.
[Info] DocN Server started successfully
Application is running on: https://localhost:5211
```

### Additional Notes

- The fix allows the application to start even when AI services are misconfigured
- Users will see helpful error messages when trying to use AI features without proper configuration
- All non-AI features should work normally
- The application gracefully degrades instead of crashing

## Rollback Instructions

If any issues occur, you can rollback to the previous version:

```bash
git revert HEAD~2..HEAD
```

However, this would bring back the startup crash issue, so it's recommended to report any new issues instead.

## Support

If you encounter any issues during testing:
1. Check the application logs in the `logs/` directory
2. Verify your configuration settings
3. Ensure database connectivity
4. Review the error messages - they should now be clear and actionable

## Date
December 31, 2024
