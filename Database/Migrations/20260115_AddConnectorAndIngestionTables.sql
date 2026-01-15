-- =============================================
-- Document Connectors and Ingestion System Migration Script
-- Version: 1.0
-- Date: 2026-01-15
-- Description: Adds document connectors, ingestion schedules, and ingestion logs tables
-- =============================================

PRINT 'Starting Document Connectors and Ingestion System Migration...'

-- =============================================
-- Table: DocumentConnectors
-- Description: Stores connection configurations for external document repositories
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentConnectors')
BEGIN
    PRINT 'Creating DocumentConnectors table...'
    
    CREATE TABLE DocumentConnectors (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(255) NOT NULL,
        ConnectorType NVARCHAR(50) NOT NULL, -- SharePoint, OneDrive, GoogleDrive, LocalFolder, FTP, SFTP
        
        -- Configuration and Credentials
        Configuration NVARCHAR(MAX) NOT NULL, -- JSON with endpoint URLs, folder paths, etc.
        EncryptedCredentials NVARCHAR(MAX) NULL, -- JSON with OAuth tokens, API keys, etc.
        
        -- Status
        IsActive BIT NOT NULL DEFAULT 1,
        LastConnectionTest DATETIME2 NULL,
        LastConnectionTestResult NVARCHAR(500) NULL,
        LastSyncedAt DATETIME2 NULL,
        
        -- Ownership and Tenancy
        OwnerId NVARCHAR(450) NULL,
        TenantId INT NULL,
        
        -- Audit
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Description NVARCHAR(1000) NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_DocumentConnectors_Tenant FOREIGN KEY (TenantId) 
            REFERENCES Tenants(Id) ON DELETE NO ACTION
    )
    
    -- Indexes for performance
    CREATE INDEX IX_DocumentConnectors_OwnerId ON DocumentConnectors(OwnerId)
    CREATE INDEX IX_DocumentConnectors_TenantId ON DocumentConnectors(TenantId)
    CREATE INDEX IX_DocumentConnectors_ConnectorType_IsActive ON DocumentConnectors(ConnectorType, IsActive)
    
    PRINT 'DocumentConnectors table created successfully.'
END
ELSE
BEGIN
    PRINT 'DocumentConnectors table already exists. Skipping...'
END

-- =============================================
-- Table: IngestionSchedules
-- Description: Stores ingestion schedules for document connectors
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IngestionSchedules')
BEGIN
    PRINT 'Creating IngestionSchedules table...'
    
    CREATE TABLE IngestionSchedules (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ConnectorId INT NOT NULL,
        Name NVARCHAR(255) NOT NULL,
        
        -- Schedule Configuration
        ScheduleType NVARCHAR(50) NOT NULL DEFAULT 'Manual', -- Manual, Scheduled, Continuous
        CronExpression NVARCHAR(100) NULL, -- For Scheduled type
        IntervalMinutes INT NULL, -- For Continuous type
        IsEnabled BIT NOT NULL DEFAULT 1,
        
        -- Document Configuration
        DefaultCategory NVARCHAR(255) NULL,
        DefaultVisibility INT NOT NULL DEFAULT 0, -- 0=Private, 1=Shared, 2=Organization, 3=Public
        FilterConfiguration NVARCHAR(MAX) NULL, -- JSON with file type filters, path patterns, date ranges
        
        -- Processing Options
        GenerateEmbeddingsImmediately BIT NOT NULL DEFAULT 0,
        EnableAIAnalysis BIT NOT NULL DEFAULT 1,
        
        -- Execution Status
        LastExecutedAt DATETIME2 NULL,
        NextExecutionAt DATETIME2 NULL,
        LastExecutionDocumentCount INT NOT NULL DEFAULT 0,
        LastExecutionStatus NVARCHAR(50) NULL,
        
        -- Ownership
        OwnerId NVARCHAR(450) NULL,
        
        -- Audit
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        Description NVARCHAR(1000) NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_IngestionSchedules_Connector FOREIGN KEY (ConnectorId) 
            REFERENCES DocumentConnectors(Id) ON DELETE CASCADE
    )
    
    -- Indexes for performance
    CREATE INDEX IX_IngestionSchedules_ConnectorId ON IngestionSchedules(ConnectorId)
    CREATE INDEX IX_IngestionSchedules_IsEnabled_NextExecutionAt ON IngestionSchedules(IsEnabled, NextExecutionAt)
    CREATE INDEX IX_IngestionSchedules_OwnerId ON IngestionSchedules(OwnerId)
    
    PRINT 'IngestionSchedules table created successfully.'
END
ELSE
BEGIN
    PRINT 'IngestionSchedules table already exists. Skipping...'
END

-- =============================================
-- Table: IngestionLogs
-- Description: Stores execution logs for ingestion schedules
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'IngestionLogs')
BEGIN
    PRINT 'Creating IngestionLogs table...'
    
    CREATE TABLE IngestionLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        IngestionScheduleId INT NOT NULL,
        
        -- Execution Info
        StartedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CompletedAt DATETIME2 NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Running', -- Running, Completed, Failed, Cancelled
        
        -- Statistics
        DocumentsDiscovered INT NOT NULL DEFAULT 0,
        DocumentsProcessed INT NOT NULL DEFAULT 0,
        DocumentsSkipped INT NOT NULL DEFAULT 0,
        DocumentsFailed INT NOT NULL DEFAULT 0,
        
        -- Details
        ErrorMessage NVARCHAR(MAX) NULL,
        DetailedLog NVARCHAR(MAX) NULL, -- JSON array of log messages
        
        -- Execution Context
        IsManualExecution BIT NOT NULL DEFAULT 0,
        TriggeredByUserId NVARCHAR(450) NULL,
        DurationSeconds INT NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_IngestionLogs_Schedule FOREIGN KEY (IngestionScheduleId) 
            REFERENCES IngestionSchedules(Id) ON DELETE CASCADE
    )
    
    -- Indexes for performance
    CREATE INDEX IX_IngestionLogs_IngestionScheduleId ON IngestionLogs(IngestionScheduleId)
    CREATE INDEX IX_IngestionLogs_StartedAt_Status ON IngestionLogs(StartedAt, Status)
    CREATE INDEX IX_IngestionLogs_TriggeredByUserId ON IngestionLogs(TriggeredByUserId)
    
    PRINT 'IngestionLogs table created successfully.'
END
ELSE
BEGIN
    PRINT 'IngestionLogs table already exists. Skipping...'
END

PRINT ''
PRINT '========================================='
PRINT 'Document Connectors and Ingestion System Migration Completed Successfully!'
PRINT '========================================='
PRINT ''
PRINT 'Tables created/verified:'
PRINT '  - DocumentConnectors'
PRINT '  - IngestionSchedules'
PRINT '  - IngestionLogs'
PRINT ''
PRINT 'Next steps:'
PRINT '  1. Configure connectors via UI at /connectors'
PRINT '  2. Create ingestion schedules via UI at /ingestion'
PRINT '  3. Monitor ingestion logs through the API'
PRINT ''
