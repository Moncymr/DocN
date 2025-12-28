-- =============================================
-- Script per aggiornamento Database Esistente
-- Aggiunge tabella SimilarDocuments per similarità vettoriale
-- Data: 2024-12-28
-- =============================================

USE [DocumentArchive]
GO

PRINT 'Inizio creazione tabella SimilarDocuments per tracking similarità documenti...'
GO

-- Crea tabella SimilarDocuments se non esiste
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SimilarDocuments]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SimilarDocuments](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [SourceDocumentId] [int] NOT NULL,
        [SimilarDocumentId] [int] NOT NULL,
        [SimilarityScore] [float] NOT NULL,
        [RelevantChunk] [nvarchar](1000) NULL,
        [ChunkIndex] [int] NULL,
        [AnalyzedAt] [datetime2](7) NOT NULL,
        [Rank] [int] NOT NULL,
        CONSTRAINT [PK_SimilarDocuments] PRIMARY KEY CLUSTERED ([Id] ASC)
    ) ON [PRIMARY]
    
    PRINT 'Tabella SimilarDocuments creata'
END
ELSE
BEGIN
    PRINT 'Tabella SimilarDocuments già esistente'
END
GO

-- Aggiungi Foreign Key per SourceDocumentId (con CASCADE DELETE)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SimilarDocuments_Documents_SourceDocumentId]'))
BEGIN
    ALTER TABLE [dbo].[SimilarDocuments]
    ADD CONSTRAINT [FK_SimilarDocuments_Documents_SourceDocumentId] 
    FOREIGN KEY([SourceDocumentId])
    REFERENCES [dbo].[Documents] ([Id])
    ON DELETE CASCADE
    
    PRINT 'Foreign Key FK_SimilarDocuments_Documents_SourceDocumentId aggiunta'
END
GO

-- Aggiungi Foreign Key per SimilarDocumentId (con RESTRICT per evitare cancellazioni accidentali)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_SimilarDocuments_Documents_SimilarDocumentId]'))
BEGIN
    ALTER TABLE [dbo].[SimilarDocuments]
    ADD CONSTRAINT [FK_SimilarDocuments_Documents_SimilarDocumentId] 
    FOREIGN KEY([SimilarDocumentId])
    REFERENCES [dbo].[Documents] ([Id])
    
    PRINT 'Foreign Key FK_SimilarDocuments_Documents_SimilarDocumentId aggiunta'
END
GO

-- Crea indice su SimilarDocumentId per performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SimilarDocuments]') AND name = N'IX_SimilarDocuments_SimilarDocumentId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SimilarDocuments_SimilarDocumentId]
    ON [dbo].[SimilarDocuments] ([SimilarDocumentId] ASC)
    
    PRINT 'Indice IX_SimilarDocuments_SimilarDocumentId creato'
END
GO

-- Crea indice su SourceDocumentId per performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SimilarDocuments]') AND name = N'IX_SimilarDocuments_SourceDocumentId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SimilarDocuments_SourceDocumentId]
    ON [dbo].[SimilarDocuments] ([SourceDocumentId] ASC)
    
    PRINT 'Indice IX_SimilarDocuments_SourceDocumentId creato'
END
GO

-- Crea indice composito su SourceDocumentId e Rank per query ordinate
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SimilarDocuments]') AND name = N'IX_SimilarDocuments_SourceDocumentId_Rank')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SimilarDocuments_SourceDocumentId_Rank]
    ON [dbo].[SimilarDocuments] ([SourceDocumentId] ASC, [Rank] ASC)
    
    PRINT 'Indice IX_SimilarDocuments_SourceDocumentId_Rank creato'
END
GO

-- Crea indice composito su SourceDocumentId e SimilarityScore per query di similarità
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[SimilarDocuments]') AND name = N'IX_SimilarDocuments_SourceDocumentId_SimilarityScore')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_SimilarDocuments_SourceDocumentId_SimilarityScore]
    ON [dbo].[SimilarDocuments] ([SourceDocumentId] ASC, [SimilarityScore] ASC)
    
    PRINT 'Indice IX_SimilarDocuments_SourceDocumentId_SimilarityScore creato'
END
GO

PRINT 'Aggiornamento completato con successo!'
PRINT 'La tabella SimilarDocuments è ora disponibile per:'
PRINT '  - Tracking delle relazioni di similarità tra documenti'
PRINT '  - Analisi vettoriale con score di similarità (0-1)'
PRINT '  - Top 5 documenti più simili per ogni documento'
PRINT '  - Storicizzazione delle analisi con timestamp'
PRINT ''
PRINT 'Esempio query per ottenere documenti simili:'
PRINT '  SELECT TOP 5 d.FileName, sd.SimilarityScore, sd.Rank'
PRINT '  FROM SimilarDocuments sd'
PRINT '  INNER JOIN Documents d ON sd.SimilarDocumentId = d.Id'
PRINT '  WHERE sd.SourceDocumentId = @DocumentId'
PRINT '  ORDER BY sd.Rank'
GO
