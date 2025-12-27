# VECTOR Type Support Guide - Guida Supporto Tipo VECTOR

## 🔧 Issue: EF Core and SQL Server 2025 VECTOR Type

Entity Framework Core **does not natively support** the SQL Server 2025 `VECTOR` type yet. This causes a `NullReferenceException` during model finalization.

Entity Framework Core **non supporta nativamente** il tipo `VECTOR` di SQL Server 2025. Questo causa una `NullReferenceException` durante la finalizzazione del modello.

## 🆕 Recent Fix - Correzione Recente

**December 2025 Update**: Fixed critical type mismatch that caused `SqlException: varbinary incompatible with vector` error.

**Aggiornamento Dicembre 2025**: Corretto un grave errore di tipo che causava l'errore `SqlException: varbinary incompatibile con vector`.

**The Problem / Il Problema:**
- ApplicationDbContext was using `varbinary(max)` with binary serialization (`float[]` → `byte[]`)
- SQL Server VECTOR type expects JSON array format: `[0.1, 0.2, 0.3, ...]`
- When saving embeddings, binary data was sent to VECTOR columns, causing type conflict
- ApplicationDbContext usava `varbinary(max)` con serializzazione binaria (`float[]` → `byte[]`)
- Il tipo VECTOR di SQL Server si aspetta formato JSON array: `[0.1, 0.2, 0.3, ...]`
- Durante il salvataggio degli embedding, i dati binari venivano inviati alle colonne VECTOR, causando conflitto di tipo

**The Fix / La Correzione:**
- Changed value converter to use JSON serialization (`float[]` → JSON string)
- Updated ApplicationDbContext to use `nvarchar(max)` which is compatible with VECTOR type
- Migration automatically handles conversion from varbinary to nvarchar if needed
- Cambiato il convertitore di valori per usare serializzazione JSON (`float[]` → stringa JSON)
- Aggiornato ApplicationDbContext per usare `nvarchar(max)` che è compatibile con il tipo VECTOR
- La migration gestisce automaticamente la conversione da varbinary a nvarchar se necessario

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
// Uses nvarchar(max) with JSON serialization for VECTOR compatibility
// Usa nvarchar(max) con serializzazione JSON per compatibilità con VECTOR
var vectorConverter = new ValueConverter<float[]?, string?>(
    v => v == null ? null : System.Text.Json.JsonSerializer.Serialize(v),
    v => v == null ? null : System.Text.Json.JsonSerializer.Deserialize<float[]>(v) ?? Array.Empty<float>()
);

entity.Property(e => e.EmbeddingVector)
    .HasColumnType("nvarchar(max)")  
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

The application uses `System.Text.Json` to convert between `float[]` (C#) and JSON string (storage):

L'applicazione usa `System.Text.Json` per convertire tra `float[]` (C#) e stringa JSON (storage):

```csharp
// float[] -> JSON string
string json = System.Text.Json.JsonSerializer.Serialize(floats);
// Example: [0.1, 0.2, 0.3, ...]

// JSON string -> float[]
float[] floats = System.Text.Json.JsonSerializer.Deserialize<float[]>(json) ?? Array.Empty<float>();
```

This works correctly with both `nvarchar(max)` and `VECTOR(1536)` since SQL Server VECTOR type accepts JSON array format.

Questo funziona correttamente sia con `nvarchar(max)` che con `VECTOR(1536)` poiché il tipo VECTOR di SQL Server accetta formato JSON array.

## ⚠️ Important Notes - Note Importanti

**English:**
- The SQL script creates `VECTOR(1536)` columns ✅
- EF Core migrations create `nvarchar(max)` columns with JSON serialization ✅
- Both approaches work because VECTOR accepts JSON array format
- The application code works with both column types
- JSON format: `[0.1, 0.2, 0.3, ...]` is compatible with VECTOR type

**Italiano:**
- Lo script SQL crea colonne `VECTOR(1536)` ✅
- Le migration di EF Core creano colonne `nvarchar(max)` con serializzazione JSON ✅
- Entrambi gli approcci funzionano perché VECTOR accetta formato JSON array
- Il codice dell'applicazione funziona con entrambi i tipi di colonna
- Formato JSON: `[0.1, 0.2, 0.3, ...]` è compatibile con il tipo VECTOR

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
