-- ================================================
-- DocN - Add Flexible Vector Dimensions Support
-- SQL Server 2025
-- Date: December 2024
-- ================================================
-- This script adds EmbeddingDimension tracking columns
-- to support flexible vector dimensions (700, 768, 1536, 1583, etc.)
-- ================================================

USE DocNDb;
GO

PRINT '';
PRINT '================================================';
PRINT '🔄 Adding Flexible Vector Dimensions Support';
PRINT '================================================';
PRINT '';

-- Check if columns already exist
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Documents' AND COLUMN_NAME = 'EmbeddingDimension'
)
BEGIN
    PRINT '📊 Adding EmbeddingDimension column to Documents table...';
    
    ALTER TABLE Documents
    ADD EmbeddingDimension INT NULL;
    
    PRINT '  ✓ EmbeddingDimension column added to Documents';
    PRINT '';
END
ELSE
BEGIN
    PRINT '  ℹ️  EmbeddingDimension column already exists in Documents';
    PRINT '';
END

IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'DocumentChunks' AND COLUMN_NAME = 'EmbeddingDimension'
)
BEGIN
    PRINT '📊 Adding EmbeddingDimension column to DocumentChunks table...';
    
    ALTER TABLE DocumentChunks
    ADD EmbeddingDimension INT NULL;
    
    PRINT '  ✓ EmbeddingDimension column added to DocumentChunks';
    PRINT '';
END
ELSE
BEGIN
    PRINT '  ℹ️  EmbeddingDimension column already exists in DocumentChunks';
    PRINT '';
END

-- Optional: Calculate and populate dimensions for existing embeddings
PRINT '📊 Would you like to calculate dimensions for existing embeddings?';
PRINT '  Note: This requires the EmbeddingVector to be stored as JSON array format';
PRINT '  If embeddings are stored in binary format, dimension calculation won''t work';
PRINT '';
PRINT '  To manually update dimensions for specific documents:';
PRINT '  UPDATE Documents SET EmbeddingDimension = [dimension_value] WHERE Id = [document_id];';
PRINT '';

-- Verify the changes
PRINT '🔍 Verifying schema changes...';
PRINT '';

SELECT 
    'Documents' AS TableName,
    COLUMN_NAME AS ColumnName,
    DATA_TYPE AS DataType,
    IS_NULLABLE AS IsNullable
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Documents'
AND COLUMN_NAME IN ('EmbeddingVector', 'EmbeddingDimension')
UNION ALL
SELECT 
    'DocumentChunks' AS TableName,
    COLUMN_NAME AS ColumnName,
    DATA_TYPE AS DataType,
    IS_NULLABLE AS IsNullable
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'DocumentChunks'
AND COLUMN_NAME IN ('ChunkEmbedding', 'EmbeddingDimension')
ORDER BY TableName, ColumnName;

PRINT '';
PRINT '================================================';
PRINT '✅ Flexible Vector Dimensions Support Added';
PRINT '================================================';
PRINT '';
PRINT '📝 Summary:';
PRINT '  • EmbeddingDimension column added to Documents';
PRINT '  • EmbeddingDimension column added to DocumentChunks';
PRINT '  • System now supports any dimension from 256 to 4096';
PRINT '  • Common dimensions:';
PRINT '    - 700 (Gemini custom)';
PRINT '    - 768 (Gemini default)';
PRINT '    - 1536 (OpenAI ada-002)';
PRINT '    - 1583 (OpenAI custom)';
PRINT '    - 3072 (OpenAI large)';
PRINT '';
PRINT '💡 Next steps:';
PRINT '  1. New embeddings will automatically track their dimension';
PRINT '  2. Existing embeddings will continue to work';
PRINT '  3. When re-processing documents, dimension will be tracked';
PRINT '  4. Different dimensions can coexist in the same database';
PRINT '';
PRINT '📚 See FLEXIBLE_VECTOR_DIMENSIONS.md for full documentation';
PRINT '';

GO
