-- =============================================================================
-- Script: Add dual VECTOR fields for 768 and 1536 dimensions
-- Purpose: Support both Gemini (768) and OpenAI (1536) embeddings simultaneously
-- 
-- This script adds two separate VECTOR fields to support different AI providers:
-- - EmbeddingVector768 for Gemini and similar providers
-- - EmbeddingVector1536 for OpenAI and similar providers
--
-- Benefits:
-- - Native VECTOR type for optimal performance
-- - No dimension mismatch errors
-- - Support multiple AI providers in the same database
-- - Each document uses the appropriate field based on its embedding dimension
--
-- Safe to run multiple times - checks if columns exist before adding
-- =============================================================================

USE [DocN];
GO

PRINT '=================================================================';
PRINT 'Adding dual VECTOR fields for flexible AI provider support';
PRINT '=================================================================';
PRINT '';

-- Check if Documents table exists
IF OBJECT_ID('dbo.Documents', 'U') IS NOT NULL
BEGIN
    PRINT 'Processing Documents table...';
    
    -- Add EmbeddingVector768 column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'Documents'
        AND COLUMN_NAME = 'EmbeddingVector768'
    )
    BEGIN
        PRINT '  Adding EmbeddingVector768 VECTOR(768)...';
        ALTER TABLE dbo.Documents 
        ADD EmbeddingVector768 VECTOR(768) NULL;
        PRINT '  ✓ EmbeddingVector768 added successfully';
    END
    ELSE
    BEGIN
        PRINT '  ✓ EmbeddingVector768 already exists';
    END
    
    -- Add EmbeddingVector1536 column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'Documents'
        AND COLUMN_NAME = 'EmbeddingVector1536'
    )
    BEGIN
        PRINT '  Adding EmbeddingVector1536 VECTOR(1536)...';
        ALTER TABLE dbo.Documents 
        ADD EmbeddingVector1536 VECTOR(1536) NULL;
        PRINT '  ✓ EmbeddingVector1536 added successfully';
    END
    ELSE
    BEGIN
        PRINT '  ✓ EmbeddingVector1536 already exists';
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
    PRINT 'Processing DocumentChunks table...';
    
    -- Add ChunkEmbedding768 column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'DocumentChunks'
        AND COLUMN_NAME = 'ChunkEmbedding768'
    )
    BEGIN
        PRINT '  Adding ChunkEmbedding768 VECTOR(768)...';
        ALTER TABLE dbo.DocumentChunks 
        ADD ChunkEmbedding768 VECTOR(768) NULL;
        PRINT '  ✓ ChunkEmbedding768 added successfully';
    END
    ELSE
    BEGIN
        PRINT '  ✓ ChunkEmbedding768 already exists';
    END
    
    -- Add ChunkEmbedding1536 column if it doesn't exist
    IF NOT EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'DocumentChunks'
        AND COLUMN_NAME = 'ChunkEmbedding1536'
    )
    BEGIN
        PRINT '  Adding ChunkEmbedding1536 VECTOR(1536)...';
        ALTER TABLE dbo.DocumentChunks 
        ADD ChunkEmbedding1536 VECTOR(1536) NULL;
        PRINT '  ✓ ChunkEmbedding1536 added successfully';
    END
    ELSE
    BEGIN
        PRINT '  ✓ ChunkEmbedding1536 already exists';
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
PRINT 'Verification: New VECTOR columns';
PRINT '=================================================================';
PRINT '';

SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Documents', 'DocumentChunks')
AND COLUMN_NAME IN ('EmbeddingVector768', 'EmbeddingVector1536', 'ChunkEmbedding768', 'ChunkEmbedding1536')
ORDER BY TABLE_NAME, COLUMN_NAME;

PRINT '';
PRINT '=================================================================';
PRINT 'Migration complete!';
PRINT '';
PRINT 'Next steps:';
PRINT '1. Restart the DocN application';
PRINT '2. Upload documents - the system will automatically use:';
PRINT '   - EmbeddingVector768 for 768-dimensional embeddings (Gemini)';
PRINT '   - EmbeddingVector1536 for 1536-dimensional embeddings (OpenAI)';
PRINT '3. No more dimension mismatch errors!';
PRINT '';
PRINT 'Note: The EmbeddingDimension field tracks which field is used.';
PRINT '=================================================================';
