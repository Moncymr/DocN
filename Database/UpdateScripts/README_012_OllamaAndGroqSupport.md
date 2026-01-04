# Script 012: Aggiunta Supporto Ollama e Groq

## Descrizione

Questo script aggiunge il supporto per i provider AI **Ollama** e **Groq** alla tabella `AIConfigurations` del database.

## Cosa viene aggiunto

### Ollama (Modelli AI Locali)

Nuove colonne per Ollama:
- `OllamaEndpoint` (nvarchar(max)) - Endpoint del server Ollama (default: `http://localhost:11434`)
- `OllamaChatModel` (nvarchar(max)) - Modello per chat (default: `llama3`)
- `OllamaEmbeddingModel` (nvarchar(max)) - Modello per embeddings (default: `nomic-embed-text`)

### Groq (API Cloud Veloce)

Nuove colonne per Groq:
- `GroqApiKey` (nvarchar(max)) - Chiave API Groq
- `GroqChatModel` (nvarchar(max)) - Modello per chat (default: `llama-3.1-8b-instant`)
- `GroqEndpoint` (nvarchar(max)) - Endpoint API (default: `https://api.groq.com/openai/v1`)

## Valori Enum ProviderType Aggiornati

```
0 = AzureOpenAI
1 = OpenAI
2 = Gemini
3 = Ollama  ← NUOVO
4 = Groq    ← NUOVO
```

## Come Eseguire

### Opzione 1: SQL Server Management Studio (SSMS)

1. Apri SSMS e connettiti al database
2. Apri il file `012_AddOllamaAndGroqSupport.sql`
3. Assicurati che il database `DocumentArchive` sia selezionato
4. Esegui lo script (F5)
5. Verifica l'output nella finestra messaggi

### Opzione 2: sqlcmd (Command Line)

```bash
sqlcmd -S localhost -d DocumentArchive -i 012_AddOllamaAndGroqSupport.sql
```

Con autenticazione:
```bash
sqlcmd -S localhost -U sa -P YourPassword -d DocumentArchive -i 012_AddOllamaAndGroqSupport.sql
```

### Opzione 3: Azure Data Studio

1. Apri Azure Data Studio
2. Connettiti al database DocumentArchive
3. Apri il file `012_AddOllamaAndGroqSupport.sql`
4. Esegui lo script
5. Verifica i messaggi di output

## Output Atteso

```
Inizio aggiornamento tabella AIConfigurations per supporto Ollama e Groq...
✓ Colonna OllamaEndpoint aggiunta
✓ Colonna OllamaChatModel aggiunta
✓ Colonna OllamaEmbeddingModel aggiunta
✓ Colonna GroqApiKey aggiunta
✓ Colonna GroqChatModel aggiunta
✓ Colonna GroqEndpoint aggiunta

=========================================
Aggiornamento completato con successo!
=========================================

La tabella AIConfigurations ora supporta:
  ✓ Ollama (modelli AI locali)
    - Endpoint: http://localhost:11434
    - Chat Model: llama3
    - Embedding Model: nomic-embed-text

  ✓ Groq (API cloud veloce)
    - Endpoint: https://api.groq.com/openai/v1
    - Chat Model: llama-3.1-8b-instant
    - Nota: Groq non supporta embeddings
```

## Verifica

Dopo aver eseguito lo script, verifica che le colonne siano state aggiunte:

```sql
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AIConfigurations'
AND COLUMN_NAME IN (
    'OllamaEndpoint', 'OllamaChatModel', 'OllamaEmbeddingModel',
    'GroqApiKey', 'GroqChatModel', 'GroqEndpoint'
)
ORDER BY COLUMN_NAME;
```

## Rollback

Se necessario eseguire il rollback (NON CONSIGLIATO se ci sono dati):

```sql
USE [DocumentArchive]
GO

ALTER TABLE [dbo].[AIConfigurations] DROP COLUMN [OllamaEndpoint];
ALTER TABLE [dbo].[AIConfigurations] DROP COLUMN [OllamaChatModel];
ALTER TABLE [dbo].[AIConfigurations] DROP COLUMN [OllamaEmbeddingModel];
ALTER TABLE [dbo].[AIConfigurations] DROP COLUMN [GroqApiKey];
ALTER TABLE [dbo].[AIConfigurations] DROP COLUMN [GroqChatModel];
ALTER TABLE [dbo].[AIConfigurations] DROP COLUMN [GroqEndpoint];
GO

PRINT 'Rollback completato. Colonne Ollama e Groq rimosse.';
GO
```

## Configurazione Post-Installazione

### Ollama (Locale)

Dopo aver eseguito lo script, per usare Ollama:

1. Installa Ollama: https://ollama.ai
2. Avvia Ollama: `ollama serve`
3. Scarica modelli:
   ```bash
   ollama pull llama3
   ollama pull nomic-embed-text
   ```
4. Configura in DocN tramite `/config` nell'applicazione

**Guide complete:**
- [GUIDA_OLLAMA_LOCALE.md](../../GUIDA_OLLAMA_LOCALE.md) - Installazione locale
- [GUIDA_OLLAMA_COLAB.md](../../GUIDA_OLLAMA_COLAB.md) - Google Colab gratis

### Groq (Cloud)

Dopo aver eseguito lo script, per usare Groq:

1. Registrati su: https://console.groq.com
2. Ottieni API key
3. Configura in DocN tramite `/config` nell'applicazione
4. Tier gratuito: 14,400 richieste/giorno

**Guida completa:**
- [GUIDA_GROQ.md](../../GUIDA_GROQ.md) - Setup completo Groq API

## Configurazione Multi-Provider Consigliata

**Migliore configurazione (velocità + qualità):**
```json
{
  "AIProvider": {
    "DefaultProvider": "Groq",
    "Groq": {
      "ApiKey": "gsk_...",
      "ChatModel": "llama-3.1-8b-instant"
    },
    "Gemini": {
      "ApiKey": "...",
      "EmbeddingModel": "text-embedding-004"
    }
  }
}
```

**Configurazione privacy (tutto locale):**
```json
{
  "AIProvider": {
    "DefaultProvider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ChatModel": "llama3",
      "EmbeddingModel": "nomic-embed-text"
    }
  }
}
```

## Troubleshooting

### Errore: "Invalid column name 'OllamaEndpoint'"

**Causa**: Script non eseguito o esecuzione fallita

**Soluzione**: 
1. Verifica che il database sia `DocumentArchive`
2. Esegui lo script manualmente
3. Controlla i permessi dell'utente database

### Errore: "Column already exists"

**Causa**: Script già eseguito in precedenza

**Soluzione**: Questo è normale, lo script è idempotente (può essere eseguito più volte)

### Verifica Tabella

```sql
-- Mostra tutte le colonne della tabella
SELECT * FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AIConfigurations'
ORDER BY ORDINAL_POSITION;
```

## Note Importanti

⚠️ **Importante**:
- Groq NON supporta embeddings nativamente
- Per embeddings con Groq, usa un altro provider (Gemini, OpenAI, o Ollama)
- Ollama richiede installazione locale o Google Colab
- Groq richiede registrazione e API key (gratuita)

## Compatibilità

- SQL Server 2019+
- SQL Server 2022+
- SQL Server 2025+
- Azure SQL Database

## Link Utili

- **Ollama**: https://ollama.ai
- **Groq Console**: https://console.groq.com
- **DocN Repository**: https://github.com/Moncymr/DocN

## Cronologia

- **2026-01-04**: Creazione script iniziale
- **Versione**: 012
- **Autore**: @copilot
- **Riferimento**: PR "Add Ollama and Groq providers"
