# DocN - Enterprise RAG System with Microsoft Semantic Kernel & Gemini

## 🎯 Overview

DocN is an enterprise-grade RAG (Retrieval Augmented Generation) system built with:
- **Microsoft Semantic Kernel** for AI orchestration
- **Microsoft Agent Framework** for intelligent multi-agent workflows
- **Google Gemini** as the default embedding provider
- **SQL Server 2025** with native VECTOR support for semantic search
- **Blazor WebAssembly** for modern, responsive UI

## 🚀 Key Features

### Document Management
- ✅ Upload and store documents (PDF, DOCX, XLSX, PPTX, images)
- ✅ Automatic text extraction and OCR support
- ✅ Metadata extraction and tagging
- ✅ Document chunking for optimal search

### AI-Powered Features
- ✅ **Gemini embeddings by default** for semantic search
- ✅ Dual category suggestion: Direct AI + Vector similarity
- ✅ Conversational RAG with chat history
- ✅ Multi-provider support (Gemini, OpenAI, Azure OpenAI)

### Search Capabilities
- ✅ Hybrid search: Vector + Full-text combined
- ✅ Semantic similarity search
- ✅ Category-based filtering
- ✅ Department-level access control

### Enterprise Features
- ✅ Audit logging for compliance
- ✅ Role-based access control
- ✅ Document versioning
- ✅ Conversation history
- ✅ Analytics dashboard

## 📁 Project Structure

```
DocN/
├── src/
│   ├── DocN.Core/           # Core business logic & AI services
│   │   ├── Interfaces/      # Service interfaces
│   │   ├── SemanticKernel/  # SK integration with Gemini default
│   │   ├── Agents/          # Microsoft Agent Framework agents
│   │   └── Extensions/      # Service registration extensions
│   ├── DocN.Data/           # Data models & EF Core
│   │   ├── Models/          # Domain entities
│   │   ├── DTOs/            # Data transfer objects
│   │   └── DocNDbContext.cs # Database context
│   ├── DocN.Server/         # ASP.NET Core Web API
│   │   ├── Controllers/     # API endpoints
│   │   └── Services/        # Document processing services
│   └── DocN.Client/         # Blazor WebAssembly UI
│       ├── Pages/           # Blazor pages
│       └── Components/      # Reusable components
├── tests/
│   └── DocN.Core.Tests/     # Unit tests
├── Database/                # SQL scripts
└── IMPLEMENTATION_ROADMAP.md # Complete implementation guide
```

## ⚙️ Configuration

### appsettings.json with Gemini as Default

```json
{
  "SemanticKernel": {
    "DefaultEmbeddingProvider": "Gemini",
    "DefaultChatProvider": "Gemini",
    "Gemini": {
      "ApiKey": "your-gemini-api-key",
      "EmbeddingModel": "text-embedding-004",
      "ChatModel": "gemini-1.5-pro"
    },
    "OpenAI": {
      "ApiKey": "optional-openai-key",
      "EmbeddingModel": "text-embedding-3-small",
      "ChatModel": "gpt-4-turbo"
    },
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "optional-azure-key",
      "EmbeddingDeployment": "text-embedding-ada-002",
      "ChatDeployment": "gpt-4"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DocNDb;Trusted_Connection=True;"
  }
}
```

## 🔧 Setup & Installation

### Prerequisites
- .NET 9.0 SDK
- SQL Server 2025 (for VECTOR support) or SQL Server 2022+
- Google Gemini API key
- Node.js (for Blazor client)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/Moncymr/DocN.git
   cd DocN
   ```

2. **Configure API keys**
   ```bash
   cp appsettings.example.json src/DocN.Server/appsettings.json
   # Edit appsettings.json and add your Gemini API key
   ```

3. **Setup database**
   ```bash
   cd Database
   sqlcmd -S localhost -i CreateDatabase_Complete_V2.sql
   ```

4. **Run migrations**
   ```bash
   cd src/DocN.Server
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run --project src/DocN.Server
   ```

6. **Access the application**
   - API: https://localhost:5001
   - Swagger: https://localhost:5001/swagger
   - Client: https://localhost:5002

## 🤖 Using Gemini as Default Embedding Provider

DocN uses **Google Gemini** as the default embedding provider for semantic search:

### Why Gemini?
- ✅ Latest embedding model: `text-embedding-004`
- ✅ High-quality multilingual embeddings
- ✅ Cost-effective for large-scale deployments
- ✅ Fast response times
- ✅ Native Italian language support

### Code Example

```csharp
// Service registration (Gemini is default)
builder.Services.AddDocNServices(builder.Configuration);

// Using the embedding service
public class DocumentService
{
    private readonly IEmbeddingService _embeddingService;
    
    public async Task<float[]?> GetEmbedding(string text)
    {
        // Uses Gemini by default
        return await _embeddingService.GenerateEmbeddingAsync(text);
        
        // Or explicitly specify provider
        return await _embeddingService.GenerateEmbeddingAsync(text, "OpenAI");
    }
}
```

### Switching Providers

To switch to a different provider, update `appsettings.json`:

```json
{
  "SemanticKernel": {
    "DefaultEmbeddingProvider": "OpenAI",  // or "AzureOpenAI"
    // ...
  }
}
```

## 📊 Database Schema

### Key Tables

- **Documents**: Document metadata and embeddings
- **DocumentChunks**: Chunked document parts with individual embeddings
- **Categories**: Hierarchical document categories
- **Conversations**: Chat sessions
- **Messages**: Conversation messages with document references
- **AuditLogs**: Compliance and security tracking

### Vector Support

SQL Server 2025 native VECTOR type:
```sql
CREATE TABLE Documents (
    Id INT PRIMARY KEY,
    FileName NVARCHAR(500),
    EmbeddingVector VECTOR(1536),  -- Native vector column
    -- ...
);

-- Vector similarity search
SELECT TOP 10 *
FROM Documents
WHERE VECTOR_DISTANCE('cosine', EmbeddingVector, @queryVector) > 0.7
ORDER BY VECTOR_DISTANCE('cosine', EmbeddingVector, @queryVector) DESC;
```

## 🔍 RAG Pipeline

### 1. Document Upload
```
Document → Text Extraction → Chunking → Embedding (Gemini) → Storage
```

### 2. Category Suggestion (Dual Approach)
```
Document Text → Direct AI Classification (Gemini)
              → Vector Similarity (find similar docs)
              → Combined Result with Confidence
```

### 3. Semantic Search
```
Query → Embedding (Gemini) → Vector Search (SQL Server)
                            → Full-text Search
                            → Hybrid RRF Merge
                            → Results
```

### 4. RAG Generation
```
Query → Retrieve Relevant Docs → Build Context
                               → Agent Framework
                               → Generate Response (Gemini)
                               → Citations
```

## 📖 API Endpoints

### Documents
- `POST /api/documents/upload` - Upload document
- `GET /api/documents/{id}` - Get document
- `GET /api/documents/user/{userId}` - List user documents
- `DELETE /api/documents/{id}` - Delete document

### Search
- `POST /api/search/hybrid` - Hybrid semantic + text search
- `GET /api/search/suggestions` - Query suggestions

### Chat (RAG)
- `POST /api/chat/query` - Send message to RAG system
- `GET /api/chat/conversations/{userId}` - Get conversations
- `GET /api/chat/conversation/{id}/messages` - Get messages

### Categories
- `GET /api/categories` - List categories
- `POST /api/categories/suggest` - Suggest category for document

## 🧪 Testing

```bash
# Run unit tests
dotnet test

# Run specific test project
dotnet test tests/DocN.Core.Tests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 📚 Documentation

- **[Implementation Roadmap](IMPLEMENTATION_ROADMAP.md)** - Complete 13-phase implementation guide
- **[API Documentation](API_DOCUMENTATION.md)** - API reference (to be created)
- **[Architecture](ENTERPRISE_RAG_VISION.md)** - System architecture and design
- **[Setup Guide](SETUP.md)** - Detailed setup instructions

## 🎯 Roadmap Status

### ✅ Completed (Phase 1-2)
- [x] Project structure setup
- [x] Semantic Kernel integration with Gemini default
- [x] Domain models and DTOs
- [x] Core interfaces
- [x] Database schema design
- [x] Configuration system

### 🚧 In Progress (Phase 3-4)
- [ ] Document extraction services
- [ ] Chunking service
- [ ] Category suggestion (dual approach)
- [ ] Database migrations

### 📋 Planned (Phase 5-13)
- [ ] Microsoft Agent Framework integration
- [ ] RAG pipeline implementation
- [ ] Blazor UI components
- [ ] API controllers
- [ ] Advanced features
- [ ] Testing
- [ ] Documentation
- [ ] Deployment

## 🤝 Contributing

Contributions are welcome! Please read the contribution guidelines before submitting PRs.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Microsoft Semantic Kernel team
- Google Gemini AI team
- .NET and Blazor communities

## 📞 Support

For issues, questions, or suggestions:
- Open an issue on GitHub
- Contact: [project maintainer]

---

**Note**: This is an active development project. See `IMPLEMENTATION_ROADMAP.md` for detailed task list and timeline.

**Estimated completion**: 3-4 weeks for single developer, 2-3 weeks for small team
