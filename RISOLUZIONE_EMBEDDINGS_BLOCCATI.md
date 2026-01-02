# Risoluzione Problema: Embeddings Bloccati in Coda

## Situazione Attuale

**Dashboard mostra:**
- ⏳ 4 Documenti in Coda
- 📦 0 Chunks da Processare  
- ⏱️ ~4 min Tempo Stimato

**Diagnosi:** I documenti sono bloccati in stato "Pending" perché i chunks non sono mai stati creati. Dopo 5+ minuti, questo non è normale.

## Causa Root Probabile

Il **BatchEmbeddingProcessor** non sta elaborando i documenti per una di queste ragioni:

### 1. ❌ Configurazione AI Mancante (PIÙ PROBABILE)
Il sistema richiede una configurazione AI attiva nel database per generare embeddings.

**Come verificare:**
```sql
SELECT Id, Name, ProviderType, IsActive, 
       CASE WHEN ApiKey IS NOT NULL THEN '***SET***' ELSE 'NULL' END AS ApiKey
FROM AIConfigurations 
WHERE IsActive = 1;
```

**Cosa aspettarsi:**
- ✅ Almeno 1 riga con `IsActive = 1` e `ApiKey = '***SET***'`
- ❌ Nessuna riga = **QUESTO È IL PROBLEMA**

**Soluzione se manca:**
1. Apri browser: `https://localhost:7114/config` (o il tuo URL)
2. Clicca "Add New Configuration"
3. Seleziona provider (es. Gemini)
4. Inserisci API Key valida
5. Attiva toggle "Active" ✓
6. Salva

### 2. ❌ BatchEmbeddingProcessor Non Avviato
Il servizio background potrebbe non essere partito.

**Come verificare:**
Controlla i log del DocN.Server all'avvio per:
```
Batch Embedding Processor started
```

**Soluzione:**
Riavvia `DocN.Server` (il backend API sulla porta 5211)

### 3. ❌ Documenti Senza Testo
I documenti potrebbero non avere `ExtractedText`.

**Come verificare:**
```sql
SELECT Id, FileName, ChunkEmbeddingStatus, 
       CASE WHEN ExtractedText IS NULL OR ExtractedText = '' 
            THEN 'VUOTO' ELSE 'OK' END AS HasText
FROM Documents 
WHERE ChunkEmbeddingStatus = 'Pending';
```

**Soluzione:**
Se `HasText = 'VUOTO'`, il documento non ha testo estraibile (es. immagine senza OCR).

## Flusso Normale di Elaborazione

```
1. Caricamento Documento
   ↓
2. Estrazione Testo (OCR se necessario)
   ↓ 
3. Status = "Pending"
   ↓
4. [OGNI 30 SECONDI] BatchEmbeddingProcessor
   ↓
5. Creazione Chunks (10-30 chunks per documento)
   ↓
6. Status = "Processing"
   ↓
7. Generazione Embeddings (AI service)
   ↓
8. Salvataggio Embeddings nel DB
   ↓
9. Status = "Completed" ✓
```

**Il tuo caso si è bloccato al punto 4** - il BatchEmbeddingProcessor non sta creando i chunks.

## Fix Applicati

Ho migliorato il sistema nel commit `986d2fb`:

### Nuovo: Error Handling Migliorato
```csharp
// Ora cattura errori specifici di configurazione AI
catch (InvalidOperationException ex) when (ex.Message.Contains("Nessuna configurazione AI"))
{
    // Aggiorna documento con messaggio di errore visibile
    doc.ProcessingError = "Configurazione AI mancante. Configura un provider AI in /config";
}
```

### Cosa cambia per te:
- ✅ Errori più chiari nei log
- ✅ Campo `ProcessingError` visibile nel documento
- ✅ Logging dettagliato quando AI service non è disponibile

## Azioni Immediate da Fare

### Passo 1: Verifica Configurazione AI
```sql
SELECT * FROM AIConfigurations WHERE IsActive = 1;
```
- **Se vuoto**: Vai su `/config` e configura un provider
- **Se presente**: Verifica che ApiKey sia valida

### Passo 2: Controlla Log Server
Cerca questi pattern nei log del DocN.Server:
```
[INFO] Batch Embedding Processor started
[INFO] Found 4 documents needing chunk creation
[INFO] Creating chunks for document X
[ERROR] AI Configuration missing
[WARN] AI service not available
```

### Passo 3: Riavvia Server (se necessario)
Se il BatchEmbeddingProcessor non è nei log:
```bash
# Ferma e riavvia DocN.Server
dotnet run --project DocN.Server
```

### Passo 4: Monitora Dashboard
Dopo aver risolto, il Dashboard dovrebbe mostrare progressi ogni 30 secondi:
- Prima: "4 Documenti in Coda"
- Dopo 30s: "3 Documenti in Coda" (se sta elaborando)
- Dopo 2-5 min: "0 Documenti in Coda" ✓

## Forza Rielaborazione (se necessario)

Se i documenti restano bloccati anche dopo aver configurato l'AI:

```sql
-- Opzione 1: Reset status a Pending
UPDATE Documents 
SET ChunkEmbeddingStatus = 'Pending', ProcessingError = NULL
WHERE ChunkEmbeddingStatus = 'Pending';

-- Opzione 2: Elimina chunks esistenti (se presenti) e ricrea
DELETE FROM DocumentChunks WHERE DocumentId IN (
    SELECT Id FROM Documents WHERE ChunkEmbeddingStatus = 'Pending'
);
UPDATE Documents 
SET ChunkEmbeddingStatus = 'Pending', ProcessingError = NULL
WHERE ChunkEmbeddingStatus = 'Pending';
```

## Test di Verifica

Dopo la configurazione, testa con un nuovo documento:

1. Carica un PDF piccolo di test (1-2 pagine)
2. Osserva il badge: "⏳ Embeddings in coda"
3. Aspetta 30 secondi
4. Refresh della pagina
5. Dovresti vedere badge cambiare a "⚙️ Elaborazione..."
6. Dopo 1-2 minuti: "✓ Pronto"

## Documentazione Completa

- **Diagnostica dettagliata**: `docs/DIAGNOSTICA_EMBEDDINGS_BLOCCATI.md`
- **Monitoraggio coda**: `docs/EMBEDDING_QUEUE_MONITORING.md`
- **Riepilogo fix**: `SOLUZIONE_EMBEDDINGS_IN_CODA.md`

## Prossimi Passi

1. ✅ Esegui query SQL per verificare configurazione AI
2. ✅ Controlla log del server per errori
3. ✅ Configura provider AI se mancante
4. ✅ Riavvia server se necessario
5. ✅ Monitora Dashboard per progressi

---

**Commit con fix**: `986d2fb`  
**Data**: 2 Gennaio 2026  
**Priorità**: 🔴 ALTA - Sistema bloccato
