# Migration: Document Connectors and Ingestion System

**Version:** 1.0  
**Date:** 2026-01-15  
**Script:** `20260115_AddConnectorAndIngestionTables.sql`

## Overview

This migration adds support for external document connectors and scheduled/continuous ingestion, allowing the system to automatically import documents from external repositories beyond manual uploads.

## Changes

### New Tables

#### 1. **DocumentConnectors**
Stores connection configurations for external document repositories.

**Fields:**
- `Id`: Primary key
- `Name`: User-defined name for the connector
- `ConnectorType`: Type (SharePoint, OneDrive, GoogleDrive, LocalFolder, FTP, SFTP)
- `Configuration`: JSON with endpoint URLs, folder paths, etc.
- `EncryptedCredentials`: JSON with OAuth tokens, API keys (encrypted)
- `IsActive`: Whether the connector is active
- `LastConnectionTest`: Last successful connection test timestamp
- `LastConnectionTestResult`: Result message from last test
- `LastSyncedAt`: Last successful sync timestamp
- `OwnerId`: Owner of the connector
- `TenantId`: Multi-tenancy support
- `CreatedAt`, `UpdatedAt`, `Description`: Audit fields

**Indexes:**
- `IX_DocumentConnectors_OwnerId`
- `IX_DocumentConnectors_TenantId`
- `IX_DocumentConnectors_ConnectorType_IsActive`

#### 2. **IngestionSchedules**
Stores ingestion schedules for document connectors.

**Fields:**
- `Id`: Primary key
- `ConnectorId`: Foreign key to DocumentConnectors
- `Name`: User-defined name for the schedule
- `ScheduleType`: Manual, Scheduled, or Continuous
- `CronExpression`: Cron expression for scheduled ingestion
- `IntervalMinutes`: Interval for continuous ingestion
- `IsEnabled`: Whether the schedule is active
- `DefaultCategory`: Default category for ingested documents
- `DefaultVisibility`: Default visibility level
- `FilterConfiguration`: JSON with filters (file types, paths, dates)
- `GenerateEmbeddingsImmediately`: Whether to generate embeddings immediately
- `EnableAIAnalysis`: Whether to enable AI analysis
- `LastExecutedAt`, `NextExecutionAt`: Execution timing
- `LastExecutionDocumentCount`, `LastExecutionStatus`: Last execution stats
- `OwnerId`: Owner of the schedule
- `CreatedAt`, `UpdatedAt`, `Description`: Audit fields

**Indexes:**
- `IX_IngestionSchedules_ConnectorId`
- `IX_IngestionSchedules_IsEnabled_NextExecutionAt`
- `IX_IngestionSchedules_OwnerId`

#### 3. **IngestionLogs**
Stores execution logs for ingestion runs.

**Fields:**
- `Id`: Primary key
- `IngestionScheduleId`: Foreign key to IngestionSchedules
- `StartedAt`, `CompletedAt`: Execution timestamps
- `Status`: Running, Completed, Failed, Cancelled
- `DocumentsDiscovered`, `DocumentsProcessed`, `DocumentsSkipped`, `DocumentsFailed`: Statistics
- `ErrorMessage`: Error details if failed
- `DetailedLog`: JSON array of detailed log messages
- `IsManualExecution`: Whether triggered manually
- `TriggeredByUserId`: User who triggered manual execution
- `DurationSeconds`: Execution duration

**Indexes:**
- `IX_IngestionLogs_IngestionScheduleId`
- `IX_IngestionLogs_StartedAt_Status`
- `IX_IngestionLogs_TriggeredByUserId`

## Prerequisites

- The `Tenants` table must exist (for multi-tenancy support)
- SQL Server 2016 or later

## How to Run

### Option 1: Using SQL Server Management Studio (SSMS)
1. Open SSMS and connect to your database
2. Open the migration script `20260115_AddConnectorAndIngestionTables.sql`
3. Execute the script
4. Verify the output messages confirm successful table creation

### Option 2: Using sqlcmd
```bash
sqlcmd -S <server> -d <database> -U <username> -P <password> -i 20260115_AddConnectorAndIngestionTables.sql
```

### Option 3: Using Entity Framework Core
```bash
cd DocN.Data
dotnet ef migrations add AddConnectorAndIngestionTables
dotnet ef database update
```

## Post-Migration Steps

1. **Configure Connectors**: Navigate to `/connectors` in the UI to create your first connector
2. **Create Schedules**: Navigate to `/ingestion` to set up ingestion schedules
3. **Monitor Logs**: Use the API endpoints to monitor ingestion execution:
   - `GET /Ingestion/schedules/{id}/logs` - View execution logs

## API Endpoints

### Connectors
- `GET /Connectors` - List all connectors
- `POST /Connectors` - Create a new connector
- `PUT /Connectors/{id}` - Update a connector
- `DELETE /Connectors/{id}` - Delete a connector
- `POST /Connectors/{id}/test` - Test connection
- `GET /Connectors/{id}/files` - List files from connector

### Ingestion Schedules
- `GET /Ingestion/schedules` - List all schedules
- `POST /Ingestion/schedules` - Create a new schedule
- `PUT /Ingestion/schedules/{id}` - Update a schedule
- `DELETE /Ingestion/schedules/{id}` - Delete a schedule
- `POST /Ingestion/schedules/{id}/execute` - Execute manually
- `GET /Ingestion/schedules/{id}/logs` - View execution logs

## Configuration Examples

### SharePoint Connector
```json
{
  "siteUrl": "https://yourtenant.sharepoint.com",
  "folderPath": "/Shared Documents",
  "clientId": "your-app-client-id",
  "tenantId": "your-tenant-id"
}
```

### Scheduled Ingestion (Daily at Midnight)
```json
{
  "scheduleType": "Scheduled",
  "cronExpression": "0 0 * * *",
  "enableAIAnalysis": true,
  "defaultCategory": "Imported Documents",
  "defaultVisibility": 0
}
```

### Continuous Ingestion (Every 30 minutes)
```json
{
  "scheduleType": "Continuous",
  "intervalMinutes": 30,
  "enableAIAnalysis": true,
  "generateEmbeddingsImmediately": false
}
```

## Rollback

To rollback this migration, execute:

```sql
DROP TABLE IF EXISTS IngestionLogs
DROP TABLE IF EXISTS IngestionSchedules
DROP TABLE IF EXISTS DocumentConnectors
```

## Notes

- Connector credentials are stored as encrypted JSON for security
- Cron expressions use standard Unix cron syntax
- Background worker services will automatically pick up enabled schedules
- Continuous ingestion uses polling; consider performance implications

## Support

For issues or questions:
1. Check the application logs for detailed error messages
2. Verify network connectivity to external repositories
3. Ensure proper authentication credentials are configured
4. Review ingestion logs via the API for detailed execution information
