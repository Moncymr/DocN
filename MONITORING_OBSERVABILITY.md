# Monitoring & Observability - DocN

## 📊 Panoramica

Questa guida definisce strategie e implementazioni per monitoring, logging, e observability del sistema DocN.

## 🎯 Obiettivi

### I Tre Pilastri dell'Observability

1. **Metrics** - Metriche numeriche aggregate (CPU, RAM, richieste/sec)
2. **Logs** - Eventi discreti con timestamp e contesto
3. **Traces** - Percorso di una richiesta attraverso il sistema

---

## 📈 Metriche (Metrics)

### Metriche Business

Cosa misurare per capire il valore del business:

| Metrica | Descrizione | Target | Allarme |
|---------|-------------|--------|---------|
| **Documenti Caricati/Giorno** | Nuovi documenti | >100 | <10 |
| **Query RAG/Giorno** | Interazioni chat | >500 | <50 |
| **Ricerche/Giorno** | Ricerche eseguite | >1000 | <100 |
| **Utenti Attivi/Giorno** | Utenti che fanno login | >50 | <5 |
| **Tasso di Successo Upload** | % upload completati | >98% | <90% |
| **Tempo Medio Risposta RAG** | Latenza chat | <3s | >10s |
| **Costo AI per Query** | $ per query | <$0.05 | >$0.20 |

### Metriche Tecniche

#### Application Metrics

```csharp
// Implementazione con System.Diagnostics.Metrics (.NET)
public class ApplicationMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _documentUploadCounter;
    private readonly Counter<long> _searchCounter;
    private readonly Histogram<double> _uploadDuration;
    private readonly Histogram<double> _ragResponseTime;
    private readonly ObservableGauge<int> _activeUsers;
    
    public ApplicationMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("DocN.Application");
        
        _documentUploadCounter = _meter.CreateCounter<long>(
            "docn.documents.uploaded",
            description: "Number of documents uploaded");
        
        _searchCounter = _meter.CreateCounter<long>(
            "docn.searches.total",
            description: "Total number of searches");
        
        _uploadDuration = _meter.CreateHistogram<double>(
            "docn.upload.duration",
            unit: "ms",
            description: "Document upload duration");
        
        _ragResponseTime = _meter.CreateHistogram<double>(
            "docn.rag.response_time",
            unit: "ms",
            description: "RAG query response time");
        
        _activeUsers = _meter.CreateObservableGauge<int>(
            "docn.users.active",
            () => GetActiveUsersCount());
    }
    
    public void RecordDocumentUpload(string contentType, long sizeBytes)
    {
        _documentUploadCounter.Add(1, 
            new KeyValuePair<string, object?>("content_type", contentType));
    }
    
    public void RecordSearch(string searchType)
    {
        _searchCounter.Add(1,
            new KeyValuePair<string, object?>("type", searchType));
    }
    
    public IDisposable MeasureUploadDuration()
    {
        var startTime = Stopwatch.GetTimestamp();
        return new DurationMeasurement(() =>
        {
            var elapsed = Stopwatch.GetElapsedTime(startTime);
            _uploadDuration.Record(elapsed.TotalMilliseconds);
        });
    }
}

// Usage
using (metrics.MeasureUploadDuration())
{
    await UploadDocument();
}
```

#### Infrastructure Metrics

**ASP.NET Core Built-in:**
- `http.server.request.duration` - Durata richieste HTTP
- `http.server.active_requests` - Richieste attive
- `aspnetcore.routing.match_attempts` - Tentativi routing
- `aspnetcore.diagnostics.exceptions` - Eccezioni

**Database:**
- Connection pool size
- Query execution time
- Deadlocks
- Index fragmentation

**Cache (Redis):**
- Hit rate
- Memory usage
- Evictions
- Connection errors

---

## 📝 Logging

### Livelli di Log

| Livello | Quando Usare | Esempio |
|---------|--------------|---------|
| **Trace** | Debug dettagliato | Valori variabili, stato interno |
| **Debug** | Informazioni sviluppo | Query SQL, parametri chiamate |
| **Information** | Flusso normale | "Documento caricato", "Utente loggato" |
| **Warning** | Situazione inaspettata recuperabile | "API retry", "Cache miss" |
| **Error** | Errore che impedisce operazione | "Upload failed", "DB connection lost" |
| **Critical** | Errore che richiede attenzione immediata | "Sistema down", "Data corruption" |

### Structured Logging con Serilog

```csharp
// Program.cs
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "DocN")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/docn-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(
        serverUrl: context.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341",
        apiKey: context.Configuration["Seq:ApiKey"])
    .WriteTo.ApplicationInsights(
        services.GetRequiredService<TelemetryConfiguration>(),
        TelemetryConverter.Traces));
```

### Best Practices Logging

```csharp
// ✅ GOOD - Structured logging
_logger.LogInformation(
    "Document uploaded by {UserId} with size {SizeBytes} bytes and type {ContentType}",
    userId, document.FileSize, document.ContentType);

// ❌ BAD - String concatenation
_logger.LogInformation($"Document uploaded by {userId}");

// ✅ GOOD - Scope logging
using (_logger.BeginScope("Processing document {DocumentId}", documentId))
{
    _logger.LogInformation("Starting OCR");
    // OCR operations
    _logger.LogInformation("OCR completed");
}

// ✅ GOOD - Exception logging con context
try
{
    await ProcessDocument(document);
}
catch (Exception ex)
{
    _logger.LogError(ex, 
        "Failed to process document {DocumentId} for user {UserId}",
        document.Id, userId);
    throw;
}
```

### Log Aggregation

**Opzioni:**

1. **Seq** (self-hosted)
   - Sviluppato per .NET
   - Query potenti
   - UI intuitiva
   - Gratis per single user

2. **ELK Stack** (Elasticsearch, Logstash, Kibana)
   - Open source
   - Scalabile
   - Complesso da gestire

3. **Azure Application Insights**
   - Cloud-native
   - Integrazione Azure
   - Pay-per-use

4. **Splunk**
   - Enterprise-grade
   - Costoso
   - Potente analytics

---

## 🔍 Distributed Tracing

### OpenTelemetry Implementation

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("DocN.Application")
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService("DocN")
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName
            }))
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Endpoint"]);
        }));

// Custom tracing
public class DocumentService
{
    private static readonly ActivitySource ActivitySource = new("DocN.Application");
    
    public async Task<Document> ProcessDocumentAsync(Stream fileStream)
    {
        using var activity = ActivitySource.StartActivity("ProcessDocument");
        activity?.SetTag("document.size", fileStream.Length);
        
        try
        {
            // Extract text
            using (var extractActivity = ActivitySource.StartActivity("ExtractText"))
            {
                var text = await ExtractText(fileStream);
                extractActivity?.SetTag("text.length", text.Length);
            }
            
            // Generate embeddings
            using (var embeddingActivity = ActivitySource.StartActivity("GenerateEmbeddings"))
            {
                var embeddings = await GenerateEmbeddings(text);
                embeddingActivity?.SetTag("embedding.dimensions", embeddings.Length);
            }
            
            return document;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

### Trace Visualization

**Jaeger UI Example:**
```
Request: POST /api/documents
├─ DocumentService.ProcessDocument (150ms)
│  ├─ FileProcessingService.ExtractText (50ms)
│  │  └─ TesseractOCR.Process (45ms)
│  ├─ MultiProviderAI.GenerateEmbeddings (80ms)
│  │  └─ HTTP POST https://api.openai.com/v1/embeddings (75ms)
│  └─ DocumentRepository.Save (20ms)
│     └─ SQL INSERT Documents (18ms)
```

---

## 🎛️ Dashboard e Visualization

### Grafana Dashboard Example

```json
{
  "dashboard": {
    "title": "DocN - Overview",
    "panels": [
      {
        "title": "Documents Uploaded (24h)",
        "type": "graph",
        "targets": [
          {
            "expr": "sum(rate(docn_documents_uploaded_total[5m])) * 86400"
          }
        ]
      },
      {
        "title": "RAG Response Time (p95)",
        "type": "graph",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(docn_rag_response_time_bucket[5m]))"
          }
        ]
      },
      {
        "title": "Active Users",
        "type": "stat",
        "targets": [
          {
            "expr": "docn_users_active"
          }
        ]
      },
      {
        "title": "Error Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(docn_errors_total[5m])"
          }
        ]
      }
    ]
  }
}
```

### Key Dashboards

1. **System Overview**
   - Request rate
   - Error rate
   - Response time (p50, p95, p99)
   - Active users

2. **Document Processing**
   - Upload success/failure rate
   - Processing time per file type
   - OCR success rate
   - Embedding generation time

3. **RAG Performance**
   - Query response time
   - Retrieval accuracy (documents found)
   - AI provider usage
   - Cost per query

4. **Infrastructure**
   - CPU utilization
   - Memory usage
   - Disk I/O
   - Network bandwidth
   - Database connections

5. **Business Metrics**
   - Daily active users
   - Documents per user
   - Search queries per user
   - Retention rate

---

## 🚨 Alerting

### Alert Rules

```yaml
# Prometheus Alert Rules
groups:
- name: docn_alerts
  rules:
  
  # High Error Rate
  - alert: HighErrorRate
    expr: rate(docn_errors_total[5m]) > 0.1
    for: 5m
    labels:
      severity: critical
    annotations:
      summary: "High error rate detected"
      description: "Error rate is {{ $value }} errors/second"
  
  # Slow RAG Response
  - alert: SlowRAGResponse
    expr: histogram_quantile(0.95, rate(docn_rag_response_time_bucket[5m])) > 10000
    for: 10m
    labels:
      severity: warning
    annotations:
      summary: "RAG response time is slow"
      description: "P95 response time is {{ $value }}ms"
  
  # High CPU Usage
  - alert: HighCPUUsage
    expr: cpu_usage_percent > 80
    for: 10m
    labels:
      severity: warning
    annotations:
      summary: "High CPU usage"
      description: "CPU usage is {{ $value }}%"
  
  # Database Connection Pool Exhausted
  - alert: DatabaseConnectionPoolExhausted
    expr: database_connections_active / database_connections_max > 0.9
    for: 5m
    labels:
      severity: critical
    annotations:
      summary: "Database connection pool nearly exhausted"
```

### Alert Channels

```yaml
# Alertmanager config
global:
  smtp_smarthost: 'smtp.gmail.com:587'
  smtp_from: 'alerts@docn.example.com'

route:
  receiver: 'default-receiver'
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 1h
  
  routes:
  - match:
      severity: critical
    receiver: 'pagerduty-critical'
  - match:
      severity: warning
    receiver: 'email-team'

receivers:
- name: 'default-receiver'
  email_configs:
  - to: 'team@docn.example.com'

- name: 'pagerduty-critical'
  pagerduty_configs:
  - service_key: '<PAGERDUTY_KEY>'

- name: 'email-team'
  email_configs:
  - to: 'team@docn.example.com'
  slack_configs:
  - api_url: '<SLACK_WEBHOOK_URL>'
    channel: '#docn-alerts'
```

---

## 🏥 Health Checks

### Implementation

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddCheck<RedisHealthCheck>("redis")
    .AddCheck<AIProviderHealthCheck>("ai_provider")
    .AddCheck<FileStorageHealthCheck>("file_storage")
    .AddCheck<OCRServiceHealthCheck>("ocr_service");

// Custom health check
public class AIProviderHealthCheck : IHealthCheck
{
    private readonly IMultiProviderAIService _aiService;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get a simple response
            var response = await _aiService.GenerateChatCompletionAsync(
                "system", "test", cancellationToken);
            
            return response != null
                ? HealthCheckResult.Healthy("AI provider is responding")
                : HealthCheckResult.Degraded("AI provider returned null");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "AI provider is not responding",
                ex);
        }
    }
}

// Endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false  // No checks, just "alive"
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Health Check UI

```csharp
builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(30);
    setup.MaximumHistoryEntriesPerEndpoint(50);
    setup.AddHealthCheckEndpoint("DocN", "/health");
})
.AddInMemoryStorage();

app.MapHealthChecksUI(options => options.UIPath = "/health-ui");
```

---

## 📊 Monitoring Stack Completo

### Option 1: Prometheus + Grafana (Open Source)

```yaml
# docker-compose.monitoring.yml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    volumes:
      - grafana-data:/var/lib/grafana
      - ./grafana/dashboards:/etc/grafana/provisioning/dashboards
    depends_on:
      - prometheus

  alertmanager:
    image: prom/alertmanager:latest
    ports:
      - "9093:9093"
    volumes:
      - ./alertmanager.yml:/etc/alertmanager/alertmanager.yml
    command:
      - '--config.file=/etc/alertmanager/alertmanager.yml'

volumes:
  prometheus-data:
  grafana-data:
```

### Option 2: Azure Monitor (Cloud)

```csharp
// Application Insights + Azure Monitor
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    options.EnableAdaptiveSampling = true;
    options.EnableQuickPulseMetricStream = true;
});

// Custom metrics to Azure Monitor
var telemetryClient = new TelemetryClient();
telemetryClient.TrackMetric("DocumentsUploaded", 1);
telemetryClient.TrackEvent("SearchPerformed", 
    new Dictionary<string, string> { { "SearchType", "Hybrid" } });
```

### Option 3: ELK Stack (Elasticsearch, Logstash, Kibana)

```yaml
# docker-compose.elk.yml
version: '3.8'

services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
    ports:
      - "9200:9200"
    volumes:
      - elasticsearch-data:/usr/share/elasticsearch/data

  logstash:
    image: docker.elastic.co/logstash/logstash:8.11.0
    ports:
      - "5000:5000"
    volumes:
      - ./logstash.conf:/usr/share/logstash/pipeline/logstash.conf
    depends_on:
      - elasticsearch

  kibana:
    image: docker.elastic.co/kibana/kibana:8.11.0
    ports:
      - "5601:5601"
    environment:
      - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    depends_on:
      - elasticsearch

volumes:
  elasticsearch-data:
```

---

## 🎯 SLI, SLO, SLA

### Service Level Indicators (SLI)

Metriche misurabili:
- Availability: % di tempo che il servizio è disponibile
- Latency: Tempo di risposta per operazioni
- Throughput: Richieste processate per secondo
- Error rate: % di richieste fallite

### Service Level Objectives (SLO)

Target interni:
- **Availability**: 99.9% uptime mensile
- **Latency**: 
  - P95 < 500ms per ricerca
  - P95 < 3s per RAG query
- **Error rate**: < 1% per tutte le operazioni
- **Throughput**: Supportare 1000 richieste/sec

### Service Level Agreements (SLA)

Contratti con utenti:
- **Uptime**: 99.9% mensile (43 minuti downtime/mese)
- **Support response**: 
  - P1 (Critical): < 1 ora
  - P2 (High): < 4 ore
  - P3 (Medium): < 24 ore
- **Data retention**: Backup giornalieri, retention 30 giorni
- **Disaster recovery**: RPO < 1 ora, RTO < 4 ore

---

## ✅ Monitoring Checklist

### Setup Iniziale
- [ ] Prometheus configurato
- [ ] Grafana dashboards creati
- [ ] Alert rules definiti
- [ ] Health checks implementati
- [ ] Structured logging attivo
- [ ] Distributed tracing configurato

### Dashboards Essenziali
- [ ] System overview dashboard
- [ ] Application performance dashboard
- [ ] Business metrics dashboard
- [ ] Infrastructure dashboard
- [ ] Error tracking dashboard

### Alerting
- [ ] Email alerts configurati
- [ ] Slack/Teams integration
- [ ] PagerDuty per critical alerts
- [ ] On-call rotation definita

### Documentation
- [ ] Runbook per alert comuni
- [ ] Escalation procedures
- [ ] Dashboard documentation
- [ ] SLA documented

---

**Versione:** 1.0  
**Data:** Dicembre 2024
