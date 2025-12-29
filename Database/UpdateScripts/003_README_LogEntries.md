# Script Database per Sistema di Logging

## File: 003_AddLogEntriesTable.sql

Questo script SQL crea la tabella `LogEntries` per il sistema di logging dell'applicazione DocN.

### Cosa fa lo script

1. **Crea la tabella LogEntries** con i seguenti campi:
   - `Id`: Chiave primaria (auto-incremento)
   - `Timestamp`: Data e ora del log
   - `Level`: Livello di log (Info, Warning, Error, Debug)
   - `Category`: Categoria del log (Upload, Embedding, AI, Tag, Metadata, etc.)
   - `Message`: Messaggio del log (max 2000 caratteri)
   - `Details`: Dettagli aggiuntivi (illimitato)
   - `UserId`: ID dell'utente (opzionale)
   - `FileName`: Nome del file coinvolto (opzionale)
   - `StackTrace`: Stack trace per errori (opzionale)

2. **Crea 3 indici** per migliorare le performance:
   - Indice su `Timestamp` per query temporali
   - Indice su `Category` + `Timestamp` per filtrare per categoria
   - Indice su `UserId` + `Timestamp` per filtrare per utente

### Come eseguire lo script

#### Opzione 1: SQL Server Management Studio (SSMS)
1. Apri SQL Server Management Studio
2. Connetti al server: `NTSPJ-060-02\SQL2025`
3. Apri il database: `DocumentArchive`
4. Apri il file `Database/UpdateScripts/003_AddLogEntriesTable.sql`
5. Clicca su "Execute" (F5)

#### Opzione 2: Azure Data Studio
1. Apri Azure Data Studio
2. Connetti al server: `NTSPJ-060-02\SQL2025`
3. Seleziona il database: `DocumentArchive`
4. Apri il file `Database/UpdateScripts/003_AddLogEntriesTable.sql`
5. Clicca su "Run" o premi F5

#### Opzione 3: Command Line (sqlcmd)
```bash
sqlcmd -S NTSPJ-060-02\SQL2025 -d DocumentArchive -i Database/UpdateScripts/003_AddLogEntriesTable.sql
```

### Note Importanti

- ✅ Lo script è **idempotente**: può essere eseguito più volte senza problemi
- ✅ Controlla se la tabella esiste prima di crearla
- ✅ Controlla se gli indici esistono prima di crearli
- ✅ Non elimina dati esistenti

### Verifica dell'installazione

Dopo aver eseguito lo script, verifica che la tabella sia stata creata:

```sql
-- Verifica esistenza tabella
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'LogEntries';

-- Verifica struttura tabella
EXEC sp_help 'LogEntries';

-- Verifica indici
EXEC sp_helpindex 'LogEntries';
```

### Alternative: Entity Framework Migration

Se preferisci usare Entity Framework per applicare le migrazioni:

```bash
# Dalla directory DocN.Data
dotnet ef database update --startup-project ../DocN.Server/DocN.Server.csproj --context DocArcContext
```

Questo comando applicherà automaticamente la migration `20251229074500_AddLogEntriesTable`.

## Supporto

Per problemi o domande, contatta l'amministratore del sistema.
