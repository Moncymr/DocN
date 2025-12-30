# Soluzione VECTOR Doppi: Supporto Multi-Provider

## 🎯 Soluzione Implementata

Per risolvere l'errore "Le dimensioni del vettore 1536 e 768 non corrispondono", abbiamo implementato **due campi VECTOR separati** con dimensioni diverse:

### Campi nel Database

**Documents:**
- `EmbeddingVector768` → VECTOR(768) per Gemini
- `EmbeddingVector1536` → VECTOR(1536) per OpenAI
- `EmbeddingDimension` → int per tracciare quale campo è usato

**DocumentChunks:**
- `ChunkEmbedding768` → VECTOR(768) per Gemini
- `ChunkEmbedding1536` → VECTOR(1536) per OpenAI
- `EmbeddingDimension` → int per tracciare quale campo è usato

## ✅ Vantaggi

1. ✅ **Tipo VECTOR Nativo**: Prestazioni ottimali con il tipo nativo di SQL Server 2025
2. ✅ **Nessun Errore di Dimensioni**: Ogni dimensione ha il suo campo dedicato
3. ✅ **Multi-Provider**: Supporta Gemini E OpenAI nello stesso database
4. ✅ **Flessibilità**: Ogni documento usa il campo appropriato per la sua dimensione

## 🚀 Come Funziona

### Upload di un Documento

1. **Gemini genera embedding di 768 dimensioni:**
   ```csharp
   document.EmbeddingVector768 = embedding;  // 768 dimensioni
   document.EmbeddingDimension = 768;
   ```

2. **OpenAI genera embedding di 1536 dimensioni:**
   ```csharp
   document.EmbeddingVector1536 = embedding; // 1536 dimensioni
   document.EmbeddingDimension = 1536;
   ```

### Il Codice Sceglie Automaticamente

Il sistema determina automaticamente quale campo usare:

```csharp
if (embedding.Length == 768)
{
    document.EmbeddingVector768 = embedding;
    document.EmbeddingDimension = 768;
}
else if (embedding.Length == 1536)
{
    document.EmbeddingVector1536 = embedding;
    document.EmbeddingDimension = 1536;
}
```

## 📦 Applicare la Migrazione

### Opzione 1: Script SQL Diretto

```bash
sqlcmd -S localhost -U sa -P YourPassword -i Database/UpdateScripts/008_AddDualVectorFields.sql
```

### Opzione 2: EF Core Migration

```bash
cd DocN.Server
dotnet ef database update
```

Lo script:
- ✅ Aggiunge i nuovi campi VECTOR(768) e VECTOR(1536)
- ✅ Mantiene i dati esistenti
- ✅ È sicuro da eseguire più volte
- ✅ Verifica se le colonne esistono già

## 🔍 Verifica

Dopo aver applicato la migrazione, verifica che i campi esistano:

```sql
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Documents', 'DocumentChunks')
AND COLUMN_NAME LIKE '%Embedding%'
ORDER BY TABLE_NAME, COLUMN_NAME;
```

Dovresti vedere:
```
Documents       | EmbeddingVector768   | vector
Documents       | EmbeddingVector1536  | vector
DocumentChunks  | ChunkEmbedding768    | vector
DocumentChunks  | ChunkEmbedding1536   | vector
```

## 📊 Esempio di Utilizzo

### Database con Entrambi i Provider

```sql
-- Documento da Gemini (768 dimensioni)
INSERT INTO Documents (FileName, EmbeddingVector768, EmbeddingDimension, ...)
VALUES ('doc_gemini.pdf', '[0.1, 0.2, ...]', 768, ...);

-- Documento da OpenAI (1536 dimensioni)  
INSERT INTO Documents (FileName, EmbeddingVector1536, EmbeddingDimension, ...)
VALUES ('doc_openai.pdf', '[0.1, 0.2, ...]', 1536, ...);
```

### Query per Statistiche

```sql
-- Conta documenti per dimensione
SELECT 
    EmbeddingDimension,
    COUNT(*) as DocumentCount
FROM Documents
WHERE EmbeddingDimension IS NOT NULL
GROUP BY EmbeddingDimension;

-- Risultato:
-- 768  | 15
-- 1536 | 23
```

## ⚠️ Note Importanti

### Ricerca Semantica

Per la ricerca vettoriale, devi cercare nel campo corretto:

```sql
-- Cerca documenti Gemini (768)
SELECT * FROM Documents
WHERE VECTOR_DISTANCE('cosine', EmbeddingVector768, @QueryVector768) < 0.5;

-- Cerca documenti OpenAI (1536)
SELECT * FROM Documents
WHERE VECTOR_DISTANCE('cosine', EmbeddingVector1536, @QueryVector1536) < 0.5;
```

### Confronto tra Provider Diversi

⚠️ **NON puoi confrontare direttamente** vettori di dimensioni diverse:
- VECTOR(768) ≠ VECTOR(1536)
- La similarità coseno non è significativa tra dimensioni diverse

**Soluzione**: Organizza i documenti per provider o usa filtri per cercare solo documenti dello stesso tipo.

### Storage

- Ogni documento usa **solo UNO** dei due campi
- L'altro campo rimane NULL
- `EmbeddingDimension` indica quale campo è popolato

## 🔄 Migrazione Dati Esistenti

Se hai già documenti con il vecchio campo `EmbeddingVector`:

```sql
-- Sposta vettori 768 nel campo appropriato
UPDATE Documents
SET EmbeddingVector768 = EmbeddingVector
WHERE LEN(EmbeddingVector) = /* calcola per 768 dimensioni */;

-- Sposta vettori 1536 nel campo appropriato
UPDATE Documents
SET EmbeddingVector1536 = EmbeddingVector
WHERE LEN(EmbeddingVector) = /* calcola per 1536 dimensioni */;
```

## 🎉 Risultato

Dopo l'implementazione:
- ✅ Nessun errore "Le dimensioni del vettore non corrispondono"
- ✅ Supporto nativo per Gemini (768) e OpenAI (1536)
- ✅ Prestazioni ottimali con tipo VECTOR nativo
- ✅ Flessibilità per aggiungere altri provider in futuro

## 📚 File Modificati

1. **DocN.Data/Models/Document.cs** - Aggiunti campi EmbeddingVector768 e EmbeddingVector1536
2. **DocN.Data/Models/DocumentChunk.cs** - Aggiunti campi ChunkEmbedding768 e ChunkEmbedding1536
3. **DocN.Data/ApplicationDbContext.cs** - Configurazione VECTOR(768) e VECTOR(1536)
4. **DocN.Client/Components/Pages/Upload.razor** - Logica per selezionare il campo corretto
5. **DocN.Data/Services/DocumentService.cs** - Gestione automatica dei campi
6. **Database/UpdateScripts/008_AddDualVectorFields.sql** - Script di migrazione SQL

## 💡 Best Practice

1. **Usa un solo provider per documenti correlati** che devono essere cercati insieme
2. **Documenta quale provider usi** nella configurazione
3. **Monitora l'uso** tramite il campo `EmbeddingDimension`
4. **Organizza per tenant** se ogni tenant usa un provider diverso

---

**Status**: ✅ Implementato e pronto all'uso. I campi VECTOR nativi garantiscono prestazioni ottimali!
