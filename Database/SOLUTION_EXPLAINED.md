# Risoluzione Errore Full-Text Index SQL Server 7653

## 📋 Problema Originale

Durante la creazione delle tabelle del database DocN, si verificava il seguente errore:

```
Messaggio 7653, livello 16, stato 1, riga 240
'PK__Document__3214EC074A8DB6ED' non è un indice valido per l'applicazione 
di una chiave di ricerca full-text. Una chiave di ricerca full-text deve essere 
un indice univoco che non ammette i valori Null, costituito da una singola colonna 
che non sia offline, non definito in una colonna calcolata non deterministica 
o in una colonna calcolata non persistente imprecisa e deve avere dimensioni 
massime di 900 byte. Scegliere un altro indice per la chiave full-text.
```

## 🔍 Analisi del Problema

L'errore SQL Server 7653 indica che la chiave primaria utilizzata per creare un indice full-text non soddisfa i requisiti di SQL Server. Analizzando il nome della chiave `PK__Document__3214EC074A8DB6ED`, si può dedurre che:

1. Il nome con doppio underscore e hash casuale indica una chiave generata automaticamente
2. La struttura della chiave probabilmente non era ottimale per il full-text indexing
3. Possibili cause:
   - Chiave primaria su colonna NVARCHAR troppo lunga (> 900 bytes)
   - Chiave primaria composita (multiple colonne)
   - Chiave primaria su colonna calcolata
   - Chiave primaria nullable

## ✅ Soluzione Implementata

### Requisiti per una Chiave Full-Text Valida

Secondo la documentazione Microsoft, una chiave primaria per full-text search deve soddisfare:

| Requisito | Descrizione | Soluzione Applicata |
|-----------|-------------|---------------------|
| **Univoco** | Indice UNIQUE | ✓ PRIMARY KEY implica UNIQUE |
| **Non Null** | NOT NULL | ✓ IDENTITY implica NOT NULL |
| **Colonna Singola** | Una sola colonna | ✓ Solo `DocumentId` |
| **Dimensione** | Max 900 bytes | ✓ INT = 4 bytes |
| **Non Calcolata** | Nessuna colonna calcolata | ✓ Colonna normale |
| **Non Offline** | Indice online | ✓ Indice standard |

### Struttura della Tabella Documents

```sql
CREATE TABLE [dbo].[Documents] (
    -- Chiave primaria ottimizzata per full-text index
    [DocumentId] INT IDENTITY(1,1) NOT NULL,
    
    -- Campi per contenuto e ricerca
    [ExtractedText] NVARCHAR(MAX) NULL,
    [Title] NVARCHAR(500) NULL,
    [Description] NVARCHAR(MAX) NULL,
    [Category] NVARCHAR(100) NULL,
    [Keywords] NVARCHAR(500) NULL,
    
    -- Altri campi...
    [FileName] NVARCHAR(255) NOT NULL,
    [FilePath] NVARCHAR(500) NOT NULL,
    [FileSize] BIGINT NOT NULL,
    [MimeType] NVARCHAR(100) NOT NULL,
    [UploadedBy] NVARCHAR(450) NOT NULL,
    [UploadedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    -- Chiave primaria su INT IDENTITY
    CONSTRAINT [PK_Documents] PRIMARY KEY CLUSTERED ([DocumentId] ASC)
);
```

### Configurazione Full-Text Index

```sql
-- Crea il catalogo full-text
CREATE FULLTEXT CATALOG [DocumentCatalog] AS DEFAULT;

-- Crea l'indice full-text
CREATE FULLTEXT INDEX ON [dbo].[Documents]
(
    [ExtractedText] LANGUAGE 1040,  -- Italiano
    [Title] LANGUAGE 1040,
    [Description] LANGUAGE 1040,
    [Keywords] LANGUAGE 1040,
    [FileName] LANGUAGE 1040
)
KEY INDEX [PK_Documents]  -- Usa la chiave primaria INT
ON [DocumentCatalog]
WITH (
    CHANGE_TRACKING = AUTO,
    STOPLIST = SYSTEM
);
```

## 🎯 Perché Funziona

### INT IDENTITY come Chiave Primaria

La scelta di `INT IDENTITY(1,1)` come chiave primaria è ideale perché:

1. **Dimensione Minima**: Solo 4 bytes (molto meno dei 900 bytes massimi)
2. **Sequenziale**: Ottimizza l'inserimento e l'indicizzazione
3. **Prestazioni**: Ricerca veloce su chiavi numeriche
4. **Auto-incrementale**: Nessuna gestione manuale richiesta
5. **Standard**: Pattern comune in SQL Server e ASP.NET

### Alternative Considerate

| Tipo Chiave | Dimensione | Pro | Contro | Valido? |
|-------------|-----------|-----|--------|---------|
| INT | 4 bytes | Veloce, standard | Limite ~2.1B record | ✓ SI |
| BIGINT | 8 bytes | Nessun limite pratico | Più spazio | ✓ SI |
| UNIQUEIDENTIFIER | 16 bytes | Distribuito, sicuro | Frammentazione | ✓ SI |
| NVARCHAR(450) | 900 bytes | Leggibile | Lento, spazio | ⚠ LIMITE |
| NVARCHAR(500) | 1000 bytes | - | Troppo grande | ✗ NO |
| Composita | Variabile | Normalizzazione | Complesso | ✗ NO |

## 📊 Vantaggi della Soluzione

### 1. Compatibilità Full-Text
- ✅ Soddisfa tutti i requisiti SQL Server
- ✅ Nessun errore 7653
- ✅ Supporto completo per ricerca full-text

### 2. Prestazioni
- ⚡ Indice numerico veloce
- ⚡ Scansione e join efficienti
- ⚡ Memoria ridotta

### 3. Scalabilità
- 📈 Supporta fino a 2.147.483.647 documenti (INT)
- 📈 Espandibile a BIGINT se necessario
- 📈 Indici ben ottimizzati

### 4. Manutenibilità
- 🔧 Struttura standard e documentata
- 🔧 Facile da capire e mantenere
- 🔧 Compatibile con Entity Framework

## 🚀 Come Usare

### 1. Eseguire gli Script

**Windows (PowerShell):**
```powershell
cd Database
.\RunSetup.ps1 -ServerName "localhost" -DatabaseName "DocN" -UseWindowsAuth
```

**Linux/macOS (Bash):**
```bash
cd Database
chmod +x run_setup.sh
./run_setup.sh -s localhost -d DocN -u sa -p 'YourPassword'
```

**SQL Server Management Studio:**
1. Aprire `SetupDatabase.sql`
2. Modificare `USE [DocN]` con il nome del database
3. Eseguire (F5)

### 2. Verificare la Configurazione

```sql
-- Verifica tabelle
SELECT name FROM sys.tables 
WHERE name IN ('Documents', 'DocumentShares', 'DocumentTags');

-- Verifica catalogo full-text
SELECT * FROM sys.fulltext_catalogs WHERE name = 'DocumentCatalog';

-- Verifica indice full-text
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    is_enabled,
    change_tracking_state_desc
FROM sys.fulltext_indexes
WHERE object_id = OBJECT_ID('Documents');
```

### 3. Testare la Ricerca Full-Text

```sql
-- Inserire un documento di test
INSERT INTO Documents (
    DocumentId, FileName, OriginalFileName, FilePath, FileSize, MimeType,
    ExtractedText, Title, Description, Category, Keywords,
    UploadedBy, UploadedDate, IsActive, IsPublic
)
VALUES (
    1, 'test.pdf', 'Documento Test.pdf', '/uploads/test.pdf', 1024, 'application/pdf',
    'Questo è un documento di test per la ricerca full-text',
    'Documento Test', 'Test della funzionalità di ricerca',
    'Test', 'test, ricerca, full-text',
    'user-id', GETDATE(), 1, 0
);

-- Ricerca full-text
SELECT DocumentId, Title, FileName
FROM Documents
WHERE CONTAINS(ExtractedText, 'ricerca');

-- Ricerca con ranking
SELECT d.DocumentId, d.Title, ft.RANK
FROM Documents d
INNER JOIN CONTAINSTABLE(Documents, ExtractedText, 'ricerca') AS ft
    ON d.DocumentId = ft.[KEY]
ORDER BY ft.RANK DESC;
```

## 📚 Riferimenti

### Documentazione Microsoft
- [Full-Text Search (SQL Server)](https://learn.microsoft.com/en-us/sql/relational-databases/search/full-text-search)
- [CREATE FULLTEXT INDEX](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-fulltext-index-transact-sql)
- [SQL Server Error 7653](https://learn.microsoft.com/en-us/sql/relational-databases/errors-events/database-engine-events-and-errors)

### Best Practices
- [Indexing Best Practices](https://learn.microsoft.com/en-us/sql/relational-databases/sql-server-index-design-guide)
- [Primary Key Design](https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)

## 🔄 Migrazione da Chiavi Esistenti

Se hai già una tabella Documents con una chiave primaria diversa:

```sql
-- 1. Rimuovi l'indice full-text esistente
DROP FULLTEXT INDEX ON Documents;

-- 2. Rimuovi la chiave primaria esistente
ALTER TABLE Documents DROP CONSTRAINT PK_Documents_OLD;

-- 3. Aggiungi nuova colonna INT IDENTITY
ALTER TABLE Documents ADD DocumentId INT IDENTITY(1,1) NOT NULL;

-- 4. Crea nuova chiave primaria
ALTER TABLE Documents ADD CONSTRAINT PK_Documents PRIMARY KEY (DocumentId);

-- 5. Ricrea l'indice full-text
CREATE FULLTEXT INDEX ON Documents
(ExtractedText, Title, Description, Keywords, FileName)
KEY INDEX PK_Documents
ON DocumentCatalog;
```

## ⚠️ Note Importanti

1. **Full-Text Search deve essere installato**
   - Verificare con: `SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')`
   - Se non installato, eseguire SQL Server Setup

2. **Permessi necessari**
   - CREATE TABLE
   - CREATE FULLTEXT CATALOG
   - CREATE FULLTEXT INDEX

3. **Lingua italiana**
   - LCID 1040 per supporto linguistico italiano
   - Modifica se necessario per altre lingue

4. **Popolamento indice**
   - Il popolamento è automatico (CHANGE_TRACKING = AUTO)
   - Potrebbe richiedere tempo per molti documenti

## 💡 Troubleshooting

### Problema: "Full-Text Search non installato"
**Soluzione**: Installare tramite SQL Server Setup

### Problema: "Errore 7653 persiste"
**Soluzione**: Verificare che la chiave primaria sia INT/BIGINT/UNIQUEIDENTIFIER

### Problema: "Ricerca non trova risultati"
**Soluzione**: Attendere popolamento indice o forzare con `ALTER FULLTEXT INDEX ... START FULL POPULATION`

### Problema: "Prestazioni lente"
**Soluzione**: 
- Verificare piano di esecuzione
- Aggiungere indici non-clustered
- Ottimizzare query CONTAINS/CONTAINSTABLE

## ✨ Conclusione

La soluzione implementata risolve definitivamente l'errore SQL Server 7653 utilizzando una chiave primaria INT IDENTITY ottimale per il full-text indexing. La struttura è:

- ✅ **Conforme** ai requisiti SQL Server
- ✅ **Performante** per ricerche e inserimenti
- ✅ **Scalabile** per grandi volumi di dati
- ✅ **Manutenibile** con pattern standard
- ✅ **Completa** con tutte le tabelle necessarie

Il database è ora pronto per l'archiviazione e la ricerca intelligente di documenti con supporto full-text in italiano.
