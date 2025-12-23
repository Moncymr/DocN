# DocN - Document Management System

## Overview
DocN is a complete document management solution built with .NET 10 and Blazor, designed for intelligent document archiving and retrieval with AI-powered semantic search capabilities.

## Problem Solved
This implementation addresses the issue where the `/documents` endpoint was not returning documents when the vector field (used for semantic search embeddings) was not yet populated. Documents exist in the database but were not being displayed because the vector calculation was not complete.

## Solution
The application now returns **all documents regardless of vector field status**, allowing users to view and work with documents even before AI embeddings are calculated.

## Architecture

### Projects
1. **DocN.Data** - Data access layer with Entity Framework Core
2. **DocN.Server** - ASP.NET Core Web API backend
3. **DocN.Client** - Blazor WebAssembly frontend
4. **DocN.Server.Tests** - Unit tests for API endpoints

### Key Components

#### Document Entity
```csharp
public class Document
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string? FilePath { get; set; }
    public string? ContentText { get; set; }
    public string? Category { get; set; }
    public byte[]? Vector { get; set; }  // ✅ NULLABLE - Optional
    public DateTime UploadedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

#### API Endpoints
- `GET /documents` - Returns all documents (including those without vectors)
- `GET /documents/{id}` - Returns a specific document
- `POST /documents` - Creates a new document

## Running the Application

### Prerequisites
- .NET 10 SDK
- SQL Server (optional - uses in-memory database by default)

### Build
```bash
dotnet build
```

### Run Server (API)
```bash
cd DocN.Server
dotnet run
```
Server will start on: `http://localhost:5210`

### Run Tests
```bash
dotnet test
```

### Run Client (Blazor UI)
```bash
cd DocN.Client
dotnet run
```

## Configuration

### Database Connection
The application uses an in-memory database by default for development. To use SQL Server:

1. Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DocArc": "Server=YOUR_SERVER;Database=DocArc;Trusted_Connection=true;"
  }
}
```

2. Run migrations:
```bash
dotnet ef database update --project DocN.Data --startup-project DocN.Server
```

## Features

### Current Features
- ✅ Document upload and storage
- ✅ Document listing (all documents, regardless of vector status)
- ✅ Document retrieval by ID
- ✅ Vector status indicator in UI
- ✅ Automatic database seeding for testing
- ✅ RESTful API

### Planned Features
- 🔄 AI-powered category suggestion on upload
- 🔄 Vector embedding calculation
- 🔄 Semantic search
- 🔄 Document text extraction
- 🔄 Integration with Azure OpenAI

## Testing

The application includes comprehensive unit tests that verify:
1. All documents are returned regardless of vector field status
2. Empty result handling
3. Individual document retrieval with null vectors

Test coverage includes scenarios with:
- Documents WITH vectors
- Documents WITHOUT vectors
- Mixed scenarios

## Key Implementation Details

### Vector Field Handling
The critical fix ensures documents without vectors are not filtered out:

**Entity Configuration:**
```csharp
entity.Property(e => e.Vector).IsRequired(false); // Vector is optional
```

**Controller Query:**
```csharp
var documents = await _context.Documents
    .OrderByDescending(d => d.UploadedAt)
    .ToListAsync(); // No filter on Vector field
```

## Security
- ✅ Uses Entity Framework Core parameterized queries (no SQL injection risk)
- ✅ Connection strings from configuration (not hardcoded)
- ✅ Input validation via ASP.NET Core model binding
- ✅ Appropriate error handling
- ✅ Logging for debugging without exposing sensitive data

## Development

### Project Structure
```
DocN/
├── DocN.Data/              # Data access layer
│   ├── Entities/          
│   │   └── Document.cs    # Document entity
│   └── DocArcContext.cs   # EF Core DbContext
├── DocN.Server/            # Web API
│   ├── Controllers/       
│   │   └── DocumentsController.cs
│   └── Services/
│       └── DatabaseSeeder.cs
├── DocN.Client/            # Blazor WebAssembly UI
│   ├── Pages/
│   │   └── Documents.razor # Document list page
│   └── Models/
│       └── Document.cs
└── DocN.Server.Tests/      # Unit tests
    └── DocumentsControllerTests.cs
```

## Troubleshooting

### Issue: Documents not showing
**Solution**: This was the original problem. Ensure you're using the latest version where documents are returned regardless of vector status.

### Issue: LocalDB not supported
**Solution**: The application automatically falls back to in-memory database when SQL Server is not available.

## Contributing
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `dotnet test`
5. Submit a pull request

## License
[Specify your license here]

## Authors
- Initial implementation: Moncymr
- Vector field fix: Copilot Agent

## Changelog

### v1.0.0 (2025-12-23)
- ✅ Initial implementation
- ✅ Fix for vector field issue
- ✅ Document listing endpoint
- ✅ Blazor UI
- ✅ Unit tests
- ✅ Database seeding
