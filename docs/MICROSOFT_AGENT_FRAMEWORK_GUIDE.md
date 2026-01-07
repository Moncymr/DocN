# Microsoft Agent Framework Integration Guide

## 📋 Overview

This guide explains how DocN leverages the **Microsoft Agent Framework** (part of Semantic Kernel) to create an optimized, production-ready RAG (Retrieval-Augmented Generation) system.

**Version**: 2.0  
**Last Updated**: January 2026  
**Framework**: Microsoft.SemanticKernel.Agents 1.29.0-alpha

---

## 🎯 What is Microsoft Agent Framework?

The Microsoft Agent Framework is a powerful component of Semantic Kernel that enables:

- **Multi-Agent Orchestration**: Coordinate multiple specialized AI agents
- **AgentGroupChat**: Facilitate complex multi-turn agent conversations
- **Specialized Roles**: Each agent has specific expertise and instructions
- **Termination Strategies**: Control when agent conversations end
- **Memory Management**: Share context between agents efficiently
- **Observability**: Built-in telemetry and monitoring

---

## 🏗️ Architecture

### DocN Enhanced Agent RAG Pipeline

```
User Query
    ↓
┌─────────────────────────────────────────────────────────┐
│              AgentGroupChat Coordinator                  │
└─────────────────────────────────────────────────────────┘
    ↓                ↓                ↓                ↓
[Query]        [Retrieval]       [Reranking]      [Synthesis]
Analyzer        Agent             Agent            Agent
    ↓                ↓                ↓                ↓
Analyze          Find            Optimize         Generate
Intent        Documents          Ranking          Answer
    ↓                ↓                ↓                ↓
└────────────────→ Collaborative Processing ←──────────────┘
                        ↓
                 Final Response
```

### Four Specialized Agents

#### 1. **Query Analyzer Agent**
- **Role**: Understanding user intent
- **Tasks**:
  - Parse and understand the query
  - Identify key concepts and entities
  - Expand query with synonyms
  - Suggest search filters
  - Determine information type needed

#### 2. **Retrieval Agent**
- **Role**: Finding relevant documents
- **Tasks**:
  - Execute vector similarity search
  - Apply filters and constraints
  - Extract relevant document chunks
  - Calculate relevance scores
  - Handle fallback strategies

#### 3. **Reranking Agent**
- **Role**: Optimizing document order
- **Tasks**:
  - Re-evaluate document relevance
  - Consider query-document alignment
  - Assess information quality
  - Remove redundant content
  - Optimize for context window

#### 4. **Synthesis Agent**
- **Role**: Generating final answers
- **Tasks**:
  - Analyze reranked documents
  - Extract key information
  - Generate coherent response
  - Add citations and references
  - Indicate confidence level

---

## 🚀 Implementation

### Service Registration

In `Program.cs`:

```csharp
// Register Enhanced Agent RAG Service
builder.Services.AddScoped<ISemanticRAGService, EnhancedAgentRAGService>();

// Required dependencies
builder.Services.AddScoped<IKernelProvider, KernelProvider>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<ICacheService, CacheService>();
```

### Basic Usage

```csharp
public class ChatController : ControllerBase
{
    private readonly ISemanticRAGService _ragService;
    
    [HttpPost("ask")]
    public async Task<ActionResult<RAGResponse>> AskQuestion([FromBody] QueryRequest request)
    {
        var response = await _ragService.GenerateResponseAsync(
            query: request.Question,
            userId: User.GetUserId(),
            conversationId: request.ConversationId,
            topK: 5
        );
        
        return Ok(response);
    }
}
```

### Streaming Responses

```csharp
[HttpPost("ask-stream")]
public async IAsyncEnumerable<string> AskQuestionStreaming(
    [FromBody] QueryRequest request)
{
    await foreach (var chunk in _ragService.GenerateStreamingResponseAsync(
        query: request.Question,
        userId: User.GetUserId(),
        conversationId: request.ConversationId))
    {
        yield return chunk;
    }
}
```

---

## 📊 Performance Optimization

### Multi-Phase Timing

The Enhanced Agent RAG Service tracks performance across all phases:

```json
{
  "responseTimeMs": 2450,
  "metadata": {
    "pipelinePhases": {
      "queryAnalysis": 320,
      "retrieval": 850,
      "reranking": 430,
      "synthesis": 850
    }
  }
}
```

### Optimization Strategies

#### 1. **Parallel Retrieval**
```csharp
// Retrieve more candidates than needed (topK * 2)
var relevantDocs = await SearchDocumentsAsync(
    query, userId, topK * 2, minSimilarity: 0.5);
```

#### 2. **Early Termination**
```csharp
var agentChat = new AgentGroupChat(agents)
{
    ExecutionSettings = new AgentGroupChatSettings
    {
        TerminationStrategy = new AgentTerminationStrategy
        {
            MaximumIterations = 10,
            AutomaticReset = true
        }
    }
};
```

#### 3. **Context Compression**
```csharp
// Only include relevant chunks, not full documents
RelevantChunk = document.Content?.Substring(0, Math.Min(500, length))
```

#### 4. **Conversation Context Limiting**
```csharp
// Only use last 5 messages for context
.OrderByDescending(m => m.Timestamp)
.Take(5)
.OrderBy(m => m.Timestamp)
```

---

## 🔍 Agent Communication Flow

### Example Multi-Agent Conversation

```
1. User Query → AgentGroupChat
   "Quali sono i requisiti per il progetto X?"

2. QueryAnalyzer → Analysis
   "Query richiede: requisiti specifici, progetto X
    Concetti chiave: requisiti, progetto, specifiche
    Tipo: informazioni specifiche e strutturate"

3. RetrievalAgent → Documents
   "Trovati 8 documenti con similarity > 0.7
    Top 3: Specifiche_Progetto_X.pdf (0.89),
           Requirements_X.docx (0.85),
           Project_Overview.pdf (0.72)"

4. RerankingAgent → Optimized Order
   "Riordinamento basato su pertinenza:
    1. Requirements_X.docx (0.92) - contiene lista requisiti
    2. Specifiche_Progetto_X.pdf (0.89) - dettagli tecnici
    3. Project_Overview.pdf (0.65) - contesto generale"

5. SynthesisAgent → Final Answer
   "I requisiti per il progetto X sono:
    1. [Requisito 1] - [Doc: Requirements_X.docx]
    2. [Requisito 2] - [Doc: Requirements_X.docx]
    3. [Requisito 3] - [Doc: Specifiche_Progetto_X.pdf]
    
    Per maggiori dettagli tecnici, consulta Specifiche_Progetto_X.pdf"
```

---

## 📈 Telemetry and Monitoring

### Activity Source Integration

```csharp
private readonly ActivitySource _activitySource = 
    new("DocN.EnhancedAgentRAG");

using var activity = _activitySource.StartActivity("GenerateResponse");
activity?.SetTag("query", query);
activity?.SetTag("userId", userId);
activity?.AddEvent(new ActivityEvent("QueryAnalysis.Start"));
```

### Metrics Tracked

- **Query Analysis Time**: Time to understand and expand query
- **Retrieval Time**: Time to find and score documents
- **Reranking Time**: Time to optimize document order
- **Synthesis Time**: Time to generate final answer
- **Total Response Time**: End-to-end latency
- **Documents Retrieved**: Number of relevant documents found
- **Top Similarity Score**: Best match quality
- **Agent Framework Version**: For compatibility tracking

### Monitoring Dashboard Query

```sql
SELECT 
    AVG(CAST(JSON_VALUE(Metadata, '$.pipelinePhases.queryAnalysis') AS BIGINT)) as AvgQueryAnalysisMs,
    AVG(CAST(JSON_VALUE(Metadata, '$.pipelinePhases.retrieval') AS BIGINT)) as AvgRetrievalMs,
    AVG(CAST(JSON_VALUE(Metadata, '$.pipelinePhases.reranking') AS BIGINT)) as AvgRerankingMs,
    AVG(CAST(JSON_VALUE(Metadata, '$.pipelinePhases.synthesis') AS BIGINT)) as AvgSynthesisMs,
    AVG(ResponseTimeMs) as AvgTotalMs
FROM Messages
WHERE Timestamp > DATEADD(day, -7, GETUTCDATE())
    AND Role = 'assistant'
    AND JSON_VALUE(Metadata, '$.agentFramework') = 'Microsoft.SemanticKernel.Agents'
```

---

## 🎓 Best Practices

### 1. **Agent Specialization**

✅ **DO**: Give each agent a specific, well-defined role
```csharp
Instructions = "Sei un esperto analista di query. Il tuo compito è SOLO analizzare..."
```

❌ **DON'T**: Create generic agents that try to do everything
```csharp
Instructions = "You're a helpful assistant" // Too vague!
```

### 2. **Clear Communication**

✅ **DO**: Use structured formats for agent communication
```csharp
await agentChat.AddChatMessageAsync(new ChatMessageContent(
    AuthorRole.User,
    $"Query analysis: {analysis}\n\nDocumenti:\n{documents}\n\nTask: Riordina"
));
```

❌ **DON'T**: Send ambiguous messages
```csharp
await agentChat.AddChatMessageAsync("Here's some stuff, do something");
```

### 3. **Termination Control**

✅ **DO**: Implement clear termination conditions
```csharp
protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, ...)
{
    return Task.FromResult(
        agent.Name == "SynthesisAgent" && 
        history.Any(m => m.AuthorName == "SynthesisAgent")
    );
}
```

❌ **DON'T**: Let agents run indefinitely
```csharp
MaximumIterations = int.MaxValue // Dangerous!
```

### 4. **Error Handling**

✅ **DO**: Handle agent failures gracefully
```csharp
try
{
    await InitializeAgentsAsync();
    // ... agent processing
}
catch (Exception ex)
{
    _logger.LogError(ex, "Agent pipeline failed");
    return FallbackResponse();
}
```

### 5. **Resource Management**

✅ **DO**: Dispose of resources properly
```csharp
using var activity = _activitySource.StartActivity("GenerateResponse");
// Activity automatically disposed
```

---

## 🔧 Configuration

### Agent Configuration Options

```json
{
  "AgentRAG": {
    "QueryAnalysis": {
      "Enabled": true,
      "MaxExpansionTerms": 10,
      "IncludeSynonyms": true
    },
    "Retrieval": {
      "DefaultTopK": 10,
      "MinSimilarity": 0.5,
      "FallbackToKeyword": true
    },
    "Reranking": {
      "Enabled": true,
      "RerankingModel": "cross-encoder/ms-marco-MiniLM-L-6-v2"
    },
    "Synthesis": {
      "MaxContextLength": 4000,
      "IncludeCitations": true,
      "ConfidenceThreshold": 0.7
    },
    "Telemetry": {
      "EnableDetailedLogging": true,
      "TrackPerformance": true
    }
  }
}
```

---

## 🚨 Troubleshooting

### Agent Initialization Fails

**Symptom**: `Could not initialize agents` error

**Causes**:
1. Kernel not properly configured
2. Missing AI provider configuration
3. Database connection issues

**Solutions**:
```csharp
// Check kernel provider
var kernel = await _kernelProvider.GetKernelAsync();
if (kernel == null)
{
    _logger.LogError("Kernel provider returned null");
}

// Verify AI configuration
var chatService = kernel.GetRequiredService<IChatCompletionService>();
```

### Agents Not Communicating

**Symptom**: Agents don't respond or skip phases

**Causes**:
1. Incorrect termination strategy
2. Agent names mismatch
3. Message routing issues

**Solutions**:
```csharp
// Verify agent names are consistent
_queryAnalyzerAgent = new ChatCompletionAgent
{
    Name = "QueryAnalyzer", // Must match in termination strategy
    // ...
};

// Check termination logic
_logger.LogDebug("Agent {Name} processed, history count: {Count}", 
    agent.Name, history.Count);
```

### Performance Degradation

**Symptom**: Response time increases over time

**Causes**:
1. Growing conversation history
2. Too many documents retrieved
3. Inefficient reranking

**Solutions**:
```csharp
// Limit conversation history
.Take(5) // Only last 5 messages

// Reduce candidate documents
topK * 2 // Instead of topK * 5

// Enable caching
var cacheKey = $"rag:{userId}:{query}";
var cached = await _cacheService.GetAsync<RAGResponse>(cacheKey);
```

---

## 📚 API Reference

### EnhancedAgentRAGService

#### GenerateResponseAsync

```csharp
Task<SemanticRAGResponse> GenerateResponseAsync(
    string query,                    // User question
    string userId,                   // User identifier
    int? conversationId = null,      // Optional conversation context
    List<int>? specificDocumentIds = null,  // Limit to specific docs
    int topK = 5                     // Number of documents to use
)
```

**Returns**: `SemanticRAGResponse` with answer, sources, and metadata

#### GenerateStreamingResponseAsync

```csharp
IAsyncEnumerable<string> GenerateStreamingResponseAsync(
    string query,
    string userId,
    int? conversationId = null,
    List<int>? specificDocumentIds = null,
    int topK = 5
)
```

**Returns**: Async stream of response chunks

---

## 🔗 Additional Resources

### Microsoft Documentation
- [Semantic Kernel Agents](https://learn.microsoft.com/en-us/semantic-kernel/agents/)
- [Agent Framework Concepts](https://learn.microsoft.com/en-us/semantic-kernel/agents/agent-concepts)
- [AgentGroupChat](https://learn.microsoft.com/en-us/semantic-kernel/agents/agent-group-chat)

### DocN Guides
- [RAG Quality Guide](./RAG_QUALITY_GUIDE.md)
- [Multi-Provider AI Configuration](./README_DOCUMENTAZIONE_SVILUPPATORI.md)
- [Performance Optimization](./IMPLEMENTATION_SUMMARY_MONITORING.md)

### Research Papers
- [RAG: Retrieval-Augmented Generation](https://arxiv.org/abs/2005.11401)
- [Multi-Agent Systems](https://arxiv.org/abs/2308.08155)

---

## 🎯 Migration Path

### From MultiProviderSemanticRAGService

**Old Code**:
```csharp
builder.Services.AddScoped<ISemanticRAGService, MultiProviderSemanticRAGService>();
```

**New Code**:
```csharp
builder.Services.AddScoped<ISemanticRAGService, EnhancedAgentRAGService>();
```

**Benefits**:
- ✅ Better answer quality through multi-agent collaboration
- ✅ Improved relevance through query analysis and reranking
- ✅ Enhanced observability with detailed phase tracking
- ✅ More maintainable code with clear agent separation
- ✅ Production-ready with proper error handling

**No Breaking Changes**: API remains compatible

---

## 🏆 Success Metrics

Track these KPIs to measure agent framework impact:

| Metric | Target | Current |
|--------|--------|---------|
| Answer Relevance | > 0.85 | - |
| Response Time | < 3000ms | - |
| User Satisfaction | > 4.0/5 | - |
| Citation Accuracy | > 95% | - |
| Document Retrieval Precision | > 0.80 | - |

---

## 📝 Version History

- **v2.0** (Jan 2026): Enhanced Agent Framework with AgentGroupChat
- **v1.5** (Dec 2025): Basic agent interfaces and orchestration
- **v1.0** (Nov 2025): Initial Semantic Kernel integration

---

**Remember**: The Microsoft Agent Framework enables sophisticated multi-agent workflows that significantly improve RAG quality and maintainability. Start with the enhanced service and monitor the metrics to validate the improvements!
