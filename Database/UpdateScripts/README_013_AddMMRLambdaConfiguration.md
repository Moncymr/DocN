# Update 013: MMR Lambda Configuration

## 📋 Overview / Panoramica

**English:**
This update adds the `MMRLambda` column to the `AIConfigurations` table, allowing database-level configuration of the Maximal Marginal Relevance (MMR) parameter for controlling diversity in search results.

**Italiano:**
Questo aggiornamento aggiunge la colonna `MMRLambda` alla tabella `AIConfigurations`, permettendo la configurazione a livello di database del parametro MMR (Maximal Marginal Relevance) per controllare la diversità nei risultati di ricerca.

---

## 🎯 Purpose / Scopo

**English:**
The MMR Lambda parameter controls the balance between relevance and diversity in vector search results:
- **High Lambda (0.8-1.0)**: Prioritizes relevance (more similar documents)
- **Medium Lambda (0.5-0.7)**: Balanced approach (recommended)
- **Low Lambda (0.0-0.4)**: Prioritizes diversity (more varied documents)

This update enables:
- ✅ Per-user or per-tenant MMR configuration
- ✅ Database-driven parameter management
- ✅ Dynamic adjustment without code changes
- ✅ Override of default appsettings.json values

**Italiano:**
Il parametro Lambda MMR controlla il bilanciamento tra rilevanza e diversità nei risultati di ricerca vettoriale:
- **Lambda Alto (0.8-1.0)**: Priorità alla rilevanza (documenti più simili)
- **Lambda Medio (0.5-0.7)**: Approccio bilanciato (raccomandato)
- **Lambda Basso (0.0-0.4)**: Priorità alla diversità (documenti più vari)

Questo aggiornamento abilita:
- ✅ Configurazione MMR per utente o tenant
- ✅ Gestione parametri guidata dal database
- ✅ Regolazione dinamica senza modifiche al codice
- ✅ Override dei valori default di appsettings.json

---

## 📝 Changes / Modifiche

### Database Schema

```sql
ALTER TABLE [dbo].[AIConfigurations]
ADD MMRLambda FLOAT NOT NULL DEFAULT 0.7;
```

### New Column

| Column Name | Type | Default | Nullable | Description |
|-------------|------|---------|----------|-------------|
| `MMRLambda` | FLOAT | 0.7 | NO | MMR Lambda parameter (0.0-1.0) |

---

## 🚀 How to Apply / Come Applicare

### Option 1: Using SQL Script / Usando Script SQL

**SQL Server Management Studio (SSMS)**:
1. Open SQL Server Management Studio
2. Connect to your DocN database
3. Open file: `Database/UpdateScripts/013_AddMMRLambdaConfiguration.sql`
4. Execute (F5)

**Azure Data Studio**:
1. Open Azure Data Studio
2. Connect to your DocN database
3. Open file: `Database/UpdateScripts/013_AddMMRLambdaConfiguration.sql`
4. Click "Run"

**Command Line (sqlcmd)**:
```bash
sqlcmd -S YOUR_SERVER -d DocNDb -i Database/UpdateScripts/013_AddMMRLambdaConfiguration.sql
```

### Option 2: Manual SQL / SQL Manuale

```sql
-- Add column if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID(N'[dbo].[AIConfigurations]') 
               AND name = 'MMRLambda')
BEGIN
    ALTER TABLE [dbo].[AIConfigurations]
    ADD MMRLambda FLOAT NOT NULL DEFAULT 0.7;
END

-- Update existing configurations
UPDATE [dbo].[AIConfigurations]
SET MMRLambda = 0.7
WHERE MMRLambda IS NULL;
```

---

## ✅ Verification / Verifica

After applying the update, verify the changes:

```sql
-- Check if column exists
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    COLUMN_DEFAULT,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AIConfigurations'
AND COLUMN_NAME = 'MMRLambda';

-- View all configurations
SELECT 
    Id,
    ConfigurationName,
    MMRLambda,
    SimilarityThreshold,
    MaxDocumentsToRetrieve,
    IsActive
FROM AIConfigurations;
```

Expected output:
```
COLUMN_NAME  DATA_TYPE  COLUMN_DEFAULT  IS_NULLABLE
MMRLambda    float      ((0.7))         NO
```

---

## 📊 Usage Examples / Esempi di Utilizzo

### Example 1: Set Global Default

```sql
-- Set default MMR Lambda for all active configurations
UPDATE AIConfigurations
SET MMRLambda = 0.7
WHERE IsActive = 1;
```

### Example 2: Per-User Configuration

```sql
-- Create user-specific configuration with high diversity
INSERT INTO AIConfigurations (
    ConfigurationName,
    MMRLambda,
    MaxDocumentsToRetrieve,
    SimilarityThreshold,
    IsActive
)
VALUES (
    'User123 - High Diversity',
    0.3,  -- Low lambda = high diversity
    10,
    0.7,
    1
);
```

### Example 3: Per-Tenant Configuration

```sql
-- Different lambda values for different use cases
UPDATE AIConfigurations
SET MMRLambda = CASE 
    WHEN ConfigurationName LIKE '%Legal%' THEN 0.9  -- High relevance for legal
    WHEN ConfigurationName LIKE '%Research%' THEN 0.4  -- High diversity for research
    ELSE 0.7  -- Balanced for others
END
WHERE IsActive = 1;
```

---

## 🎛️ Lambda Value Guide / Guida ai Valori Lambda

| Value / Valore | Use Case / Caso d'Uso | Result / Risultato |
|----------------|------------------------|---------------------|
| **0.0 - 0.3** | Exploratory search, Creative research | Maximum diversity, varied results |
| **0.4 - 0.6** | Balanced search, General exploration | Good mix of relevance and diversity |
| **0.7** | **Default - General Q&A** | **Recommended balance (70% relevance, 30% diversity)** |
| **0.8 - 0.9** | Precise search, Legal/Technical docs | High relevance, minimal diversity |
| **1.0** | Exact match only, Compliance/Audit | Maximum relevance, no diversity |

---

## 🔄 Integration with Code / Integrazione con il Codice

The application code will automatically use database values:

```csharp
// In your service (EnhancedVectorStoreService or PgVectorStoreService)
public async Task<List<VectorSearchResult>> SearchForUserAsync(
    string userId, 
    float[] queryVector, 
    int topK)
{
    // 1. Load user configuration from database
    var userConfig = await _context.AIConfigurations
        .FirstOrDefaultAsync(c => c.IsActive && c.ConfigurationName.Contains(userId));

    // 2. Use database lambda or fallback to appsettings.json default
    var lambda = userConfig?.MMRLambda ?? _ragConfig.Reranking.MMRLambda;

    // 3. Perform search with configured lambda
    return await _vectorStore.SearchWithMMRAsync(
        queryVector, 
        topK, 
        lambda: lambda
    );
}
```

---

## 🔐 Security Considerations / Considerazioni sulla Sicurezza

**English:**
- Lambda values are stored per-configuration, allowing tenant/user isolation
- Only authorized users should modify AIConfigurations table
- Validate lambda values are within 0.0-1.0 range in application code

**Italiano:**
- I valori Lambda sono memorizzati per configurazione, permettendo isolamento tenant/utente
- Solo utenti autorizzati dovrebbero modificare la tabella AIConfigurations
- Validare che i valori lambda siano nell'intervallo 0.0-1.0 nel codice applicativo

---

## 📈 Performance Impact / Impatto sulle Prestazioni

| Lambda | Search Speed | Result Quality | Use Case |
|--------|-------------|----------------|-----------|
| 0.0 | +20ms | High diversity, lower avg relevance | Exploration |
| 0.5 | +15ms | Balanced | General purpose |
| 0.7 | +12ms | Good balance | **Recommended** |
| 1.0 | 0ms | Maximum relevance | Exact matches |

**Note**: Lower lambda values require more computation for diversity calculation.

---

## 🔧 Rollback / Annullamento

If you need to remove this update:

```sql
-- Remove the column (will lose data!)
ALTER TABLE AIConfigurations
DROP COLUMN MMRLambda;
```

**Warning**: This will permanently delete all stored lambda values.

---

## 📚 Related Documentation / Documentazione Correlata

- **CONFIGURAZIONE_LAMBDA_MMR.md** - Complete lambda configuration guide
- **ADVANCED_RAG_FEATURES.md** - Technical details on MMR algorithm
- **ANALISI_SISTEMA_RAG_RISPOSTA.md** - System analysis and improvements

---

## 🆘 Troubleshooting / Risoluzione Problemi

### Issue: Column already exists / La colonna esiste già

**Solution**: The script checks for existence and skips if already present.

### Issue: Default value not applied / Valore default non applicato

**Solution**:
```sql
UPDATE AIConfigurations
SET MMRLambda = 0.7
WHERE MMRLambda IS NULL OR MMRLambda < 0 OR MMRLambda > 1;
```

### Issue: Migration fails / La migrazione fallisce

**Check**:
```sql
-- Verify table exists
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'AIConfigurations';

-- Check permissions
-- User must have ALTER permission on table
```

---

## 📞 Support / Supporto

For questions or issues:
1. Check CONFIGURAZIONE_LAMBDA_MMR.md for configuration details
2. Review Database/UpdateScripts/README_013_AddMMRLambdaConfiguration.md
3. Check application logs for lambda value usage
4. Verify database column exists and has valid values

---

**Version**: 1.0  
**Date**: January 7, 2026  
**SQL Server**: 2025  
**Compatibility**: Backward compatible, non-breaking change
