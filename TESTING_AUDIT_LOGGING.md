# Testing Audit Logging - Quick Guide

## Il Problema / The Problem

La tabella AuditLogs era vuota dopo aver caricato un documento perché l'audit logging non era integrato nelle operazioni effettive dell'applicazione.

The AuditLogs table was empty after uploading a document because audit logging was not integrated into the actual application operations.

## La Soluzione / The Solution

Ora l'audit logging è completamente integrato in:

Now audit logging is fully integrated into:

### 1. Operazioni Documento / Document Operations
- ✅ **DocumentUploaded** - Quando carichi un documento / When you upload a document
- ✅ **DocumentDownloaded** - Quando scarichi un documento / When you download a document
- ✅ **DocumentUpdated** - Quando modifichi un documento / When you update a document
- ✅ **DocumentUploadFailed** - Quando il caricamento fallisce / When upload fails
- ✅ **DocumentDownloadDenied** - Quando l'accesso è negato / When access is denied

### 2. Autenticazione / Authentication
- ✅ **UserLogin** - Login riuscito / Successful login
- ✅ **UserLoginFailed** - Login fallito / Failed login
- ✅ **UserLogout** - Logout / Logout

## Come Testare / How to Test

### 1. Aggiorna Database / Update Database

Se hai già il database, applica l'update:
If you already have the database, apply the update:

```sql
-- Usa lo script di update / Use the update script
sqlcmd -S localhost -d DocNDb -U sa -P YourPassword -i Database/UpdateScripts/009_EnhancedAuditLogging.sql
```

O usa EF Core migrations:
Or use EF Core migrations:

```bash
cd DocN.Data
dotnet ef database update --startup-project ../DocN.Client
```

### 2. Riavvia l'Applicazione / Restart the Application

```bash
cd DocN.Client
dotnet run
```

### 3. Testa Upload Documento / Test Document Upload

1. Vai su `/upload`
2. Carica un documento qualsiasi
3. Analizza il documento
4. Salva

### 4. Verifica AuditLogs / Check AuditLogs

```sql
-- Verifica che ci siano record / Check that there are records
SELECT COUNT(*) FROM AuditLogs;

-- Vedi gli upload recenti / See recent uploads
SELECT TOP 10 
    Id,
    Action,
    ResourceType,
    ResourceId,
    Username,
    IpAddress,
    Timestamp,
    Success,
    Details
FROM AuditLogs
ORDER BY Timestamp DESC;

-- Vedi solo gli upload di documenti / See only document uploads
SELECT 
    Action,
    ResourceId,
    Username,
    Timestamp,
    Details
FROM AuditLogs
WHERE Action = 'DocumentUploaded'
ORDER BY Timestamp DESC;

-- Vedi i login / See logins
SELECT 
    Action,
    Username,
    IpAddress,
    Timestamp,
    Success,
    ErrorMessage
FROM AuditLogs
WHERE Action IN ('UserLogin', 'UserLoginFailed', 'UserLogout')
ORDER BY Timestamp DESC;
```

### 5. Usa l'API / Use the API

```bash
# Tutti gli audit logs
curl http://localhost:5000/api/audit

# Filtrati per azione
curl "http://localhost:5000/api/audit?action=DocumentUploaded"

# Filtrati per data
curl "http://localhost:5000/api/audit?startDate=2024-12-30"

# Statistiche
curl http://localhost:5000/api/audit/statistics
```

## Cosa Viene Loggato / What Gets Logged

Ogni log contiene:
Each log contains:

- ✅ **UserId** - ID dell'utente (se autenticato) / User ID (if authenticated)
- ✅ **Username** - Nome utente / Username
- ✅ **Action** - Azione eseguita / Action performed
- ✅ **ResourceType** - Tipo di risorsa (Document, Authentication, etc.)
- ✅ **ResourceId** - ID della risorsa (es. document ID)
- ✅ **Details** - Dettagli in JSON (file size, content type, etc.)
- ✅ **IpAddress** - Indirizzo IP del client / Client IP address
- ✅ **UserAgent** - Browser/client info
- ✅ **Timestamp** - Data/ora UTC
- ✅ **Severity** - Livello (Info, Warning, Error, Critical)
- ✅ **Success** - true/false
- ✅ **ErrorMessage** - Messaggio di errore (se fallito) / Error message (if failed)

## Esempio di Log / Example Log

### Upload Documento Riuscito / Successful Document Upload

```json
{
  "id": 1,
  "userId": "abc123",
  "username": "user@example.com",
  "action": "DocumentUploaded",
  "resourceType": "Document",
  "resourceId": "42",
  "details": "{\"FileName\":\"report.pdf\",\"FileSize\":1024567,\"ContentType\":\"application/pdf\",\"Category\":\"Reports\",\"HasEmbedding\":true}",
  "ipAddress": "192.168.1.100",
  "userAgent": "Mozilla/5.0...",
  "timestamp": "2024-12-30T13:30:00Z",
  "severity": "Info",
  "success": true
}
```

### Login Fallito / Failed Login

```json
{
  "id": 2,
  "username": "user@example.com",
  "action": "UserLoginFailed",
  "resourceType": "Authentication",
  "ipAddress": "192.168.1.100",
  "timestamp": "2024-12-30T13:25:00Z",
  "severity": "Warning",
  "success": false,
  "errorMessage": "Invalid password"
}
```

## Troubleshooting

### Nessun Log Dopo Upload / No Logs After Upload

1. **Verifica database aggiornato:**
   ```sql
   -- Controlla che la tabella esista
   SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AuditLogs';
   
   -- Controlla i campi
   SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS 
   WHERE TABLE_NAME = 'AuditLogs';
   ```

2. **Verifica IAuditService registrato:**
   - Controlla `DocN.Client/Program.cs` 
   - Deve avere: `builder.Services.AddScoped<IAuditService, AuditService>();`
   - Deve avere: `builder.Services.AddHttpContextAccessor();`

3. **Verifica errori nei log:**
   ```bash
   # Guarda i log dell'applicazione
   dotnet run --project DocN.Client
   ```

4. **Test diretto del servizio:**
   ```csharp
   // Nella tua pagina Blazor
   @inject IAuditService AuditService
   
   // Test
   await AuditService.LogAsync("TestAction", "TestResource");
   ```

### Log Senza Utente / Logs Without User

Se `UserId` è `NULL`:
If `UserId` is `NULL`:

- ✅ **Normale per operazioni non autenticate** / Normal for unauthenticated operations
- ✅ **L'IP address viene comunque loggato** / IP address is still logged
- ⚠️ **Verifica autenticazione se dovrebbe esserci un utente** / Check authentication if there should be a user

## Performance

Gli audit logs sono ottimizzati con 8 indici:
Audit logs are optimized with 8 indexes:

- Query veloci su UserId, Action, ResourceType, Timestamp
- Indici compositi per query comuni
- Nessun impatto significativo sulle performance dell'app

## Conformità / Compliance

✅ **GDPR Article 30** - Registro attività di trattamento / Processing activities record  
✅ **SOC2 CC6.2** - Security event logging  
✅ **ISO 27001** - Access control monitoring  

## Commit

Questo fix è nel commit: **841cae0**

Changes:
- `DocN.Data/Services/DocumentService.cs` - Added audit logging to all document operations
- `DocN.Client/Program.cs` - Added IAuditService registration and authentication logging

---

**Adesso l'audit logging funziona! / Now audit logging works!**

Dopo aver caricato un documento, vedrai i record nella tabella AuditLogs.
After uploading a document, you will see records in the AuditLogs table.
