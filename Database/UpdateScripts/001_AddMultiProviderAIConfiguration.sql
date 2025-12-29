-- =============================================
-- Script per aggiornamento Database Esistente
-- Aggiunge supporto multi-provider AI
-- Data: 2024-12-28
-- =============================================

USE [DocumentArchive]
GO

PRINT 'Inizio aggiornamento tabella AIConfigurations per supporto multi-provider...'
GO

-- Aggiungi nuove colonne alla tabella AIConfigurations
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'ProviderType')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [ProviderType] int NOT NULL DEFAULT 1; -- Default: Gemini
    PRINT 'Colonna ProviderType aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'ProviderEndpoint')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [ProviderEndpoint] nvarchar(max) NULL;
    PRINT 'Colonna ProviderEndpoint aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'ProviderApiKey')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [ProviderApiKey] nvarchar(max) NULL;
    PRINT 'Colonna ProviderApiKey aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'ChatModelName')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [ChatModelName] nvarchar(max) NULL;
    PRINT 'Colonna ChatModelName aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'EmbeddingModelName')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [EmbeddingModelName] nvarchar(max) NULL;
    PRINT 'Colonna EmbeddingModelName aggiunta'
END
GO

-- Service-specific provider assignments
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'ChatProvider')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [ChatProvider] int NULL;
    PRINT 'Colonna ChatProvider aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'EmbeddingsProvider')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [EmbeddingsProvider] int NULL;
    PRINT 'Colonna EmbeddingsProvider aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'TagExtractionProvider')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [TagExtractionProvider] int NULL;
    PRINT 'Colonna TagExtractionProvider aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'RAGProvider')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [RAGProvider] int NULL;
    PRINT 'Colonna RAGProvider aggiunta'
END
GO

-- Gemini Settings
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'GeminiApiKey')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [GeminiApiKey] nvarchar(max) NULL;
    PRINT 'Colonna GeminiApiKey aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'GeminiChatModel')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [GeminiChatModel] nvarchar(max) NULL;
    PRINT 'Colonna GeminiChatModel aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'GeminiEmbeddingModel')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [GeminiEmbeddingModel] nvarchar(max) NULL;
    PRINT 'Colonna GeminiEmbeddingModel aggiunta'
END
GO

-- OpenAI Settings
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'OpenAIApiKey')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [OpenAIApiKey] nvarchar(max) NULL;
    PRINT 'Colonna OpenAIApiKey aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'OpenAIChatModel')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [OpenAIChatModel] nvarchar(max) NULL;
    PRINT 'Colonna OpenAIChatModel aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'OpenAIEmbeddingModel')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [OpenAIEmbeddingModel] nvarchar(max) NULL;
    PRINT 'Colonna OpenAIEmbeddingModel aggiunta'
END
GO

-- Azure OpenAI additional settings
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'AzureOpenAIChatModel')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [AzureOpenAIChatModel] nvarchar(max) NULL;
    PRINT 'Colonna AzureOpenAIChatModel aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'AzureOpenAIEmbeddingModel')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [AzureOpenAIEmbeddingModel] nvarchar(max) NULL;
    PRINT 'Colonna AzureOpenAIEmbeddingModel aggiunta'
END
GO

-- Chunking Configuration
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'EnableChunking')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [EnableChunking] bit NOT NULL DEFAULT 1; -- Default: enabled
    PRINT 'Colonna EnableChunking aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'ChunkSize')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [ChunkSize] int NOT NULL DEFAULT 1000;
    PRINT 'Colonna ChunkSize aggiunta'
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'ChunkOverlap')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [ChunkOverlap] int NOT NULL DEFAULT 200;
    PRINT 'Colonna ChunkOverlap aggiunta'
END
GO

-- Enable fallback
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') AND name = 'EnableFallback')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD [EnableFallback] bit NOT NULL DEFAULT 1; -- Default: enabled
    PRINT 'Colonna EnableFallback aggiunta'
END
GO

-- Aggiorna record esistenti con valori di default
UPDATE [dbo].[AIConfigurations]
SET 
    [GeminiChatModel] = 'gemini-2.0-flash-exp',
    [GeminiEmbeddingModel] = 'text-embedding-004',
    [OpenAIChatModel] = 'gpt-4',
    [OpenAIEmbeddingModel] = 'text-embedding-ada-002',
    [AzureOpenAIChatModel] = 'gpt-4',
    [AzureOpenAIEmbeddingModel] = 'text-embedding-ada-002'
WHERE 
    [GeminiChatModel] IS NULL 
    OR [GeminiEmbeddingModel] IS NULL
    OR [OpenAIChatModel] IS NULL
    OR [OpenAIEmbeddingModel] IS NULL
    OR [AzureOpenAIChatModel] IS NULL
    OR [AzureOpenAIEmbeddingModel] IS NULL;
GO

PRINT 'Aggiornamento completato con successo!'
PRINT 'La tabella AIConfigurations ora supporta configurazioni multi-provider per:'
PRINT '  - Gemini'
PRINT '  - OpenAI'
PRINT '  - Azure OpenAI'
PRINT '  - Configurazione specifica per servizio (Chat, Embeddings, Tag Extraction, RAG)'
PRINT '  - Supporto chunking per documenti'
GO
