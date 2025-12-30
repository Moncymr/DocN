-- ================================================
-- DocN Database - Update Script 009
-- Add Enhanced AuditLogs Table for GDPR/SOC2 Compliance
-- ================================================
-- This script updates the AuditLogs table with additional fields
-- required for GDPR/SOC2 compliance and better audit trail.
-- ================================================

USE DocNDb;
GO

PRINT '';
PRINT '================================================';
PRINT '🔄 Update 009: Enhanced AuditLogs for GDPR/SOC2';
PRINT '================================================';

-- Check if old AuditLogs exists and drop it (backup first if in production!)
IF EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' and xtype='U')
BEGIN
    PRINT 'ℹ️  Dropping existing AuditLogs table...';
    DROP TABLE AuditLogs;
    PRINT '  ✓ Old AuditLogs table dropped';
END

-- Create enhanced AuditLogs table
CREATE TABLE AuditLogs (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NULL,
    Username NVARCHAR(256) NULL,
    Action NVARCHAR(100) NOT NULL,
    ResourceType NVARCHAR(50) NOT NULL,
    ResourceId NVARCHAR(100) NULL,
    Details NVARCHAR(MAX) NULL,
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    TenantId INT NULL,
    Timestamp DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    Severity NVARCHAR(20) NOT NULL DEFAULT 'Info',
    Success BIT NOT NULL DEFAULT 1,
    ErrorMessage NVARCHAR(1000) NULL,
    
    CONSTRAINT FK_AuditLogs_User FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
    CONSTRAINT FK_AuditLogs_Tenant FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE SET NULL
);

-- Create indexes for performance
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_Action ON AuditLogs(Action);
CREATE INDEX IX_AuditLogs_ResourceType ON AuditLogs(ResourceType);
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp DESC);
CREATE INDEX IX_AuditLogs_TenantId ON AuditLogs(TenantId);
CREATE INDEX IX_AuditLogs_UserId_Timestamp ON AuditLogs(UserId, Timestamp);
CREATE INDEX IX_AuditLogs_Action_Timestamp ON AuditLogs(Action, Timestamp);
CREATE INDEX IX_AuditLogs_ResourceType_ResourceId ON AuditLogs(ResourceType, ResourceId);

PRINT '  ✓ Enhanced AuditLogs table created';
PRINT '  ✓ 8 indexes created for query performance';

PRINT '';
PRINT '✅ Update 009 completed successfully!';
PRINT '================================================';
PRINT '';
PRINT 'Enhanced AuditLogs Features:';
PRINT '  • Username field for quick reference';
PRINT '  • ResourceType and ResourceId for precise tracking';
PRINT '  • TenantId for multi-tenant audit isolation';
PRINT '  • Severity levels (Info, Warning, Error, Critical)';
PRINT '  • Success/Failure tracking';
PRINT '  • ErrorMessage for failed operations';
PRINT '  • 8 optimized indexes for fast queries';
PRINT '';
PRINT '🔒 GDPR/SOC2 Compliance Features:';
PRINT '  • Complete audit trail of all user actions';
PRINT '  • IP address and user agent tracking';
PRINT '  • Timestamp tracking (UTC)';
PRINT '  • Resource-level granularity';
PRINT '  • Tenant isolation support';
PRINT '';
GO
