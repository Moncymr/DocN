-- =============================================
-- Script: 03_ConfigureFullTextSearch.sql
-- Description: Configura il catalogo e l'indice full-text per la ricerca nei documenti
-- =============================================

PRINT '⏳ Configurazione Full-Text Search...';

-- Verifica che Full-Text Search sia installato
IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
BEGIN
    PRINT '  ✓ Full-Text Search è installato';
    
    -- Crea il catalogo full-text se non esiste
    IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'DocumentCatalog')
    BEGIN
        CREATE FULLTEXT CATALOG [DocumentCatalog] AS DEFAULT;
        PRINT '  ✓ Catalogo Full-Text "DocumentCatalog" creato';
    END
    ELSE
        PRINT '    Catalogo Full-Text "DocumentCatalog" già esistente';
    
    -- Verifica che la tabella Documents esista
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Documents]') AND type in (N'U'))
    BEGIN
        -- Rimuovi l'indice full-text esistente se presente (per ricreare)
        IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'[dbo].[Documents]'))
        BEGIN
            DROP FULLTEXT INDEX ON [dbo].[Documents];
            PRINT '    Indice Full-Text esistente rimosso per riconfigurazione';
        END
        
        -- Crea l'indice full-text sulla tabella Documents
        -- IMPORTANTE: La chiave primaria DEVE essere:
        -- - Un indice univoco (UNIQUE)
        -- - Non nullable (NOT NULL)
        -- - A colonna singola
        -- - Dimensione massima 900 bytes
        -- - Non può essere su colonne calcolate non deterministiche
        -- 
        -- La nostra chiave primaria PK_Documents su DocumentId (INT IDENTITY) soddisfa tutti i requisiti:
        -- - INT è 4 bytes (< 900 bytes) ✓
        -- - È UNIQUE per definizione ✓
        -- - È NOT NULL (IDENTITY implica NOT NULL) ✓
        -- - È a colonna singola ✓
        -- - Non è una colonna calcolata ✓
        
        CREATE FULLTEXT INDEX ON [dbo].[Documents]
        (
            [ExtractedText] LANGUAGE 1040, -- 1040 = Italiano
            [Title] LANGUAGE 1040,
            [Description] LANGUAGE 1040,
            [Keywords] LANGUAGE 1040,
            [FileName] LANGUAGE 1040
        )
        KEY INDEX [PK_Documents] -- Usa la chiave primaria corretta (INT IDENTITY)
        ON [DocumentCatalog]
        WITH (
            CHANGE_TRACKING = AUTO,
            STOPLIST = SYSTEM
        );
        
        PRINT '  ✓ Indice Full-Text creato sulla tabella Documents';
        PRINT '    - Colonne indicizzate: ExtractedText, Title, Description, Keywords, FileName';
        PRINT '    - Chiave primaria utilizzata: PK_Documents (DocumentId INT)';
        PRINT '    - Lingua: Italiano (1040)';
    END
    ELSE
    BEGIN
        PRINT '  ✗ ERRORE: Tabella Documents non trovata. Eseguire prima 02_CreateDocumentTables.sql';
    END
END
ELSE
BEGIN
    PRINT '  ✗ AVVERTENZA: Full-Text Search non è installato su questa istanza di SQL Server';
    PRINT '    Per installare Full-Text Search, eseguire il setup di SQL Server e selezionare';
    PRINT '    la funzionalità "Full-Text and Semantic Extractions for Search"';
END

GO

-- Query di test per verificare la configurazione full-text
PRINT '';
PRINT '📊 Verifica configurazione Full-Text:';
PRINT '----------------------------------------';

-- Mostra i cataloghi full-text
SELECT 
    'Cataloghi Full-Text' AS [Tipo],
    name AS [Nome],
    CASE is_default WHEN 1 THEN 'Sì' ELSE 'No' END AS [Default]
FROM sys.fulltext_catalogs;

-- Mostra gli indici full-text
SELECT 
    'Indici Full-Text' AS [Tipo],
    OBJECT_NAME(object_id) AS [Tabella],
    CASE is_enabled WHEN 1 THEN 'Abilitato' ELSE 'Disabilitato' END AS [Stato],
    change_tracking_state_desc AS [Tracciamento modifiche]
FROM sys.fulltext_indexes
WHERE object_id = OBJECT_ID(N'[dbo].[Documents]');

-- Mostra le colonne indicizzate
SELECT 
    'Colonne Full-Text' AS [Tipo],
    c.name AS [Colonna],
    l.name AS [Lingua]
FROM sys.fulltext_index_columns fic
INNER JOIN sys.columns c ON fic.object_id = c.object_id AND fic.column_id = c.column_id
INNER JOIN sys.fulltext_languages l ON fic.language_id = l.lcid
WHERE fic.object_id = OBJECT_ID(N'[dbo].[Documents]');

PRINT '----------------------------------------';
PRINT '✓ Configurazione Full-Text Search completata';

GO
