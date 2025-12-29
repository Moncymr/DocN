-- ============================================================================
-- Migration Script: Update VECTOR dimension from 1536 to 768
-- ============================================================================
-- This script updates existing databases that were created with VECTOR(1536)
-- to use VECTOR(768) to match Gemini embedding requirements.
--
-- IMPORTANT: This script will:
-- 1. Drop existing vector data (embeddings will need to be regenerated)
-- 2. Alter column types from VECTOR(1536) to VECTOR(768)
-- 3. Update stored procedures to use VECTOR(768)
--
-- Run this script only if you have a database with VECTOR(1536) columns
-- and want to use Gemini embeddings (768 dimensions)
-- ============================================================================

USE DocN;
GO

PRINT '============================================================================';
PRINT 'Starting migration from VECTOR(1536) to VECTOR(768)';
PRINT '============================================================================';
PRINT '';

-- ============================================================================
-- Step 1: Check current vector column types
-- ============================================================================
PRINT 'Step 1: Checking current vector column configuration...';
PRINT '';

SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Documents', 'DocumentChunks')
AND COLUMN_NAME IN ('Embedding', 'EmbeddingVector', 'ChunkEmbedding');

PRINT '';
PRINT '============================================================================';

-- ============================================================================
-- Step 2: Backup existing vector data (optional - currently cleared)
-- ============================================================================
PRINT 'Step 2: Clearing existing vector data...';
PRINT 'NOTE: Embeddings will need to be regenerated after this migration.';
PRINT '';

-- Clear existing embeddings to avoid conversion issues
DECLARE @DocumentsCleared INT;
DECLARE @ChunksCleared INT;

UPDATE [dbo].[Documents] 
SET [Embedding] = NULL 
WHERE [Embedding] IS NOT NULL;
SET @DocumentsCleared = @@ROWCOUNT;

UPDATE [dbo].[DocumentChunks] 
SET [Embedding] = NULL 
WHERE [Embedding] IS NOT NULL;
SET @ChunksCleared = @@ROWCOUNT;

PRINT 'Cleared embeddings: ' + CAST(@DocumentsCleared AS NVARCHAR) + ' documents, ' + CAST(@ChunksCleared AS NVARCHAR) + ' chunks.';
PRINT '';

-- ============================================================================
-- Step 3: Alter Documents table
-- ============================================================================
PRINT 'Step 3: Updating Documents.Embedding column to VECTOR(768)...';

BEGIN TRY
    -- Check if column exists and alter it
    IF EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'Documents' 
        AND COLUMN_NAME = 'Embedding'
    )
    BEGIN
        ALTER TABLE [dbo].[Documents] 
        ALTER COLUMN [Embedding] VECTOR(768) NULL;
        
        PRINT '✓ Documents.Embedding updated to VECTOR(768)';
    END
    ELSE
    BEGIN
        PRINT '⚠ Documents.Embedding column not found - skipping';
    END
END TRY
BEGIN CATCH
    PRINT '✗ Error updating Documents.Embedding: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- ============================================================================
-- Step 4: Alter DocumentChunks table
-- ============================================================================
PRINT 'Step 4: Updating DocumentChunks.Embedding column to VECTOR(768)...';

BEGIN TRY
    -- Check if column exists and alter it
    IF EXISTS (
        SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'DocumentChunks' 
        AND COLUMN_NAME = 'Embedding'
    )
    BEGIN
        ALTER TABLE [dbo].[DocumentChunks] 
        ALTER COLUMN [Embedding] VECTOR(768) NULL;
        
        PRINT '✓ DocumentChunks.Embedding updated to VECTOR(768)';
    END
    ELSE
    BEGIN
        PRINT '⚠ DocumentChunks.Embedding column not found - skipping';
    END
END TRY
BEGIN CATCH
    PRINT '✗ Error updating DocumentChunks.Embedding: ' + ERROR_MESSAGE();
END CATCH

PRINT '';

-- ============================================================================
-- Step 5: Update stored procedures
-- ============================================================================
PRINT 'Step 5: Updating stored procedures to use VECTOR(768)...';
PRINT '';

-- Update sp_HybridDocumentSearch
PRINT '  Updating sp_HybridDocumentSearch...';
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_HybridDocumentSearch]
    @QueryEmbedding VECTOR(768),
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

PRINT '  ✓ sp_HybridDocumentSearch updated';
PRINT '';

-- Update sp_FindSimilarDocuments
PRINT '  Updating sp_FindSimilarDocuments...';
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_FindSimilarDocuments]
    @DocumentId INT,
    @TopN INT = 5,
    @MinSimilarity FLOAT = 0.7
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @SourceEmbedding VECTOR(768);
    
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

PRINT '  ✓ sp_FindSimilarDocuments updated';
PRINT '';

-- Update sp_SemanticChunkSearch
PRINT '  Updating sp_SemanticChunkSearch...';
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_SemanticChunkSearch]
    @QueryEmbedding VECTOR(768),
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

PRINT '  ✓ sp_SemanticChunkSearch updated';
PRINT '';

-- ============================================================================
-- Step 6: Verify the migration
-- ============================================================================
PRINT 'Step 6: Verifying migration results...';
PRINT '';

SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Documents', 'DocumentChunks')
AND COLUMN_NAME IN ('Embedding', 'EmbeddingVector', 'ChunkEmbedding');

PRINT '';
PRINT '============================================================================';
PRINT 'Migration completed successfully!';
PRINT '';
PRINT 'NEXT STEPS:';
PRINT '1. Update AI Configuration to use Gemini embeddings provider';
PRINT '2. Restart the DocN application';
PRINT '3. Re-process documents to regenerate embeddings with 768 dimensions';
PRINT '4. Verify that document uploads and searches work correctly';
PRINT '';
PRINT 'NOTE: All existing embeddings have been cleared and must be regenerated.';
PRINT '============================================================================';
GO
