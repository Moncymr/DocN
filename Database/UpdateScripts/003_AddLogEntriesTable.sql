-- =============================================
-- Script: 003_AddLogEntriesTable.sql
-- Description: Aggiunge la tabella LogEntries per il sistema di logging centralizzato
-- Date: 2025-12-29
-- =============================================

PRINT '⏳ Creazione tabella LogEntries per il sistema di logging...';

-- Controlla se la tabella esiste già
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[LogEntries]') AND type in (N'U'))
BEGIN
    -- Crea la tabella LogEntries
    CREATE TABLE [dbo].[LogEntries] (
        [Id] INT NOT NULL IDENTITY(1,1),
        [Timestamp] DATETIME2 NOT NULL,
        [Level] NVARCHAR(50) NOT NULL,
        [Category] NVARCHAR(100) NOT NULL,
        [Message] NVARCHAR(2000) NOT NULL,
        [Details] NVARCHAR(MAX) NULL,
        [UserId] NVARCHAR(450) NULL,
        [FileName] NVARCHAR(500) NULL,
        [StackTrace] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_LogEntries] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT '✅ Tabella LogEntries creata con successo.';
END
ELSE
BEGIN
    PRINT '✓ La tabella LogEntries esiste già.';
END
GO

-- Crea indice su Timestamp per query di ricerca temporale
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LogEntries_Timestamp' AND object_id = OBJECT_ID(N'[dbo].[LogEntries]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LogEntries_Timestamp] 
    ON [dbo].[LogEntries] ([Timestamp] ASC);
    
    PRINT '✅ Indice IX_LogEntries_Timestamp creato.';
END
ELSE
BEGIN
    PRINT '✓ Indice IX_LogEntries_Timestamp già esistente.';
END
GO

-- Crea indice su Category e Timestamp per filtrare per categoria
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LogEntries_Category_Timestamp' AND object_id = OBJECT_ID(N'[dbo].[LogEntries]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LogEntries_Category_Timestamp] 
    ON [dbo].[LogEntries] ([Category] ASC, [Timestamp] ASC);
    
    PRINT '✅ Indice IX_LogEntries_Category_Timestamp creato.';
END
ELSE
BEGIN
    PRINT '✓ Indice IX_LogEntries_Category_Timestamp già esistente.';
END
GO

-- Crea indice su UserId e Timestamp per filtrare per utente
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LogEntries_UserId_Timestamp' AND object_id = OBJECT_ID(N'[dbo].[LogEntries]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_LogEntries_UserId_Timestamp] 
    ON [dbo].[LogEntries] ([UserId] ASC, [Timestamp] ASC);
    
    PRINT '✅ Indice IX_LogEntries_UserId_Timestamp creato.';
END
ELSE
BEGIN
    PRINT '✓ Indice IX_LogEntries_UserId_Timestamp già esistente.';
END
GO

PRINT '✅ Script di creazione tabella LogEntries completato con successo!';
GO

-- ================================================
-- Query di esempio per testare la tabella
-- ================================================

-- Inserisci un log di test (opzionale - commentato per sicurezza)
/*
INSERT INTO [dbo].[LogEntries] ([Timestamp], [Level], [Category], [Message], [Details], [UserId], [FileName])
VALUES (GETUTCDATE(), 'Info', 'System', 'Tabella LogEntries creata e testata con successo', 'Test inserimento', NULL, NULL);

SELECT * FROM [dbo].[LogEntries] ORDER BY [Timestamp] DESC;
*/
