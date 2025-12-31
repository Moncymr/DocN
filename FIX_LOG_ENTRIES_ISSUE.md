# Fix: LogEntries and AuditLogs Tables Empty Issue

## Problem Statement
After selecting a file and analyzing tables, both `AuditLogs` and `LogEntries` tables appear empty, and log buttons are not visible in the UI.

## Root Cause Analysis

### Database Context Confusion
The application had two separate `DbContext` classes that were causing confusion:

1. **DocArcContext** - Declared `LogEntries` DbSet but was NOT properly configured for migrations
2. **ApplicationDbContext** - Contains `AuditLogs` and all other application tables, used for migrations

### The Problem
- `LogService` was using `DocArcContext` to write logs to the `LogEntries` table
- However, `DocArcContext` was NOT being used to apply migrations in `Program.cs`
- Only `ApplicationDbContext` migrations were being applied (line 361 in Program.cs)
- Even though a migration file `20251229074500_AddLogEntriesTable.cs` existed for the `LogEntries` table, it wasn't being recognized because `LogEntries` wasn't properly configured in `ApplicationDbContext`

### Connection String Configuration
Looking at Program.cs:
```csharp
// Line 157-169: DocArcContext configuration
builder.Services.AddDbContext<DocArcContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DocArc");
    // ...
});

// Line 172-184: ApplicationDbContext configuration  
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                        ?? builder.Configuration.GetConnectionString("DocArc");
    // ...
});
```

Both contexts can use the same database, but the migration system only applies `ApplicationDbContext` migrations.

## Solution Implemented

### Changes Made

#### 1. Added LogEntries to ApplicationDbContext
**File**: `DocN.Data/ApplicationDbContext.cs`

Added the `LogEntries` DbSet:
```csharp
// Application logs for debugging and monitoring
public DbSet<LogEntry> LogEntries { get; set; }
```

#### 2. Added LogEntry Configuration
**File**: `DocN.Data/ApplicationDbContext.cs`

Added entity configuration in `OnModelCreating`:
```csharp
// LogEntry configuration
modelBuilder.Entity<LogEntry>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Timestamp).IsRequired();
    entity.Property(e => e.Level).IsRequired().HasMaxLength(50);
    entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
    entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
    entity.Property(e => e.Details).HasColumnType("nvarchar(max)");
    entity.Property(e => e.UserId).HasMaxLength(450);
    entity.Property(e => e.FileName).HasMaxLength(500);
    entity.Property(e => e.StackTrace).HasColumnType("nvarchar(max)");
    
    // Indexes for efficient querying
    entity.HasIndex(e => e.Timestamp);
    entity.HasIndex(e => new { e.Category, e.Timestamp });
    entity.HasIndex(e => new { e.UserId, e.Timestamp });
});
```

#### 3. Updated LogService to Use ApplicationDbContext
**File**: `DocN.Data/Services/LogService.cs`

Changed from:
```csharp
private readonly DocArcContext _context;

public LogService(DocArcContext context)
{
    _context = context;
}
```

To:
```csharp
private readonly ApplicationDbContext _context;

public LogService(ApplicationDbContext context)
{
    _context = context;
}
```

#### 4. Removed LogEntries from DocArcContext
**File**: `DocN.Data/DocArcContext.cs`

Removed the `LogEntries` DbSet to avoid confusion:
```csharp
public DbSet<Document> Documents { get; set; }
public DbSet<DocumentChunk> DocumentChunks { get; set; }
public DbSet<SimilarDocument> SimilarDocuments { get; set; }
// Removed: public DbSet<LogEntry> LogEntries { get; set; }
```

## Migration Status

The existing migration `20251229074500_AddLogEntriesTable.cs` will now be properly applied when the application starts because:

1. `LogEntries` is now properly declared in `ApplicationDbContext`
2. `LogEntry` entity configuration is now in `ApplicationDbContext.OnModelCreating`
3. Migrations are applied to `ApplicationDbContext` in Program.cs (line 361)

## Expected Behavior After Fix

1. When the application starts, migrations will be automatically applied
2. The `LogEntries` table will be created in the database (if not already exists)
3. The `LogService` will successfully write logs to the `LogEntries` table
4. The "View Upload Logs" button in the Upload page will display logs from the database
5. Both `AuditLogs` and `LogEntries` tables will be populated with data

## UI Impact

The Upload page (`/upload`) has a "📋 View Upload Logs" button that opens a modal showing:
- Log entries from the last 24 hours
- Filtered by category (Upload, Embedding, AI, Tag, Metadata, Category, etc.)
- Color-coded by log level (Error, Warning, Info, Debug)
- Detailed information including timestamp, category, message, and stack traces

## Testing Recommendations

1. **Delete the database** (or just the tables) to force a fresh migration
2. **Start the application** - migrations should apply automatically
3. **Upload a document** to generate log entries
4. **Click "📋 View Upload Logs"** button to verify logs are visible
5. **Verify AuditLogs** are also being created for compliance tracking

## Notes

- Both `AuditLogs` and `LogEntries` are now in the same database context
- `AuditLogs` are used for GDPR/SOC2 compliance tracking
- `LogEntries` are used for application debugging and monitoring
- The fix maintains backward compatibility with existing migrations
