# API Documentation - DocN

## 📋 Panoramica API

DocN espone una REST API per l'integrazione programmatica con sistemi esterni.

**Base URL:** `https://api.docn.example.com/api/v1`

**Autenticazione:** API Key o JWT Bearer Token

---

## 🔐 Autenticazione

### API Key Authentication

Includere l'API key nell'header:

```http
GET /api/v1/documents HTTP/1.1
Host: api.docn.example.com
X-API-Key: docn_1234567890abcdef
```

### JWT Bearer Token (User Login)

```http
POST /api/v1/auth/login HTTP/1.1
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2024-12-29T19:51:38Z",
  "refreshToken": "refresh_token_here"
}
```

**Usage:**
```http
GET /api/v1/documents HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 📄 Endpoints Documenti

### Upload Document

```http
POST /api/v1/documents
Content-Type: multipart/form-data
X-API-Key: your-api-key

{
  "file": <binary>,
  "category": "Contracts",
  "tags": ["legal", "2024"],
  "visibility": "Private",
  "notes": "Important contract",
  "extractTags": true,
  "generateEmbeddings": true
}
```

**Response:**
```json
{
  "id": 123,
  "fileName": "contract.pdf",
  "filePath": "/uploads/2024/12/contract.pdf",
  "fileSize": 1048576,
  "contentType": "application/pdf",
  "category": "Contracts",
  "tags": ["legal", "2024", "contract"],
  "extractedText": "This is a contract...",
  "aiSuggestions": {
    "category": "Legal Contracts",
    "reasoning": "Document contains legal terminology",
    "confidence": 0.95
  },
  "uploadedAt": "2024-12-28T19:51:38Z",
  "processingStatus": "Completed"
}
```

**Errors:**
- `400 Bad Request` - File troppo grande o tipo non supportato
- `401 Unauthorized` - API key mancante o non valida
- `413 Payload Too Large` - File > 50MB
- `500 Internal Server Error` - Errore server

---

### List Documents

```http
GET /api/v1/documents?page=1&perPage=20&category=Contracts&sortBy=uploadedAt&order=desc
X-API-Key: your-api-key
```

**Query Parameters:**
- `page` (int, default: 1) - Numero pagina
- `perPage` (int, default: 20, max: 100) - Risultati per pagina
- `category` (string, optional) - Filtra per categoria
- `tags` (string[], optional) - Filtra per tags (comma-separated)
- `visibility` (string, optional) - Private, Shared, Organization, Public
- `sortBy` (string, default: uploadedAt) - Campo ordinamento
- `order` (string, default: desc) - asc o desc
- `searchText` (string, optional) - Ricerca full-text

**Response:**
```json
{
  "documents": [
    {
      "id": 123,
      "fileName": "contract.pdf",
      "category": "Contracts",
      "tags": ["legal", "2024"],
      "fileSize": 1048576,
      "uploadedAt": "2024-12-28T19:51:38Z",
      "visibility": "Private"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "perPage": 20,
    "totalPages": 5,
    "totalCount": 95
  }
}
```

---

### Get Document Details

```http
GET /api/v1/documents/123
X-API-Key: your-api-key
```

**Response:**
```json
{
  "id": 123,
  "fileName": "contract.pdf",
  "filePath": "/uploads/2024/12/contract.pdf",
  "fileSize": 1048576,
  "contentType": "application/pdf",
  "extractedText": "Full text content...",
  "category": "Contracts",
  "tags": ["legal", "2024", "contract"],
  "notes": "Important contract",
  "visibility": "Private",
  "pageCount": 5,
  "detectedLanguage": "en",
  "aiMetadata": {
    "entities": ["Company A", "Company B"],
    "dates": ["2024-01-15"],
    "amounts": ["$100,000"]
  },
  "uploadedAt": "2024-12-28T19:51:38Z",
  "uploadedBy": "user@example.com",
  "accessCount": 5,
  "lastAccessedAt": "2024-12-28T20:00:00Z"
}
```

---

### Download Document

```http
GET /api/v1/documents/123/download
X-API-Key: your-api-key
```

**Response:**
- Content-Type: application/pdf (o altro MIME type)
- Content-Disposition: attachment; filename="contract.pdf"
- Body: file binario

---

### Delete Document

```http
DELETE /api/v1/documents/123
X-API-Key: your-api-key
```

**Response:**
```json
{
  "success": true,
  "message": "Document deleted successfully"
}
```

**Errors:**
- `404 Not Found` - Documento non trovato
- `403 Forbidden` - Non autorizzato a eliminare

---

### Update Document Metadata

```http
PATCH /api/v1/documents/123
Content-Type: application/json
X-API-Key: your-api-key

{
  "category": "Legal Documents",
  "tags": ["legal", "2024", "updated"],
  "notes": "Updated notes",
  "visibility": "Shared"
}
```

**Response:**
```json
{
  "id": 123,
  "fileName": "contract.pdf",
  "category": "Legal Documents",
  "tags": ["legal", "2024", "updated"],
  "notes": "Updated notes",
  "visibility": "Shared",
  "updatedAt": "2024-12-28T20:15:00Z"
}
```

---

## 🔍 Endpoints Ricerca

### Semantic Search

```http
POST /api/v1/search
Content-Type: application/json
X-API-Key: your-api-key

{
  "query": "contratti di vendita 2024",
  "searchType": "Hybrid",
  "topK": 10,
  "minSimilarity": 0.7,
  "filters": {
    "category": "Contracts",
    "tags": ["2024"],
    "dateFrom": "2024-01-01",
    "dateTo": "2024-12-31"
  }
}
```

**Search Types:**
- `Vector` - Solo ricerca semantica
- `FullText` - Solo full-text search
- `Hybrid` - Combina vector + full-text con RRF

**Response:**
```json
{
  "results": [
    {
      "document": {
        "id": 123,
        "fileName": "contract_2024.pdf",
        "category": "Contracts",
        "extractedText": "Contract excerpt..."
      },
      "score": 0.95,
      "relevance": "High",
      "highlightedText": "...contratti di <em>vendita</em>...",
      "matchedChunks": [
        {
          "chunkId": 456,
          "text": "Questo contratto di vendita...",
          "score": 0.97
        }
      ]
    }
  ],
  "searchMetadata": {
    "totalResults": 15,
    "searchTime": "125ms",
    "searchType": "Hybrid",
    "query": "contratti di vendita 2024"
  }
}
```

---

### Find Similar Documents

```http
GET /api/v1/documents/123/similar?topK=5&minSimilarity=0.7
X-API-Key: your-api-key
```

**Response:**
```json
{
  "sourceDocument": {
    "id": 123,
    "fileName": "contract.pdf"
  },
  "similarDocuments": [
    {
      "id": 124,
      "fileName": "similar_contract.pdf",
      "category": "Contracts",
      "similarityScore": 0.92,
      "matchingConcepts": ["contract terms", "payment clause"]
    }
  ]
}
```

---

## 💬 Endpoints RAG (Chat)

### Chat with Documents

```http
POST /api/v1/chat
Content-Type: application/json
X-API-Key: your-api-key

{
  "query": "Quali sono i termini di pagamento nei contratti del 2024?",
  "conversationId": null,
  "maxDocuments": 5,
  "documentFilters": {
    "category": "Contracts",
    "tags": ["2024"]
  },
  "responseLanguage": "it"
}
```

**Response:**
```json
{
  "answer": "Nei contratti del 2024, i termini di pagamento prevedono...",
  "conversationId": 789,
  "sourceDocuments": [
    {
      "id": 123,
      "fileName": "contract_2024.pdf",
      "relevance": 0.95,
      "citedPassages": [
        "I termini di pagamento sono net 30 giorni..."
      ]
    }
  ],
  "confidence": 0.89,
  "responseTime": "2.3s",
  "tokensUsed": 450
}
```

---

### Get Conversation History

```http
GET /api/v1/conversations/789
X-API-Key: your-api-key
```

**Response:**
```json
{
  "conversationId": 789,
  "createdAt": "2024-12-28T19:00:00Z",
  "messages": [
    {
      "role": "user",
      "content": "Quali sono i termini di pagamento?",
      "timestamp": "2024-12-28T19:00:00Z"
    },
    {
      "role": "assistant",
      "content": "I termini di pagamento prevedono...",
      "timestamp": "2024-12-28T19:00:02Z",
      "sourceDocuments": [123, 124]
    }
  ]
}
```

---

## 🤖 Endpoints AI

### Generate Embeddings

```http
POST /api/v1/embeddings
Content-Type: application/json
X-API-Key: your-api-key

{
  "text": "This is a sample text to embed",
  "provider": "Gemini"
}
```

**Response:**
```json
{
  "embedding": [0.123, -0.456, 0.789, ...],
  "dimensions": 768,
  "provider": "Gemini",
  "model": "text-embedding-004",
  "processingTime": "150ms"
}
```

---

### Extract Tags

```http
POST /api/v1/ai/extract-tags
Content-Type: application/json
X-API-Key: your-api-key

{
  "text": "This is a legal contract between Company A and Company B...",
  "maxTags": 5
}
```

**Response:**
```json
{
  "tags": [
    {
      "tag": "legal",
      "confidence": 0.95
    },
    {
      "tag": "contract",
      "confidence": 0.92
    },
    {
      "tag": "companies",
      "confidence": 0.85
    }
  ]
}
```

---

### Suggest Category

```http
POST /api/v1/ai/suggest-category
Content-Type: application/json
X-API-Key: your-api-key

{
  "fileName": "invoice_2024.pdf",
  "extractedText": "Invoice for services rendered..."
}
```

**Response:**
```json
{
  "suggestedCategory": "Financial Documents",
  "reasoning": "Document is an invoice containing financial information",
  "confidence": 0.93,
  "alternativeCategories": [
    {
      "category": "Invoices",
      "confidence": 0.87
    }
  ]
}
```

---

## 📊 Endpoints Admin

### Get Statistics

```http
GET /api/v1/admin/statistics
X-API-Key: your-admin-api-key
```

**Response:**
```json
{
  "documents": {
    "total": 10543,
    "uploadedToday": 127,
    "uploadedThisMonth": 3421,
    "byCategory": {
      "Contracts": 2341,
      "Financial": 1823,
      "Other": 6379
    }
  },
  "users": {
    "total": 235,
    "active": 89,
    "newThisMonth": 12
  },
  "searches": {
    "today": 1234,
    "thisMonth": 45678,
    "avgResponseTime": "245ms"
  },
  "rag": {
    "queriesThisMonth": 8765,
    "avgResponseTime": "2.8s"
  },
  "storage": {
    "usedGB": 456.7,
    "limitGB": 1000
  }
}
```

---

### List API Keys

```http
GET /api/v1/admin/api-keys
X-API-Key: your-admin-api-key
```

**Response:**
```json
{
  "apiKeys": [
    {
      "id": 1,
      "name": "Integration App",
      "keyPrefix": "docn_1234...",
      "scopes": ["read:documents", "write:documents"],
      "createdAt": "2024-01-15T10:00:00Z",
      "lastUsedAt": "2024-12-28T19:30:00Z",
      "isActive": true
    }
  ]
}
```

---

### Create API Key

```http
POST /api/v1/admin/api-keys
Content-Type: application/json
X-API-Key: your-admin-api-key

{
  "name": "New Integration",
  "scopes": ["read:documents", "write:documents", "search"],
  "expiresAt": "2025-12-31T23:59:59Z"
}
```

**Response:**
```json
{
  "id": 2,
  "name": "New Integration",
  "apiKey": "docn_9876543210fedcba_KEEP_THIS_SECRET",
  "scopes": ["read:documents", "write:documents", "search"],
  "createdAt": "2024-12-28T20:00:00Z",
  "expiresAt": "2025-12-31T23:59:59Z",
  "warning": "Save this API key - it won't be shown again!"
}
```

---

## 🔄 Batch Operations

### Batch Upload

```http
POST /api/v1/batch/upload
Content-Type: multipart/form-data
X-API-Key: your-api-key

{
  "files": [<binary1>, <binary2>, ...],
  "defaultCategory": "Documents",
  "defaultVisibility": "Private",
  "generateEmbeddings": true
}
```

**Response:**
```json
{
  "batchId": "batch_abc123",
  "totalFiles": 10,
  "status": "Processing",
  "estimatedCompletionTime": "2024-12-28T20:30:00Z"
}
```

---

### Check Batch Status

```http
GET /api/v1/batch/upload/batch_abc123
X-API-Key: your-api-key
```

**Response:**
```json
{
  "batchId": "batch_abc123",
  "status": "Completed",
  "totalFiles": 10,
  "successfulUploads": 9,
  "failedUploads": 1,
  "results": [
    {
      "fileName": "doc1.pdf",
      "status": "Success",
      "documentId": 123
    },
    {
      "fileName": "doc2.pdf",
      "status": "Failed",
      "error": "File too large"
    }
  ],
  "completedAt": "2024-12-28T20:25:00Z"
}
```

---

## ⚡ Rate Limits

| Endpoint | Rate Limit | Burst |
|----------|-----------|-------|
| `/api/v1/documents` (POST) | 10/min | 20 |
| `/api/v1/documents` (GET) | 60/min | 100 |
| `/api/v1/search` | 30/min | 50 |
| `/api/v1/chat` | 20/min | 30 |
| `/api/v1/embeddings` | 100/min | 150 |
| Admin endpoints | 100/min | 200 |

**Rate Limit Headers:**
```http
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1703794800
```

**429 Too Many Requests Response:**
```json
{
  "error": "Rate limit exceeded",
  "retryAfter": 30,
  "limit": 60,
  "resetAt": "2024-12-28T20:30:00Z"
}
```

---

## 🐛 Error Codes

| Code | Descrizione |
|------|-------------|
| 400 | Bad Request - Parametri non validi |
| 401 | Unauthorized - Autenticazione mancante |
| 403 | Forbidden - Permessi insufficienti |
| 404 | Not Found - Risorsa non trovata |
| 409 | Conflict - Conflitto (es. file già esistente) |
| 413 | Payload Too Large - File troppo grande |
| 422 | Unprocessable Entity - Validazione fallita |
| 429 | Too Many Requests - Rate limit superato |
| 500 | Internal Server Error - Errore server |
| 503 | Service Unavailable - Servizio temporaneamente non disponibile |

**Error Response Format:**
```json
{
  "error": {
    "code": "INVALID_INPUT",
    "message": "File type not supported",
    "details": {
      "field": "file",
      "supportedTypes": [".pdf", ".docx", ".txt"]
    },
    "timestamp": "2024-12-28T20:00:00Z",
    "requestId": "req_abc123"
  }
}
```

---

## 📦 SDKs e Librerie

### JavaScript/TypeScript

```javascript
import { DocNClient } from '@docn/sdk';

const client = new DocNClient({
  apiKey: 'your-api-key',
  baseUrl: 'https://api.docn.example.com'
});

// Upload document
const document = await client.documents.upload({
  file: fileBlob,
  category: 'Contracts',
  tags: ['legal', '2024']
});

// Search
const results = await client.search({
  query: 'contratti di vendita',
  searchType: 'Hybrid',
  topK: 10
});

// Chat
const response = await client.chat({
  query: 'Quali sono i termini di pagamento?',
  maxDocuments: 5
});
```

### Python

```python
from docn import DocNClient

client = DocNClient(api_key='your-api-key')

# Upload document
with open('contract.pdf', 'rb') as f:
    document = client.documents.upload(
        file=f,
        category='Contracts',
        tags=['legal', '2024']
    )

# Search
results = client.search(
    query='contratti di vendita',
    search_type='Hybrid',
    top_k=10
)

# Chat
response = client.chat(
    query='Quali sono i termini di pagamento?',
    max_documents=5
)
```

### C# / .NET

```csharp
using DocN.SDK;

var client = new DocNClient("your-api-key");

// Upload document
using var fileStream = File.OpenRead("contract.pdf");
var document = await client.Documents.UploadAsync(new UploadRequest
{
    File = fileStream,
    Category = "Contracts",
    Tags = new[] { "legal", "2024" }
});

// Search
var results = await client.Search.QueryAsync(new SearchRequest
{
    Query = "contratti di vendita",
    SearchType = SearchType.Hybrid,
    TopK = 10
});

// Chat
var response = await client.Chat.QueryAsync(new ChatRequest
{
    Query = "Quali sono i termini di pagamento?",
    MaxDocuments = 5
});
```

---

## 📖 OpenAPI / Swagger

Documentazione interattiva disponibile su:
- **Swagger UI**: https://api.docn.example.com/swagger
- **OpenAPI Spec**: https://api.docn.example.com/openapi.json

---

## 🔒 Best Practices Sicurezza

1. **Non esporre mai API keys** in codice frontend
2. **Usa HTTPS** per tutte le richieste
3. **Ruota API keys** regolarmente
4. **Limita scopes** al minimo necessario
5. **Monitora usage** per rilevare anomalie
6. **Usa IP whitelisting** per ambienti produzione
7. **Implementa retry logic** con exponential backoff

---

**Versione API:** v1  
**Data:** Dicembre 2024  
**Support:** api-support@docn.example.com
