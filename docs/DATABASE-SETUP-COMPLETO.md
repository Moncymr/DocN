# DocN - Setup Database Completo

## Opzione 1: Migrazione Automatica (Consigliato)

### Passo 1: Elimina il Database Esistente (se necessario)
```sql
-- In SQL Server Management Studio o Azure Data Studio
USE master;
GO

DROP DATABASE IF EXISTS DocNDb;
GO

CREATE DATABASE DocNDb;
GO
```

### Passo 2: Aggiorna la Connection String
Modifica `appsettings.json` in `DocN.Server`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### Passo 3: Esegui le Migrazioni
```powershell
# Dalla directory principale del progetto
cd C:\GestDoc

# Metodo 1: Usando DocN.Server come startup project (CONSIGLIATO)
dotnet ef database update --project DocN.Data\DocN.Data.csproj --startup-project DocN.Server\DocN.Server.csproj --context ApplicationDbContext

# Metodo 2: Se hai problemi con il primo metodo
cd DocN.Server
dotnet ef database update --project ..\DocN.Data\DocN.Data.csproj --context ApplicationDbContext
```

### Passo 4: Verifica Database Creato
```sql
USE DocNDb;
GO

-- Verifica tabelle create
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Dovresti vedere queste tabelle:
-- AIConfigurations
-- AspNetRoles
-- AspNetRoleClaims
-- AspNetUsers
-- AspNetUserClaims
-- AspNetUserLogins
-- AspNetUserRoles
-- AspNetUserTokens
-- DocumentChunks         ← NUOVA (per semantic search)
-- Documents
-- DocumentTags
-- LogEntries
-- SimilarDocuments
-- Tenants
-- __EFMigrationsHistory
```

---

## Opzione 2: Script SQL Completo (Alternativo)

Se preferisci eseguire lo script SQL manualmente:

### Passo 1: Genera lo Script SQL
```powershell
cd C:\GestDoc

# Genera script SQL idempotente (può essere eseguito più volte)
dotnet ef migrations script --project DocN.Data\DocN.Data.csproj --startup-project DocN.Server\DocN.Server.csproj --context ApplicationDbContext --output database-setup.sql --idempotent
```

### Passo 2: Esegui lo Script in SQL Server
1. Apri SQL Server Management Studio
2. Connettiti al server
3. Apri il file `database-setup.sql` generato
4. Esegui lo script

---

## Struttura Database Principale

### Tabella: Documents
```sql
CREATE TABLE [Documents] (
    [Id] int NOT NULL IDENTITY,
    [FileName] nvarchar(255) NOT NULL,
    [FilePath] nvarchar(500) NOT NULL,
    [FileSize] bigint NOT NULL,
    [ContentType] nvarchar(max) NOT NULL,
    [UploadedAt] datetime2 NOT NULL,
    [OwnerId] nvarchar(450) NULL,
    [TenantId] int NULL,
    [ExtractedText] nvarchar(max) NULL,
    [ProcessingStatus] nvarchar(max) NULL,
    [ProcessingError] nvarchar(max) NULL,
    [Notes] nvarchar(max) NULL,
    [Visibility] int NOT NULL,
    [AccessCount] int NOT NULL,
    [LastAccessedAt] datetime2 NULL,
    
    -- Metadata AI
    [DetectedLanguage] nvarchar(max) NULL,
    [SuggestedCategory] nvarchar(450) NULL,
    [ActualCategory] nvarchar(450) NULL,
    [CategoryReasoning] nvarchar(2000) NULL,
    [AITagsJson] nvarchar(max) NULL,
    [AIAnalysisDate] datetime2 NULL,
    [ExtractedMetadataJson] nvarchar(max) NULL,
    [PageCount] int NULL,
    
    -- Vector Embeddings (768 dim per Gemini, 1536 per OpenAI)
    [EmbeddingVector768] varbinary(max) NULL,
    [EmbeddingVector1536] varbinary(max) NULL,
    [EmbeddingDimension] int NULL,
    
    -- NUOVO: Stato elaborazione chunks
    [ChunkEmbeddingStatus] nvarchar(50) NULL,
    
    CONSTRAINT [PK_Documents] PRIMARY KEY ([Id])
);
```

### Tabella: DocumentChunks (NUOVA)
```sql
CREATE TABLE [DocumentChunks] (
    [Id] int NOT NULL IDENTITY,
    [DocumentId] int NOT NULL,
    [ChunkIndex] int NOT NULL,
    [ChunkText] nvarchar(max) NOT NULL,
    [StartPosition] int NOT NULL,
    [EndPosition] int NOT NULL,
    [TokenCount] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    
    -- Vector Embeddings per chunk
    [ChunkEmbedding768] varbinary(max) NULL,
    [ChunkEmbedding1536] varbinary(max) NULL,
    [EmbeddingDimension] int NULL,
    
    CONSTRAINT [PK_DocumentChunks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DocumentChunks_Documents_DocumentId] FOREIGN KEY ([DocumentId]) 
        REFERENCES [Documents] ([Id]) ON DELETE CASCADE
);
```

### Tabella: AIConfigurations
```sql
CREATE TABLE [AIConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [ConfigurationName] nvarchar(100) NOT NULL,
    
    -- Provider Settings
    [ProviderType] nvarchar(50) NULL,
    [ChatProvider] nvarchar(50) NULL,
    [EmbeddingsProvider] nvarchar(50) NULL,
    [RAGProvider] nvarchar(50) NULL,
    [TagExtractionProvider] nvarchar(50) NULL,
    
    -- OpenAI
    [OpenAIApiKey] nvarchar(500) NULL,
    [OpenAIChatModel] nvarchar(max) NULL,
    [OpenAIEmbeddingModel] nvarchar(max) NULL,
    
    -- Azure OpenAI
    [AzureOpenAIEndpoint] nvarchar(max) NULL,
    [AzureOpenAIKey] nvarchar(500) NULL,
    [AzureOpenAIChatModel] nvarchar(max) NULL,
    [AzureOpenAIEmbeddingModel] nvarchar(max) NULL,
    
    -- Gemini
    [GeminiApiKey] nvarchar(500) NULL,
    [GeminiChatModel] nvarchar(max) NULL,
    [GeminiEmbeddingModel] nvarchar(max) NULL,
    
    -- RAG Settings
    [MaxDocumentsToRetrieve] int NOT NULL,
    [SimilarityThreshold] float NOT NULL,
    [MaxTokensForContext] int NOT NULL,
    [SystemPrompt] nvarchar(2000) NULL,
    
    -- Chunking Settings (NUOVO)
    [EnableChunking] bit NOT NULL DEFAULT 1,
    [ChunkSize] int NOT NULL DEFAULT 1000,
    [ChunkOverlap] int NOT NULL DEFAULT 200,
    
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    
    CONSTRAINT [PK_AIConfigurations] PRIMARY KEY ([Id])
);
```

---

## Dati Iniziali (Seed Data)

Le seguenti entità vengono create automaticamente all'avvio:

### Tenant Default
```sql
INSERT INTO Tenants (Name, Description, IsActive, CreatedAt)
VALUES ('Default', 'Default tenant for the application', 1, GETDATE());
```

### Ruoli
```sql
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
VALUES 
    (NEWID(), 'Admin', 'ADMIN', NEWID()),
    (NEWID(), 'User', 'USER', NEWID()),
    (NEWID(), 'Manager', 'MANAGER', NEWID());
```

### Utente Admin
```sql
-- Email: admin@docn.local
-- Password: Admin123!
-- (creato automaticamente dal ApplicationSeeder)
```

---

## Costanti ChunkEmbeddingStatus

```csharp
public static class ChunkEmbeddingStatus
{
    public const string Pending = "Pending";         // In attesa di elaborazione background
    public const string Processing = "Processing";   // In elaborazione
    public const string Completed = "Completed";     // Completato
    public const string NotRequired = "NotRequired"; // Non richiesto (no text)
}
```

---

## Verifiche Post-Installazione

### 1. Verifica Tabelle Create
```sql
USE DocNDb;
GO

SELECT COUNT(*) AS TotalTables
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';
-- Dovrebbe restituire circa 15-16 tabelle
```

### 2. Verifica Migrazioni Applicate
```sql
SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId;
-- Dovrebbe mostrare tutte le migrazioni inclusa 'AddChunkEmbeddingStatus'
```

### 3. Verifica Colonne Vector
```sql
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLUMN_NAME LIKE '%Embedding%'
ORDER BY TABLE_NAME, COLUMN_NAME;
```

### 4. Verifica Indici
```sql
SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('Documents', 'DocumentChunks')
ORDER BY t.name, i.name;
```

---

## Troubleshooting

### Errore: "An item with the same key has already been added"
**Causa:** ApplicationDbContext registrato più volte

**Soluzione:**
```powershell
# Specifica esplicitamente il context
dotnet ef database update --project DocN.Data\DocN.Data.csproj --startup-project DocN.Server\DocN.Server.csproj --context ApplicationDbContext
```

### Errore: "Assets file not found"
**Soluzione:**
```powershell
dotnet restore
dotnet build
dotnet ef database update --project DocN.Data\DocN.Data.csproj --startup-project DocN.Server\DocN.Server.csproj
```

### Warning: NU1608 (OpenAI version mismatch)
**Nota:** Questi sono solo warning e non bloccano la migrazione. Sono relativi alle dipendenze di SemanticKernel.

---

## Backup e Restore

### Backup Database
```sql
BACKUP DATABASE DocNDb 
TO DISK = 'C:\Backup\DocNDb_Backup.bak'
WITH FORMAT, INIT, NAME = 'Full Backup of DocNDb';
```

### Restore Database
```sql
USE master;
GO

RESTORE DATABASE DocNDb
FROM DISK = 'C:\Backup\DocNDb_Backup.bak'
WITH REPLACE;
```

---

## Prossimi Passi

Dopo aver creato il database:

1. **Avvia l'applicazione**:
   ```powershell
   cd C:\GestDoc\DocN.Server
   dotnet run
   ```

2. **Login con admin**:
   - Email: `admin@docn.local`
   - Password: `Admin123!`

3. **Configura AI Provider** (Settings → AI Configuration):
   - Gemini API Key
   - Scegli Gemini come EmbeddingsProvider
   - Salva configurazione

4. **Upload documento di test**:
   - Vai su Upload
   - Scegli modalità (veloce/completo)
   - Carica testdocupdat.pdf

5. **Verifica chunks creati**:
   ```sql
   SELECT d.FileName, d.ChunkEmbeddingStatus, COUNT(c.Id) AS ChunkCount
   FROM Documents d
   LEFT JOIN DocumentChunks c ON d.Id = c.DocumentId
   GROUP BY d.Id, d.FileName, d.ChunkEmbeddingStatus;
   ```

6. **Test ricerca**:
   - Cerca "Rossi"
   - Dovrebbe trovare risultati dal testdocupdat.pdf

---

## Note Importanti

- **Vector Storage**: Gli embeddings sono memorizzati come `varbinary(max)` in formato ottimizzato
- **Chunking**: Default 1000 caratteri per chunk con 200 caratteri di overlap
- **Background Processing**: `BatchEmbeddingProcessor` gira ogni 30 secondi
- **Performance**: Modalità veloce (default) rende upload istantanei
- **Scalabilità**: Supporta migliaia di documenti e chunks
