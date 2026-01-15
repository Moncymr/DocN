# Esempi di Configurazione - Connettori e Pianificazione

## Indice
- [Connettori](#connettori)
  - [Cartella Locale](#cartella-locale)
  - [SharePoint](#sharepoint)
  - [Google Drive](#google-drive)
  - [OneDrive](#onedrive)
- [Pianificazioni](#pianificazioni)
  - [Manuale](#pianificazione-manuale)
  - [Schedulata (Cron)](#pianificazione-schedulata-cron)
  - [Continua](#pianificazione-continua)

---

## Connettori

### Cartella Locale

**Esempio base:**
```json
{
  "name": "Cartella Documenti Lavoro",
  "connectorType": "LocalFolder",
  "configuration": "{\"folderPath\":\"C:\\\\Documents\\\\Work\",\"recursive\":true,\"filePattern\":\"*.*\"}",
  "isActive": true,
  "description": "Cartella documenti di lavoro"
}
```

**Esempio con path complesso (come richiesto):**
```json
{
  "name": "Categoria 3 - Cartella Business",
  "connectorType": "LocalFolder",
  "configuration": "{\"folderPath\":\"C:\\\\BusinessFile\\\\Lavoro\\\\Cartella Indicizzati\\\\Categoria_3\\\\1\",\"recursive\":true,\"filePattern\":\"*.pdf,*.docx,*.xlsx\"}",
  "isActive": true,
  "description": "Documenti business - Categoria 3, sottocartella 1"
}
```

**Configurazione dettagliata:**
```json
{
  "folderPath": "C:\\BusinessFile\\Lavoro\\Cartella Indicizzati\\Categoria_3\\1",
  "recursive": true,
  "filePattern": "*.pdf,*.docx,*.xlsx,*.txt",
  "excludePattern": "~*,*.tmp",
  "minFileSizeBytes": 1024,
  "maxFileSizeBytes": 52428800
}
```

**Note per Windows:**
- Usare `\\\\` (doppio backslash) nei path JSON
- `recursive: true` = include sottocartelle
- `filePattern` = filtro per estensioni (separati da virgola)

---

### SharePoint

**Esempio base:**
```json
{
  "name": "SharePoint Aziendale",
  "connectorType": "SharePoint",
  "configuration": "{\"siteUrl\":\"https://yourtenant.sharepoint.com\",\"folderPath\":\"/Shared Documents\",\"clientId\":\"your-app-client-id\",\"tenantId\":\"your-tenant-id\"}",
  "encryptedCredentials": "{\"clientSecret\":\"your-encrypted-secret\"}",
  "isActive": true,
  "description": "Libreria documenti condivisi SharePoint"
}
```

**Configurazione dettagliata:**
```json
{
  "siteUrl": "https://yourtenant.sharepoint.com",
  "folderPath": "/Shared Documents/Projects",
  "clientId": "12345678-1234-1234-1234-123456789abc",
  "tenantId": "87654321-4321-4321-4321-cba987654321",
  "recursive": true,
  "fileTypes": ["pdf", "docx", "xlsx", "pptx"]
}
```

---

### Google Drive

**Esempio base:**
```json
{
  "name": "Google Drive Aziendale",
  "connectorType": "GoogleDrive",
  "configuration": "{\"folderId\":\"1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms\",\"serviceAccountEmail\":\"service@project.iam.gserviceaccount.com\"}",
  "encryptedCredentials": "{\"serviceAccountKey\":\"your-encrypted-json-key\"}",
  "isActive": true,
  "description": "Cartella principale Google Drive"
}
```

**Configurazione dettagliata:**
```json
{
  "folderId": "1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms",
  "serviceAccountEmail": "docn-connector@project-id.iam.gserviceaccount.com",
  "recursive": true,
  "mimeTypes": [
    "application/pdf",
    "application/vnd.google-apps.document",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
  ]
}
```

---

### OneDrive

**Esempio base:**
```json
{
  "name": "OneDrive Personale",
  "connectorType": "OneDrive",
  "configuration": "{\"folderPath\":\"/Documents\",\"clientId\":\"your-app-id\",\"tenantId\":\"your-tenant-id\"}",
  "encryptedCredentials": "{\"refreshToken\":\"your-encrypted-token\"}",
  "isActive": true,
  "description": "Cartella Documenti OneDrive"
}
```

---

## Pianificazioni

### Pianificazione Manuale

**Esempio base:**
```json
{
  "connectorId": 1,
  "name": "Import Manuale Documenti",
  "scheduleType": "Manual",
  "isEnabled": true,
  "defaultCategory": "Documenti Manuali",
  "defaultVisibility": 0,
  "enableAIAnalysis": true,
  "generateEmbeddingsImmediately": false,
  "description": "Esecuzione manuale quando necessario"
}
```

---

### Pianificazione Schedulata (Cron)

**Ogni giorno alle 00:00:**
```json
{
  "connectorId": 1,
  "name": "Import Giornaliero Notturno",
  "scheduleType": "Scheduled",
  "cronExpression": "0 0 * * *",
  "isEnabled": true,
  "defaultCategory": "Import Automatico",
  "defaultVisibility": 0,
  "enableAIAnalysis": true,
  "generateEmbeddingsImmediately": false
}
```

**Ogni ora:**
```json
{
  "connectorId": 1,
  "name": "Sync Orario",
  "scheduleType": "Scheduled",
  "cronExpression": "0 * * * *",
  "isEnabled": true,
  "defaultCategory": "Sync Orario",
  "enableAIAnalysis": true
}
```

**Ogni lunedì alle 08:00:**
```json
{
  "connectorId": 1,
  "name": "Sync Settimanale",
  "scheduleType": "Scheduled",
  "cronExpression": "0 8 * * 1",
  "isEnabled": true,
  "defaultCategory": "Import Settimanale",
  "enableAIAnalysis": true
}
```

**Ogni 15 minuti (durante l'orario lavorativo 8-18):**
```json
{
  "connectorId": 1,
  "name": "Sync Frequente Orario Lavorativo",
  "scheduleType": "Scheduled",
  "cronExpression": "*/15 8-18 * * 1-5",
  "isEnabled": true,
  "defaultCategory": "Sync Rapido",
  "enableAIAnalysis": true
}
```

**Riferimento Cron Expression:**
```
* * * * *
│ │ │ │ │
│ │ │ │ └─── Giorno della settimana (0-6, 0=Domenica, 1=Lunedì, ..., 6=Sabato)
│ │ │ └───── Mese (1-12)
│ │ └─────── Giorno del mese (1-31)
│ └───────── Ora (0-23)
└─────────── Minuto (0-59)
```

Esempi comuni:
- `0 0 * * *` - Ogni giorno a mezzanotte
- `0 */6 * * *` - Ogni 6 ore
- `30 9 * * 1-5` - Alle 9:30 dal lunedì al venerdì
- `0 0 1 * *` - Il primo giorno di ogni mese a mezzanotte

---

### Pianificazione Continua

**Ogni 30 minuti:**
```json
{
  "connectorId": 1,
  "name": "Sync Continuo (30 min)",
  "scheduleType": "Continuous",
  "intervalMinutes": 30,
  "isEnabled": true,
  "defaultCategory": "Sync Automatico",
  "defaultVisibility": 0,
  "enableAIAnalysis": true,
  "generateEmbeddingsImmediately": false
}
```

**Ogni 5 minuti (monitoring intensivo):**
```json
{
  "connectorId": 1,
  "name": "Monitoring Rapido",
  "scheduleType": "Continuous",
  "intervalMinutes": 5,
  "isEnabled": true,
  "defaultCategory": "Hot Folder",
  "enableAIAnalysis": true,
  "generateEmbeddingsImmediately": true
}
```

**Ogni 2 ore:**
```json
{
  "connectorId": 1,
  "name": "Sync Bi-Orario",
  "scheduleType": "Continuous",
  "intervalMinutes": 120,
  "isEnabled": true,
  "defaultCategory": "Sync Periodico",
  "enableAIAnalysis": true
}
```

---

## Configurazione Filtri Avanzati

**FilterConfiguration (JSON dentro JSON):**
```json
{
  "connectorId": 1,
  "name": "Import con Filtri",
  "scheduleType": "Scheduled",
  "cronExpression": "0 0 * * *",
  "isEnabled": true,
  "filterConfiguration": "{\"fileTypes\":[\"pdf\",\"docx\",\"xlsx\"],\"excludePatterns\":[\"~*\",\"*.tmp\",\"*_backup*\"],\"minSizeBytes\":1024,\"maxSizeBytes\":52428800,\"modifiedAfter\":\"2024-01-01T00:00:00Z\"}",
  "defaultCategory": "Documenti Filtrati",
  "enableAIAnalysis": true
}
```

**Dettaglio FilterConfiguration:**
```json
{
  "fileTypes": ["pdf", "docx", "xlsx", "pptx", "txt"],
  "excludePatterns": ["~*", "*.tmp", "*_backup*", "*.old"],
  "minSizeBytes": 1024,
  "maxSizeBytes": 52428800,
  "modifiedAfter": "2024-01-01T00:00:00Z",
  "pathIncludes": ["importante", "progetti"],
  "pathExcludes": ["archivio", "cestino", "temp"]
}
```

---

## Valori per defaultVisibility

- `0` = **Private** - Solo il proprietario
- `1` = **Shared** - Condiviso con utenti specifici
- `2` = **Organization** - Visibile a tutta l'organizzazione
- `3` = **Public** - Pubblico per tutti

---

## Esempio Completo: Workflow Tipico

### 1. Creare il Connettore
```bash
POST /Connectors
Content-Type: application/json

{
  "name": "Cartella Progetti 2024",
  "connectorType": "LocalFolder",
  "configuration": "{\"folderPath\":\"C:\\\\Progetti\\\\2024\",\"recursive\":true,\"filePattern\":\"*.pdf,*.docx\"}",
  "isActive": true,
  "description": "Documenti progetti anno 2024"
}
```

### 2. Testare la Connessione
```bash
POST /Connectors/1/test
```

### 3. Creare la Pianificazione
```bash
POST /Ingestion/schedules
Content-Type: application/json

{
  "connectorId": 1,
  "name": "Import Giornaliero Progetti",
  "scheduleType": "Scheduled",
  "cronExpression": "0 2 * * *",
  "isEnabled": true,
  "defaultCategory": "Progetti 2024",
  "defaultVisibility": 2,
  "enableAIAnalysis": true,
  "generateEmbeddingsImmediately": false,
  "description": "Import automatico alle 2 AM ogni giorno"
}
```

### 4. Esecuzione Manuale (Test)
```bash
POST /Ingestion/schedules/1/execute
```

### 5. Monitorare i Log
```bash
GET /Ingestion/schedules/1/logs?count=50
```

---

## Note Importanti

1. **Path Windows**: Sempre usare `\\\\` (doppio backslash) nei JSON
2. **Credentials**: Le credenziali vengono crittografate automaticamente dal sistema
3. **Cron vs Continuous**: 
   - Cron = orari specifici e precisi
   - Continuous = polling continuo a intervalli
4. **AI Analysis**: Se `enableAIAnalysis = true`, il sistema analizza automaticamente i documenti
5. **Embeddings**: Se `generateEmbeddingsImmediately = true`, genera embeddings durante l'import (più lento ma immediato)

---

## Supporto

Per ulteriori informazioni:
- **Database Migration**: `Database/Migrations/README_CONNECTOR_INGESTION_MIGRATION.md`
- **Implementation Details**: `IMPLEMENTATION_SUMMARY_CONNECTORS.md`
- **API Documentation**: Vedi PR description per lista completa endpoint
