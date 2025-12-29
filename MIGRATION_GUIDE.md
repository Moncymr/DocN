# Guida alla Migrazione - Sistema Multi-Provider AI

Questa guida spiega come migrare da una configurazione esistente di DocN al nuovo sistema multi-provider.

## Panoramica delle Modifiche

Il sistema è stato aggiornato per supportare:
- Configurazione multi-provider (Gemini, OpenAI, Azure OpenAI)
- Configurazione basata su database invece di `appsettings.json`
- Assegnazione provider specifica per servizio
- Supporto chunking per documenti

## Retrocompatibilità

✅ **La tua installazione esistente continuerà a funzionare senza modifiche.**

Il sistema mantiene la retrocompatibilità con `appsettings.json`. Se non c'è configurazione nel database, userà i valori da `appsettings.json` come fallback.

## Passi per la Migrazione

### Passo 1: Backup del Database

```sql
-- Crea un backup completo prima della migrazione
BACKUP DATABASE [DocumentArchive] 
TO DISK = 'C:\Backup\DocumentArchive_PreMigration.bak'
WITH FORMAT, INIT, NAME = 'Pre-Migration Backup';
```

### Passo 2: Aggiorna lo Schema del Database

Hai due opzioni:

#### Opzione A: Usa Entity Framework Migrations

```bash
cd DocN.Data
dotnet ef database update --startup-project ../DocN.Client
```

Questo applicherà la migration `20251228072726_AddMultiProviderAIConfiguration`.

#### Opzione B: Esegui lo Script SQL Manualmente

```sql
-- Esegui: Database/UpdateScripts/001_AddMultiProviderAIConfiguration.sql
-- Questo script è idempotent - può essere eseguito più volte senza problemi
```

### Passo 3: Verifica lo Schema

Dopo l'aggiornamento, verifica che le nuove colonne siano state aggiunte:

```sql
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AIConfigurations'
ORDER BY ORDINAL_POSITION;
```

Dovresti vedere le nuove colonne come:
- `ProviderType`
- `ChatProvider`
- `EmbeddingsProvider`
- `TagExtractionProvider`
- `RAGProvider`
- `GeminiApiKey`
- `GeminiChatModel`
- `GeminiEmbeddingModel`
- `OpenAIApiKey`
- `OpenAIChatModel`
- `OpenAIEmbeddingModel`
- `AzureOpenAIChatModel`
- `AzureOpenAIEmbeddingModel`
- `EnableChunking`
- `ChunkSize`
- `ChunkOverlap`
- `EnableFallback`

### Passo 4: Riavvia l'Applicazione

```bash
# Se stai usando dotnet run
dotnet run --project DocN.Client

# Se stai usando IIS, riavvia il pool applicazioni
```

### Passo 5: Configura i Provider (Facoltativo)

1. Naviga su `https://your-app-url/config`
2. Vedrai un form pre-compilato con valori predefiniti
3. Inserisci le tue API keys e preferenze
4. Clicca "💾 Salva Configurazione"

## Mapping della Configurazione Precedente

### Da appsettings.json a Database

Se avevi questa configurazione in `appsettings.json`:

```json
{
  "AI": {
    "Provider": "Gemini",
    "EnableFallback": true
  },
  "Gemini": {
    "ApiKey": "your-gemini-key"
  },
  "OpenAI": {
    "ApiKey": "your-openai-key",
    "Model": "gpt-4"
  },
  "Embeddings": {
    "Provider": "AzureOpenAI",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-azure-key",
    "DeploymentName": "text-embedding-ada-002"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-azure-key",
    "ChatDeployment": "gpt-4",
    "EmbeddingDeployment": "text-embedding-ada-002"
  }
}
```

Inserisci questi valori nella pagina `/config`:

**Gemini Configuration:**
- API Key: `your-gemini-key`
- Chat Model: `gemini-2.0-flash-exp` (or newer models like `gemini-2.5-flash`, `gemini-3-flash`)
- Embedding Model: `text-embedding-004`

**OpenAI Configuration:**
- API Key: `your-openai-key`
- Chat Model: `gpt-4`
- Embedding Model: `text-embedding-ada-002`

**Azure OpenAI Configuration:**
- Endpoint: `https://your-resource.openai.azure.com/`
- API Key: `your-azure-key`
- Chat Deployment: `gpt-4`
- Embedding Deployment: `text-embedding-ada-002`

**Provider Assignment:**
- Chat Provider: Gemini
- Embeddings Provider: Azure OpenAI
- Tag Extraction Provider: Gemini
- RAG Provider: Gemini

**Advanced:**
- Enable Fallback: ✓

## Scenari di Migrazione Comuni

### Scenario 1: Configurazione Esistente Solo Azure OpenAI

**Prima (appsettings.json):**
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://company.openai.azure.com/",
    "ApiKey": "key123",
    "ChatDeployment": "gpt-4",
    "EmbeddingDeployment": "embedding"
  }
}
```

**Dopo (Database via /config):**
- Vai su `/config`
- Compila solo la sezione "Azure OpenAI Configuration"
- Lascia i provider servizi vuoti (useranno Azure come predefinito)
- Salva

### Scenario 2: Migrazione da Gemini a Multi-Provider

**Prima:** Solo Gemini configurato

**Dopo:**
1. Mantieni la configurazione Gemini esistente
2. Aggiungi le credenziali OpenAI o Azure come backup
3. Abilita "Enable Fallback"
4. Assegna Gemini per Chat/Tags (economico)
5. Assegna OpenAI per Embeddings (preciso)

### Scenario 3: Installazione Nuova

Se stai partendo da zero:
1. Applica le migrations al database nuovo
2. Vai su `/config`
3. Configura almeno un provider
4. Salva e testa

## Verifiche Post-Migrazione

### 1. Verifica Configurazione

```sql
-- Controlla le configurazioni salvate
SELECT * FROM AIConfigurations;
```

### 2. Test Funzionalità Upload

1. Vai su `/upload`
2. Carica un documento di test
3. Verifica che:
   - L'estrazione testo funziona
   - Gli embeddings vengono generati
   - La categoria viene suggerita
   - I tag vengono estratti

### 3. Test Ricerca Semantica

1. Vai su `/search`
2. Cerca un termine nei tuoi documenti
3. Verifica che la ricerca semantica funzioni

### 4. Test RAG Chat

1. Vai su `/chat`
2. Fai una domanda sui documenti
3. Verifica che il sistema risponda usando i documenti

### 5. Controlla i Log

```bash
# Controlla i log dell'applicazione per errori
tail -f logs/application.log

# Su Windows con IIS
# Controlla Event Viewer > Application Logs
```

## Rollback (se necessario)

Se riscontri problemi:

### Rollback Database

```sql
-- Ripristina il backup
USE master;
GO
RESTORE DATABASE [DocumentArchive]
FROM DISK = 'C:\Backup\DocumentArchive_PreMigration.bak'
WITH REPLACE;
```

### Rollback Codice

```bash
# Torna alla versione precedente
git checkout <previous-version-tag>
dotnet restore
dotnet build
```

## Problemi Comuni

### Problema: "Nessuna configurazione AI attiva trovata"

**Soluzione:**
1. Vai su `/config`
2. Inserisci almeno una API key valida
3. Spunta "Imposta come configurazione attiva"
4. Salva

### Problema: Embeddings non vengono generati

**Soluzione:**
1. Verifica che il provider Embeddings sia configurato
2. Controlla l'API key
3. Verifica che `EmbeddingDimensions` corrisponda al modello:
   - Gemini: 768
   - OpenAI/Azure: 1536

### Problema: Migration fallisce con errore di vincolo

**Soluzione:**
```sql
-- Rimuovi la migration fallita
DELETE FROM __EFMigrationsHistory 
WHERE MigrationId = '20251228072726_AddMultiProviderAIConfiguration';

-- Riprova
```

### Problema: Valori NULL in colonne NOT NULL

**Soluzione:**
Lo script di migrazione imposta valori di default. Se hai problemi:

```sql
-- Imposta manualmente i default
UPDATE AIConfigurations
SET 
    ProviderType = 1,
    EnableChunking = 1,
    ChunkSize = 1000,
    ChunkOverlap = 200,
    EnableFallback = 1
WHERE ProviderType IS NULL;
```

## Timeline Suggerita

Per ambienti di produzione:

1. **Settimana 1**: Test in ambiente di sviluppo
2. **Settimana 2**: Test in ambiente di staging
3. **Settimana 3**: Deploy in produzione durante finestra di manutenzione
4. **Settimana 4**: Monitoraggio e ottimizzazione

## Supporto

Per assistenza durante la migrazione:
- Controlla i log: `/logs`
- Rivedi questa guida
- Consulta `MULTI_PROVIDER_CONFIG.md`
- Contatta il supporto tecnico con:
  - Versione dell'applicazione
  - Logs degli errori
  - Dettagli configurazione (senza API keys!)

## Checklist Migrazione

- [ ] Backup database creato
- [ ] Schema database aggiornato
- [ ] Applicazione riavviata
- [ ] Pagina `/config` accessibile
- [ ] Almeno un provider configurato
- [ ] Test upload documento eseguito
- [ ] Test ricerca semantica eseguito
- [ ] Test RAG chat eseguito
- [ ] Logs controllati (nessun errore)
- [ ] Documentazione aggiornata
- [ ] Team informato delle modifiche

---

**Nota Importante:** Questa migrazione è non distruttiva. Le tue configurazioni esistenti in `appsettings.json` continueranno a funzionare come fallback.

Ultimo aggiornamento: 2024-12-28
