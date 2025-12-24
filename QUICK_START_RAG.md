# Quick Start Guide - Advanced RAG Features

## 🚀 Quick Reference

### API Endpoints

#### Search
```bash
# Hybrid Search
POST /api/search/hybrid
{
  "query": "employee benefits",
  "topK": 10,
  "minSimilarity": 0.7
}

# Vector Search Only
POST /api/search/vector

# Text Search Only  
POST /api/search/text
```

#### Chat (RAG)
```bash
# Ask a question
POST /api/chat/query
{
  "message": "What is the return policy?",
  "userId": "user123",
  "conversationId": 456  # optional
}

# Get conversations
GET /api/chat/conversations?userId=user123

# Get messages
GET /api/chat/conversations/456/messages

# Delete conversation
DELETE /api/chat/conversations/456
```

### C# Code Examples

#### Simple Search
```csharp
var options = new SearchOptions { TopK = 5, MinSimilarity = 0.7 };
var results = await _searchService.SearchAsync("refund policy", options);
```

#### Chat with Documents
```csharp
var result = await _orchestrator.ProcessQueryAsync(
    "What are the key terms?",
    userId: "user123",
    conversationId: 456
);
Console.WriteLine(result.Answer);
```

#### Classify Document
```csharp
var classification = await _orchestrator.ClassifyDocumentAsync(document);
document.SuggestedCategory = classification.CategorySuggestion.Category;
```

#### Process Document Manually
```csharp
await _batchProcessingService.ProcessDocumentAsync(documentId);
```

### Service Dependencies

```
IAgentOrchestrator
├── IRetrievalAgent
│   ├── IHybridSearchService
│   │   └── IEmbeddingService (with ICacheService)
│   └── IEmbeddingService
├── ISynthesisAgent
│   └── ApplicationDbContext
└── IClassificationAgent
    └── IEmbeddingService
```

### Configuration

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=DocN;..."
  },
  "Memory": {
    "CacheSizeMB": 100,
    "EmbeddingCacheDays": 30,
    "SearchCacheMinutes": 15
  }
}
```

### Database Schema

**Key Tables**:
- `Documents` - Main document storage with embeddings
- `DocumentChunks` - Chunked documents with embeddings
- `Conversations` - Chat conversations
- `Messages` - Individual messages with document references

### Performance Tips

1. **Enable Caching**: Inject `ICacheService` into `EmbeddingService`
2. **Use Chunks**: For long documents, chunk-based retrieval is more precise
3. **Tune Batch Size**: Adjust `BatchEmbeddingProcessor` interval based on load
4. **Monitor Coverage**: Use `GetStatsAsync()` to track embedding coverage
5. **Index Vectors**: In production, use SQL Server 2025 native VECTOR type with IVF index

### Common Workflows

#### Upload → Process → Search
```csharp
// 1. Upload
var doc = await UploadDocument(file);

// 2. Background service processes automatically (or manually)
await _batchService.ProcessDocumentAsync(doc.Id);

// 3. Search
var results = await _searchService.SearchAsync("query", options);
```

#### Chat Workflow
```csharp
// Start conversation
var response1 = await ProcessQuery("What is X?", userId);

// Continue conversation
var response2 = await ProcessQuery("Tell me more", userId, response1.ConversationId);

// View history
var messages = await GetMessages(response1.ConversationId);
```

### Troubleshooting

| Issue | Solution |
|-------|----------|
| No search results | Check if embeddings are generated (`GetStatsAsync()`) |
| Slow queries | Enable caching, reduce `topK` value |
| Out of memory | Reduce cache size limit in Program.cs |
| Background service not running | Check logs, ensure `AddHostedService<BatchEmbeddingProcessor>()` is registered |

### Testing

```bash
# Build
dotnet build

# Run tests (when available)
dotnet test

# Run server
cd DocN.Server && dotnet run

# Test endpoint
curl -X POST http://localhost:5000/api/search/hybrid \
  -H "Content-Type: application/json" \
  -d '{"query":"test","topK":5}'
```

### Migration

```bash
# Apply migrations
dotnet ef database update --project DocN.Data --startup-project DocN.Client --context ApplicationDbContext
```

---

For detailed documentation, see [ADVANCED_RAG_IMPLEMENTATION.md](./ADVANCED_RAG_IMPLEMENTATION.md)
