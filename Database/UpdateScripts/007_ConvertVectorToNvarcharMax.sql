-- =============================================================================
-- Script: Convert VECTOR columns to nvarchar(max) for flexible dimensions
-- Purpose: Fix "Le dimensioni del vettore X e Y non corrispondono" error
-- 
-- This script converts native SQL Server VECTOR columns with fixed dimensions
-- (e.g., VECTOR(768), VECTOR(1536)) to nvarchar(max) to support flexible
-- embedding dimensions from different AI providers.
--
-- Background:
-- - Gemini generates embeddings with 768 dimensions (or custom like 700)
-- - OpenAI generates embeddings with 1536 dimensions (or custom like 1583)
-- - Native VECTOR type with fixed dimensions causes errors when dimensions don't match
-- - Using nvarchar(max) with JSON storage allows any dimension to be stored
--
-- Safe to run multiple times - checks column types before altering
-- =============================================================================

USE [DocN];
GO

PRINT '=================================================================';
PRINT 'Converting VECTOR columns to nvarchar(max) for flexible dimensions';
PRINT '=================================================================';
PRINT '';

-- Check if Documents table exists
IF OBJECT_ID('dbo.Documents', 'U') IS NOT NULL
BEGIN
    PRINT 'Checking Documents.EmbeddingVector column...';
    
    -- Check if EmbeddingVector column is a VECTOR type
    IF EXISTS (
        SELECT 1 
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID('dbo.Documents')
        AND c.name = 'EmbeddingVector'
        AND t.name = 'vector'
    )
    BEGIN
        PRINT '  ⚠ EmbeddingVector is VECTOR type - converting to nvarchar(max)...';
        
        -- Alter column from VECTOR to nvarchar(max)
        ALTER TABLE dbo.Documents 
        ALTER COLUMN EmbeddingVector nvarchar(max) NULL;
        
        PRINT '  ✓ Documents.EmbeddingVector converted to nvarchar(max)';
    END
    ELSE IF EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'Documents'
        AND COLUMN_NAME = 'EmbeddingVector'
        AND DATA_TYPE = 'nvarchar'
    )
    BEGIN
        PRINT '  ✓ Documents.EmbeddingVector is already nvarchar(max) - no changes needed';
    END
    ELSE
    BEGIN
        PRINT '  ⚠ Documents.EmbeddingVector column not found or has unexpected type';
    END
    PRINT '';
END
ELSE
BEGIN
    PRINT '⚠ Documents table not found';
    PRINT '';
END

-- Check if DocumentChunks table exists
IF OBJECT_ID('dbo.DocumentChunks', 'U') IS NOT NULL
BEGIN
    PRINT 'Checking DocumentChunks.ChunkEmbedding column...';
    
    -- Check if ChunkEmbedding column is a VECTOR type
    IF EXISTS (
        SELECT 1 
        FROM sys.columns c
        INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
        WHERE c.object_id = OBJECT_ID('dbo.DocumentChunks')
        AND c.name = 'ChunkEmbedding'
        AND t.name = 'vector'
    )
    BEGIN
        PRINT '  ⚠ ChunkEmbedding is VECTOR type - converting to nvarchar(max)...';
        
        -- Alter column from VECTOR to nvarchar(max)
        ALTER TABLE dbo.DocumentChunks 
        ALTER COLUMN ChunkEmbedding nvarchar(max) NULL;
        
        PRINT '  ✓ DocumentChunks.ChunkEmbedding converted to nvarchar(max)';
    END
    ELSE IF EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'DocumentChunks'
        AND COLUMN_NAME = 'ChunkEmbedding'
        AND DATA_TYPE = 'nvarchar'
    )
    BEGIN
        PRINT '  ✓ DocumentChunks.ChunkEmbedding is already nvarchar(max) - no changes needed';
    END
    ELSE
    BEGIN
        PRINT '  ⚠ DocumentChunks.ChunkEmbedding column not found or has unexpected type';
    END
    PRINT '';
END
ELSE
BEGIN
    PRINT '⚠ DocumentChunks table not found';
    PRINT '';
END

-- Verify the changes
PRINT '=================================================================';
PRINT 'Verification: Current column types';
PRINT '=================================================================';
PRINT '';

SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Documents', 'DocumentChunks')
AND COLUMN_NAME IN ('EmbeddingVector', 'ChunkEmbedding');

PRINT '';
PRINT '=================================================================';
PRINT 'Migration complete!';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Restart the DocN application';
PRINT '2. Try uploading a new document';
PRINT '3. The system now supports flexible embedding dimensions:';
PRINT '   - Gemini: 700-768 dimensions';
PRINT '   - OpenAI: 1536, 1583, 3072 dimensions';
PRINT '   - Any custom dimension between 256 and 4096';
PRINT '=================================================================';
