# VECTOR Type Support Guide - Guida Supporto Tipo VECTOR

## 🔧 Issue: EF Core and SQL Server 2025 VECTOR Type

Entity Framework Core **does not natively support** the SQL Server 2025 `VECTOR` type yet. This causes a `NullReferenceException` during model finalization.

Entity Framework Core **non supporta nativamente** il tipo `VECTOR` di SQL Server 2025. Questo causa una `NullReferenceException` durante la finalizzazione del modello.

## 🆕 Recent Fix - Correzione Recente

**December 2025 Update**: Fixed critical type mismatch in migrations that caused `SqlException: varbinary incompatible with vector` error.

**Aggiornamento Dicembre 2025**: Corretto un grave errore di tipo nelle migration che causava l'errore `SqlException: varbinary incompatibile con vector`.

**The Problem / Il Problema:**
- Migration files incorrectly defined `EmbeddingVector` and `ChunkEmbedding` as `nvarchar(max)` (string)
- ApplicationDbContext.cs correctly configured them as `varbinary(max)` with value converter
- When saving embeddings, binary data was sent to string columns, causing type conflict
- Le migration definivano erroneamente `EmbeddingVector` e `ChunkEmbedding` come `nvarchar(max)` (stringa)
- ApplicationDbContext.cs li configurava correttamente come `varbinary(max)` con convertitore di valori
- Durante il salvataggio degli embedding, i dati binari venivano inviati a colonne stringa, causando conflitto di tipo

**The Fix / La Correzione:**
- Updated migrations to use `varbinary(max)` instead of `nvarchar(max)`
- Added migration `20250103000000_FixVectorColumnTypes.cs` to alter existing databases
- Aggiornate le migration per usare `varbinary(max)` invece di `nvarchar(max)`
- Aggiunta migration `20250103000000_FixVectorColumnTypes.cs` per alterare i database esistenti

## ✅ Solution - Soluzione

### Approach 1: Use SQL Script Directly (Recommended)
### Approccio 1: Usa direttamente lo Script SQL (Raccomandato)

**English:**
1. Run the SQL script `Database/CreateDatabase_Complete_V2.sql` directly on SQL Server
2. This creates tables with native `VECTOR(1536)` columns
3. The application will work correctly with the existing schema
4. **Do not use EF Core migrations** - they will try to change VECTOR to varbinary(max)

**Italiano:**
1. Esegui lo script SQL `Database/CreateDatabase_Complete_V2.sql` direttamente su SQL Server
2. Questo crea le tabelle con colonne native `VECTOR(1536)`
3. L'applicazione funzionerà correttamente con lo schema esistente
4. **Non usare le migration di EF Core** - proveranno a cambiare VECTOR in varbinary(max)

```sql
-- Run this on SQL Server:
-- Esegui questo su SQL Server:
sqlcmd -S your_server -d your_database -i Database/CreateDatabase_Complete_V2.sql
```

### Approach 2: Use EF Core with Manual ALTER
### Approccio 2: Usa EF Core con ALTER Manuale

**English:**
1. EF Core configuration uses `varbinary(max)` as a compatible intermediate type
2. Run EF Core migrations to create the initial schema
3. Manually alter the columns to use VECTOR type
4. The application will work correctly after the manual alteration

**Italiano:**
1. La configurazione EF Core usa `varbinary(max)` come tipo intermedio compatibile
2. Esegui le migration di EF Core per creare lo schema iniziale
3. Altera manualmente le colonne per usare il tipo VECTOR
4. L'applicazione funzionerà correttamente dopo l'alterazione manuale

```bash
# Step 1: Run migrations
dotnet ef migrations add InitialCreate --project DocN.Data
dotnet ef database update --project DocN.Data

# Step 2: Run this SQL to convert to VECTOR type
```

```sql
-- After EF Core migrations, run:
-- Dopo le migration di EF Core, esegui:

ALTER TABLE Documents 
ALTER COLUMN EmbeddingVector VECTOR(1536) NULL;

ALTER TABLE DocumentChunks
ALTER COLUMN ChunkEmbedding VECTOR(1536) NULL;

PRINT '✅ Columns converted to VECTOR type';
```

## 📋 Current Configuration - Configurazione Attuale

### In EF Core (ApplicationDbContext.cs):
```csharp
// Uses varbinary(max) for EF Core compatibility
// Usa varbinary(max) per compatibilità con EF Core
entity.Property(e => e.EmbeddingVector)
    .HasColumnType("varbinary(max)")  
    .HasConversion(vectorConverter)
    .IsRequired(false);
```

### In SQL Script (CreateDatabase_Complete_V2.sql):
```sql
-- Creates with native VECTOR type
-- Crea con il tipo nativo VECTOR
EmbeddingVector VECTOR(1536) NULL,
ChunkEmbedding VECTOR(1536) NULL,
```

## 🔄 Data Conversion - Conversione Dati

The application uses `Buffer.BlockCopy` to convert between `float[]` (C#) and `byte[]` (storage):

L'applicazione usa `Buffer.BlockCopy` per convertire tra `float[]` (C#) e `byte[]` (storage):

```csharp
// float[] -> byte[]
byte[] bytes = new byte[floats.Length * sizeof(float)];
Buffer.BlockCopy(floats, 0, bytes, 0, bytes.Length);

// byte[] -> float[]
float[] floats = new float[bytes.Length / sizeof(float)];
Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
```

This works correctly with both `varbinary(max)` and `VECTOR(1536)` since both store binary data.

Questo funziona correttamente sia con `varbinary(max)` che con `VECTOR(1536)` poiché entrambi memorizzano dati binari.

## ⚠️ Important Notes - Note Importanti

**English:**
- The SQL script creates `VECTOR(1536)` columns ✅
- EF Core migrations would create `varbinary(max)` columns ⚠️
- If you use the SQL script, **do not run EF Core migrations**
- If you use EF Core migrations, **manually ALTER to VECTOR after migration**
- The application code works with both column types

**Italiano:**
- Lo script SQL crea colonne `VECTOR(1536)` ✅
- Le migration di EF Core creerebbero colonne `varbinary(max)` ⚠️
- Se usi lo script SQL, **non eseguire le migration di EF Core**
- Se usi le migration di EF Core, **esegui ALTER manuale a VECTOR dopo la migration**
- Il codice dell'applicazione funziona con entrambi i tipi di colonna

## 🚀 Recommended Workflow - Flusso di Lavoro Raccomandato

### For New Installations - Per Nuove Installazioni:
```bash
# 1. Run SQL script directly
sqlcmd -S localhost -d DocNDb -i Database/CreateDatabase_Complete_V2.sql

# 2. Start application
dotnet run --project DocN.Client

# 3. Login with admin@docn.local / Admin@123
```

### For Existing Databases - Per Database Esistenti:
```sql
-- Check current column type
-- Verifica il tipo di colonna attuale
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Documents', 'DocumentChunks')
AND COLUMN_NAME IN ('EmbeddingVector', 'ChunkEmbedding');

-- If varbinary, convert to VECTOR
-- Se è varbinary, converti a VECTOR
ALTER TABLE Documents ALTER COLUMN EmbeddingVector VECTOR(1536) NULL;
ALTER TABLE DocumentChunks ALTER COLUMN ChunkEmbedding VECTOR(1536) NULL;
```

## 🔍 Troubleshooting - Risoluzione Problemi

### Error: NullReferenceException in RelationalTypeMappingSource

**Problem:** EF Core trying to map VECTOR type fails  
**Problema:** EF Core che prova a mappare il tipo VECTOR fallisce

**Solution:**  
**Soluzione:**

Use SQL script instead of EF Core migrations, or ensure EF Core configuration uses `varbinary(max)` as shown above.

Usa lo script SQL invece delle migration EF Core, o assicurati che la configurazione EF Core usi `varbinary(max)` come mostrato sopra.

### Error: ApplicationSeeder MigrateAsync fails

**Problem:** Trying to run `MigrateAsync()` with incompatible schema  
**Problema:** Tentativo di eseguire `MigrateAsync()` con schema incompatibile

**Solution:**  
**Soluzione:**

Comment out or remove the `MigrateAsync()` call in `ApplicationSeeder.SeedAsync()`:

Commenta o rimuovi la chiamata `MigrateAsync()` in `ApplicationSeeder.SeedAsync()`:

```csharp
public async Task SeedAsync()
{
    try
    {
        // Ensure database is created
        // await _context.Database.MigrateAsync(); // COMMENT THIS LINE
        
        // Seed default tenant
        var defaultTenant = await SeedDefaultTenantAsync();
        // ...
    }
}
```

Then use the SQL script to create the database structure.

Poi usa lo script SQL per creare la struttura del database.
