# DocN

DocN è una soluzione web modulare basata su .NET e Blazor, progettata per l'archiviazione intelligente e la consultazione di documenti, con ricerca semantica AI e integrazione multi-provider (Azure OpenAI, OpenAI, Google Gemini).

## 🚀 Inizia Subito

**📖 [Guida Configurazione Completa](CONFIGURAZIONE_COMPLETA.md)** - Tutto quello che devi impostare per il corretto funzionamento

### Guide Rapide
- 📘 [Setup Base](SETUP.md) - Installazione e configurazione generale
- 🔑 [Configurazione API Keys](CONFIGURAZIONE_API_KEYS.md) - Setup chiavi API dettagliato
- ⚡ [Quick Reference](QUICK_REFERENCE_API_KEYS.md) - Riferimento rapido configurazione

## Funzionalità principali
- Archiviazione documenti e metadati in SQL Server 2025.
- Estrazione automatica testo/metadati dai documenti caricati.
- Proposta categoria tramite AI al caricamento documento.
- Calcolo embedding vettoriali e ricerca semantica.
- **Supporto multi-provider AI**: Azure OpenAI, OpenAI, Google Gemini.
- Orchestrazione retrieval e generazione risposte tramite Microsoft Agent Framework.
- Interfaccia Blazor per upload, ricerca e consultazione documenti.

## Architettura
- Progetti separati per:
  - **DocN.Core**: Libreria core con interfacce AI e implementazioni provider
  - **DocN.Data**: Accesso ai dati
  - **DocN.Server**: Server logic (ASP.NET Core)
  - **DocN.Client**: Client Blazor
- Integrazione chatbot AI con supporto per provider multipli.

## Provider AI Supportati

DocN supporta tre provider AI principali per embeddings e generazione di contenuti:

### 1. Azure OpenAI
Il provider predefinito, ottimale per scenari enterprise con esigenze di compliance e sicurezza.

**Configurazione:**
```json
{
  "AIProvider": {
    "DefaultProvider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "your-api-key",
      "EmbeddingDeployment": "text-embedding-ada-002",
      "ChatDeployment": "gpt-4",
      "ApiVersion": "2024-02-15-preview"
    }
  }
}
```

### 2. OpenAI
Provider diretto OpenAI, ideale per prototipazione rapida e accesso ai modelli più recenti.

**Configurazione:**
```json
{
  "AIProvider": {
    "DefaultProvider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-your-openai-api-key",
      "EmbeddingModel": "text-embedding-3-small",
      "ChatModel": "gpt-4-turbo",
      "OrganizationId": null
    }
  }
}
```

### 3. Google Gemini
Provider Google Gemini per sfruttare i modelli Gemini e le loro capacità multimodali.

**Configurazione:**
```json
{
  "AIProvider": {
    "DefaultProvider": "Gemini",
    "Gemini": {
      "ApiKey": "your-gemini-api-key",
      "EmbeddingModel": "text-embedding-004",
      "GenerationModel": "gemini-1.5-pro",
      "ApiEndpoint": null
    }
  }
}
```

## Utilizzo

### Configurazione dei Provider

1. Copia il file `appsettings.example.json` in `appsettings.json`
2. Inserisci le tue API keys per i provider che vuoi utilizzare
3. Imposta il `DefaultProvider` al provider desiderato

### Registrazione dei Servizi

Nel tuo `Program.cs`:

```csharp
using DocN.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Registra i servizi AI
builder.Services.AddDocNAIServices(builder.Configuration);

var app = builder.Build();
```

### Utilizzo dei Provider nel Codice

```csharp
using DocN.Core.AI.Interfaces;
using DocN.Core.AI.Models;

public class DocumentService
{
    private readonly IAIProviderFactory _aiFactory;

    public DocumentService(IAIProviderFactory aiFactory)
    {
        _aiFactory = aiFactory;
    }

    public async Task AnalyzeDocument(string documentText)
    {
        // Usa il provider predefinito
        var provider = _aiFactory.GetDefaultProvider();
        
        // Oppure seleziona un provider specifico
        // var provider = _aiFactory.CreateProvider(AIProviderType.Gemini);
        
        // Genera embedding
        var embedding = await provider.GenerateEmbeddingAsync(documentText);
        
        // Suggerisci categorie
        var categories = new List<string> { "Contratti", "Fatture", "Report" };
        var suggestions = await provider.SuggestCategoriesAsync(documentText, categories);
        
        // Analisi completa
        var result = await provider.AnalyzeDocumentAsync(documentText, categories);
    }
}
```

## Switching tra Provider

È possibile cambiare provider in qualsiasi momento modificando la configurazione o utilizzando direttamente la factory:

```csharp
// Usa Azure OpenAI
var azureProvider = _aiFactory.CreateProvider(AIProviderType.AzureOpenAI);
var embedding1 = await azureProvider.GenerateEmbeddingAsync(text);

// Usa Google Gemini
var geminiProvider = _aiFactory.CreateProvider(AIProviderType.Gemini);
var embedding2 = await geminiProvider.GenerateEmbeddingAsync(text);

// Usa OpenAI
var openaiProvider = _aiFactory.CreateProvider(AIProviderType.OpenAI);
var embedding3 = await openaiProvider.GenerateEmbeddingAsync(text);
```

## Dipendenze

- .NET 9.0
- Azure.AI.OpenAI 2.1.0 (per Azure OpenAI)
- OpenAI 2.1.0 (per OpenAI)
- Mscc.GenerativeAI 2.1.0 (per Google Gemini)
- Microsoft.Extensions.* (Dependency Injection, Configuration, Logging)

**Nota Database**: L'integrazione con SQL Server 2025 per l'archiviazione documenti è prevista nelle prossime fasi. Attualmente, il progetto si concentra sulla gestione multi-provider AI. Se si verifica un errore relativo al database durante l'esecuzione, è normale - l'implementazione del database è in fase di sviluppo.

## Prossimi Passi

- [ ] Implementazione database SQL Server 2025 per archiviazione documenti
- [ ] Integrazione ricerca semantica con vector database
- [ ] Implementazione Microsoft Agent Framework per orchestrazione
- [ ] UI Blazor per upload e consultazione documenti
- [ ] Test end-to-end per tutti i provider AI

## Contributi

Questo progetto è in fase di sviluppo attivo. Contributi e suggerimenti sono benvenuti!
