# Database Setup per DocN

Questo directory contiene gli script SQL per creare e configurare il database DocN con supporto per Full-Text Search.

## 🔧 Soluzione al Problema Full-Text Index

### Problema Originale
L'errore SQL Server 7653 si verificava perché la chiave primaria utilizzata per l'indice full-text non soddisfaceva i requisiti:

```
Messaggio 7653, livello 16, stato 1, riga 240
'PK__Document__3214EC074A8DB6ED' non è un indice valido per l'applicazione di una chiave di ricerca full-text.
```

### Requisiti Chiave Full-Text
Una chiave primaria per full-text search deve essere:
1. ✅ Un indice **univoco** (UNIQUE)
2. ✅ **Non ammette valori NULL** (NOT NULL)
3. ✅ Costituita da una **singola colonna**
4. ✅ Non definita su colonna **calcolata non deterministica** o **non persistente imprecisa**
5. ✅ Avere dimensioni massime di **900 byte**

### Soluzione Implementata
La tabella `Documents` ora utilizza una chiave primaria corretta:

```sql
[DocumentId] INT IDENTITY(1,1) NOT NULL
CONSTRAINT [PK_Documents] PRIMARY KEY CLUSTERED ([DocumentId] ASC)
```

Questa chiave soddisfa tutti i requisiti:
- ✅ `INT` occupa solo **4 bytes** (molto meno di 900 bytes)
- ✅ È **UNIQUE** per definizione (PRIMARY KEY)
- ✅ È **NOT NULL** (IDENTITY implica NOT NULL)
- ✅ È una **colonna singola**
- ✅ Non è una **colonna calcolata**

## 📁 Script Disponibili

### 1. `01_CreateIdentityTables.sql`
Crea tutte le tabelle necessarie per ASP.NET Core Identity:
- AspNetRoles
- AspNetUsers
- AspNetUserClaims
- AspNetUserLogins
- AspNetUserTokens
- AspNetUserRoles
- AspNetRoleClaims

### 2. `02_CreateDocumentTables.sql`
Crea le tabelle per la gestione dei documenti:
- **Documents**: Tabella principale con la chiave primaria corretta per full-text
  - `DocumentId` (INT IDENTITY): Chiave primaria ottimizzata per full-text
  - Campi per metadati, contenuto estratto, embedding vettoriali
  - Foreign keys verso AspNetUsers
- **DocumentShares**: Condivisione documenti tra utenti
- **DocumentTags**: Tag per categorizzazione documenti

### 3. `03_ConfigureFullTextSearch.sql`
Configura il Full-Text Search:
- Crea il catalogo `DocumentCatalog`
- Crea l'indice full-text sulla tabella `Documents`
- Indicizza le colonne: ExtractedText, Title, Description, Keywords, FileName
- Utilizza la lingua italiana (LCID 1040)
- Verifica la configurazione

### 4. `SetupDatabase.sql`
Script master che esegue tutti gli script nell'ordine corretto.

## 🚀 Come Eseguire

### Opzione 1: Script Master (Raccomandato)
```bash
sqlcmd -S <server> -d <database> -i SetupDatabase.sql
```

### Opzione 2: Script Individuali
```bash
sqlcmd -S <server> -d <database> -i 01_CreateIdentityTables.sql
sqlcmd -S <server> -d <database> -i 02_CreateDocumentTables.sql
sqlcmd -S <server> -d <database> -i 03_ConfigureFullTextSearch.sql
```

### Opzione 3: Da SQL Server Management Studio (SSMS)
1. Aprire SSMS e connettersi al server
2. Aprire lo script `SetupDatabase.sql`
3. Modificare la prima riga `USE [DocN]` con il nome del database desiderato
4. Eseguire lo script (F5)

### ⚠️ Nota sulla Sicurezza
Quando si utilizzano gli script helper PowerShell o Bash con autenticazione SQL, la password viene passata come parametro da riga di comando. In ambienti di produzione, considerare l'uso di:
- Windows Authentication (opzione `-w` o `-UseWindowsAuth`)
- Variabili d'ambiente per le credenziali
- File di configurazione sicuri
- Azure AD Authentication

## ⚙️ Prerequisiti

1. **SQL Server 2019 o superiore** (raccomandato SQL Server 2022/2025)
2. **Full-Text Search** deve essere installato:
   - Durante l'installazione di SQL Server, selezionare la funzionalità
   - "Full-Text and Semantic Extractions for Search"
3. **Database esistente** dove eseguire gli script
4. **Permessi appropriati**:
   - CREATE TABLE
   - CREATE FULLTEXT CATALOG
   - CREATE FULLTEXT INDEX

## 🔍 Verifica Installazione Full-Text

Per verificare se Full-Text Search è installato:

```sql
SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled');
-- Risultato: 1 = Installato, 0 = Non installato
```

## 📊 Query di Esempio

### Ricerca Full-Text nei Documenti
```sql
-- Ricerca semplice
SELECT DocumentId, Title, FileName
FROM Documents
WHERE CONTAINS(ExtractedText, 'parola chiave');

-- Ricerca con ranking
SELECT DocumentId, Title, RANK
FROM Documents
INNER JOIN CONTAINSTABLE(Documents, ExtractedText, 'parola chiave') AS FT
    ON Documents.DocumentId = FT.[KEY]
ORDER BY RANK DESC;

-- Ricerca su più colonne
SELECT DocumentId, Title, FileName, Description
FROM Documents
WHERE CONTAINS((Title, ExtractedText, Description), 'parola chiave');
```

### Verifica Stato Indice Full-Text
```sql
-- Verifica popolamento indice
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    FULLTEXTCATALOGPROPERTY('DocumentCatalog', 'PopulateStatus') AS PopulateStatus,
    FULLTEXTCATALOGPROPERTY('DocumentCatalog', 'ItemCount') AS ItemCount
FROM sys.fulltext_indexes
WHERE object_id = OBJECT_ID('Documents');

-- 0 = Inattivo, 1 = Popolamento in corso, altri valori = Completo
```

## 🏗️ Struttura Tabella Documents

### Campi Principali
- **DocumentId**: Chiave primaria (INT IDENTITY) - ottimizzata per full-text
- **FileName**: Nome file sul server
- **OriginalFileName**: Nome file originale dell'utente
- **FilePath**: Percorso file sul server
- **FileSize**: Dimensione in bytes
- **MimeType**: Tipo MIME del file
- **FileHash**: Hash SHA-256 per rilevamento duplicati

### Campi per Ricerca
- **ExtractedText**: Testo estratto dal documento (NVARCHAR(MAX))
- **Title**: Titolo documento (NVARCHAR(500))
- **Description**: Descrizione (NVARCHAR(MAX))
- **Category**: Categoria suggerita da AI (NVARCHAR(100))
- **Keywords**: Parole chiave (NVARCHAR(500))

### Campi AI/Embedding
- **TextEmbedding**: Vettore embedding (VARBINARY(MAX))
- **EmbeddingModel**: Modello utilizzato (es. "text-embedding-ada-002")

### Metadati
- **UploadedBy**: ID utente che ha caricato
- **UploadedDate**: Data caricamento
- **ModifiedBy**: ID utente ultima modifica
- **ModifiedDate**: Data ultima modifica
- **IsActive**: Flag attivo/disattivo
- **IsPublic**: Flag pubblico/privato

## 🔒 Sicurezza

- Tutte le tabelle utilizzano **foreign keys** con integrità referenziale
- Le cancellazioni utilizzano **CASCADE** dove appropriato
- Gli indici sono ottimizzati per le query più comuni
- I campi sensibili utilizzano **NVARCHAR(MAX)** per supportare Unicode

## 📝 Note

1. Gli script sono **idempotenti**: possono essere eseguiti più volte senza errori
2. Se una tabella esiste già, viene saltata (non sovrascritta)
3. L'indice full-text viene ricreato se esiste già (per riconfigurazione)
4. Tutti i messaggi di output utilizzano caratteri Unicode (✓, ✗, ⏳, 📋, etc.)

## 🐛 Troubleshooting

### Errore: Full-Text Search non installato
**Soluzione**: Installare Full-Text Search tramite SQL Server Setup

### Errore: Chiave primaria non valida per full-text
**Soluzione**: Verificare che la chiave primaria sia INT, BIGINT o UNIQUEIDENTIFIER (non NVARCHAR)

### Errore: Permessi insufficienti
**Soluzione**: Eseguire come utente con privilegi `db_owner` o `sysadmin`

## 📚 Riferimenti

- [SQL Server Full-Text Search](https://learn.microsoft.com/en-us/sql/relational-databases/search/full-text-search)
- [CREATE FULLTEXT INDEX](https://learn.microsoft.com/en-us/sql/t-sql/statements/create-fulltext-index-transact-sql)
- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)

## 📧 Supporto

Per problemi o domande, aprire un issue nel repository GitHub.
