-- =============================================
-- Script: 02_CreateDocumentTables.sql
-- Description: Crea le tabelle per la gestione documenti con supporto full-text search
-- =============================================

PRINT '⏳ Creazione tabelle documenti...';

-- Tabella Documents
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Documents]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Documents] (
        -- Chiave primaria: INT IDENTITY per garantire unicità e compatibilità con full-text index
        -- La chiave primaria deve essere un indice univoco, non nullable, a colonna singola, < 900 bytes
        [DocumentId] INT IDENTITY(1,1) NOT NULL,
        
        -- Informazioni documento
        [FileName] NVARCHAR(255) NOT NULL,
        [OriginalFileName] NVARCHAR(255) NOT NULL,
        [FilePath] NVARCHAR(500) NOT NULL,
        [FileSize] BIGINT NOT NULL,
        [MimeType] NVARCHAR(100) NOT NULL,
        [FileHash] NVARCHAR(64) NULL, -- SHA-256 hash
        
        -- Contenuto e metadati per la ricerca
        [ExtractedText] NVARCHAR(MAX) NULL, -- Testo estratto dal documento per full-text search
        [Title] NVARCHAR(500) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [Category] NVARCHAR(100) NULL,
        [Keywords] NVARCHAR(500) NULL,
        
        -- Embedding vettoriali per ricerca semantica
        [TextEmbedding] VARBINARY(MAX) NULL, -- Vettore embedding per ricerca semantica
        [EmbeddingModel] NVARCHAR(100) NULL, -- Modello utilizzato per generare l'embedding
        
        -- Metadati utente e temporali
        [UploadedBy] NVARCHAR(450) NOT NULL,
        [UploadedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedBy] NVARCHAR(450) NULL,
        [ModifiedDate] DATETIME2 NULL,
        
        -- Stato e visibilità
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsPublic] BIT NOT NULL DEFAULT 0,
        
        -- Chiave primaria su INT IDENTITY - perfetta per full-text index
        CONSTRAINT [PK_Documents] PRIMARY KEY CLUSTERED ([DocumentId] ASC),
        
        -- Foreign key verso AspNetUsers
        CONSTRAINT [FK_Documents_AspNetUsers_UploadedBy] FOREIGN KEY ([UploadedBy]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]),
        CONSTRAINT [FK_Documents_AspNetUsers_ModifiedBy] FOREIGN KEY ([ModifiedBy]) 
            REFERENCES [dbo].[AspNetUsers] ([Id])
    );
    
    -- Indici per migliorare le performance
    CREATE INDEX [IX_Documents_UploadedBy] ON [dbo].[Documents] ([UploadedBy]);
    CREATE INDEX [IX_Documents_Category] ON [dbo].[Documents] ([Category]);
    CREATE INDEX [IX_Documents_UploadedDate] ON [dbo].[Documents] ([UploadedDate]);
    CREATE INDEX [IX_Documents_FileHash] ON [dbo].[Documents] ([FileHash]);
    
    PRINT '  ✓ Documents creata';
END
ELSE
    PRINT '    Documents già esistente';

-- Tabella DocumentShares
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentShares]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DocumentShares] (
        [ShareId] INT IDENTITY(1,1) NOT NULL,
        [DocumentId] INT NOT NULL,
        [SharedWithUserId] NVARCHAR(450) NOT NULL,
        [SharedBy] NVARCHAR(450) NOT NULL,
        [ShareDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [CanEdit] BIT NOT NULL DEFAULT 0,
        [CanShare] BIT NOT NULL DEFAULT 0,
        [ExpiryDate] DATETIME2 NULL,
        
        CONSTRAINT [PK_DocumentShares] PRIMARY KEY CLUSTERED ([ShareId] ASC),
        CONSTRAINT [FK_DocumentShares_Documents_DocumentId] FOREIGN KEY ([DocumentId]) 
            REFERENCES [dbo].[Documents] ([DocumentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentShares_AspNetUsers_SharedWithUserId] FOREIGN KEY ([SharedWithUserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]),
        CONSTRAINT [FK_DocumentShares_AspNetUsers_SharedBy] FOREIGN KEY ([SharedBy]) 
            REFERENCES [dbo].[AspNetUsers] ([Id])
    );
    
    CREATE INDEX [IX_DocumentShares_DocumentId] ON [dbo].[DocumentShares] ([DocumentId]);
    CREATE INDEX [IX_DocumentShares_SharedWithUserId] ON [dbo].[DocumentShares] ([SharedWithUserId]);
    
    PRINT '  ✓ DocumentShares creata';
END
ELSE
    PRINT '    DocumentShares già esistente';

-- Tabella DocumentTags
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentTags]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DocumentTags] (
        [TagId] INT IDENTITY(1,1) NOT NULL,
        [DocumentId] INT NOT NULL,
        [TagName] NVARCHAR(100) NOT NULL,
        [CreatedBy] NVARCHAR(450) NOT NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT [PK_DocumentTags] PRIMARY KEY CLUSTERED ([TagId] ASC),
        CONSTRAINT [FK_DocumentTags_Documents_DocumentId] FOREIGN KEY ([DocumentId]) 
            REFERENCES [dbo].[Documents] ([DocumentId]) ON DELETE CASCADE,
        CONSTRAINT [FK_DocumentTags_AspNetUsers_CreatedBy] FOREIGN KEY ([CreatedBy]) 
            REFERENCES [dbo].[AspNetUsers] ([Id])
    );
    
    CREATE INDEX [IX_DocumentTags_DocumentId] ON [dbo].[DocumentTags] ([DocumentId]);
    CREATE INDEX [IX_DocumentTags_TagName] ON [dbo].[DocumentTags] ([TagName]);
    
    PRINT '  ✓ DocumentTags creata';
END
ELSE
    PRINT '    DocumentTags già esistente';

GO
