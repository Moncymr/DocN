# DocN Database Configuration

## Impostazione Connessione Database

### 1. **File di Configurazione: `appsettings.json`**

La stringa di connessione al database si trova in `/src/DocN.Server/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=DocN;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  }
}
```

### 2. **Configurazione per SQL Server 2025**

Modifica la sezione `ConnectionStrings` in `appsettings.json`:

**Autenticazione Windows:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=DocN;Integrated Security=True;TrustServerCertificate=True;"
}
```

**Autenticazione SQL Server:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=DocN;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;"
}
```

**Azure SQL Database:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=tcp:yourserver.database.windows.net,1433;Database=DocN;User ID=yourusername;Password=yourpassword;Encrypt=True;"
}
```

### 3. **Aggiornare Program.cs**

Il file `/src/DocN.Server/Program.cs` deve essere modificato per usare SQL Server invece di SQLite:

**Cambiare da:**
```csharp
options.UseSqlite(connectionString);
```

**A:**
```csharp
options.UseSqlServer(connectionString, sqlOptions => 
{
    sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(30),
        errorNumbersToAdd: null);
});
```

## Script Database SQL Server 2025

### Creazione Database

Esegui lo script completo: `SqlServer2025_Schema.sql`

```bash
sqlcmd -S localhost -U sa -P YourPassword -i database/SqlServer2025_Schema.sql
```

O tramite SQL Server Management Studio (SSMS):
1. Apri SSMS e connettiti al server
2. Apri il file `SqlServer2025_Schema.sql`
3. Esegui lo script (F5)

### Caratteristiche dello Schema

#### Tabelle Principali

1. **Categories** - Categorie gerarchiche per documenti
2. **Documents** - Documenti con embeddings vettoriali (VECTOR(1536))
3. **DocumentChunks** - Chunks con embeddings per ricerca granulare
4. **Conversations** - Cronologia conversazioni RAG
5. **Messages** - Messaggi con riferimenti a documenti
6. **AuditLogs** - Log di audit per compliance

#### Supporto VECTOR (SQL Server 2025)

Lo schema utilizza il tipo `VECTOR(1536)` per:
- **Document.Embedding**: Embedding dell'intero documento (OpenAI text-embedding-ada-002 / Azure OpenAI)
- **DocumentChunk.Embedding**: Embedding di ogni chunk per ricerca precisa

#### Stored Procedures

1. **sp_HybridDocumentSearch** - Ricerca ibrida con Reciprocal Rank Fusion
   - Vector search (cosine similarity)
   - Full-text search
   - Combina risultati con RRF algorithm

2. **sp_FindSimilarDocuments** - Trova documenti simili
   - Basato su similarity vettoriale
   - Esclude il documento sorgente

3. **sp_SemanticChunkSearch** - Ricerca semantica a livello di chunk
   - Precision retrieval per RAG
   - Restituisce chunks più rilevanti

#### Indici

- **Full-text indexes** su Documents e DocumentChunks
- **Vector indexes** per performance (commentati, da abilitare in produzione)
- **B-tree indexes** su foreign keys e colonne di ricerca

## Migrazione da SQLite a SQL Server

### Step 1: Aggiornare Package References

In `/src/DocN.Data/DocN.Data.csproj`, assicurati che sia presente:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
```

### Step 2: Modificare Program.cs

```csharp
// Prima (SQLite):
builder.Services.AddDbContext<DocNDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=docn.db";
    options.UseSqlite(connectionString);
});

// Dopo (SQL Server 2025):
builder.Services.AddDbContext<DocNDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    
    options.UseSqlServer(connectionString, sqlOptions => 
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(120); // 2 minuti timeout per operazioni vettoriali
    });
});
```

### Step 3: Creare il Database

```bash
# Opzione 1: Eseguire lo script SQL
sqlcmd -S localhost -U sa -P YourPassword -i database/SqlServer2025_Schema.sql

# Opzione 2: Usare EF Core Migrations (se configurato)
dotnet ef database update --project src/DocN.Data --startup-project src/DocN.Server
```

### Step 3b: Migrazione da VECTOR(768) a VECTOR(1536) (se necessario)

Se hai già un database esistente con `VECTOR(768)`, usa lo script di migrazione:

```bash
# Eseguire lo script di aggiornamento
sqlcmd -S localhost -U sa -P YourPassword -i database/Update_Vector_768_to_1536.sql
```

**⚠️ IMPORTANTE:**
- Lo script cancella tutti gli embeddings esistenti
- Dopo la migrazione, dovrai ri-processare i documenti per rigenerare gli embeddings
- Fai un backup del database prima di eseguire la migrazione

```sql
-- Backup del database prima della migrazione
BACKUP DATABASE [DocN] TO DISK = N'C:\Backup\DocN_Before_Vector_Migration.bak' WITH INIT;
```

### Step 4: Verificare la Connessione

```bash
cd src/DocN.Server
dotnet run
```

Controlla i log per confermare la connessione al database.

## Esempi di Utilizzo

### Ricerca Ibrida

```sql
DECLARE @QueryVector VECTOR(1536) = ... -- Vector from embedding service

EXEC [dbo].[sp_HybridDocumentSearch]
    @QueryEmbedding = @QueryVector,
    @QueryText = N'contratti di vendita',
    @CategoryId = 1, -- Contratti
    @TopN = 10,
    @MinSimilarity = 0.7,
    @UseVectorSearch = 1,
    @UseFullTextSearch = 1;
```

### Trova Documenti Simili

```sql
EXEC [dbo].[sp_FindSimilarDocuments]
    @DocumentId = 42,
    @TopN = 5,
    @MinSimilarity = 0.75;
```

### Ricerca Chunk Semantica (per RAG)

```sql
DECLARE @QueryVector VECTOR(1536) = ... -- Vector della query utente

EXEC [dbo].[sp_SemanticChunkSearch]
    @QueryEmbedding = @QueryVector,
    @CategoryId = NULL, -- Tutte le categorie
    @TopN = 20,
    @MinSimilarity = 0.7;
```

### Statistiche Dashboard

```sql
SELECT * FROM [dbo].[fn_GetDocumentStatistics]();
```

## Performance Tips

1. **Indexes Vettoriali**: In produzione, abilita gli indici vettoriali commentati nello script
2. **Full-Text Catalog**: Pianifica rebuild periodici per mantenere performance
3. **Partition**: Considera partizionamento per tabelle con milioni di documenti
4. **Memory**: Assicura memoria sufficiente per operazioni vettoriali (almeno 16GB RAM)
5. **Connection Pooling**: Già abilitato di default in EF Core

## Backup e Manutenzione

```sql
-- Backup completo
BACKUP DATABASE [DocN] TO DISK = N'C:\Backup\DocN_Full.bak' WITH INIT;

-- Rebuild Full-Text Index
ALTER FULLTEXT CATALOG [DocNFullTextCatalog] REBUILD;

-- Update Statistics
EXEC sp_updatestats;

-- Check Vector Index Health (se abilitato)
-- DBCC SHOW_STATISTICS('Documents', 'IX_Documents_Embedding_Vector');
```

## Troubleshooting

### Problema: VECTOR type non riconosciuto
**Soluzione**: Assicurati di usare SQL Server 2025 o versione superiore

### Problema: Timeout durante ricerca vettoriale
**Soluzione**: Aumenta CommandTimeout in Program.cs e ottimizza query

### Problema: Full-text search non funziona
**Soluzione**: Verifica che il Full-Text Service sia installato e avviato

```sql
-- Verifica Full-Text Service
SELECT SERVERPROPERTY('IsFullTextInstalled');
```

### Problema: Connessione fallita
**Soluzione**: Controlla:
- SQL Server è in esecuzione
- Porta 1433 è aperta
- Credenziali sono corrette
- TrustServerCertificate=True per certificati self-signed

## Security Best Practices

1. **Mai mettere password in appsettings.json** - Usa User Secrets o Azure Key Vault
2. **Usa sempre SSL/TLS** per connessioni di produzione
3. **Limita permessi** - Crea un utente database specifico con privilegi minimi
4. **Enable auditing** - Usa la tabella AuditLogs per tracciare accessi

```bash
# Setup User Secrets (development)
cd src/DocN.Server
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=DocN;..."
```
