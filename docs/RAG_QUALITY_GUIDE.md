# RAG Quality Verification & RAGAS Metrics Guide

## 📋 Overview

This guide explains how to use DocN's automatic RAG quality verification system and RAGAS metrics for continuous monitoring and improvement of your RAG implementation.

**Version**: 1.0  
**Last Updated**: January 2026

---

## 🎯 What is RAG Quality Verification?

RAG (Retrieval-Augmented Generation) quality verification ensures that AI-generated responses are:
- **Accurate**: Based on source documents
- **Relevant**: Answer the user's question
- **Trustworthy**: No hallucinations or unsupported claims
- **Cited**: Properly reference source material

---

## 🔍 Components

### 1. RAG Quality Service

Provides real-time quality checks for individual responses:
- **Cross-reference with source documents**
- **Confidence score per statement**
- **Hallucination detection**
- **Citation verification**
- **Low-confidence warnings**

### 2. RAGAS Metrics Service

Evaluates RAG system performance using industry-standard metrics:
- **Faithfulness**: Response grounded in context
- **Answer Relevancy**: Response relevant to query
- **Context Precision**: Retrieved contexts are relevant
- **Context Recall**: All relevant contexts retrieved

### 3. Continuous Monitoring

Tracks quality over time:
- **Quality trend analysis**
- **Degradation alerts**
- **A/B testing framework**
- **Golden dataset evaluation**

---

## 🚀 Quick Start

### Verify a Single Response

```bash
curl -X POST https://localhost:5211/api/rag-quality/verify \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What is the main purpose of DocN?",
    "response": "DocN is a document management system with RAG capabilities...",
    "sourceDocumentIds": ["doc-id-1", "doc-id-2"]
  }'
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
    "unverifiedCitations": 0
  },
  "qualityWarnings": []
}
```

### Evaluate with RAGAS Metrics

```bash
curl -X POST https://localhost:5211/api/rag-quality/ragas/evaluate \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What features does DocN provide?",
    "response": "DocN provides document management, RAG, and semantic search.",
    "contexts": [
      "DocN is an advanced document management system...",
      "Key features include RAG and semantic search..."
    ],
    "groundTruth": "DocN is a document management system with RAG capabilities."
  }'
```

**Response**:
```json
{
  "faithfulnessScore": 0.92,
  "answerRelevancyScore": 0.88,
  "contextPrecisionScore": 0.85,
  "contextRecallScore": 0.90,
  "overallRAGASScore": 0.89,
  "insights": [
    "Excellent RAG quality - all metrics are strong"
  ]
}
```

---

## 📊 Understanding Metrics

### Confidence Score (0.0 - 1.0)

Measures how well each statement is supported by source documents.

- **0.8 - 1.0**: High confidence ✅
- **0.6 - 0.8**: Medium confidence ⚠️
- **< 0.6**: Low confidence ❌

**Example**:
```json
{
  "statementConfidenceScores": {
    "DocN uses semantic search": 0.95,
    "It was created in 2020": 0.35  // Low confidence - may be hallucination
  }
}
```

### Faithfulness Score (RAGAS)

**Definition**: Percentage of statements in response that are supported by the retrieved contexts.

**Formula**: `(Supported Statements) / (Total Statements)`

**Threshold**: > 0.75

**What it measures**: How much of the response is grounded in actual source material vs. made up.

**Example**:
```
Response: "DocN uses semantic search. It has 1 million users. It supports PDFs."
Contexts: ["DocN uses semantic search", "It supports PDF documents"]

Faithfulness = 2/3 = 0.67 ❌ (One statement not supported)
```

### Answer Relevancy Score (RAGAS)

**Definition**: How relevant the response is to the user's query.

**Threshold**: > 0.75

**What it measures**: Whether the response actually answers the question asked.

**Example**:
```
Query: "How do I upload documents to DocN?"
Response A: "DocN supports multiple document formats..." ❌ Irrelevant
Response B: "To upload documents, click the Upload button..." ✅ Relevant
```

### Context Precision (RAGAS)

**Definition**: Percentage of retrieved contexts that are actually relevant to the query.

**Formula**: `(Relevant Contexts) / (Total Retrieved Contexts)`

**Threshold**: > 0.70

**What it measures**: Quality of retrieval - are we getting the right documents?

**Example**:
```
Query: "How to export documents?"
Retrieved 5 contexts:
- 3 about export functionality ✅
- 2 about import functionality ❌

Context Precision = 3/5 = 0.60 ⚠️ (Too much noise)
```

### Context Recall (RAGAS)

**Definition**: How much of the necessary information was retrieved.

**Formula**: `(Retrieved Relevant Info) / (Total Relevant Info)`

**Threshold**: > 0.70

**What it measures**: Completeness of retrieval - did we get all the relevant documents?

**Example**:
```
Total relevant information: 5 documents about "export"
Retrieved: 3 of those 5 documents

Context Recall = 3/5 = 0.60 ⚠️ (Missing 40% of relevant info)
```

### Overall RAGAS Score

**Definition**: Harmonic mean of all RAGAS metrics.

**Formula**: `n / (1/faithfulness + 1/relevancy + 1/precision + 1/recall)`

**Target**: > 0.75

**Interpretation**:
- **0.80 - 1.0**: Excellent RAG system ✅
- **0.70 - 0.80**: Good, minor improvements possible ⚠️
- **< 0.70**: Needs improvement ❌

---

## 🎯 Quality Thresholds

### Production Targets

| Metric | Target | Warning | Critical |
|--------|--------|---------|----------|
| Overall Confidence | > 0.80 | < 0.70 | < 0.60 |
| Faithfulness | > 0.80 | < 0.75 | < 0.65 |
| Answer Relevancy | > 0.80 | < 0.75 | < 0.65 |
| Context Precision | > 0.75 | < 0.70 | < 0.60 |
| Context Recall | > 0.75 | < 0.70 | < 0.60 |
| Hallucination Rate | < 5% | > 10% | > 20% |
| Citation Verification | > 95% | < 90% | < 80% |

---

## 📈 Monitoring Dashboard

### Get Quality Dashboard

```bash
curl https://localhost:5211/api/rag-quality/dashboard?from=2026-01-01&to=2026-01-31
```

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
      "contextPrecisionScore": 0.79,
      "contextRecallScore": 0.81,
      "overallRAGASScore": 0.82
    },
    "qualityTrend": 0.05  // Improving 5%
  }
}
```

### Get Active Alerts

```bash
curl https://localhost:5211/api/alerts/active
```

---

## 🧪 A/B Testing RAG Configurations

Compare two different RAG configurations:

```bash
curl -X POST https://localhost:5211/api/rag-quality/ragas/ab-test \
  -H "Content-Type: application/json" \
  -d '{
    "configurationA": "default",
    "configurationB": "experimental",
    "testDatasetId": "golden-dataset-v1"
  }'
```

**Response**:
```json
{
  "configurationA": "default",
  "configurationB": "experimental",
  "scoresA": {
    "overallRAGASScore": 0.78
  },
  "scoresB": {
    "overallRAGASScore": 0.85
  },
  "winner": "experimental",
  "improvementPercentages": {
    "faithfulness": 8.5,
    "relevancy": 6.2,
    "precision": 9.1,
    "recall": 5.8
  },
  "isStatisticallySignificant": true,
  "sampleSize": 100
}
```

---

## 🗂️ Golden Dataset Management

### What is a Golden Dataset?

A curated set of query-answer pairs with known correct responses, used for:
- Regression testing
- Configuration comparison
- Continuous evaluation
- Benchmark tracking

### Structure

```json
{
  "id": "golden-dataset-v1",
  "samples": [
    {
      "query": "How do I upload a document?",
      "groundTruth": "Click the Upload button and select your file.",
      "relevantDocIds": ["doc-user-guide"],
      "expectedResponse": "To upload a document..."
    }
  ]
}
```

### Best Practices

1. **Coverage**: Include diverse query types
2. **Quality**: Manually verify ground truth
3. **Maintenance**: Update as system evolves
4. **Size**: Start with 50-100 samples, grow over time

---

## 🚨 Quality Degradation Alerts

The system automatically alerts when quality degrades:

### Alert Configuration

```json
{
  "metricName": "faithfulness",
  "currentValue": 0.68,
  "threshold": 0.75,
  "previousValue": 0.82,
  "detectedAt": "2026-01-03T18:00:00Z",
  "severity": "warning"
}
```

### Common Causes

1. **Faithfulness Drop**:
   - New documents with poor quality
   - Prompt template changes
   - AI model degradation

2. **Relevancy Drop**:
   - Query rewriting issues
   - Poor document ranking
   - Context window too small

3. **Precision Drop**:
   - Retrieval returning too much noise
   - Similarity threshold too low
   - Metadata filtering not working

4. **Recall Drop**:
   - Retrieval returning too few documents
   - Similarity threshold too high
   - Missing embeddings

---

## 🔧 Troubleshooting

### Low Confidence Scores

**Symptoms**: Overall confidence < 0.70

**Possible Causes**:
1. Poor document quality
2. Insufficient source material
3. Incorrect document chunking
4. Wrong retrieval parameters

**Solutions**:
1. Review and improve document preprocessing
2. Add more relevant documents
3. Adjust chunk size and overlap
4. Tune similarity threshold

### High Hallucination Rate

**Symptoms**: Hallucination rate > 10%

**Possible Causes**:
1. Context window too small
2. AI model generating beyond context
3. Poor prompt engineering
4. Insufficient grounding instructions

**Solutions**:
1. Increase number of retrieved documents
2. Add stronger grounding instructions to prompts
3. Use lower temperature for generation
4. Implement stricter hallucination filtering

### Poor Citation Verification

**Symptoms**: Citation verification < 90%

**Possible Causes**:
1. Response not including citations
2. Citation format incorrect
3. Source documents not accessible
4. Chunking breaks citation context

**Solutions**:
1. Enforce citation format in prompts
2. Validate citation format before storage
3. Ensure all source docs are indexed
4. Adjust chunk boundaries

---

## 📚 API Reference

### RAG Quality Endpoints

- `POST /api/rag-quality/verify` - Verify response quality
- `POST /api/rag-quality/hallucinations` - Detect hallucinations
- `GET /api/rag-quality/metrics` - Get quality metrics
- `GET /api/rag-quality/dashboard` - Get dashboard data

### RAGAS Endpoints

- `POST /api/rag-quality/ragas/evaluate` - Evaluate with RAGAS
- `GET /api/rag-quality/ragas/monitoring` - Get monitoring metrics
- `POST /api/rag-quality/ragas/ab-test` - Run A/B test

### Alert Endpoints

- `GET /api/alerts/active` - Get active alerts
- `GET /api/alerts/statistics` - Get alert statistics
- `POST /api/alerts/{id}/acknowledge` - Acknowledge alert
- `POST /api/alerts/{id}/resolve` - Resolve alert

---

## 🎓 Best Practices

### 1. Continuous Monitoring

- Monitor quality metrics daily
- Set up automated alerts
- Review weekly trends
- Investigate degradations immediately

### 2. Regular Evaluation

- Run golden dataset evaluation weekly
- Update golden dataset monthly
- Compare configurations before deployment
- Track improvements over time

### 3. Quality Gates

- Enforce minimum confidence scores
- Block responses with high hallucination risk
- Require citation verification
- Add warnings for low-confidence responses

### 4. Improvement Process

1. **Measure**: Run comprehensive evaluation
2. **Analyze**: Identify weak areas
3. **Experiment**: Test improvements with A/B testing
4. **Deploy**: Roll out winning configuration
5. **Monitor**: Verify improvement persists

---

## 🔗 Additional Resources

- [RAGAS Framework](https://github.com/explodinggradients/ragas)
- [Alerting Runbook](./ALERTING_RUNBOOK.md)
- [DocN Architecture](../README.md)
- [Gap Analysis](../GAP_ANALYSIS_E_RACCOMANDAZIONI.md)

---

**Remember**: High-quality RAG is an iterative process. Continuous monitoring and improvement are key to success!
