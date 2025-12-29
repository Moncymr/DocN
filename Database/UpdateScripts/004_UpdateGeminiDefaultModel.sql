-- =============================================
-- Script per aggiornamento Database Esistente
-- Aggiorna il modello Gemini predefinito da gemini-1.5-flash a gemini-2.0-flash-exp
-- Data: 2024-12-29
-- Motivo: Il modello gemini-1.5-flash è deprecato e non disponibile per i nuovi account
-- =============================================

USE [DocumentArchive]
GO

PRINT 'Inizio aggiornamento modelli Gemini nella tabella AIConfigurations...'
GO

-- Aggiorna i record esistenti che usano ancora gemini-1.5-flash
UPDATE [dbo].[AIConfigurations]
SET [GeminiChatModel] = 'gemini-2.0-flash-exp'
WHERE [GeminiChatModel] = 'gemini-1.5-flash'
GO

-- Verifica e stampa il numero di record aggiornati
DECLARE @UpdatedRows INT
SELECT @UpdatedRows = COUNT(*)
FROM [dbo].[AIConfigurations]
WHERE [GeminiChatModel] = 'gemini-2.0-flash-exp'

PRINT 'Record aggiornati: ' + CAST(@UpdatedRows AS VARCHAR(10))
PRINT 'Aggiornamento completato con successo!'
PRINT ''
PRINT 'Nota: Il modello gemini-1.5-flash è deprecato.'
PRINT 'Modelli Gemini raccomandati:'
PRINT '  - gemini-2.0-flash-exp (predefinito)'
PRINT '  - gemini-2.5-flash'
PRINT '  - gemini-3-flash'
GO
