# DocN.Core - Documentazione Tecnica

## Indice
1. [Panoramica Progetto](#panoramica-progetto)
2. [Scopo e Funzionalità](#scopo-e-funzionalità)
3. [Architettura](#architettura)
4. [Tecnologie Utilizzate](#tecnologie-utilizzate)
5. [Struttura del Progetto](#struttura-del-progetto)
6. [Componenti Principali](#componenti-principali)
7. [Interfacce e Contratti](#interfacce-e-contratti)
8. [Integrazione con Altri Progetti](#integrazione-con-altri-progetti)

---

## Panoramica Progetto

**DocN.Core** è il progetto fondamentale della soluzione DocN che contiene i modelli di dominio, le interfacce di servizio e le componenti core per l'integrazione con l'intelligenza artificiale. Questo progetto rappresenta il "cuore" dell'architettura e definisce i contratti che tutti gli altri progetti devono rispettare.

### Informazioni di Base
- **Tipo**: Class Library (.NET)
- **Target Framework**: .NET 10.0
- **Ruolo**: Domain Layer + AI Abstractions
- **Dipendenze**: Nessuna dipendenza da altri progetti della soluzione

---

## Scopo e Funzionalità

### Scopo Principale

DocN.Core serve come **fondazione architettonica** dell'intera soluzione, fornendo:

1. **Definizione del Dominio**
   - Modelli di dominio che rappresentano le entità business
   - Value objects e DTOs per il trasferimento dati
   - Enumerazioni e costanti di sistema

2. **Contratti di Servizio**
   - Interfacce che definiscono i contratti per i servizi AI
   - Astrazioni per provider multipli (Gemini, OpenAI, Azure OpenAI)
   - Interfacce per servizi di elaborazione documenti

3. **Integrazione AI**
   - Wrapper per Microsoft Semantic Kernel
   - Astrazioni per generazione embeddings
   - Componenti per orchestrazione agenti AI

4. **Estensioni e Utilities**
   - Extension methods per tipi comuni
   - Helper per configurazione
   - Utilità per validazione e conversione

### Funzionalità Specifiche

#### 1. Gestione Provider AI Multi-Platform
Supporto per diversi provider di intelligenza artificiale:
- **Google Gemini**: API per embeddings e chat
- **OpenAI**: GPT models e text-embedding
- **Azure OpenAI**: Servizi enterprise AI di Microsoft

#### 2. Semantic Kernel Integration
Integrazione completa con Microsoft Semantic Kernel per:
- Orchestrazione di chiamate AI complesse
- Gestione della memoria conversazionale
- Pipeline di elaborazione multi-step
- Sistema di plugin e skill

#### 3. Agent Framework
Sistema di agenti AI specializzati:
- Definizione interfacce per agenti custom
- Orchestrazione multi-agente
- Gestione contesto e stato agenti

#### 4. Modelli di Configurazione
Configurazione centralizzata per:
- Parametri AI provider
- Impostazioni RAG (Retrieval-Augmented Generation)
- Opzioni di elaborazione documenti
- Configurazione OCR

---

## Architettura

### Principi Architetturali

DocN.Core segue i principi di **Clean Architecture** e **Domain-Driven Design**:

1. **Separation of Concerns**
   - Separazione tra definizioni di dominio e implementazioni
   - Interfacce separate dalle implementazioni concrete

2. **Dependency Inversion**
   - Le dipendenze puntano verso le astrazioni
   - Nessuna dipendenza su progetti infrastrutturali

3. **Interface Segregation**
   - Interfacce piccole e focalizzate
   - Client dipendono solo da ciò che usano

4. **Provider Agnostic**
   - Astrazioni indipendenti dal provider specifico
   - Facilita cambio o aggiunta di nuovi provider

### Layers Architetturali

```
DocN.Core
├── AI/                          # Astrazioni AI
│   ├── IMultiProviderAIService  # Interfaccia provider multipli
│   ├── IAIProvider              # Interfaccia base provider
│   └── AIConfiguration          # Configurazione AI
│
├── SemanticKernel/              # Integrazione Semantic Kernel
│   ├── Agents/                  # Framework agenti
│   ├── Memory/                  # Gestione memoria
│   └── Plugins/                 # Plugin e skill
│
├── Interfaces/                  # Interfacce di servizio
│   ├── IEmbeddingService        # Generazione embeddings
│   ├── IChunkingService         # Suddivisione documenti
│   └── IDocumentProcessor       # Elaborazione documenti
│
└── Extensions/                  # Extension methods
    ├── ServiceCollectionExtensions
    └── ConfigurationExtensions
```

### Pattern Implementati

1. **Repository Pattern**
   - Interfacce per accesso dati
   - Astrazione dal data layer specifico

2. **Factory Pattern**
   - Creazione provider AI basata su configurazione
   - Factory per agenti specializzati

3. **Strategy Pattern**
   - Strategie diverse per diversi provider AI
   - Selezione algoritmo embedding/chunking

4. **Dependency Injection**
   - Configurazione servizi tramite DI
   - Lifecycle management automatico

---

## Tecnologie Utilizzate

### Framework e Runtime
- **.NET 10.0**: Ultima versione LTS del framework Microsoft
  - **Novità**: Performance migliorata, nuove API, supporto C# 13
  - **Vantaggi**: Stabilità, long-term support, ecosystem maturo

### Librerie AI Core

#### 1. Microsoft Semantic Kernel (v1.29.0)
**Scopo**: Orchestrazione AI e gestione skill

**Funzionalità principali:**
- Orchestration di chiamate LLM multiple
- Memory management per conversazioni
- Plugin system estensibile
- Pianificazione automatica task complessi

**Utilizzo in DocN:**
```csharp
// Esempio configurazione kernel
var kernel = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(modelId, apiKey)
    .Build();

// Esempio invocazione con memoria
var chatHistory = new ChatHistory();
chatHistory.AddUserMessage(userInput);
var response = await kernel.InvokePromptAsync(prompt, arguments);
```

#### 2. Microsoft Semantic Kernel Agents (v1.29.0-alpha)
**Scopo**: Framework per agenti AI specializzati

**Caratteristiche:**
- Definizione agenti con personalità e expertise
- Coordinazione multi-agente
- Handoff tra agenti
- Tracciamento conversazioni

**Utilizzo in DocN:**
- Agenti specializzati per domini (legal, technical, support)
- Orchestrazione retrieval + synthesis
- Routing query a agente appropriato

#### 3. Microsoft Semantic Kernel Connectors OpenAI (v1.29.0)
**Scopo**: Connector per servizi OpenAI e compatibili

**Supporto:**
- OpenAI API diretta
- Azure OpenAI Service
- API compatibili (LocalAI, LM Studio, etc.)

### Librerie AI Provider

#### 4. Azure.AI.OpenAI (v2.1.0)
**Scopo**: SDK ufficiale Microsoft per Azure OpenAI

**Funzionalità:**
- Chat completions (GPT-4, GPT-3.5)
- Embeddings (text-embedding-ada-002, text-embedding-3)
- Streaming responses
- Token counting
- Error handling e retry logic

**Best practices:**
```csharp
// Gestione connection con retry policy
var client = new AzureOpenAIClient(
    new Uri(endpoint),
    new AzureKeyCredential(apiKey)
);

var chatClient = client.GetChatClient(deploymentName);
var response = await chatClient.CompleteChatAsync(messages);
```

#### 5. OpenAI SDK (v2.1.0)
**Scopo**: SDK per OpenAI API diretta

**Differenze con Azure:**
- Accesso a modelli più recenti (GPT-4 Turbo, etc.)
- Pricing diverso
- Non richiede Azure subscription

#### 6. Mscc.GenerativeAI (v2.1.0)
**Scopo**: SDK non ufficiale per Google Gemini API

**Funzionalità:**
- Accesso a Gemini Pro, Gemini Ultra
- Embeddings (text-embedding-004)
- Multimodal input (testo + immagini)
- Streaming e batch processing

**Caratteristiche Gemini:**
- Context window molto grande (fino a 1M tokens)
- Costo competitivo
- Ottimo per embeddings

### Librerie di Configurazione

#### 7. Microsoft.Extensions.*
Serie di package per configurazione e dependency injection:

- **Microsoft.Extensions.Configuration.Abstractions** (v9.0.1)
  - Interfacce per sistema configurazione
  - Binding configurazione a oggetti

- **Microsoft.Extensions.Configuration.Binder** (v9.0.1)
  - Binding automatico JSON/XML a classi C#
  - Validazione configurazione

- **Microsoft.Extensions.DependencyInjection.Abstractions** (v9.0.1)
  - Interfacce per Dependency Injection
  - Service lifetime management

- **Microsoft.Extensions.Logging.Abstractions** (v9.0.1)
  - Interfacce per logging strutturato
  - Log levels e scopes

- **Microsoft.Extensions.Options** (v9.0.1)
  - Pattern Options per configurazione tipizzata
  - Change tokens per reload configurazione runtime

### Vantaggi Stack Tecnologico

1. **Interoperabilità**
   - Tutti i provider condividono interfacce comuni
   - Facile switch tra provider senza modificare business logic

2. **Scalabilità**
   - Semantic Kernel gestisce carico e retry
   - Supporto async/await nativo

3. **Manutenibilità**
   - SDK ufficiali garantiscono aggiornamenti
   - Breaking changes minimizzati

4. **Performance**
   - .NET 10 offre ottimizzazioni significative
   - Async I/O per operazioni AI

5. **Ecosystem**
   - Community attiva
   - Documentazione estesa
   - Samples e best practices

---

## Struttura del Progetto

### Directory e File Principali

```
DocN.Core/
│
├── AI/                                    # Astrazioni AI
│   ├── AIConfiguration.cs                 # Configurazione provider AI
│   ├── AIProviderType.cs                  # Enum provider (Gemini, OpenAI, Azure)
│   ├── IMultiProviderAIService.cs         # Interfaccia servizio multi-provider
│   ├── IAIProvider.cs                     # Interfaccia base per provider
│   ├── ChatMessage.cs                     # Modello messaggio chat
│   └── EmbeddingOptions.cs                # Opzioni generazione embedding
│
├── SemanticKernel/                        # Integrazione Semantic Kernel
│   │
│   ├── Agents/                            # Framework agenti
│   │   ├── IAgentConfiguration.cs         # Configurazione agente
│   │   ├── AgentBase.cs                   # Classe base agenti
│   │   ├── AgentCapability.cs             # Enum capability agenti
│   │   └── AgentOrchestrator.cs           # Orchestratore multi-agente
│   │
│   ├── Memory/                            # Gestione memoria conversazionale
│   │   ├── IConversationMemory.cs         # Interfaccia memoria
│   │   ├── ConversationContext.cs         # Contesto conversazione
│   │   └── MemoryOptions.cs               # Opzioni memoria
│   │
│   ├── Plugins/                           # Plugin e skill Semantic Kernel
│   │   ├── DocumentRetrievalPlugin.cs     # Plugin retrieval documenti
│   │   ├── SummarizationPlugin.cs         # Plugin summarization
│   │   └── SemanticSearchPlugin.cs        # Plugin ricerca semantica
│   │
│   └── KernelExtensions.cs                # Extension per configurazione kernel
│
├── Interfaces/                            # Interfacce di servizio
│   ├── IEmbeddingService.cs               # Generazione embeddings
│   ├── IChunkingService.cs                # Chunking documenti
│   ├── IDocumentProcessor.cs              # Elaborazione documenti
│   ├── ISemanticRAGService.cs             # Servizio RAG semantico
│   ├── IOCRService.cs                     # Servizio OCR
│   ├── ISearchService.cs                  # Servizio ricerca
│   └── ICacheService.cs                   # Servizio caching
│
├── Extensions/                            # Extension methods
│   ├── ServiceCollectionExtensions.cs     # Extensions per DI
│   ├── ConfigurationExtensions.cs         # Extensions per configuration
│   ├── StringExtensions.cs                # Extensions per stringhe
│   └── EnumerableExtensions.cs            # Extensions per collections
│
└── DocN.Core.csproj                       # Project file
```

### Descrizione Componenti

#### AI/
Contiene tutte le astrazioni e configurazioni per i provider di intelligenza artificiale.

**Responsabilità:**
- Definizione interfacce provider-agnostic
- Modelli di configurazione AI
- Enumerazioni e costanti AI

**File chiave:**
- `IMultiProviderAIService.cs`: Interfaccia principale per servizi AI multi-provider
- `AIConfiguration.cs`: Classe configurazione con tutti i parametri AI

#### SemanticKernel/
Componenti per integrazione con Microsoft Semantic Kernel.

**Responsabilità:**
- Configurazione e setup Semantic Kernel
- Definizione agenti AI specializzati
- Gestione memoria conversazionale
- Plugin e skill custom

**Sottodirectory:**
- `Agents/`: Framework per agenti specializzati
- `Memory/`: Gestione contesto e cronologia
- `Plugins/`: Skill per retrieval, summarization, search

#### Interfaces/
Definizione di tutte le interfacce di servizio utilizzate nell'applicazione.

**Responsabilità:**
- Contratti per servizi AI
- Contratti per elaborazione documenti
- Contratti per ricerca e retrieval

**Principi:**
- Interface Segregation Principle
- Dependency Inversion Principle
- Interfacce piccole e focalizzate

#### Extensions/
Extension methods per semplificare configurazione e utilizzo.

**Responsabilità:**
- Setup Dependency Injection
- Configuration binding
- Helper methods comuni

---

## Componenti Principali

### 1. IMultiProviderAIService

**Scopo**: Interfaccia per servizio AI che supporta provider multipli.

**Responsabilità:**
- Gestire chiamate a diversi provider (Gemini, OpenAI, Azure)
- Routing richieste al provider corretto
- Fallback automatico in caso di errore
- Load balancing tra provider

**Metodi principali:**
```csharp
public interface IMultiProviderAIService
{
    // Genera embedding per testo
    Task<float[]> GenerateEmbeddingAsync(
        string text, 
        AIProviderType? provider = null
    );
    
    // Chat completion
    Task<string> GetChatCompletionAsync(
        List<ChatMessage> messages,
        AIProviderType? provider = null,
        double temperature = 0.7
    );
    
    // Estrazione tag con AI
    Task<List<string>> ExtractTagsAsync(
        string content,
        AIProviderType? provider = null
    );
    
    // Verifica disponibilità provider
    Task<bool> IsProviderAvailableAsync(AIProviderType provider);
    
    // Ottiene provider attivo per servizio
    AIProviderType GetActiveProvider(string serviceType);
}
```

**Output atteso:**
- Embedding: Array di float (dimensione 768 o 1536 in base al provider)
- Chat completion: Stringa con risposta generata
- Tag extraction: Lista di stringhe (tag estratti)
- Availability: Boolean (true se provider disponibile)

### 2. AIConfiguration

**Scopo**: Classe che rappresenta la configurazione completa di un provider AI.

**Proprietà principali:**
```csharp
public class AIConfiguration
{
    // Identificazione
    public int Id { get; set; }
    public string Name { get; set; }
    public AIProviderType ProviderType { get; set; }
    public bool IsActive { get; set; }
    
    // Configurazione Gemini
    public string? GeminiApiKey { get; set; }
    public string? GeminiModel { get; set; }
    public string? GeminiEmbeddingModel { get; set; }
    
    // Configurazione OpenAI
    public string? OpenAIApiKey { get; set; }
    public string? OpenAIModel { get; set; }
    public string? OpenAIEmbeddingModel { get; set; }
    
    // Configurazione Azure OpenAI
    public string? AzureOpenAIEndpoint { get; set; }
    public string? AzureOpenAIKey { get; set; }
    public string? AzureDeploymentName { get; set; }
    public string? EmbeddingDeploymentName { get; set; }
    
    // Parametri RAG
    public double SimilarityThreshold { get; set; } = 0.7;
    public int MaxDocumentsToRetrieve { get; set; } = 10;
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
    
    // Assegnazione servizi
    public bool UsedForChat { get; set; }
    public bool UsedForEmbeddings { get; set; }
    public bool UsedForTagExtraction { get; set; }
    public bool UsedForRAG { get; set; }
    
    // Metadata
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
}
```

**Utilizzo:**
- Persistita nel database (tabella AIConfigurations)
- Caricata all'avvio applicazione
- Modificabile via UI di configurazione
- Supporta hot-reload (cambio runtime)

### 3. Semantic Kernel Agents

**Scopo**: Framework per creare agenti AI specializzati.

**IAgentConfiguration:**
```csharp
public interface IAgentConfiguration
{
    string Name { get; }
    string Description { get; }
    string SystemPrompt { get; }
    AIProviderType Provider { get; }
    string ModelId { get; }
    double Temperature { get; }
    AgentCapability[] Capabilities { get; }
    string[] AllowedCategories { get; }
    string[] AllowedTags { get; }
}
```

**AgentCapability Enum:**
```csharp
public enum AgentCapability
{
    DocumentRetrieval,    // Recupero documenti
    Summarization,        // Riassunto
    QuestionAnswering,    // Q&A
    Comparison,           // Confronto documenti
    Extraction,           // Estrazione informazioni
    Translation,          // Traduzione
    Classification        // Classificazione
}
```

**AgentBase:**
Classe base per implementare agenti custom.

```csharp
public abstract class AgentBase
{
    protected IAgentConfiguration Config { get; }
    protected Kernel Kernel { get; }
    
    protected AgentBase(IAgentConfiguration config, Kernel kernel)
    {
        Config = config;
        Kernel = kernel;
    }
    
    // Metodo principale per processare richieste
    public abstract Task<string> ProcessAsync(
        string query,
        Dictionary<string, object>? context = null
    );
    
    // Verifica se l'agente può gestire la richiesta
    public virtual bool CanHandle(string query, string[]? categories = null)
    {
        // Implementazione default basata su categories e capabilities
    }
    
    // Ottiene documenti rilevanti per la query
    protected virtual async Task<List<Document>> RetrieveDocumentsAsync(
        string query,
        int maxResults = 10
    )
    {
        // Implementazione retrieval
    }
}
```

**Output atteso:**
- ProcessAsync: Stringa con risposta elaborata dall'agente
- CanHandle: Boolean indicante se agente può gestire query
- RetrieveDocumentsAsync: Lista documenti rilevanti

### 4. Document Processing Interfaces

**IChunkingService:**
```csharp
public interface IChunkingService
{
    /// <summary>
    /// Suddivide un documento in chunk ottimizzati per RAG
    /// </summary>
    /// <param name="content">Contenuto del documento</param>
    /// <param name="chunkSize">Dimensione target chunk in caratteri</param>
    /// <param name="overlap">Sovrapposizione tra chunk in caratteri</param>
    /// <returns>Lista di chunk con metadata</returns>
    Task<List<DocumentChunk>> ChunkDocumentAsync(
        string content,
        int chunkSize = 1000,
        int overlap = 200
    );
    
    /// <summary>
    /// Suddivide rispettando i confini di paragrafo/frase
    /// </summary>
    Task<List<DocumentChunk>> SmartChunkAsync(
        string content,
        int targetSize = 1000
    );
}

public class DocumentChunk
{
    public string Content { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

**IDocumentProcessor:**
```csharp
public interface IDocumentProcessor
{
    /// <summary>
    /// Estrae testo da documento
    /// </summary>
    /// <param name="filePath">Percorso file</param>
    /// <param name="fileType">Tipo file (PDF, DOCX, etc.)</param>
    /// <returns>Testo estratto</returns>
    Task<string> ExtractTextAsync(string filePath, string fileType);
    
    /// <summary>
    /// Processa documento completo: estrazione + chunking + embeddings
    /// </summary>
    Task<ProcessedDocument> ProcessDocumentAsync(
        Stream fileStream,
        string fileName,
        DocumentProcessingOptions options
    );
    
    /// <summary>
    /// Estrae metadata da documento
    /// </summary>
    Task<DocumentMetadata> ExtractMetadataAsync(string content);
}
```

**IOCRService:**
```csharp
public interface IOCRService
{
    /// <summary>
    /// Estrae testo da immagine usando OCR
    /// </summary>
    /// <param name="imagePath">Percorso immagine</param>
    /// <param name="language">Codice lingua (ita, eng, etc.)</param>
    /// <returns>Testo estratto</returns>
    Task<string> ExtractTextFromImageAsync(
        string imagePath,
        string language = "ita"
    );
    
    /// <summary>
    /// Verifica se OCR è disponibile
    /// </summary>
    bool IsAvailable();
}
```

### 5. Semantic RAG Service

**ISemanticRAGService:**
```csharp
public interface ISemanticRAGService
{
    /// <summary>
    /// Esegue query RAG sui documenti
    /// </summary>
    /// <param name="query">Query utente</param>
    /// <param name="chatHistory">Cronologia conversazione</param>
    /// <param name="options">Opzioni RAG</param>
    /// <returns>Risposta con citazioni</returns>
    Task<RAGResponse> QueryAsync(
        string query,
        List<ChatMessage>? chatHistory = null,
        RAGOptions? options = null
    );
    
    /// <summary>
    /// Query RAG su documenti specifici
    /// </summary>
    Task<RAGResponse> QueryDocumentsAsync(
        string query,
        int[] documentIds,
        RAGOptions? options = null
    );
}

public class RAGResponse
{
    // Risposta generata
    public string Answer { get; set; }
    
    // Documenti utilizzati
    public List<RetrievedDocument> Sources { get; set; }
    
    // Citazioni specifiche
    public List<Citation> Citations { get; set; }
    
    // Confidence score (0-1)
    public double Confidence { get; set; }
    
    // Token utilizzati
    public int TokensUsed { get; set; }
}

public class RetrievedDocument
{
    public int DocumentId { get; set; }
    public string Title { get; set; }
    public double RelevanceScore { get; set; }
    public string Excerpt { get; set; }
}

public class Citation
{
    public int DocumentId { get; set; }
    public string DocumentTitle { get; set; }
    public string Text { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
}
```

---

## Interfacce e Contratti

### Principi di Design

Le interfacce in DocN.Core seguono questi principi:

1. **Single Responsibility**
   - Ogni interfaccia ha una responsabilità ben definita
   - Metodi coesi e focalizzati

2. **Interface Segregation**
   - Interfacce piccole invece di "god interfaces"
   - Client dipendono solo da metodi che usano

3. **Dependency Inversion**
   - Dipendenze su astrazioni, non implementazioni
   - Permette mock e test unitari

4. **Async by Default**
   - Tutte le operazioni I/O sono async
   - Supporto cancellation tokens

### Pattern Comuni

**Async Methods:**
```csharp
Task<TResult> MethodAsync(params, CancellationToken cancellationToken = default);
```

**Options Pattern:**
```csharp
public class ServiceOptions
{
    public int Timeout { get; set; } = 30;
    public bool EnableCaching { get; set; } = true;
}

public interface IService
{
    Task<Result> ExecuteAsync(Request request, ServiceOptions? options = null);
}
```

**Result Pattern:**
```csharp
public class Result<T>
{
    public bool Success { get; set; }
    public T? Value { get; set; }
    public string? Error { get; set; }
    
    public static Result<T> Ok(T value) => new() { Success = true, Value = value };
    public static Result<T> Fail(string error) => new() { Success = false, Error = error };
}
```

---

## Integrazione con Altri Progetti

### Dipendenze

**DocN.Core → Nessuna dipendenza**
- Core è alla base della gerarchia
- Non referenzia altri progetti della soluzione
- Solo dipendenze NuGet esterne

**Altri progetti → DocN.Core**
- **DocN.Data**: Implementa interfacce di Core
- **DocN.Server**: Usa interfacce per dependency injection
- **DocN.Client**: Usa modelli e DTOs di Core

### Flow di Utilizzo

```
1. DocN.Core definisce IEmbeddingService

2. DocN.Data implementa EmbeddingService : IEmbeddingService

3. DocN.Server registra in DI:
   services.AddScoped<IEmbeddingService, EmbeddingService>();

4. Controller/Services usano via DI:
   public class DocumentsController
   {
       private readonly IEmbeddingService _embeddingService;
       
       public DocumentsController(IEmbeddingService embeddingService)
       {
           _embeddingService = embeddingService;
       }
   }
```

### Extension Methods per DI

**ServiceCollectionExtensions.cs:**
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDocNCoreServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurazione AI providers
        services.Configure<AIConfiguration>(
            configuration.GetSection("AI")
        );
        
        // Semantic Kernel
        services.AddSemanticKernel(configuration);
        
        return services;
    }
    
    public static IServiceCollection AddSemanticKernel(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<Kernel>(sp =>
        {
            var builder = Kernel.CreateBuilder();
            
            // Configurazione da appsettings
            var aiConfig = configuration.GetSection("AI").Get<AIConfiguration>();
            
            if (aiConfig?.ProviderType == AIProviderType.OpenAI)
            {
                builder.AddOpenAIChatCompletion(
                    aiConfig.OpenAIModel,
                    aiConfig.OpenAIApiKey
                );
            }
            // ... altri provider
            
            return builder.Build();
        });
        
        return services;
    }
}
```

---

## Per Analisti

### Cosa Offre DocN.Core?

DocN.Core è il **layer di astrazione** che permette all'applicazione di:

1. **Supportare Multiple AI**: Cambiare provider AI (Gemini ↔ OpenAI) senza modificare business logic
2. **Estendibilità**: Aggiungere nuovi provider o servizi senza riscrivere codice esistente
3. **Testabilità**: Mock delle interfacce per unit testing
4. **Configurabilità**: Tutto configurabile via file o database, no hardcoding

### Vantaggi Business

- **Riduzione Lock-in**: Non dipendenza da singolo vendor AI
- **Ottimizzazione Costi**: Possibilità di usare provider più economico per servizio specifico
- **Resilienza**: Fallback automatico se un provider non disponibile
- **Scalabilità**: Architettura supporta crescita e nuove funzionalità

---

## Per Sviluppatori

### Come Estendere DocN.Core

**Aggiungere Nuovo Provider AI:**

1. Aggiungere enum value in `AIProviderType`:
```csharp
public enum AIProviderType
{
    Gemini,
    OpenAI,
    AzureOpenAI,
    Anthropic  // Nuovo provider
}
```

2. Estendere `AIConfiguration` con proprietà specifiche:
```csharp
public string? AnthropicApiKey { get; set; }
public string? AnthropicModel { get; set; }
```

3. Implementare provider in DocN.Data

**Creare Nuovo Agente:**

1. Definire configurazione:
```csharp
public class LegalAgentConfiguration : IAgentConfiguration
{
    public string Name => "Legal Advisor";
    public string SystemPrompt => "You are a legal document specialist...";
    public AgentCapability[] Capabilities => new[]
    {
        AgentCapability.DocumentRetrieval,
        AgentCapability.Extraction,
        AgentCapability.Comparison
    };
}
```

2. Implementare agente:
```csharp
public class LegalAgent : AgentBase
{
    public LegalAgent(IAgentConfiguration config, Kernel kernel)
        : base(config, kernel) { }
    
    public override async Task<string> ProcessAsync(
        string query,
        Dictionary<string, object>? context = null)
    {
        // Implementazione specifica per legal domain
    }
}
```

### Best Practices

1. **Usa sempre interfacce** nei costruttori, mai classi concrete
2. **Async/await** per tutte le operazioni I/O
3. **CancellationToken** per operazioni lunghe
4. **Logging strutturato** con ILogger
5. **Options pattern** per configurazione
6. **Result pattern** per gestione errori

---

**Versione Documento**: 1.0  
**Data Aggiornamento**: Dicembre 2024  
**Autori**: Team DocN  
**Target Audience**: Analisti e Sviluppatori
