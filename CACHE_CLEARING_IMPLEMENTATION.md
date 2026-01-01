# Configuration Cache Clearing Implementation

## Problem Statement
The diagnostics page at `/config/diagnostica` was showing the "Default Configuration" from the database, but the system was using a cached configuration in memory. The cache had a 5-minute duration in `MultiProviderAIService`, and there was no way to clear it manually from the UI.

The user wanted a way to clear the cached configuration from the diagnostics page to force the system to reload the configuration from the database immediately.

## Solution Implemented

### 1. API Endpoint for Cache Clearing
**File**: `DocN.Server/Controllers/ConfigController.cs`

Added a new endpoint:
```csharp
[HttpPost("clear-cache")]
```

**Features:**
- Calls `_aiService.ClearConfigurationCache()` to invalidate the in-memory cache
- Returns a success message in Italian
- Includes timestamp in response
- Proper error handling with logging

**Response Format:**
```json
{
  "success": true,
  "message": "✅ Cache della configurazione svuotata con successo. La configurazione verrà ricaricata dal database al prossimo utilizzo.",
  "timestamp": "2026-01-01T20:30:00Z"
}
```

### 2. UI Enhancement
**File**: `DocN.Client/Components/Pages/ConfigDiagnostics.razor`

Added a **Cache Management Section** with:
- Clear and informative header explaining the cache mechanism
- "Svuota Cache" (Clear Cache) button
- Loading state during cache clearing operation
- Success/error message display after operation
- Automatic page reload after successful cache clear

**UI Components:**
- Cache management section with bordered card design
- Responsive layout (stacks on mobile)
- Visual feedback during operation
- Success (green) and error (red) message styling

### 3. Test Coverage
**File**: `DocN.Server.Tests/ConfigControllerTests.cs`

Added test:
```csharp
[Fact]
public void ClearConfigurationCache_ReturnsSuccess()
```

**Test Verifies:**
- Endpoint returns HTTP 200 OK
- Response contains success flag = true
- Response message is correct
- `ClearConfigurationCache()` is called on the AI service

## How It Works

### Cache Flow Before Fix
```
User visits /config/diagnostica
   ↓
Page displays "Default Configuration" (from DB)
   ↓
System continues using CACHED config (up to 5 minutes old)
   ↓
No way to force reload
```

### Cache Flow After Fix
```
User visits /config/diagnostica
   ↓
Page displays "Default Configuration" (from DB)
   ↓
User clicks "🗑️ Svuota Cache" button
   ↓
API calls _aiService.ClearConfigurationCache()
   ↓
Cache is cleared (_cachedConfig = null, _lastConfigCheck = DateTime.MinValue)
   ↓
Page reloads after 500ms
   ↓
Next AI operation fetches fresh config from database
```

### Cache Mechanism in MultiProviderAIService

**Location**: `DocN.Data/Services/MultiProviderAIService.cs`

```csharp
private AIConfiguration? _cachedConfig;
private DateTime _lastConfigCheck = DateTime.MinValue;
private readonly TimeSpan _configCacheDuration = TimeSpan.FromMinutes(5);

public void ClearConfigurationCache()
{
    _cachedConfig = null;
    _lastConfigCheck = DateTime.MinValue;
}
```

**Cache Invalidation Points:**
1. Manual clear via new endpoint: `POST /api/config/clear-cache`
2. After configuration activation: `POST /api/config/{id}/activate`
3. After configuration save: `POST /api/config`
4. Automatic expiry after 5 minutes

## Usage Instructions

### From UI
1. Navigate to `/config/diagnostica`
2. Scroll to "🗑️ Gestione Cache Configurazione" section
3. Click "🗑️ Svuota Cache" button
4. Wait for success message
5. Page automatically reloads with fresh data

### From API
```bash
curl -X POST http://localhost:7114/api/config/clear-cache
```

Response:
```json
{
  "success": true,
  "message": "✅ Cache della configurazione svuotata con successo...",
  "timestamp": "2026-01-01T20:30:00Z"
}
```

## Visual Design

### Desktop View
```
┌─────────────────────────────────────────────────────┐
│ 🗑️ Gestione Cache Configurazione    [🗑️ Svuota Cache] │
│ Svuota la cache per forzare il ricaricamento...    │
├─────────────────────────────────────────────────────┤
│ ✅ Cache della configurazione svuotata con successo │
└─────────────────────────────────────────────────────┘
```

### Mobile View
```
┌───────────────────────────────┐
│ 🗑️ Gestione Cache             │
│ Configurazione                │
│ Svuota la cache per...       │
├───────────────────────────────┤
│  [🗑️ Svuota Cache]            │
├───────────────────────────────┤
│ ✅ Cache della configurazione │
│    svuotata con successo      │
└───────────────────────────────┘
```

## Benefits

1. **Immediate Cache Clear**: No need to wait up to 5 minutes for cache expiry
2. **User Control**: Admins can force configuration reload when needed
3. **Debugging Aid**: Helps troubleshoot configuration issues
4. **Feedback**: Clear visual confirmation of operation success/failure
5. **Auto-reload**: Page refreshes to show the effect immediately

## Related Changes

### Modified Files
- `DocN.Server/Controllers/ConfigController.cs` - Added clear-cache endpoint
- `DocN.Client/Components/Pages/ConfigDiagnostics.razor` - Added UI section
- `DocN.Server.Tests/ConfigControllerTests.cs` - Added test coverage

### No Changes Required
- `MultiProviderAIService.cs` - Already had `ClearConfigurationCache()` method
- Cache mechanism was already in place and working

## Technical Notes

### Why Cache Exists
The configuration cache exists to avoid excessive database queries on every AI operation:
- AI operations happen frequently (embeddings, chat, tag extraction)
- Database queries add latency
- Configuration rarely changes
- 5-minute cache is reasonable balance

### When to Clear Cache
Clear the cache when:
- You've updated configuration in the database manually
- You've activated a different configuration
- You're troubleshooting why old configuration is being used
- You want to verify the latest database configuration immediately

### Cache Scope
- Cache is per-application instance
- In multi-instance deployments, each instance has its own cache
- Clearing cache affects only the current instance
- Other instances will continue using their cache until expiry

## Future Enhancements

Possible improvements:
1. Add "Last Cleared" timestamp display
2. Show current cache age
3. Add automatic cache clear after X operations
4. Implement distributed cache clearing for multi-instance scenarios
5. Add cache statistics (hit rate, age, etc.)
