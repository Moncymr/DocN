# DocN - API and Embedding Configuration Documentation

## Table of Contents
1. [Embedding Service API](#embedding-service-api)
2. [Vector Database Configuration](#vector-database-configuration)
3. [Authentication API](#authentication-api)
4. [Document Management API](#document-management-api)

---

## Embedding Service API

### Overview
DocN uses Azure OpenAI's text-embedding-ada-002 model to generate 1536-dimensional vector embeddings for semantic search capabilities.

### Configuration

#### Current Implementation (Value Converter Approach)

**Important Note:** Currently, DocN uses a value converter to store embeddings as CSV strings in `nvarchar(max)` columns. This is a temporary solution until SQL Server 2025's VECTOR type is fully supported by Entity Framework Core.

**Entity Framework Configuration:**
```csharp
// Value converter for float[] to string
var converter = new ValueConverter<float[]?, string?>(
    v => v == null ? null : string.Join(",", v),
    v => v == null ? null : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(float.Parse)
        .ToArray()
);

entity.Property(e => e.EmbeddingVector)
    .HasColumnType("nvarchar(max)")
    .HasConversion(converter)
    .IsRequired(false);
```

**Database Column:**
```sql
ALTER TABLE Documents 
ADD EmbeddingVector NVARCHAR(MAX) NULL;
```

#### Future: SQL Server 2025 Vector Support (When Available)

Once EF Core adds native support for SQL Server 2025's VECTOR type, the configuration will be:

**Database Column Configuration:**
```sql
ALTER TABLE Documents 
ADD EmbeddingVector VECTOR(1536) NULL;
```

**Entity Framework Configuration:**
```csharp
entity.Property(e => e.EmbeddingVector)
    .HasColumnType("VECTOR(1536)")
    .IsRequired(false);
```

### C# Model

```csharp
public class Document
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    
    // Vector embedding for semantic search (1536 dimensions for text-embedding-ada-002)
    public float[]? EmbeddingVector { get; set; }
    
    // ... other properties
}
```

### IEmbeddingService Interface

```csharp
public interface IEmbeddingService
{
    /// <summary>
    /// Generates a 1536-dimensional embedding vector for the given text
    /// </summary>
    /// <param name="text">Text to embed</param>
    /// <returns>Float array of 1536 dimensions, or null if embedding fails</returns>
    Task<float[]?> GenerateEmbeddingAsync(string text);
    
    /// <summary>
    /// Searches for documents similar to the query embedding using cosine similarity
    /// </summary>
    /// <param name="queryEmbedding">Query embedding vector (1536 dimensions)</param>
    /// <param name="topK">Number of top results to return (default: 5)</param>
    /// <returns>List of documents ordered by similarity score</returns>
    Task<List<Document>> SearchSimilarDocumentsAsync(float[] queryEmbedding, int topK = 5);
}
```

### Usage Examples

#### Generate Embedding for Text
```csharp
var embeddingService = serviceProvider.GetRequiredService<IEmbeddingService>();
var text = "This is a sample document about machine learning.";
float[]? embedding = await embeddingService.GenerateEmbeddingAsync(text);

if (embedding != null)
{
    Console.WriteLine($"Generated embedding with {embedding.Length} dimensions");
}
```

#### Search Similar Documents
```csharp
var queryText = "machine learning algorithms";
var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(queryText);

if (queryEmbedding != null)
{
    var similarDocs = await embeddingService.SearchSimilarDocumentsAsync(queryEmbedding, topK: 10);
    
    foreach (var doc in similarDocs)
    {
        Console.WriteLine($"Similar document: {doc.FileName}");
    }
}
```

### Performance Considerations

**Vector Storage:**
- SQL Server 2025 VECTOR type provides optimized storage
- 1536 float32 values = ~6KB per embedding
- Consider indexing strategies for large document collections

**Cosine Similarity Formula:**
```
similarity = (A · B) / (||A|| × ||B||)
```

Where:
- A · B = dot product of vectors A and B
- ||A|| = magnitude of vector A
- ||B|| = magnitude of vector B

**Optimization Tips:**
1. Use batch processing for multiple documents
2. Cache embeddings to avoid regeneration
3. Consider approximate nearest neighbor (ANN) algorithms for >10,000 documents
4. Use SQL Server's native vector search when available

---

## Vector Database Configuration

### SQL Server 2025 Setup

#### 1. Enable Vector Support
```sql
-- Check if vector support is available
SELECT SERVERPROPERTY('IsVectorSupported') as VectorSupported;
```

#### 2. Create Database with Vector Support
```sql
CREATE DATABASE DocNDb
COLLATE Latin1_General_100_CI_AS_SC_UTF8;
GO

USE DocNDb;
GO
```

#### 3. Create Table with Vector Column
```sql
CREATE TABLE Documents (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    ContentType NVARCHAR(MAX) NOT NULL,
    FileSize BIGINT NOT NULL,
    ExtractedText NVARCHAR(MAX),
    
    -- Vector embedding column for semantic search
    EmbeddingVector VECTOR(1536) NULL,
    
    -- Metadata
    UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    OwnerId NVARCHAR(450) NULL,
    
    -- Foreign key to AspNetUsers
    CONSTRAINT FK_Documents_AspNetUsers FOREIGN KEY (OwnerId)
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
GO

-- Create index for owner lookups
CREATE INDEX IX_Documents_OwnerId ON Documents(OwnerId);
GO
```

#### 4. Vector Search Query Example
```sql
-- Find documents similar to a query vector
DECLARE @queryVector VECTOR(1536) = CAST('[0.1, 0.2, ...]' AS VECTOR(1536));

SELECT TOP 5
    Id,
    FileName,
    -- Calculate cosine similarity
    VECTOR_DISTANCE('cosine', EmbeddingVector, @queryVector) as Similarity
FROM Documents
WHERE EmbeddingVector IS NOT NULL
ORDER BY Similarity DESC;
```

### Migration from String to VECTOR Type

If you have existing data stored as JSON string:

```sql
-- Backup table first
SELECT * INTO Documents_Backup FROM Documents;

-- Add new vector column
ALTER TABLE Documents ADD EmbeddingVector_New VECTOR(1536) NULL;
GO

-- Migrate data (if embeddings were stored as JSON)
-- Note: This requires custom migration logic in C#
-- The migration will handle conversion from JSON string to float array

-- Drop old column and rename new one
ALTER TABLE Documents DROP COLUMN EmbeddingVector;
GO

EXEC sp_rename 'Documents.EmbeddingVector_New', 'EmbeddingVector', 'COLUMN';
GO
```

### Connection String Configuration

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=DocNDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**For SQL Server 2025 Preview:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER\\SQL2025;Database=DocNDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

---

## Authentication API

### Overview
DocN uses ASP.NET Core Identity for secure user authentication with email/password.

### Endpoints

#### POST /login
Authenticate user with email and password.

**Form Data:**
- `Email` (required): User email address
- `Password` (required): User password
- `RememberMe` (optional): Boolean for persistent login

**Response:**
- Success: Redirects to `/`
- Failure: Returns error message

#### POST /register
Register a new user account.

**Form Data:**
- `FirstName` (required): User's first name
- `LastName` (required): User's last name
- `Email` (required): Valid email address
- `Password` (required): Password (min 6 chars, requires uppercase, lowercase, digit)
- `ConfirmPassword` (required): Must match password

**Response:**
- Success: Auto-login and redirect to `/`
- Failure: Returns validation errors

#### POST /forgot-password
Request password reset link.

**Form Data:**
- `Email` (required): Registered email address

**Response:**
- Success message (always shown for security)

#### POST /reset-password
Reset password with token.

**Query Parameters:**
- `email`: User email
- `token`: Password reset token

**Form Data:**
- `Email` (required): User email
- `NewPassword` (required): New password
- `ConfirmPassword` (required): Must match new password

**Response:**
- Success: Redirect to login
- Failure: Returns error message

#### POST /logout
Sign out current user (requires authentication).

**Response:**
- Redirects to `/`

### Password Policy

Default requirements (configurable in `Program.cs`):
- Minimum length: 6 characters
- Requires at least one uppercase letter
- Requires at least one lowercase letter
- Requires at least one digit
- Special characters: Not required
- Unique email: Required

**Configure in Program.cs:**
```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;  // Change to 8
    options.Password.RequireNonAlphanumeric = true;  // Require special chars
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

### Security Features

1. **Password Hashing**: Uses ASP.NET Core Identity's secure password hashing
2. **Account Lockout**: Enabled after multiple failed login attempts
3. **CSRF Protection**: Antiforgery tokens on all forms
4. **Secure Cookies**: HttpOnly, Secure flags in production
5. **Email Confirmation**: Can be enabled for production
6. **Two-Factor Authentication**: Framework support available

---

## Document Management API

### Document Upload with Embedding

When uploading a document, embeddings are automatically generated:

```csharp
// Extract text from document
string extractedText = ExtractTextFromDocument(file);

// Generate embedding
var embedding = await embeddingService.GenerateEmbeddingAsync(extractedText);

// Create document
var document = new Document
{
    FileName = file.Name,
    FilePath = savedPath,
    ExtractedText = extractedText,
    EmbeddingVector = embedding,  // float[] type
    OwnerId = currentUserId,
    UploadedAt = DateTime.UtcNow
};

await documentService.CreateDocumentAsync(document);
```

### Semantic Search

Search documents by natural language query:

```csharp
// User search query
string query = "contracts about software licensing";

// Generate query embedding
var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query);

// Find similar documents
var results = await embeddingService.SearchSimilarDocumentsAsync(
    queryEmbedding, 
    topK: 10
);
```

### RAG (Retrieval Augmented Generation)

Combine document retrieval with AI generation:

```csharp
var ragService = serviceProvider.GetRequiredService<IRAGService>();

string userQuestion = "What are the key terms in our contracts?";
string response = await ragService.GenerateResponseAsync(userQuestion);

// Response includes context from relevant documents
Console.WriteLine(response);
```

---

## Best Practices

### 1. Embedding Generation
- Generate embeddings asynchronously to avoid blocking
- Batch process multiple documents for efficiency
- Cache embeddings to avoid regeneration
- Handle API rate limits gracefully

### 2. Vector Search
- Set appropriate `topK` values based on use case
- Use similarity thresholds to filter low-quality matches
- Consider user permissions when retrieving documents
- Monitor query performance for large datasets

### 3. Security
- Always validate user permissions before returning documents
- Sanitize user input before embedding generation
- Use HTTPS in production
- Rotate API keys regularly
- Enable audit logging for sensitive operations

### 4. Performance
- Index frequently queried columns
- Use connection pooling
- Implement caching for frequently accessed data
- Monitor embedding API usage and costs
- Consider CDN for static assets

---

## Troubleshooting

### Common Issues

**Issue: "Vector type not supported"**
- Solution: Ensure SQL Server 2025 or later is installed
- Check: `SELECT SERVERPROPERTY('ProductVersion')`

**Issue: "Embedding generation fails"**
- Check Azure OpenAI API key configuration
- Verify endpoint URL is correct
- Ensure deployment name matches
- Check API quota and rate limits

**Issue: "Search returns no results"**
- Verify documents have embeddings generated
- Check similarity threshold isn't too high
- Confirm query embedding generation succeeded
- Review vector dimensions match (1536)

**Issue: "Authentication not working"**
- Verify database migrations ran successfully
- Check Identity tables exist (AspNetUsers, etc.)
- Confirm antiforgery tokens are enabled
- Review browser cookies and CORS settings

---

## Support and Resources

- **Azure OpenAI Documentation**: https://learn.microsoft.com/azure/ai-services/openai/
- **SQL Server 2025 Vector Preview**: https://learn.microsoft.com/sql/relational-databases/vectors/
- **ASP.NET Core Identity**: https://learn.microsoft.com/aspnet/core/security/authentication/identity
- **Entity Framework Core**: https://learn.microsoft.com/ef/core/

---

**Last Updated**: December 2024  
**Version**: 1.0
