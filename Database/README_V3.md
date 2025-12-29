# CreateDatabase_Complete_V3.sql - Documentazione

## Descrizione

Questo è lo script SQL completo e consolidato per creare il database DocN dalla versione 3.0. Include tutte le tabelle, indici, stored procedures, views e dati iniziali necessari per far funzionare l'applicazione DocN con tutte le funzionalità più recenti.

## Cosa Contiene V3

### 🆕 Novità rispetto a V2

1. **Multi-Provider AI Configuration**
   - Supporto per Gemini, OpenAI e Azure OpenAI
   - Configurazione specifica per ogni servizio (Chat, Embeddings, Tag Extraction, RAG)
   - Possibilità di scegliere provider diversi per servizi diversi
   - Supporto fallback tra provider

2. **Tabella SimilarDocuments**
   - Tracking delle similarità vettoriali tra documenti
   - Top 5 documenti più simili per ogni documento
   - Score di similarità e chunk rilevanti
   - Storicizzazione delle analisi con timestamp

3. **Tabella LogEntries**
   - Sistema di logging centralizzato
   - Categorizzazione dei log (Info, Warning, Error, ecc.)
   - Tracking per utente e file
   - Stack trace per errori

4. **Modello Gemini Aggiornato**
   - Aggiornato da `gemini-1.5-flash` (deprecato) a `gemini-2.0-flash-exp`
   - Compatibile con i nuovi account Google AI

5. **OwnerId Foreign Key Fix**
   - Vincolo FK modificato da `ON DELETE CASCADE` a `ON DELETE SET NULL`
   - Permette documenti senza proprietario (documenti pubblici/anonimi)
   - Maggiore sicurezza nella gestione degli utenti

6. **ExtractedMetadataJson**
   - Nuovo campo nella tabella Documents
   - Memorizza metadata strutturati estratti dall'AI
   - Esempi: numeri fattura, date, autori, numeri contratto, ecc.
   - Formato JSON per flessibilità

7. **Chunking Configuration**
   - Configurazione per dimensioni chunk e overlap
   - Opzioni per abilitare/disabilitare chunking
   - Ottimizzazione per RAG preciso

### 📊 Tabelle Create (18 totali)

**Identity & Authentication (7 tabelle):**
- AspNetRoles
- AspNetUsers
- AspNetUserClaims
- AspNetUserLogins
- AspNetUserRoles
- AspNetUserTokens
- AspNetRoleClaims
- Tenants

**Documenti (5 tabelle):**
- Documents (con ExtractedMetadataJson V3)
- DocumentShares
- DocumentTags
- DocumentChunks
- SimilarDocuments (V3 nuova)

**Conversazioni (2 tabelle):**
- Conversations
- Messages

**Configurazione (1 tabella):**
- AIConfigurations (V3 con multi-provider)

**Audit & Logging (2 tabelle):**
- AuditLogs
- LogEntries (V3 nuova)

### 🔧 Stored Procedures (6 totali)

**Manutenzione:**
1. `sp_CleanupOldAuditLogs` - Pulizia audit logs vecchi
2. `sp_CleanupOldLogEntries` - Pulizia log entries vecchi (V3 nuova)
3. `sp_GetDashboardStatistics` - Statistiche dashboard

**RAG & Search:**
4. `sp_HybridSearch` - Ricerca ibrida (vector + full-text)
5. `sp_VectorSearch` - Ricerca semantica vettoriale
6. `sp_RetrieveRAGContext` - Recupero contesto per RAG

### 👁️ Views (2 totali)

1. `vw_DocumentStatistics` - Statistiche documenti
2. `vw_UserActivity` - Attività utenti

## Come Usare

### 1. Prerequisiti

- SQL Server 2025 (o versione compatibile con tipo VECTOR)
- SQL Server Management Studio (SSMS) o Azure Data Studio
- Permessi di creazione database

### 2. Esecuzione Script

```sql
-- Opzione 1: Eseguire tutto lo script in una volta
-- Aprire il file in SSMS e premere F5

-- Opzione 2: Eseguire da linea di comando
sqlcmd -S <server_name> -i "CreateDatabase_Complete_V3.sql"
```

### 3. Verifica Installazione

Dopo l'esecuzione, verificare che:

```sql
USE DocNDb;

-- Verificare tabelle
SELECT COUNT(*) AS TabelleCreate 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_CATALOG = 'DocNDb';
-- Risultato atteso: 18+

-- Verificare stored procedures
SELECT COUNT(*) AS StoredProcedures 
FROM sys.procedures;
-- Risultato atteso: 6

-- Verificare views
SELECT COUNT(*) AS Views 
FROM sys.views 
WHERE name LIKE 'vw_%';
-- Risultato atteso: 2

-- Verificare tenant predefinito
SELECT * FROM Tenants WHERE Name = 'Default';

-- Verificare utente admin
SELECT * FROM AspNetUsers WHERE Email = 'admin@docn.local';

-- Verificare configurazione AI
SELECT * FROM AIConfigurations WHERE ConfigurationName LIKE '%Multi-Provider%';
```

### 4. Credenziali Predefinite

**Utente Amministratore:**
- Email: `admin@docn.local`
- Password: `Admin@123`

⚠️ **IMPORTANTE:** Cambiare questa password dopo il primo login!

### 5. Configurazione Applicazione

Aggiornare `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=DocNDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-openai-instance.openai.azure.com/",
    "Key": "your-api-key-here",
    "EmbeddingDeployment": "text-embedding-ada-002",
    "ChatDeployment": "gpt-4"
  },
  "Gemini": {
    "ApiKey": "your-gemini-api-key-here",
    "ChatModel": "gemini-2.0-flash-exp",
    "EmbeddingModel": "text-embedding-004"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here",
    "ChatModel": "gpt-4",
    "EmbeddingModel": "text-embedding-ada-002"
  }
}
```

## Differenze rispetto alle Versioni Precedenti

### Da V1 a V2
- Aggiunto supporto multi-tenant
- Aggiunto chunking documenti
- Aggiunto AI tag analysis
- Migliorati indici per performance

### Da V2 a V3
- ✅ Multi-provider AI (Gemini + OpenAI + Azure)
- ✅ Tabella SimilarDocuments
- ✅ Tabella LogEntries
- ✅ ExtractedMetadataJson in Documents
- ✅ Gemini 2.0 flash exp (non più 1.5)
- ✅ OwnerId nullable con ON DELETE SET NULL
- ✅ Chunking configuration
- ✅ Fallback configuration

## Migrazione da Versioni Precedenti

### Da V2 a V3

Se hai già un database V2, puoi applicare solo gli aggiornamenti:

1. `Database/UpdateScripts/001_AddMultiProviderAIConfiguration.sql`
2. `Database/UpdateScripts/002_AddSimilarDocumentsTable.sql`
3. `Database/UpdateScripts/003_AddLogEntriesTable.sql`
4. `Database/UpdateScripts/004_UpdateGeminiDefaultModel.sql`
5. `Database/UpdateScripts/005_FixOwnerIdForeignKeyConstraint.sql`
6. `Database/UpdateScripts/AddExtractedMetadataJson.sql`

### Database Fresco

Se stai creando un nuovo database, usa direttamente:
- `CreateDatabase_Complete_V3.sql` (questo file)

## Supporto e Troubleshooting

### Problema: Full-text search non funziona

```sql
-- Verificare che il catalogo esista
SELECT * FROM sys.fulltext_catalogs WHERE name = 'DocumentFullTextCatalog';

-- Verificare che l'indice full-text sia attivo
SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Documents');

-- Se necessario, ricreare l'indice
DROP FULLTEXT INDEX ON Documents;
CREATE FULLTEXT INDEX ON Documents(ExtractedText, FileName)
    KEY INDEX PK__Documents__3214EC07 ON DocumentFullTextCatalog;
```

### Problema: Tipo VECTOR non supportato

Se SQL Server non supporta il tipo VECTOR:

```sql
-- Usare VARBINARY(MAX) come alternativa
ALTER TABLE Documents ALTER COLUMN EmbeddingVector VARBINARY(MAX);
ALTER TABLE DocumentChunks ALTER COLUMN ChunkEmbedding VARBINARY(MAX);
```

### Problema: Performance lente

```sql
-- Ricostruire gli indici
ALTER INDEX ALL ON Documents REBUILD;
ALTER INDEX ALL ON DocumentChunks REBUILD;
ALTER INDEX ALL ON AuditLogs REBUILD;

-- Aggiornare le statistiche
UPDATE STATISTICS Documents;
UPDATE STATISTICS DocumentChunks;
UPDATE STATISTICS AuditLogs;
```

## Manutenzione Consigliata

### Pulizia Periodica

```sql
-- Eseguire mensilmente
EXEC sp_CleanupOldAuditLogs @RetentionDays = 90;
EXEC sp_CleanupOldLogEntries @RetentionDays = 30;
```

### Backup

```sql
-- Backup completo
BACKUP DATABASE DocNDb 
TO DISK = 'C:\Backups\DocNDb_Full.bak' 
WITH INIT, COMPRESSION;

-- Backup differenziale
BACKUP DATABASE DocNDb 
TO DISK = 'C:\Backups\DocNDb_Diff.bak' 
WITH DIFFERENTIAL, COMPRESSION;
```

## Riferimenti

- Documentazione principale: [README.md](../README.md)
- API Documentation: [API_DOCUMENTATION.md](../API_DOCUMENTATION.md)
- Multi-Provider Config: [MULTI_PROVIDER_CONFIG.md](../MULTI_PROVIDER_CONFIG.md)
- Migration Guide: [MIGRATION_GUIDE.md](../MIGRATION_GUIDE.md)

## Changelog V3

### [3.0.0] - 2024-12-29

#### Added
- Multi-provider AI support (Gemini, OpenAI, Azure OpenAI)
- SimilarDocuments table for vector similarity tracking
- LogEntries table for centralized logging
- ExtractedMetadataJson field in Documents table
- Chunking configuration (EnableChunking, ChunkSize, ChunkOverlap)
- Fallback configuration (EnableFallback)
- sp_CleanupOldLogEntries stored procedure

#### Changed
- Updated default Gemini model from gemini-1.5-flash to gemini-2.0-flash-exp
- Changed OwnerId FK constraint from ON DELETE CASCADE to ON DELETE SET NULL
- Made OwnerId nullable in Documents table
- Enhanced AIConfigurations table with provider-specific fields

#### Fixed
- OwnerId foreign key constraint allows documents without owner
- Prevents accidental user deletion cascading to documents

---

**Versione Script:** 3.0  
**Data Rilascio:** 29 Dicembre 2024  
**Compatibilità:** SQL Server 2025+  
**Autore:** DocN Development Team
