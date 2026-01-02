# Diagnostica: Embeddings Bloccati in Coda

## Query SQL per Diagnosticare il Problema

Esegui queste query per capire cosa sta succedendo:

### 1. Verifica Configurazione AI Attiva
```sql
-- Controlla se esiste una configurazione AI attiva
SELECT 
    Id,
    Name, 
    ProviderType,
    IsActive,
    ChatProvider,
    EmbeddingsProvider,
    ApiKey = CASE WHEN ApiKey IS NOT NULL THEN '***configured***' ELSE 'NULL' END
FROM AIConfigurations
WHERE IsActive = 1;
```

**Cosa cercare**: Deve esserci ALMENO UNA riga con `IsActive = 1` e `ApiKey` configurata.

Se non c'è NESSUNA configurazione attiva:
1. Vai su `https://localhost:7114/config` (o il tuo URL del client)
2. Configura un provider AI (Gemini, OpenAI, o Azure OpenAI)
3. Inserisci una API key valida
4. Attiva la configurazione (toggle "Active")

### 2. Verifica Documenti in Pending
```sql
-- Controlla documenti in stato Pending
SELECT 
    Id,
    FileName,
    ChunkEmbeddingStatus,
    UploadedAt,
    DATEDIFF(MINUTE, UploadedAt, GETUTCDATE()) AS MinutiInCoda,
    LEN(ExtractedText) AS TextLength
FROM Documents
WHERE ChunkEmbeddingStatus = 'Pending'
ORDER BY UploadedAt DESC;
```

### 3. Verifica Chunks Creati
```sql
-- Controlla se i chunks sono stati creati per i documenti pending
SELECT 
    d.Id AS DocumentId,
    d.FileName,
    d.ChunkEmbeddingStatus,
    COUNT(c.Id) AS NumeroChunks,
    SUM(CASE WHEN c.ChunkEmbedding768 IS NOT NULL OR c.ChunkEmbedding1536 IS NOT NULL 
        THEN 1 ELSE 0 END) AS ChunksConEmbeddings
FROM Documents d
LEFT JOIN DocumentChunks c ON d.Id = c.DocumentId
WHERE d.ChunkEmbeddingStatus IN ('Pending', 'Processing')
GROUP BY d.Id, d.FileName, d.ChunkEmbeddingStatus;
```

**Cosa cercare**:
- Se `NumeroChunks = 0`: I chunks non sono ancora stati creati
- Se `NumeroChunks > 0` e `ChunksConEmbeddings = 0`: I chunks esistono ma nessun embedding è stato generato (problema con AI service)
- Se `ChunksConEmbeddings > 0` ma `< NumeroChunks`: Elaborazione in corso

### 4. Controlla Log Recenti
```sql
-- Controlla i log per errori relativi agli embeddings
SELECT TOP 50
    Timestamp,
    Level,
    Category,
    Message,
    Details
FROM LogEntries
WHERE Category LIKE '%Embedding%' 
   OR Category LIKE '%Batch%'
   OR Message LIKE '%embedding%'
   OR Message LIKE '%AI service%'
ORDER BY Timestamp DESC;
```

## Scenari Comuni e Soluzioni

### Scenario 1: Nessuna Configurazione AI Attiva
**Sintomo**: Query #1 non restituisce righe

**Soluzione**:
1. Vai su `/config` nell'interfaccia web
2. Configura un provider AI
3. Inserisci API key valida
4. Attiva la configurazione

### Scenario 2: Chunks Non Creati
**Sintomo**: Query #3 mostra `NumeroChunks = 0`

**Possibili cause**:
- ExtractedText vuoto o nullo
- ChunkingService non disponibile
- Errore durante creazione chunks

**Soluzione**:
```sql
-- Forza ricreazione chunks
UPDATE Documents 
SET ChunkEmbeddingStatus = 'Pending' 
WHERE Id = [TUO_DOCUMENT_ID];
```

### Scenario 3: AI Service Non Risponde
**Sintomo**: Query #3 mostra chunks creati ma nessun embedding

**Possibili cause**:
- API key non valida
- Rate limiting del provider
- Network issues
- Timeout

**Soluzione**:
1. Testa la configurazione AI in `/config`
2. Controlla i log per errori specifici
3. Verifica la connettività internet del server
4. Controlla rate limits del provider

### Scenario 4: BatchEmbeddingProcessor Non Attivo
**Sintomo**: Nessun log nel query #4 negli ultimi 5 minuti

**Soluzione**:
1. Riavvia l'applicazione server (DocN.Server)
2. Controlla i log all'avvio per: "Batch Embedding Processor started"
3. Verifica che non ci siano errori durante l'avvio

## Forza Rielaborazione Documento

Se un documento è bloccato, puoi forzare la rielaborazione:

```sql
-- Resetta lo stato a Pending per forzare rielaborazione
UPDATE Documents 
SET ChunkEmbeddingStatus = 'Pending' 
WHERE Id = [ID_DEL_DOCUMENTO];

-- Opzionale: elimina chunks esistenti per ricrearli da zero
DELETE FROM DocumentChunks 
WHERE DocumentId = [ID_DEL_DOCUMENTO];
```

## Verifica Manuale Funzionamento

Per verificare che il sistema funzioni:

1. Configura AI in `/config` e testa
2. Carica un documento di test (piccolo PDF)
3. Esegui query #2 ogni 30 secondi
4. Dovresti vedere `ChunkEmbeddingStatus` cambiare:
   - `Pending` → `Processing` → `Completed`

Tempo atteso: 2-5 minuti per PDF semplice

## Contatti per Supporto

Se il problema persiste dopo questi controlli:
1. Esporta i risultati delle 4 query SQL
2. Copia gli ultimi 100 log dall'applicazione
3. Segnala il problema con queste informazioni
