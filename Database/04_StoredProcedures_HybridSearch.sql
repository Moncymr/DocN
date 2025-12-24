-- ================================================
-- DocN - Stored Procedures per Ricerca Ibrida e RAG
-- SQL Server 2025 con supporto VECTOR
-- Versione: 1.0 - Dicembre 2024
-- ================================================

USE DocNDb;
GO

PRINT '';
PRINT '================================================';
PRINT '📦 Installazione Stored Procedures Avanzate';
PRINT '================================================';
PRINT '';

-- ================================================
-- 1. sp_HybridSearch
-- Ricerca ibrida combinando vector search e full-text
-- ================================================

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_HybridSearch')
    DROP PROCEDURE sp_HybridSearch;
GO

CREATE PROCEDURE sp_HybridSearch
    @QueryVector NVARCHAR(MAX), -- CSV string of embedding values (temporary until VECTOR type)
    @QueryText NVARCHAR(MAX),
    @UserId NVARCHAR(450),
    @CategoryFilter NVARCHAR(200) = NULL,
    @TopK INT = 10,
    @MinSimilarity FLOAT = 0.7
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Note: This is a simplified version using NVARCHAR(MAX) for vectors
    -- In production with SQL Server 2025, replace with native VECTOR(1536) type
    
    -- CTE per ricerca vettoriale (simulated with cosine similarity calculation)
    WITH VectorResults AS (
        SELECT TOP (@TopK * 2)
            d.Id,
            d.FileName,
            d.ActualCategory,
            d.EmbeddingVector,
            -- Placeholder score (in production, use VECTOR_DISTANCE)
            0.85 as VectorScore,
            ROW_NUMBER() OVER (ORDER BY d.UploadedAt DESC) as VectorRank
        FROM Documents d
        WHERE d.EmbeddingVector IS NOT NULL
          AND (@CategoryFilter IS NULL OR d.ActualCategory = @CategoryFilter)
          AND (d.OwnerId = @UserId 
               OR d.Visibility = 3 -- Public
               OR EXISTS (SELECT 1 FROM DocumentShares ds WHERE ds.DocumentId = d.Id AND ds.SharedWithUserId = @UserId))
    ),
    -- CTE per full-text search
    TextResults AS (
        SELECT TOP (@TopK * 2)
            d.Id,
            d.FileName,
            d.ActualCategory,
            -- Simple keyword matching score
            CASE 
                WHEN d.FileName LIKE '%' + @QueryText + '%' THEN 1.0
                WHEN d.ExtractedText LIKE '%' + @QueryText + '%' THEN 0.8
                ELSE 0.5
            END as TextScore,
            ROW_NUMBER() OVER (
                ORDER BY 
                    CASE WHEN d.FileName LIKE '%' + @QueryText + '%' THEN 0 ELSE 1 END,
                    d.UploadedAt DESC
            ) as TextRank
        FROM Documents d
        WHERE (@CategoryFilter IS NULL OR d.ActualCategory = @CategoryFilter)
          AND (d.FileName LIKE '%' + @QueryText + '%' OR d.ExtractedText LIKE '%' + @QueryText + '%')
          AND (d.OwnerId = @UserId 
               OR d.Visibility = 3
               OR EXISTS (SELECT 1 FROM DocumentShares ds WHERE ds.DocumentId = d.Id AND ds.SharedWithUserId = @UserId))
    )
    -- Reciprocal Rank Fusion (RRF)
    SELECT TOP (@TopK)
        d.Id,
        d.FileName,
        d.FilePath,
        d.ContentType,
        d.ActualCategory,
        d.SuggestedCategory,
        d.UploadedAt,
        -- RRF score: sum of 1/(k+rank) where k=60
        COALESCE((1.0 / (60 + CAST(v.VectorRank AS FLOAT))), 0) + 
        COALESCE((1.0 / (60 + CAST(t.TextRank AS FLOAT))), 0) as CombinedScore,
        v.VectorScore,
        t.TextScore,
        v.VectorRank,
        t.TextRank
    FROM Documents d
    LEFT JOIN VectorResults v ON d.Id = v.Id
    LEFT JOIN TextResults t ON d.Id = t.Id
    WHERE v.Id IS NOT NULL OR t.Id IS NOT NULL
    ORDER BY CombinedScore DESC;
END
GO

PRINT '✓ sp_HybridSearch creata';

-- ================================================
-- 2. sp_VectorSearch
-- Ricerca semantica pura basata su embedding vettoriali
-- ================================================

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_VectorSearch')
    DROP PROCEDURE sp_VectorSearch;
GO

CREATE PROCEDURE sp_VectorSearch
    @QueryVector NVARCHAR(MAX), -- CSV string of embedding values
    @UserId NVARCHAR(450),
    @CategoryFilter NVARCHAR(200) = NULL,
    @TopK INT = 10,
    @MinSimilarity FLOAT = 0.7
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Note: This is a simplified version
    -- In production with SQL Server 2025, use VECTOR_DISTANCE function
    
    SELECT TOP (@TopK)
        d.Id,
        d.FileName,
        d.FilePath,
        d.ContentType,
        d.ActualCategory,
        d.SuggestedCategory,
        d.ExtractedText,
        d.UploadedAt,
        d.EmbeddingVector,
        -- Placeholder similarity score
        0.85 as SimilarityScore
    FROM Documents d
    WHERE d.EmbeddingVector IS NOT NULL
      AND (@CategoryFilter IS NULL OR d.ActualCategory = @CategoryFilter)
      AND (d.OwnerId = @UserId 
           OR d.Visibility = 3
           OR EXISTS (SELECT 1 FROM DocumentShares ds WHERE ds.DocumentId = d.Id AND ds.SharedWithUserId = @UserId))
    ORDER BY d.UploadedAt DESC;
END
GO

PRINT '✓ sp_VectorSearch creata';

-- ================================================
-- 3. sp_RetrieveRAGContext
-- Recupera contesto ottimale per RAG (documenti + chunks)
-- ================================================

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_RetrieveRAGContext')
    DROP PROCEDURE sp_RetrieveRAGContext;
GO

CREATE PROCEDURE sp_RetrieveRAGContext
    @QueryVector NVARCHAR(MAX), -- CSV string of embedding values
    @QueryText NVARCHAR(MAX),
    @UserId NVARCHAR(450),
    @TopDocuments INT = 5,
    @TopChunks INT = 10,
    @MinSimilarity FLOAT = 0.7
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Restituisce sia documenti interi che chunks per RAG ottimale
    
    -- 1. Top documenti rilevanti
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
      AND (d.OwnerId = @UserId 
           OR d.Visibility = 3
           OR EXISTS (SELECT 1 FROM DocumentShares ds WHERE ds.DocumentId = d.Id AND ds.SharedWithUserId = @UserId))
    ORDER BY d.UploadedAt DESC
    
    UNION ALL
    
    -- 2. Top chunks rilevanti
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
      AND (d.OwnerId = @UserId 
           OR d.Visibility = 3
           OR EXISTS (SELECT 1 FROM DocumentShares ds WHERE ds.DocumentId = d.Id AND ds.SharedWithUserId = @UserId))
    ORDER BY dc.CreatedAt DESC;
END
GO

PRINT '✓ sp_RetrieveRAGContext creata';

-- ================================================
-- 4. sp_GetConversationContext
-- Recupera contesto conversazione con documenti referenziati
-- ================================================

IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GetConversationContext')
    DROP PROCEDURE sp_GetConversationContext;
GO

CREATE PROCEDURE sp_GetConversationContext
    @ConversationId INT,
    @MaxMessages INT = 10
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Messaggi recenti
    SELECT TOP (@MaxMessages)
        m.Id,
        m.Role,
        m.Content,
        m.Timestamp,
        m.ReferencedDocumentIds
    FROM Messages m
    WHERE m.ConversationId = @ConversationId
    ORDER BY m.Timestamp DESC;
    
    -- Documenti referenziati nella conversazione
    SELECT DISTINCT
        d.Id,
        d.FileName,
        d.ActualCategory,
        d.UploadedAt
    FROM Messages m
    CROSS APPLY (
        SELECT value as DocumentId
        FROM STRING_SPLIT(m.ReferencedDocumentIds, ',')
        WHERE value != ''
    ) refs
    INNER JOIN Documents d ON CAST(refs.DocumentId AS INT) = d.Id
    WHERE m.ConversationId = @ConversationId;
END
GO

PRINT '✓ sp_GetConversationContext creata';

PRINT '';
PRINT '================================================';
PRINT '✅ Stored Procedures Installate con Successo!';
PRINT '================================================';
PRINT '';
PRINT '📋 STORED PROCEDURES CREATE:';
PRINT '  1. sp_HybridSearch - Ricerca ibrida con RRF';
PRINT '  2. sp_VectorSearch - Ricerca semantica vettoriale';
PRINT '  3. sp_RetrieveRAGContext - Context retrieval per RAG';
PRINT '  4. sp_GetConversationContext - Context conversazione';
PRINT '';
PRINT '💡 USO:';
PRINT '  EXEC sp_HybridSearch @QueryVector=''...'', @QueryText=''policy'', @UserId=''user123'', @TopK=10';
PRINT '  EXEC sp_VectorSearch @QueryVector=''...'', @UserId=''user123'', @TopK=5';
PRINT '  EXEC sp_RetrieveRAGContext @QueryVector=''...'', @QueryText=''benefits'', @UserId=''user123''';
PRINT '  EXEC sp_GetConversationContext @ConversationId=123';
PRINT '';
PRINT '⚠️  NOTE:';
PRINT '  - Le stored procedures usano NVARCHAR(MAX) per i vettori';
PRINT '  - In produzione con SQL Server 2025, migrare a VECTOR(1536)';
PRINT '  - La similarità vettoriale è simulata (placeholder)';
PRINT '  - Sostituire con VECTOR_DISTANCE in produzione';
PRINT '';

GO
