# Confronto CreateDatabase V2 vs V3

## Riepilogo Rapido

Il file **CreateDatabase_Complete_V3.sql** è lo script completo e aggiornato che include **TUTTE** le funzionalità di V2 più i seguenti miglioramenti:

## 📊 Tabelle

### V2 (16 tabelle)
1. AspNetRoles
2. AspNetUsers
3. AspNetUserClaims
4. AspNetUserLogins
5. AspNetUserRoles
6. AspNetUserTokens
7. AspNetRoleClaims
8. Tenants
9. Documents
10. DocumentShares
11. DocumentTags
12. DocumentChunks
13. Conversations
14. Messages
15. AIConfigurations
16. AuditLogs

### V3 (18 tabelle = V2 + 2 nuove)
- **Tutte le 16 tabelle di V2**
- ✨ **SimilarDocuments** (nuova) - tracking similarità vettoriale
- ✨ **LogEntries** (nuova) - logging centralizzato

## 🔧 Modifiche alle Tabelle Esistenti

### Documents
```sql
-- V2
OwnerId NVARCHAR(450) NOT NULL,  -- NOT NULL, FK con CASCADE
ExtractedMetadataJson -- NON PRESENTE

-- V3
OwnerId NVARCHAR(450) NULL,  -- ✅ NULLABLE
ExtractedMetadataJson NVARCHAR(MAX) NULL,  -- ✅ NUOVO CAMPO
CONSTRAINT FK_Documents_Owner FOREIGN KEY (OwnerId) 
    REFERENCES AspNetUsers(Id) ON DELETE SET NULL  -- ✅ SET NULL invece di CASCADE
```

### AIConfigurations
```sql
-- V2 (campi base)
AzureOpenAIEndpoint
AzureOpenAIKey
EmbeddingDeploymentName
ChatDeploymentName

-- V3 (+ 20+ nuovi campi per multi-provider)
-- ✅ Provider Configuration
ProviderType INT NOT NULL DEFAULT 1
ProviderEndpoint NVARCHAR(MAX) NULL
ProviderApiKey NVARCHAR(MAX) NULL
ChatModelName NVARCHAR(MAX) NULL
EmbeddingModelName NVARCHAR(MAX) NULL

-- ✅ Service-Specific Providers
ChatProvider INT NULL
EmbeddingsProvider INT NULL
TagExtractionProvider INT NULL
RAGProvider INT NULL

-- ✅ Gemini Settings
GeminiApiKey NVARCHAR(MAX) NULL
GeminiChatModel NVARCHAR(MAX) NULL  -- Default: 'gemini-2.0-flash-exp'
GeminiEmbeddingModel NVARCHAR(MAX) NULL

-- ✅ OpenAI Settings
OpenAIApiKey NVARCHAR(MAX) NULL
OpenAIChatModel NVARCHAR(MAX) NULL
OpenAIEmbeddingModel NVARCHAR(MAX) NULL

-- ✅ Azure OpenAI Extended
AzureOpenAIChatModel NVARCHAR(MAX) NULL
AzureOpenAIEmbeddingModel NVARCHAR(MAX) NULL

-- ✅ Chunking Configuration
EnableChunking BIT NOT NULL DEFAULT 1
ChunkSize INT NOT NULL DEFAULT 1000
ChunkOverlap INT NOT NULL DEFAULT 200

-- ✅ Fallback Configuration
EnableFallback BIT NOT NULL DEFAULT 1
```

## 📦 Stored Procedures

### V2 (5 procedures)
1. sp_CleanupOldAuditLogs
2. sp_GetDashboardStatistics
3. sp_HybridSearch
4. sp_VectorSearch
5. sp_RetrieveRAGContext

### V3 (6 procedures = V2 + 1 nuova)
- **Tutte le 5 procedures di V2**
- ✨ **sp_CleanupOldLogEntries** (nuova) - pulizia log entries

## 🌱 Dati Iniziali

### V2
- Tenant predefinito: "Default"
- Utente admin: admin@docn.local / Admin@123
- Configurazione AI: "Default Azure OpenAI"
- Ruoli: Admin, User, Manager

### V3
- ✅ Tutto di V2 PLUS:
- Configurazione AI aggiornata a: **"Default Multi-Provider AI"**
- Modello Gemini aggiornato: **gemini-2.0-flash-exp** (non più gemini-1.5-flash)
- Campi multi-provider popolati automaticamente

## 📝 Differenze Chiave nei Commenti e Output

### V2 Header
```sql
-- DocN Database - Complete Creation Script
-- Versione: 2.0 - Dicembre 2024
```

### V3 Header
```sql
-- DocN Database - Complete Creation Script V3
-- Versione: 3.0 - Dicembre 2024
-- ================================================
-- CHANGELOG V3:
-- • Multi-provider AI (Gemini, OpenAI, Azure OpenAI)
-- • Tabella SimilarDocuments per similarità vettoriale
-- • Tabella LogEntries per logging centralizzato
-- • Aggiornato modello Gemini a gemini-2.0-flash-exp
-- • Corretto vincolo FK OwnerId (ON DELETE SET NULL)
-- • Aggiunto campo ExtractedMetadataJson per metadata AI
```

## 🔢 Statistiche File

| Metrica | V2 | V3 | Delta |
|---------|----|----|-------|
| Dimensione | 34KB | 40KB | +6KB |
| Righe | 985 | 1147 | +162 |
| CREATE TABLE | 16 | 18 | +2 |
| CREATE PROCEDURE | 5 | 6 | +1 |
| CREATE VIEW | 2 | 2 | = |
| CREATE INDEX | ~40 | ~45 | +5 |

## ✅ Compatibilità

### Retrocompatibilità
- ✅ V3 è **completamente retrocompatibile** con V2
- ✅ Tutti i campi di V2 sono presenti in V3
- ✅ Tutte le tabelle di V2 sono presenti in V3
- ✅ Tutte le procedures di V2 sono presenti in V3

### Migrazione da V2 a V3
Se hai già un database V2, hai **2 opzioni**:

#### Opzione 1: Aggiornamento Incrementale
Esegui solo gli script di update:
1. `001_AddMultiProviderAIConfiguration.sql`
2. `002_AddSimilarDocumentsTable.sql`
3. `003_AddLogEntriesTable.sql`
4. `004_UpdateGeminiDefaultModel.sql`
5. `005_FixOwnerIdForeignKeyConstraint.sql`
6. `AddExtractedMetadataJson.sql`

#### Opzione 2: Backup e Ricreazione
1. Backup del database V2
2. Drop database
3. Esegui `CreateDatabase_Complete_V3.sql`
4. Restore dei dati

## 🎯 Quale Usare?

### Usa V3 se:
- ✅ Stai creando un **nuovo database**
- ✅ Vuoi il **supporto multi-provider AI**
- ✅ Hai bisogno di **similarità documenti**
- ✅ Vuoi **logging centralizzato**
- ✅ Vuoi il modello **Gemini 2.0 aggiornato**
- ✅ Hai bisogno di **ExtractedMetadataJson**

### Usa V2 se:
- ⚠️ Hai vincoli specifici su Azure OpenAI solo
- ⚠️ Non vuoi aggiornare l'infrastruttura esistente
- ⚠️ Stai usando solo il vecchio modello Gemini 1.5

## 🚀 Raccomandazione

**Per tutti i nuovi progetti: usa CreateDatabase_Complete_V3.sql**

V3 è la versione più completa, aggiornata e con più funzionalità. Include tutto di V2 più molti miglioramenti importanti per la produzione.

---

**Ultimo aggiornamento:** 29 Dicembre 2024
