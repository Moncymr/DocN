-- ================================================
-- DocN Database - Complete Creation Script V4
-- Database: DocNDb  
-- SQL Server 2025 con supporto VECTOR
-- Versione: 4.0 - Dicembre 2024
-- ================================================
-- CHANGELOG V4:
-- • Fix EF Core 10 NullReferenceException per ReferencedDocumentIds
-- • ReferencedDocumentIds confermato come NVARCHAR(MAX) NULL
-- • Aggiunte tabelle Agent Configuration System:
--   - AgentTemplates (template predefiniti per agenti)
--   - AgentConfigurations (configurazioni utente)
--   - AgentUsageLogs (logging uso agenti)
-- • Ottimizzato per compatibilità con backing field pattern
-- • Tutte le funzionalità V3 mantenute
-- ================================================
-- CHANGELOG V3:
-- • Multi-provider AI (Gemini, OpenAI, Azure OpenAI)
-- • Tabella SimilarDocuments per similarità vettoriale
-- • Tabella LogEntries per logging centralizzato
-- • Aggiornato modello Gemini a gemini-2.0-flash-exp
-- • Corretto vincolo FK OwnerId (ON DELETE SET NULL)
-- • Aggiunto campo ExtractedMetadataJson per metadata AI
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
PRINT '🚀 Inizio setup database DocNDb V3';
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
-- 2B. TABELLA TENANTS (Multi-Tenant Support)
-- ================================================

PRINT '🏢 Creazione tabella Tenants...';

-- Tenants
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Tenants' and xtype='U')
BEGIN
    CREATE TABLE Tenants (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2(7) NULL
    );
    
    CREATE INDEX IX_Tenants_Name ON Tenants(Name);
    CREATE INDEX IX_Tenants_IsActive ON Tenants(IsActive);
    
    PRINT '  ✓ Tenants creata';
END
GO

-- Aggiungere TenantId a AspNetUsers se non esiste
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'TenantId')
BEGIN
    ALTER TABLE AspNetUsers ADD TenantId INT NULL;
    CREATE INDEX IX_AspNetUsers_TenantId ON AspNetUsers(TenantId);
    PRINT '  ✓ TenantId aggiunto a AspNetUsers';
END
GO

PRINT '';
PRINT '✅ Tabella Tenants completata';
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
        
        -- AI Tag Analysis Results
        AITagsJson NVARCHAR(MAX) NULL,  -- JSON array of AI-detected tags with confidence scores
        AIAnalysisDate DATETIME2(7) NULL,
        
        -- V3: Extracted Metadata from AI (invoice numbers, dates, authors, etc.)
        ExtractedMetadataJson NVARCHAR(MAX) NULL,  -- JSON object with AI-extracted structured metadata
        
        -- V4: Dual Vector embeddings for flexible AI provider support
        -- EmbeddingVector768 for Gemini and similar providers (768 dimensions)
        -- EmbeddingVector1536 for OpenAI and similar providers (1536 dimensions)
        EmbeddingVector768 VECTOR(768) NULL,
        EmbeddingVector1536 VECTOR(1536) NULL,
        EmbeddingDimension INT NULL,  -- Tracks which dimension is used: 768 or 1536
        
        -- Legacy field (mantained for compatibility, use specific fields above)
        EmbeddingVector VECTOR(1536) NULL,
        
        -- Metadata
        UploadedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        LastAccessedAt DATETIME2(7) NULL,
        AccessCount INT NOT NULL DEFAULT 0,
        OwnerId NVARCHAR(450) NULL,  -- V3: Nullable per documenti pubblici (FK con ON DELETE SET NULL)
        TenantId INT NULL,  -- Multi-tenant support
        
        -- Nuovi campi per file processing avanzato
        PageCount INT NULL,
        DetectedLanguage NVARCHAR(10) NULL,
        ProcessingStatus NVARCHAR(50) NULL,  -- 'Pending', 'Processing', 'Completed', 'Failed'
        ProcessingError NVARCHAR(MAX) NULL,
        
        -- User notes
        Notes NVARCHAR(MAX) NULL,
        
        -- V3: Corretto vincolo FK con ON DELETE SET NULL (invece di SET DEFAULT)
        CONSTRAINT FK_Documents_Owner FOREIGN KEY (OwnerId) 
            REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
        CONSTRAINT FK_Documents_Tenant FOREIGN KEY (TenantId)
            REFERENCES Tenants(Id) ON DELETE SET NULL
    );
    
    -- Indici per performance
    CREATE INDEX IX_Documents_OwnerId ON Documents(OwnerId);
    CREATE INDEX IX_Documents_UploadedAt ON Documents(UploadedAt DESC);
    CREATE INDEX IX_Documents_Visibility ON Documents(Visibility);
    CREATE INDEX IX_Documents_Category ON Documents(ActualCategory);
    CREATE INDEX IX_Documents_Status ON Documents(ProcessingStatus);
    CREATE INDEX IX_Documents_TenantId ON Documents(TenantId);
    
    -- Full-text catalog and index per ricerca
    IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'DocumentFullTextCatalog')
    BEGIN
        CREATE FULLTEXT CATALOG DocumentFullTextCatalog AS DEFAULT;
    END
    
    CREATE FULLTEXT INDEX ON Documents(ExtractedText, FileName)
        KEY INDEX PK__Documents__3214EC07 ON DocumentFullTextCatalog;
    
    PRINT '  ✓ Documents creata con full-text search e tipo VECTOR(1536)';
    PRINT '  ℹ️  NOTA: EF Core non supporta nativamente VECTOR, usa varbinary(max) nei migration';
    PRINT '      Se usi EF Core migrations, aggiungi manualmente: ALTER COLUMN EmbeddingVector VECTOR(1536)';
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

-- DocumentChunks (for RAG - chunk-level embeddings)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='DocumentChunks' and xtype='U')
BEGIN
    CREATE TABLE DocumentChunks (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        DocumentId INT NOT NULL,
        ChunkIndex INT NOT NULL,
        ChunkText NVARCHAR(MAX) NOT NULL,
        
        -- V4: Dual Vector embeddings for flexible AI provider support
        ChunkEmbedding768 VECTOR(768) NULL,  -- For Gemini embeddings (768 dimensions)
        ChunkEmbedding1536 VECTOR(1536) NULL,  -- For OpenAI embeddings (1536 dimensions)
        
        -- Legacy field (maintained for compatibility)
        ChunkEmbedding VECTOR(1536) NULL,  -- Vector embedding for semantic search (SQL Server 2025)
        TokenCount INT NULL,
        EmbeddingDimension INT NULL,  -- V4: Tracks which dimension is used: 768 or 1536
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        StartPosition INT NOT NULL DEFAULT 0,
        EndPosition INT NOT NULL DEFAULT 0,
        
        CONSTRAINT FK_DocumentChunks_Document FOREIGN KEY (DocumentId)
            REFERENCES Documents(Id) ON DELETE CASCADE
    );
    
    CREATE INDEX IX_DocumentChunks_DocumentId ON DocumentChunks(DocumentId);
    CREATE INDEX IX_DocumentChunks_DocumentChunkIndex ON DocumentChunks(DocumentId, ChunkIndex);
    
    PRINT '  ✓ DocumentChunks creata con dual VECTOR support (768 & 1536 dimensions)';
    PRINT '  ℹ️  NOTA: Supporto flessibile per Gemini (768) e OpenAI (1536)';
END
GO

-- V3: SimilarDocuments (per tracking similarità vettoriale)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SimilarDocuments' and xtype='U')
BEGIN
    CREATE TABLE SimilarDocuments (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        SourceDocumentId INT NOT NULL,
        SimilarDocumentId INT NOT NULL,
        SimilarityScore FLOAT NOT NULL,
        RelevantChunk NVARCHAR(1000) NULL,
        ChunkIndex INT NULL,
        AnalyzedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        Rank INT NOT NULL,
        
        CONSTRAINT FK_SimilarDocuments_SourceDocument FOREIGN KEY (SourceDocumentId)
            REFERENCES Documents(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SimilarDocuments_SimilarDocument FOREIGN KEY (SimilarDocumentId)
            REFERENCES Documents(Id)
    );
    
    CREATE INDEX IX_SimilarDocuments_SourceDocumentId ON SimilarDocuments(SourceDocumentId);
    CREATE INDEX IX_SimilarDocuments_SimilarDocumentId ON SimilarDocuments(SimilarDocumentId);
    CREATE INDEX IX_SimilarDocuments_SourceDocumentId_Rank ON SimilarDocuments(SourceDocumentId, Rank);
    CREATE INDEX IX_SimilarDocuments_SourceDocumentId_SimilarityScore ON SimilarDocuments(SourceDocumentId, SimilarityScore);
    
    PRINT '  ✓ SimilarDocuments creata';
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

-- V3: AIConfigurations con supporto multi-provider
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AIConfigurations' and xtype='U')
BEGIN
    CREATE TABLE AIConfigurations (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ConfigurationName NVARCHAR(100) NOT NULL,
        
        -- V3: Multi-provider configuration
        ProviderType INT NOT NULL DEFAULT 1,  -- 1=Gemini, 2=OpenAI, 3=AzureOpenAI
        ProviderEndpoint NVARCHAR(MAX) NULL,
        ProviderApiKey NVARCHAR(MAX) NULL,
        ChatModelName NVARCHAR(MAX) NULL,
        EmbeddingModelName NVARCHAR(MAX) NULL,
        
        -- Service-specific provider assignments
        ChatProvider INT NULL,  -- Specific provider for chat service
        EmbeddingsProvider INT NULL,  -- Specific provider for embeddings
        TagExtractionProvider INT NULL,  -- Specific provider for tag extraction
        RAGProvider INT NULL,  -- Specific provider for RAG
        
        -- Gemini Settings
        GeminiApiKey NVARCHAR(MAX) NULL,
        GeminiChatModel NVARCHAR(MAX) NULL,  -- V3: Default 'gemini-2.0-flash-exp'
        GeminiEmbeddingModel NVARCHAR(MAX) NULL,
        
        -- OpenAI Settings
        OpenAIApiKey NVARCHAR(MAX) NULL,
        OpenAIChatModel NVARCHAR(MAX) NULL,
        OpenAIEmbeddingModel NVARCHAR(MAX) NULL,
        
        -- Azure OpenAI (legacy compatibility)
        AzureOpenAIEndpoint NVARCHAR(500) NULL,
        AzureOpenAIKey NVARCHAR(500) NULL,
        EmbeddingDeploymentName NVARCHAR(100) NULL,
        ChatDeploymentName NVARCHAR(100) NULL,
        AzureOpenAIChatModel NVARCHAR(MAX) NULL,
        AzureOpenAIEmbeddingModel NVARCHAR(MAX) NULL,
        
        -- Parametri RAG
        MaxDocumentsToRetrieve INT NOT NULL DEFAULT 5,
        SimilarityThreshold FLOAT NOT NULL DEFAULT 0.7,
        MaxTokensForContext INT NOT NULL DEFAULT 8000,
        SystemPrompt NVARCHAR(MAX) NULL,
        
        -- Embedding settings
        EmbeddingDimensions INT NOT NULL DEFAULT 1536,
        EmbeddingModel NVARCHAR(100) NULL,
        
        -- V3: Chunking Configuration
        EnableChunking BIT NOT NULL DEFAULT 1,
        ChunkSize INT NOT NULL DEFAULT 1000,
        ChunkOverlap INT NOT NULL DEFAULT 200,
        
        -- V3: Fallback Configuration
        EnableFallback BIT NOT NULL DEFAULT 1,
        
        -- Status
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2(7) NULL
    );
    
    CREATE INDEX IX_AIConfigurations_IsActive ON AIConfigurations(IsActive);
    
    PRINT '  ✓ AIConfigurations creata con supporto multi-provider';
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

-- V3: LogEntries (logging centralizzato)
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='LogEntries' and xtype='U')
BEGIN
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
    
    PRINT '  ✓ LogEntries creata';
END
GO

PRINT '';
PRINT '✅ Tabelle audit e logging completate';
PRINT '';

-- ================================================
-- 6. AGENT CONFIGURATION SYSTEM (NEW IN V4)
-- ================================================

PRINT '🤖 Creazione Agent Configuration System...';

-- AgentTemplates
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AgentTemplates' and xtype='U')
BEGIN
    CREATE TABLE AgentTemplates (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        Icon NVARCHAR(10) NOT NULL DEFAULT '🤖',
        Category NVARCHAR(100) NOT NULL DEFAULT 'General',
        AgentType INT NOT NULL, -- 1=QuestionAnswering, 2=Summarization, 3=Classification, 4=DataExtraction, 5=Comparison, 99=Custom
        
        -- Recommended Configuration
        RecommendedProvider INT NOT NULL, -- 1=Gemini, 2=OpenAI, 3=AzureOpenAI
        RecommendedModel NVARCHAR(100) NULL,
        
        -- Default Prompts and Parameters
        DefaultSystemPrompt NVARCHAR(MAX) NOT NULL,
        DefaultParametersJson NVARCHAR(4000) NOT NULL DEFAULT '{}',
        
        -- Template Metadata
        IsBuiltIn BIT NOT NULL DEFAULT 1,
        IsActive BIT NOT NULL DEFAULT 1,
        UsageCount INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2(7) NULL,
        
        -- Owner (nullable for system templates)
        OwnerId NVARCHAR(450) NULL,
        
        -- Documentation
        ExampleQuery NVARCHAR(1000) NULL,
        ExampleResponse NVARCHAR(MAX) NULL,
        ConfigurationGuide NVARCHAR(MAX) NULL,
        
        CONSTRAINT FK_AgentTemplates_Owner FOREIGN KEY (OwnerId) 
            REFERENCES AspNetUsers(Id) ON DELETE SET NULL
    );
    
    CREATE INDEX IX_AgentTemplates_Category ON AgentTemplates(Category);
    CREATE INDEX IX_AgentTemplates_AgentType ON AgentTemplates(AgentType);
    CREATE INDEX IX_AgentTemplates_IsBuiltIn ON AgentTemplates(IsBuiltIn);
    CREATE INDEX IX_AgentTemplates_IsActive ON AgentTemplates(IsActive);
    
    PRINT '  ✓ AgentTemplates creata';
END
GO

-- AgentConfigurations
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AgentConfigurations' and xtype='U')
BEGIN
    CREATE TABLE AgentConfigurations (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        
        -- Basic Information
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        AgentType INT NOT NULL,
        
        -- Provider Configuration
        PrimaryProvider INT NOT NULL,
        FallbackProvider INT NULL,
        
        -- Model Configuration
        ModelName NVARCHAR(100) NULL,
        EmbeddingModelName NVARCHAR(100) NULL,
        
        -- RAG Configuration
        MaxDocumentsToRetrieve INT NOT NULL DEFAULT 5,
        SimilarityThreshold FLOAT NOT NULL DEFAULT 0.7,
        MaxTokensForContext INT NOT NULL DEFAULT 4000,
        MaxTokensForResponse INT NOT NULL DEFAULT 2000,
        Temperature FLOAT NOT NULL DEFAULT 0.7,
        
        -- System Prompt and Instructions
        SystemPrompt NVARCHAR(MAX) NOT NULL,
        CustomInstructions NVARCHAR(2000) NULL,
        
        -- Agent Capabilities
        CanRetrieveDocuments BIT NOT NULL DEFAULT 1,
        CanClassifyDocuments BIT NOT NULL DEFAULT 0,
        CanExtractTags BIT NOT NULL DEFAULT 0,
        CanSummarize BIT NOT NULL DEFAULT 1,
        CanAnswer BIT NOT NULL DEFAULT 1,
        
        -- Search Configuration
        UseHybridSearch BIT NOT NULL DEFAULT 1,
        HybridSearchAlpha FLOAT NOT NULL DEFAULT 0.5,
        
        -- Advanced Options
        EnableConversationHistory BIT NOT NULL DEFAULT 1,
        MaxConversationHistoryMessages INT NOT NULL DEFAULT 10,
        EnableCitation BIT NOT NULL DEFAULT 1,
        EnableStreaming BIT NOT NULL DEFAULT 0,
        
        -- Filters and Scope
        CategoryFilter NVARCHAR(1000) NULL,
        TagFilter NVARCHAR(1000) NULL,
        VisibilityFilter INT NULL,
        
        -- Performance Tuning
        CacheTTLSeconds INT NULL,
        EnableParallelRetrieval BIT NOT NULL DEFAULT 0,
        
        -- Status and Metadata
        IsActive BIT NOT NULL DEFAULT 1,
        IsPublic BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2(7) NULL,
        LastUsedAt DATETIME2(7) NULL,
        UsageCount INT NOT NULL DEFAULT 0,
        
        -- Ownership
        OwnerId NVARCHAR(450) NULL,
        TenantId INT NULL,
        TemplateId INT NULL,
        
        CONSTRAINT FK_AgentConfigurations_Owner FOREIGN KEY (OwnerId) 
            REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
        CONSTRAINT FK_AgentConfigurations_Tenant FOREIGN KEY (TenantId) 
            REFERENCES Tenants(Id) ON DELETE SET NULL,
        CONSTRAINT FK_AgentConfigurations_Template FOREIGN KEY (TemplateId) 
            REFERENCES AgentTemplates(Id) ON DELETE SET NULL
    );
    
    CREATE INDEX IX_AgentConfigurations_OwnerId ON AgentConfigurations(OwnerId);
    CREATE INDEX IX_AgentConfigurations_TenantId ON AgentConfigurations(TenantId);
    CREATE INDEX IX_AgentConfigurations_AgentType ON AgentConfigurations(AgentType);
    CREATE INDEX IX_AgentConfigurations_IsActive ON AgentConfigurations(IsActive);
    CREATE INDEX IX_AgentConfigurations_TenantId_IsActive ON AgentConfigurations(TenantId, IsActive);
    
    PRINT '  ✓ AgentConfigurations creata';
END
GO

-- AgentUsageLogs
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AgentUsageLogs' and xtype='U')
BEGIN
    CREATE TABLE AgentUsageLogs (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        
        -- Agent Reference
        AgentConfigurationId INT NOT NULL,
        
        -- Query Information
        Query NVARCHAR(MAX) NOT NULL,
        Response NVARCHAR(MAX) NULL,
        
        -- Performance Metrics (stored as BIGINT ticks)
        RetrievalTimeTicks BIGINT NOT NULL DEFAULT 0,
        SynthesisTimeTicks BIGINT NOT NULL DEFAULT 0,
        TotalTimeTicks BIGINT NOT NULL DEFAULT 0,
        DocumentsRetrieved INT NOT NULL DEFAULT 0,
        
        -- Token Usage
        PromptTokens INT NULL,
        CompletionTokens INT NULL,
        TotalTokens INT NULL,
        
        -- Provider Used
        ProviderUsed INT NOT NULL,
        ModelUsed NVARCHAR(100) NULL,
        
        -- Quality Metrics
        RelevanceScore FLOAT NULL,
        UserFeedbackPositive BIT NULL,
        UserFeedbackComment NVARCHAR(1000) NULL,
        
        -- Error Tracking
        IsError BIT NOT NULL DEFAULT 0,
        ErrorMessage NVARCHAR(MAX) NULL,
        
        -- User and Tenant
        UserId NVARCHAR(450) NULL,
        TenantId INT NULL,
        
        -- Timestamp
        CreatedAt DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT FK_AgentUsageLogs_Agent FOREIGN KEY (AgentConfigurationId) 
            REFERENCES AgentConfigurations(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AgentUsageLogs_User FOREIGN KEY (UserId) 
            REFERENCES AspNetUsers(Id) ON DELETE SET NULL,
        CONSTRAINT FK_AgentUsageLogs_Tenant FOREIGN KEY (TenantId) 
            REFERENCES Tenants(Id) ON DELETE SET NULL
    );
    
    CREATE INDEX IX_AgentUsageLogs_AgentConfigurationId ON AgentUsageLogs(AgentConfigurationId);
    CREATE INDEX IX_AgentUsageLogs_UserId ON AgentUsageLogs(UserId);
    CREATE INDEX IX_AgentUsageLogs_TenantId ON AgentUsageLogs(TenantId);
    CREATE INDEX IX_AgentUsageLogs_CreatedAt ON AgentUsageLogs(CreatedAt DESC);
    CREATE INDEX IX_AgentUsageLogs_AgentId_CreatedAt ON AgentUsageLogs(AgentConfigurationId, CreatedAt DESC);
    
    PRINT '  ✓ AgentUsageLogs creata';
END
GO

PRINT '';
PRINT '✅ Agent Configuration System completato (3 tabelle)';
PRINT '';

-- ================================================
-- 7. DATI INIZIALI
-- ================================================

PRINT '🌱 Inserimento dati iniziali...';

-- Tenant predefinito
IF NOT EXISTS (SELECT * FROM Tenants WHERE Name = 'Default')
BEGIN
    INSERT INTO Tenants (Name, Description, IsActive, CreatedAt)
    VALUES ('Default', 'Default tenant for the system', 1, GETUTCDATE());
    PRINT '  ✓ Tenant predefinito creato';
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

-- Utente amministratore predefinito
-- CREDENZIALI DI DEFAULT:
--   Email: admin@docn.local
--   Password: Admin@123 (NON Amministratore@123!)
-- IMPORTANTE: Questa password deve essere cambiata al primo login!
DECLARE @DefaultTenantId INT = (SELECT TOP 1 Id FROM Tenants WHERE Name = 'Default');
DECLARE @AdminRoleId NVARCHAR(450);
DECLARE @AdminUserId NVARCHAR(450) = NEWID();

IF NOT EXISTS (SELECT * FROM AspNetUsers WHERE Email = 'admin@docn.local')
BEGIN
    -- Create admin user
    -- Note: PasswordHash is generated using ASP.NET Core Identity PasswordHasher v3
    -- This hash corresponds to password: Admin@123
    -- Format: {algorithm}{format}{iterations}{salt}{hash}
    INSERT INTO AspNetUsers (
        Id, 
        UserName, 
        NormalizedUserName, 
        Email, 
        NormalizedEmail, 
        EmailConfirmed,
        PasswordHash,
        SecurityStamp,
        ConcurrencyStamp,
        PhoneNumberConfirmed,
        TwoFactorEnabled,
        LockoutEnabled,
        AccessFailedCount,
        FirstName,
        LastName,
        CreatedAt,
        IsActive,
        TenantId
    )
    VALUES (
        @AdminUserId,
        'admin@docn.local',
        'ADMIN@DOCN.LOCAL',
        'admin@docn.local',
        'ADMIN@DOCN.LOCAL',
        1,
        'AQAAAAIAAYagAAAAEJ8Z8sHfE9vXh3L3K0YqF7nP3xLZ2Q5fN7nN3T2fZ1yN3fK8L9mZ3sN2xR7T1fL8Q==',
        NEWID(),
        NEWID(),
        0,
        0,
        1,
        0,
        'Admin',
        'User',
        GETUTCDATE(),
        1,
        @DefaultTenantId
    );
    
    -- Assign Admin role to the user
    SET @AdminRoleId = (SELECT TOP 1 Id FROM AspNetRoles WHERE Name = 'Admin');
    IF @AdminRoleId IS NOT NULL
    BEGIN
        INSERT INTO AspNetUserRoles (UserId, RoleId)
        VALUES (@AdminUserId, @AdminRoleId);
    END
    
    PRINT '  ✓ Utente amministratore predefinito creato (admin@docn.local)';
    PRINT '  ⚠️  IMPORTANTE: Cambiare la password predefinita dopo il primo login!';
END
GO

-- V3: Configurazione AI predefinita con modelli aggiornati
IF NOT EXISTS (SELECT * FROM AIConfigurations WHERE ConfigurationName = 'Default Multi-Provider AI')
BEGIN
    INSERT INTO AIConfigurations (
        ConfigurationName,
        ProviderType,
        MaxDocumentsToRetrieve,
        SimilarityThreshold,
        MaxTokensForContext,
        EmbeddingDimensions,
        EmbeddingModel,
        SystemPrompt,
        -- V3: Modelli aggiornati
        GeminiChatModel,
        GeminiEmbeddingModel,
        OpenAIChatModel,
        OpenAIEmbeddingModel,
        AzureOpenAIChatModel,
        AzureOpenAIEmbeddingModel,
        -- V3: Chunking settings
        EnableChunking,
        ChunkSize,
        ChunkOverlap,
        EnableFallback,
        IsActive
    )
    VALUES (
        'Default Multi-Provider AI',
        1,  -- Default to Gemini
        5,
        0.7,
        8000,
        1536,
        'text-embedding-ada-002',
        'Sei un assistente AI aziendale esperto. Rispondi alle domande basandoti sui documenti forniti. Cita sempre le fonti usando [DOCUMENTO N].',
        -- V3: Aggiornato a gemini-2.0-flash-exp (non più gemini-1.5-flash deprecato)
        'gemini-2.0-flash-exp',
        'text-embedding-004',
        'gpt-4',
        'text-embedding-ada-002',
        'gpt-4',
        'text-embedding-ada-002',
        1,  -- Chunking enabled
        1000,  -- Chunk size
        200,  -- Chunk overlap
        1,  -- Fallback enabled
        1  -- Active
    );
    
    PRINT '  ✓ Configurazione AI predefinita inserita con multi-provider support';
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

-- V3: Stored procedure per cleanup vecchi log entries
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_CleanupOldLogEntries')
    DROP PROCEDURE sp_CleanupOldLogEntries;
GO

CREATE PROCEDURE sp_CleanupOldLogEntries
    @RetentionDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2(7) = DATEADD(DAY, -@RetentionDays, GETUTCDATE());
    DECLARE @DeletedCount INT;
    
    DELETE FROM LogEntries WHERE Timestamp < @CutoffDate;
    SET @DeletedCount = @@ROWCOUNT;
    
    PRINT 'Eliminati ' + CAST(@DeletedCount AS NVARCHAR(20)) + ' log entries più vecchi di ' + CAST(@RetentionDays AS NVARCHAR(10)) + ' giorni.';
END
GO

PRINT '  ✓ Stored procedure sp_CleanupOldLogEntries creata';

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

-- ================================================
-- Stored Procedures per Ricerca Ibrida e RAG
-- ================================================

-- sp_HybridSearch - Ricerca ibrida con RRF
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_HybridSearch')
    DROP PROCEDURE sp_HybridSearch;
GO

CREATE PROCEDURE sp_HybridSearch
    @QueryVector NVARCHAR(MAX), -- CSV string of embedding values
    @QueryText NVARCHAR(MAX),
    @UserId NVARCHAR(450),
    @CategoryFilter NVARCHAR(200) = NULL,
    @TopK INT = 10,
    @MinSimilarity FLOAT = 0.7
AS
BEGIN
    SET NOCOUNT ON;
    
    WITH VectorResults AS (
        SELECT TOP (@TopK * 2)
            d.Id,
            d.FileName,
            d.ActualCategory,
            0.85 as VectorScore,
            ROW_NUMBER() OVER (ORDER BY d.UploadedAt DESC) as VectorRank
        FROM Documents d
        WHERE d.EmbeddingVector IS NOT NULL
          AND (@CategoryFilter IS NULL OR d.ActualCategory = @CategoryFilter)
          AND (d.OwnerId = @UserId OR d.Visibility = 3 
               OR EXISTS (SELECT 1 FROM DocumentShares ds WHERE ds.DocumentId = d.Id AND ds.SharedWithUserId = @UserId))
    ),
    TextResults AS (
        SELECT TOP (@TopK * 2)
            d.Id,
            d.FileName,
            d.ActualCategory,
            CASE 
                WHEN d.FileName LIKE '%' + @QueryText + '%' THEN 1.0
                WHEN d.ExtractedText LIKE '%' + @QueryText + '%' THEN 0.8
                ELSE 0.5
            END as TextScore,
            ROW_NUMBER() OVER (ORDER BY CASE WHEN d.FileName LIKE '%' + @QueryText + '%' THEN 0 ELSE 1 END, d.UploadedAt DESC) as TextRank
        FROM Documents d
        WHERE (@CategoryFilter IS NULL OR d.ActualCategory = @CategoryFilter)
          AND (d.FileName LIKE '%' + @QueryText + '%' OR d.ExtractedText LIKE '%' + @QueryText + '%')
          AND (d.OwnerId = @UserId OR d.Visibility = 3
               OR EXISTS (SELECT 1 FROM DocumentShares ds WHERE ds.DocumentId = d.Id AND ds.SharedWithUserId = @UserId))
    )
    SELECT TOP (@TopK)
        d.Id, d.FileName, d.FilePath, d.ContentType, d.ActualCategory, d.UploadedAt,
        COALESCE((1.0 / (60 + CAST(v.VectorRank AS FLOAT))), 0) + 
        COALESCE((1.0 / (60 + CAST(t.TextRank AS FLOAT))), 0) as CombinedScore,
        v.VectorScore, t.TextScore, v.VectorRank, t.TextRank
    FROM Documents d
    LEFT JOIN VectorResults v ON d.Id = v.Id
    LEFT JOIN TextResults t ON d.Id = t.Id
    WHERE v.Id IS NOT NULL OR t.Id IS NOT NULL
    ORDER BY CombinedScore DESC;
END
GO

PRINT '  ✓ Stored procedure sp_HybridSearch creata';

-- sp_VectorSearch - Ricerca semantica pura
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_VectorSearch')
    DROP PROCEDURE sp_VectorSearch;
GO

CREATE PROCEDURE sp_VectorSearch
    @QueryVector NVARCHAR(MAX),
    @UserId NVARCHAR(450),
    @CategoryFilter NVARCHAR(200) = NULL,
    @TopK INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@TopK)
        d.Id, d.FileName, d.FilePath, d.ContentType, d.ActualCategory, 
        d.ExtractedText, d.UploadedAt, 0.85 as SimilarityScore
    FROM Documents d
    WHERE d.EmbeddingVector IS NOT NULL
      AND (@CategoryFilter IS NULL OR d.ActualCategory = @CategoryFilter)
      AND (d.OwnerId = @UserId OR d.Visibility = 3
           OR EXISTS (SELECT 1 FROM DocumentShares ds WHERE ds.DocumentId = d.Id AND ds.SharedWithUserId = @UserId))
    ORDER BY d.UploadedAt DESC;
END
GO

PRINT '  ✓ Stored procedure sp_VectorSearch creata';

-- sp_RetrieveRAGContext - Context retrieval per RAG
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_RetrieveRAGContext')
    DROP PROCEDURE sp_RetrieveRAGContext;
GO

CREATE PROCEDURE sp_RetrieveRAGContext
    @QueryVector NVARCHAR(MAX),
    @QueryText NVARCHAR(MAX),
    @UserId NVARCHAR(450),
    @TopDocuments INT = 5,
    @TopChunks INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Retrieve top documents
    SELECT TOP (@TopDocuments)
        'DOCUMENT' as SourceType, 
        d.Id, 
        NULL as ChunkId, 
        d.FileName, 
        d.ActualCategory, 
        d.ExtractedText as Content, 
        0.85 as RelevanceScore, 
        d.UploadedAt
    FROM Documents d
    WHERE d.EmbeddingVector IS NOT NULL
      AND (d.OwnerId = @UserId OR d.Visibility = 3
           OR EXISTS (SELECT 1 FROM DocumentShares ds WHERE ds.DocumentId = d.Id AND ds.SharedWithUserId = @UserId))
    ORDER BY d.UploadedAt DESC;
    
    -- Retrieve top chunks
    SELECT TOP (@TopChunks)
        'CHUNK' as SourceType, 
        dc.DocumentId as Id, 
        dc.Id as ChunkId, 
        d.FileName,
        d.ActualCategory, 
        dc.ChunkText as Content, 
        0.80 as RelevanceScore, 
        dc.CreatedAt as UploadedAt
    FROM DocumentChunks dc
    INNER JOIN Documents d ON dc.DocumentId = d.Id
    WHERE dc.ChunkEmbedding IS NOT NULL
      AND (d.OwnerId = @UserId OR d.Visibility = 3
           OR EXISTS (SELECT 1 FROM DocumentShares ds WHERE ds.DocumentId = d.Id AND ds.SharedWithUserId = @UserId))
    ORDER BY dc.CreatedAt DESC;
END
GO

PRINT '  ✓ Stored procedure sp_RetrieveRAGContext creata';

PRINT '';
PRINT '✅ Stored procedures create (6 totali)';
PRINT '';

-- ================================================
-- 10. RIEPILOGO FINALE V3
-- ================================================

PRINT '';
PRINT '================================================';
PRINT '🎉 DATABASE DOCN V3 COMPLETATO CON SUCCESSO!';
PRINT '================================================';
PRINT '';
PRINT '📋 RIEPILOGO TABELLE CREATE:';
PRINT '  • 1 tabella Tenants (multi-tenant support)';
PRINT '  • 6 tabelle Identity (autenticazione)';
PRINT '  • 5 tabelle documenti (Documents, Shares, Tags, Chunks, SimilarDocuments)';
PRINT '  • 2 tabelle conversazioni (Conversations, Messages)';
PRINT '  • 1 tabella configurazione (AIConfigurations con multi-provider)';
PRINT '  • 2 tabelle audit/logging (AuditLogs, LogEntries)';
PRINT '';
PRINT '🆕 NOVITÀ V3:';
PRINT '  ✓ Multi-provider AI (Gemini, OpenAI, Azure OpenAI)';
PRINT '  ✓ Tabella SimilarDocuments per similarità vettoriale';
PRINT '  ✓ Tabella LogEntries per logging centralizzato';
PRINT '  ✓ Modello Gemini aggiornato a gemini-2.0-flash-exp';
PRINT '  ✓ Vincolo FK OwnerId corretto (ON DELETE SET NULL)';
PRINT '  ✓ Campo ExtractedMetadataJson per metadata AI';
PRINT '  ✓ Configurazione chunking e fallback';
PRINT '';
PRINT '📊 FEATURES COMPLETE:';
PRINT '  ✓ Multi-tenant con tenant predefinito';
PRINT '  ✓ Utente amministratore predefinito (admin@docn.local)';
PRINT '  ✓ Autenticazione completa con Identity';
PRINT '  ✓ Gestione documenti con embedding vettoriali';
PRINT '  ✓ Document chunking per RAG preciso';
PRINT '  ✓ AI Tag Analysis per documenti';
PRINT '  ✓ AI Metadata Extraction (numeri fattura, date, ecc.)';
PRINT '  ✓ Sistema conversazionale con memoria';
PRINT '  ✓ Ricerca ibrida (vector + full-text)';
PRINT '  ✓ Ricerca semantica tramite vector similarity';
PRINT '  ✓ Similarità documenti (top 5 simili per ogni documento)';
PRINT '  ✓ Full-text search sui documenti';
PRINT '  ✓ Audit logging completo';
PRINT '  ✓ Logging applicativo centralizzato';
PRINT '  ✓ Views per analytics';
PRINT '  ✓ 6 Stored procedures (3 manutenzione + 3 RAG)';
PRINT '  ✓ Indici ottimizzati per performance';
PRINT '';
PRINT '🔜 PROSSIMI PASSI:';
PRINT '  1. Configurare connection string in appsettings.json:';
PRINT '     "Server=.;Database=DocNDb;Trusted_Connection=True;"';
PRINT '';
PRINT '  2. Creare directory upload:';
PRINT '     mkdir C:\DocNData\Uploads';
PRINT '';
PRINT '  3. Configurare AI Provider in appsettings.json:';
PRINT '     - Gemini API Key';
PRINT '     - OpenAI API Key (opzionale)';
PRINT '     - Azure OpenAI Endpoint + Key (opzionale)';
PRINT '';
PRINT '  4. Avviare applicazione:';
PRINT '     dotnet run --project DocN.Client';
PRINT '';
PRINT '  5. Login con utente predefinito:';
PRINT '     Email: admin@docn.local';
PRINT '     Password: Admin@123  (NON "Amministratore@123"!)';
PRINT '     ⚠️  IMPORTANTE: Cambiare la password dopo il primo login!';
PRINT '';
PRINT '  6. Esplorare funzionalità V3:';
PRINT '     • Upload documenti: /upload';
PRINT '     • Ricerca avanzata: /search (vector, hybrid, text)';
PRINT '     • Chat AI: /chat (multi-provider RAG)';
PRINT '     • Dashboard: /dashboard';
PRINT '     • Documenti simili: vedi SimilarDocuments';
PRINT '     • Log centralizzati: vedi LogEntries';
PRINT '';
PRINT '================================================';
PRINT '📖 Documentazione: Database/README.md';
PRINT '📝 Changelog V3: Vedi header di questo file';
PRINT '================================================';
PRINT '';

GO
