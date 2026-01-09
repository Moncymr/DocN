# Script di Aggiornamento 014: Gruppi Utenti e Condivisione Documenti

## 📋 Panoramica

Questo script aggiunge il supporto per i **gruppi utenti** e la **condivisione documenti con gruppi**, permettendo un controllo granulare dell'accesso ai documenti.

## 🎯 Obiettivo

Implementare un sistema completo di gestione visibilità e condivisione documenti con 4 livelli:

1. **🔒 Privato** - Solo il proprietario
2. **👥 Condiviso** - Utenti o gruppi specifici
3. **🏢 Organizzazione** - Tutti nell'organizzazione
4. **🌐 Pubblico** - Tutti possono accedere

## 📦 Cosa Viene Creato

### Nuove Tabelle

#### 1. UserGroups
Gestisce i gruppi di utenti per organizzare l'accesso ai documenti.

**Campi:**
- `Id` - Chiave primaria
- `Name` - Nome del gruppo (max 200 caratteri)
- `Description` - Descrizione opzionale (max 1000 caratteri)
- `IsActive` - Stato attivo/disattivo
- `CreatedAt` - Data creazione
- `UpdatedAt` - Data ultimo aggiornamento
- `OwnerId` - Proprietario del gruppo (FK su AspNetUsers)
- `TenantId` - Tenant di appartenenza (FK su Tenants)

**Indici:**
- `IX_UserGroups_Name` - Per ricerche veloci per nome
- `IX_UserGroups_OwnerId` - Per filtrare gruppi per proprietario
- `IX_UserGroups_TenantId` - Per isolamento multi-tenant

#### 2. UserGroupMembers
Gestisce i membri di ogni gruppo.

**Campi:**
- `Id` - Chiave primaria
- `GroupId` - Riferimento al gruppo (FK su UserGroups)
- `UserId` - Riferimento all'utente (FK su AspNetUsers)
- `Role` - Ruolo nel gruppo (0=Member, 1=Admin)
- `JoinedAt` - Data di adesione

**Indici:**
- `IX_UserGroupMembers_GroupId_UserId` (UNIQUE) - Previene duplicati
- `IX_UserGroupMembers_UserId` - Per query su utenti

#### 3. DocumentGroupShares
Gestisce la condivisione di documenti con gruppi.

**Campi:**
- `Id` - Chiave primaria
- `DocumentId` - Riferimento al documento (FK su Documents)
- `GroupId` - Riferimento al gruppo (FK su UserGroups)
- `Permission` - Livello permesso (0=Read, 1=Write, 2=Delete)
- `SharedAt` - Data condivisione
- `SharedByUserId` - Chi ha condiviso

**Indici:**
- `IX_DocumentGroupShares_DocumentId_GroupId` (UNIQUE) - Previene duplicati
- `IX_DocumentGroupShares_GroupId` - Per query su gruppi

## 🔒 Sicurezza

### Foreign Keys
- ✅ **CASCADE DELETE** su UserGroupMembers e DocumentGroupShares
- ✅ **SET NULL** su UserGroups per OwnerId e TenantId
- ✅ Integrità referenziale garantita

### Constraint Unique
- ✅ Un utente non può essere membro dello stesso gruppo più volte
- ✅ Un documento non può essere condiviso con lo stesso gruppo più volte

### Multi-Tenancy
- ✅ Supporto completo per TenantId
- ✅ Isolamento tra organizzazioni

## 📊 Livelli di Permesso

### DocumentGroupShares.Permission
- **0 = Read** - Visualizzazione e download
- **1 = Write** - Read + modifica metadati
- **2 = Delete** - Write + eliminazione

### UserGroupMembers.Role
- **0 = Member** - Membro normale
- **1 = Admin** - Può gestire membri

## 🚀 Come Applicare lo Script

### Metodo 1: Entity Framework (Consigliato)
```bash
cd /path/to/DocN
dotnet ef database update --project DocN.Data --startup-project DocN.Server --context ApplicationDbContext
```

### Metodo 2: SQL Diretto
```sql
-- Connettiti al database SQL Server
sqlcmd -S your_server -d DocN_Database -U your_user -P your_password -i 014_AddUserGroupsAndDocumentSharing.sql
```

### Metodo 3: SQL Server Management Studio
1. Apri SSMS
2. Connettiti al database
3. Apri il file `014_AddUserGroupsAndDocumentSharing.sql`
4. Esegui lo script (F5)

## ✅ Verifica

Lo script include query di verifica automatiche che mostrano:
- Numero di record in ogni tabella (dovrebbe essere 0 dopo la creazione)
- Lista degli indici creati
- Conferma completamento

### Query di Verifica Manuale
```sql
-- Verifica esistenza tabelle
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('UserGroups', 'UserGroupMembers', 'DocumentGroupShares');

-- Verifica foreign keys
SELECT 
    fk.name AS ForeignKeyName,
    tp.name AS ParentTable,
    cp.name AS ParentColumn,
    tr.name AS ReferencedTable,
    cr.name AS ReferencedColumn
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.tables AS tp ON fkc.parent_object_id = tp.object_id
INNER JOIN sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
INNER JOIN sys.tables AS tr ON fkc.referenced_object_id = tr.object_id
INNER JOIN sys.columns AS cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
WHERE tp.name IN ('UserGroups', 'UserGroupMembers', 'DocumentGroupShares')
ORDER BY tp.name, fk.name;
```

## 🔄 Rollback

In caso di problemi, eseguire:
```sql
BEGIN TRANSACTION;

-- Rimuovi indici
DROP INDEX IF EXISTS [IX_DocumentGroupShares_DocumentId_GroupId] ON [DocumentGroupShares];
DROP INDEX IF EXISTS [IX_DocumentGroupShares_GroupId] ON [DocumentGroupShares];
DROP INDEX IF EXISTS [IX_UserGroupMembers_GroupId_UserId] ON [UserGroupMembers];
DROP INDEX IF EXISTS [IX_UserGroupMembers_UserId] ON [UserGroupMembers];
DROP INDEX IF EXISTS [IX_UserGroups_Name] ON [UserGroups];
DROP INDEX IF EXISTS [IX_UserGroups_OwnerId] ON [UserGroups];
DROP INDEX IF EXISTS [IX_UserGroups_TenantId] ON [UserGroups];

-- Rimuovi tabelle (ordine importante per foreign keys)
DROP TABLE IF EXISTS [DocumentGroupShares];
DROP TABLE IF EXISTS [UserGroupMembers];
DROP TABLE IF EXISTS [UserGroups];

-- Rimuovi entry dalla migration history
DELETE FROM [__EFMigrationsHistory] 
WHERE [MigrationId] = N'20260108043707_AddUserGroupsAndDocumentGroupShares';

COMMIT;
GO
```

## 📚 Documentazione Correlata

- **DOCUMENT_VISIBILITY_IMPLEMENTATION.md** - Documentazione tecnica completa
- **IMPLEMENTAZIONE_VISIBILITA.md** - Guida utente in italiano
- **SUMMARY_IMPLEMENTATION.md** - Riepilogo implementazione

## 🔗 API Endpoints

Dopo l'applicazione dello script, saranno disponibili questi endpoint:

```
PATCH /documents/{id}/visibility          - Aggiorna visibilità
POST  /documents/{id}/shares/user         - Condividi con utente
POST  /documents/{id}/shares/group        - Condividi con gruppo
GET   /documents/{id}/shares              - Lista condivisioni
DELETE /documents/{id}/shares/user/{id}   - Rimuovi condivisione utente
DELETE /documents/{id}/shares/group/{id}  - Rimuovi condivisione gruppo
```

## ⚠️ Note Importanti

1. **Backup**: Eseguire sempre un backup del database prima di applicare lo script
2. **Ambiente di Test**: Testare prima in un ambiente di sviluppo
3. **Downtime**: Lo script è veloce, ma pianificare una finestra di manutenzione
4. **Dipendenze**: Richiede che le tabelle Documents, AspNetUsers e Tenants esistano già
5. **SQL Server Version**: Richiede SQL Server 2016 o superiore

## 📈 Performance

### Ottimizzazioni Incluse
- ✅ Indici su colonne frequentemente interrogate
- ✅ Indici unique per prevenire duplicati
- ✅ Foreign keys con CASCADE appropriati
- ✅ Supporto per query multi-tenant efficienti

### Impatto Previsto
- Tabelle vuote all'inizio: **impatto minimo**
- Ogni tabella supporta migliaia di record senza degrado prestazioni
- Indici ottimizzati per query comuni

## 🎉 Risultato Finale

Dopo l'applicazione dello script, il sistema supporta:
- ✅ Creazione e gestione gruppi utenti
- ✅ Aggiunta membri ai gruppi con ruoli
- ✅ Condivisione documenti con gruppi
- ✅ Controllo granulare permessi
- ✅ Isolamento multi-tenant
- ✅ UI moderna per gestione visibilità

## 📞 Supporto

Per problemi o domande:
1. Controllare i log SQL Server
2. Verificare permessi utente database
3. Controllare versione SQL Server
4. Consultare la documentazione tecnica

---

**Data Creazione:** 2026-01-08  
**Versione:** 1.0.0  
**Migration ID:** 20260108043707_AddUserGroupsAndDocumentGroupShares  
**Stato:** ✅ Pronto per produzione
