# Vector Dimension Fix: 768 → 1536

## 🔴 Problema / Problem

**Italiano:**
Quando si carica un documento, l'applicazione fallisce con l'errore:
```
Database save failed: Le dimensioni del vettore 1536 e 768 non corrispondono.
```

**English:**
When uploading a document, the application fails with the error:
```
Database save failed: Vector dimensions 1536 and 768 do not match.
```

## 🎯 Causa / Root Cause

**Italiano:**
- L'applicazione genera embeddings con **1536 dimensioni** (OpenAI text-embedding-ada-002 / Azure OpenAI)
- Il database era configurato con **768 dimensioni** (Gemini text-embedding-004)
- Questa incompatibilità causava il fallimento del salvataggio

**English:**
- The application generates embeddings with **1536 dimensions** (OpenAI text-embedding-ada-002 / Azure OpenAI)
- The database was configured with **768 dimensions** (Gemini text-embedding-004)
- This incompatibility caused the save to fail

## ✅ Soluzione / Solution

### Per Nuove Installazioni / For New Installations

**Italiano:**
Usa il file aggiornato `database/SqlServer2025_Schema.sql` che ora usa `VECTOR(1536)`.

```bash
sqlcmd -S localhost -U sa -P YourPassword -i database/SqlServer2025_Schema.sql
```

**English:**
Use the updated `database/SqlServer2025_Schema.sql` file which now uses `VECTOR(1536)`.

```bash
sqlcmd -S localhost -U sa -P YourPassword -i database/SqlServer2025_Schema.sql
```

### Per Database Esistenti / For Existing Databases

**Italiano:**
Se hai già un database con `VECTOR(768)`, esegui lo script di migrazione:

```bash
# 1. Fai un backup
sqlcmd -S localhost -U sa -P YourPassword -Q "BACKUP DATABASE [DocN] TO DISK = N'C:\Backup\DocN_Before_Migration.bak' WITH INIT;"

# 2. Esegui la migrazione
sqlcmd -S localhost -U sa -P YourPassword -i database/Update_Vector_768_to_1536.sql

# 3. Riavvia l'applicazione e ri-processa i documenti
```

**English:**
If you already have a database with `VECTOR(768)`, run the migration script:

```bash
# 1. Backup the database
sqlcmd -S localhost -U sa -P YourPassword -Q "BACKUP DATABASE [DocN] TO DISK = N'C:\Backup\DocN_Before_Migration.bak' WITH INIT;"

# 2. Run the migration
sqlcmd -S localhost -U sa -P YourPassword -i database/Update_Vector_768_to_1536.sql

# 3. Restart the application and re-process documents
```

## 📝 Cosa fa lo Script di Migrazione / What the Migration Script Does

**Italiano:**
1. Controlla la configurazione attuale delle colonne vettoriali
2. Cancella gli embeddings esistenti (devono essere rigenerati)
3. Altera le colonne da `VECTOR(768)` a `VECTOR(1536)`:
   - `Documents.Embedding`
   - `DocumentChunks.Embedding`
4. Aggiorna tutte le stored procedures per usare `VECTOR(1536)`
5. Verifica che la migrazione sia completata con successo

**English:**
1. Checks the current configuration of vector columns
2. Clears existing embeddings (they need to be regenerated)
3. Alters columns from `VECTOR(768)` to `VECTOR(1536)`:
   - `Documents.Embedding`
   - `DocumentChunks.Embedding`
4. Updates all stored procedures to use `VECTOR(1536)`
5. Verifies that the migration completed successfully

## ⚠️ Note Importanti / Important Notes

**Italiano:**
- ⚠️ **Tutti gli embeddings esistenti verranno cancellati** durante la migrazione
- 🔄 **Dovrai ri-processare i documenti** dopo la migrazione per rigenerare gli embeddings
- 💾 **Fai sempre un backup** prima di eseguire la migrazione
- ✅ Lo script è idempotente: può essere eseguito più volte senza problemi

**English:**
- ⚠️ **All existing embeddings will be cleared** during migration
- 🔄 **You will need to re-process documents** after migration to regenerate embeddings
- 💾 **Always backup** before running the migration
- ✅ The script is idempotent: it can be run multiple times without issues

## 🔍 Come Verificare / How to Verify

**Italiano:**
Dopo la migrazione, verifica la configurazione:

```sql
USE DocN;
GO

SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Documents', 'DocumentChunks')
AND COLUMN_NAME IN ('Embedding', 'EmbeddingVector', 'ChunkEmbedding');
```

Dovresti vedere `VECTOR(1536)` per tutte le colonne.

**English:**
After migration, verify the configuration:

```sql
USE DocN;
GO

SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Documents', 'DocumentChunks')
AND COLUMN_NAME IN ('Embedding', 'EmbeddingVector', 'ChunkEmbedding');
```

You should see `VECTOR(1536)` for all columns.

## 📚 File Modificati / Modified Files

1. **database/SqlServer2025_Schema.sql** - Schema aggiornato con VECTOR(1536)
2. **database/Update_Vector_768_to_1536.sql** - Script di migrazione (NUOVO)
3. **database/README.md** - Documentazione aggiornata
4. **Database/README.md** - Documentazione aggiornata

## 🚀 Prossimi Passi / Next Steps

**Italiano:**
1. ✅ Applica la migrazione al database
2. ✅ Riavvia l'applicazione DocN
3. ✅ Carica un nuovo documento per verificare che funzioni
4. ✅ (Opzionale) Ri-processa i documenti esistenti per rigenerare gli embeddings

**English:**
1. ✅ Apply the migration to the database
2. ✅ Restart the DocN application
3. ✅ Upload a new document to verify it works
4. ✅ (Optional) Re-process existing documents to regenerate embeddings

## 📞 Supporto / Support

Se hai problemi con la migrazione, verifica:
- SQL Server 2025 è installato e in esecuzione
- Hai i permessi necessari per alterare le tabelle
- Hai fatto un backup del database

If you have issues with the migration, check:
- SQL Server 2025 is installed and running
- You have the necessary permissions to alter tables
- You have backed up the database
