# Fixing Vector Dimension Mismatch Error

## 🔴 Problem

When uploading a document, you encounter the error:
```
CRITICAL ERROR - File saved but DB create failed
Error: DATABASE DIMENSION MISMATCH ERROR:
Le dimensioni del vettore 1536 e 768 non corrispondono.
```

## 🎯 Root Cause

This error occurs when:
1. Your database was created using SQL scripts (e.g., `SqlServer2025_Schema.sql`) that define VECTOR columns with **fixed dimensions** (e.g., `VECTOR(1536)`)
2. Your AI provider generates embeddings with a **different dimension** than what the database expects
   - Example: Database expects VECTOR(1536), but Gemini generates 768-dimensional embeddings
   - Example: Database expects VECTOR(768), but OpenAI generates 1536-dimensional embeddings

## ✅ Solution

### Option 1: Apply the Database Fix (Recommended)

Run the SQL script that converts VECTOR columns to flexible `nvarchar(max)` storage:

```bash
# Connect to your SQL Server and run the migration script
sqlcmd -S localhost -U sa -P YourPassword -i Database/UpdateScripts/007_ConvertVectorToNvarcharMax.sql
```

This script:
- ✅ Converts `VECTOR(X)` columns to `nvarchar(max)`
- ✅ Preserves all existing data
- ✅ Enables support for any embedding dimension (256-4096)
- ✅ Is safe to run multiple times

### Option 2: Use EF Core Migrations (If Using Code-First Approach)

If you're using Entity Framework Core migrations, the latest migration already includes support for flexible dimensions:

```bash
cd DocN.Server
dotnet ef database update
```

## 🔧 What Was Fixed

### Code Changes

1. **Upload.razor**: Now sets `EmbeddingDimension` when creating documents
   ```csharp
   EmbeddingVector = embedding,
   EmbeddingDimension = embedding?.Length,  // ← NEW: Track the dimension
   ```

2. **DocumentService.cs**: Automatically sets `EmbeddingDimension` as a safety net
   ```csharp
   if (document.EmbeddingVector != null && document.EmbeddingVector.Length > 0)
   {
       document.EmbeddingDimension = document.EmbeddingVector.Length;
   }
   ```

3. **Database Schema**: VECTOR columns converted to `nvarchar(max)` for flexibility

### Database Changes

**Before** (Rigid - Fixed Dimensions):
```sql
EmbeddingVector VECTOR(1536) NULL  -- ❌ Only accepts 1536 dimensions
```

**After** (Flexible - Any Dimensions):
```sql
EmbeddingVector nvarchar(max) NULL  -- ✅ Accepts any dimension (256-4096)
EmbeddingDimension int NULL         -- ✅ Tracks the actual dimension used
```

## 📊 Supported Embedding Dimensions

After applying the fix, your system supports:

| AI Provider | Model | Dimensions | Notes |
|-------------|-------|------------|-------|
| **Gemini** | text-embedding-004 | 768 (default) | Google's embedding model |
| **Gemini** | text-embedding-004 | 700 (custom) | Custom dimension supported |
| **OpenAI** | text-embedding-ada-002 | 1536 | Classic OpenAI model |
| **OpenAI** | text-embedding-3-small | 1536 (default) | New model with flexibility |
| **OpenAI** | text-embedding-3-small | 256-1536 (custom) | Configurable dimensions |
| **OpenAI** | text-embedding-3-large | 3072 (default) | High-capacity model |
| **Custom** | Any provider | 256-4096 | Any dimension in this range |

## 🚀 Verification Steps

After applying the fix:

1. **Check the database schema**:
   ```sql
   SELECT 
       TABLE_NAME,
       COLUMN_NAME,
       DATA_TYPE,
       CHARACTER_MAXIMUM_LENGTH
   FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_NAME IN ('Documents', 'DocumentChunks')
   AND COLUMN_NAME IN ('EmbeddingVector', 'ChunkEmbedding');
   ```
   
   You should see `DATA_TYPE = 'nvarchar'` and `CHARACTER_MAXIMUM_LENGTH = -1` (meaning max)

2. **Restart your application**:
   ```bash
   # Stop and start DocN application
   ```

3. **Upload a test document**:
   - Try uploading a document through the web interface
   - The upload should now succeed
   - Check the database to verify `EmbeddingDimension` is populated

4. **Check different AI providers** (optional):
   - Test with Gemini (768 dimensions)
   - Test with OpenAI (1536 dimensions)
   - Both should work in the same database

## 🔍 Technical Details

### Why nvarchar(max)?

The system stores embeddings as JSON arrays in `nvarchar(max)` columns:
```json
[0.123, 0.456, 0.789, ...]
```

**Benefits**:
- ✅ Flexible dimensions - no fixed size requirement
- ✅ Human-readable format for debugging
- ✅ Compatible with SQL Server 2025 VECTOR functions (which accept JSON)
- ✅ Easy to migrate between different embedding dimensions
- ✅ Multiple AI providers can coexist in same database

### EmbeddingDimension Column

The `EmbeddingDimension` column tracks the actual dimension of each vector:
- Used for monitoring and analytics
- Helps identify which AI provider was used
- Enables future optimizations based on dimension
- Nullable for backward compatibility with existing data

## ⚠️ Important Notes

1. **Existing Data**: 
   - Vectors stored in native VECTOR format are automatically converted
   - No data loss occurs during migration
   - Existing embeddings continue to work

2. **Performance**:
   - `nvarchar(max)` storage is efficient for JSON data
   - SQL Server 2025 VECTOR functions work with JSON format
   - No significant performance difference vs native VECTOR type

3. **Mixed Dimensions**:
   - You can have documents with different embedding dimensions in the same database
   - For best search results, use the same dimension for documents you want to compare
   - Consider organizing by dimension if needed for your use case

## 📚 Related Documentation

- [FLEXIBLE_VECTOR_DIMENSIONS.md](../FLEXIBLE_VECTOR_DIMENSIONS.md) - Overview of flexible dimension support
- [VECTOR_DIMENSION_FIX.md](../VECTOR_DIMENSION_FIX.md) - Migration guide for existing databases
- [Database/UpdateScripts/007_ConvertVectorToNvarcharMax.sql](../Database/UpdateScripts/007_ConvertVectorToNvarcharMax.sql) - The SQL migration script

## 🆘 Troubleshooting

### Error persists after running migration

1. Verify the script ran successfully:
   ```sql
   -- Should show nvarchar, not vector
   SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS 
   WHERE TABLE_NAME = 'Documents' AND COLUMN_NAME = 'EmbeddingVector';
   ```

2. Restart the application to clear any cached connections

3. Check application logs for detailed error messages

### Need to revert to fixed VECTOR dimensions

If you specifically need fixed VECTOR dimensions (not recommended):
```sql
-- WARNING: This will only work if all your embeddings have the same dimension
ALTER TABLE Documents ALTER COLUMN EmbeddingVector VECTOR(1536) NULL;
ALTER TABLE DocumentChunks ALTER COLUMN ChunkEmbedding VECTOR(1536) NULL;
```

### Different embedding dimensions needed for different tenants

The flexible dimension approach is perfect for multi-tenant scenarios:
- Each tenant can use their preferred AI provider
- No database schema changes needed per tenant
- `EmbeddingDimension` column tracks which dimension is used

## 💡 Best Practices

1. **Choose One Provider for Related Documents**:
   - Use the same AI provider (and dimension) for documents you want to search together
   - Semantic similarity works best when comparing same-dimension vectors

2. **Monitor Dimension Usage**:
   ```sql
   -- See which dimensions are in use
   SELECT EmbeddingDimension, COUNT(*) as Count
   FROM Documents
   WHERE EmbeddingDimension IS NOT NULL
   GROUP BY EmbeddingDimension;
   ```

3. **Document Your AI Provider Configuration**:
   - Keep track of which provider/model you're using
   - Document the expected dimension in your configuration
   - Use the `EmbeddingDimension` column to audit actual usage

---

**Status**: ✅ This issue has been resolved. After applying the fix, the system supports flexible embedding dimensions from any AI provider.
