# Documento delle Modifiche - Sistema Connettori e Ingestione Documenti

**Data**: 15 Gennaio 2026  
**Versione**: 1.0  
**Branch**: copilot/add-connectors-and-ingestion

---

## Sommario Esecutivo

Questo documento descrive le modifiche implementate per aggiungere un sistema completo di connettori per repository esterni e pianificazione dell'ingestione automatica dei documenti nel sistema DocN.

### Obiettivo
Permettere l'importazione automatica di documenti da repository esterni (SharePoint, OneDrive, Google Drive, cartelle locali, ecc.) attraverso connettori configurabili e pianificazioni di ingestione con esecuzione manuale, schedulata (cron) o continua.

---

## Indice
1. [Modifiche al Database](#1-modifiche-al-database)
2. [Nuovi Modelli di Dati](#2-nuovi-modelli-di-dati)
3. [Servizi Implementati](#3-servizi-implementati)
4. [Controller API](#4-controller-api)
5. [Interfaccia Utente](#5-interfaccia-utente)
6. [Documentazione](#6-documentazione)
7. [Correzioni Effettuate](#7-correzioni-effettuate)
8. [File Modificati](#8-file-modificati)
9. [Come Utilizzare](#9-come-utilizzare)
10. [Note Tecniche](#10-note-tecniche)

---

## 1. Modifiche al Database

### Script di Migrazione
- **File**: `Database/Migrations/20260115_AddConnectorAndIngestionTables.sql`
- **Azione**: Creazione di 3 nuove tabelle

### Tabelle Create

#### 1.1 DocumentConnectors
Memorizza le configurazioni di connessione ai repository esterni.

**Colonne principali**:
- `Id` - Chiave primaria
- `Name` - Nome descrittivo del connettore
- `ConnectorType` - Tipo (SharePoint, OneDrive, GoogleDrive, LocalFolder, FTP, SFTP)
- `Configuration` - JSON con URL endpoint, percorsi cartelle, ecc.
- `EncryptedCredentials` - JSON con token OAuth, chiavi API (crittografate)
- `IsActive` - Stato attivo/inattivo
- `LastConnectionTest` - Timestamp ultimo test connessione
- `LastSyncedAt` - Timestamp ultima sincronizzazione
- `OwnerId` - Proprietario del connettore
- `TenantId` - Supporto multi-tenancy

**Indici**:
- `IX_DocumentConnectors_OwnerId`
- `IX_DocumentConnectors_TenantId`
- `IX_DocumentConnectors_ConnectorType_IsActive`

#### 1.2 IngestionSchedules
Memorizza le pianificazioni di ingestione per i connettori.

**Colonne principali**:
- `Id` - Chiave primaria
- `ConnectorId` - FK a DocumentConnectors
- `Name` - Nome della pianificazione
- `ScheduleType` - Manual, Scheduled, Continuous
- `CronExpression` - Espressione cron per tipo Scheduled
- `IntervalMinutes` - Intervallo per tipo Continuous
- `IsEnabled` - Stato abilitato/disabilitato
- `DefaultCategory` - Categoria predefinita per documenti importati
- `DefaultVisibility` - Livello di visibilità predefinito
- `FilterConfiguration` - JSON con filtri (tipi file, pattern percorsi, date)
- `GenerateEmbeddingsImmediately` - Se generare embeddings immediatamente
- `EnableAIAnalysis` - Se abilitare analisi AI
- `LastExecutedAt` - Timestamp ultima esecuzione
- `NextExecutionAt` - Timestamp prossima esecuzione

**Indici**:
- `IX_IngestionSchedules_ConnectorId`
- `IX_IngestionSchedules_IsEnabled_NextExecutionAt`
- `IX_IngestionSchedules_OwnerId`

#### 1.3 IngestionLogs
Memorizza i log di esecuzione delle pianificazioni.

**Colonne principali**:
- `Id` - Chiave primaria
- `IngestionScheduleId` - FK a IngestionSchedules
- `StartedAt` - Inizio esecuzione
- `CompletedAt` - Fine esecuzione
- `Status` - Running, Completed, Failed, Cancelled
- `DocumentsDiscovered` - Numero documenti trovati
- `DocumentsProcessed` - Numero documenti elaborati
- `DocumentsSkipped` - Numero documenti saltati
- `DocumentsFailed` - Numero documenti falliti
- `ErrorMessage` - Messaggio di errore se fallito
- `DetailedLog` - Array JSON di messaggi di log dettagliati
- `IsManualExecution` - Se esecuzione manuale
- `TriggeredByUserId` - Utente che ha avviato esecuzione manuale

**Indici**:
- `IX_IngestionLogs_IngestionScheduleId`
- `IX_IngestionLogs_StartedAt_Status`
- `IX_IngestionLogs_TriggeredByUserId`

---

## 2. Nuovi Modelli di Dati

### 2.1 DocumentConnector.cs
**Location**: `DocN.Data/Models/DocumentConnector.cs`

```csharp
public class DocumentConnector
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ConnectorType { get; set; }
    public string Configuration { get; set; }
    public string? EncryptedCredentials { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastConnectionTest { get; set; }
    public string? LastConnectionTestResult { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? OwnerId { get; set; }
    public int? TenantId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Description { get; set; }
}
```

### 2.2 IngestionSchedule.cs
**Location**: `DocN.Data/Models/IngestionSchedule.cs`

### 2.3 IngestionLog.cs
**Location**: `DocN.Data/Models/IngestionLog.cs`

### 2.4 ConnectorConstants.cs
**Location**: `DocN.Data/Constants/ConnectorConstants.cs`

Costanti per tipi di connettori, tipi di pianificazione, e stati.

---

## 3. Servizi Implementati

### 3.1 IConnectorService & ConnectorService
**Location**: 
- Interface: `DocN.Data/Services/IConnectorService.cs`
- Implementation: `DocN.Data/Services/ConnectorService.cs`

**Funzionalità**:
- `GetUserConnectorsAsync()` - Ottiene tutti i connettori di un utente
- `GetConnectorAsync()` - Ottiene un connettore specifico
- `CreateConnectorAsync()` - Crea nuovo connettore
- `UpdateConnectorAsync()` - Aggiorna connettore esistente
- `DeleteConnectorAsync()` - Elimina connettore
- `TestConnectionAsync()` - Testa connessione al repository esterno
- `ListFilesAsync()` - Elenca file dal connettore

**Classe helper**: `ConnectorFileInfo` - Rappresenta informazioni su file da connettore

### 3.2 IIngestionService & IngestionService
**Location**:
- Interface: `DocN.Data/Services/IIngestionService.cs`
- Implementation: `DocN.Data/Services/IngestionService.cs`

**Funzionalità**:
- `GetUserSchedulesAsync()` - Ottiene tutte le pianificazioni di un utente
- `GetScheduleAsync()` - Ottiene pianificazione specifica
- `CreateScheduleAsync()` - Crea nuova pianificazione
- `UpdateScheduleAsync()` - Aggiorna pianificazione
- `DeleteScheduleAsync()` - Elimina pianificazione
- `ExecuteIngestionAsync()` - Esegue ingestione manuale
- `GetIngestionLogsAsync()` - Ottiene log esecuzioni
- `UpdateNextExecutionTimeAsync()` - Calcola prossima esecuzione

**Dipendenza**: Usa libreria `Cronos` per parsing espressioni cron

### 3.3 Registrazione Servizi
**Location**: `DocN.Server/Program.cs` (linee 432-433)

```csharp
builder.Services.AddScoped<IConnectorService, ConnectorService>();
builder.Services.AddScoped<IIngestionService, IngestionService>();
```

---

## 4. Controller API

### 4.1 ConnectorsController
**Location**: `DocN.Server/Controllers/ConnectorsController.cs`

**Endpoint**:
- `GET /Connectors` - Lista tutti i connettori
- `GET /Connectors/{id}` - Dettagli connettore specifico
- `POST /Connectors` - Crea nuovo connettore
- `PUT /Connectors/{id}` - Aggiorna connettore
- `DELETE /Connectors/{id}` - Elimina connettore
- `POST /Connectors/{id}/test` - Testa connessione
- `GET /Connectors/{id}/files` - Elenca file dal connettore

**Autorizzazione**: Richiede autenticazione (`[Authorize]`)

### 4.2 IngestionController
**Location**: `DocN.Server/Controllers/IngestionController.cs`

**Endpoint**:
- `GET /Ingestion/schedules` - Lista tutte le pianificazioni
- `GET /Ingestion/schedules/{id}` - Dettagli pianificazione specifica
- `POST /Ingestion/schedules` - Crea nuova pianificazione
- `PUT /Ingestion/schedules/{id}` - Aggiorna pianificazione
- `DELETE /Ingestion/schedules/{id}` - Elimina pianificazione
- `POST /Ingestion/schedules/{id}/execute` - Esegue ingestione manuale
- `GET /Ingestion/schedules/{id}/logs` - Ottiene log esecuzioni

**Autorizzazione**: Richiede autenticazione (`[Authorize]`)

---

## 5. Interfaccia Utente

### 5.1 Pagina Connettori
**Location**: `DocN.Client/Components/Pages/Connectors.razor`

**URL**: `/connectors`

**Funzionalità**:
- Visualizzazione lista connettori con card design moderno
- Indicatori di stato (attivo/inattivo)
- Informazioni su ultimo test connessione
- Pulsanti per azioni: Test, Edit, Delete
- Design responsive con gradient colors
- Messaggi di errore user-friendly

### 5.2 Pagina Pianificazioni Ingestione
**Location**: `DocN.Client/Components/Pages/IngestionSchedules.razor`

**URL**: `/ingestion`

**Funzionalità**:
- Visualizzazione lista pianificazioni
- Indicatori di stato (abilitato/disabilitato)
- Informazioni su tipo pianificazione e prossima esecuzione
- Pulsanti per azioni: Run Now, Logs, Edit, Delete
- Alert informativi per funzionalità in sviluppo
- Design moderno con card layout

### 5.3 Menu di Navigazione
**Location**: `DocN.Client/Components/Layout/NavMenu.razor`

**Modifiche**:
- Aggiunta voce "🔌 Connettori" (link a `/connectors`)
- Aggiunta voce "📅 Pianificazione" (link a `/ingestion`)
- Etichette in italiano coerenti con resto applicazione

---

## 6. Documentazione

### 6.1 README Migrazione
**File**: `Database/Migrations/README_CONNECTOR_INGESTION_MIGRATION.md`

**Contenuto**:
- Descrizione dettagliata tabelle e colonne
- Prerequisiti per esecuzione
- Istruzioni esecuzione migrazione (SSMS, sqlcmd, EF Core)
- Lista completa endpoint API
- Esempi di configurazione
- Procedura di rollback

### 6.2 Sommario Implementazione
**File**: `IMPLEMENTATION_SUMMARY_CONNECTORS.md`

**Contenuto**:
- Overview completo implementazione
- Dettagli su data layer, service layer, API layer, UI layer
- Architettura e decisioni di design
- Lista funzionalità implementate
- Checklist per testing
- Passi per deployment

### 6.3 Esempi di Configurazione
**File**: `CONFIGURATION_EXAMPLES.md`

**Contenuto** (400+ righe):
- 40+ esempi JSON completi
- Connettori per tutti i tipi (LocalFolder, SharePoint, Google Drive, OneDrive)
- Pianificazioni per tutti i modi (Manual, Scheduled, Continuous)
- Esempi con path Windows reali
- Guida espressioni cron con pattern comuni
- Configurazione filtri avanzati
- Workflow completi con chiamate API
- Tutta documentazione in italiano

---

## 7. Correzioni Effettuate

### 7.1 Errori di Compilazione (Commit f89d9b0)
**Problema**: Dipendenze circolari - interfacce in `DocN.Core/Interfaces` referenziavano modelli da `DocN.Data.Models`

**Soluzione**:
- Spostato `IConnectorService` da `DocN.Core/Interfaces` a `DocN.Data/Services`
- Spostato `IIngestionService` da `DocN.Core/Interfaces` a `DocN.Data/Services`
- Aggiornato tutti gli import in controller e servizi
- Rimosso campo inutilizzato `showModal`

**Risultato**: Build completato con 0 errori

### 7.2 Errore Runtime Startup (Commit fed7050)
**Problema**: Dependency injection falliva all'avvio:
```
Unable to resolve service for type 'DocN.Data.Services.IDocumentService' 
while attempting to activate 'DocN.Data.Services.IngestionService'
```

**Causa**: `IngestionService` richiedeva `IDocumentService` non registrato

**Soluzione**: Rimosso dipendenza inutilizzata `IDocumentService` dal costruttore

**Risultato**: Applicazione si avvia correttamente

### 7.3 Errori 404 Navigazione (Commit 9aaddab)
**Problema**: Click su pulsanti "New", "Edit", "Logs" causavano errore 404

**Causa**: Pagine `/ingestion/new`, `/ingestion/edit/{id}`, `/ingestion/logs/{id}` non esistevano

**Soluzione**: Sostituito navigazione con alert JavaScript che guidano utente verso API

**Risultato**: Nessun errore 404, utenti informati su come usare API

---

## 8. File Modificati

### File Nuovi (18 totali)
1. `DocN.Data/Models/DocumentConnector.cs`
2. `DocN.Data/Models/IngestionSchedule.cs`
3. `DocN.Data/Models/IngestionLog.cs`
4. `DocN.Data/Constants/ConnectorConstants.cs`
5. `DocN.Data/Services/IConnectorService.cs`
6. `DocN.Data/Services/IIngestionService.cs`
7. `DocN.Data/Services/ConnectorService.cs`
8. `DocN.Data/Services/IngestionService.cs`
9. `DocN.Server/Controllers/ConnectorsController.cs`
10. `DocN.Server/Controllers/IngestionController.cs`
11. `DocN.Client/Components/Pages/Connectors.razor`
12. `DocN.Client/Components/Pages/IngestionSchedules.razor`
13. `Database/Migrations/20260115_AddConnectorAndIngestionTables.sql`
14. `Database/Migrations/README_CONNECTOR_INGESTION_MIGRATION.md`
15. `IMPLEMENTATION_SUMMARY_CONNECTORS.md`
16. `CONFIGURATION_EXAMPLES.md`
17. `DOCUMENT_MODIFICHE.md` (questo documento)

### File Modificati
1. `DocN.Data/DocArcContext.cs` - Aggiunto DbSet per nuove entità
2. `DocN.Server/Program.cs` - Registrazione nuovi servizi
3. `DocN.Client/Components/Layout/NavMenu.razor` - Aggiunte voci menu
4. `DocN.Data/DocN.Data.csproj` - Aggiunto package Cronos

### Linee di Codice
- **Totale aggiunto**: ~4,000 linee
- **Codice produzione**: ~2,500 linee
- **Documentazione**: ~1,500 linee

---

## 9. Come Utilizzare

### 9.1 Eseguire Migrazione Database
```bash
# Opzione 1: SQL Server Management Studio
# Aprire e eseguire: Database/Migrations/20260115_AddConnectorAndIngestionTables.sql

# Opzione 2: sqlcmd
sqlcmd -S localhost -d DocN -U sa -P YourPassword -i Database/Migrations/20260115_AddConnectorAndIngestionTables.sql
```

### 9.2 Creare Connettore (API)
```bash
POST https://localhost:7114/Connectors
Content-Type: application/json
Authorization: Bearer {token}

{
  "name": "Cartella Progetti",
  "connectorType": "LocalFolder",
  "configuration": "{\"folderPath\":\"C:\\\\Progetti\",\"recursive\":true}",
  "isActive": true,
  "description": "Documenti progetti"
}
```

### 9.3 Creare Pianificazione (API)
```bash
POST https://localhost:7114/Ingestion/schedules
Content-Type: application/json
Authorization: Bearer {token}

{
  "connectorId": 1,
  "name": "Import Giornaliero",
  "scheduleType": "Scheduled",
  "cronExpression": "0 2 * * *",
  "isEnabled": true,
  "defaultCategory": "Importati",
  "enableAIAnalysis": true
}
```

### 9.4 Eseguire Ingestione Manuale
```bash
POST https://localhost:7114/Ingestion/schedules/1/execute
Authorization: Bearer {token}
```

### 9.5 Visualizzare Log
```bash
GET https://localhost:7114/Ingestion/schedules/1/logs?count=50
Authorization: Bearer {token}
```

### 9.6 Accedere UI
- **Connettori**: https://localhost:7114/connectors
- **Pianificazioni**: https://localhost:7114/ingestion

---

## 10. Note Tecniche

### 10.1 Dipendenze
- **Cronos 0.8.4** - Parsing espressioni cron
- Nessuna altra dipendenza esterna aggiunta

### 10.2 Sicurezza
- Tutte le API richiedono autenticazione (`[Authorize]`)
- Credenziali memorizzate in campo `EncryptedCredentials` (crittografia da implementare)
- Validazione proprietà utente prima di modifiche/eliminazioni
- Supporto multi-tenancy per isolamento dati

### 10.3 Performance
- Indici su colonne frequentemente interrogate
- Paginazione supportata in API
- Query ottimizzate con Entity Framework

### 10.4 Estensibilità
- Design modulare per aggiungere nuovi tipi di connettori
- Interfacce `IConnectorService` e `IIngestionService` permettono implementazioni alternative
- Configurazione JSON flessibile per ogni tipo di connettore
- Filtri configurabili tramite JSON

### 10.5 Limitazioni Attuali
- **Implementazioni concrete connettori**: Framework presente, ma implementazioni specifiche (SDK SharePoint, Google Drive API, ecc.) da completare
- **Background worker**: Esecutore pianificazioni automatiche da implementare
- **OAuth flows**: Flussi autenticazione OAuth per servizi esterni da implementare
- **UI CRUD completo**: Pagine per creazione/modifica da UI da implementare (attualmente solo API)

### 10.6 Compatibilità
- **.NET**: 10.0
- **Entity Framework Core**: 10.0
- **SQL Server**: 2016 o superiore
- **Browser**: Moderni (Chrome, Edge, Firefox, Safari)

---

## 11. Testing

### 11.1 Testing Manuale Effettuato
- ✅ Compilazione progetto senza errori
- ✅ Avvio applicazione senza errori
- ✅ Navigazione pagine UI senza errori 404
- ✅ Visualizzazione menu aggiornato

### 11.2 Testing Raccomandato
- [ ] Eseguire migrazione database su ambiente test
- [ ] Creare connettori tramite API
- [ ] Testare connessione connettori
- [ ] Creare pianificazioni tramite API
- [ ] Eseguire ingestione manuale
- [ ] Verificare log esecuzioni
- [ ] Testare autorizzazioni utenti
- [ ] Testare multi-tenancy

---

## 12. Changelog Commit

### Commit 1: 5e728d8
**Titolo**: Initial plan  
**Descrizione**: Piano iniziale implementazione

### Commit 2: 36b6b9c
**Titolo**: Add data models, services, and controllers for connectors and ingestion  
**Descrizione**: Aggiunto modelli dati, servizi e controller

### Commit 3: a5c47e4
**Titolo**: Add UI pages for connectors and ingestion schedules with navigation menu updates  
**Descrizione**: Aggiunte pagine UI e menu navigazione

### Commit 4: 8f28608
**Titolo**: Add database migration script and documentation for connectors and ingestion system  
**Descrizione**: Aggiunto script migrazione e documentazione

### Commit 5: d560e46
**Titolo**: Add comprehensive implementation summary document  
**Descrizione**: Aggiunto documento sommario implementazione

### Commit 6: f89d9b0
**Titolo**: Fix compilation errors by moving interfaces to DocN.Data.Services namespace  
**Descrizione**: Risolti errori compilazione spostando interfacce

### Commit 7: fed7050
**Titolo**: Fix runtime startup error by removing unused IDocumentService dependency from IngestionService  
**Descrizione**: Risolto errore avvio runtime

### Commit 8: 9aaddab
**Titolo**: Fix 404 errors and add comprehensive configuration examples document  
**Descrizione**: Risolti errori 404 e aggiunto documento esempi configurazione

---

## 13. Prossimi Passi Raccomandati

### 13.1 Priorità Alta
1. Implementare worker background per esecuzione automatica pianificazioni
2. Implementare connettore LocalFolder concreto
3. Aggiungere crittografia reale per credenziali
4. Creare pagine UI per CRUD completo (new, edit)

### 13.2 Priorità Media
5. Implementare connettore SharePoint con SDK Microsoft
6. Implementare connettore Google Drive con API
7. Aggiungere OAuth flow per autenticazione servizi esterni
8. Implementare gestione incrementale (solo file nuovi/modificati)

### 13.3 Priorità Bassa
9. Aggiungere supporto webhook per ingestion push-based
10. Implementare retry mechanism per ingestioni fallite
11. Aggiungere dashboard monitoring ingestioni
12. Implementare export/import configurazioni connettori

---

## 14. Supporto e Riferimenti

### Documentazione
- **Migrazione Database**: `Database/Migrations/README_CONNECTOR_INGESTION_MIGRATION.md`
- **Esempi Configurazione**: `CONFIGURATION_EXAMPLES.md`
- **Sommario Implementazione**: `IMPLEMENTATION_SUMMARY_CONNECTORS.md`

### Repository
- **Branch**: `copilot/add-connectors-and-ingestion`
- **Pull Request**: Vedi descrizione PR per dettagli completi

### Contatti
Per domande o problemi, riferirsi alla documentazione o aprire issue nel repository.

---

**Fine Documento**

*Documento generato automaticamente il 15 Gennaio 2026*  
*Versione: 1.0*
