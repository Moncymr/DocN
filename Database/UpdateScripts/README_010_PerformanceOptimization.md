# Database Performance Optimization for Monitoring Stack

## Overview
This script (`010_PerformanceOptimizationForMonitoring.sql`) optimizes the DocN database for the new monitoring, caching, and background job processing infrastructure.

## What's Included

### 1. Hangfire Indexes
- **IX_HangFire_Job_StateName_CreatedAt**: Optimizes job queue queries by state and creation time
- **IX_HangFire_Job_ExpireAt**: Filtered index for efficient cleanup of expired jobs

### 2. AuditLogs Optimizations
- **IX_AuditLogs_Severity_Timestamp**: Composite index for severity-based monitoring queries
- **IX_AuditLogs_Errors**: Filtered index for error tracking and alerting (only failed operations)
- **IX_AuditLogs_User_Action_Date**: Analytics index for user behavior analysis

### 3. Documents Caching Indexes
- **IX_Documents_Id_Include_Cache**: Covering index for fast cache lookups
- **IX_Documents_Category_ModifiedAt**: Filtered index for cache invalidation by category

### 4. DocumentChunks RAG Indexes
- **IX_DocumentChunks_Doc_Idx_Include**: Covering index for chunk retrieval with embeddings
- **IX_DocumentChunks_HasEmbedding768**: Filtered index for 768-dimension embeddings
- **IX_DocumentChunks_HasEmbedding1536**: Filtered index for 1536-dimension embeddings

### 5. Business Metrics Stored Procedures

#### sp_GetDocumentMetrics
Retrieves document upload metrics for business intelligence dashboards.

```sql
EXEC sp_GetDocumentMetrics 
    @StartDate = '2024-12-01', 
    @EndDate = '2024-12-31',
    @TenantId = NULL;
```

**Returns:**
- Documents uploaded per day
- Unique users per day
- Processing success/failure counts
- Average file size

**Use Cases:**
- Grafana dashboard showing document upload trends
- PowerBI reports for business KPIs
- Capacity planning

#### sp_GetSearchMetrics
Tracks search query performance and usage patterns.

```sql
EXEC sp_GetSearchMetrics 
    @StartDate = '2024-12-30', 
    @EndDate = '2024-12-31',
    @TenantId = 1;
```

**Returns:**
- Queries per hour
- Unique users per hour
- Success rate
- Error count

**Use Cases:**
- Real-time monitoring of search performance
- Identifying peak usage hours
- Query success rate tracking

#### sp_GetErrorMetrics
Aggregates errors for alerting and troubleshooting.

```sql
EXEC sp_GetErrorMetrics 
    @MinutesAgo = 60,
    @Severity = 'Error';
```

**Returns:**
- Error count by action and resource type
- Last occurrence timestamp
- Sample error messages

**Use Cases:**
- Prometheus alerting rules
- PagerDuty/Slack notifications
- Error trend analysis

### 6. Monitoring Views

#### vw_SystemHealthMetrics
Real-time system health overview for dashboards.

```sql
SELECT * FROM vw_SystemHealthMetrics;
```

**Provides:**
- Documents uploaded in last 24 hours
- Searches in last hour
- Errors in last hour
- Active users in last 24 hours
- Documents currently processing
- Failed documents in last 7 days

**Use Cases:**
- Grafana "System Health" panel
- Operations dashboard
- SLA monitoring

## Usage

### Running the Script

```bash
# Using sqlcmd
sqlcmd -S localhost -d DocNDb -i Database/UpdateScripts/010_PerformanceOptimizationForMonitoring.sql

# Or from SSMS
# Open the file and execute against DocNDb database
```

### Verification

After running the script, verify the optimizations:

```sql
-- Check indexes were created
SELECT 
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType
FROM sys.indexes i
WHERE i.name LIKE 'IX_%Hangfire%' 
   OR i.name LIKE 'IX_%Audit%'
   OR i.name LIKE 'IX_%Cache%'
ORDER BY TableName, IndexName;

-- Check stored procedures
SELECT name, create_date, modify_date
FROM sys.procedures
WHERE name LIKE 'sp_Get%Metrics'
ORDER BY name;

-- Check views
SELECT name, create_date, modify_date
FROM sys.views
WHERE name LIKE 'vw_%Metrics'
ORDER BY name;
```

## Performance Impact

### Expected Improvements

| Query Type | Before | After | Improvement |
|------------|--------|-------|-------------|
| AuditLog error queries | 500ms | 50ms | **10x faster** |
| Document cache lookups | 100ms | 10ms | **10x faster** |
| Chunk retrieval for RAG | 200ms | 30ms | **6x faster** |
| Business metrics aggregation | 2000ms | 200ms | **10x faster** |

### Storage Impact

- **Additional indexes**: ~50-100MB (depends on data volume)
- **Statistics**: ~5-10MB
- **Stored procedures/views**: <1MB

### CPU Impact

- **Auto-update statistics**: 1-3% CPU during off-peak hours
- **Index maintenance**: Minimal (handled by SQL Server automatically)

## Integration with Monitoring Stack

### Grafana Dashboard

Configure Grafana to use these queries:

```json
{
  "panels": [
    {
      "title": "Documents Uploaded (Last 7 Days)",
      "targets": [{
        "rawSql": "EXEC sp_GetDocumentMetrics @StartDate='2024-12-23', @EndDate='2024-12-30'"
      }]
    },
    {
      "title": "Search Performance",
      "targets": [{
        "rawSql": "EXEC sp_GetSearchMetrics @StartDate='2024-12-30', @EndDate='2024-12-31'"
      }]
    },
    {
      "title": "System Health",
      "targets": [{
        "rawSql": "SELECT * FROM vw_SystemHealthMetrics"
      }]
    }
  ]
}
```

### Prometheus Alerts

Use these queries for alerting:

```yaml
# prometheus-alerts.yml
groups:
  - name: docn-database
    interval: 1m
    rules:
      - alert: HighErrorRate
        expr: |
          (
            SELECT ErrorsLastHour FROM vw_SystemHealthMetrics
          ) > 100
        for: 5m
        annotations:
          summary: "High error rate detected"
          
      - alert: ProcessingBacklog
        expr: |
          (
            SELECT DocumentsProcessing FROM vw_SystemHealthMetrics
          ) > 50
        for: 15m
        annotations:
          summary: "Document processing backlog"
```

### PowerBI Reports

Connect PowerBI to SQL Server and use:

1. **Document Trends Report**: `sp_GetDocumentMetrics`
2. **Search Analytics Report**: `sp_GetSearchMetrics`
3. **Error Dashboard**: `sp_GetErrorMetrics`
4. **System Health**: `vw_SystemHealthMetrics`

## Maintenance

### Regular Tasks

1. **Weekly**: Check index fragmentation
```sql
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 30
ORDER BY ips.avg_fragmentation_in_percent DESC;
```

2. **Monthly**: Update statistics manually for critical tables
```sql
UPDATE STATISTICS Documents WITH FULLSCAN;
UPDATE STATISTICS DocumentChunks WITH FULLSCAN;
UPDATE STATISTICS AuditLogs WITH FULLSCAN;
```

3. **Quarterly**: Review unused indexes
```sql
SELECT 
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    us.user_seeks,
    us.user_scans,
    us.user_lookups,
    us.user_updates
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats us 
    ON i.object_id = us.object_id AND i.index_id = us.index_id
WHERE us.user_seeks = 0 
  AND us.user_scans = 0 
  AND us.user_lookups = 0
  AND i.name IS NOT NULL
ORDER BY us.user_updates DESC;
```

## Troubleshooting

### Index Not Created
**Symptom**: Script completes but index is missing

**Solution**:
```sql
-- Check for duplicate index
SELECT * FROM sys.indexes 
WHERE object_id = OBJECT_ID('TableName')
ORDER BY name;

-- Drop duplicate and recreate
DROP INDEX IX_OldIndexName ON TableName;
-- Then re-run optimization script
```

### Slow Query After Optimization
**Symptom**: Query slower than expected

**Solution**:
```sql
-- Force statistics update
UPDATE STATISTICS TableName WITH FULLSCAN;

-- Clear plan cache for that query
DBCC FREEPROCCACHE;

-- Re-run query and check execution plan
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
-- Your query here
```

### High CPU During Statistics Update
**Symptom**: CPU spikes when auto-statistics runs

**Solution**:
```sql
-- Switch to synchronous statistics update
ALTER DATABASE DocNDb SET AUTO_UPDATE_STATISTICS_ASYNC OFF;

-- Or increase threshold for updates
-- (requires trace flag, consult DBA)
```

## Best Practices

1. **Run during maintenance window**: Initial creation of indexes can be resource-intensive
2. **Monitor index usage**: Remove unused indexes after 3 months
3. **Test on non-production first**: Verify performance improvements before production
4. **Backup before running**: Always backup database before schema changes
5. **Schedule statistics updates**: Use SQL Agent jobs for regular statistics maintenance

## Related Documentation

- [MONITORING_AND_APM_IMPLEMENTATION.md](../../MONITORING_AND_APM_IMPLEMENTATION.md) - Monitoring stack overview
- [KUBERNETES_DEPLOYMENT.md](../../KUBERNETES_DEPLOYMENT.md) - Kubernetes deployment with monitoring
- [Database/README.md](../README.md) - General database documentation

---

**Created**: December 2024  
**Version**: 1.0  
**Compatibility**: SQL Server 2019+, Azure SQL Database
