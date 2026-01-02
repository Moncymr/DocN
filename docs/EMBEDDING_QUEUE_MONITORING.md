# Monitoraggio Coda Embeddings

## Panoramica

Il sistema DocN elabora gli embeddings dei documenti in background per evitare rallentamenti durante il caricamento. Questa documentazione spiega come funziona il sistema e come monitorarlo.

## Come Funziona

### 1. Caricamento Documento
Quando carichi un documento:
1. Il file viene salvato immediatamente
2. Il testo viene estratto (con OCR se necessario)
3. Il documento viene marcato con `ChunkEmbeddingStatus = "Pending"`
4. L'utente riceve conferma istantanea

### 2. Elaborazione in Background
Il servizio `BatchEmbeddingProcessor` esegue ogni **30 secondi**:
1. Trova fino a 5 documenti con status "Pending"
2. Crea i chunks (pezzi di testo ottimali per RAG)
3. Genera embeddings per ogni chunk usando l'AI provider configurato
4. Salva i chunks con embeddings nel database
5. Aggiorna lo status a "Completed"

### 3. Tempi Tipici
- **PDF semplice (10-20 pagine)**: 2-5 minuti
- **Documento lungo**: 5-15 minuti
- **Batch di documenti**: elaborati 5 alla volta

## Monitoraggio

### Dashboard
Il dashboard ora mostra:
- 📊 **Documenti in Coda**: Quanti documenti aspettano elaborazione
- ⚙️ **In Elaborazione**: Documenti attualmente in elaborazione
- 📦 **Chunks da Processare**: Numero totale di chunks senza embeddings
- ⏱️ **Tempo Stimato**: Stima del tempo rimanente

### Pagina Documenti
Ogni documento mostra un badge:
- ⏳ **Embeddings in coda**: In attesa di elaborazione
- ⚙️ **Elaborazione...**: Attualmente in elaborazione
- ✓ **Pronto**: Embeddings completati, pronto per ricerca semantica

## Fix Implementati

### Bug Critico Risolto (2 Gennaio 2026)
**Problema**: Gli embeddings dei chunks venivano generati ma non salvati nel database.

**Causa**: Il metodo `GenerateChunkEmbeddingsForDocumentAsync` modificava i chunks in memoria ma non marcava esplicitamente le entità come modificate nel context di Entity Framework.

**Soluzione**: Aggiunto codice per marcare esplicitamente i chunks con embeddings come `EntityState.Modified` prima del `SaveChangesAsync`:

```csharp
// Ensure chunks are marked as modified so their embeddings are saved
foreach (var chunk in chunksWithoutEmbeddings.Where(c => c.ChunkEmbedding != null))
{
    _context.Entry(chunk).State = EntityState.Modified;
}
```

Questo garantisce che tutti gli embeddings generati vengano persistiti nel database.

## Verificare che Funzioni

### 1. Verifica Log
Controlla i log dell'applicazione per messaggi come:
```
Processing batch 1/3 (10 chunks) for document 123
Generated embeddings for 10/10 chunks of document 123 - Status: Completed
```

### 2. Verifica Database
Query SQL per verificare gli embeddings:
```sql
-- Documenti con embeddings in coda
SELECT FileName, ChunkEmbeddingStatus, UploadedAt
FROM Documents
WHERE ChunkEmbeddingStatus = 'Pending';

-- Chunks senza embeddings
SELECT d.FileName, COUNT(c.Id) AS ChunksWithoutEmbeddings
FROM Documents d
INNER JOIN DocumentChunks c ON d.Id = c.DocumentId
WHERE c.ChunkEmbedding768 IS NULL AND c.ChunkEmbedding1536 IS NULL
GROUP BY d.Id, d.FileName;

-- Stato generale
SELECT 
    d.FileName,
    d.ChunkEmbeddingStatus,
    COUNT(c.Id) AS TotalChunks,
    SUM(CASE WHEN c.ChunkEmbedding768 IS NOT NULL OR c.ChunkEmbedding1536 IS NOT NULL THEN 1 ELSE 0 END) AS ChunksWithEmbeddings
FROM Documents d
LEFT JOIN DocumentChunks c ON d.Id = c.DocumentId
GROUP BY d.Id, d.FileName, d.ChunkEmbeddingStatus;
```

### 3. Test Manuale
1. Carica un PDF di test
2. Osserva il badge "⏳ Embeddings in coda"
3. Aspetta 1-2 minuti
4. Aggiorna la pagina
5. Verifica che il badge diventi "✓ Pronto"
6. Prova una ricerca semantica sul documento

## Troubleshooting

### Gli embeddings non vengono mai completati
1. **Verifica che il BatchEmbeddingProcessor sia attivo**:
   - Controlla i log all'avvio: "Batch Embedding Processor started"
   - Cerca messaggi di processing ogni 30 secondi

2. **Verifica configurazione AI**:
   - Vai in `/config` e assicurati che un provider AI sia configurato e attivo
   - Testa la connessione con il pulsante "Test Configuration"

3. **Verifica permessi database**:
   - L'utente del database deve poter scrivere nella tabella DocumentChunks

### Processamento molto lento
1. **Rate limiting API**: Gemini/OpenAI hanno limiti di rate
2. **Documenti grandi**: Documenti con molte pagine generano molti chunks
3. **Concorrenza**: Di default elabora 1 chunk alla volta per evitare rate limits

### Chunks salvati ma senza embeddings
Se vedi chunks nel database ma senza embeddings:
1. Controlla errori nei log durante la generazione
2. Verifica che l'AI provider risponda correttamente
3. Il bug del salvataggio è stato risolto nella versione corrente

## Configurazione Avanzata

### Modificare Intervallo di Elaborazione
In `BatchEmbeddingProcessor.cs`:
```csharp
private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(30); // Modifica qui
```

### Modificare Batch Size
In `BatchEmbeddingProcessor.cs`:
```csharp
.Take(5) // Documenti per ciclo - aumenta per elaborazione più veloce
```

### Modificare Concorrenza
In `DocumentService.cs` metodo `GenerateChunkEmbeddingsAsync`:
```csharp
int maxConcurrency = 1 // Aumenta per più chunks in parallelo (attenzione ai rate limits!)
```

## Best Practices

1. **Non spegnere il server** durante l'elaborazione - gli embeddings in corso potrebbero essere persi
2. **Monitora i log** per identificare problemi tempestivamente
3. **Verifica lo spazio database** - gli embeddings occupano spazio (768 o 1536 floats per chunk)
4. **Configura alert** se il numero di documenti pending cresce troppo
5. **Usa il Dashboard** per monitorare la coda regolarmente

## Riferimenti

- Codice principale: `DocN.Data/Services/BatchEmbeddingProcessor.cs`
- Elaborazione chunks: `DocN.Data/Services/DocumentService.cs`
- UI Dashboard: `DocN.Client/Components/Pages/Dashboard.razor`
- Costanti status: `DocN.Data/Constants/DocumentStatusConstants.cs`
