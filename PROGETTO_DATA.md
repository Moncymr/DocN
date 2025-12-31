# DocN.Data - Documentazione Tecnica

## Indice
1. [Panoramica Progetto](#panoramica-progetto)
2. [Scopo e Funzionalità](#scopo-e-funzionalità)
3. [Architettura](#architettura)
4. [Tecnologie Utilizzate](#tecnologie-utilizzate)
5. [Struttura del Progetto](#struttura-del-progetto)
6. [Componenti Principali](#componenti-principali)
7. [Database e Modelli](#database-e-modelli)
8. [Servizi Implementati](#servizi-implementati)

---

## Panoramica Progetto

**DocN.Data** è il progetto che costituisce il **Data Access Layer** e il **Service Layer** della soluzione DocN. Contiene tutte le implementazioni concrete per l'accesso ai dati, l'elaborazione documenti, i servizi AI e l'integrazione con provider esterni.

### Informazioni di Base
- **Tipo**: Class Library (.NET)
- **Target Framework**: .NET 10.0
- **Ruolo**: Data Layer + Service Layer + Infrastructure
- **Dipendenze**: DocN.Core

---

## Scopo e Funzionalità

### Scopo Principale

DocN.Data ha tre responsabilità principali:

1. **Data Access Layer (DAL)**
   - Gestione database tramite Entity Framework Core
   - Definizione modelli entità
   - Migrazioni database
   - Repository e Unit of Work pattern

2. **Service Layer**
   - Implementazione servizi business
   - Servizi AI (embeddings, chat, OCR)
   - Servizi elaborazione documenti
   - Servizi RAG (Retrieval-Augmented Generation)

3. **Infrastructure Services**
   - Integrazione provider esterni (Gemini, OpenAI, Azure)
   - File storage e processing
   - Caching e performance optimization
   - Background jobs e batch processing

### Funzionalità Specifiche

#### 1. Gestione Database
- **Entity Framework Core 10**: ORM per accesso dati
- **SQL Server 2025**: Database con supporto tipo VECTOR nativo
- **Migrations**: Gestione schema database versionato
- **Identity**: Autenticazione e autorizzazione utenti

#### 2. Elaborazione Documenti
- **Text Extraction**: Estrazione testo da PDF, DOCX, XLSX, TXT
- **OCR**: Tesseract per estrazione testo da immagini
- **Chunking**: Suddivisione intelligente documenti in chunk
- **Metadata Extraction**: AI-powered extraction di categorie e tag

#### 3. Servizi AI
- **Multi-Provider**: Supporto Gemini, OpenAI, Azure OpenAI
- **Embeddings**: Generazione vettori per ricerca semantica
- **Chat**: Conversazioni con LLM
- **RAG**: Retrieval-Augmented Generation completo

#### 4. Ricerca Avanzata
- **Semantic Search**: Ricerca basata su embeddings vettoriali
- **Full-Text Search**: Ricerca testuale su SQL Server
- **Hybrid Search**: Combinazione semantic + full-text con RRF
- **Re-Ranking**: Riordino risultati per massima rilevanza

#### 5. Servizi Avanzati RAG
- **HyDE**: Hypothetical Document Embeddings
- **Query Rewriting**: Riscrittura automatica query
- **Self-Query**: Estrazione filtri strutturati da query naturale
- **Multi-Query**: Generazione query multiple per coverage

---

## Architettura

### Principi Architetturali

DocN.Data implementa:

1. **Repository Pattern**
   - ApplicationDbContext come Unit of Work
   - DbSet come repository per entità

2. **Service Layer Pattern**
   - Servizi business separati da data access
   - Dependency injection per tutti i servizi

3. **Provider Pattern**
   - Implementazioni specifiche per provider AI
   - Factory per selezione provider runtime

4. **Strategy Pattern**
   - Strategie diverse per chunking (sentence, paragraph, semantic)
   - Strategie ricerca (semantic, fulltext, hybrid)

### Layers Architetturali

```
DocN.Data
├── Data Layer
│   ├── ApplicationDbContext       # EF Core DbContext
│   ├── Models/                   # Entità database
│   └── Migrations/               # Migration files
│
├── Service Layer
│   ├── Services/                 # Implementazioni servizi
│   │   ├── Document Processing
│   │   ├── AI Services
│   │   ├── RAG Services
│   │   └── Search Services
│   └── Utilities/                # Helper e utilities
│
└── Infrastructure Layer
    ├── File Storage
    ├── External APIs
    └── Background Jobs
```

---

## Tecnologie Utilizzate

### Database e ORM

#### 1. Microsoft.EntityFrameworkCore.SqlServer (v10.0.1)
**Scopo**: Object-Relational Mapper per SQL Server

**Funzionalità:**
- Mapping entità C# ↔ tabelle SQL
- LINQ per query database
- Change tracking automatico
- Migration management

**Utilizzo:**
```csharp
public class ApplicationDbContext : DbContext
{
    public DbSet<Document> Documents { get; set; }
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>()
            .HasIndex(d => d.Title);
    }
}
```

#### 2. Microsoft.AspNetCore.Identity.EntityFrameworkCore (v10.0.0)
**Scopo**: Sistema autenticazione e autorizzazione integrato

**Funzionalità:**
- Gestione utenti e password (hashing automatico)
- Ruoli e claims
- Two-factor authentication
- Password policy enforcement

**Vantaggi:**
- Security best practices integrate
- Estensibile per custom properties
- Multi-tenancy support

### Document Processing

#### 3. DocumentFormat.OpenXml (v3.2.0)
**Scopo**: Lettura/scrittura file Office Open XML

**Supporto formati:**
- DOCX (Word)
- XLSX (Excel)
- PPTX (PowerPoint)

**Utilizzo:**
```csharp
using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
{
    var body = doc.MainDocumentPart.Document.Body;
    string text = body.InnerText;
}
```

#### 4. itext7 (v9.0.0)
**Scopo**: Elaborazione file PDF

**Funzionalità:**
- Estrazione testo da PDF
- Lettura metadata PDF
- Gestione PDF complessi (multi-page, forms, etc.)

**Utilizzo:**
```csharp
using (PdfReader reader = new PdfReader(filePath))
using (PdfDocument pdf = new PdfDocument(reader))
{
    for (int i = 1; i <= pdf.GetNumberOfPages(); i++)
    {
        string text = PdfTextExtractor.GetTextFromPage(pdf.GetPage(i));
    }
}
```

#### 5. ClosedXML (v0.104.2)
**Scopo**: Manipolazione Excel files (alternativa a OpenXml)

**Vantaggi:**
- API più semplice di OpenXml
- Supporto formule e formattazione
- Migliori performance su file grandi

### OCR (Optical Character Recognition)

#### 6. Tesseract (v5.2.0)
**Scopo**: OCR open-source per estrazione testo da immagini

**Caratteristiche:**
- Supporto 100+ lingue
- Riconoscimento multi-lingua
- Layout analysis automatico
- Confidence scoring per carattere

**Utilizzo:**
```csharp
using (var engine = new TesseractEngine(@"./tessdata", "ita+eng", EngineMode.Default))
{
    using (var img = Pix.LoadFromFile(imagePath))
    {
        using (var page = engine.Process(img))
        {
            string text = page.GetText();
            float confidence = page.GetMeanConfidence();
        }
    }
}
```

**Performance:**
- Velocità: 1-3 secondi per immagine A4 300dpi
- Accuratezza: 90-98% su testo pulito
- Requisiti: Immagini ad alta risoluzione (min 300 DPI)

#### 7. SixLabors.ImageSharp (v3.1.12)
**Scopo**: Elaborazione immagini cross-platform

**Utilizzo con OCR:**
- Pre-processing immagini per OCR
- Miglioramento contrasto
- Deskew (raddrizzamento)
- Resize e crop

**Vantaggi:**
- Pure .NET (no native dependencies)
- Performance eccellenti
- API moderna e fluente

### AI e Semantic Kernel

#### 8. Microsoft.SemanticKernel (v1.29.0)
Già descritto in DocN.Core, qui utilizzato per:
- Implementazioni concrete plugin
- Orchestrazione workflow RAG
- Memory management

#### 9. Google Gemini (Mscc.GenerativeAI v2.1.0)
**Implementazione specifiche:**

```csharp
public class GeminiEmbeddingService : IEmbeddingService
{
    private readonly GenerativeModel _model;
    
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var result = await _model.EmbedContentAsync(text);
        return result.Embedding.Values.ToArray();
    }
}
```

**Caratteristiche Gemini:**
- **text-embedding-004**: 768 dimensioni, $0.00001 per 1K tokens
- **gemini-1.5-pro**: Context 1M tokens, multimodal
- **gemini-1.5-flash**: Veloce ed economico

#### 10. OpenAI & Azure OpenAI
**Implementazioni:**

```csharp
public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly OpenAIClient _client;
    
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var response = await _client.GetEmbeddingsAsync(
            new EmbeddingsOptions("text-embedding-3-large", new[] { text })
        );
        return response.Value.Data[0].Embedding.ToArray();
    }
}
```

**Modelli:**
- **text-embedding-3-large**: 3072 dimensioni (riducibile)
- **text-embedding-3-small**: 1536 dimensioni, più economico
- **text-embedding-ada-002**: Legacy, 1536 dimensioni

### Configuration e Utilities

#### 11. Microsoft.Extensions.* Suite
- **Hosting.Abstractions**: IHostedService per background jobs
- **Configuration.Binder**: Binding configurazione

**Esempio Background Service:**
```csharp
public class DocumentProcessingService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessPendingDocuments();
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

---

## Struttura del Progetto

```
DocN.Data/
│
├── ApplicationDbContext.cs              # EF Core DbContext principale
│
├── Models/                               # Entità database
│   ├── Document.cs                       # Entità documento
│   ├── DocumentChunk.cs                  # Chunk documento
│   ├── User.cs                           # Utente (extends IdentityUser)
│   ├── Organization.cs                   # Organizzazione
│   ├── Category.cs                       # Categoria
│   ├── Tag.cs                            # Tag
│   ├── Conversation.cs                   # Conversazione chat
│   ├── ConversationMessage.cs            # Messaggio chat
│   ├── AIConfiguration.cs                # Configurazione AI
│   ├── AgentConfiguration.cs             # Configurazione agente
│   └── AuditLog.cs                       # Log audit
│
├── Migrations/                           # EF Core migrations
│   ├── 20241201_Initial.cs
│   ├── 20241210_AddVectorSupport.cs
│   └── ...
│
├── Services/                             # Implementazioni servizi
│   │
│   ├── Document Processing/
│   │   ├── DocumentService.cs            # CRUD documenti
│   │   ├── FileProcessingService.cs      # Elaborazione file
│   │   ├── ChunkingService.cs            # Chunking documenti
│   │   └── CategoryService.cs            # Gestione categorie
│   │
│   ├── AI Services/
│   │   ├── MultiProviderAIService.cs     # Servizio multi-provider
│   │   ├── EmbeddingService.cs           # Generazione embeddings
│   │   ├── TesseractOCRService.cs        # Servizio OCR
│   │   └── BatchEmbeddingProcessor.cs    # Batch processing embeddings
│   │
│   ├── RAG Services/
│   │   ├── RAGService.cs                 # RAG base
│   │   ├── SemanticRAGService.cs         # RAG con Semantic Kernel
│   │   ├── ModernRAGService.cs           # RAG avanzato
│   │   ├── HyDEService.cs                # Hypothetical Document Embeddings
│   │   ├── QueryRewritingService.cs      # Riscrittura query
│   │   ├── SelfQueryService.cs           # Self-query parser
│   │   └── ReRankingService.cs           # Re-ranking risultati
│   │
│   ├── Search Services/
│   │   └── HybridSearchService.cs        # Ricerca ibrida
│   │
│   ├── Agent Services/
│   │   ├── AgentConfigurationService.cs  # Gestione agenti
│   │   ├── AgentTemplateSeeder.cs        # Template predefiniti
│   │   └── Agents/                       # Agenti specifici
│   │       ├── CustomerSupportAgent.cs
│   │       ├── LegalAnalyzerAgent.cs
│   │       └── TechnicalDocsAgent.cs
│   │
│   ├── Infrastructure Services/
│   │   ├── CacheService.cs               # Caching
│   │   ├── AuditService.cs               # Audit logging
│   │   ├── LogService.cs                 # Application logging
│   │   ├── ApplicationSeeder.cs          # Data seeding
│   │   └── DocumentStatisticsService.cs  # Statistiche
│   │
│   └── Interfaces/
│       └── (Interfaces da DocN.Core)
│
├── Utilities/                            # Helper e utilities
│   ├── VectorHelper.cs                   # Operazioni su vettori
│   ├── TextHelper.cs                     # Elaborazione testo
│   └── FileHelper.cs                     # Gestione file
│
└── DocN.Data.csproj                      # Project file
```

---

## Database e Modelli

### Schema Database

**Tabelle Principali:**

#### 1. Documents
```sql
CREATE TABLE Documents (
    Id INT PRIMARY KEY IDENTITY,
    Title NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX),
    FileName NVARCHAR(500),
    FilePath NVARCHAR(1000),
    FileSize BIGINT,
    ContentType NVARCHAR(200),
    
    -- Contenuto estratto
    ExtractedContent NVARCHAR(MAX),
    
    -- Embeddings vettoriali
    EmbeddingVector768 VECTOR(768),        -- Gemini
    EmbeddingVector1536 VECTOR(1536),      -- OpenAI
    
    -- Metadata
    CategoryId INT,
    Tags NVARCHAR(MAX),                    -- JSON array
    Visibility INT,                         -- Enum: Private, Shared, Org, Public
    
    -- Multi-tenancy
    OrganizationId INT,
    OwnerId NVARCHAR(450),
    
    -- Audit
    UploadedAt DATETIME2 DEFAULT GETDATE(),
    UploadedBy NVARCHAR(450),
    UpdatedAt DATETIME2,
    IsDeleted BIT DEFAULT 0,
    
    CONSTRAINT FK_Documents_Categories FOREIGN KEY (CategoryId) 
        REFERENCES Categories(Id),
    CONSTRAINT FK_Documents_Organizations FOREIGN KEY (OrganizationId) 
        REFERENCES Organizations(Id),
    CONSTRAINT FK_Documents_Users FOREIGN KEY (OwnerId) 
        REFERENCES AspNetUsers(Id)
);

-- Indici per performance
CREATE INDEX IX_Documents_OrganizationId ON Documents(OrganizationId);
CREATE INDEX IX_Documents_CategoryId ON Documents(CategoryId);
CREATE FULLTEXT INDEX ON Documents(ExtractedContent, Title, Description);
```

**Nota importante**: SQL Server 2025 supporta nativamente il tipo `VECTOR` per ricerca semantica efficiente.

#### 2. DocumentChunks
```sql
CREATE TABLE DocumentChunks (
    Id INT PRIMARY KEY IDENTITY,
    DocumentId INT NOT NULL,
    
    -- Contenuto chunk
    Content NVARCHAR(MAX) NOT NULL,
    ChunkIndex INT NOT NULL,
    
    -- Embeddings
    EmbeddingVector768 VECTOR(768),
    EmbeddingVector1536 VECTOR(1536),
    
    -- Metadata chunk
    StartIndex INT,
    EndIndex INT,
    TokenCount INT,
    
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_DocumentChunks_Documents FOREIGN KEY (DocumentId) 
        REFERENCES Documents(Id) ON DELETE CASCADE
);

CREATE INDEX IX_DocumentChunks_DocumentId ON DocumentChunks(DocumentId);
```

#### 3. AIConfigurations
```sql
CREATE TABLE AIConfigurations (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(200) NOT NULL,
    ProviderType INT NOT NULL,              -- Enum: Gemini, OpenAI, Azure
    IsActive BIT NOT NULL DEFAULT 0,
    
    -- Provider-specific keys
    GeminiApiKey NVARCHAR(500),
    GeminiModel NVARCHAR(200),
    GeminiEmbeddingModel NVARCHAR(200),
    
    OpenAIApiKey NVARCHAR(500),
    OpenAIModel NVARCHAR(200),
    OpenAIEmbeddingModel NVARCHAR(200),
    
    AzureOpenAIEndpoint NVARCHAR(500),
    AzureOpenAIKey NVARCHAR(500),
    AzureDeploymentName NVARCHAR(200),
    EmbeddingDeploymentName NVARCHAR(200),
    
    -- RAG parameters
    SimilarityThreshold FLOAT DEFAULT 0.7,
    MaxDocumentsToRetrieve INT DEFAULT 10,
    ChunkSize INT DEFAULT 1000,
    ChunkOverlap INT DEFAULT 200,
    
    -- Service assignments
    UsedForChat BIT DEFAULT 0,
    UsedForEmbeddings BIT DEFAULT 0,
    UsedForTagExtraction BIT DEFAULT 0,
    UsedForRAG BIT DEFAULT 0,
    
    -- Audit
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy NVARCHAR(450),
    UpdatedAt DATETIME2,
    
    OrganizationId INT,
    CONSTRAINT FK_AIConfigurations_Organizations FOREIGN KEY (OrganizationId) 
        REFERENCES Organizations(Id)
);
```

#### 4. Conversations & Messages
```sql
CREATE TABLE Conversations (
    Id INT PRIMARY KEY IDENTITY,
    Title NVARCHAR(500),
    UserId NVARCHAR(450) NOT NULL,
    OrganizationId INT NOT NULL,
    
    -- RAG configuration snapshot
    RAGConfigurationSnapshot NVARCHAR(MAX),  -- JSON
    
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    LastMessageAt DATETIME2,
    IsActive BIT DEFAULT 1,
    
    CONSTRAINT FK_Conversations_Users FOREIGN KEY (UserId) 
        REFERENCES AspNetUsers(Id),
    CONSTRAINT FK_Conversations_Organizations FOREIGN KEY (OrganizationId) 
        REFERENCES Organizations(Id)
);

CREATE TABLE ConversationMessages (
    Id INT PRIMARY KEY IDENTITY,
    ConversationId INT NOT NULL,
    
    Role NVARCHAR(50) NOT NULL,             -- User, Assistant, System
    Content NVARCHAR(MAX) NOT NULL,
    
    -- Metadata
    TokensUsed INT,
    DocumentsUsed NVARCHAR(MAX),            -- JSON array di IDs
    SourceCitations NVARCHAR(MAX),          -- JSON array
    
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    
    CONSTRAINT FK_ConversationMessages_Conversations FOREIGN KEY (ConversationId) 
        REFERENCES Conversations(Id) ON DELETE CASCADE
);
```

### Modelli EF Core

**Document.cs:**
```csharp
public class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    
    // Contenuto estratto
    public string? ExtractedContent { get; set; }
    
    // Embeddings - SQL Server 2025 native VECTOR type
    [Column(TypeName = "VECTOR(768)")]
    public float[]? EmbeddingVector768 { get; set; }
    
    [Column(TypeName = "VECTOR(1536)")]
    public float[]? EmbeddingVector1536 { get; set; }
    
    // Relationships
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    
    public string OwnerId { get; set; } = string.Empty;
    public User Owner { get; set; } = null!;
    
    // Collections
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    
    // Metadata
    public DocumentVisibility Visibility { get; set; }
    public string? Tags { get; set; }  // JSON serialized
    
    // Audit
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public enum DocumentVisibility
{
    Private = 0,      // Solo owner
    Shared = 1,       // Condiviso con utenti specifici
    Organization = 2, // Tutta l'organizzazione
    Public = 3        // Pubblico
}
```

**AIConfiguration.cs:**
```csharp
public class AIConfiguration
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AIProviderType ProviderType { get; set; }
    public bool IsActive { get; set; }
    
    // Gemini
    public string? GeminiApiKey { get; set; }
    public string? GeminiModel { get; set; }
    public string? GeminiEmbeddingModel { get; set; }
    
    // OpenAI
    public string? OpenAIApiKey { get; set; }
    public string? OpenAIModel { get; set; }
    public string? OpenAIEmbeddingModel { get; set; }
    
    // Azure OpenAI
    public string? AzureOpenAIEndpoint { get; set; }
    public string? AzureOpenAIKey { get; set; }
    public string? AzureDeploymentName { get; set; }
    public string? EmbeddingDeploymentName { get; set; }
    
    // RAG Configuration
    public double SimilarityThreshold { get; set; } = 0.7;
    public int MaxDocumentsToRetrieve { get; set; } = 10;
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
    
    // Service Assignment
    public bool UsedForChat { get; set; }
    public bool UsedForEmbeddings { get; set; }
    public bool UsedForTagExtraction { get; set; }
    public bool UsedForRAG { get; set; }
    
    // Audit
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Multi-tenancy
    public int OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
}
```

---

## Servizi Implementati

### 1. MultiProviderAIService

**Scopo:** Gestisce chiamate a diversi provider AI con fallback automatico.

**Funzionalità:**
- Routing chiamate al provider corretto
- Fallback se provider primario non disponibile
- Load balancing tra provider
- Caching configurazioni

**Implementazione chiave:**
```csharp
public class MultiProviderAIService : IMultiProviderAIService
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MultiProviderAIService> _logger;
    
    /// <summary>
    /// Genera embedding per testo utilizzando il provider configurato
    /// </summary>
    /// <param name="text">Testo da convertire in embedding</param>
    /// <param name="provider">Provider specifico (optional, usa default se null)</param>
    /// <returns>Array di float rappresentante l'embedding vettoriale</returns>
    /// <output>Float array di 768 dimensioni (Gemini) o 1536 (OpenAI)</output>
    public async Task<float[]> GenerateEmbeddingAsync(
        string text, 
        AIProviderType? provider = null)
    {
        // Determina provider da usare
        var activeProvider = provider ?? GetActiveProviderForService("Embeddings");
        
        // Ottiene configurazione provider
        var config = await GetProviderConfigurationAsync(activeProvider);
        
        try
        {
            return activeProvider switch
            {
                AIProviderType.Gemini => await GenerateGeminiEmbeddingAsync(text, config),
                AIProviderType.OpenAI => await GenerateOpenAIEmbeddingAsync(text, config),
                AIProviderType.AzureOpenAI => await GenerateAzureEmbeddingAsync(text, config),
                _ => throw new NotSupportedException($"Provider {activeProvider} not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding with {Provider}", activeProvider);
            
            // Fallback a provider alternativo
            if (provider == null)  // Solo se non era specificato
            {
                return await TryFallbackProviderAsync(text, activeProvider);
            }
            
            throw;
        }
    }
}
```

**Output atteso:** Array di float (embedding vettoriale) con dimensione dipendente dal provider.

### 2. ChunkingService

**Scopo:** Suddivide documenti in chunk ottimizzati per RAG.

**Strategie implementate:**
1. **Fixed Size**: Chunk di dimensione fissa con overlap
2. **Sentence Boundary**: Rispetta i confini di frase
3. **Paragraph Boundary**: Rispetta i confini di paragrafo
4. **Semantic**: Chunking basato su similarità semantica

**Implementazione:**
```csharp
public class ChunkingService : IChunkingService
{
    /// <summary>
    /// Suddivide documento in chunk con strategia sentence-aware
    /// </summary>
    /// <param name="content">Contenuto del documento</param>
    /// <param name="chunkSize">Dimensione target del chunk in caratteri</param>
    /// <param name="overlap">Numero di caratteri di sovrapposizione tra chunk</param>
    /// <returns>Lista di DocumentChunk con contenuto e metadata</returns>
    /// <output>Lista di chunk, ognuno con Content, StartIndex, EndIndex</output>
    public async Task<List<DocumentChunk>> ChunkDocumentAsync(
        string content,
        int chunkSize = 1000,
        int overlap = 200)
    {
        var chunks = new List<DocumentChunk>();
        
        // Split in frasi
        var sentences = SplitIntoSentences(content);
        
        var currentChunk = new StringBuilder();
        int startIndex = 0;
        int chunkIndex = 0;
        
        foreach (var sentence in sentences)
        {
            // Se aggiungere frase supera limite, crea nuovo chunk
            if (currentChunk.Length + sentence.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    Content = currentChunk.ToString(),
                    ChunkIndex = chunkIndex++,
                    StartIndex = startIndex,
                    EndIndex = startIndex + currentChunk.Length,
                    TokenCount = EstimateTokenCount(currentChunk.ToString())
                });
                
                // Overlap: mantieni ultime frasi per contesto
                var overlapText = GetOverlapText(currentChunk.ToString(), overlap);
                currentChunk.Clear();
                currentChunk.Append(overlapText);
                
                startIndex += currentChunk.Length - overlapText.Length;
            }
            
            currentChunk.Append(sentence);
        }
        
        // Ultimo chunk
        if (currentChunk.Length > 0)
        {
            chunks.Add(new DocumentChunk
            {
                Content = currentChunk.ToString(),
                ChunkIndex = chunkIndex,
                StartIndex = startIndex,
                EndIndex = startIndex + currentChunk.Length,
                TokenCount = EstimateTokenCount(currentChunk.ToString())
            });
        }
        
        return chunks;
    }
    
    /// <summary>
    /// Split testo in frasi usando regex
    /// </summary>
    private List<string> SplitIntoSentences(string text)
    {
        // Regex per identificare fine frase (., !, ?)
        var regex = new Regex(@"(?<=[.!?])\s+(?=[A-Z])");
        return regex.Split(text).ToList();
    }
}
```

**Output atteso:** Lista di chunk con proprietà Content, StartIndex, EndIndex, TokenCount.

### 3. HybridSearchService

**Scopo:** Implementa ricerca ibrida (semantic + full-text) con Reciprocal Rank Fusion.

**Algoritmo:**
```csharp
public class HybridSearchService
{
    /// <summary>
    /// Esegue ricerca ibrida combinando semantic search e full-text search
    /// </summary>
    /// <param name="query">Query di ricerca</param>
    /// <param name="topK">Numero massimo di risultati</param>
    /// <returns>Lista di documenti ordinati per rilevanza ibrida</returns>
    /// <output>Lista documenti con score ibrido calcolato tramite RRF</output>
    public async Task<List<SearchResult>> HybridSearchAsync(
        string query,
        int topK = 10)
    {
        // 1. Semantic search con embeddings
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
        var semanticResults = await VectorSearchAsync(queryEmbedding, topK * 2);
        
        // 2. Full-text search con SQL Server FTS
        var fullTextResults = await FullTextSearchAsync(query, topK * 2);
        
        // 3. Reciprocal Rank Fusion (RRF)
        var hybridScores = CalculateRRFScores(
            semanticResults, 
            fullTextResults, 
            k: 60  // Parametro RRF standard
        );
        
        // 4. Ordina per score ibrido e prendi top K
        return hybridScores
            .OrderByDescending(r => r.HybridScore)
            .Take(topK)
            .ToList();
    }
    
    /// <summary>
    /// Calcola score ibrido usando Reciprocal Rank Fusion
    /// Formula: RRF(d) = Σ 1/(k + rank_i(d))
    /// </summary>
    private Dictionary<int, SearchResult> CalculateRRFScores(
        List<SearchResult> semantic,
        List<SearchResult> fullText,
        int k = 60)
    {
        var results = new Dictionary<int, SearchResult>();
        
        // Aggiungi score da semantic search
        for (int i = 0; i < semantic.Count; i++)
        {
            var doc = semantic[i];
            var rrfScore = 1.0 / (k + i + 1);
            
            if (!results.ContainsKey(doc.DocumentId))
            {
                results[doc.DocumentId] = doc;
                results[doc.DocumentId].HybridScore = 0;
            }
            
            results[doc.DocumentId].HybridScore += rrfScore;
            results[doc.DocumentId].SemanticScore = doc.SemanticScore;
        }
        
        // Aggiungi score da full-text search
        for (int i = 0; i < fullText.Count; i++)
        {
            var doc = fullText[i];
            var rrfScore = 1.0 / (k + i + 1);
            
            if (!results.ContainsKey(doc.DocumentId))
            {
                results[doc.DocumentId] = doc;
                results[doc.DocumentId].HybridScore = 0;
            }
            
            results[doc.DocumentId].HybridScore += rrfScore;
            results[doc.DocumentId].FullTextScore = doc.FullTextScore;
        }
        
        return results;
    }
}
```

**Output atteso:** Lista `SearchResult` con score ibrido, semantico e full-text.

### 4. TesseractOCRService

**Scopo:** Estrazione testo da immagini usando Tesseract OCR.

**Implementazione:**
```csharp
public class TesseractOCRService : IOCRService
{
    private readonly string _tessDataPath;
    private readonly ILogger<TesseractOCRService> _logger;
    
    /// <summary>
    /// Estrae testo da file immagine usando Tesseract OCR
    /// </summary>
    /// <param name="imagePath">Percorso completo del file immagine</param>
    /// <param name="language">Codice lingua per OCR (es: "ita", "eng")</param>
    /// <returns>Testo estratto dall'immagine</returns>
    /// <output>Stringa con testo riconosciuto, vuota se nessun testo trovato</output>
    public async Task<string> ExtractTextFromImageAsync(
        string imagePath,
        string language = "ita")
    {
        try
        {
            // Pre-processing immagine per migliorare OCR
            using var processedImage = await PreProcessImageAsync(imagePath);
            
            // Inizializza Tesseract engine
            using var engine = new TesseractEngine(_tessDataPath, language, EngineMode.Default);
            
            // Configura parametri OCR
            engine.SetVariable("tessedit_char_whitelist", 
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,;:!?()[]{}\"'-/");
            
            // Processa immagine
            using var page = engine.Process(processedImage);
            
            var text = page.GetText();
            var confidence = page.GetMeanConfidence();
            
            _logger.LogInformation(
                "OCR completed for {ImagePath}. Confidence: {Confidence}%", 
                imagePath, 
                confidence * 100
            );
            
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing OCR on {ImagePath}", imagePath);
            throw;
        }
    }
    
    /// <summary>
    /// Pre-elabora immagine per migliorare accuratezza OCR
    /// </summary>
    private async Task<Pix> PreProcessImageAsync(string imagePath)
    {
        using var image = await Image.LoadAsync<Rgb24>(imagePath);
        
        // 1. Converti in grayscale
        image.Mutate(x => x.Grayscale());
        
        // 2. Aumenta contrasto
        image.Mutate(x => x.Contrast(1.5f));
        
        // 3. Applica threshold per binarizzazione
        image.Mutate(x => x.BinaryThreshold(0.5f));
        
        // Salva temporaneamente e carica in Tesseract
        var tempPath = Path.GetTempFileName();
        await image.SaveAsync(tempPath);
        
        return Pix.LoadFromFile(tempPath);
    }
    
    /// <summary>
    /// Verifica se Tesseract è configurato correttamente
    /// </summary>
    public bool IsAvailable()
    {
        return Directory.Exists(_tessDataPath) && 
               Directory.GetFiles(_tessDataPath, "*.traineddata").Any();
    }
}
```

**Output atteso:** Stringa con testo estratto, confidence score nel log.

### 5. SemanticRAGService

**Scopo:** Implementa RAG (Retrieval-Augmented Generation) completo con Semantic Kernel.

**Workflow:**
```csharp
public class SemanticRAGService : ISemanticRAGService
{
    /// <summary>
    /// Esegue query RAG: retrieval + generation con citazioni
    /// </summary>
    /// <param name="query">Query utente in linguaggio naturale</param>
    /// <param name="chatHistory">Cronologia conversazione per contesto</param>
    /// <param name="options">Opzioni RAG (threshold, max docs, etc.)</param>
    /// <returns>Risposta generata con documenti fonte e citazioni</returns>
    /// <output>RAGResponse con Answer, Sources, Citations, Confidence</output>
    public async Task<RAGResponse> QueryAsync(
        string query,
        List<ChatMessage>? chatHistory = null,
        RAGOptions? options = null)
    {
        options ??= new RAGOptions();
        
        // STEP 1: Query Rewriting (se abilitato)
        var processedQuery = query;
        if (options.EnableQueryRewriting)
        {
            processedQuery = await _queryRewritingService.RewriteQueryAsync(
                query, 
                chatHistory
            );
            _logger.LogInformation("Query rewritten: {Original} -> {Rewritten}", 
                query, processedQuery);
        }
        
        // STEP 2: Retrieval - trova documenti rilevanti
        var retrievedDocs = await RetrieveDocumentsAsync(
            processedQuery,
            options.MaxDocuments,
            options.SimilarityThreshold
        );
        
        if (!retrievedDocs.Any())
        {
            return new RAGResponse
            {
                Answer = "Non ho trovato documenti rilevanti per rispondere alla tua domanda.",
                Sources = new List<RetrievedDocument>(),
                Confidence = 0.0
            };
        }
        
        // STEP 3: Re-Ranking (se abilitato)
        if (options.EnableReRanking)
        {
            retrievedDocs = await _reRankingService.ReRankAsync(
                processedQuery,
                retrievedDocs
            );
        }
        
        // STEP 4: Costruisce contesto per LLM
        var context = BuildContext(retrievedDocs);
        
        // STEP 5: Generation - genera risposta con Semantic Kernel
        var prompt = BuildRAGPrompt(query, context, chatHistory);
        
        var response = await _kernel.InvokePromptAsync(
            prompt,
            new KernelArguments
            {
                ["query"] = query,
                ["context"] = context,
                ["history"] = chatHistory != null ? 
                    string.Join("\n", chatHistory.Select(m => $"{m.Role}: {m.Content}")) : ""
            }
        );
        
        var answer = response.ToString();
        
        // STEP 6: Estrae citazioni dalla risposta
        var citations = ExtractCitations(answer, retrievedDocs);
        
        // STEP 7: Calcola confidence score
        var confidence = CalculateConfidence(retrievedDocs, answer);
        
        return new RAGResponse
        {
            Answer = answer,
            Sources = retrievedDocs.Select(d => new RetrievedDocument
            {
                DocumentId = d.Id,
                Title = d.Title,
                RelevanceScore = d.RelevanceScore,
                Excerpt = d.Excerpt
            }).ToList(),
            Citations = citations,
            Confidence = confidence,
            TokensUsed = EstimateTokens(prompt + answer)
        };
    }
    
    /// <summary>
    /// Costruisce prompt RAG con template
    /// </summary>
    private string BuildRAGPrompt(
        string query, 
        string context, 
        List<ChatMessage>? history)
    {
        return $@"
Sei un assistente AI che risponde a domande basandoti ESCLUSIVAMENTE sui documenti forniti.

ISTRUZIONI:
1. Rispondi SOLO basandoti sul contesto fornito
2. Se il contesto non contiene informazioni sufficienti, dillo chiaramente
3. Cita sempre la fonte (usa [Doc ID] per riferimento)
4. Sii preciso e conciso
5. Mantieni il tono professionale

CONTESTO:
{context}

{(history != null && history.Any() ? $@"
CRONOLOGIA CONVERSAZIONE:
{string.Join("\n", history.Select(m => $"{m.Role}: {m.Content}"))}" : "")}

DOMANDA: {query}

RISPOSTA:";
    }
}
```

**Output atteso:** `RAGResponse` completo con risposta, fonti, citazioni e confidence.

---

## Per Analisti

### Cosa Offre DocN.Data?

DocN.Data è il **motore operativo** dell'applicazione che:

1. **Gestisce i Dati**: Tutta la persistenza e accesso al database
2. **Elabora Documenti**: Upload → Estrazione testo → OCR → Chunking → Embeddings
3. **Implementa AI**: Connessioni reali a Gemini, OpenAI, Azure
4. **Fornisce RAG**: Ricerca semantica e generazione risposte contestuali

### Value Proposition

- **Flessibilità Provider**: Cambio provider AI senza impatto su utenti
- **Performance**: Caching, batch processing, query ottimizzate
- **Scalabilità**: Architettura pronta per crescita (background jobs, async)
- **Manutenibilità**: Servizi ben separati, testabili singolarmente

---

## Per Sviluppatori

### Come Estendere DocN.Data

**Aggiungere Nuovo Formato Documento:**

1. Implementare estrattore in `FileProcessingService`:
```csharp
public async Task<string> ExtractMarkdownAsync(string filePath)
{
    var text = await File.ReadAllTextAsync(filePath);
    // Parsing markdown → plain text
    return ConvertMarkdownToPlainText(text);
}
```

2. Registrare nel factory:
```csharp
public async Task<string> ExtractTextAsync(string filePath, string extension)
{
    return extension.ToLower() switch
    {
        ".pdf" => await ExtractPdfAsync(filePath),
        ".docx" => await ExtractDocxAsync(filePath),
        ".md" => await ExtractMarkdownAsync(filePath),  // Nuovo
        _ => throw new NotSupportedException()
    };
}
```

**Aggiungere Nuova Strategia Chunking:**

```csharp
public async Task<List<DocumentChunk>> SemanticChunkingAsync(string content)
{
    // 1. Genera embeddings per frasi
    var sentences = SplitIntoSentences(content);
    var embeddings = await GenerateEmbeddingsForSentencesAsync(sentences);
    
    // 2. Raggruppa frasi semanticamente simili
    var groups = ClusterBySimilarity(sentences, embeddings, threshold: 0.8);
    
    // 3. Crea chunk dai gruppi
    return groups.Select((g, i) => new DocumentChunk
    {
        Content = string.Join(" ", g),
        ChunkIndex = i
    }).ToList();
}
```

### Best Practices

1. **Async/await everywhere** per I/O operations
2. **Using statements** per IDisposable (EF context, file streams)
3. **Try-catch** con logging per operazioni esterne (AI APIs)
4. **Transactions** per operazioni multi-step critiche
5. **Migrations** per ogni cambio schema database

---

**Versione Documento**: 1.0  
**Data Aggiornamento**: Dicembre 2024  
**Autori**: Team DocN  
**Target Audience**: Analisti e Sviluppatori
