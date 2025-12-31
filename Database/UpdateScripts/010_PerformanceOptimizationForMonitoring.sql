-- ================================================
-- DocN Database - Performance Optimization Script
-- SQL Server 2025 
-- Versione: 1.0 - Dicembre 2024
-- ================================================
-- SCOPO:
-- • Ottimizzazioni per il nuovo stack di monitoring (Serilog, OpenTelemetry, Hangfire)
-- • Indici aggiuntivi per performance delle nuove funzionalità
-- • Supporto per distributed caching e background jobs
-- ================================================

USE DocNDb;
GO

PRINT '';
PRINT '================================================';
PRINT '⚡ Ottimizzazione Database per Monitoring Stack';
PRINT '================================================';
PRINT '';

-- ================================================
-- 1. HANGFIRE TABLES VERIFICHE
-- ================================================
-- NOTA: Hangfire crea automaticamente le sue tabelle al primo avvio
-- Questo script verifica solo la loro esistenza e configurazione

PRINT '📋 Verifica tabelle Hangfire...';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'HangFire.Job')
BEGIN
    PRINT '  ✓ Tabelle Hangfire già create';
    
    -- Verifica indici Hangfire per performance
    IF NOT EXISTS (SELECT * FROM sys.indexes 
                   WHERE name = 'IX_HangFire_Job_StateName_CreatedAt' 
                   AND object_id = OBJECT_ID('[HangFire].[Job]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HangFire_Job_StateName_CreatedAt
        ON [HangFire].[Job] (StateName, CreatedAt DESC)
        INCLUDE (ExpireAt);
        PRINT '  ✓ Indice ottimizzato per Job State creato';
    END
    
    IF NOT EXISTS (SELECT * FROM sys.indexes 
                   WHERE name = 'IX_HangFire_Job_ExpireAt' 
                   AND object_id = OBJECT_ID('[HangFire].[Job]'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_HangFire_Job_ExpireAt
        ON [HangFire].[Job] (ExpireAt)
        WHERE ExpireAt IS NOT NULL;
        PRINT '  ✓ Indice filtered per Job Expiration creato';
    END
END
ELSE
BEGIN
    PRINT '  ℹ️  Tabelle Hangfire non ancora create (verranno create al primo avvio dell''applicazione)';
END
GO

-- ================================================
-- 2. OTTIMIZZAZIONI AUDITLOGS
-- ================================================
PRINT '';
PRINT '🔍 Ottimizzazione AuditLogs per query di monitoring...';

-- Indice composito per query temporali filtrate per severity
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_AuditLogs_Severity_Timestamp' 
               AND object_id = OBJECT_ID('AuditLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Severity_Timestamp
    ON AuditLogs (Severity, Timestamp DESC)
    INCLUDE (Action, ResourceType, Success);
    PRINT '  ✓ Indice per query severity-based creato';
END

-- Indice filtered per errori (per monitoring alerts)
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_AuditLogs_Errors' 
               AND object_id = OBJECT_ID('AuditLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Errors
    ON AuditLogs (Timestamp DESC, Action)
    INCLUDE (UserId, ResourceType, ErrorMessage)
    WHERE Success = 0;
    PRINT '  ✓ Indice filtered per errori creato';
END

-- Indice per aggregazioni per utente (analytics)
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_AuditLogs_User_Action_Date' 
               AND object_id = OBJECT_ID('AuditLogs'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AuditLogs_User_Action_Date
    ON AuditLogs (UserId, Action, CAST(Timestamp AS DATE))
    INCLUDE (Success, Severity);
    PRINT '  ✓ Indice per analytics utente creato';
END
GO

-- ================================================
-- 3. OTTIMIZZAZIONI DOCUMENTS PER CACHING
-- ================================================
PRINT '';
PRINT '💾 Ottimizzazione Documents per distributed caching...';

-- Indice per query di lookup rapido (cache warming)
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Documents_Id_Include_Cache' 
               AND object_id = OBJECT_ID('Documents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Documents_Id_Include_Cache
    ON Documents (Id)
    INCLUDE (FileName, ContentType, ActualCategory, UploadedAt, OwnerId);
    PRINT '  ✓ Indice covering per cache lookup creato';
END

-- Indice per query di invalidazione cache per categoria
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Documents_Category_ModifiedAt' 
               AND object_id = OBJECT_ID('Documents'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Documents_Category_ModifiedAt
    ON Documents (ActualCategory, ModifiedAt DESC)
    WHERE ActualCategory IS NOT NULL;
    PRINT '  ✓ Indice per invalidazione cache categoria creato';
END
GO

-- ================================================
-- 4. OTTIMIZZAZIONI DOCUMENTCHUNKS PER RAG
-- ================================================
PRINT '';
PRINT '🔤 Ottimizzazione DocumentChunks per RAG performance...';

-- Indice composito per retrieval chunks ordinati
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_DocumentChunks_Doc_Idx_Include' 
               AND object_id = OBJECT_ID('DocumentChunks'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DocumentChunks_Doc_Idx_Include
    ON DocumentChunks (DocumentId, ChunkIndex)
    INCLUDE (Content, EmbeddingVector, TokenCount);
    PRINT '  ✓ Indice covering per chunk retrieval creato';
END

-- Indice per vector search su chunks (se EmbeddingVector768 è popolato)
IF COL_LENGTH('DocumentChunks', 'EmbeddingVector768') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes 
                   WHERE name = 'IX_DocumentChunks_HasEmbedding768' 
                   AND object_id = OBJECT_ID('DocumentChunks'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_DocumentChunks_HasEmbedding768
        ON DocumentChunks (DocumentId)
        WHERE EmbeddingVector768 IS NOT NULL;
        PRINT '  ✓ Indice filtered per chunks con embedding 768 creato';
    END
END

IF COL_LENGTH('DocumentChunks', 'EmbeddingVector1536') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes 
                   WHERE name = 'IX_DocumentChunks_HasEmbedding1536' 
                   AND object_id = OBJECT_ID('DocumentChunks'))
    BEGIN
        CREATE NONCLUSTERED INDEX IX_DocumentChunks_HasEmbedding1536
        ON DocumentChunks (DocumentId)
        WHERE EmbeddingVector1536 IS NOT NULL;
        PRINT '  ✓ Indice filtered per chunks con embedding 1536 creato';
    END
END
GO

-- ================================================
-- 5. STATISTICHE E MAINTENANCE
-- ================================================
PRINT '';
PRINT '📊 Aggiornamento statistiche per query optimizer...';

-- Aggiorna statistiche per le tabelle principali
UPDATE STATISTICS Documents WITH FULLSCAN;
UPDATE STATISTICS DocumentChunks WITH FULLSCAN;
UPDATE STATISTICS AuditLogs WITH FULLSCAN;
PRINT '  ✓ Statistiche aggiornate';

-- ================================================
-- 6. STORED PROCEDURE PER MONITORING METRICS
-- ================================================
PRINT '';
PRINT '📈 Creazione stored procedures per metrics...';

-- Stored procedure per business metrics (documenti caricati/giorno)
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetDocumentMetrics')
    DROP PROCEDURE sp_GetDocumentMetrics;
GO

CREATE PROCEDURE sp_GetDocumentMetrics
    @StartDate DATETIME2(7),
    @EndDate DATETIME2(7),
    @TenantId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        CAST(UploadedAt AS DATE) AS Date,
        COUNT(*) AS DocumentCount,
        COUNT(DISTINCT OwnerId) AS UniqueUsers,
        SUM(CASE WHEN ProcessingStatus = 2 THEN 1 ELSE 0 END) AS ProcessedCount,
        SUM(CASE WHEN ProcessingStatus = 3 THEN 1 ELSE 0 END) AS FailedCount,
        AVG(CAST(FileSize AS BIGINT)) AS AvgFileSizeBytes
    FROM Documents
    WHERE UploadedAt >= @StartDate 
      AND UploadedAt < @EndDate
      AND (@TenantId IS NULL OR TenantId = @TenantId)
    GROUP BY CAST(UploadedAt AS DATE)
    ORDER BY Date DESC;
END
GO

PRINT '  ✓ sp_GetDocumentMetrics creata';

-- Stored procedure per query metrics (query/secondo)
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetSearchMetrics')
    DROP PROCEDURE sp_GetSearchMetrics;
GO

CREATE PROCEDURE sp_GetSearchMetrics
    @StartDate DATETIME2(7),
    @EndDate DATETIME2(7),
    @TenantId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0) AS Hour,
        COUNT(*) AS QueryCount,
        COUNT(DISTINCT UserId) AS UniqueUsers,
        AVG(CASE WHEN Success = 1 THEN 1.0 ELSE 0.0 END) AS SuccessRate,
        SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) AS ErrorCount
    FROM AuditLogs
    WHERE Action IN ('SearchDocuments', 'HybridSearch', 'VectorSearch')
      AND Timestamp >= @StartDate 
      AND Timestamp < @EndDate
      AND (@TenantId IS NULL OR TenantId = @TenantId)
    GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, Timestamp), 0)
    ORDER BY Hour DESC;
END
GO

PRINT '  ✓ sp_GetSearchMetrics creata';

-- Stored procedure per error metrics (per alerting)
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetErrorMetrics')
    DROP PROCEDURE sp_GetErrorMetrics;
GO

CREATE PROCEDURE sp_GetErrorMetrics
    @MinutesAgo INT = 60,
    @Severity NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StartTime DATETIME2(7) = DATEADD(MINUTE, -@MinutesAgo, GETUTCDATE());
    
    SELECT 
        Action,
        ResourceType,
        Severity,
        COUNT(*) AS ErrorCount,
        MAX(Timestamp) AS LastOccurrence,
        STRING_AGG(CAST(ErrorMessage AS NVARCHAR(MAX)), '; ') AS SampleErrors
    FROM AuditLogs
    WHERE Success = 0
      AND Timestamp >= @StartTime
      AND (@Severity IS NULL OR Severity = @Severity)
    GROUP BY Action, ResourceType, Severity
    HAVING COUNT(*) > 0
    ORDER BY ErrorCount DESC;
END
GO

PRINT '  ✓ sp_GetErrorMetrics creata';

-- ================================================
-- 7. VIEW PER MONITORING DASHBOARD
-- ================================================
PRINT '';
PRINT '📺 Creazione views per dashboard...';

-- View per statistiche real-time
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_SystemHealthMetrics')
    DROP VIEW vw_SystemHealthMetrics;
GO

CREATE VIEW vw_SystemHealthMetrics
AS
SELECT
    (SELECT COUNT(*) FROM Documents WHERE UploadedAt >= DATEADD(DAY, -1, GETUTCDATE())) AS DocumentsLast24h,
    (SELECT COUNT(*) FROM AuditLogs WHERE Action LIKE '%Search%' AND Timestamp >= DATEADD(HOUR, -1, GETUTCDATE())) AS SearchesLastHour,
    (SELECT COUNT(*) FROM AuditLogs WHERE Success = 0 AND Timestamp >= DATEADD(HOUR, -1, GETUTCDATE())) AS ErrorsLastHour,
    (SELECT COUNT(DISTINCT UserId) FROM AuditLogs WHERE Timestamp >= DATEADD(DAY, -1, GETUTCDATE())) AS ActiveUsersLast24h,
    (SELECT COUNT(*) FROM Documents WHERE ProcessingStatus = 1) AS DocumentsProcessing,
    (SELECT COUNT(*) FROM Documents WHERE ProcessingStatus = 3 AND UploadedAt >= DATEADD(DAY, -7, GETUTCDATE())) AS FailedDocumentsLast7d;
GO

PRINT '  ✓ vw_SystemHealthMetrics creata';

-- ================================================
-- 8. CONFIGURAZIONE AUTOMATIC STATISTICS
-- ================================================
PRINT '';
PRINT '⚙️  Configurazione auto-update statistics...';

-- Abilita auto-update statistics con threshold più basso per tabelle grandi
ALTER DATABASE CURRENT SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE CURRENT SET AUTO_UPDATE_STATISTICS_ASYNC ON;
PRINT '  ✓ Auto-update statistics abilitato';

-- ================================================
-- RIEPILOGO
-- ================================================
PRINT '';
PRINT '================================================';
PRINT '✅ Ottimizzazione completata con successo!';
PRINT '================================================';
PRINT '';
PRINT 'Modifiche applicate:';
PRINT '  • Indici Hangfire per performance job queue';
PRINT '  • Indici AuditLogs per monitoring e alerts';
PRINT '  • Indici Documents per distributed caching';
PRINT '  • Indici DocumentChunks per RAG performance';
PRINT '  • Stored procedures per business metrics';
PRINT '  • Views per dashboard real-time';
PRINT '  • Statistiche aggiornate';
PRINT '';
PRINT 'Prossimi passi:';
PRINT '  1. Verificare performance con query reali';
PRINT '  2. Monitorare utilizzo indici con DMV';
PRINT '  3. Configurare maintenance plan per rebuild/reorganize';
PRINT '  4. Integrare metriche con Grafana/PowerBI';
PRINT '';
GO
