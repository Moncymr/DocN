-- =============================================
-- Script per aggiornamento Database Esistente
-- Corregge vincolo NOT NULL su ReferencedDocumentIds
-- Data: 2026-01-03
-- =============================================
-- 
-- PROBLEMA:
-- La colonna ReferencedDocumentIds nella tabella Messages è stata 
-- creata con vincolo NOT NULL, ma dovrebbe essere NULLABLE.
-- I messaggi utente non hanno documenti referenziati, quindi il 
-- campo rimane NULL causando errori SQL.
--
-- SOLUZIONE:
-- Modifica la colonna da NOT NULL a NULL
-- =============================================

USE [DocNDb]
GO

PRINT 'Inizio correzione vincolo ReferencedDocumentIds nella tabella Messages...'
GO

-- Verifica se la colonna esiste e ha il vincolo NOT NULL
IF EXISTS (
    SELECT * 
    FROM sys.columns c
    JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = 'Messages' 
    AND c.name = 'ReferencedDocumentIds'
    AND c.is_nullable = 0
)
BEGIN
    PRINT 'Modifica colonna ReferencedDocumentIds da NOT NULL a NULL...'
    
    -- Modifica la colonna per permettere valori NULL
    ALTER TABLE [dbo].[Messages]
    ALTER COLUMN [ReferencedDocumentIds] NVARCHAR(MAX) NULL;
    
    PRINT '✓ Colonna ReferencedDocumentIds ora accetta valori NULL'
END
ELSE IF EXISTS (
    SELECT * 
    FROM sys.columns c
    JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = 'Messages' 
    AND c.name = 'ReferencedDocumentIds'
    AND c.is_nullable = 1
)
BEGIN
    PRINT '✓ Colonna ReferencedDocumentIds già configurata come NULL (nessuna azione necessaria)'
END
ELSE
BEGIN
    PRINT '⚠ ATTENZIONE: Colonna ReferencedDocumentIds non trovata nella tabella Messages'
END
GO

-- Verifica il risultato
IF EXISTS (
    SELECT * 
    FROM sys.columns c
    JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = 'Messages' 
    AND c.name = 'ReferencedDocumentIds'
    AND c.is_nullable = 1
)
BEGIN
    PRINT ''
    PRINT '========================================='
    PRINT 'AGGIORNAMENTO COMPLETATO CON SUCCESSO!'
    PRINT '========================================='
    PRINT 'La colonna ReferencedDocumentIds ora accetta NULL'
    PRINT 'I messaggi utente possono essere salvati senza errori'
    PRINT ''
END
ELSE
BEGIN
    PRINT ''
    PRINT '========================================='
    PRINT 'ERRORE: Aggiornamento non riuscito'
    PRINT '========================================='
    PRINT 'Verificare manualmente lo stato della tabella Messages'
    PRINT ''
END
GO

PRINT 'Script completato.'
GO
