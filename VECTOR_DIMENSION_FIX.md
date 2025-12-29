# Vector Dimension Migration Guide

## 🔴 Problema / Problem

**Italiano:**
Quando si carica un documento, l'applicazione fallisce con un errore di incompatibilità delle dimensioni del vettore:
```
Database save failed: Le dimensioni del vettore 1536 e 768 non corrispondono.
```

**English:**
When uploading a document, the application fails with a vector dimension mismatch error:
```
Database save failed: Vector dimensions 1536 and 768 do not match.
```

## 🎯 Causa / Root Cause

**Italiano:**
Il problema si verifica quando le dimensioni degli embedding generate non corrispondono alla configurazione del database:
- **OpenAI/Azure OpenAI** genera embeddings con **1536 dimensioni** (text-embedding-ada-002)
- **Gemini** genera embeddings con **768 dimensioni** (text-embedding-004)
- Se il database è configurato per una dimensione diversa da quella degli embedding generati, il salvataggio fallisce

**English:**
The problem occurs when the embedding dimensions generated don't match the database configuration:
- **OpenAI/Azure OpenAI** generates embeddings with **1536 dimensions** (text-embedding-ada-002)
- **Gemini** generates embeddings with **768 dimensions** (text-embedding-004)
- If the database is configured for a different dimension than the generated embeddings, saving fails

## ✅ Soluzione / Solution

### Per Nuove Installazioni / For New Installations

**Italiano:**
Per nuove installazioni, usa il file `database/SqlServer2025_Schema.sql` che è configurato per `VECTOR(1536)` (OpenAI/Azure OpenAI).

```bash
sqlcmd -S localhost -U sa -P YourPassword -i database/SqlServer2025_Schema.sql
```

**English:**
For new installations, use the `database/SqlServer2025_Schema.sql` file which is configured for `VECTOR(1536)` (OpenAI/Azure OpenAI).

```bash
sqlcmd -S localhost -U sa -P YourPassword -i database/SqlServer2025_Schema.sql
```

### Per Database Esistenti / For Existing Databases

#### Scenario 1: Database con VECTOR(768) → VECTOR(1536)

**Italiano:**
Se hai un database con `VECTOR(768)` e vuoi usare OpenAI/Azure OpenAI, esegui lo script di migrazione:

```bash
# 1. Fai un backup
sqlcmd -S localhost -U sa -P YourPassword -Q "BACKUP DATABASE [DocN] TO DISK = N'C:\Backup\DocN_Before_Migration.bak' WITH INIT;"

# 2. Esegui la migrazione 768 → 1536
sqlcmd -S localhost -U sa -P YourPassword -i database/Update_Vector_768_to_1536.sql

# 3. Configura l'applicazione per usare OpenAI/Azure OpenAI
# 4. Riavvia l'applicazione e ri-processa i documenti
```

**English:**
If you have a database with `VECTOR(768)` and want to use OpenAI/Azure OpenAI, run the migration script:

```bash
# 1. Backup the database
sqlcmd -S localhost -U sa -P YourPassword -Q "BACKUP DATABASE [DocN] TO DISK = N'C:\Backup\DocN_Before_Migration.bak' WITH INIT;"

# 2. Run the migration 768 → 1536
sqlcmd -S localhost -U sa -P YourPassword -i database/Update_Vector_768_to_1536.sql

# 3. Configure the application to use OpenAI/Azure OpenAI
# 4. Restart the application and re-process documents
```

#### Scenario 2: Database con VECTOR(1536) → VECTOR(768)

**Italiano:**
Se hai un database con `VECTOR(1536)` e vuoi usare Gemini, esegui lo script di migrazione:

```bash
# 1. Fai un backup
sqlcmd -S localhost -U sa -P YourPassword -Q "BACKUP DATABASE [DocN] TO DISK = N'C:\Backup\DocN_Before_Migration.bak' WITH INIT;"

# 2. Esegui la migrazione 1536 → 768
sqlcmd -S localhost -U sa -P YourPassword -i database/Update_Vector_1536_to_768.sql

# 3. Configura l'applicazione per usare Gemini
# 4. Riavvia l'applicazione e ri-processa i documenti
```

**English:**
If you have a database with `VECTOR(1536)` and want to use Gemini, run the migration script:

```bash
# 1. Backup the database
sqlcmd -S localhost -U sa -P YourPassword -Q "BACKUP DATABASE [DocN] TO DISK = N'C:\Backup\DocN_Before_Migration.bak' WITH INIT;"

# 2. Run the migration 1536 → 768
sqlcmd -S localhost -U sa -P YourPassword -i database/Update_Vector_1536_to_768.sql

# 3. Configure the application to use Gemini
# 4. Restart the application and re-process documents
```

## 📝 Cosa fanno gli Script di Migrazione / What the Migration Scripts Do

### Update_Vector_768_to_1536.sql (Gemini → OpenAI)

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

### Update_Vector_1536_to_768.sql (OpenAI → Gemini)

**Italiano:**
1. Controlla la configurazione attuale delle colonne vettoriali
2. Cancella gli embeddings esistenti (devono essere rigenerati)
3. Altera le colonne da `VECTOR(1536)` a `VECTOR(768)`:
   - `Documents.Embedding`
   - `DocumentChunks.Embedding`
4. Aggiorna tutte le stored procedures per usare `VECTOR(768)`
5. Verifica che la migrazione sia completata con successo

**English:**
1. Checks the current configuration of vector columns
2. Clears existing embeddings (they need to be regenerated)
3. Alters columns from `VECTOR(1536)` to `VECTOR(768)`:
   - `Documents.Embedding`
   - `DocumentChunks.Embedding`
4. Updates all stored procedures to use `VECTOR(768)`
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
2. **database/Update_Vector_768_to_1536.sql** - Script di migrazione da 768 a 1536 dimensioni
3. **database/Update_Vector_1536_to_768.sql** - Script di migrazione da 1536 a 768 dimensioni
4. **database/README.md** - Documentazione aggiornata
5. **Database/README.md** - Documentazione aggiornata
6. **DocN.Data/Utilities/EmbeddingValidationHelper.cs** - Messaggi di errore aggiornati

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
