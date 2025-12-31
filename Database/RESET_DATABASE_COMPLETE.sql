-- ================================================
-- DocN Database - RESET COMPLETO E RICREAZIONE
-- Database: DocNDb  
-- SQL Server 2025 con supporto VECTOR
-- Versione: 6.0 - Dicembre 2024 (Post-Fix LogEntries)
-- ================================================
-- ATTENZIONE: Questo script ELIMINA COMPLETAMENTE il database e lo ricrea da zero!
-- TUTTI I DATI VERRANNO PERSI!
-- 
-- USO:
-- 1. Aprire SQL Server Management Studio
-- 2. Connettersi al server SQL
-- 3. Aprire questo script
-- 4. Eseguire l'intero script (F5)
-- ================================================
-- NOVITÀ V6:
-- • Fix LogEntries: consolidata in ApplicationDbContext
-- • LogService ora usa ApplicationDbContext invece di DocArcContext
-- • Entrambe AuditLogs e LogEntries in stesso database context
-- • Migrazioni automatiche applicano correttamente tutte le tabelle
-- ================================================

USE master;
GO

-- ================================================
-- 1. DISCONNETTI TUTTE LE CONNESSIONI ATTIVE
-- ================================================
PRINT '🔄 Disconnessione connessioni attive...';

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'DocNDb')
BEGIN
    ALTER DATABASE DocNDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    PRINT '  ✓ Connessioni disconnesse';
END
GO

-- ================================================
-- 2. ELIMINA DATABASE ESISTENTE
-- ================================================
PRINT '🗑️  Eliminazione database esistente...';

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'DocNDb')
BEGIN
    DROP DATABASE DocNDb;
    PRINT '  ✓ Database DocNDb eliminato';
END
ELSE
BEGIN
    PRINT '  ℹ️  Database DocNDb non esistente (ok)';
END
GO

-- ================================================
-- 3. CREAZIONE NUOVO DATABASE
-- ================================================
PRINT '📦 Creazione nuovo database...';

CREATE DATABASE DocNDb;
PRINT '  ✓ Database DocNDb creato';
GO

USE DocNDb;
GO

PRINT '✅ Ora utilizzo database DocNDb';
PRINT '';

-- ================================================
-- 4. CREAZIONE TABELLE IDENTITY (ASP.NET Core Identity)
-- ================================================
PRINT '👤 Creazione tabelle Identity...';

-- AspNetRoles
CREATE TABLE AspNetRoles (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    Name NVARCHAR(256) NULL,
    NormalizedName NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL
);
CREATE UNIQUE INDEX IX_AspNetRoles_NormalizedName ON AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
PRINT '  ✓ AspNetRoles creata';

-- AspNetUsers
CREATE TABLE AspNetUsers (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    UserName NVARCHAR(256) NULL,
    NormalizedUserName NVARCHAR(256) NULL,
    Email NVARCHAR(256) NULL,
    NormalizedEmail NVARCHAR(256) NULL,
    EmailConfirmed BIT NOT NULL,
    PasswordHash NVARCHAR(MAX) NULL,
    SecurityStamp NVARCHAR(MAX) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(MAX) NULL,
    PhoneNumberConfirmed BIT NOT NULL,
    TwoFactorEnabled BIT NOT NULL,
    LockoutEnd DATETIMEOFFSET(7) NULL,
    LockoutEnabled BIT NOT NULL,
    AccessFailedCount INT NOT NULL,
    TenantId INT NULL,
    FullName NVARCHAR(256) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    LastLoginAt DATETIME2(7) NULL
);
CREATE UNIQUE INDEX IX_AspNetUsers_NormalizedUserName ON AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
CREATE INDEX IX_AspNetUsers_NormalizedEmail ON AspNetUsers(NormalizedEmail);
CREATE INDEX IX_AspNetUsers_TenantId ON AspNetUsers(TenantId);
PRINT '  ✓ AspNetUsers creata';

-- AspNetUserClaims
CREATE TABLE AspNetUserClaims (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserClaims_UserId ON AspNetUserClaims(UserId);
PRINT '  ✓ AspNetUserClaims creata';

-- AspNetUserLogins
CREATE TABLE AspNetUserLogins (
    LoginProvider NVARCHAR(450) NOT NULL,
    ProviderKey NVARCHAR(450) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX) NULL,
    UserId NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserLogins_UserId ON AspNetUserLogins(UserId);
PRINT '  ✓ AspNetUserLogins creata';

-- AspNetUserTokens
CREATE TABLE AspNetUserTokens (
    UserId NVARCHAR(450) NOT NULL,
    LoginProvider NVARCHAR(450) NOT NULL,
    Name NVARCHAR(450) NOT NULL,
    Value NVARCHAR(MAX) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
PRINT '  ✓ AspNetUserTokens creata';

-- AspNetRoleClaims
CREATE TABLE AspNetRoleClaims (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleId NVARCHAR(450) NOT NULL,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL,
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) 
        REFERENCES AspNetRoles(Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims(RoleId);
PRINT '  ✓ AspNetRoleClaims creata';

-- AspNetUserRoles
CREATE TABLE AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) 
        REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserRoles_RoleId ON AspNetUserRoles(RoleId);
PRINT '  ✓ AspNetUserRoles creata';

PRINT '';

-- ================================================
-- 5. CREAZIONE TABELLE MULTI-TENANT
-- ================================================
PRINT '🏢 Creazione tabelle Multi-Tenant...';

-- Tenants
CREATE TABLE Tenants (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    MaxUsers INT NULL,
    MaxStorageGB INT NULL
);
CREATE INDEX IX_Tenants_Name ON Tenants(Name);
PRINT '  ✓ Tenants creata';

-- Aggiungi FK da AspNetUsers a Tenants
ALTER TABLE AspNetUsers ADD CONSTRAINT FK_AspNetUsers_Tenants_TenantId 
    FOREIGN KEY (TenantId) REFERENCES Tenants(Id) ON DELETE SET NULL;
PRINT '  ✓ FK AspNetUsers -> Tenants aggiunta';

PRINT '';

-- ================================================
-- 6. CREAZIONE TABELLE DOCUMENTI
-- ================================================
PRINT '📄 Creazione tabelle Documenti...';

-- Documents
CREATE TABLE Documents (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    ContentType NVARCHAR(100) NOT NULL,
    FileSize BIGINT NOT NULL,
    ExtractedText NVARCHAR(MAX) NOT NULL,
    SuggestedCategory NVARCHAR(100) NULL,
    CategoryReasoning NVARCHAR(2000) NULL,
    ActualCategory NVARCHAR(100) NULL,
    Visibility INT NOT NULL DEFAULT 0,
    OwnerId NVARCHAR(450) NULL,
    UploadedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    LastAccessedAt DATETIME2(7) NULL,
    AccessCount INT NOT NULL DEFAULT 0,
    TenantId INT NULL,
    -- Vector fields (JSON format per SQL Server 2025 VECTOR type compatibility)
    EmbeddingVector768 NVARCHAR(MAX) NULL,
    EmbeddingVector1536 NVARCHAR(MAX) NULL,
    EmbeddingDimension INT NULL,
    -- AI metadata fields
    AITagsJson NVARCHAR(MAX) NULL,
    AIAnalysisDate DATETIME2(7) NULL,
    ExtractedMetadataJson NVARCHAR(MAX) NULL,
    -- Document processing metadata
    PageCount INT NULL,
    DetectedLanguage NVARCHAR(50) NULL,
    ProcessingStatus NVARCHAR(50) NULL,
    ProcessingError NVARCHAR(MAX) NULL,
    Notes NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Documents_AspNetUsers_OwnerId FOREIGN KEY (OwnerId) 
        REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
    CONSTRAINT FK_Documents_Tenants_TenantId FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE SET NULL
);
CREATE INDEX IX_Documents_OwnerId ON Documents(OwnerId);
CREATE INDEX IX_Documents_UploadedAt ON Documents(UploadedAt DESC);
CREATE INDEX IX_Documents_Visibility ON Documents(Visibility);
CREATE INDEX IX_Documents_SuggestedCategory ON Documents(SuggestedCategory);
CREATE INDEX IX_Documents_ActualCategory ON Documents(ActualCategory);
CREATE INDEX IX_Documents_TenantId ON Documents(TenantId);
PRINT '  ✓ Documents creata';

-- DocumentTags
CREATE TABLE DocumentTags (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DocumentId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_DocumentTags_Documents_DocumentId FOREIGN KEY (DocumentId) 
        REFERENCES Documents(Id) ON DELETE CASCADE
);
CREATE INDEX IX_DocumentTags_DocumentId ON DocumentTags(DocumentId);
CREATE INDEX IX_DocumentTags_Name ON DocumentTags(Name);
PRINT '  ✓ DocumentTags creata';

-- DocumentChunks (per RAG chunking strategy)
CREATE TABLE DocumentChunks (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DocumentId INT NOT NULL,
    ChunkText NVARCHAR(MAX) NOT NULL,
    ChunkIndex INT NOT NULL,
    TokenCount INT NULL,
    -- Vector fields per chunks
    ChunkEmbedding768 NVARCHAR(MAX) NULL,
    ChunkEmbedding1536 NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    StartPosition INT NOT NULL,
    EndPosition INT NOT NULL,
    CONSTRAINT FK_DocumentChunks_Documents_DocumentId FOREIGN KEY (DocumentId) 
        REFERENCES Documents(Id) ON DELETE CASCADE
);
CREATE INDEX IX_DocumentChunks_DocumentId ON DocumentChunks(DocumentId);
CREATE INDEX IX_DocumentChunks_ChunkIndex ON DocumentChunks(DocumentId, ChunkIndex);
PRINT '  ✓ DocumentChunks creata';

-- DocumentShares (per condivisione documenti)
CREATE TABLE DocumentShares (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DocumentId INT NOT NULL,
    SharedWithUserId NVARCHAR(450) NOT NULL,
    SharedByUserId NVARCHAR(450) NOT NULL,
    SharedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CanEdit BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_DocumentShares_Documents_DocumentId FOREIGN KEY (DocumentId) 
        REFERENCES Documents(Id) ON DELETE CASCADE,
    CONSTRAINT FK_DocumentShares_AspNetUsers_SharedWithUserId FOREIGN KEY (SharedWithUserId) 
        REFERENCES AspNetUsers(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_DocumentShares_AspNetUsers_SharedByUserId FOREIGN KEY (SharedByUserId) 
        REFERENCES AspNetUsers(Id) ON DELETE NO ACTION
);
CREATE INDEX IX_DocumentShares_DocumentId ON DocumentShares(DocumentId);
CREATE INDEX IX_DocumentShares_SharedWithUserId ON DocumentShares(SharedWithUserId);
PRINT '  ✓ DocumentShares creata';

-- SimilarDocuments (similarità vettoriale tra documenti)
CREATE TABLE SimilarDocuments (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    SourceDocumentId INT NOT NULL,
    SimilarDocumentId INT NOT NULL,
    SimilarityScore FLOAT NOT NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_SimilarDocuments_Documents_SourceDocumentId FOREIGN KEY (SourceDocumentId) 
        REFERENCES Documents(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_SimilarDocuments_Documents_SimilarDocumentId FOREIGN KEY (SimilarDocumentId) 
        REFERENCES Documents(Id) ON DELETE NO ACTION
);
CREATE INDEX IX_SimilarDocuments_SourceDocumentId ON SimilarDocuments(SourceDocumentId);
CREATE INDEX IX_SimilarDocuments_SimilarityScore ON SimilarDocuments(SimilarityScore DESC);
PRINT '  ✓ SimilarDocuments creata';

PRINT '';

-- ================================================
-- 7. CREAZIONE TABELLE AI CONFIGURATION
-- ================================================
PRINT '🤖 Creazione tabelle AI Configuration...';

-- AIConfigurations (multi-provider AI support)
CREATE TABLE AIConfigurations (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    ProviderType NVARCHAR(50) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    ApiKey NVARCHAR(500) NULL,
    Endpoint NVARCHAR(500) NULL,
    Model NVARCHAR(100) NULL,
    EmbeddingModel NVARCHAR(100) NULL,
    MaxTokens INT NULL,
    Temperature FLOAT NULL,
    TopP FLOAT NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AIConfigurations_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
CREATE INDEX IX_AIConfigurations_UserId ON AIConfigurations(UserId);
CREATE INDEX IX_AIConfigurations_ProviderType ON AIConfigurations(ProviderType);
PRINT '  ✓ AIConfigurations creata';

-- AgentTemplates (template predefiniti per agenti AI)
CREATE TABLE AgentTemplates (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    Category NVARCHAR(100) NOT NULL,
    SystemPrompt NVARCHAR(MAX) NOT NULL,
    DefaultModel NVARCHAR(100) NULL,
    DefaultTemperature FLOAT NULL,
    DefaultMaxTokens INT NULL,
    IsBuiltIn BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_AgentTemplates_Category ON AgentTemplates(Category);
CREATE INDEX IX_AgentTemplates_IsActive ON AgentTemplates(IsActive);
PRINT '  ✓ AgentTemplates creata';

-- AgentConfigurations (configurazioni personalizzate utente)
CREATE TABLE AgentConfigurations (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    TemplateId INT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    SystemPrompt NVARCHAR(MAX) NOT NULL,
    Model NVARCHAR(100) NULL,
    Temperature FLOAT NULL,
    MaxTokens INT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AgentConfigurations_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AgentConfigurations_AgentTemplates_TemplateId FOREIGN KEY (TemplateId) 
        REFERENCES AgentTemplates(Id) ON DELETE SET NULL
);
CREATE INDEX IX_AgentConfigurations_UserId ON AgentConfigurations(UserId);
CREATE INDEX IX_AgentConfigurations_TemplateId ON AgentConfigurations(TemplateId);
PRINT '  ✓ AgentConfigurations creata';

-- AgentUsageLogs (tracking uso agenti)
CREATE TABLE AgentUsageLogs (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    AgentConfigurationId INT NULL,
    Action NVARCHAR(100) NOT NULL,
    InputTokens INT NULL,
    OutputTokens INT NULL,
    TotalTokens INT NULL,
    ExecutionTimeMs INT NULL,
    Success BIT NOT NULL DEFAULT 1,
    ErrorMessage NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AgentUsageLogs_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_AgentUsageLogs_AgentConfigurations_AgentConfigurationId FOREIGN KEY (AgentConfigurationId) 
        REFERENCES AgentConfigurations(Id) ON DELETE SET NULL
);
CREATE INDEX IX_AgentUsageLogs_UserId ON AgentUsageLogs(UserId);
CREATE INDEX IX_AgentUsageLogs_CreatedAt ON AgentUsageLogs(CreatedAt DESC);
PRINT '  ✓ AgentUsageLogs creata';

PRINT '';

-- ================================================
-- 8. CREAZIONE TABELLE RAG (Conversazioni e Messaggi)
-- ================================================
PRINT '💬 Creazione tabelle RAG...';

-- Conversations
CREATE TABLE Conversations (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    Title NVARCHAR(500) NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    TenantId INT NULL,
    CONSTRAINT FK_Conversations_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Conversations_Tenants_TenantId FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE SET NULL
);
CREATE INDEX IX_Conversations_UserId ON Conversations(UserId);
CREATE INDEX IX_Conversations_CreatedAt ON Conversations(CreatedAt DESC);
PRINT '  ✓ Conversations creata';

-- Messages
CREATE TABLE Messages (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ConversationId INT NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    ReferencedDocumentIds NVARCHAR(MAX) NULL,
    CONSTRAINT FK_Messages_Conversations_ConversationId FOREIGN KEY (ConversationId) 
        REFERENCES Conversations(Id) ON DELETE CASCADE
);
CREATE INDEX IX_Messages_ConversationId ON Messages(ConversationId);
CREATE INDEX IX_Messages_CreatedAt ON Messages(CreatedAt);
PRINT '  ✓ Messages creata';

PRINT '';

-- ================================================
-- 9. CREAZIONE TABELLE LOGGING E AUDIT
-- ================================================
PRINT '📋 Creazione tabelle Logging e Audit...';

-- LogEntries (logging centralizzato applicazione) - FIX V6
CREATE TABLE LogEntries (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Timestamp DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    Level NVARCHAR(50) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    Message NVARCHAR(2000) NOT NULL,
    Details NVARCHAR(MAX) NULL,
    UserId NVARCHAR(450) NULL,
    FileName NVARCHAR(500) NULL,
    StackTrace NVARCHAR(MAX) NULL
);
CREATE INDEX IX_LogEntries_Timestamp ON LogEntries(Timestamp DESC);
CREATE INDEX IX_LogEntries_Category_Timestamp ON LogEntries(Category, Timestamp DESC);
CREATE INDEX IX_LogEntries_UserId_Timestamp ON LogEntries(UserId, Timestamp DESC);
CREATE INDEX IX_LogEntries_Level ON LogEntries(Level);
PRINT '  ✓ LogEntries creata (con fix ApplicationDbContext)';

-- AuditLogs (GDPR/SOC2 compliance) - Enhanced V5
CREATE TABLE AuditLogs (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Timestamp DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    UserId NVARCHAR(450) NULL,
    Username NVARCHAR(256) NULL,
    Action NVARCHAR(100) NOT NULL,
    ResourceType NVARCHAR(50) NOT NULL,
    ResourceId NVARCHAR(100) NULL,
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,
    Details NVARCHAR(MAX) NULL,
    Success BIT NOT NULL DEFAULT 1,
    Severity NVARCHAR(20) NULL,
    ErrorMessage NVARCHAR(1000) NULL,
    TenantId INT NULL,
    CONSTRAINT FK_AuditLogs_AspNetUsers_UserId FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
    CONSTRAINT FK_AuditLogs_Tenants_TenantId FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE SET NULL
);
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_Action ON AuditLogs(Action);
CREATE INDEX IX_AuditLogs_ResourceType ON AuditLogs(ResourceType);
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp DESC);
CREATE INDEX IX_AuditLogs_TenantId ON AuditLogs(TenantId);
CREATE INDEX IX_AuditLogs_UserId_Timestamp ON AuditLogs(UserId, Timestamp DESC);
CREATE INDEX IX_AuditLogs_Action_Timestamp ON AuditLogs(Action, Timestamp DESC);
CREATE INDEX IX_AuditLogs_ResourceType_ResourceId ON AuditLogs(ResourceType, ResourceId);
PRINT '  ✓ AuditLogs creata (Enhanced per GDPR/SOC2)';

PRINT '';

-- ================================================
-- 10. CREAZIONE STORED PROCEDURES
-- ================================================
PRINT '⚙️  Creazione Stored Procedures...';

-- Stored procedure per pulizia vecchi log
CREATE PROCEDURE sp_CleanupOldLogEntries
    @DaysToKeep INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@DaysToKeep, GETUTCDATE());
    
    DELETE FROM LogEntries WHERE Timestamp < @CutoffDate;
    
    SELECT @@ROWCOUNT AS DeletedRows;
END
GO
PRINT '  ✓ sp_CleanupOldLogEntries creata';

-- Stored procedure per pulizia vecchi audit logs
CREATE PROCEDURE sp_CleanupOldAuditLogs
    @DaysToKeep INT = 365
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@DaysToKeep, GETUTCDATE());
    
    DELETE FROM AuditLogs WHERE Timestamp < @CutoffDate;
    
    SELECT @@ROWCOUNT AS DeletedRows;
END
GO
PRINT '  ✓ sp_CleanupOldAuditLogs creata';

PRINT '';

-- ================================================
-- 11. INSERIMENTO DATI INIZIALI
-- ================================================
PRINT '💾 Inserimento dati iniziali...';

-- Ruoli predefiniti
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'Admin')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());
    PRINT '  ✓ Ruolo Admin creato';
END

IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE Name = 'User')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'User', 'USER', NEWID());
    PRINT '  ✓ Ruolo User creato';
END

-- Tenant di default (opzionale)
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE Name = 'Default')
BEGIN
    INSERT INTO Tenants (Name, Description, IsActive, MaxUsers, MaxStorageGB)
    VALUES ('Default', 'Tenant predefinito', 1, NULL, NULL);
    PRINT '  ✓ Tenant Default creato';
END

PRINT '';

-- ================================================
-- 12. TABELLA MIGRAZIONI EF CORE
-- ================================================
PRINT '🔄 Creazione tabella migrazioni EF Core...';

-- __EFMigrationsHistory (gestita da EF Core ma la creiamo per compatibilità)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='__EFMigrationsHistory' and xtype='U')
BEGIN
    CREATE TABLE __EFMigrationsHistory (
        MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY,
        ProductVersion NVARCHAR(32) NOT NULL
    );
    PRINT '  ✓ __EFMigrationsHistory creata';
END
ELSE
BEGIN
    PRINT '  ℹ️  __EFMigrationsHistory già esistente';
END

PRINT '';

-- ================================================
-- 13. RIEPILOGO FINALE
-- ================================================
PRINT '';
PRINT '================================================';
PRINT '✅ DATABASE RICREATO CON SUCCESSO!';
PRINT '================================================';
PRINT '';
PRINT '📊 Tabelle create:';
PRINT '   IDENTITY:';
PRINT '     • AspNetUsers (con TenantId)';
PRINT '     • AspNetRoles';
PRINT '     • AspNetUserClaims';
PRINT '     • AspNetUserLogins';
PRINT '     • AspNetUserTokens';
PRINT '     • AspNetRoleClaims';
PRINT '     • AspNetUserRoles';
PRINT '';
PRINT '   MULTI-TENANT:';
PRINT '     • Tenants';
PRINT '';
PRINT '   DOCUMENTI:';
PRINT '     • Documents (con vettori duali 768/1536)';
PRINT '     • DocumentTags';
PRINT '     • DocumentChunks (per RAG)';
PRINT '     • DocumentShares';
PRINT '     • SimilarDocuments (similarità vettoriale)';
PRINT '';
PRINT '   AI CONFIGURATION:';
PRINT '     • AIConfigurations (multi-provider)';
PRINT '     • AgentTemplates';
PRINT '     • AgentConfigurations';
PRINT '     • AgentUsageLogs';
PRINT '';
PRINT '   RAG:';
PRINT '     • Conversations';
PRINT '     • Messages';
PRINT '';
PRINT '   LOGGING & AUDIT:';
PRINT '     • LogEntries (✅ FIX V6: ApplicationDbContext)';
PRINT '     • AuditLogs (Enhanced GDPR/SOC2)';
PRINT '';
PRINT '⚙️  Stored Procedures:';
PRINT '     • sp_CleanupOldLogEntries';
PRINT '     • sp_CleanupOldAuditLogs';
PRINT '';
PRINT '================================================';
PRINT '🚀 PROSSIMI PASSI:';
PRINT '================================================';
PRINT '1. Avviare l''applicazione DocN';
PRINT '2. Le migrazioni EF Core verranno applicate automaticamente';
PRINT '3. Creare il primo utente via /register';
PRINT '4. Configurare AI provider in /aiconfig';
PRINT '5. Iniziare a caricare documenti in /upload';
PRINT '';
PRINT '📝 Note importanti:';
PRINT '   • LogEntries e AuditLogs sono ora entrambe in ApplicationDbContext';
PRINT '   • LogService usa ApplicationDbContext (fix applicato)';
PRINT '   • Migrazioni automatiche funzioneranno correttamente';
PRINT '   • I log dell''upload saranno visibili nel pulsante "📋 View Upload Logs"';
PRINT '';
PRINT '================================================';
PRINT '📖 Documentazione:';
PRINT '   • Database/README.md';
PRINT '   • FIX_LOG_ENTRIES_ISSUE.md';
PRINT '================================================';
PRINT '';

GO
