# RAG Provider Initialization Guide - Dove inizializza il provider per RAG?

## 🎯 Risposta Rapida

Il provider RAG per i documenti viene inizializzato in **DocN.Server/Program.cs** alla riga **324**, dove viene registrato `MultiProviderSemanticRAGService` come implementazione di `ISemanticRAGService` tramite dependency injection.

```csharp
// DocN.Server/Program.cs - Riga 324
builder.Services.AddScoped<ISemanticRAGService, MultiProviderSemanticRAGService>();
```

## 📋 Flusso Completo di Inizializzazione

### 1️⃣ **Registrazione del Servizio (Program.cs)**

**File**: `DocN.Server/Program.cs`  
**Righe**: 321-324

```csharp
// Register Semantic RAG Service
// Always use MultiProviderSemanticRAGService which supports both appsettings and database config
// It will try configured providers with fallback mechanism (similar to embedding service)
builder.Services.AddScoped<ISemanticRAGService, MultiProviderSemanticRAGService>();
```

Questo registra il servizio RAG nel container di dependency injection di ASP.NET Core con scope "Scoped" (una nuova istanza per ogni richiesta HTTP).

### 2️⃣ **Dependency Injection nel Controller**

**File**: `DocN.Server/Controllers/SemanticChatController.cs`  
**Righe**: 22-32

```csharp
public SemanticChatController(
    ISemanticRAGService ragService,     // ← Iniettato automaticamente
    ApplicationDbContext context,
    ILogger<SemanticChatController> logger,
    IWebHostEnvironment environment)
{
    _ragService = ragService;
    _context = context;
    _logger = logger;
    _environment = environment;
}
```

Quando viene chiamato l'endpoint `/api/SemanticChat/query`, ASP.NET Core crea automaticamente un'istanza di `MultiProviderSemanticRAGService` e la inietta nel controller.

### 3️⃣ **Configurazione AI Provider (MultiProviderAIService)**

**File**: `DocN.Data/Services/MultiProviderSemanticRAGService.cs`  
**Righe**: 18-27

Il `MultiProviderSemanticRAGService` riceve `IMultiProviderAIService` tramite dependency injection:

```csharp
public MultiProviderSemanticRAGService(
    ApplicationDbContext context,
    ILogger<MultiProviderSemanticRAGService> logger,
    IMultiProviderAIService aiService)      // ← Provider AI
{
    _context = context;
    _logger = logger;
    _aiService = aiService;
}
```

### 4️⃣ **Caricamento della Configurazione**

**File**: `DocN.Data/Services/MultiProviderAIService.cs`  
**Righe**: 43-68

Il `MultiProviderAIService` legge la configurazione da due possibili fonti:

#### Opzione A: Database (Prioritaria)
```csharp
public async Task<AIConfiguration?> GetActiveConfigurationAsync()
{
    // Fetch active configuration from database
    _cachedConfig = await _context.AIConfigurations
        .Where(c => c.IsActive)
        .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
        .FirstOrDefaultAsync();
    
    // If no database configuration exists, create a default one from appsettings
    if (_cachedConfig == null)
    {
        _cachedConfig = CreateDefaultConfigurationFromAppSettings();
    }
    
    return _cachedConfig;
}
```

#### Opzione B: appsettings.json (Fallback)
```csharp
private AIConfiguration CreateDefaultConfigurationFromAppSettings()
{
    return new AIConfiguration
    {
        GeminiApiKey = _configuration["Gemini:ApiKey"],
        GeminiChatModel = "gemini-2.0-flash-exp",
        OpenAIApiKey = _configuration["OpenAI:ApiKey"],
        AzureOpenAIEndpoint = _configuration["AzureOpenAI:Endpoint"],
        AzureOpenAIKey = _configuration["AzureOpenAI:ApiKey"],
        // ... altre configurazioni
    };
}
```

## 🔧 Configurazione del Provider RAG

### Metodo 1: Configurazione Database (Consigliato)

Il provider RAG viene configurato tramite la tabella `AIConfigurations` nel database:

```sql
-- Esempio di configurazione nel database
INSERT INTO AIConfigurations (
    ConfigurationName,
    GeminiApiKey,
    GeminiChatModel,
    GeminiEmbeddingModel,
    ChatProvider,
    EmbeddingsProvider,
    RAGProvider,
    EnableFallback,
    IsActive
) VALUES (
    'Production Configuration',
    'your-gemini-api-key',
    'gemini-2.0-flash-exp',
    'text-embedding-004',
    0, -- 0=Gemini, 1=OpenAI, 2=AzureOpenAI
    0,
    0,
    1, -- Enable fallback
    1  -- Active
);
```

**Vantaggi**:
- ✅ Configurazione dinamica senza riavvio del server
- ✅ Gestione UI tramite la pagina Settings
- ✅ Cache di 5 minuti per performance

### Metodo 2: appsettings.json (Fallback)

Se non c'è configurazione nel database, il sistema usa `appsettings.json`:

```json
{
  "Gemini": {
    "ApiKey": "your-api-key-here",
    "Model": "gemini-2.0-flash-exp"
  },
  "OpenAI": {
    "ApiKey": "your-api-key-here",
    "Model": "gpt-4"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "ChatDeployment": "gpt-4",
    "EmbeddingDeployment": "text-embedding-ada-002"
  },
  "AI": {
    "Provider": "Gemini",
    "EnableFallback": true
  }
}
```

## 🔄 Processo di Generazione Risposta RAG

### Quando l'utente invia un messaggio:

1. **Client (Chat.razor)** - Riga 876:
   ```csharp
   var response = await _httpClient.PostAsJsonAsync("api/SemanticChat/query", request);
   ```

2. **Controller** riceve la richiesta - `SemanticChatController.Query()` - Riga 46

3. **MultiProviderSemanticRAGService.GenerateResponseAsync()** - Riga 29:
   - Cerca documenti rilevanti con vector search
   - Carica la cronologia della conversazione
   - Genera la risposta usando AI configurato
   - Salva la conversazione nel database

4. **MultiProviderAIService** esegue:
   - `GenerateEmbeddingAsync()` - Crea embedding della query
   - `GenerateChatCompletionAsync()` - Genera risposta con AI

## 📊 Architettura Componenti

```
┌─────────────────────────────────────────────────────────────┐
│                      Client (Blazor)                        │
│                     Chat.razor                              │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP POST /api/SemanticChat/query
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                Server (ASP.NET Core)                        │
│              SemanticChatController                         │
└──────────────────────┬──────────────────────────────────────┘
                       │ ISemanticRAGService
                       ▼
┌─────────────────────────────────────────────────────────────┐
│            MultiProviderSemanticRAGService                  │
│  ┌────────────────────────────────────────────────────┐    │
│  │ • SearchDocumentsAsync()                           │    │
│  │ • LoadConversationHistoryAsync()                   │    │
│  │ • GenerateResponseAsync()                          │    │
│  │ • SaveConversationAsync()                          │    │
│  └────────────────────┬───────────────────────────────┘    │
└────────────────────────┼────────────────────────────────────┘
                        │ IMultiProviderAIService
                        ▼
┌─────────────────────────────────────────────────────────────┐
│               MultiProviderAIService                        │
│  ┌────────────────────────────────────────────────────┐    │
│  │ • GetActiveConfigurationAsync()                    │    │
│  │ • GenerateEmbeddingAsync()                         │    │
│  │ • GenerateChatCompletionAsync()                    │    │
│  └────────────────────┬───────────────────────────────┘    │
└────────────────────────┼────────────────────────────────────┘
                        │
           ┌────────────┴────────────┐
           ▼                         ▼
    ┌─────────────┐          ┌─────────────────┐
    │   Database  │          │ appsettings.json│
    │AIConfigurations│        │  (Fallback)     │
    └─────────────┘          └─────────────────┘
```

## 🚀 Avvio Rapido

### Per configurare il sistema RAG:

1. **Vai alla pagina Settings** nell'applicazione web
2. **Configura almeno un provider AI**:
   - Gemini (Consigliato per embedding)
   - OpenAI
   - Azure OpenAI
3. **Inserisci le API Keys** necessarie
4. **Attiva la configurazione** (toggle "Active")
5. **Testa il sistema** nella pagina Chat

### O configura via appsettings.json:

```bash
# Edita appsettings.json o appsettings.Development.json
# Inserisci le API keys nella sezione appropriata

# Riavvia il server
dotnet run --project DocN.Server
```

## 🔍 Debug e Troubleshooting

### Verificare quale provider è attivo:

Guarda i log del server quando viene eseguita una query:

```
[INFO] Using Gemini for embedding generation
[INFO] RAG response generated in 1234ms with 3 documents using Gemini
```

### Errori comuni:

1. **"AI_PROVIDER_NOT_CONFIGURED"**
   - Nessun provider configurato
   - Vai in Settings e configura almeno un provider

2. **"No relevant documents found"**
   - Nessun documento con embeddings
   - Carica documenti e attendi che vengano processati

3. **"Failed to generate query embedding"**
   - API Key non valida o provider non raggiungibile
   - Verifica le API keys e la connessione internet

## 📝 File Chiave

| File | Descrizione |
|------|-------------|
| `DocN.Server/Program.cs` | **Registrazione servizi** RAG nel container DI |
| `DocN.Server/Controllers/SemanticChatController.cs` | **Endpoint API** per chat semantica |
| `DocN.Data/Services/MultiProviderSemanticRAGService.cs` | **Implementazione RAG** con vector search |
| `DocN.Data/Services/MultiProviderAIService.cs` | **Gestione provider AI** e configurazione |
| `DocN.Client/Components/Pages/Chat.razor` | **UI Chat** lato client |
| `DocN.Data/Models/AIConfiguration.cs` | **Modello configurazione** AI |
| `appsettings.json` | **Configurazione fallback** provider |

## 🎓 Per Saperne di Più

- **Documentazione completa**: [INDICE_DOCUMENTAZIONE.md](INDICE_DOCUMENTAZIONE.md)
- **Quick Start RAG**: Vedi sezione "🔍 Guida alle Funzionalità di Ricerca e RAG"
- **Esempi pratici**: Documentazione con screenshot e casi d'uso
- **Configurazione avanzata**: Chunking, embedding models, fallback strategies

## ✨ Conclusione

Il provider RAG viene **inizializzato automaticamente** dal sistema di dependency injection di ASP.NET Core quando parte il server. La configurazione viene caricata dal **database** (priorità) o da **appsettings.json** (fallback).

Non è necessario inizializzare manualmente il provider - è tutto gestito automaticamente dal framework!
