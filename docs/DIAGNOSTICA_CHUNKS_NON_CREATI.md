# Diagnostica: Chunks Non Vengono Creati

## Problema Confermato

✅ **Configurazione AI funziona** (puoi creare embeddings manualmente)  
❌ **I chunks NON vengono creati** per i documenti in coda

## Cosa Controllare Nei Log

Dopo il commit `dcd0943`, i log sono molto più dettagliati. Cerca questi pattern:

### 1. ChunkingService Disponibile?

```log
[ERROR] ChunkingService not available - chunks cannot be created! Check service registration in Program.cs
```

**Se vedi questo errore:**
- Il ChunkingService non è registrato correttamente
- Verifica in `DocN.Server/Program.cs` la riga:
  ```csharp
  builder.Services.AddScoped<IChunkingService, ChunkingService>();
  ```

### 2. Documenti Trovati?

```log
[INFO] Found 4 documents needing chunk creation
```

**Se NON vedi questo:**
- I documenti non hanno `ChunkEmbeddingStatus = 'Pending'`
- Oppure `ExtractedText` è vuoto/null

**Controlla con SQL:**
```sql
SELECT Id, FileName, ChunkEmbeddingStatus,
       LEN(ISNULL(ExtractedText, '')) AS TextLength
FROM Documents
WHERE ChunkEmbeddingStatus = 'Pending';
```

### 3. ExtractedText Presente?

```log
[INFO] Creating chunks for document 123: esempio.pdf (ExtractedText length: 5420)
```

**Se vedi `length: 0`:**
- Il documento non ha testo estratto
- Possibile causa: PDF è un'immagine senza OCR
- Oppure estrazione testo fallita

### 4. Chunks Creati dal ChunkingService?

```log
[INFO] ChunkingService returned 15 chunks for document 123
```

**Se vedi `returned 0 chunks`:**
- Il ChunkingService non riesce a creare chunks dal testo
- Possibile causa: testo troppo corto
- Oppure problema nel ChunkingService stesso

### 5. Chunks Salvati?

```log
[INFO] Created 15 chunks for document 123, will generate embeddings next
```

**Se vedi questo:**
- ✅ Chunks creati con successo!
- Il prossimo ciclo dovrebbe generare gli embeddings

## Breakpoint Strategici (Visual Studio)

### File: BatchEmbeddingProcessor.cs

**1. Riga 161:** Dopo `GetService<IChunkingService>()`
```csharp
var chunkingService = scope.ServiceProvider.GetService<IChunkingService>();
// ⬅️ BREAKPOINT QUI - Guarda se chunkingService è null
```

**2. Riga 177:** Dopo query documenti
```csharp
if (documentsNeedingChunks.Any())
// ⬅️ BREAKPOINT QUI - Verifica quanti documenti trovati
```

**3. Riga 196:** Prima di chiamare ChunkDocument
```csharp
var chunks = chunkingService.ChunkDocument(document);
// ⬅️ BREAKPOINT QUI - Step Into per entrare nel metodo
```

**4. Riga 203:** Prima del SaveChanges
```csharp
context.DocumentChunks.AddRange(chunks);
// ⬅️ BREAKPOINT QUI - Verifica chunks.Count > 0
```

### File: ChunkingService.cs

**Riga 70-80:** Metodo ChunkDocument
```csharp
public List<DocumentChunk> ChunkDocument(Document document)
{
    // ⬅️ BREAKPOINT ALL'INIZIO
    if (string.IsNullOrWhiteSpace(document.ExtractedText))
        return new List<DocumentChunk>(); // ⬅️ Guarda se entra qui
    
    // ⬅️ BREAKPOINT PRIMA DEL RETURN per vedere chunks.Count
    return chunks;
}
```

## Scenari Possibili

### Scenario A: ChunkingService è Null
**Sintomo:** Log mostra "ChunkingService not available"

**Soluzione:**
1. Verifica registrazione in Program.cs
2. Riavvia DocN.Server
3. Se persiste, controlla namespace imports

### Scenario B: ExtractedText Vuoto
**Sintomo:** Log mostra "length: 0"

**Soluzione:**
```sql
-- Controlla se il testo è presente
SELECT Id, FileName, 
       LEN(ISNULL(ExtractedText, '')) AS TextLength,
       SUBSTRING(ExtractedText, 1, 100) AS TextPreview
FROM Documents
WHERE Id = [TUO_DOCUMENT_ID];
```

Se TextLength = 0:
- Il documento non è stato processato correttamente all'upload
- Ricarica il documento

### Scenario C: ChunkingService Ritorna 0 Chunks
**Sintomo:** Log mostra "returned 0 chunks"

**Cause possibili:**
1. Testo troppo corto (< 100 caratteri)
2. Bug nel ChunkingService
3. Parametri di chunking troppo restrittivi

**Debug:**
- Metti breakpoint in ChunkingService.ChunkDocument()
- Verifica la logica di split del testo
- Controlla parametri ChunkSize e Overlap

### Scenario D: Chunks Creati Ma Non Salvati
**Sintomo:** Log mostra "ChunkingService returned X" ma non "Created X chunks"

**Causa:** Eccezione durante SaveChanges

**Soluzione:**
- Controlla log per errori SQL
- Verifica permessi database
- Controlla constraint violations

## Test Manuale Rapido

### Passo 1: Verifica Servizi
```csharp
// In Program.cs, aggiungi temporary logging dopo registration:
var serviceProvider = builder.Build().Services;
var chunkService = serviceProvider.GetService<IChunkingService>();
Console.WriteLine($"ChunkingService: {chunkService != null}");
```

### Passo 2: Test Diretto
```csharp
// Crea un test endpoint temporaneo:
app.MapGet("/test-chunking", async (ApplicationDbContext db, IChunkingService chunking) =>
{
    var doc = await db.Documents
        .Where(d => d.ChunkEmbeddingStatus == "Pending")
        .FirstOrDefaultAsync();
    
    if (doc == null) return "No pending documents";
    
    var chunks = chunking.ChunkDocument(doc);
    return $"Created {chunks.Count} chunks from text length {doc.ExtractedText?.Length ?? 0}";
});
```

Chiama: `GET https://localhost:5211/test-chunking`

## Prossimi Passi

1. **Riavvia DocN.Server** con i nuovi log
2. **Aspetta 30 secondi** (o forza con nuovo upload)
3. **Copia tutti i log** relativi a "chunk"
4. **Identifica** quale scenario corrisponde ai tuoi log
5. **Applica** la soluzione specifica

---

**Commit con logging migliorato:** `dcd0943`  
**Data:** 2 Gennaio 2026  
**Priorità:** 🔴 CRITICA - Chunks non vengono creati
