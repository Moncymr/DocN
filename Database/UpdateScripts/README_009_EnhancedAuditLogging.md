# Update 009: Enhanced AuditLogs for GDPR/SOC2 Compliance

## Overview
This update enhances the AuditLogs table with additional fields required for GDPR/SOC2 compliance and comprehensive audit trail capabilities.

## What Changed

### New Fields Added
- **Username** (NVARCHAR(256)): Quick reference to username without joining AspNetUsers
- **ResourceType** (NVARCHAR(50)): Type of resource affected (Document, Configuration, User, etc.)
- **ResourceId** (NVARCHAR(100)): ID of the specific resource
- **TenantId** (INT): Multi-tenant isolation support
- **Severity** (NVARCHAR(20)): Log severity (Info, Warning, Error, Critical)
- **Success** (BIT): Whether the action succeeded or failed
- **ErrorMessage** (NVARCHAR(1000)): Error details for failed operations

### Enhanced Indexes
Created 8 optimized indexes for fast querying:
- UserId
- Action
- ResourceType
- Timestamp (descending)
- TenantId
- UserId + Timestamp (composite)
- Action + Timestamp (composite)
- ResourceType + ResourceId (composite)

## Compliance Benefits

### GDPR Article 30 - Records of Processing Activities
✅ Complete audit trail of all data processing activities  
✅ User identification and timestamp tracking  
✅ IP address tracking for accountability  
✅ Resource-level granularity  

### SOC2 Trust Service Criteria
✅ Security event logging (CC6.2)  
✅ Access controls tracking (CC6.1)  
✅ Audit trail for configuration changes (CC8.1)  
✅ Tenant isolation support for multi-tenant environments  

## How to Apply

### Option 1: Using the Update Script (Recommended for existing databases)
```sql
-- Run the update script
sqlcmd -S localhost -d DocNDb -i 009_EnhancedAuditLogging.sql
```

### Option 2: Using EF Core Migrations (Recommended for new deployments)
The migration `20250104000000_AddAuditLogging.cs` will be applied automatically when you start the application.

```bash
# Or manually apply migrations
cd DocN.Data
dotnet ef database update --startup-project ../DocN.Server
```

## Important Notes

⚠️ **Data Loss Warning**: This script drops the existing AuditLogs table. If you have existing audit data, back it up first!

```sql
-- Backup existing audit logs (if any)
SELECT * INTO AuditLogs_Backup FROM AuditLogs;
```

## Usage Examples

### Logging a Document Upload
```csharp
await _auditService.LogDocumentOperationAsync(
    "DocumentUploaded",
    documentId,
    fileName,
    new { FileSize = fileSize, ContentType = contentType }
);
```

### Logging Authentication
```csharp
await _auditService.LogAuthenticationAsync(
    "UserLogin",
    userId,
    username,
    success: true
);
```

### Logging Configuration Change
```csharp
await _auditService.LogConfigurationChangeAsync(
    "ConfigurationUpdated",
    configName,
    oldValue,
    newValue
);
```

### Querying Audit Logs
```csharp
// Get all document uploads in the last 7 days
var logs = await _auditService.GetAuditLogsAsync(
    startDate: DateTime.UtcNow.AddDays(-7),
    action: "DocumentUploaded",
    resourceType: "Document"
);

// Get failed operations
var failedLogs = await _context.AuditLogs
    .Where(a => !a.Success)
    .OrderByDescending(a => a.Timestamp)
    .Take(100)
    .ToListAsync();
```

## Query Performance

With the new indexes, common queries are highly optimized:
- User activity queries: Use IX_AuditLogs_UserId_Timestamp
- Action-specific queries: Use IX_AuditLogs_Action_Timestamp
- Resource tracking: Use IX_AuditLogs_ResourceType_ResourceId
- Tenant isolation: Use IX_AuditLogs_TenantId

## Retention Policy Recommendations

For compliance and performance:
- Keep hot data for 90 days in main table
- Archive older data to separate table or cold storage
- Minimum 1 year retention for SOC2 compliance
- Consider 7 years retention for GDPR compliance (depending on jurisdiction)

## Related Files

- **Migration**: `DocN.Data/Migrations/20250104000000_AddAuditLogging.cs`
- **Model**: `DocN.Data/Models/AuditLog.cs`
- **Service**: `DocN.Data/Services/AuditService.cs`
- **API Controller**: `DocN.Server/Controllers/AuditController.cs`
- **Documentation**: `AUDIT_HEALTH_SECURITY_IMPLEMENTATION.md`

## Version History

- **v1.0** (December 2024): Initial enhanced audit logging implementation
- Added GDPR/SOC2 compliance fields
- Added 8 optimized indexes
- Added tenant isolation support
