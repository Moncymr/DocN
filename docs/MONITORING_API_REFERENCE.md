# Monitoring & Alerting API Reference

## 📋 Overview

Complete API reference for DocN's monitoring, alerting, and RAG quality verification endpoints.

**Base URL**: `https://localhost:5211/api`  
**Version**: 1.0

---

## 🚨 Alerts API

### Get Active Alerts

Get all currently active alerts.

**Endpoint**: `GET /alerts/active`  
**Rate Limit**: 100 req/min

**Response**:
```json
[
  {
    "id": "alert-123",
    "name": "HighErrorRate",
    "description": "Error rate is 8.5%",
    "severity": "Critical",
    "source": "AlertMetricsMiddleware",
    "startsAt": "2026-01-03T18:00:00Z",
    "status": "Firing",
    "labels": {
      "error_rate": 0.085
    }
  }
]
```

---

### Get Alert Statistics

Get aggregated alert statistics for a time period.

**Endpoint**: `GET /alerts/statistics`  
**Rate Limit**: 100 req/min

**Query Parameters**:
- `from` (optional): Start date (ISO 8601)
- `to` (optional): End date (ISO 8601)

**Example**:
```bash
GET /alerts/statistics?from=2026-01-01T00:00:00Z&to=2026-01-31T23:59:59Z
```

**Response**:
```json
{
  "totalAlerts": 42,
  "activeAlerts": 3,
  "acknowledgedAlerts": 5,
  "resolvedAlerts": 34,
  "alertsBySeverity": {
    "Critical": 5,
    "Warning": 15,
    "Info": 22
  },
  "alertsBySource": {
    "AlertMetricsMiddleware": 30,
    "RAGQualityService": 12
  },
  "averageTimeToResolve": "00:15:30"
}
```

---

### Acknowledge Alert

Mark an alert as acknowledged.

**Endpoint**: `POST /alerts/{alertId}/acknowledge`  
**Rate Limit**: 100 req/min

**Request Body**:
```json
{
  "acknowledgedBy": "john.doe@company.com"
}
```

**Response**:
```json
{
  "message": "Alert acknowledged successfully"
}
```

---

### Resolve Alert

Mark an alert as resolved.

**Endpoint**: `POST /alerts/{alertId}/resolve`  
**Rate Limit**: 100 req/min

**Request Body**:
```json
{
  "resolvedBy": "john.doe@company.com"
}
```

**Response**:
```json
{
  "message": "Alert resolved successfully"
}
```

---

### Send Test Alert

Send a test alert to verify configuration.

**Endpoint**: `POST /alerts/test`  
**Rate Limit**: 100 req/min

**Request Body**:
```json
{
  "name": "Test Alert",
  "description": "Testing alert system",
  "severity": "Info"
}
```

**Response**:
```json
{
  "message": "Test alert sent successfully",
  "alertId": "alert-456"
}
```

---

## ✅ RAG Quality API

### Verify Response Quality

Verify the quality of a RAG response.

**Endpoint**: `POST /rag-quality/verify`  
**Rate Limit**: 20 concurrent req (AI rate limit)

**Request Body**:
```json
{
  "query": "What is DocN?",
  "response": "DocN is a document management system with RAG capabilities...",
  "sourceDocumentIds": ["1", "2", "3"]
}
```

**Response**:
```json
{
  "overallConfidenceScore": 0.85,
  "hasLowConfidenceWarnings": false,
  "lowConfidenceStatements": [],
  "hallucinationDetection": {
    "hasPotentialHallucinations": false,
    "hallucinations": [],
    "hallucinationScore": 0.0
  },
  "citationVerification": {
    "totalCitations": 3,
    "verifiedCitations": 3,
    "unverifiedCitations": 0,
    "citations": [
      {
        "citedText": "DocN is a document management system...",
        "sourceDocumentId": "1",
        "isVerified": true,
        "confidenceScore": 0.92
      }
    ]
  },
  "qualityWarnings": [],
  "statementConfidenceScores": {
    "DocN is a document management system": 0.95,
    "It has RAG capabilities": 0.88
  }
}
```

---

### Detect Hallucinations

Detect potential hallucinations in a response.

**Endpoint**: `POST /rag-quality/hallucinations`  
**Rate Limit**: 20 concurrent req

**Request Body**:
```json
{
  "response": "DocN was created in 2020 and has 1 million users.",
  "sourceTexts": [
    "DocN is a document management system...",
    "It provides RAG capabilities..."
  ]
}
```

**Response**:
```json
{
  "hasPotentialHallucinations": true,
  "hallucinations": [
    {
      "text": "DocN was created in 2020",
      "confidence": 0.15,
      "reason": "No supporting evidence found in source documents"
    },
    {
      "text": "has 1 million users",
      "confidence": 0.20,
      "reason": "No supporting evidence found in source documents"
    }
  ],
  "hallucinationScore": 0.82
}
```

---

### Get Quality Metrics

Get quality metrics for a time period.

**Endpoint**: `GET /rag-quality/metrics`  
**Rate Limit**: 100 req/min

**Query Parameters**:
- `from` (optional): Start date (ISO 8601)
- `to` (optional): End date (ISO 8601)

**Response**:
```json
{
  "totalResponses": 1523,
  "averageConfidenceScore": 0.83,
  "lowConfidenceResponses": 45,
  "hallucinationsDetected": 12,
  "citationVerificationRate": 0.96,
  "discrepanciesByType": {
    "QualityWarning": 45,
    "Hallucination": 12
  },
  "topWarnings": [
    "Low confidence responses detected",
    "Citations not verified"
  ]
}
```

---

## 📊 RAGAS Metrics API

### Evaluate with RAGAS

Evaluate a RAG response using RAGAS metrics.

**Endpoint**: `POST /rag-quality/ragas/evaluate`  
**Rate Limit**: 20 concurrent req

**Request Body**:
```json
{
  "query": "How do I upload documents?",
  "response": "To upload documents, click the Upload button...",
  "contexts": [
    "DocN provides document upload functionality...",
    "Users can upload files through the web interface..."
  ],
  "groundTruth": "Click the Upload button to upload documents"
}
```

**Response**:
```json
{
  "faithfulnessScore": 0.92,
  "answerRelevancyScore": 0.88,
  "contextPrecisionScore": 0.85,
  "contextRecallScore": 0.90,
  "overallRAGASScore": 0.89,
  "detailedMetrics": {
    "faithfulness": 0.92,
    "answer_relevancy": 0.88,
    "context_precision": 0.85,
    "context_recall": 0.90,
    "overall": 0.89
  },
  "insights": [
    "Excellent RAG quality - all metrics are strong"
  ]
}
```

---

### Get Monitoring Metrics

Get continuous monitoring metrics.

**Endpoint**: `GET /rag-quality/ragas/monitoring`  
**Rate Limit**: 100 req/min

**Query Parameters**:
- `from` (optional): Start date (ISO 8601)
- `to` (optional): End date (ISO 8601)

**Response**:
```json
{
  "totalEvaluations": 1523,
  "averageScores": {
    "faithfulnessScore": 0.85,
    "answerRelevancyScore": 0.82,
    "contextPrecisionScore": 0.79,
    "contextRecallScore": 0.81,
    "overallRAGASScore": 0.82
  },
  "trendData": {
    "2026-01-01T00:00:00Z": {
      "overallRAGASScore": 0.80
    },
    "2026-01-02T00:00:00Z": {
      "overallRAGASScore": 0.82
    }
  },
  "qualityAlerts": [
    {
      "metricName": "faithfulness",
      "currentValue": 0.68,
      "threshold": 0.75,
      "previousValue": 0.82,
      "detectedAt": "2026-01-03T18:00:00Z",
      "severity": "warning"
    }
  ],
  "qualityTrend": 0.05
}
```

---

### A/B Test RAG Configurations

Compare two RAG configurations.

**Endpoint**: `POST /rag-quality/ragas/ab-test`  
**Rate Limit**: 20 concurrent req

**Request Body**:
```json
{
  "configurationA": "default",
  "configurationB": "experimental",
  "testDatasetId": "golden-dataset-v1"
}
```

**Response**:
```json
{
  "configurationA": "default",
  "configurationB": "experimental",
  "scoresA": {
    "faithfulnessScore": 0.78,
    "answerRelevancyScore": 0.75,
    "contextPrecisionScore": 0.72,
    "contextRecallScore": 0.74,
    "overallRAGASScore": 0.75
  },
  "scoresB": {
    "faithfulnessScore": 0.85,
    "answerRelevancyScore": 0.82,
    "contextPrecisionScore": 0.80,
    "contextRecallScore": 0.83,
    "overallRAGASScore": 0.83
  },
  "winner": "experimental",
  "improvementPercentages": {
    "faithfulness": 8.97,
    "answer_relevancy": 9.33,
    "context_precision": 11.11,
    "context_recall": 12.16
  },
  "isStatisticallySignificant": true,
  "sampleSize": 100
}
```

---

### Get Quality Dashboard

Get combined quality dashboard data.

**Endpoint**: `GET /rag-quality/dashboard`  
**Rate Limit**: 100 req/min

**Query Parameters**:
- `from` (optional): Start date (ISO 8601)
- `to` (optional): End date (ISO 8601)

**Response**:
```json
{
  "quality": {
    "totalResponses": 1523,
    "averageConfidenceScore": 0.83,
    "lowConfidenceResponses": 45,
    "hallucinationsDetected": 12,
    "citationVerificationRate": 0.96
  },
  "ragas": {
    "totalEvaluations": 1523,
    "averageScores": {
      "faithfulnessScore": 0.85,
      "answerRelevancyScore": 0.82,
      "overallRAGASScore": 0.82
    },
    "qualityTrend": 0.05
  },
  "timestamp": "2026-01-03T18:30:00Z"
}
```

---

## 📈 Metrics API

### Get Alert Metrics

Get detailed metrics from the alert system.

**Endpoint**: `GET /metrics/alerts`  
**Rate Limit**: 100 req/min

**Response**:
```json
{
  "total_requests": 10523,
  "failed_requests": 45,
  "error_rate": 0.0043,
  "latency_by_endpoint": {
    "/api/documents": {
      "avg": 150.5,
      "p50": 120.0,
      "p95": 280.0,
      "p99": 450.0
    },
    "/api/search": {
      "avg": 220.3,
      "p50": 180.0,
      "p95": 420.0,
      "p99": 680.0
    }
  }
}
```

---

## 🏥 Health Check API

### Overall Health

**Endpoint**: `GET /health`  
**Rate Limit**: None

**Response**:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": null,
      "duration": 15.3
    },
    {
      "name": "ai_provider",
      "status": "Healthy",
      "description": "AI provider service is operational",
      "duration": 8.2
    }
  ],
  "totalDuration": 125.7
}
```

---

### Liveness Probe

Check if the application is running.

**Endpoint**: `GET /health/live`  
**Rate Limit**: None

**Response**: `200 OK` (Healthy) or `503 Service Unavailable` (Unhealthy)

---

### Readiness Probe

Check if the application is ready to serve requests.

**Endpoint**: `GET /health/ready`  
**Rate Limit**: None

**Response**: `200 OK` (Ready) or `503 Service Unavailable` (Not Ready)

---

## 📊 Prometheus Metrics

### Metrics Endpoint

Export Prometheus-compatible metrics.

**Endpoint**: `GET /metrics`  
**Rate Limit**: None

**Response** (Prometheus format):
```
# HELP http_server_requests_total Total number of HTTP requests
# TYPE http_server_requests_total counter
http_server_requests_total{method="GET",status="200"} 10478
http_server_requests_total{method="POST",status="200"} 3245
http_server_requests_total{method="GET",status="500"} 45

# HELP ragas_faithfulness_score RAGAS faithfulness score
# TYPE ragas_faithfulness_score gauge
ragas_faithfulness_score 0.85

# HELP ragas_overall_score Overall RAGAS score
# TYPE ragas_overall_score gauge
ragas_overall_score 0.82
```

---

## 🔐 Authentication

All API endpoints (except health checks and metrics) require authentication using JWT tokens or API keys.

**Header**: `Authorization: Bearer {token}`

---

## ⚠️ Rate Limits

| Category | Limit | Window |
|----------|-------|--------|
| General API | 100 req | 1 minute |
| AI Operations | 20 concurrent | - |
| Upload | 20 req | 15 minutes |
| Health Checks | Unlimited | - |
| Metrics | Unlimited | - |

**Rate Limit Response**:
```json
{
  "error": "Too many requests. Please try again later.",
  "retryAfter": 45
}
```
**Status Code**: `429 Too Many Requests`

---

## 📚 Additional Resources

- [Alerting Runbook](./ALERTING_RUNBOOK.md)
- [RAG Quality Guide](./RAG_QUALITY_GUIDE.md)
- [Monitoring Integration](./MONITORING_INTEGRATION_GUIDE.md)
- [Full API Documentation](/swagger)

---

**Version**: 1.0  
**Last Updated**: January 2026
