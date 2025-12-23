# 🏢 DocN - Sistema RAG Aziendale con SQL Server 2025

## 🎯 Visione e Obiettivi

### Scopo del Sistema
**Sistema RAG (Retrieval Augmented Generation) interno aziendale** che permette ai dipendenti di:
- 📄 Caricare documenti aziendali (contratti, manuali, report, policy)
- 🔍 Cercare informazioni usando linguaggio naturale
- 💬 Ottenere risposte precise basate sui documenti aziendali
- 🔐 Mantenere sicurezza e privacy dei dati (tutto on-premise)
- 📊 Tracciare l'uso e migliorare continuamente

### Vantaggi Chiave SQL Server 2025
- ✅ Tipo VECTOR nativo per embeddings
- ✅ Ricerca vettoriale ultra-veloce
- ✅ Tutto on-premise (nessun dato esce dall'azienda)
- ✅ Integrazione con ecosistema Microsoft
- ✅ Scalabilità enterprise

---

## 💡 Idee Innovative per Migliorare il Sistema

### 🚀 PRIORITÀ 1: Funzionalità Core Mancanti

#### 1.1 **Multi-Tenancy e Dipartimenti**
**Problema attuale:** Sistema flat senza separazione organizzativa

**Soluzione:**
```csharp
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } // "HR", "Legal", "IT", "Finance"
    public string? ParentDepartmentId { get; set; } // Struttura gerarchica
    public List<ApplicationUser> Members { get; set; }
    public List<Document> Documents { get; set; }
}

public class Document
{
    // ... campi esistenti ...
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    
    // Visibilità avanzata
    public List<Department> AccessibleByDepartments { get; set; }
}
```

**Vantaggi:**
- 🔒 Documenti HR visibili solo a HR
- 📊 Report per dipartimento
- 🔐 Controllo accessi granulare

#### 1.2 **Versioning Documenti**
**Problema attuale:** Nessuna gestione versioni

**Soluzione:**
```csharp
public class DocumentVersion
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string FilePath { get; set; }
    public float[]? EmbeddingVector { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedBy { get; set; }
    public string? ChangeNotes { get; set; }
    public bool IsActive { get; set; }
}
```

**Funzionalità:**
- 📝 Traccia tutte le modifiche
- 🔄 Rollback a versioni precedenti
- 📊 Confronto versioni (diff)
- 🔍 Ricerca in versioni specifiche

#### 1.3 **Pipeline OCR Avanzata**
**Problema attuale:** Estrazione testo basilare

**Soluzione:**
```csharp
public interface IOCRService
{
    Task<ExtractedContent> ExtractAsync(Stream fileStream, string fileName);
}

public class ExtractedContent
{
    public string Text { get; set; }
    public List<ExtractedTable> Tables { get; set; }
    public List<ExtractedImage> Images { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public List<ExtractedEntity> Entities { get; set; } // NER: nomi, date, importi
}

public class AdvancedOCRService : IOCRService
{
    // Supporto formati
    - PDF (anche scansionati)
    - Word, Excel, PowerPoint
    - Immagini (PNG, JPG) con Azure Computer Vision
    - Email (MSG, EML)
    - Scansioni multi-pagina
    
    // Estrazione avanzata
    - Layout preservation
    - Table detection
    - Form recognition
    - Handwriting recognition
}
```

**Integrazione suggerita:**
- Azure Document Intelligence (Form Recognizer)
- Tesseract OCR per immagini
- Apache Tika per formati ufficio

#### 1.4 **Ricerca Ibrida (Vector + Full-Text)**
**Problema attuale:** Solo ricerca vettoriale

**Soluzione:**
```csharp
public class HybridSearchService
{
    public async Task<List<Document>> SearchAsync(
        string query,
        SearchOptions options)
    {
        // 1. Genera embedding per query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
        
        // 2. Ricerca vettoriale (semantica)
        var vectorResults = await VectorSearch(queryEmbedding, options.TopK * 2);
        
        // 3. Full-text search (keyword)
        var textResults = await FullTextSearch(query, options.TopK * 2);
        
        // 4. Metadata filters
        var filteredVector = ApplyFilters(vectorResults, options.Filters);
        var filteredText = ApplyFilters(textResults, options.Filters);
        
        // 5. Reciprocal Rank Fusion
        var merged = MergeWithRRF(filteredVector, filteredText);
        
        return merged.Take(options.TopK).ToList();
    }
}
```

**SQL Server 2025:**
```sql
-- Full-text search
CREATE FULLTEXT INDEX ON Documents(ExtractedText, FileName)
    KEY INDEX PK_Documents;

-- Vector search + Full-text combinati
WITH VectorSearch AS (
    SELECT 
        Id,
        FileName,
        VECTOR_DISTANCE('cosine', EmbeddingVector, @queryVector) as VectorScore
    FROM Documents
    WHERE VECTOR_DISTANCE('cosine', EmbeddingVector, @queryVector) > 0.7
),
TextSearch AS (
    SELECT 
        Id,
        FileName,
        RANK as TextScore
    FROM Documents
    WHERE CONTAINS(ExtractedText, @queryText)
)
SELECT 
    d.*,
    COALESCE(v.VectorScore * 0.6, 0) + COALESCE(t.TextScore * 0.4, 0) as CombinedScore
FROM Documents d
LEFT JOIN VectorSearch v ON d.Id = v.Id
LEFT JOIN TextSearch t ON d.Id = t.Id
WHERE v.Id IS NOT NULL OR t.Id IS NOT NULL
ORDER BY CombinedScore DESC;
```

#### 1.5 **Conversational Memory (Chat History)**
**Problema attuale:** Ogni query è isolata

**Soluzione:**
```csharp
public class Conversation
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Title { get; set; } // Auto-generato dal primo messaggio
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public List<Message> Messages { get; set; }
}

public class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string Role { get; set; } // "user" | "assistant"
    public string Content { get; set; }
    public List<int> ReferencedDocumentIds { get; set; } // Documenti usati
    public DateTime Timestamp { get; set; }
}

// RAG con memoria conversazionale
public async Task<string> GenerateResponseWithHistoryAsync(
    string currentQuery,
    List<Message> conversationHistory,
    List<Document> relevantDocuments)
{
    var messages = new List<ChatMessage>
    {
        new SystemChatMessage(systemPrompt)
    };
    
    // Aggiungi storia conversazione
    foreach (var msg in conversationHistory.TakeLast(5)) // Ultime 5 per context
    {
        messages.Add(msg.Role == "user" 
            ? new UserChatMessage(msg.Content)
            : new AssistantChatMessage(msg.Content));
    }
    
    // Aggiungi contesto documenti
    messages.Add(new UserChatMessage(BuildContext(relevantDocuments)));
    
    // Query corrente
    messages.Add(new UserChatMessage(currentQuery));
    
    return await _chatClient.CompleteChatAsync(messages);
}
```

**UI:**
- Sidebar con conversazioni salvate
- Ricerca nelle conversazioni
- Export conversazioni (PDF/Word)
- Condivisione conversazioni con colleghi

---

### 🎨 PRIORITÀ 2: Miglioramenti UI/UX

#### 2.1 **Dashboard Analytics Aziendale**
```csharp
public class DashboardMetrics
{
    // Metriche utente
    public int TotalQueries { get; set; }
    public int DocumentsAccessed { get; set; }
    public Dictionary<string, int> TopQueries { get; set; }
    public Dictionary<string, int> MostAccessedDocuments { get; set; }
    
    // Metriche dipartimento
    public Dictionary<string, int> QueriesByDepartment { get; set; }
    public Dictionary<string, int> DocumentsByDepartment { get; set; }
    
    // Metriche sistema
    public double AverageResponseTime { get; set; }
    public int CacheHitRate { get; set; }
    public double AverageRelevanceScore { get; set; }
    
    // Trend temporali
    public List<TimeSeriesData> QueriesOverTime { get; set; }
    public List<TimeSeriesData> DocumentUploadsOverTime { get; set; }
}
```

**Visualizzazioni:**
- 📊 Grafici interattivi (Chart.js/ApexCharts)
- 📈 Trend settimanali/mensili
- 🔥 Heatmap documenti più usati
- 🎯 KPI aziendali

#### 2.2 **Interfaccia Chat Migliorata**
**Design:**
```razor
<!-- Chat moderna tipo ChatGPT -->
<div class="chat-interface">
    <div class="chat-sidebar">
        <!-- Conversazioni precedenti -->
        <div class="conversation-list">
            @foreach (var conv in conversations)
            {
                <div class="conversation-item" @onclick="() => LoadConversation(conv.Id)">
                    <span class="conv-icon">💬</span>
                    <div class="conv-details">
                        <h4>@conv.Title</h4>
                        <small>@conv.LastMessageAt.Humanize()</small>
                    </div>
                </div>
            }
        </div>
        
        <button class="new-chat-btn" @onclick="StartNewChat">
            ➕ Nuova Conversazione
        </button>
    </div>
    
    <div class="chat-main">
        <!-- Messaggi -->
        <div class="messages-container">
            @foreach (var msg in currentMessages)
            {
                <div class="message @msg.Role">
                    <div class="message-avatar">
                        @(msg.Role == "user" ? "👤" : "🤖")
                    </div>
                    <div class="message-content">
                        <Markdown Content="@msg.Content" />
                        
                        @if (msg.ReferencedDocuments.Any())
                        {
                            <div class="referenced-docs">
                                <small>📚 Fonti:</small>
                                @foreach (var doc in msg.ReferencedDocuments)
                                {
                                    <span class="doc-badge" @onclick="() => ViewDocument(doc.Id)">
                                        @doc.FileName
                                    </span>
                                }
                            </div>
                        }
                    </div>
                </div>
            }
            
            @if (isTyping)
            {
                <div class="typing-indicator">
                    <span></span><span></span><span></span>
                </div>
            }
        </div>
        
        <!-- Input area -->
        <div class="input-area">
            <textarea 
                @bind="currentQuery" 
                placeholder="Chiedi qualcosa sui tuoi documenti..."
                @onkeydown="HandleKeyPress">
            </textarea>
            <button @onclick="SendMessage" disabled="@isTyping">
                @(isTyping ? "⏳" : "➤")
            </button>
        </div>
    </div>
    
    <div class="chat-sidebar-right">
        <!-- Documenti correlati alla conversazione -->
        <h3>📄 Documenti Correlati</h3>
        <div class="related-docs">
            @foreach (var doc in relatedDocuments)
            {
                <DocumentCard Document="@doc" />
            }
        </div>
    </div>
</div>
```

**Funzionalità avanzate:**
- ✨ Suggerimenti domande (query suggestions)
- 🔖 Evidenziazione citazioni nei documenti
- 📎 Attach documenti specifici alla query
- 🎤 Input vocale (Speech-to-Text)
- 🌐 Multilingua (traduzioni automatiche)

#### 2.3 **Document Viewer Integrato**
```razor
<div class="document-viewer">
    <div class="viewer-toolbar">
        <button @onclick="ZoomIn">🔍+</button>
        <button @onclick="ZoomOut">🔍-</button>
        <button @onclick="Download">⬇️ Download</button>
        <button @onclick="Share">🔗 Condividi</button>
        <button @onclick="Print">🖨️ Stampa</button>
    </div>
    
    <div class="viewer-content">
        @if (document.ContentType.Contains("pdf"))
        {
            <PdfViewer FileUrl="@document.FileUrl" 
                      HighlightedText="@searchTerms" />
        }
        else if (IsImageType(document.ContentType))
        {
            <img src="@document.FileUrl" alt="@document.FileName" />
        }
        else
        {
            <pre>@document.ExtractedText</pre>
        }
    </div>
    
    <div class="viewer-sidebar">
        <!-- Metadata -->
        <h4>📋 Metadata</h4>
        <dl>
            <dt>Caricato il:</dt>
            <dd>@document.UploadedAt.ToShortDateString()</dd>
            
            <dt>Dipartimento:</dt>
            <dd>@document.Department?.Name</dd>
            
            <dt>Categoria:</dt>
            <dd>@document.ActualCategory</dd>
        </dl>
        
        <!-- Similar documents -->
        <h4>🔗 Documenti Simili</h4>
        @foreach (var similar in similarDocuments)
        {
            <div class="similar-doc" @onclick="() => LoadDocument(similar.Id)">
                <span>📄 @similar.FileName</span>
                <small>Similarità: @similar.SimilarityScore%</small>
            </div>
        }
    </div>
</div>
```

---

### 🔐 PRIORITÀ 3: Sicurezza e Compliance

#### 3.1 **Audit Log Completo**
```csharp
public class AuditLog
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; } // "VIEW", "DOWNLOAD", "SEARCH", "UPLOAD", "DELETE"
    public string EntityType { get; set; } // "Document", "Conversation"
    public int EntityId { get; set; }
    public string? Details { get; set; } // JSON con dettagli
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
}

// Middleware per logging automatico
public class AuditMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        
        // Log dopo ogni operazione
        await _auditService.LogAsync(new AuditLog
        {
            UserId = context.User.Identity.Name,
            Action = context.Request.Method,
            IpAddress = context.Connection.RemoteIpAddress.ToString(),
            Timestamp = DateTime.UtcNow
        });
    }
}
```

**Report Compliance:**
- 📊 Chi ha accesso a cosa
- 🕒 Quando è stato acceduto
- 📥 Cosa è stato scaricato
- 🔍 Query effettuate per utente/dipartimento

#### 3.2 **Data Retention Policy**
```csharp
public class RetentionPolicy
{
    public int Id { get; set; }
    public string Category { get; set; }
    public int RetentionDays { get; set; }
    public bool AutoDelete { get; set; }
    public bool RequireApprovalForDelete { get; set; }
}

// Background service
public class RetentionService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckExpiredDocuments();
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
    
    private async Task CheckExpiredDocuments()
    {
        var policies = await _context.RetentionPolicies.ToListAsync();
        
        foreach (var policy in policies)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-policy.RetentionDays);
            var expiredDocs = await _context.Documents
                .Where(d => d.ActualCategory == policy.Category 
                         && d.UploadedAt < cutoffDate)
                .ToListAsync();
            
            if (policy.AutoDelete)
            {
                // Soft delete
                foreach (var doc in expiredDocs)
                {
                    doc.IsDeleted = true;
                    doc.DeletedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // Notifica admin per approvazione
                await NotifyAdminsForApproval(expiredDocs);
            }
        }
        
        await _context.SaveChangesAsync();
    }
}
```

#### 3.3 **Content Filtering & DLP**
```csharp
public interface IContentFilterService
{
    Task<FilterResult> CheckContentAsync(string text, Stream fileStream);
}

public class ContentFilterService : IContentFilterService
{
    public async Task<FilterResult> CheckContentAsync(string text, Stream file)
    {
        var result = new FilterResult();
        
        // 1. PII Detection (GDPR compliance)
        result.PersonalInfo = await DetectPII(text);
        
        // 2. Sensitive data (carte credito, password, ecc.)
        result.SensitiveData = await DetectSensitiveData(text);
        
        // 3. Malware scan
        result.MalwareDetected = await ScanForMalware(file);
        
        // 4. Profanity/inappropriate content
        result.InappropriateContent = await CheckInappropriateContent(text);
        
        return result;
    }
    
    private async Task<List<PIIEntity>> DetectPII(string text)
    {
        // Azure Text Analytics per PII detection
        // Rileva: nomi, email, telefoni, indirizzi, SSN, ecc.
        var entities = new List<PIIEntity>();
        
        // Pattern matching per email
        var emailRegex = new Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b");
        var emails = emailRegex.Matches(text);
        
        foreach (Match email in emails)
        {
            entities.Add(new PIIEntity
            {
                Type = "Email",
                Value = email.Value,
                StartIndex = email.Index,
                Confidence = 1.0
            });
        }
        
        // Altri pattern: telefoni, SSN, ecc.
        
        return entities;
    }
}

// Blocco caricamento se contenuto problematico
public async Task<UploadResult> UploadDocumentWithFilteringAsync(
    IFormFile file,
    string userId)
{
    using var stream = file.OpenReadStream();
    var text = await ExtractText(stream);
    
    var filterResult = await _contentFilter.CheckContentAsync(text, stream);
    
    if (filterResult.HasIssues)
    {
        return new UploadResult
        {
            Success = false,
            Message = "Documento bloccato: contiene dati sensibili",
            Issues = filterResult.Issues
        };
    }
    
    // Procedi con upload...
}
```

---

### ⚡ PRIORITÀ 4: Performance e Scalabilità

#### 4.1 **Batch Processing per Embeddings**
```csharp
public class EmbeddingBatchProcessor : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly Queue<Document> _queue = new();
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var document))
            {
                await ProcessDocument(document);
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
    
    public void EnqueueDocument(Document document)
    {
        _queue.Enqueue(document);
    }
    
    private async Task ProcessDocument(Document document)
    {
        using var scope = _services.CreateScope();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        try
        {
            // Chunking
            var chunks = ChunkDocument(document.ExtractedText);
            
            // Generate embeddings per chunk
            var embeddings = new List<float[]>();
            foreach (var chunk in chunks)
            {
                var embedding = await embeddingService.GenerateEmbeddingAsync(chunk);
                if (embedding != null)
                    embeddings.Add(embedding);
            }
            
            // Salva chunks e embeddings
            foreach (var (chunk, embedding) in chunks.Zip(embeddings))
            {
                var docChunk = new DocumentChunk
                {
                    DocumentId = document.Id,
                    ChunkIndex = chunks.IndexOf(chunk),
                    Text = chunk,
                    EmbeddingVector = embedding
                };
                context.DocumentChunks.Add(docChunk);
            }
            
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing document {document.Id}");
        }
    }
}
```

#### 4.2 **Caching Redis Multi-Livello**
```csharp
public class MultiLevelCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;
    
    public async Task<T?> GetAsync<T>(string key)
    {
        // L1: Memory cache (velocissimo)
        if (_memoryCache.TryGetValue(key, out T? cached))
            return cached;
        
        // L2: Redis cache
        var redisValue = await _redisCache.GetStringAsync(key);
        if (redisValue != null)
        {
            var value = JsonSerializer.Deserialize<T>(redisValue);
            
            // Popola L1
            _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
            
            return value;
        }
        
        return default;
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        // Scrivi su entrambi
        _memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
        
        var serialized = JsonSerializer.Serialize(value);
        await _redisCache.SetStringAsync(key, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });
    }
}

// Cache keys strategici
public static class CacheKeys
{
    public static string UserDocuments(string userId) => $"user:docs:{userId}";
    public static string DocumentEmbedding(int docId) => $"doc:emb:{docId}";
    public static string QueryResult(string queryHash) => $"query:result:{queryHash}";
    public static string ConversationHistory(int convId) => $"conv:history:{convId}";
}
```

#### 4.3 **SQL Server 2025 Ottimizzazioni**
```sql
-- Indici vettoriali ottimizzati
CREATE INDEX IX_Documents_Vector 
ON Documents(EmbeddingVector) 
INCLUDE (FileName, ActualCategory, UploadedAt)
WITH (VECTOR_INDEX_TYPE = 'IVF', -- Inverted File Index
      NLIST = 100);               -- Numero di cluster

-- Indici compositi per filtri comuni
CREATE INDEX IX_Documents_Department_Category_Date
ON Documents(DepartmentId, ActualCategory, UploadedAt DESC)
INCLUDE (FileName, FileSize, Visibility);

-- Indice full-text
CREATE FULLTEXT INDEX ON Documents(ExtractedText, FileName)
KEY INDEX PK_Documents
WITH STOPLIST = SYSTEM;

-- Columnstore per analytics
CREATE COLUMNSTORE INDEX IX_AuditLog_Columnstore
ON AuditLog(Timestamp, UserId, Action, EntityType);

-- Partition per grandi dataset
CREATE PARTITION FUNCTION PF_Documents_ByYear(DATETIME2)
AS RANGE RIGHT FOR VALUES 
    ('2023-01-01', '2024-01-01', '2025-01-01');

CREATE PARTITION SCHEME PS_Documents_ByYear
AS PARTITION PF_Documents_ByYear
ALL TO ([PRIMARY]);

ALTER TABLE Documents 
DROP CONSTRAINT PK_Documents;

ALTER TABLE Documents 
ADD CONSTRAINT PK_Documents 
PRIMARY KEY (Id, UploadedAt)
ON PS_Documents_ByYear(UploadedAt);

-- Stored procedure ottimizzata per ricerca ibrida
CREATE OR ALTER PROCEDURE sp_HybridSearch
    @QueryVector VECTOR(1536),
    @QueryText NVARCHAR(MAX),
    @UserId NVARCHAR(450),
    @DepartmentId INT = NULL,
    @TopK INT = 10,
    @MinSimilarity FLOAT = 0.7
AS
BEGIN
    SET NOCOUNT ON;
    
    -- CTE per ricerca vettoriale
    WITH VectorResults AS (
        SELECT TOP (@TopK * 2)
            d.Id,
            d.FileName,
            d.ActualCategory,
            VECTOR_DISTANCE('cosine', d.EmbeddingVector, @QueryVector) as VectorScore,
            ROW_NUMBER() OVER (ORDER BY VECTOR_DISTANCE('cosine', d.EmbeddingVector, @QueryVector) DESC) as VectorRank
        FROM Documents d
        WHERE d.EmbeddingVector IS NOT NULL
          AND (@DepartmentId IS NULL OR d.DepartmentId = @DepartmentId)
          AND VECTOR_DISTANCE('cosine', d.EmbeddingVector, @QueryVector) >= @MinSimilarity
          AND (d.OwnerId = @UserId 
               OR d.Visibility = 3 -- Public
               OR EXISTS (SELECT 1 FROM DocumentShares WHERE DocumentId = d.Id AND SharedWithUserId = @UserId))
    ),
    -- CTE per full-text search
    TextResults AS (
        SELECT TOP (@TopK * 2)
            d.Id,
            d.FileName,
            d.ActualCategory,
            ft.[RANK] / 1000.0 as TextScore,
            ROW_NUMBER() OVER (ORDER BY ft.[RANK] DESC) as TextRank
        FROM Documents d
        INNER JOIN FREETEXTTABLE(Documents, ExtractedText, @QueryText) ft ON d.Id = ft.[KEY]
        WHERE (@DepartmentId IS NULL OR d.DepartmentId = @DepartmentId)
          AND (d.OwnerId = @UserId 
               OR d.Visibility = 3
               OR EXISTS (SELECT 1 FROM DocumentShares WHERE DocumentId = d.Id AND SharedWithUserId = @UserId))
    )
    -- Reciprocal Rank Fusion
    SELECT TOP (@TopK)
        d.*,
        COALESCE(v.VectorScore, 0) * 0.6 + COALESCE(t.TextScore, 0) * 0.4 as CombinedScore,
        v.VectorRank,
        t.TextRank
    FROM Documents d
    LEFT JOIN VectorResults v ON d.Id = v.Id
    LEFT JOIN TextResults t ON d.Id = t.Id
    WHERE v.Id IS NOT NULL OR t.Id IS NOT NULL
    ORDER BY CombinedScore DESC;
END;
```

---

### 🤖 PRIORITÀ 5: AI Avanzato

#### 5.1 **Auto-Tagging e Classificazione Multi-Label**
```csharp
public class SmartClassificationService
{
    public async Task<DocumentClassification> ClassifyAsync(Document document)
    {
        var classification = new DocumentClassification
        {
            DocumentId = document.Id
        };
        
        // 1. Category (singola)
        classification.Category = await SuggestCategory(document);
        
        // 2. Tags (multipli)
        classification.Tags = await ExtractTags(document);
        
        // 3. Entities (NER)
        classification.Entities = await ExtractEntities(document);
        
        // 4. Sentiment
        classification.Sentiment = await AnalyzeSentiment(document);
        
        // 5. Document type
        classification.DocumentType = await DetectDocumentType(document);
        
        // 6. Language
        classification.Language = await DetectLanguage(document);
        
        return classification;
    }
    
    private async Task<List<string>> ExtractTags(Document document)
    {
        // Usa Azure Text Analytics o modello custom
        var prompt = $@"Extract key tags from this document. 
Return 5-10 relevant tags.

Document: {TruncateText(document.ExtractedText, 2000)}

Return ONLY a JSON array of tags: [""tag1"", ""tag2"", ...]";

        var response = await _chatClient.CompleteChatAsync(new[]
        {
            new SystemChatMessage("You are a tagging expert."),
            new UserChatMessage(prompt)
        });
        
        var tags = JsonSerializer.Deserialize<List<string>>(response.Value.Content[0].Text);
        return tags ?? new List<string>();
    }
    
    private async Task<List<NamedEntity>> ExtractEntities(Document document)
    {
        // Named Entity Recognition
        // Estrae: persone, aziende, luoghi, date, importi, ecc.
        
        var entities = new List<NamedEntity>();
        
        // Regex patterns per entità comuni
        var datePattern = @"\b\d{1,2}[/-]\d{1,2}[/-]\d{2,4}\b";
        var moneyPattern = @"[$€£]\s?\d+(?:,\d{3})*(?:\.\d{2})?";
        var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
        
        // Usa Azure Text Analytics per NER più avanzato
        // var azureEntities = await _textAnalytics.RecognizeEntitiesAsync(document.ExtractedText);
        
        return entities;
    }
}
```

#### 5.2 **Query Suggestions & Autocomplete**
```csharp
public class QuerySuggestionService
{
    private readonly ApplicationDbContext _context;
    
    public async Task<List<string>> GetSuggestionsAsync(string partialQuery, string userId)
    {
        var suggestions = new List<string>();
        
        // 1. Query precedenti dell'utente
        var userQueries = await _context.AuditLogs
            .Where(l => l.UserId == userId 
                     && l.Action == "SEARCH"
                     && l.Details.Contains(partialQuery))
            .OrderByDescending(l => l.Timestamp)
            .Take(5)
            .Select(l => JsonDocument.Parse(l.Details).RootElement.GetProperty("query").GetString())
            .ToListAsync();
        
        suggestions.AddRange(userQueries);
        
        // 2. Query popolari nel dipartimento
        var user = await _context.Users.Include(u => u.Department).FirstAsync(u => u.Id == userId);
        if (user.Department != null)
        {
            var deptQueries = await _context.AuditLogs
                .Join(_context.Users, l => l.UserId, u => u.Id, (l, u) => new { l, u })
                .Where(x => x.u.DepartmentId == user.DepartmentId
                         && x.l.Action == "SEARCH")
                .GroupBy(x => JsonDocument.Parse(x.l.Details).RootElement.GetProperty("query").GetString())
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToListAsync();
            
            suggestions.AddRange(deptQueries);
        }
        
        // 3. AI-generated suggestions
        if (suggestions.Count < 5)
        {
            var aiSuggestions = await GenerateAISuggestions(partialQuery, userId);
            suggestions.AddRange(aiSuggestions);
        }
        
        return suggestions.Distinct().Take(5).ToList();
    }
    
    private async Task<List<string>> GenerateAISuggestions(string partial, string userId)
    {
        // Genera suggerimenti basati su contesto utente e documenti disponibili
        var userDocs = await GetUserAccessibleDocuments(userId);
        var categories = userDocs.Select(d => d.ActualCategory).Distinct().ToList();
        
        var prompt = $@"User started typing: ""{partial}""

Available document categories: {string.Join(", ", categories)}

Suggest 3 complete questions the user might want to ask.
Return ONLY a JSON array: [""question1"", ""question2"", ""question3""]";

        var response = await _chatClient.CompleteChatAsync(new[]
        {
            new SystemChatMessage("You are a helpful assistant that suggests queries."),
            new UserChatMessage(prompt)
        });
        
        var suggestions = JsonSerializer.Deserialize<List<string>>(response.Value.Content[0].Text);
        return suggestions ?? new List<string>();
    }
}
```

#### 5.3 **Summarization Pipeline**
```csharp
public class DocumentSummarizationService
{
    public async Task<DocumentSummary> SummarizeAsync(Document document)
    {
        var summary = new DocumentSummary
        {
            DocumentId = document.Id
        };
        
        // 1. Executive Summary (breve)
        summary.ExecutiveSummary = await GenerateExecutiveSummary(document, maxWords: 100);
        
        // 2. Detailed Summary (medio)
        summary.DetailedSummary = await GenerateDetailedSummary(document, maxWords: 500);
        
        // 3. Key Points (bullet points)
        summary.KeyPoints = await ExtractKeyPoints(document);
        
        // 4. Action Items (se presenti)
        summary.ActionItems = await ExtractActionItems(document);
        
        // 5. Key Figures (numeri importanti)
        summary.KeyFigures = await ExtractKeyFigures(document);
        
        return summary;
    }
    
    private async Task<string> GenerateExecutiveSummary(Document doc, int maxWords)
    {
        var prompt = $@"Create a concise executive summary of this document in {maxWords} words or less.
Focus on the most important information.

Document: {TruncateText(doc.ExtractedText, 3000)}

Executive Summary:";

        var response = await _chatClient.CompleteChatAsync(new[]
        {
            new SystemChatMessage("You are an expert at creating concise summaries."),
            new UserChatMessage(prompt)
        });
        
        return response.Value.Content[0].Text;
    }
    
    private async Task<List<string>> ExtractKeyPoints(Document doc)
    {
        var prompt = $@"Extract 5-7 key points from this document as bullet points.

Document: {TruncateText(doc.ExtractedText, 3000)}

Return ONLY a JSON array of key points: [""point1"", ""point2"", ...]";

        var response = await _chatClient.CompleteChatAsync(new[]
        {
            new SystemChatMessage("You are an expert at identifying key information."),
            new UserChatMessage(prompt)
        });
        
        var points = JsonSerializer.Deserialize<List<string>>(response.Value.Content[0].Text);
        return points ?? new List<string>();
    }
}
```

---

### 📱 PRIORITÀ 6: Integrazioni

#### 6.1 **Microsoft Teams Integration**
```csharp
public class TeamsIntegrationService
{
    // Bot per Teams
    public async Task<string> HandleTeamsMessageAsync(string message, string userId)
    {
        // Interpreta comando
        if (message.StartsWith("/search"))
        {
            var query = message.Substring(7).Trim();
            return await SearchAndFormat(query, userId);
        }
        else if (message.StartsWith("/summarize"))
        {
            var docId = ExtractDocId(message);
            return await SummarizeDocument(docId, userId);
        }
        else
        {
            // RAG normale
            return await _ragService.GenerateResponseAsync(message, userId);
        }
    }
    
    // Adaptive Card per risposta formattata
    private string CreateAdaptiveCard(List<Document> docs, string answer)
    {
        return @"{
    ""type"": ""AdaptiveCard"",
    ""body"": [
        {
            ""type"": ""TextBlock"",
            ""text"": """ + answer + @""",
            ""wrap"": true
        },
        {
            ""type"": ""TextBlock"",
            ""text"": ""📚 Documenti Fonte:"",
            ""weight"": ""Bolder""
        },
        {
            ""type"": ""FactSet"",
            ""facts"": [" + string.Join(",", docs.Select(d => $@"
                {{
                    ""title"": ""{d.FileName}"",
                    ""value"": ""{d.ActualCategory}""
                }}")) + @"]
        }
    ],
    ""actions"": [
        {
            ""type"": ""Action.OpenUrl"",
            ""title"": ""Apri in DocN"",
            ""url"": ""https://docn.company.com""
        }
    ]
}";
    }
}
```

#### 6.2 **Outlook Add-in**
```csharp
// Salva email come documento
public async Task<Document> SaveEmailAsDocumentAsync(
    string subject,
    string body,
    List<IFormFile> attachments,
    string userId)
{
    var document = new Document
    {
        FileName = $"Email - {subject}",
        ExtractedText = $"Subject: {subject}\n\nBody:\n{body}",
        ContentType = "message/rfc822",
        OwnerId = userId,
        UploadedAt = DateTime.UtcNow
    };
    
    // Processa allegati
    foreach (var attachment in attachments)
    {
        var attachDoc = await ProcessAttachment(attachment, userId);
        document.RelatedDocuments.Add(attachDoc);
    }
    
    // Genera embedding
    var embedding = await _embeddingService.GenerateEmbeddingAsync(document.ExtractedText);
    document.EmbeddingVector = embedding;
    
    await _context.Documents.AddAsync(document);
    await _context.SaveChangesAsync();
    
    return document;
}
```

#### 6.3 **API REST Completa**
```csharp
// Controllers/ApiController.cs
[ApiController]
[Route("api/v1")]
[Authorize]
public class DocumentApiController : ControllerBase
{
    [HttpPost("search")]
    [ProducesResponseType(200)]
    [SwaggerOperation(Summary = "Ricerca ibrida documenti")]
    public async Task<ActionResult<SearchResponse>> Search([FromBody] SearchRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var results = await _searchService.HybridSearchAsync(
            request.Query,
            userId,
            request.Options
        );
        
        return Ok(new SearchResponse
        {
            Results = results,
            TotalCount = results.Count,
            QueryTime = stopwatch.ElapsedMilliseconds
        });
    }
    
    [HttpPost("chat")]
    [ProducesResponseType(200)]
    [SwaggerOperation(Summary = "Chat RAG interattivo")]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Recupera conversazione esistente o creane una nuova
        var conversation = await GetOrCreateConversation(request.ConversationId, userId);
        
        // RAG con storia
        var answer = await _ragService.GenerateResponseWithHistoryAsync(
            request.Message,
            conversation.Messages,
            userId
        );
        
        // Salva messaggio
        conversation.Messages.Add(new Message
        {
            Role = "user",
            Content = request.Message,
            Timestamp = DateTime.UtcNow
        });
        
        conversation.Messages.Add(new Message
        {
            Role = "assistant",
            Content = answer,
            Timestamp = DateTime.UtcNow
        });
        
        await _context.SaveChangesAsync();
        
        return Ok(new ChatResponse
        {
            ConversationId = conversation.Id,
            Answer = answer,
            ReferencedDocuments = answer.ReferencedDocIds
        });
    }
    
    [HttpGet("documents/{id}")]
    [ProducesResponseType(200)]
    [SwaggerOperation(Summary = "Ottieni dettagli documento")]
    public async Task<ActionResult<DocumentDetailsResponse>> GetDocument(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var document = await _documentService.GetDocumentAsync(id, userId);
        
        if (document == null)
            return NotFound();
        
        return Ok(new DocumentDetailsResponse
        {
            Document = document,
            RelatedDocuments = await GetRelatedDocuments(id, userId),
            AccessHistory = await GetAccessHistory(id)
        });
    }
}

// Swagger documentation
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DocN API",
        Version = "v1",
        Description = "API per sistema RAG aziendale"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
});
```

---

## 🎯 Roadmap Implementazione

### Fase 1: Foundation (Settimana 1-2)
1. ✅ Fix servizi esistenti (lazy init)
2. ✅ Chunking documenti
3. ✅ Ricerca ibrida base
4. ✅ Ottimizzazioni SQL Server

### Fase 2: Core Features (Settimana 3-4)
5. Multi-tenancy e dipartimenti
6. Versioning documenti
7. OCR avanzato
8. Conversational memory

### Fase 3: Enterprise (Settimana 5-6)
9. Audit log completo
10. Data retention policies
11. Content filtering & DLP
12. Dashboard analytics

### Fase 4: AI Advanced (Settimana 7-8)
13. Auto-tagging multi-label
14. Query suggestions
15. Document summarization
16. Reranking semantico

### Fase 5: Integrations (Settimana 9-10)
17. Microsoft Teams bot
18. Outlook add-in
19. REST API completa
20. Webhook notifications

---

## 📊 Metriche di Successo

### KPI Tecnici
- ⚡ Query response time: < 2 sec
- 📈 Supporto fino a 100,000 documenti
- 💾 Cache hit rate: > 60%
- 🎯 Relevance score medio: > 0.8

### KPI Business
- 👥 Adozione utenti: 80% entro 3 mesi
- ⏱️ Tempo risparmiatoper ricerca: -70%
- 📚 Documenti indicizzati: 100% entro 1 mese
- 😊 User satisfaction: > 4.5/5

---

## 💰 Stima Costi

### Infrastructure (Azure)
- SQL Server 2025: $500-1000/mese
- App Service: $200/mese
- Redis Cache: $100/mese
- Storage: $50/mese
- **Totale infra: ~$1000/mese**

### AI Services
- Azure OpenAI embeddings: $0.0001/1K tokens
- GPT-4 chat: $0.03/1K tokens
- Document Intelligence: $0.001/pagina
- **Stima: $500-2000/mese** (dipende da utilizzo)

### Totale Stimato: **$1500-3000/mese**

**ROI:** Se risparmia anche solo 2 ore/settimana per 50 dipendenti = 400 ore/mese = 16 dipendenti equivalenti!

---

Vuoi che implementi qualcuna di queste funzionalità? Posso iniziare con quelle a priorità alta!
