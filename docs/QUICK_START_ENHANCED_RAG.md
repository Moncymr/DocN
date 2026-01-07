# Quick Start: Abilita Microsoft Agent Framework RAG

## 🚀 Come Attivare la Funzionalità

### Passo 1: Configurazione

Modifica `appsettings.json` o `appsettings.Development.json`:

```json
{
  "EnhancedRAG": {
    "UseEnhancedAgentRAG": true
  }
}
```

### Passo 2: Restart dell'Applicazione

```bash
dotnet run --project DocN.Server
```

L'applicazione caricherà automaticamente il nuovo servizio RAG con Microsoft Agent Framework.

---

## ⚠️ Note Importanti

### Stato Implementazione (Gennaio 2026)

#### ✅ Completato
- **Analisi completa** del codice esistente
- **Gap Analysis** dettagliata con priorità
- **Documentazione** comprensiva del Microsoft Agent Framework
- **Configurazione** feature flag e settings
- **Roadmap** di implementazione

#### 🚧 In Development
- **EnhancedAgentRAGService** - Richiede aggiustamenti per compatibilità con Document model esistente
- Gli agent correnti utilizzano interfacce custom, migrazione necessaria

#### 📋 Prossimi Passi

Per completare l'implementazione dell'Enhanced Agent RAG Service:

1. **Adatta al Document Model**
   - Cambia `Embeddings` → `EmbeddingVector`
   - Cambia `Content` → `ExtractedText`
   - Cambia `Title` → `FileName`

2. **Aggiorna API AgentGroupChat**
   - Semantic Kernel 1.29.0-alpha ha API specifiche
   - Usa `InvokeAsync` invece di `AddChatMessageAsync`
   - Implementa correttamente `TerminationStrategy`

3. **Test e Validazione**
   - Unit tests per i nuovi agent
   - Integration tests per pipeline completa
   - Performance benchmarks

---

## 📚 Documentazione Disponibile

### Guide Implementate

1. **[Microsoft Agent Framework Guide](./MICROSOFT_AGENT_FRAMEWORK_GUIDE.md)**
   - Architettura completa
   - 4 agent specializzati (Query, Retrieval, Reranking, Synthesis)
   - Best practices e troubleshooting
   - Esempi di codice completi

2. **[Gap Analysis e Raccomandazioni](./GAP_ANALYSIS_E_RACCOMANDAZIONI.md)**
   - Analisi dettagliata dello stato attuale
   - Gap identificati con priorità
   - Roadmap di implementazione (11-12 giorni)
   - ROI atteso (+10-15% qualità, -25-30% latency)

3. **[RAG Quality Guide](./RAG_QUALITY_GUIDE.md)** (Esistente)
   - RAGAS metrics e monitoring
   - Quality verification
   - Alert system

---

## 🎯 Benefici Attesi

### Qualità
- **+10-15%** RAGAS score medio
- **+25-30%** precision nel document ranking
- **-50%** hallucinations rate

### Performance
- **-25-30%** response time medio
- **-30-40%** token usage (costi ridotti)
- **+20%** cache hit rate

### Developer Experience
- **Migliore separazione** delle responsabilità
- **Più facile debugging** con telemetry dettagliata
- **Codice più maintainable** con agent specializzati

---

## 🔧 Fallback Strategy

Se l'Enhanced Agent RAG Service non è disponibile o configurato:

```json
{
  "EnhancedRAG": {
    "UseEnhancedAgentRAG": false
  }
}
```

Il sistema userà automaticamente `MultiProviderSemanticRAGService` (default, già funzionante).

---

## 📊 Monitoring

### Metriche Chiave da Monitorare

Dopo l'attivazione, controlla:

```sql
-- Performance per fase
SELECT 
    AVG(CAST(JSON_VALUE(Metadata, '$.pipelinePhases.queryAnalysis') AS BIGINT)) as AvgQueryMs,
    AVG(CAST(JSON_VALUE(Metadata, '$.pipelinePhases.retrieval') AS BIGINT)) as AvgRetrievalMs,
    AVG(CAST(JSON_VALUE(Metadata, '$.pipelinePhases.reranking') AS BIGINT)) as AvgRerankingMs,
    AVG(CAST(JSON_VALUE(Metadata, '$.pipelinePhases.synthesis') AS BIGINT)) as AvgSynthesisMs
FROM Messages
WHERE JSON_VALUE(Metadata, '$.agentFramework') = 'Microsoft.SemanticKernel.Agents';

-- Qualità risposte
SELECT 
    AVG(CAST(JSON_VALUE(Metadata, '$.topSimilarityScore') AS FLOAT)) as AvgSimilarity,
    AVG(CAST(JSON_VALUE(Metadata, '$.documentsRetrieved') AS INT)) as AvgDocsRetrieved
FROM Messages
WHERE Role = 'assistant';
```

### Dashboard Consigliati

1. **Response Time Trends** - Verifica miglioramenti latency
2. **Quality Metrics** - RAGAS scores nel tempo
3. **Cost Tracking** - Token usage reduction
4. **Agent Performance** - Breakdown per agent

---

## 🆘 Supporto

### Problemi Comuni

**Q: Il build fallisce con errori su EnhancedAgentRAGService?**
A: Disabilita temporaneamente con `UseEnhancedAgentRAG: false` e usa il servizio esistente.

**Q: Come verifico quale servizio è attivo?**
A: Controlla i log all'avvio: `"Using EnhancedAgentRAGService with Microsoft Agent Framework"` o `"Using MultiProviderSemanticRAGService (default)"`

**Q: Posso usare entrambi i servizi?**
A: No, solo uno alla volta. Usa feature flag per switchare.

### Debugging

Abilita logging dettagliato:

```json
{
  "Logging": {
    "LogLevel": {
      "DocN.Data.Services.EnhancedAgentRAGService": "Debug",
      "Microsoft.SemanticKernel": "Debug"
    }
  },
  "EnhancedRAG": {
    "Telemetry": {
      "EnableDetailedLogging": true,
      "TrackPerformance": true,
      "TrackAgentDecisions": true
    }
  }
}
```

---

## 📞 Contatti

Per domande o supporto sull'implementazione:
- Consulta le guide nella cartella `/docs`
- Review il codice sorgente con commenti dettagliati
- Controlla gli esempi nella documentazione

---

**Ultima Modifica**: Gennaio 2026  
**Stato**: Documentazione Completa, Implementazione In Progress
