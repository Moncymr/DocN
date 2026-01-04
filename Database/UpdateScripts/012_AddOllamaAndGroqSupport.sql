-- =============================================
-- Script per aggiornamento Database Esistente
-- Aggiunge supporto per provider Ollama e Groq
-- Data: 2026-01-04
-- =============================================

USE [DocumentArchive]
GO

PRINT 'Inizio aggiornamento tabella AIConfigurations per supporto Ollama e Groq...'
GO

-- =============================================
-- Ollama Settings
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'OllamaEndpoint')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [OllamaEndpoint] nvarchar(max) NULL;
    PRINT '✓ Colonna OllamaEndpoint aggiunta'
END
ELSE
BEGIN
    PRINT '  OllamaEndpoint già esistente'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'OllamaChatModel')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [OllamaChatModel] nvarchar(max) NULL;
    PRINT '✓ Colonna OllamaChatModel aggiunta'
END
ELSE
BEGIN
    PRINT '  OllamaChatModel già esistente'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'OllamaEmbeddingModel')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [OllamaEmbeddingModel] nvarchar(max) NULL;
    PRINT '✓ Colonna OllamaEmbeddingModel aggiunta'
END
ELSE
BEGIN
    PRINT '  OllamaEmbeddingModel già esistente'
END
GO

-- =============================================
-- Groq Settings
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'GroqApiKey')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [GroqApiKey] nvarchar(max) NULL;
    PRINT '✓ Colonna GroqApiKey aggiunta'
END
ELSE
BEGIN
    PRINT '  GroqApiKey già esistente'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'GroqChatModel')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [GroqChatModel] nvarchar(max) NULL;
    PRINT '✓ Colonna GroqChatModel aggiunta'
END
ELSE
BEGIN
    PRINT '  GroqChatModel già esistente'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'GroqEndpoint')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [GroqEndpoint] nvarchar(max) NULL;
    PRINT '✓ Colonna GroqEndpoint aggiunta'
END
ELSE
BEGIN
    PRINT '  GroqEndpoint già esistente'
END
GO

-- =============================================
-- Aggiorna record esistenti con valori di default
-- =============================================

UPDATE [dbo].[AIConfigurations]
SET 
    [OllamaEndpoint] = 'http://localhost:11434',
    [OllamaChatModel] = 'llama3',
    [OllamaEmbeddingModel] = 'nomic-embed-text',
    [GroqEndpoint] = 'https://api.groq.com/openai/v1',
    [GroqChatModel] = 'llama-3.1-8b-instant'
WHERE 
    [OllamaEndpoint] IS NULL 
    OR [OllamaChatModel] IS NULL
    OR [OllamaEmbeddingModel] IS NULL
    OR [GroqEndpoint] IS NULL
    OR [GroqChatModel] IS NULL;
GO

PRINT ''
PRINT '========================================='
PRINT 'Aggiornamento completato con successo!'
PRINT '========================================='
PRINT ''
PRINT 'La tabella AIConfigurations ora supporta:'
PRINT '  ✓ Ollama (modelli AI locali)'
PRINT '    - Endpoint: http://localhost:11434'
PRINT '    - Chat Model: llama3'
PRINT '    - Embedding Model: nomic-embed-text'
PRINT ''
PRINT '  ✓ Groq (API cloud veloce)'
PRINT '    - Endpoint: https://api.groq.com/openai/v1'
PRINT '    - Chat Model: llama-3.1-8b-instant'
PRINT '    - Nota: Groq non supporta embeddings'
PRINT ''
PRINT 'Provider disponibili (enum ProviderType):'
PRINT '  0 = AzureOpenAI'
PRINT '  1 = OpenAI'
PRINT '  2 = Gemini'
PRINT '  3 = Ollama'
PRINT '  4 = Groq'
PRINT ''
PRINT 'Per ulteriori informazioni consultare:'
PRINT '  - GUIDA_OLLAMA_LOCALE.md (installazione locale)'
PRINT '  - GUIDA_OLLAMA_COLAB.md (Google Colab gratis)'
PRINT '  - GUIDA_GROQ.md (setup API Groq)'
PRINT ''
GO
