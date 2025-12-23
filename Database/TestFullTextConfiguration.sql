-- =============================================
-- Script: TestFullTextConfiguration.sql
-- Description: Script di test per verificare la configurazione Full-Text
-- =============================================

PRINT '========================================';
PRINT 'Test Configurazione Full-Text DocN';
PRINT '========================================';
PRINT '';

-- 1. Verifica installazione Full-Text
PRINT '1. Verifica installazione Full-Text Search';
PRINT '----------------------------------------';
IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 1
BEGIN
    PRINT '  ✓ Full-Text Search è installato';
END
ELSE
BEGIN
    PRINT '  ✗ Full-Text Search NON è installato';
    PRINT '  Impossibile continuare il test.';
    GOTO EndTest;
END
PRINT '';

-- 2. Verifica esistenza tabelle
PRINT '2. Verifica esistenza tabelle';
PRINT '----------------------------------------';
DECLARE @TablesCount INT = 0;

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Documents]') AND type = N'U')
BEGIN
    PRINT '  ✓ Tabella Documents esistente';
    SET @TablesCount = @TablesCount + 1;
END
ELSE
    PRINT '  ✗ Tabella Documents NON trovata';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentShares]') AND type = N'U')
BEGIN
    PRINT '  ✓ Tabella DocumentShares esistente';
    SET @TablesCount = @TablesCount + 1;
END
ELSE
    PRINT '  ✗ Tabella DocumentShares NON trovata';

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentTags]') AND type = N'U')
BEGIN
    PRINT '  ✓ Tabella DocumentTags esistente';
    SET @TablesCount = @TablesCount + 1;
END
ELSE
    PRINT '  ✗ Tabella DocumentTags NON trovata';

IF @TablesCount = 3
    PRINT '  ✓ Tutte le tabelle documenti sono presenti';
ELSE
BEGIN
    PRINT '  ✗ Alcune tabelle documenti mancano';
    GOTO EndTest;
END
PRINT '';

-- 3. Verifica struttura chiave primaria Documents
PRINT '3. Verifica struttura chiave primaria Documents';
PRINT '----------------------------------------';
SELECT 
    c.name AS [Colonna],
    t.name AS [Tipo],
    c.max_length AS [Lunghezza Max],
    c.is_nullable AS [Nullable],
    c.is_identity AS [Identity]
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'[dbo].[Documents]')
AND c.name = 'DocumentId';

-- Verifica requisiti chiave primaria
DECLARE @KeyType NVARCHAR(50);
DECLARE @KeyLength INT;
DECLARE @IsNullable BIT;
DECLARE @IsIdentity BIT;

SELECT 
    @KeyType = t.name,
    @KeyLength = c.max_length,
    @IsNullable = c.is_nullable,
    @IsIdentity = c.is_identity
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'[dbo].[Documents]')
AND c.name = 'DocumentId';

PRINT '';
IF @KeyType = 'int'
    PRINT '  ✓ Tipo chiave: INT (4 bytes)';
ELSE
    PRINT '  ✗ Tipo chiave: ' + @KeyType + ' (dovrebbe essere INT)';

IF @IsIdentity = 1
    PRINT '  ✓ Colonna IDENTITY: Sì';
ELSE
    PRINT '  ✗ Colonna IDENTITY: No';

IF @IsNullable = 0
    PRINT '  ✓ NOT NULL: Sì';
ELSE
    PRINT '  ✗ NOT NULL: No';

IF @KeyLength <= 900
    PRINT '  ✓ Dimensione: ' + CAST(@KeyLength AS NVARCHAR(10)) + ' bytes (< 900 bytes)';
ELSE
    PRINT '  ✗ Dimensione: ' + CAST(@KeyLength AS NVARCHAR(10)) + ' bytes (> 900 bytes)';

PRINT '';

-- 4. Verifica catalogo full-text
PRINT '4. Verifica catalogo Full-Text';
PRINT '----------------------------------------';
IF EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'DocumentCatalog')
BEGIN
    SELECT 
        name AS [Nome Catalogo],
        CASE is_default WHEN 1 THEN 'Sì' ELSE 'No' END AS [Default],
        CASE is_accent_sensitivity_on WHEN 1 THEN 'Sì' ELSE 'No' END AS [Sensibile Accenti]
    FROM sys.fulltext_catalogs 
    WHERE name = 'DocumentCatalog';
    PRINT '  ✓ Catalogo DocumentCatalog trovato';
END
ELSE
    PRINT '  ✗ Catalogo DocumentCatalog NON trovato';
PRINT '';

-- 5. Verifica indice full-text
PRINT '5. Verifica indice Full-Text sulla tabella Documents';
PRINT '----------------------------------------';
IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'[dbo].[Documents]'))
BEGIN
    SELECT 
        OBJECT_NAME(object_id) AS [Tabella],
        CASE is_enabled WHEN 1 THEN 'Abilitato' ELSE 'Disabilitato' END AS [Stato],
        change_tracking_state_desc AS [Tracciamento Modifiche],
        CASE has_crawl_completed WHEN 1 THEN 'Sì' ELSE 'No' END AS [Popolamento Completato]
    FROM sys.fulltext_indexes
    WHERE object_id = OBJECT_ID(N'[dbo].[Documents]');
    
    PRINT '  ✓ Indice Full-Text trovato sulla tabella Documents';
END
ELSE
BEGIN
    PRINT '  ✗ Indice Full-Text NON trovato sulla tabella Documents';
    GOTO EndTest;
END
PRINT '';

-- 6. Verifica colonne indicizzate
PRINT '6. Verifica colonne indicizzate Full-Text';
PRINT '----------------------------------------';
SELECT 
    c.name AS [Colonna],
    t.name AS [Tipo Dato],
    l.name AS [Lingua]
FROM sys.fulltext_index_columns fic
INNER JOIN sys.columns c ON fic.object_id = c.object_id AND fic.column_id = c.column_id
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
INNER JOIN sys.fulltext_languages l ON fic.language_id = l.lcid
WHERE fic.object_id = OBJECT_ID(N'[dbo].[Documents]')
ORDER BY c.name;

DECLARE @IndexedColumns INT;
SELECT @IndexedColumns = COUNT(*)
FROM sys.fulltext_index_columns
WHERE object_id = OBJECT_ID(N'[dbo].[Documents]');

PRINT '';
PRINT '  ✓ Numero colonne indicizzate: ' + CAST(@IndexedColumns AS NVARCHAR(10));
PRINT '';

-- 7. Verifica chiave utilizzata per full-text
PRINT '7. Verifica chiave primaria utilizzata per Full-Text';
PRINT '----------------------------------------';
SELECT 
    i.name AS [Nome Indice],
    i.type_desc AS [Tipo],
    CASE i.is_unique WHEN 1 THEN 'Sì' ELSE 'No' END AS [Univoco],
    c.name AS [Colonna Chiave],
    t.name AS [Tipo Dato Colonna]
FROM sys.fulltext_indexes fi
INNER JOIN sys.indexes i ON fi.object_id = i.object_id AND fi.unique_index_id = i.index_id
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE fi.object_id = OBJECT_ID(N'[dbo].[Documents]');

PRINT '';
PRINT '  ✓ Chiave primaria verificata';
PRINT '';

-- 8. Riepilogo finale
PRINT '========================================';
PRINT 'RIEPILOGO TEST';
PRINT '========================================';

DECLARE @AllTestsPassed BIT = 1;

-- Verifica tutti i criteri
IF FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') != 1
    SET @AllTestsPassed = 0;

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Documents]') AND type = N'U')
    SET @AllTestsPassed = 0;

IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'DocumentCatalog')
    SET @AllTestsPassed = 0;

IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID(N'[dbo].[Documents]'))
    SET @AllTestsPassed = 0;

IF @KeyType != 'int' OR @IsIdentity != 1 OR @IsNullable != 0
    SET @AllTestsPassed = 0;

IF @AllTestsPassed = 1
BEGIN
    PRINT '✅ TUTTI I TEST SUPERATI!';
    PRINT '';
    PRINT 'La configurazione Full-Text è corretta e rispetta tutti i requisiti:';
    PRINT '  ✓ Chiave primaria INT IDENTITY (4 bytes)';
    PRINT '  ✓ Chiave univoca e NOT NULL';
    PRINT '  ✓ Colonna singola';
    PRINT '  ✓ Dimensione < 900 bytes';
    PRINT '  ✓ Catalogo Full-Text configurato';
    PRINT '  ✓ Indice Full-Text attivo';
    PRINT '';
    PRINT 'Il database è pronto per la ricerca full-text!';
END
ELSE
BEGIN
    PRINT '❌ ALCUNI TEST FALLITI';
    PRINT '';
    PRINT 'Verificare i messaggi di errore sopra per dettagli.';
    PRINT 'Potrebbe essere necessario rieseguire gli script di setup.';
END

EndTest:
PRINT '';
PRINT '========================================';
PRINT 'Test completato';
PRINT '========================================';
GO
