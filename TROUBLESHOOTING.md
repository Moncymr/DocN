# Fix for Connection Error: "Impossibile stabilire la connessione (localhost:5211)"

## Problem Description

Users encountered the error: "Failed to load conversations: Impossibile stabilire la connessione. Rifiuto persistente del computer di destinazione. (localhost:5211)"

This error occurred because:
1. DocN uses a dual-server architecture:
   - **DocN.Client** (Blazor Server) on port 7114
   - **DocN.Server** (Backend API) on port 5211
2. The Client makes HTTP calls to the Server for RAG/chat functionality
3. If the Server is not running, connection attempts fail with cryptic error messages
4. The documentation did not clearly explain this architecture

## Solution Implemented

### 1. Enhanced Error Handling
**File: `DocN.Client/Components/Pages/Chat.razor`**

- Added specific `HttpRequestException` handling to detect backend connection failures
- Display user-friendly error messages:
  ```
  ⚠️ Unable to connect to the backend service. Please ensure the DocN.Server 
  is running on https://localhost:5211. See README.md for setup instructions.
  ```
- Improved error recovery by removing failed messages from UI
- Added console logging for debugging

### 2. Health Check Endpoint
**File: `DocN.Server/Program.cs`**

- Added `/api/health` endpoint for service availability checking
- Returns: `{ status: "healthy", service: "DocN.Server" }`
- Can be used for future monitoring and orchestration

### 3. HttpClient Configuration
**File: `DocN.Client/Program.cs`**

- Added 30-second timeout to prevent indefinite hanging
- Configured for future retry policies if needed

### 4. Startup Scripts
Created two scripts to simplify development:

**`start-dev.sh`** (Linux/Mac):
- Checks for .NET SDK
- Builds both projects
- Offers tmux-based parallel startup
- Provides clear instructions

**`start-dev.ps1`** (Windows PowerShell):
- Checks for .NET SDK
- Builds both projects
- Starts both servers in parallel using PowerShell jobs
- Handles graceful shutdown

### 5. Documentation Updates
**File: `README.md`**

- Added clear architecture diagram showing dual-server setup
- Explained which ports each service uses
- Added prominent warnings about dual-server requirement
- Documented startup scripts usage
- Clarified startup sequence (Server before Client)

## Benefits

1. **Better User Experience**: Clear, actionable error messages instead of cryptic exceptions
2. **Easier Setup**: Startup scripts automate the development workflow
3. **Clear Documentation**: New users understand the architecture immediately
4. **Debugging Support**: Console logging helps troubleshoot issues
5. **Future-Ready**: Health check endpoint enables monitoring and orchestration

## Usage

### Quick Start (Development)

**Option 1: Using Startup Scripts (Recommended)**
```bash
# Linux/Mac
./start-dev.sh

# Windows PowerShell
.\start-dev.ps1
```

**Option 2: Manual (Two Terminals)**
```bash
# Terminal 1 - Backend
cd DocN.Server
dotnet run

# Terminal 2 - Frontend
cd DocN.Client
dotnet run
```

### Verifying Backend is Running

Check the health endpoint:
```bash
curl https://localhost:5211/api/health
# Should return: {"status":"healthy","service":"DocN.Server"}
```

## Files Modified

1. `DocN.Client/Components/Pages/Chat.razor` - Enhanced error handling
2. `DocN.Client/Program.cs` - HttpClient timeout configuration
3. `DocN.Server/Program.cs` - Health check endpoint
4. `README.md` - Architecture and setup documentation

## Files Added

1. `start-dev.sh` - Linux/Mac startup script
2. `start-dev.ps1` - Windows startup script
3. `TROUBLESHOOTING.md` - This file

## Testing

- ✅ Both projects build successfully
- ✅ Error messages display correctly when backend is unavailable
- ✅ Health check endpoint works
- ✅ Documentation is clear and accurate
- ✅ Code review passed with no issues
- ✅ Security scan passed with no vulnerabilities

## Future Enhancements

Consider implementing:
1. **Automatic Backend Detection**: Client could check health endpoint on startup
2. **Retry Logic**: Implement Polly retry policies for transient failures
3. **Docker Compose**: Containerize both services for easier deployment
4. **Configuration Toggle**: Option to disable backend features if Server is unavailable
5. **Service Discovery**: For production deployments with multiple instances
