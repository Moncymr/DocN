-- ================================================
-- DocN Database - Complete Creation Script
-- Database: DocNDb
-- SQL Server 2025 con supporto VECTOR
-- Versione: 2.0 - Dicembre 2024
-- ================================================

USE master;
GO

-- ================================================
-- 1. CREAZIONE DATABASE
-- ================================================

-- Crea database se non esiste
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DocNDb')
BEGIN
    CREATE DATABASE DocNDb;
    PRINT '✅ Database DocNDb creato con successo.';
END
ELSE
BEGIN
    PRINT 'ℹ️  Database DocNDb già esistente.';
END
GO

USE DocNDb;
GO

PRINT '';
PRINT '================================================';
PRINT '🚀 Inizio setup database DocNDb';
PRINT '================================================';
PRINT '';

-- ================================================
-- 2. TABELLE IDENTITY (Autenticazione)
-- ================================================

PRINT '👤 Creazione tabelle ASP.NET Core Identity...';

-- AspNetRoles
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetRoles' and xtype='U')
BEGIN
    CREATE TABLE AspNetRoles (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        Name NVARCHAR(256) NULL,
        NormalizedName NVARCHAR(256) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL
    );
    
    CREATE UNIQUE INDEX RoleNameIndex ON AspNetRoles(NormalizedName) 
    WHERE NormalizedName IS NOT NULL;
    
    PRINT '  ✓ AspNetRoles creata';
END
GO

-- AspNetUsers
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetUsers' and xtype='U')
BEGIN
    CREATE TABLE AspNetUsers (
        Id NVARCHAR(450) NOT NULL PRIMARY KEY,
        UserName NVARCHAR(256) NULL,
        NormalizedUserName NVARCHAR(256) NULL,
        Email NVARCHAR(256) NULL,
        NormalizedEmail NVARCHAR(256) NULL,
        EmailConfirmed BIT NOT NULL DEFAULT 0,
        PasswordHash NVARCHAR(MAX) NULL,
        SecurityStamp NVARCHAR(MAX) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL,
        PhoneNumber NVARCHAR(MAX) NULL,
        PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,
        TwoFactorEnabled BIT NOT NULL DEFAULT 0,
        LockoutEnd DATETIMEOFFSET(7) NULL,
        LockoutEnabled BIT NOT NULL DEFAULT 0,
        AccessFailedCount INT NOT NULL DEFAULT 0,
        
        -- Campi personalizzati
        FirstName NVARCHAR(100) NULL,
        LastName NVARCHAR(100) NULL,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        LastLoginAt DATETIME2(7) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        ProfilePictureUrl NVARCHAR(500) NULL,
        Department NVARCHAR(100) NULL,
        JobTitle NVARCHAR(100) NULL
    );
    
    CREATE UNIQUE INDEX UserNameIndex ON AspNetUsers(NormalizedUserName) 
    WHERE NormalizedUserName IS NOT NULL;
    CREATE INDEX EmailIndex ON AspNetUsers(NormalizedEmail);
    CREATE INDEX IX_AspNetUsers_Department ON AspNetUsers(Department);
    
    PRINT '  ✓ AspNetUsers creata';
END
GO

-- AspNetUserClaims
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetUserClaims' and xtype='U')
BEGIN
    CREATE TABLE AspNetUserClaims (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId NVARCHAR(450) NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetUserClaims_AspNetUsers FOREIGN KEY (UserId) 
            REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_AspNetUserClaims_UserId ON AspNetUserClaims(UserId);
    PRINT '  ✓ AspNetUserClaims creata';
END
GO

-- AspNetUserLogins
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetUserLogins' and xtype='U')
BEGIN
    CREATE TABLE AspNetUserLogins (
        LoginProvider NVARCHAR(450) NOT NULL,
        ProviderKey NVARCHAR(450) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_AspNetUsers FOREIGN KEY (UserId) 
            REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_AspNetUserLogins_UserId ON AspNetUserLogins(UserId);
    PRINT '  ✓ AspNetUserLogins creata';
END
GO

-- AspNetUserRoles
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetUserRoles' and xtype='U')
BEGIN
    CREATE TABLE AspNetUserRoles (
        UserId NVARCHAR(450) NOT NULL,
        RoleId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles FOREIGN KEY (RoleId) 
            REFERENCES AspNetRoles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers FOREIGN KEY (UserId) 
            REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_AspNetUserRoles_RoleId ON AspNetUserRoles(RoleId);
    PRINT '  ✓ AspNetUserRoles creata';
END
GO

-- AspNetUserTokens
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetUserTokens' and xtype='U')
BEGIN
    CREATE TABLE AspNetUserTokens (
        UserId NVARCHAR(450) NOT NULL,
        LoginProvider NVARCHAR(450) NOT NULL,
        Name NVARCHAR(450) NOT NULL,
        Value NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_AspNetUsers FOREIGN KEY (UserId) 
            REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
    
    PRINT '  ✓ AspNetUserTokens creata';
END
GO

-- AspNetRoleClaims
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AspNetRoleClaims' and xtype='U')
BEGIN
    CREATE TABLE AspNetRoleClaims (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RoleId NVARCHAR(450) NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetRoleClaims_AspNetRoles FOREIGN KEY (RoleId) 
            REFERENCES AspNetRoles(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_AspNetRoleClaims_RoleId ON AspNetRoleClaims(RoleId);
    PRINT '  ✓ AspNetRoleClaims creata';
END
GO

PRINT '';
PRINT '✅ Tabelle Identity completate';
PRINT '';

-- ================================================
-- 3. TABELLE DOCUMENTI
-- ================================================

PRINT '📄 Creazione tabelle documenti...';

-- Documents
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Documents' and xtype='U')
BEGIN
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
        Visibility INT NOT NULL DEFAULT 0,  -- 0=Private, 1=Shared, 2=Organization, 3=Public
        
        -- Vector embedding (temporaneamente nvarchar(max), poi VECTOR(1536))
        -- Memorizzato come CSV: "0.1,0.2,0.3,..."
        EmbeddingVector NVARCHAR(MAX) NULL,
        
        -- Metadata
        UploadedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        LastAccessedAt DATETIME2(7) NULL,
        AccessCount INT NOT NULL DEFAULT 0,
        OwnerId NVARCHAR(450) NULL,  -- Nullable per documenti pubblici
        
        -- Nuovi campi per file processing avanzato
        PageCount INT NULL,
        DetectedLanguage NVARCHAR(10) NULL,
        ProcessingStatus NVARCHAR(50) NULL,  -- 'Pending', 'Processing', 'Completed', 'Failed'
        ProcessingError NVARCHAR(MAX) NULL,
        
        CONSTRAINT FK_Documents_Owner FOREIGN KEY (OwnerId) 
            REFERENCES AspNetUsers(Id) ON DELETE SET NULL
    );
    
    -- Indici per performance
    CREATE INDEX IX_Documents_OwnerId ON Documents(OwnerId);
    CREATE INDEX IX_Documents_UploadedAt ON Documents(UploadedAt DESC);
    CREATE INDEX IX_Documents_Visibility ON Documents(Visibility);
    CREATE INDEX IX_Documents_Category ON Documents(ActualCategory);
    CREATE INDEX IX_Documents_Status ON Documents(ProcessingStatus);
    
    -- Full-text index per ricerca
    CREATE FULLTEXT CATALOG DocumentFullTextCatalog AS DEFAULT;
    CREATE FULLTEXT INDEX ON Documents(ExtractedText, FileName)
        KEY INDEX PK__Document__3214EC074A8DB6ED ON DocumentFullTextCatalog;
    
    PRINT '  ✓ Documents creata con full-text search';
END
GO

-- DocumentShares
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DocumentShares' and xtype='U')
BEGIN
    CREATE TABLE DocumentShares (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DocumentId INT NOT NULL,
        SharedWithUserId NVARCHAR(450) NOT NULL,
        Permission INT NOT NULL DEFAULT 0,  -- 0=Read, 1=Write, 2=Delete
        SharedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        SharedByUserId NVARCHAR(450) NULL,
        ExpiresAt DATETIME2(7) NULL,
        
        CONSTRAINT FK_DocumentShares_Document FOREIGN KEY (DocumentId) 
            REFERENCES Documents(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DocumentShares_SharedWith FOREIGN KEY (SharedWithUserId) 
            REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
        CONSTRAINT FK_DocumentShares_SharedBy FOREIGN KEY (SharedByUserId) 
            REFERENCES AspNetUsers(Id)
    );
    
    CREATE INDEX IX_DocumentShares_DocumentId ON DocumentShares(DocumentId);
    CREATE INDEX IX_DocumentShares_SharedWithUserId ON DocumentShares(SharedWithUserId);
    CREATE INDEX IX_DocumentShares_ExpiresAt ON DocumentShares(ExpiresAt);
    
    PRINT '  ✓ DocumentShares creata';
END
GO

-- DocumentTags
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DocumentTags' and xtype='U')
BEGIN
    CREATE TABLE DocumentTags (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL,
        DocumentId INT NOT NULL,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT FK_DocumentTags_Document FOREIGN KEY (DocumentId) 
            REFERENCES Documents(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_DocumentTags_DocumentId ON DocumentTags(DocumentId);
    CREATE INDEX IX_DocumentTags_Name ON DocumentTags(Name);
    
    PRINT '  ✓ DocumentTags creata';
END
GO

PRINT '';
PRINT '✅ Tabelle documenti completate';
PRINT '';

-- ================================================
-- 4. TABELLE CONVERSAZIONI (RAG Chat)
-- ================================================

PRINT '💬 Creazione tabelle conversazioni...';

-- Conversations
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Conversations' and xtype='U')
BEGIN
    CREATE TABLE Conversations (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId NVARCHAR(450) NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        LastMessageAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        IsArchived BIT NOT NULL DEFAULT 0,
        IsStarred BIT NOT NULL DEFAULT 0,
        Tags NVARCHAR(500) NULL,
        
        CONSTRAINT FK_Conversations_User FOREIGN KEY (UserId) 
            REFERENCES AspNetUsers(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_Conversations_UserId ON Conversations(UserId);
    CREATE INDEX IX_Conversations_LastMessageAt ON Conversations(LastMessageAt DESC);
    CREATE INDEX IX_Conversations_UserArchived ON Conversations(UserId, IsArchived, LastMessageAt DESC);
    
    PRINT '  ✓ Conversations creata';
END
GO

-- Messages
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Messages' and xtype='U')
BEGIN
    CREATE TABLE Messages (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConversationId INT NOT NULL,
        Role NVARCHAR(20) NOT NULL,  -- 'user' o 'assistant'
        Content NVARCHAR(MAX) NOT NULL,
        ReferencedDocumentIds NVARCHAR(MAX) NULL,  -- JSON array: [1,2,3]
        Timestamp DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        IsError BIT NOT NULL DEFAULT 0,
        Metadata NVARCHAR(MAX) NULL,  -- JSON per token count, modello, ecc.
        UserRating INT NULL,  -- 1-5 stelle
        UserFeedback NVARCHAR(MAX) NULL,
        
        CONSTRAINT FK_Messages_Conversation FOREIGN KEY (ConversationId) 
            REFERENCES Conversations(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_Messages_ConversationId ON Messages(ConversationId);
    CREATE INDEX IX_Messages_Timestamp ON Messages(Timestamp);
    CREATE INDEX IX_Messages_Rating ON Messages(UserRating);
    
    PRINT '  ✓ Messages creata';
END
GO

PRINT '';
PRINT '✅ Tabelle conversazioni completate';
PRINT '';

-- ================================================
-- 5. TABELLE CONFIGURAZIONE
-- ================================================

PRINT '⚙️  Creazione tabelle configurazione...';

-- AIConfigurations
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AIConfigurations' and xtype='U')
BEGIN
    CREATE TABLE AIConfigurations (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConfigurationName NVARCHAR(100) NOT NULL,
        
        -- Azure OpenAI
        AzureOpenAIEndpoint NVARCHAR(500) NULL,
        AzureOpenAIKey NVARCHAR(500) NULL,
        EmbeddingDeploymentName NVARCHAR(100) NULL,
        ChatDeploymentName NVARCHAR(100) NULL,
        
        -- Parametri RAG
        MaxDocumentsToRetrieve INT NOT NULL DEFAULT 5,
        SimilarityThreshold FLOAT NOT NULL DEFAULT 0.7,
        MaxTokensForContext INT NOT NULL DEFAULT 8000,
        SystemPrompt NVARCHAR(MAX) NULL,
        
        -- Embedding settings
        EmbeddingDimensions INT NOT NULL DEFAULT 1536,
        EmbeddingModel NVARCHAR(100) NULL,
        
        -- Status
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2(7) NULL
    );
    
    CREATE INDEX IX_AIConfigurations_IsActive ON AIConfigurations(IsActive);
    
    PRINT '  ✓ AIConfigurations creata';
END
GO

PRINT '';
PRINT '✅ Tabelle configurazione completate';
PRINT '';

-- ================================================
-- 6. TABELLE AUDIT E ANALYTICS
-- ================================================

PRINT '📊 Creazione tabelle audit...';

-- AuditLogs
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLogs' and xtype='U')
BEGIN
    CREATE TABLE AuditLogs (
        Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId NVARCHAR(450) NULL,
        Action NVARCHAR(50) NOT NULL,  -- 'VIEW', 'DOWNLOAD', 'SEARCH', 'UPLOAD', 'DELETE'
        EntityType NVARCHAR(50) NULL,  -- 'Document', 'Conversation'
        EntityId INT NULL,
        Details NVARCHAR(MAX) NULL,  -- JSON con dettagli aggiuntivi
        IpAddress NVARCHAR(45) NULL,
        UserAgent NVARCHAR(500) NULL,
        Timestamp DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT FK_AuditLogs_User FOREIGN KEY (UserId) 
            REFERENCES AspNetUsers(Id) ON DELETE SET NULL
    );
    
    CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
    CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs(Timestamp DESC);
    CREATE INDEX IX_AuditLogs_Action ON AuditLogs(Action);
    CREATE INDEX IX_AuditLogs_Entity ON AuditLogs(EntityType, EntityId);
    
    -- Columnstore index per analytics veloce su grandi volumi
    CREATE COLUMNSTORE INDEX IX_AuditLogs_Columnstore 
        ON AuditLogs(Timestamp, UserId, Action, EntityType);
    
    PRINT '  ✓ AuditLogs creata con columnstore index';
END
GO

PRINT '';
PRINT '✅ Tabelle audit completate';
PRINT '';

-- ================================================
-- 7. DATI INIZIALI
-- ================================================

PRINT '🌱 Inserimento dati iniziali...';

-- Configurazione AI predefinita
IF NOT EXISTS (SELECT * FROM AIConfigurations WHERE ConfigurationName = 'Default Azure OpenAI')
BEGIN
    INSERT INTO AIConfigurations (
        ConfigurationName,
        MaxDocumentsToRetrieve,
        SimilarityThreshold,
        MaxTokensForContext,
        EmbeddingDimensions,
        EmbeddingModel,
        SystemPrompt,
        IsActive
    )
    VALUES (
        'Default Azure OpenAI',
        5,
        0.7,
        8000,
        1536,
        'text-embedding-ada-002',
        'Sei un assistente AI aziendale esperto. Rispondi alle domande basandoti sui documenti forniti. Cita sempre le fonti usando [DOCUMENTO N].',
        1
    );
    
    PRINT '  ✓ Configurazione AI predefinita inserita';
END
GO

-- Ruoli predefiniti
IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Admin')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Admin', 'ADMIN', NEWID());
    PRINT '  ✓ Ruolo Admin creato';
END

IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'User')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'User', 'USER', NEWID());
    PRINT '  ✓ Ruolo User creato';
END

IF NOT EXISTS (SELECT * FROM AspNetRoles WHERE Name = 'Manager')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'Manager', 'MANAGER', NEWID());
    PRINT '  ✓ Ruolo Manager creato';
END
GO

PRINT '';
PRINT '✅ Dati iniziali inseriti';
PRINT '';

-- ================================================
-- 8. VIEWS E STATISTICHE
-- ================================================

PRINT '👁️  Creazione views...';

-- View per statistiche documenti
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_DocumentStatistics')
BEGIN
    EXEC('
    CREATE VIEW vw_DocumentStatistics AS
    SELECT 
        d.Id,
        d.FileName,
        d.ActualCategory,
        d.UploadedAt,
        d.AccessCount,
        u.Email AS OwnerEmail,
        u.FirstName + '' '' + u.LastName AS OwnerName,
        (SELECT COUNT(*) FROM DocumentShares WHERE DocumentId = d.Id) AS ShareCount,
        (SELECT COUNT(*) FROM DocumentTags WHERE DocumentId = d.Id) AS TagCount
    FROM Documents d
    LEFT JOIN AspNetUsers u ON d.OwnerId = u.Id
    ');
    
    PRINT '  ✓ View vw_DocumentStatistics creata';
END
GO

-- View per attività utenti
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_UserActivity')
BEGIN
    EXEC('
    CREATE VIEW vw_UserActivity AS
    SELECT 
        u.Id AS UserId,
        u.Email,
        u.FirstName + '' '' + u.LastName AS FullName,
        u.Department,
        (SELECT COUNT(*) FROM Documents WHERE OwnerId = u.Id) AS DocumentsOwned,
        (SELECT COUNT(*) FROM Conversations WHERE UserId = u.Id) AS ConversationsCount,
        (SELECT COUNT(*) FROM AuditLogs WHERE UserId = u.Id AND Action = ''SEARCH'') AS SearchCount,
        u.LastLoginAt
    FROM AspNetUsers u
    WHERE u.IsActive = 1
    ');
    
    PRINT '  ✓ View vw_UserActivity creata';
END
GO

PRINT '';
PRINT '✅ Views create';
PRINT '';

-- ================================================
-- 9. STORED PROCEDURES
-- ================================================

PRINT '📦 Creazione stored procedures...';

-- Stored procedure per cleanup vecchi audit logs
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CleanupOldAuditLogs')
    DROP PROCEDURE sp_CleanupOldAuditLogs;
GO

CREATE PROCEDURE sp_CleanupOldAuditLogs
    @RetentionDays INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2(7) = DATEADD(DAY, -@RetentionDays, GETUTCDATE());
    DECLARE @DeletedCount INT;
    
    DELETE FROM AuditLogs WHERE Timestamp < @CutoffDate;
    SET @DeletedCount = @@ROWCOUNT;
    
    PRINT 'Eliminati ' + CAST(@DeletedCount AS NVARCHAR(20)) + ' audit logs più vecchi di ' + CAST(@RetentionDays AS NVARCHAR(10)) + ' giorni.';
END
GO

PRINT '  ✓ Stored procedure sp_CleanupOldAuditLogs creata';

-- Stored procedure per statistiche dashboard
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetDashboardStatistics')
    DROP PROCEDURE sp_GetDashboardStatistics;
GO

CREATE PROCEDURE sp_GetDashboardStatistics
    @UserId NVARCHAR(450) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Statistiche generali o per utente specifico
    SELECT 
        (SELECT COUNT(*) FROM Documents WHERE @UserId IS NULL OR OwnerId = @UserId) AS TotalDocuments,
        (SELECT COUNT(*) FROM Conversations WHERE @UserId IS NULL OR UserId = @UserId) AS TotalConversations,
        (SELECT COUNT(*) FROM AspNetUsers WHERE IsActive = 1) AS ActiveUsers,
        (SELECT COUNT(*) FROM AuditLogs WHERE CAST(Timestamp AS DATE) = CAST(GETUTCDATE() AS DATE)) AS TodayActions,
        (SELECT TOP 1 ActualCategory FROM Documents WHERE @UserId IS NULL OR OwnerId = @UserId GROUP BY ActualCategory ORDER BY COUNT(*) DESC) AS TopCategory,
        (SELECT AVG(UserRating) FROM Messages WHERE UserRating IS NOT NULL) AS AverageUserRating;
END
GO

PRINT '  ✓ Stored procedure sp_GetDashboardStatistics creata';

PRINT '';
PRINT '✅ Stored procedures create';
PRINT '';

-- ================================================
-- 10. RIEPILOGO FINALE
-- ================================================

PRINT '';
PRINT '================================================';
PRINT '🎉 DATABASE DOCN COMPLETATO CON SUCCESSO!';
PRINT '================================================';
PRINT '';
PRINT '📋 RIEPILOGO TABELLE CREATE:';
PRINT '  • 6 tabelle Identity (autenticazione)';
PRINT '  • 3 tabelle documenti (Documents, Shares, Tags)';
PRINT '  • 2 tabelle conversazioni (Conversations, Messages)';
PRINT '  • 1 tabella configurazione (AIConfigurations)';
PRINT '  • 1 tabella audit (AuditLogs)';
PRINT '';
PRINT '📊 FEATURES:';
PRINT '  ✓ Autenticazione completa con Identity';
PRINT '  ✓ Gestione documenti con embedding vettoriali';
PRINT '  ✓ Sistema conversazionale con memoria';
PRINT '  ✓ Full-text search sui documenti';
PRINT '  ✓ Audit logging completo';
PRINT '  ✓ Views per analytics';
PRINT '  ✓ Stored procedures per manutenzione';
PRINT '  ✓ Indici ottimizzati per performance';
PRINT '';
PRINT '🔜 PROSSIMI PASSI:';
PRINT '  1. Configurare connection string in appsettings.json:';
PRINT '     "Server=.;Database=DocNDb;Trusted_Connection=True;"';
PRINT '';
PRINT '  2. Creare directory upload:';
PRINT '     mkdir C:\DocNData\Uploads';
PRINT '';
PRINT '  3. Configurare Azure OpenAI in appsettings.json';
PRINT '';
PRINT '  4. Avviare applicazione:';
PRINT '     dotnet run --project DocN.Client';
PRINT '';
PRINT '  5. Registrare primo utente: http://localhost:5000/register';
PRINT '';
PRINT '================================================';
PRINT '📖 Documentazione: GUIDA_INSTALLAZIONE.md';
PRINT '================================================';
PRINT '';

GO
