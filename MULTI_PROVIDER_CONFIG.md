# Configurazione Multi-Provider AI

Questa guida spiega come configurare e utilizzare il sistema multi-provider AI di DocN per supportare Gemini, OpenAI e Azure OpenAI.

## Panoramica

DocN supporta ora tre provider AI principali:
- **Gemini** (Google AI)
- **OpenAI**
- **Azure OpenAI**

Puoi configurare diversi provider per servizi diversi, permettendo flessibilità e ridondanza nel caso un provider non sia disponibile.

## Servizi Configurabili

Il sistema permette di assegnare provider specifici per:

1. **Chat** - Usato per conversazioni e analisi delle categorie dei documenti
2. **Embeddings** - Usato per generare vettori per la ricerca semantica
3. **Tag Extraction** - Usato per estrarre automaticamente tag dai documenti
4. **RAG (Retrieval Augmented Generation)** - Usato per chat con i documenti

## Configurazione tramite Interfaccia Web

### 1. Accedi alla Pagina di Configurazione

Naviga su `/config` nell'applicazione web.

### 2. Compila le Informazioni Base

- **Nome Configurazione**: Un nome descrittivo per la configurazione (es: "Produzione 2024")

### 3. Assegna i Provider ai Servizi

Per ogni servizio, seleziona quale provider utilizzare:
- Lascia vuoto per usare il provider predefinito
- Seleziona un provider specifico per quel servizio

### 4. Configura Gemini

Se vuoi usare Gemini:
- **API Key**: La tua chiave API di Google AI
- **Chat Model**: `gemini-2.0-flash-exp` (predefinito), `gemini-2.5-flash`, o `gemini-3-flash` (più recenti)
- **Embedding Model**: `text-embedding-004` (predefinito)

### 5. Configura OpenAI

Se vuoi usare OpenAI:
- **API Key**: La tua chiave API di OpenAI
- **Chat Model**: `gpt-4` (predefinito) o `gpt-3.5-turbo`
- **Embedding Model**: `text-embedding-ada-002` (predefinito)

### 6. Configura Azure OpenAI

Se vuoi usare Azure OpenAI:
- **Endpoint**: URL del tuo servizio Azure OpenAI (es: `https://your-resource.openai.azure.com/`)
- **API Key**: La tua chiave API di Azure
- **Chat Deployment Name**: Nome del deployment per chat (es: `gpt-4`)
- **Embedding Deployment Name**: Nome del deployment per embeddings (es: `text-embedding-ada-002`)

### 7. Configura RAG

- **Max Documenti da Recuperare**: Numero di documenti da usare per contesto (predefinito: 5)
- **Soglia Similarità**: Valore tra 0.0 e 1.0 per filtrare risultati (predefinito: 0.7)
- **Max Token per Contesto**: Limite di token per le risposte (predefinito: 4000)
- **System Prompt**: Istruzioni per l'AI

### 8. Configura Chunking

- **Abilita Chunking**: Divide i documenti in parti più piccole per migliore precisione
- **Dimensione Chunk**: Numero di caratteri per chunk (predefinito: 1000)
- **Overlap Chunk**: Sovrapposizione tra chunks (predefinito: 200)

### 9. Impostazioni Avanzate

- **Abilita Fallback**: Se attivato, il sistema proverà automaticamente altri provider se uno fallisce
- **Imposta come Attiva**: Solo una configurazione può essere attiva alla volta

### 10. Salva

Clicca su "💾 Salva Configurazione" per salvare le impostazioni.

## Aggiornamento Database Esistente

Se hai già un database DocN esistente, devi eseguire lo script di aggiornamento:

```sql
-- Esegui questo script sul tuo database SQL Server
-- Percorso: Database/UpdateScripts/001_AddMultiProviderAIConfiguration.sql
```

Lo script aggiungerà tutte le colonne necessarie alla tabella `AIConfigurations`.

## Migrazione da Configurazione Precedente

Se stavi usando la vecchia configurazione basata su `appsettings.json`:

1. Il sistema è retrocompatibile - continuerà a funzionare con la vecchia configurazione
2. I valori da `appsettings.json` saranno usati come fallback se non c'è configurazione nel database
3. Quando crei la prima configurazione nel database, questa avrà la precedenza
4. Puoi migrare i valori manualmente copiandoli dalla pagina `/config`

## Esempi di Configurazione

### Configurazione Solo Gemini

```
Provider Servizi:
- Chat: Gemini
- Embeddings: Gemini
- Tag Extraction: Gemini
- RAG: Gemini

Gemini:
- API Key: [tua-chiave]
- Chat Model: gemini-2.0-flash-exp
- Embedding Model: text-embedding-004
```

### Configurazione Ibrida (Gemini + OpenAI)

```
Provider Servizi:
- Chat: Gemini (più economico)
- Embeddings: OpenAI (più preciso)
- Tag Extraction: Gemini
- RAG: OpenAI

Gemini:
- API Key: [tua-chiave-gemini]
- Chat Model: gemini-2.0-flash-exp

OpenAI:
- API Key: [tua-chiave-openai]
- Embedding Model: text-embedding-ada-002
- Chat Model: gpt-4
```

### Configurazione Enterprise (Azure OpenAI)

```
Provider Servizi:
- Tutti impostati su: Azure OpenAI

Azure OpenAI:
- Endpoint: https://your-company.openai.azure.com/
- API Key: [tua-chiave-azure]
- Chat Deployment: gpt-4-deployment
- Embedding Deployment: embedding-deployment
```

## Fallback Automatico

Se abiliti il fallback, il sistema:

1. Proverà il provider configurato per il servizio
2. Se fallisce, proverà gli altri provider configurati in ordine:
   - Gemini
   - OpenAI
   - Azure OpenAI
3. Restituirà un errore solo se tutti i provider falliscono

## Dimensioni Embedding

Provider diversi producono vettori di dimensioni diverse:

- **Gemini** (`text-embedding-004`): 768 dimensioni
- **OpenAI** (`text-embedding-ada-002`): 1536 dimensioni  
- **Azure OpenAI** (`text-embedding-ada-002`): 1536 dimensioni

⚠️ **Importante**: Se cambi provider per gli embeddings, potrebbe essere necessario rigenerare gli embeddings per tutti i documenti esistenti.

## Risoluzione Problemi

### Provider non funziona

1. Verifica che l'API key sia corretta
2. Controlla che l'endpoint (per Azure) sia corretto
3. Verifica i log dell'applicazione per errori specifici
4. Prova il pulsante "🔌 Testa Connessione"

### Embeddings non vengono generati

1. Verifica che il provider Embeddings sia configurato
2. Controlla che l'API key abbia i permessi necessari
3. Verifica che il modello specificato sia disponibile

### Chat non risponde correttamente

1. Verifica la configurazione del provider Chat
2. Controlla il System Prompt in configurazione RAG
3. Verifica i parametri di similarità e max token

## Best Practices

1. **Usa Gemini per chat** - Più economico e veloce per operazioni frequenti
2. **Usa OpenAI per embeddings** - Generalmente più preciso per ricerca semantica
3. **Abilita sempre il fallback** - Garantisce disponibilità del servizio
4. **Monitora i costi** - Provider diversi hanno costi diversi
5. **Testa prima di produzione** - Verifica la configurazione in ambiente di test

## Supporto

Per problemi o domande:
- Controlla i log dell'applicazione
- Verifica la documentazione del provider specifico
- Contatta il supporto tecnico

---

Ultimo aggiornamento: 2024-12-28
