# Update 006: Flexible Vector Dimensions Support

## 📋 Overview / Panoramica

**English:**
This update adds support for flexible vector dimensions, allowing vectors of different sizes (700, 768, 1536, 1583, etc.) to coexist in the same database.

**Italiano:**
Questo aggiornamento aggiunge il supporto per dimensioni vettoriali flessibili, consentendo a vettori di diverse dimensioni (700, 768, 1536, 1583, ecc.) di coesistere nello stesso database.

## 🎯 Purpose / Scopo

**English:**
Modern embedding models support custom dimensions:
- **Gemini text-embedding-004**: Default 768, can use custom like 700
- **OpenAI text-embedding-3-small**: Default 1536, can use custom like 1583
- **OpenAI text-embedding-3-large**: Default 3072, supports custom dimensions

The previous system only supported fixed dimensions (768 or 1536). This update removes that limitation.

**Italiano:**
I modelli di embedding moderni supportano dimensioni personalizzate:
- **Gemini text-embedding-004**: Default 768, può usare custom come 700
- **OpenAI text-embedding-3-small**: Default 1536, può usare custom come 1583
- **OpenAI text-embedding-3-large**: Default 3072, supporta dimensioni personalizzate

Il sistema precedente supportava solo dimensioni fisse (768 o 1536). Questo aggiornamento rimuove tale limitazione.

## 📝 Changes / Modifiche

### Database Schema

```sql
-- Add dimension tracking columns
ALTER TABLE Documents ADD EmbeddingDimension INT NULL;
ALTER TABLE DocumentChunks ADD EmbeddingDimension INT NULL;
```

### What It Does / Cosa Fa

**English:**
1. Adds `EmbeddingDimension` column to track the actual dimension of each embedding
2. Allows vectors of any dimension (256-4096) to be stored
3. Validates dimensions are within acceptable range
4. Automatically tracks dimension when embeddings are saved

**Italiano:**
1. Aggiunge colonna `EmbeddingDimension` per tracciare la dimensione effettiva di ogni embedding
2. Consente di memorizzare vettori di qualsiasi dimensione (256-4096)
3. Valida che le dimensioni siano nell'intervallo accettabile
4. Traccia automaticamente la dimensione quando gli embedding vengono salvati

## 🚀 How to Apply / Come Applicare

### Option 1: Using SQL Script / Usando Script SQL

```bash
# Connect to your database and run:
sqlcmd -S localhost -U sa -P YourPassword -d DocNDb -i Database/UpdateScripts/006_AddFlexibleVectorDimensions.sql
```

### Option 2: Using EF Core Migration / Usando Migration EF Core

```bash
# Navigate to server project
cd DocN.Server

# Apply migration
dotnet ef database update
```

## ✅ Verification / Verifica

After applying the update, verify the changes:

```sql
-- Check the new columns exist
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Documents', 'DocumentChunks')
AND COLUMN_NAME = 'EmbeddingDimension';
```

Expected output:
```
TABLE_NAME       COLUMN_NAME          DATA_TYPE  IS_NULLABLE
Documents        EmbeddingDimension   int        YES
DocumentChunks   EmbeddingDimension   int        YES
```

## 🔄 Backward Compatibility / Compatibilità Retroattiva

**English:**
- ✅ Existing embeddings continue to work without modification
- ✅ The new column is nullable, so no data loss
- ✅ When existing embeddings are re-saved, dimension is automatically tracked
- ✅ No action required on existing data

**Italiano:**
- ✅ Gli embedding esistenti continuano a funzionare senza modifiche
- ✅ La nuova colonna è nullable, quindi nessuna perdita di dati
- ✅ Quando gli embedding esistenti vengono ri-salvati, la dimensione viene automaticamente tracciata
- ✅ Nessuna azione richiesta sui dati esistenti

## 📊 Monitoring / Monitoraggio

Check dimension distribution in your database:

```sql
-- See what dimensions are being used
SELECT 
    EmbeddingDimension,
    COUNT(*) as DocumentCount,
    CAST(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER () AS DECIMAL(5,2)) as Percentage
FROM Documents
WHERE EmbeddingDimension IS NOT NULL
GROUP BY EmbeddingDimension
ORDER BY DocumentCount DESC;
```

## ⚠️ Important Notes / Note Importanti

### Vector Comparison / Confronto Vettori

**English:**
- Comparing vectors of different dimensions may not be semantically meaningful
- Best practice: Use consistent dimensions for documents you want to compare
- Consider grouping documents by dimension for search operations

**Italiano:**
- Il confronto di vettori di dimensioni diverse potrebbe non essere semanticamente significativo
- Best practice: Usare dimensioni coerenti per i documenti da confrontare
- Considerare il raggruppamento dei documenti per dimensione nelle operazioni di ricerca

### Performance / Prestazioni

**English:**
- Larger dimensions = more storage required
- Larger dimensions = potentially better semantic accuracy
- Balance storage cost vs. quality needs

**Italiano:**
- Dimensioni maggiori = più storage richiesto
- Dimensioni maggiori = potenzialmente migliore accuratezza semantica
- Bilanciare costo storage vs esigenze di qualità

## 📚 Documentation / Documentazione

For complete documentation, see:
- **FLEXIBLE_VECTOR_DIMENSIONS.md** - Comprehensive guide
- **VECTOR_DIMENSION_FIX.md** - Previous dimension issues (now resolved)

## 🎉 Benefits / Benefici

**English:**
1. ✅ **Flexibility**: Support any AI provider with any dimension
2. ✅ **Future-proof**: New models automatically supported
3. ✅ **Coexistence**: Multiple dimensions in same database
4. ✅ **Tracking**: Always know which dimension was used
5. ✅ **No Breaking Changes**: Existing code continues to work

**Italiano:**
1. ✅ **Flessibilità**: Supporto per qualsiasi provider AI con qualsiasi dimensione
2. ✅ **A prova di futuro**: Nuovi modelli automaticamente supportati
3. ✅ **Coesistenza**: Multiple dimensioni nello stesso database
4. ✅ **Tracciamento**: Sapere sempre quale dimensione è stata utilizzata
5. ✅ **Nessuna Breaking Change**: Il codice esistente continua a funzionare

## 🔗 Related Updates / Aggiornamenti Correlati

- Update 001: Multi-Provider AI Configuration
- Update 004: Update Gemini Default Model

## 🐛 Troubleshooting / Risoluzione Problemi

### Issue: Migration fails / La migrazione fallisce

**Solution:**
Check if columns already exist:
```sql
SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME IN ('Documents', 'DocumentChunks')
AND COLUMN_NAME = 'EmbeddingDimension';
```

If columns exist, the update is already applied.

### Issue: Old embeddings don't have dimension / Vecchi embedding senza dimensione

**Solution:**
This is expected. The dimension will be populated when:
1. New embeddings are generated
2. Existing documents are re-processed
3. Embeddings are manually updated

You can also set it manually:
```sql
UPDATE Documents 
SET EmbeddingDimension = 768  -- or 1536, depending on your model
WHERE EmbeddingDimension IS NULL 
AND EmbeddingVector IS NOT NULL;
```

## 📞 Support / Supporto

For issues or questions:
1. Check the documentation in FLEXIBLE_VECTOR_DIMENSIONS.md
2. Review the migration file for details
3. Check existing GitHub issues
4. Create a new issue with details about your setup
