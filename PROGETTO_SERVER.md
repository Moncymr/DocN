# DocN.Server - Documentazione Tecnica

## Indice
1. [Panoramica Progetto](#panoramica-progetto)
2. [Scopo e Funzionalità](#scopo-e-funzionalità)
3. [Architettura](#architettura)
4. [Tecnologie Utilizzate](#tecnologie-utilizzate)
5. [Struttura del Progetto](#struttura-del-progetto)
6. [API Endpoints](#api-endpoints)
7. [Servizi e Middleware](#servizi-e-middleware)
8. [Health Checks e Monitoring](#health-checks-e-monitoring)

---

## Panoramica Progetto

**DocN.Server** è il backend API della soluzione DocN, implementato come **ASP.NET Core Web API**. Fornisce servizi RAG avanzati, chat semantica con Semantic Kernel, e API REST per funzionalità enterprise.

### Informazioni di Base
- **Tipo**: ASP.NET Core Web API
- **Target Framework**: .NET 10.0
- **Porta Predefinita**: 5211 (HTTPS)
- **Ruolo**: Backend API + RAG Services + Enterprise Features
- **Dipendenze**: DocN.Data (che include DocN.Core)

---

## Scopo e Funzionalità

### Scopo Principale

DocN.Server fornisce:

1. **REST API**
   - Endpoints per operazioni documenti
   - API ricerca avanzata
   - API configurazione sistema
   - API audit e logging

2. **Servizi RAG Avanzati**
   - Chat semantica con Semantic Kernel
   - Query rewriting e HyDE
   - Self-query parsing
   - Re-ranking risultati

3. **Enterprise Features**
   - Health checks
   - Monitoring e metrics (OpenTelemetry)
   - Distributed caching (Redis)
   - Background jobs (Hangfire)
   - API documentation (Swagger/OpenAPI)

4. **Integrazione AI**
   - Orchestrazione chiamate AI multi-provider
   - Gestione agent specializzati
   - Memory management per conversazioni

### Funzionalità Specifiche

#### 1. RESTful API
Endpoints completi per:
- CRUD documenti
- Ricerca (semantic, fulltext, hybrid)
- Chat RAG
- Configurazione AI
- Gestione agenti
- Audit logs

#### 2. Chat Semantica
- Streaming responses con SignalR
- Context-aware conversations
- Multi-turn dialogue management
- Citation tracking

#### 3. Advanced RAG
- HyDE (Hypothetical Document Embeddings)
- Query rewriting per ambiguità
- Self-query per filtri strutturati
- Multi-query retrieval
- Re-ranking con cross-encoder

#### 4. Monitoring e Observability
- Health checks (database, AI providers, OCR)
- Metrics (App.Metrics + Prometheus)
- Distributed tracing (OpenTelemetry)
- Structured logging (Serilog)

#### 5. Background Processing
- Hangfire per job scheduling
- Batch embedding generation
- Document reprocessing
- Cleanup jobs

---

## Architettura

### API Architecture

```
Client Request
    ↓
API Gateway/Load Balancer (optional)
    ↓
DocN.Server (ASP.NET Core)
    ↓
Middleware Pipeline:
- Exception Handling
- Authentication
- Authorization
- CORS
- Rate Limiting
    ↓
Controllers (API Endpoints)
    ↓
Services (Business Logic)
    ↓
DocN.Data (Data Access)
    ↓
Database / External APIs
```

### Layers Architetturali

```
DocN.Server
├── Presentation Layer
│   ├── Controllers         # API endpoints
│   └── Middleware          # Custom middleware
│
├── Application Layer
│   ├── Services            # Application services
│   └── Handlers            # Command/Query handlers
│
├── Integration Layer
│   ├── External APIs       # AI providers
│   └── Background Jobs     # Hangfire jobs
│
└── Cross-Cutting
    ├── Health Checks
    ├── Logging
    └── Monitoring
```

### Pattern Implementati

1. **RESTful API**: Standard HTTP methods e status codes
2. **Dependency Injection**: ASP.NET Core DI container
3. **Middleware Pipeline**: Request/response processing
4. **Options Pattern**: Configurazione tipizzata
5. **Repository Pattern**: Via DocN.Data
6. **CQRS (light)**: Separation of read/write operations

---

## Tecnologie Utilizzate

### Core Framework

#### 1. ASP.NET Core 10.0
**Scopo**: Framework per Web API

**Caratteristiche:**
- Built-in DI container
- Middleware pipeline
- Model binding e validation
- Content negotiation
- CORS support

**Esempio controller:**
```csharp
[ApiController]
[Route("[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;
    private readonly ILogger<DocumentsController> _logger;
    
    public DocumentsController(
        IDocumentService documentService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }
    
    /// <summary>
    /// Ottiene tutti i documenti accessibili all'utente corrente
    /// </summary>
    /// <returns>Lista documenti</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Document>), 200)]
    public async Task<ActionResult<IEnumerable<Document>>> GetDocuments()
    {
        var documents = await _documentService.GetDocumentsAsync();
        return Ok(documents);
    }
}
```

### API Documentation

#### 2. Swashbuckle.AspNetCore (v10.1.0)
**Scopo**: Generazione documentazione OpenAPI/Swagger

**Funzionalità:**
- UI interattiva Swagger
- Schema generation automatico
- XML comments support
- Authentication schemes

**Configurazione:**
```csharp
// Program.cs
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DocN API",
        Version = "v1",
        Description = "API per sistema RAG documentale"
    });
    
    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DocN API V1");
    c.RoutePrefix = "swagger";
});
```

**Accesso**: `https://localhost:5211/swagger`

### Logging

#### 3. Serilog (v8.0.3)
**Scopo**: Logging strutturato

**Sinks configurati:**
- Console (development)
- File (rolling files)
- Application Insights (production - optional)

**Configurazione:**
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/docn-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7
    )
    .CreateLogger();

builder.Host.UseSerilog();
```

**Utilizzo:**
```csharp
_logger.LogInformation("Document {DocumentId} uploaded by {UserId}", docId, userId);
_logger.LogError(ex, "Error processing document {DocumentId}", docId);
```

### Monitoring e Metrics

#### 4. OpenTelemetry Suite
**Scopo**: Observability (traces, metrics, logs)

**Packages:**
- `OpenTelemetry.Extensions.Hosting` (v1.10.0)
- `OpenTelemetry.Instrumentation.AspNetCore` (v1.10.1)
- `OpenTelemetry.Instrumentation.Http` (v1.10.1)
- `OpenTelemetry.Instrumentation.SqlClient` (v1.10.0-beta.1)
- `OpenTelemetry.Exporter.Prometheus.AspNetCore` (v1.10.0-beta.1)
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` (v1.10.0)

**Configurazione:**
```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(builder => builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

app.MapPrometheusScrapingEndpoint();  // /metrics endpoint
```

**Metriche esposte:**
- HTTP requests (count, duration, errors)
- Database queries (count, duration)
- Runtime metrics (GC, memory, threads)
- Custom metrics (documents processed, AI calls, etc.)

#### 5. App.Metrics (v4.3.0)
**Scopo**: Application metrics dettagliate

**Metriche custom:**
```csharp
// Histogram per latency
_metrics.Measure.Histogram.Update(
    AICallLatency,
    () => new HistogramValue(duration, "gemini")
);

// Counter per documenti processati
_metrics.Measure.Counter.Increment(
    DocumentsProcessedCounter
);

// Gauge per queue size
_metrics.Measure.Gauge.SetValue(
    PendingDocumentsGauge,
    pendingCount
);
```

**Formato export:** Prometheus (`/metrics`)

### Health Checks

#### 6. ASP.NET Core Health Checks
**Scopo**: Endpoint status applicazione

**Configurazione:**
```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddCheck<AIProviderHealthCheck>("ai-providers")
    .AddCheck<OCRServiceHealthCheck>("ocr-service")
    .AddCheck<FileStorageHealthCheck>("file-storage")
    .AddRedis(redisConnectionString, "redis");

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

**Endpoints:**
- `/health`: Health check completo
- `/health/ready`: Readiness probe (Kubernetes)
- `/health/live`: Liveness probe (Kubernetes)

**Output esempio:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1234567",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0234567"
    },
    "ai-providers": {
      "status": "Healthy",
      "data": {
        "gemini": "Available",
        "openai": "Available"
      }
    }
  }
}
```

### Background Jobs

#### 7. Hangfire (v1.8.14)
**Scopo**: Background job processing

**Funzionalità:**
- Scheduled jobs (cron)
- Recurring jobs
- Fire-and-forget jobs
- Continuation jobs
- Dashboard UI

**Configurazione:**
```csharp
builder.Services.AddHangfire(config =>
{
    config.UseSqlServerStorage(connectionString)
          .UseConsole();
});

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 5;
    options.ServerName = "DocN-Worker";
});

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});
```

**Job examples:**
```csharp
// Fire-and-forget
BackgroundJob.Enqueue(() => ProcessDocumentAsync(docId));

// Delayed
BackgroundJob.Schedule(() => SendReminderAsync(userId), TimeSpan.FromHours(24));

// Recurring
RecurringJob.AddOrUpdate(
    "cleanup-old-logs",
    () => CleanupOldLogsAsync(),
    Cron.Daily
);
```

**Dashboard**: `https://localhost:5211/hangfire`

### Caching

#### 8. Redis (StackExchange.Redis v2.8.16)
**Scopo**: Distributed caching

**Configurazione:**
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["Redis:ConnectionString"];
    options.InstanceName = "DocN:";
});

builder.Services.AddSingleton<IDistributedCacheService, DistributedCacheService>();
```

**Utilizzo:**
```csharp
public class DistributedCacheService
{
    /// <summary>
    /// Ottiene embedding da cache o lo genera
    /// </summary>
    /// <param name="text">Testo da convertire</param>
    /// <returns>Embedding vettoriale</returns>
    /// <output>Float array di 768 o 1536 dimensioni</output>
    public async Task<float[]?> GetOrGenerateEmbeddingAsync(string text)
    {
        var cacheKey = $"embedding:{ComputeHash(text)}";
        
        // Try cache
        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<float[]>(cached);
        }
        
        // Generate
        var embedding = await _embeddingService.GenerateEmbeddingAsync(text);
        
        // Cache with 1 hour expiration
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(embedding),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            }
        );
        
        return embedding;
    }
}
```

**Cached items:**
- Embeddings (1 hour)
- AI configurations (5 minutes)
- Search results (30 seconds)
- User sessions

---

## Struttura del Progetto

```
DocN.Server/
│
├── Program.cs                          # Entry point e configurazione
│
├── Controllers/                        # API Controllers
│   ├── DocumentsController.cs          # CRUD documenti
│   ├── SearchController.cs             # Ricerca (semantic, hybrid)
│   ├── ChatController.cs               # Chat basic
│   ├── SemanticChatController.cs       # Chat avanzata con SK
│   ├── ConfigController.cs             # Configurazione AI
│   ├── AgentController.cs              # Gestione agenti
│   ├── AuditController.cs              # Audit logs
│   └── LogsController.cs               # Application logs
│
├── Services/                           # Application Services
│   ├── DatabaseSeeder.cs               # Data seeding
│   ├── DistributedCacheService.cs      # Redis caching
│   └── HealthChecks/                   # Health check implementations
│       ├── AIProviderHealthCheck.cs
│       ├── OCRServiceHealthCheck.cs
│       ├── FileStorageHealthCheck.cs
│       └── SemanticKernelHealthCheck.cs
│
├── Middleware/                         # Custom middleware
│   ├── ExceptionHandlingMiddleware.cs  # Global exception handler
│   ├── RequestLoggingMiddleware.cs     # Request/response logging
│   └── RateLimitingMiddleware.cs       # Rate limiting
│
├── appsettings.json                    # Configurazione base
├── appsettings.Development.json        # Config development
├── appsettings.Production.json         # Config production
│
└── DocN.Server.csproj                  # Project file
```

---

## API Endpoints

### Documents API

**Base path:** `/documents`

#### GET /documents
```
Descrizione: Ottiene lista documenti
Autorizzazione: Richiesta
Query params:
  - page: int (default 1)
  - pageSize: int (default 20)
  - category: int? (optional)
  - search: string? (optional)
Risposta: 200 OK
  Body: { items: Document[], totalCount: int, page: int, pageSize: int }
```

#### GET /documents/{id}
```
Descrizione: Ottiene documento specifico
Autorizzazione: Richiesta (owner o shared)
Path params:
  - id: int
Risposta: 200 OK / 404 Not Found
  Body: Document
```

#### POST /documents
```
Descrizione: Crea nuovo documento
Autorizzazione: Richiesta
Body: Document (JSON)
Risposta: 201 Created
  Location header: /documents/{id}
  Body: Document creato
```

#### PUT /documents/{id}
```
Descrizione: Aggiorna documento
Autorizzazione: Richiesta (owner)
Path params:
  - id: int
Body: Document (JSON)
Risposta: 200 OK / 404 Not Found
```

#### DELETE /documents/{id}
```
Descrizione: Elimina documento
Autorizzazione: Richiesta (owner)
Path params:
  - id: int
Risposta: 204 No Content / 404 Not Found
```

### Search API

**Base path:** `/search`

#### POST /search/semantic
```
Descrizione: Ricerca semantica basata su embeddings
Autorizzazione: Richiesta
Body: {
  query: string,
  topK: int,
  threshold: float,
  categories: int[]? (optional)
}
Risposta: 200 OK
  Body: {
    results: [{
      documentId: int,
      title: string,
      excerpt: string,
      score: float
    }],
    totalResults: int
  }
```

#### POST /search/hybrid
```
Descrizione: Ricerca ibrida (semantic + fulltext)
Autorizzazione: Richiesta
Body: {
  query: string,
  topK: int,
  semanticWeight: float (0-1),
  categories: int[]?
}
Risposta: 200 OK
  Body: SearchResults con score ibridi
```

### Chat API

**Base path:** `/chat`

#### POST /chat/query
```
Descrizione: Query RAG singola
Autorizzazione: Richiesta
Body: {
  query: string,
  chatHistory: ChatMessage[]?,
  options: {
    maxDocuments: int,
    similarityThreshold: float,
    temperature: float
  }?
}
Risposta: 200 OK
  Body: {
    answer: string,
    sources: RetrievedDocument[],
    citations: Citation[],
    confidence: float,
    tokensUsed: int
  }
```

#### POST /chat/stream (SignalR)
```
Descrizione: Query RAG con streaming response
Autorizzazione: Richiesta
Connection: SignalR WebSocket
Methods:
  - SendMessage(query, conversationId)
  - ReceiveMessageChunk(chunk)
  - ReceiveComplete(metadata)
```

### Config API

**Base path:** `/config`

#### GET /config/ai
```
Descrizione: Ottiene configurazioni AI
Autorizzazione: Admin
Risposta: 200 OK
  Body: AIConfiguration[]
```

#### POST /config/ai
```
Descrizione: Crea/aggiorna configurazione AI
Autorizzazione: Admin
Body: AIConfiguration (JSON)
Risposta: 200 OK / 201 Created
```

#### POST /config/ai/{id}/test
```
Descrizione: Testa configurazione AI
Autorizzazione: Admin
Path params:
  - id: int
Risposta: 200 OK
  Body: {
    success: bool,
    message: string,
    details: object
  }
```

### Agents API

**Base path:** `/agents`

#### GET /agents
```
Descrizione: Lista agenti disponibili
Autorizzazione: Richiesta
Risposta: 200 OK
  Body: AgentConfiguration[]
```

#### POST /agents
```
Descrizione: Crea nuovo agente
Autorizzazione: Admin
Body: AgentConfiguration
Risposta: 201 Created
```

#### POST /agents/{id}/invoke
```
Descrizione: Invoca agente specifico
Autorizzazione: Richiesta
Path params:
  - id: int
Body: {
  query: string,
  context: object?
}
Risposta: 200 OK
  Body: {
    response: string,
    documents: Document[],
    confidence: float
  }
```

---

## Servizi e Middleware

### Custom Middleware

#### ExceptionHandlingMiddleware

**Scopo:** Gestione centralizzata eccezioni.

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    
    /// <summary>
    /// Gestisce eccezioni non catturate trasformandole in risposte HTTP appropriate
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <output>Response JSON con errore e status code appropriato</output>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error");
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "VALIDATION_ERROR",
                message = ex.Message,
                errors = ex.Errors
            });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "NOT_FOUND",
                message = ex.Message
            });
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access");
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "FORBIDDEN",
                message = "Non autorizzato ad accedere a questa risorsa"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "INTERNAL_SERVER_ERROR",
                message = "Si è verificato un errore interno. Riprova più tardi.",
                requestId = context.TraceIdentifier
            });
        }
    }
}
```

#### RequestLoggingMiddleware

**Scopo:** Log strutturato richieste HTTP.

```csharp
/// <summary>
/// Logga informazioni richiesta e risposta HTTP
/// </summary>
/// <output>Log entry con method, path, duration, status code</output>
public async Task InvokeAsync(HttpContext context)
{
    var sw = Stopwatch.StartNew();
    
    // Log request
    _logger.LogInformation(
        "HTTP {Method} {Path} started",
        context.Request.Method,
        context.Request.Path
    );
    
    try
    {
        await _next(context);
    }
    finally
    {
        sw.Stop();
        
        // Log response
        _logger.LogInformation(
            "HTTP {Method} {Path} completed in {Duration}ms with status {StatusCode}",
            context.Request.Method,
            context.Request.Path,
            sw.ElapsedMilliseconds,
            context.Response.StatusCode
        );
    }
}
```

### Application Services

#### DatabaseSeeder

**Scopo:** Inizializzazione dati di base.

```csharp
public class DatabaseSeeder
{
    /// <summary>
    /// Seed database con dati iniziali (categorie, template agenti, etc.)
    /// </summary>
    /// <output>Database popolato con dati di default</output>
    public async Task SeedAsync()
    {
        // Seed categorie predefinite
        if (!await _context.Categories.AnyAsync())
        {
            var categories = new[]
            {
                new Category { Name = "Contratti", Description = "Documenti contrattuali" },
                new Category { Name = "Fatture", Description = "Fatture e documenti fiscali" },
                new Category { Name = "Manuali", Description = "Manuali e documentazione tecnica" },
                new Category { Name = "Procedure", Description = "Procedure operative" },
                new Category { Name = "Report", Description = "Report e analisi" }
            };
            
            _context.Categories.AddRange(categories);
            await _context.SaveChangesAsync();
        }
        
        // Seed template agenti
        await _agentTemplateSeeder.SeedTemplatesAsync();
    }
}
```

---

## Health Checks e Monitoring

### Health Check Implementations

#### AIProviderHealthCheck

```csharp
public class AIProviderHealthCheck : IHealthCheck
{
    /// <summary>
    /// Verifica disponibilità provider AI configurati
    /// </summary>
    /// <output>HealthCheckResult con status di ogni provider</output>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var unhealthyProviders = new List<string>();
        
        // Controlla Gemini
        try
        {
            if (await _aiService.IsProviderAvailableAsync(AIProviderType.Gemini))
            {
                data["gemini"] = "Available";
            }
            else
            {
                data["gemini"] = "Not Configured";
            }
        }
        catch (Exception ex)
        {
            data["gemini"] = $"Error: {ex.Message}";
            unhealthyProviders.Add("Gemini");
        }
        
        // Controlla OpenAI
        // ... similar checks
        
        if (unhealthyProviders.Any())
        {
            return HealthCheckResult.Degraded(
                $"AI Providers unhealthy: {string.Join(", ", unhealthyProviders)}",
                data: data
            );
        }
        
        return HealthCheckResult.Healthy("All AI providers available", data);
    }
}
```

### Metrics Collection

**Custom metrics:**
```csharp
public static class MetricsRegistry
{
    // Counters
    public static readonly CounterOptions DocumentsProcessedCounter = new()
    {
        Name = "documents_processed_total",
        MeasurementUnit = Unit.Items
    };
    
    // Histograms
    public static readonly HistogramOptions AICallLatency = new()
    {
        Name = "ai_call_duration_ms",
        MeasurementUnit = Unit.Custom("milliseconds")
    };
    
    // Gauges
    public static readonly GaugeOptions PendingDocumentsGauge = new()
    {
        Name = "pending_documents",
        MeasurementUnit = Unit.Items
    };
}

// Utilizzo
_metrics.Measure.Counter.Increment(MetricsRegistry.DocumentsProcessedCounter);
_metrics.Measure.Histogram.Update(MetricsRegistry.AICallLatency, duration);
```

---

## Per Analisti

### Cosa Offre DocN.Server?

DocN.Server è il **cuore operativo enterprise** che fornisce:

1. **API Scalabili**: REST API performanti per tutte le operazioni
2. **RAG Avanzato**: Funzionalità AI all'avanguardia (HyDE, re-ranking, etc.)
3. **Monitoring**: Visibilità completa su performance e health
4. **Enterprise Ready**: Caching, background jobs, health checks

### Vantaggi Business

- **Affidabilità**: Health checks e monitoring proattivo
- **Performance**: Caching distribuito, batch processing
- **Scalabilità**: Architettura pronta per load balancing e scaling orizzontale
- **Manutenibilità**: Logging strutturato, tracing, metrics

---

## Per Sviluppatori

### Come Estendere DocN.Server

**Aggiungere Nuovo Endpoint:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class MyController : ControllerBase
{
    /// <summary>
    /// Descrizione endpoint
    /// </summary>
    /// <param name="id">Parametro</param>
    /// <returns>Risultato</returns>
    /// <response code="200">Successo</response>
    /// <response code="404">Non trovato</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MyResult), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<MyResult>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result != null ? Ok(result) : NotFound();
    }
}
```

**Aggiungere Health Check Custom:**

```csharp
public class MyServiceHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verifica servizio
            var isHealthy = await _service.IsAvailableAsync();
            
            return isHealthy
                ? HealthCheckResult.Healthy("Service is available")
                : HealthCheckResult.Unhealthy("Service is unavailable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Health check failed",
                ex
            );
        }
    }
}

// Registrazione
builder.Services.AddHealthChecks()
    .AddCheck<MyServiceHealthCheck>("my-service");
```

### Best Practices

1. **Async/await** per tutte le operations
2. **XML comments** su tutti i controller methods
3. **ProducesResponseType** per Swagger documentation
4. **Structured logging** con Serilog
5. **Exception handling** tramite middleware
6. **Validation** con Data Annotations o FluentValidation
7. **Unit tests** per business logic
8. **Integration tests** per endpoints

### Testing

**Unit Test Controller:**
```csharp
public class DocumentsControllerTests
{
    [Fact]
    public async Task GetDocuments_ReturnsOk()
    {
        // Arrange
        var mockService = new Mock<IDocumentService>();
        mockService.Setup(s => s.GetDocumentsAsync())
            .ReturnsAsync(new List<Document> { new Document { Id = 1 } });
        
        var controller = new DocumentsController(mockService.Object, Mock.Of<ILogger>());
        
        // Act
        var result = await controller.GetDocuments();
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var documents = Assert.IsType<List<Document>>(okResult.Value);
        Assert.Single(documents);
    }
}
```

---

**Versione Documento**: 1.0  
**Data Aggiornamento**: Dicembre 2024  
**Autori**: Team DocN  
**Target Audience**: Analisti e Sviluppatori
