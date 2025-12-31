# Monitoring, APM & Background Jobs Implementation

## Overview
This document describes the comprehensive Application Performance Monitoring (APM), distributed tracing, metrics collection, and background job processing implementation for DocN.

## 1. Structured Logging with Serilog ✅

### Features Implemented
- **Console Logging**: Real-time structured logs with color coding
- **File Logging**: Daily rolling log files with 30-day retention
- **Enrichers**:
  - Environment name (Development, Staging, Production)
  - Machine name
  - Thread ID
  - Log context

### Configuration
Logs are stored in the `logs/` directory with the pattern `docn-YYYYMMDD.log`.

### Log Levels
- **Information**: Application startup, shutdown, configuration
- **Warning**: Entity Framework, Microsoft framework warnings
- **Error**: Exception details with stack traces
- **Fatal**: Critical application failures

### Usage Example
```csharp
_logger.LogInformation("Processing document {DocumentId}", documentId);
_logger.LogWarning("Cache miss for key {CacheKey}", cacheKey);
_logger.LogError(ex, "Failed to process document {DocumentId}", documentId);
```

## 2. Distributed Tracing with OpenTelemetry ✅

### Features Implemented
- **ASP.NET Core Instrumentation**: Automatic request/response tracing
- **HTTP Client Instrumentation**: Outgoing HTTP call tracing
- **SQL Client Instrumentation**: Database query tracing with full SQL statements
- **Custom Sources**: Support for custom `DocN.*` activity sources

### Exporters Configured
1. **Console Exporter**: Development debugging
2. **OTLP Exporter**: Compatible with Jaeger, Zipkin, and other OpenTelemetry collectors
   - Configure endpoint via `OpenTelemetry:OtlpEndpoint` in appsettings

### Trace Context Propagation
Automatically propagates W3C Trace Context across service boundaries.

### Integration with External Tools
```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://jaeger:4317",  // Jaeger
    // OR
    "OtlpEndpoint": "http://zipkin:9411",   // Zipkin
    // OR
    "OtlpEndpoint": "http://collector:4317" // Generic OTLP
  }
}
```

## 3. Metrics with OpenTelemetry ✅

### Metrics Collected
- **ASP.NET Core Metrics**:
  - Request count, duration, and status codes
  - Active connections
  - Request queue length
  
- **HTTP Client Metrics**:
  - Outgoing request duration
  - Request failures
  
- **Runtime Metrics**:
  - GC collections and heap size
  - ThreadPool threads
  - Exception count
  - Process CPU and memory usage

### Prometheus Endpoint
- **URL**: `/metrics`
- **Format**: Prometheus text format
- **Scrape Interval**: Recommended 15-30 seconds

### Prometheus Configuration Example
```yaml
scrape_configs:
  - job_name: 'docn-server'
    scrape_interval: 15s
    static_configs:
      - targets: ['localhost:5211']
```

### Grafana Dashboard
Use the following metrics for visualization:
- `http_server_duration_milliseconds`: Request latency
- `http_server_request_total`: Request count
- `dotnet_gc_heap_size_bytes`: Memory usage
- `process_cpu_seconds_total`: CPU usage

## 4. Hangfire Background Jobs ✅

### Features Implemented
- **SQL Server Storage**: Persistent job queue with distributed locking
- **Dashboard**: Web UI at `/hangfire` for job monitoring
- **Console Extension**: Real-time job output in dashboard
- **Multiple Queues**:
  - `critical`: High-priority jobs (e.g., real-time document processing)
  - `default`: Normal priority jobs (e.g., embeddings generation)
  - `low`: Low-priority jobs (e.g., cleanup, reports)

### Worker Configuration
- **Worker Count**: `CPU cores × 2` for optimal throughput
- **Job Timeout**: 5 minutes default
- **Retry**: Automatic retry on failure with exponential backoff

### Dashboard Features
- Job status (Succeeded, Failed, Processing, Scheduled)
- Job history with execution times
- Real-time job progress with console output
- Recurring job management
- Server statistics

### Dashboard Access Control
Currently restricted to localhost in development. **TODO**: Implement proper authentication for production.

```csharp
// Production authentication example
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.IsInRole("Admin") || 
               httpContext.User.IsInRole("Operator");
    }
}
```

### Creating Background Jobs

#### Fire-and-Forget Jobs
```csharp
BackgroundJob.Enqueue(() => ProcessDocumentAsync(documentId));
```

#### Delayed Jobs
```csharp
BackgroundJob.Schedule(() => CleanupOldLogs(), TimeSpan.FromHours(24));
```

#### Recurring Jobs
```csharp
RecurringJob.AddOrUpdate(
    "cleanup-temp-files",
    () => CleanupTempFiles(),
    Cron.Daily);
```

#### Continuations
```csharp
var jobId = BackgroundJob.Enqueue(() => ProcessDocument(documentId));
BackgroundJob.ContinueJobWith(jobId, () => NotifyUser(documentId));
```

### Queue Priority
```csharp
[Queue("critical")]
public async Task ProcessUrgentDocument(int documentId)
{
    // High-priority processing
}

[Queue("low")]
public async Task GenerateMonthlyReport()
{
    // Low-priority background task
}
```

## 5. Health Checks ✅

### Endpoints

#### `/health` - Comprehensive Health Check
Returns detailed status of all components:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database is responsive",
      "duration": 45.2
    },
    {
      "name": "file_storage",
      "status": "Healthy",
      "description": "File storage is available and writable",
      "duration": 12.3
    }
  ],
  "totalDuration": 123.5
}
```

#### `/health/live` - Liveness Probe
For Kubernetes liveness checks. Returns 200 if application is running.

#### `/health/ready` - Readiness Probe
For Kubernetes readiness checks. Returns 200 only if all components are ready.

### Health Checks Implemented

1. **Database Check** (`database`)
   - Verifies ApplicationDbContext connectivity
   - Tests query execution
   - Tags: `ready`, `db`

2. **AI Provider Check** (`ai_provider`)
   - Verifies AI configuration exists
   - Checks provider availability
   - Tags: `ready`, `ai`

3. **OCR Service Check** (`ocr_service`)
   - Verifies Tesseract installation
   - Tags: `ready`, `ocr`

4. **Semantic Kernel Check** (`semantic_kernel`)
   - Verifies kernel initialization
   - Checks service availability
   - Tags: `ready`, `orchestration`

5. **File Storage Check** (`file_storage`)
   - Verifies directory exists and is writable
   - Checks available disk space
   - **Degraded** if < 5GB available
   - Tags: `ready`, `storage`

6. **Redis Cache Check** (`redis_cache`) [Optional]
   - Only added if Redis is configured
   - Verifies connection to Redis
   - Tags: `ready`, `cache`

### Kubernetes Configuration

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: docn-server
spec:
  containers:
  - name: docn
    image: docn-server:latest
    ports:
    - containerPort: 5211
    livenessProbe:
      httpGet:
        path: /health/live
        port: 5211
      initialDelaySeconds: 30
      periodSeconds: 10
      timeoutSeconds: 5
      failureThreshold: 3
    readinessProbe:
      httpGet:
        path: /health/ready
        port: 5211
      initialDelaySeconds: 15
      periodSeconds: 5
      timeoutSeconds: 3
      failureThreshold: 3
```

## 6. Redis Distributed Cache ✅

### Configuration
Redis is optional. If not configured, the application falls back to in-memory caching.

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,password=yourpassword,ssl=true"
  }
}
```

### Features When Redis is Configured
- Distributed cache shared across multiple instances
- Session persistence across server restarts
- Cache invalidation across all instances
- High-performance key-value store

### Cache Prefix
All keys are prefixed with `DocN:` to avoid conflicts with other applications.

## 7. Monitoring Stack Recommendations

### Development Environment
- **Logs**: Console + File
- **Traces**: Console exporter
- **Metrics**: OpenTelemetry Prometheus endpoint
- **Jobs**: Hangfire dashboard

### Production Environment

#### Option 1: Azure
- **Logs**: Azure Application Insights
- **Traces**: Application Insights distributed tracing
- **Metrics**: Application Insights metrics + Azure Monitor
- **Jobs**: Hangfire with Azure SQL

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=xxx;IngestionEndpoint=https://xxx"
  }
}
```

#### Option 2: Self-Hosted (ELK + Prometheus + Grafana)
- **Logs**: Elasticsearch + Logstash + Kibana
- **Traces**: Jaeger or Zipkin
- **Metrics**: Prometheus + Grafana
- **Jobs**: Hangfire dashboard

**Stack Components:**
```yaml
services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # Jaeger UI
      - "4317:4317"    # OTLP gRPC
  
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
  
  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
```

## 8. Performance Impact

### Overhead Estimates
- **Serilog**: ~1-2% CPU, negligible memory
- **OpenTelemetry Tracing**: ~2-5% CPU, ~10-20MB memory
- **OpenTelemetry Metrics**: ~1-3% CPU, ~5-10MB memory
- **Hangfire**: ~50-100MB memory (depends on job queue size)
- **Redis**: Network latency ~1-5ms per operation

### Optimization Tips
1. **Sampling**: Use trace sampling for high-traffic endpoints (e.g., 10%)
2. **Async Logging**: Serilog already uses async writes
3. **Batch Export**: OpenTelemetry batches by default
4. **Redis Connection Pooling**: StackExchange.Redis handles this automatically

## 9. Troubleshooting

### Logs Not Appearing
- Check `logs/` directory exists and is writable
- Verify log level in configuration
- Check disk space

### Traces Not in Jaeger
- Verify OTLP endpoint is reachable
- Check Jaeger/Zipkin is running
- Verify network connectivity

### Hangfire Dashboard 403 Forbidden
- Check you're accessing from localhost in development
- Implement proper authorization for production

### Redis Connection Failed
- Verify Redis is running: `redis-cli ping`
- Check connection string in appsettings
- Verify firewall rules

### High Memory Usage
- Check Hangfire job retention settings
- Review log file retention (currently 30 days)
- Monitor Redis memory usage

## 10. Future Enhancements

### Short Term
- [ ] Add custom business metrics (documents uploaded/day, searches/minute)
- [ ] Implement distributed tracing for AI provider calls
- [ ] Add alerting rules for Prometheus/Grafana
- [ ] Create pre-built Grafana dashboards

### Medium Term
- [ ] Add Application Insights integration option
- [ ] Implement distributed rate limiting with Redis
- [ ] Add job priority and SLA monitoring
- [ ] Create automated incident response playbooks

### Long Term
- [ ] Machine learning for anomaly detection
- [ ] Predictive scaling based on metrics
- [ ] Cost optimization recommendations
- [ ] Multi-region distributed tracing correlation

## Security Considerations

- **Hangfire Dashboard**: Implement authentication before production deployment
- **Metrics Endpoint**: Consider authentication for `/metrics` in production
- **Log Sanitization**: Ensure no sensitive data (passwords, API keys) in logs
- **Redis**: Use SSL/TLS and strong passwords for production
- **OTLP Endpoint**: Use authentication tokens if exposed externally

## Compliance Notes

- **GDPR**: Logs may contain PII - implement appropriate retention and access controls
- **SOC2**: Monitoring satisfies availability and logging controls
- **HIPAA**: Ensure PHI is not logged; use encryption at rest for log files

---

**Document Version**: 1.0  
**Date**: December 2024  
**Status**: Implemented ✅
