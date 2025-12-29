-- ============================================================================
-- DocN - Enterprise RAG Database Schema for SQL Server 2025
-- With VECTOR support for semantic search
-- ============================================================================

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'DocN')
BEGIN
    CREATE DATABASE DocN;
END
GO

USE DocN;
GO

-- ============================================================================
-- Table: Categories
-- Description: Hierarchical document categories
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Categories]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Categories] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(100) NOT NULL,
        [Description] NVARCHAR(500) NULL,
        [ParentCategoryId] INT NULL,
        [Color] NVARCHAR(20) NULL,
        [Icon] NVARCHAR(50) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_Categories_ParentCategory] 
            FOREIGN KEY ([ParentCategoryId]) REFERENCES [dbo].[Categories]([Id]),
        CONSTRAINT [UQ_Categories_Name] UNIQUE ([Name])
    );
    
    CREATE INDEX [IX_Categories_ParentCategoryId] ON [dbo].[Categories]([ParentCategoryId]);
END
GO

-- ============================================================================
-- Table: Documents
-- Description: Main documents table with VECTOR embeddings for semantic search
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Documents]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Documents] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Title] NVARCHAR(255) NOT NULL,
        [FileName] NVARCHAR(255) NOT NULL,
        [ContentType] NVARCHAR(100) NOT NULL,
        [FilePath] NVARCHAR(500) NOT NULL,
        [FileSize] BIGINT NOT NULL,
        [FullText] NVARCHAR(MAX) NULL,
        
        -- Vector embedding for semantic search (SQL Server 2025 VECTOR type)
        -- OpenAI text-embedding-ada-002 produces 1536-dimensional vectors
        [Embedding] VECTOR(1536) NULL,
        
        -- Category information
        [CategoryId] INT NULL,
        [SuggestedCategories] NVARCHAR(500) NULL, -- JSON array of suggested categories
        
        -- Metadata
        [Tags] NVARCHAR(1000) NULL, -- Comma-separated or JSON
        [Notes] NVARCHAR(MAX) NULL,
        [Author] NVARCHAR(100) NULL,
        [Version] NVARCHAR(50) NULL,
        [Language] NVARCHAR(10) NULL,
        
        -- Security and visibility
        [Visibility] NVARCHAR(20) NOT NULL DEFAULT 'Private', -- Private, Department, Public
        [OwnerId] NVARCHAR(100) NULL,
        
        -- Processing status
        [ProcessingStatus] NVARCHAR(50) NOT NULL DEFAULT 'Pending', -- Pending, Processing, Completed, Failed
        [ProcessingError] NVARCHAR(MAX) NULL,
        [IsChunked] BIT NOT NULL DEFAULT 0,
        [ChunkCount] INT NOT NULL DEFAULT 0,
        
        -- Audit fields
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastAccessedAt] DATETIME2 NULL,
        [AccessCount] INT NOT NULL DEFAULT 0,
        
        CONSTRAINT [FK_Documents_Category] 
            FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE SET NULL
    );
    
    -- Indexes for performance
    CREATE INDEX [IX_Documents_CategoryId] ON [dbo].[Documents]([CategoryId]);
    CREATE INDEX [IX_Documents_CreatedAt] ON [dbo].[Documents]([CreatedAt] DESC);
    CREATE INDEX [IX_Documents_ProcessingStatus] ON [dbo].[Documents]([ProcessingStatus]);
    CREATE INDEX [IX_Documents_Visibility] ON [dbo].[Documents]([Visibility]);
    
    -- Full-text index for traditional text search
    CREATE FULLTEXT CATALOG [DocNFullTextCatalog] AS DEFAULT;
    CREATE FULLTEXT INDEX ON [dbo].[Documents]([FullText], [Title], [Tags], [Notes])
        KEY INDEX [PK__Documents] ON [DocNFullTextCatalog];
END
GO

-- ============================================================================
-- Table: DocumentChunks
-- Description: Document chunks with individual VECTOR embeddings for granular search
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentChunks]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DocumentChunks] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [DocumentId] INT NOT NULL,
        [ChunkIndex] INT NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        
        -- Vector embedding for chunk-level semantic search
        [Embedding] VECTOR(1536) NULL,
        
        -- Position information
        [StartPosition] INT NULL,
        [EndPosition] INT NULL,
        [PageNumber] INT NULL,
        
        -- Metadata
        [TokenCount] INT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_DocumentChunks_Document] 
            FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[Documents]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_DocumentChunks_DocumentId_ChunkIndex] 
            UNIQUE ([DocumentId], [ChunkIndex])
    );
    
    CREATE INDEX [IX_DocumentChunks_DocumentId] ON [dbo].[DocumentChunks]([DocumentId]);
    
    -- Full-text index for chunk content
    CREATE FULLTEXT INDEX ON [dbo].[DocumentChunks]([Content])
        KEY INDEX [PK__DocumentChunks] ON [DocNFullTextCatalog];
END
GO

-- ============================================================================
-- Table: Conversations
-- Description: RAG conversation history
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Conversations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Conversations] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Title] NVARCHAR(255) NOT NULL,
        [UserId] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IsArchived] BIT NOT NULL DEFAULT 0
    );
    
    CREATE INDEX [IX_Conversations_UserId] ON [dbo].[Conversations]([UserId]);
    CREATE INDEX [IX_Conversations_CreatedAt] ON [dbo].[Conversations]([CreatedAt] DESC);
END
GO

-- ============================================================================
-- Table: Messages
-- Description: Individual messages in conversations with document references
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Messages]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Messages] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [ConversationId] INT NOT NULL,
        [Role] NVARCHAR(20) NOT NULL, -- User, Assistant, System
        [Content] NVARCHAR(MAX) NOT NULL,
        
        -- Referenced documents for context
        [ReferencedDocumentIds] NVARCHAR(1000) NULL, -- JSON array of document IDs
        [CitedChunkIds] NVARCHAR(2000) NULL, -- JSON array of chunk IDs used in response
        
        -- Metadata
        [Model] NVARCHAR(50) NULL,
        [TokensUsed] INT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT [FK_Messages_Conversation] 
            FOREIGN KEY ([ConversationId]) REFERENCES [dbo].[Conversations]([Id]) ON DELETE CASCADE
    );
    
    CREATE INDEX [IX_Messages_ConversationId] ON [dbo].[Messages]([ConversationId]);
    CREATE INDEX [IX_Messages_CreatedAt] ON [dbo].[Messages]([CreatedAt]);
END
GO

-- ============================================================================
-- Table: AuditLogs
-- Description: Audit trail for compliance and security
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuditLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuditLogs] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [EntityType] NVARCHAR(50) NOT NULL, -- Document, Conversation, etc.
        [EntityId] INT NOT NULL,
        [Action] NVARCHAR(50) NOT NULL, -- View, Download, Delete, Update, etc.
        [UserId] NVARCHAR(100) NULL,
        [IpAddress] NVARCHAR(50) NULL,
        [UserAgent] NVARCHAR(500) NULL,
        [Details] NVARCHAR(MAX) NULL, -- JSON details
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE INDEX [IX_AuditLogs_EntityType_EntityId] ON [dbo].[AuditLogs]([EntityType], [EntityId]);
    CREATE INDEX [IX_AuditLogs_UserId] ON [dbo].[AuditLogs]([UserId]);
    CREATE INDEX [IX_AuditLogs_CreatedAt] ON [dbo].[AuditLogs]([CreatedAt] DESC);
END
GO

-- ============================================================================
-- Seed Data: Default Categories
-- ============================================================================
IF NOT EXISTS (SELECT * FROM [dbo].[Categories])
BEGIN
    SET IDENTITY_INSERT [dbo].[Categories] ON;
    
    INSERT INTO [dbo].[Categories] ([Id], [Name], [Description], [ParentCategoryId], [Color], [Icon], [CreatedAt], [UpdatedAt])
    VALUES
        (1, N'Contratti', N'Contratti e accordi legali', NULL, N'#1976d2', N'description', GETUTCDATE(), GETUTCDATE()),
        (2, N'Fatture', N'Fatture e documenti fiscali', NULL, N'#388e3c', N'receipt', GETUTCDATE(), GETUTCDATE()),
        (3, N'Report', N'Report e analisi', NULL, N'#f57c00', N'assessment', GETUTCDATE(), GETUTCDATE()),
        (4, N'Documentazione Tecnica', N'Manuali e documentazione tecnica', NULL, N'#7b1fa2', N'build', GETUTCDATE(), GETUTCDATE()),
        (5, N'Risorse Umane', N'Documenti relativi alle risorse umane', NULL, N'#c2185b', N'people', GETUTCDATE(), GETUTCDATE()),
        (6, N'Marketing', N'Materiale marketing e presentazioni', NULL, N'#e91e63', N'campaign', GETUTCDATE(), GETUTCDATE()),
        (7, N'Legale', N'Documenti legali e compliance', NULL, N'#5d4037', N'gavel', GETUTCDATE(), GETUTCDATE()),
        (8, N'Amministrazione', N'Documenti amministrativi', NULL, N'#00796b', N'business', GETUTCDATE(), GETUTCDATE()),
        (9, N'Altro', N'Altri documenti non categorizzati', NULL, N'#757575', N'folder', GETUTCDATE(), GETUTCDATE());
    
    SET IDENTITY_INSERT [dbo].[Categories] OFF;
END
GO

-- ============================================================================
-- Stored Procedure: Hybrid Vector + Full-Text Search
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_HybridDocumentSearch]
    @QueryEmbedding VECTOR(1536),
    @QueryText NVARCHAR(1000),
    @CategoryId INT = NULL,
    @TopN INT = 10,
    @MinSimilarity FLOAT = 0.7,
    @UseVectorSearch BIT = 1,
    @UseFullTextSearch BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Reciprocal Rank Fusion (RRF) for combining vector and full-text results
    WITH VectorResults AS (
        SELECT 
            d.[Id],
            d.[Title],
            d.[FileName],
            d.[FullText],
            d.[CategoryId],
            c.[Name] AS CategoryName,
            d.[CreatedAt],
            d.[FileSize],
            -- Cosine similarity for vector search
            VECTOR_DISTANCE('cosine', d.[Embedding], @QueryEmbedding) AS VectorScore,
            ROW_NUMBER() OVER (ORDER BY VECTOR_DISTANCE('cosine', d.[Embedding], @QueryEmbedding) DESC) AS VectorRank
        FROM [dbo].[Documents] d
        LEFT JOIN [dbo].[Categories] c ON d.[CategoryId] = c.[Id]
        WHERE 
            (@UseVectorSearch = 1)
            AND d.[Embedding] IS NOT NULL
            AND (@CategoryId IS NULL OR d.[CategoryId] = @CategoryId)
            AND VECTOR_DISTANCE('cosine', d.[Embedding], @QueryEmbedding) >= @MinSimilarity
    ),
    FullTextResults AS (
        SELECT 
            d.[Id],
            d.[Title],
            d.[FileName],
            d.[FullText],
            d.[CategoryId],
            c.[Name] AS CategoryName,
            d.[CreatedAt],
            d.[FileSize],
            fts.[RANK] / 1000.0 AS FullTextScore,
            ROW_NUMBER() OVER (ORDER BY fts.[RANK] DESC) AS FullTextRank
        FROM [dbo].[Documents] d
        INNER JOIN FREETEXTTABLE([dbo].[Documents], ([FullText], [Title], [Tags]), @QueryText) AS fts
            ON d.[Id] = fts.[KEY]
        LEFT JOIN [dbo].[Categories] c ON d.[CategoryId] = c.[Id]
        WHERE 
            (@UseFullTextSearch = 1)
            AND (@CategoryId IS NULL OR d.[CategoryId] = @CategoryId)
    ),
    CombinedResults AS (
        SELECT 
            COALESCE(v.[Id], f.[Id]) AS Id,
            COALESCE(v.[Title], f.[Title]) AS Title,
            COALESCE(v.[FileName], f.[FileName]) AS FileName,
            COALESCE(v.[FullText], f.[FullText]) AS FullText,
            COALESCE(v.[CategoryId], f.[CategoryId]) AS CategoryId,
            COALESCE(v.[CategoryName], f.[CategoryName]) AS CategoryName,
            COALESCE(v.[CreatedAt], f.[CreatedAt]) AS CreatedAt,
            COALESCE(v.[FileSize], f.[FileSize]) AS FileSize,
            COALESCE(v.[VectorScore], 0) AS VectorScore,
            COALESCE(f.[FullTextScore], 0) AS FullTextScore,
            -- Reciprocal Rank Fusion formula
            (CASE WHEN v.[VectorRank] IS NOT NULL THEN 1.0 / (60 + v.[VectorRank]) ELSE 0 END +
             CASE WHEN f.[FullTextRank] IS NOT NULL THEN 1.0 / (60 + f.[FullTextRank]) ELSE 0 END) AS RRFScore
        FROM VectorResults v
        FULL OUTER JOIN FullTextResults f ON v.[Id] = f.[Id]
    )
    SELECT TOP (@TopN)
        [Id],
        [Title],
        [FileName],
        LEFT([FullText], 500) AS [ContentSnippet],
        [CategoryId],
        [CategoryName],
        [CreatedAt],
        [FileSize],
        [VectorScore],
        [FullTextScore],
        [RRFScore] AS [RelevanceScore]
    FROM CombinedResults
    ORDER BY [RRFScore] DESC;
END
GO

-- ============================================================================
-- Stored Procedure: Find Similar Documents by Vector
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_FindSimilarDocuments]
    @DocumentId INT,
    @TopN INT = 5,
    @MinSimilarity FLOAT = 0.7
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SourceEmbedding VECTOR(1536);
    
    SELECT @SourceEmbedding = [Embedding]
    FROM [dbo].[Documents]
    WHERE [Id] = @DocumentId;
    
    IF @SourceEmbedding IS NULL
    BEGIN
        RAISERROR('Document not found or has no embedding', 16, 1);
        RETURN;
    END
    
    SELECT TOP (@TopN)
        d.[Id],
        d.[Title],
        d.[FileName],
        d.[CategoryId],
        c.[Name] AS CategoryName,
        d.[CreatedAt],
        VECTOR_DISTANCE('cosine', d.[Embedding], @SourceEmbedding) AS SimilarityScore
    FROM [dbo].[Documents] d
    LEFT JOIN [dbo].[Categories] c ON d.[CategoryId] = c.[Id]
    WHERE 
        d.[Id] != @DocumentId
        AND d.[Embedding] IS NOT NULL
        AND VECTOR_DISTANCE('cosine', d.[Embedding], @SourceEmbedding) >= @MinSimilarity
    ORDER BY SimilarityScore DESC;
END
GO

-- ============================================================================
-- Stored Procedure: Chunk-level Semantic Search
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_SemanticChunkSearch]
    @QueryEmbedding VECTOR(1536),
    @CategoryId INT = NULL,
    @TopN INT = 20,
    @MinSimilarity FLOAT = 0.7
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT TOP (@TopN)
        dc.[Id] AS ChunkId,
        dc.[DocumentId],
        d.[Title] AS DocumentTitle,
        d.[FileName],
        dc.[Content],
        dc.[ChunkIndex],
        dc.[PageNumber],
        d.[CategoryId],
        c.[Name] AS CategoryName,
        VECTOR_DISTANCE('cosine', dc.[Embedding], @QueryEmbedding) AS SimilarityScore
    FROM [dbo].[DocumentChunks] dc
    INNER JOIN [dbo].[Documents] d ON dc.[DocumentId] = d.[Id]
    LEFT JOIN [dbo].[Categories] c ON d.[CategoryId] = c.[Id]
    WHERE 
        dc.[Embedding] IS NOT NULL
        AND (@CategoryId IS NULL OR d.[CategoryId] = @CategoryId)
        AND VECTOR_DISTANCE('cosine', dc.[Embedding], @QueryEmbedding) >= @MinSimilarity
    ORDER BY SimilarityScore DESC;
END
GO

-- ============================================================================
-- Function: Get Document Statistics
-- ============================================================================
CREATE OR ALTER FUNCTION [dbo].[fn_GetDocumentStatistics]()
RETURNS TABLE
AS
RETURN
(
    SELECT
        (SELECT COUNT(*) FROM [dbo].[Documents]) AS TotalDocuments,
        (SELECT COUNT(*) FROM [dbo].[Documents] WHERE CAST([CreatedAt] AS DATE) = CAST(GETUTCDATE() AS DATE)) AS UploadedToday,
        (SELECT COUNT(*) FROM [dbo].[Conversations]) AS TotalConversations,
        (SELECT COUNT(*) FROM [dbo].[Categories]) AS TotalCategories,
        (SELECT SUM([FileSize]) FROM [dbo].[Documents]) AS TotalStorageBytes,
        (SELECT COUNT(*) FROM [dbo].[Documents] WHERE [Embedding] IS NOT NULL) AS DocumentsWithEmbeddings,
        (SELECT COUNT(*) FROM [dbo].[DocumentChunks]) AS TotalChunks
);
GO

-- ============================================================================
-- Indexes for Vector Search Performance
-- ============================================================================
-- Note: In SQL Server 2025, vector indexes can be created for performance
-- CREATE INDEX [IX_Documents_Embedding_Vector] ON [dbo].[Documents]([Embedding]);
-- CREATE INDEX [IX_DocumentChunks_Embedding_Vector] ON [dbo].[DocumentChunks]([Embedding]);

PRINT 'Database schema created successfully!';
PRINT 'Total tables: 6 (Categories, Documents, DocumentChunks, Conversations, Messages, AuditLogs)';
PRINT 'Stored procedures: 3 (Hybrid Search, Similar Documents, Chunk Search)';
PRINT 'Default categories seeded: 9';
GO
