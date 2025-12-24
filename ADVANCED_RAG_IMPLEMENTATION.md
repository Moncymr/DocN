# Advanced RAG Implementation - Phases 5-7 and 10-13

## Overview

This document describes the implementation of the missing phases (5-7, 10-13) of the DocN enterprise RAG system, which adds advanced vector storage, multi-agent workflows, hybrid search, and advanced features.

---

## Phase 5: Advanced Vector Storage Integration with EF Core

### DocumentChunk Model

**Purpose**: Enable more granular retrieval by splitting documents into smaller chunks for better context matching.

**Implementation**: `DocN.Data/Models/DocumentChunk.cs`

```csharp
public class DocumentChunk
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string ChunkText { get; set; }
    public float[]? ChunkEmbedding { get; set; }  // 1536-dim vector
    public int? TokenCount { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
}
```

**Key Features**:
- Tracks position in original document
- Stores individual embeddings per chunk
- Enables chunk-level retrieval for precise answers
- Token counting for LLM context management

### ChunkingService

**Purpose**: Intelligent document splitting with overlap for context preservation.

**Implementation**: `DocN.Data/Services/ChunkingService.cs`

**Algorithm**:
1. Split text using sliding window approach
2. Prefer sentence boundaries (., !, ?)
3. Fallback to word boundaries if needed
4. Configurable chunk size (default 1000 chars) and overlap (default 200 chars)

**Usage**:
```csharp
var chunkingService = new ChunkingService();
var chunks = chunkingService.ChunkDocument(document, chunkSize: 1000, overlap: 200);
```

### Database Schema

**Migration**: `20250102000000_AddDocumentChunks.cs`

**Schema**:
```sql
CREATE TABLE DocumentChunks (
    Id INT PRIMARY KEY IDENTITY,
    DocumentId INT NOT NULL,
    ChunkIndex INT NOT NULL,
    ChunkText NVARCHAR(MAX) NOT NULL,
    ChunkEmbedding NVARCHAR(MAX),  -- Temp: stored as CSV string
    TokenCount INT,
    CreatedAt DATETIME2 NOT NULL,
    StartPosition INT NOT NULL,
    EndPosition INT NOT NULL,
    FOREIGN KEY (DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE
);

CREATE INDEX IX_DocumentChunks_DocumentId ON DocumentChunks(DocumentId);
CREATE INDEX IX_DocumentChunks_DocumentId_ChunkIndex ON DocumentChunks(DocumentId, ChunkIndex);
```

**Note**: Currently using `NVARCHAR(MAX)` for embeddings. In production with SQL Server 2025, migrate to native `VECTOR(1536)` type.

---

## Phase 6: Multi-Agent Workflows

### Architecture

The system implements a multi-agent architecture where specialized agents handle different aspects of RAG:

```
User Query
    ↓
AgentOrchestrator
    ↓
┌─────────────┬─────────────┬─────────────────┐
│ Retrieval   │ Synthesis   │ Classification  │
│ Agent       │ Agent       │ Agent           │
└─────────────┴─────────────┴─────────────────┘
```

### Agent Interfaces

**File**: `DocN.Data/Services/Agents/IAgents.cs`

1. **IRetrievalAgent**: Document and chunk retrieval
2. **ISynthesisAgent**: Answer generation from context
3. **IClassificationAgent**: Category suggestion and tagging
4. **IAgentOrchestrator**: Coordinates all agents

### RetrievalAgent

**Implementation**: `DocN.Data/Services/Agents/RetrievalAgent.cs`

**Capabilities**:
- Document-level retrieval using hybrid search
- Chunk-level retrieval for precise context
- User-based filtering for privacy
- Cosine similarity calculation

**Example**:
```csharp
var agent = new RetrievalAgent(context, searchService, embeddingService);

// Get relevant documents
var documents = await agent.RetrieveAsync("what are our return policies?", userId: "user123", topK: 5);

// Get relevant chunks for more precision
var chunks = await agent.RetrieveChunksAsync("what are our return policies?", userId: "user123", topK: 10);
```

### SynthesisAgent

**Implementation**: `DocN.Data/Services/Agents/SynthesisAgent.cs`

**Capabilities**:
- Generates natural language answers from retrieved documents
- Maintains conversation history
- Includes document citations
- Handles both document-based and chunk-based synthesis

**Example**:
```csharp
var agent = new SynthesisAgent(context);

// Synthesize answer from documents
var answer = await agent.SynthesizeAsync(
    query: "what are our return policies?",
    documents: retrievedDocs,
    conversationHistory: previousMessages
);
```

### ClassificationAgent

**Implementation**: `DocN.Data/Services/Agents/ClassificationAgent.cs`

**Capabilities**:
- Dual-method category suggestion (AI + vector-based)
- Tag extraction from document content
- Document type classification
- Confidence scoring

**Methods**:
1. **AI Classification**: Uses GPT to directly classify based on content
2. **Vector-based Classification**: Finds similar documents and uses their categories

**Example**:
```csharp
var agent = new ClassificationAgent(context, embeddingService);

var suggestion = await agent.SuggestCategoryAsync(document);
// Result: { Category: "Invoice", Confidence: 0.92, Reasoning: "..." }

var tags = await agent.ExtractTagsAsync(document);
// Result: ["finance", "Q4", "expenses", ...]

var docType = await agent.ClassifyDocumentTypeAsync(document);
// Result: "Invoice"
```

### AgentOrchestrator

**Implementation**: `DocN.Data/Services/Agents/AgentOrchestrator.cs`

**Purpose**: Coordinates multiple agents for complex workflows.

**Workflow**:
1. Loads conversation history if provided
2. RetrievalAgent retrieves relevant content (chunks or documents)
3. SynthesisAgent generates answer from retrieved content
4. Returns comprehensive result with timing metrics

**Example**:
```csharp
var orchestrator = new AgentOrchestrator(retrievalAgent, synthesisAgent, classificationAgent, context);

var result = await orchestrator.ProcessQueryAsync(
    query: "what are our return policies?",
    userId: "user123",
    conversationId: 456
);

// Result includes:
// - Generated answer
// - Retrieved documents/chunks
// - Retrieval strategy used
// - Timing metrics (retrieval, synthesis, total)
```

---

## Phase 7: Hybrid Search Integration

### HybridSearchService

**Implementation**: `DocN.Data/Services/HybridSearchService.cs`

**Purpose**: Combines vector similarity search with full-text search for optimal results.

**Algorithm - Reciprocal Rank Fusion (RRF)**:

```
For each document:
  score = Σ (1 / (k + rank_i))  where k=60 (constant), rank_i is position in each ranking

Example:
  Document A: rank 1 in vector, rank 3 in text
    score = 1/(60+1) + 1/(60+3) = 0.0164 + 0.0159 = 0.0323
  
  Document B: rank 2 in vector, rank 1 in text
    score = 1/(60+2) + 1/(60+1) = 0.0161 + 0.0164 = 0.0325
  
  Result: B ranks higher (appears high in both searches)
```

**Methods**:
- `SearchAsync()`: Hybrid search (vector + text)
- `VectorSearchAsync()`: Vector similarity only
- `TextSearchAsync()`: Full-text search only

**Example**:
```csharp
var options = new SearchOptions
{
    TopK = 10,
    MinSimilarity = 0.7,
    CategoryFilter = "Contracts",
    OwnerId = "user123"
};

var results = await searchService.SearchAsync("refund policy", options);

foreach (var result in results)
{
    Console.WriteLine($"{result.Document.FileName}");
    Console.WriteLine($"  Vector Score: {result.VectorScore:F2}");
    Console.WriteLine($"  Text Score: {result.TextScore:F2}");
    Console.WriteLine($"  Combined: {result.CombinedScore:F2}");
}
```

### API Endpoints

#### SearchController

**File**: `DocN.Server/Controllers/SearchController.cs`

**Endpoints**:

1. **POST /api/search/hybrid**
   - Performs hybrid search
   - Request: `{ query, topK?, minSimilarity?, categoryFilter?, userId? }`
   - Response: `{ query, results[], totalResults, queryTimeMs, searchType }`

2. **POST /api/search/vector**
   - Vector similarity search only
   - Same request/response format

3. **POST /api/search/text**
   - Full-text search only
   - Same request/response format

**Example Request**:
```json
POST /api/search/hybrid
{
  "query": "employee benefits policy",
  "topK": 5,
  "minSimilarity": 0.75,
  "categoryFilter": "HR Policy",
  "userId": "user123"
}
```

**Example Response**:
```json
{
  "query": "employee benefits policy",
  "results": [
    {
      "document": {
        "id": 42,
        "fileName": "Benefits_Guide_2024.pdf",
        "actualCategory": "HR Policy"
      },
      "vectorScore": 0.89,
      "textScore": 0.76,
      "combinedScore": 0.0312,
      "vectorRank": 1,
      "textRank": 2
    }
  ],
  "totalResults": 5,
  "queryTimeMs": 245.6,
  "searchType": "hybrid"
}
```

#### ChatController

**File**: `DocN.Server/Controllers/ChatController.cs`

**Endpoints**:

1. **POST /api/chat/query**
   - Process RAG query with multi-agent workflow
   - Request: `{ message, userId?, conversationId? }`
   - Response: `{ conversationId, answer, referencedDocuments[], metadata }`

2. **GET /api/chat/conversations?userId={id}**
   - Get all conversations for a user
   - Response: `ConversationSummary[]`

3. **GET /api/chat/conversations/{id}/messages**
   - Get messages in a conversation
   - Response: `Message[]`

4. **DELETE /api/chat/conversations/{id}**
   - Delete a conversation

**Example Request**:
```json
POST /api/chat/query
{
  "message": "What is our return policy for defective items?",
  "userId": "user123",
  "conversationId": 456
}
```

**Example Response**:
```json
{
  "conversationId": 456,
  "answer": "According to our Return Policy (Document 1) and Customer Service Manual (Document 2), defective items can be returned within 30 days of purchase with a full refund or replacement. You'll need to provide proof of purchase and the item must be unused except for the defect...",
  "referencedDocuments": [
    {
      "id": 15,
      "fileName": "Return_Policy_2024.pdf",
      "category": "Policy"
    },
    {
      "id": 23,
      "fileName": "Customer_Service_Manual.pdf",
      "category": "Manual"
    }
  ],
  "metadata": {
    "retrievalTimeMs": 120.5,
    "synthesisTimeMs": 850.2,
    "totalTimeMs": 970.7,
    "retrievalStrategy": "chunk-based",
    "documentsRetrieved": 2,
    "chunksRetrieved": 8
  }
}
```

---

## Phase 10-13: Advanced Features

### Caching Service

**Implementation**: `DocN.Data/Services/CacheService.cs`

**Purpose**: Cache embeddings and search results to reduce API calls and improve performance.

**Features**:
- **Embedding Cache**: 30-day expiration (embeddings rarely change)
- **Search Cache**: 15-minute expiration with sliding window
- **Memory-based**: Uses IMemoryCache (100MB limit)
- **SHA256 hashing**: For cache keys

**Cache Hit Benefits**:
- **Embeddings**: ~$0.0001 saved per 1K tokens
- **Search Results**: ~200ms saved per query

**Example**:
```csharp
// Cache is automatically used by EmbeddingService
var embedding = await embeddingService.GenerateEmbeddingAsync("some text");
// First call: ~300ms, makes API call
// Second call: ~1ms, returns from cache

// Manually use cache
var cachedResults = await cacheService.GetCachedSearchResultsAsync<Document>("query");
if (cachedResults == null)
{
    var results = await PerformExpensiveSearch();
    await cacheService.SetCachedSearchResultsAsync("query", results);
}
```

### Batch Processing Service

**Implementation**: `DocN.Data/Services/BatchEmbeddingProcessor.cs`

**Purpose**: Automatically process documents in the background to generate embeddings and chunks.

**Background Service** (runs every 30 seconds):
1. Finds documents without embeddings
2. Generates embeddings for documents
3. Creates chunks with their embeddings
4. Processes up to 10 documents per cycle

**Manual Processing API**:
```csharp
// Process specific document
await batchProcessingService.ProcessDocumentAsync(documentId: 42);

// Process all pending
await batchProcessingService.ProcessAllPendingAsync();

// Get statistics
var stats = await batchProcessingService.GetStatsAsync();
Console.WriteLine($"Coverage: {stats.EmbeddingCoveragePercentage:F1}%");
Console.WriteLine($"Pending: {stats.DocumentsWithoutEmbeddings} docs, {stats.ChunksWithoutEmbeddings} chunks");
```

**Processing Pipeline**:
```
Document Upload
    ↓
Queue (no embedding)
    ↓
Background Service (every 30s)
    ↓
├─ Generate document embedding
├─ Create chunks
└─ Generate chunk embeddings
    ↓
Ready for Search
```

---

## Service Registration

**File**: `DocN.Server/Program.cs`

All new services are registered in the DI container:

```csharp
// Core services
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IChunkingService, ChunkingService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<IHybridSearchService, HybridSearchService>();
builder.Services.AddScoped<IBatchProcessingService, BatchProcessingService>();

// Agents
builder.Services.AddScoped<IRetrievalAgent, RetrievalAgent>();
builder.Services.AddScoped<ISynthesisAgent, SynthesisAgent>();
builder.Services.AddScoped<IClassificationAgent, ClassificationAgent>();
builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();

// Background services
builder.Services.AddHostedService<BatchEmbeddingProcessor>();

// Memory cache
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024 * 1024 * 100; // 100MB
});
```

---

## Usage Examples

### Complete RAG Workflow

```csharp
// 1. Upload and process document
var document = await documentService.UploadAsync(file);
// Background service will process it automatically

// 2. Chat with documents
var chatRequest = new ChatRequest
{
    Message = "What are the key requirements in the contract?",
    UserId = "user123"
};

var response = await chatController.Query(chatRequest);
Console.WriteLine(response.Answer);

// 3. View conversation history
var conversations = await chatController.GetConversations("user123");
foreach (var conv in conversations)
{
    Console.WriteLine($"{conv.Title} - {conv.MessageCount} messages");
}
```

### Hybrid Search Workflow

```csharp
// 1. Perform search
var searchRequest = new SearchRequest
{
    Query = "financial reports Q4 2023",
    TopK = 10,
    MinSimilarity = 0.75
};

var searchResponse = await searchController.HybridSearch(searchRequest);

// 2. Process results
foreach (var result in searchResponse.Results)
{
    Console.WriteLine($"Document: {result.Document.FileName}");
    Console.WriteLine($"Relevance: {result.CombinedScore:F3}");
    Console.WriteLine($"Vector: {result.VectorScore:F2}, Text: {result.TextScore:F2}");
}
```

### Document Classification Workflow

```csharp
// Automatically classify uploaded document
var classificationResult = await orchestrator.ClassifyDocumentAsync(document);

document.SuggestedCategory = classificationResult.CategorySuggestion.Category;
document.CategoryReasoning = classificationResult.CategorySuggestion.Reasoning;

// Add tags
foreach (var tag in classificationResult.Tags)
{
    document.Tags.Add(new DocumentTag { Name = tag });
}

await context.SaveChangesAsync();
```

---

## Performance Considerations

### Vector Search Optimization

**Current Implementation**: In-memory cosine similarity (O(n) complexity)

**Production Recommendations**:
1. Use SQL Server 2025 native `VECTOR` type with IVF index
2. Implement approximate nearest neighbor (ANN) search
3. Add vector index: `CREATE INDEX ON Documents(EmbeddingVector) WITH (VECTOR_INDEX_TYPE = 'IVF')`

### Caching Strategy

**What to Cache**:
- ✅ Embeddings (30 days) - rarely change
- ✅ Search results (15 minutes) - frequently accessed
- ✅ Common queries (sliding window) - user patterns
- ❌ Chat responses - always fresh
- ❌ Document metadata - frequently updated

### Batch Processing Tuning

**Configuration**:
- Process interval: 30 seconds (adjust based on load)
- Batch size: 10 documents (prevent overload)
- Chunk batch size: 20 chunks

**Monitoring**:
```csharp
var stats = await batchProcessingService.GetStatsAsync();
if (stats.EmbeddingCoveragePercentage < 90)
{
    // Alert: backlog building up
}
```

---

## Testing Recommendations

### Unit Tests

**Priority Services**:
1. ChunkingService - test boundary cases
2. HybridSearchService - test RRF algorithm
3. Agents - mock dependencies
4. CacheService - test expiration

**Example**:
```csharp
[Test]
public void ChunkingService_HandlesOverlap()
{
    var service = new ChunkingService();
    var text = "A".Repeat(2000);
    
    var chunks = service.ChunkText(text, chunkSize: 1000, overlap: 200);
    
    Assert.That(chunks.Count, Is.EqualTo(2));
    Assert.That(chunks[0].Length, Is.EqualTo(1000));
    Assert.That(chunks[1].Length, Is.EqualTo(1200)); // 1000 + 200 overlap
}
```

### Integration Tests

**Workflows to Test**:
1. End-to-end RAG: upload → process → query → answer
2. Multi-agent coordination
3. Hybrid search accuracy
4. Conversation persistence

---

## Security Considerations

### User Data Isolation

All services support user-based filtering:
```csharp
var results = await searchService.SearchAsync(query, new SearchOptions
{
    OwnerId = currentUser.Id  // Only returns user's documents
});
```

### Sensitive Data

**Cache Considerations**:
- Never cache PII in plaintext
- Use encrypted cache for sensitive embeddings
- Clear cache on user logout

**Logging**:
- Don't log user queries verbatim
- Sanitize document content in logs
- Use structured logging with redaction

---

## Deployment Checklist

- [ ] Database migration applied
- [ ] Connection strings configured
- [ ] AI service keys configured (Azure OpenAI)
- [ ] Memory cache limits set appropriately
- [ ] Background service enabled
- [ ] CORS configured for client origins
- [ ] Logging configured (Application Insights)
- [ ] Performance monitoring enabled
- [ ] Health checks implemented
- [ ] API documentation published (Swagger)

---

## Next Steps

### UI Integration
- Create search component using SearchController
- Implement chat interface using ChatController
- Add conversation history sidebar
- Display document references with links

### Advanced Features
- Implement reranking with cross-encoder
- Add query expansion for better results
- Implement faceted search (category, date filters)
- Add export functionality (PDF, Word)

### Monitoring
- Track search result relevance
- Monitor embedding cache hit rate
- Alert on batch processing delays
- Dashboard for system health

---

## Conclusion

The implementation adds sophisticated RAG capabilities:
- **Phase 5**: Document chunking for precise retrieval
- **Phase 6**: Multi-agent architecture for specialized tasks
- **Phase 7**: Hybrid search with RRF for optimal results
- **Phases 10-13**: Caching, batch processing, and production-ready features

The system is now ready for advanced document Q&A with high accuracy and performance.
