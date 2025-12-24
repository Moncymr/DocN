# 📘 DocN - Guida Completa Installazione e Configurazione

## 🎯 Panoramica

DocN è un sistema RAG (Retrieval Augmented Generation) aziendale che utilizza le ultime tecnologie Microsoft:
- **.NET 10.0** - Framework moderno
- **Microsoft Semantic Kernel** - Orchestrazione AI
- **SQL Server 2025** - Database con supporto vettoriale nativo
- **Blazor** - UI web moderna

---

## 📋 Prerequisiti

### Software Richiesto

1. **Visual Studio 2022** (v17.12+) oppure **Visual Studio Code** con C# Dev Kit
   - Download: https://visualstudio.microsoft.com/

2. **.NET 10.0 SDK** (Preview)
   - Download: https://dotnet.microsoft.com/download/dotnet/10.0
   - Verifica installazione: `dotnet --version`

3. **SQL Server 2025** (Preview con supporto VECTOR)
   - Download: https://www.microsoft.com/sql-server/sql-server-downloads
   - Oppure: SQL Server LocalDB per sviluppo locale

4. **Git** (per clonare il repository)
   - Download: https://git-scm.com/downloads

### Servizi Azure (Opzionali ma Raccomandati)

5. **Azure OpenAI Service**
   - Portal: https://portal.azure.com
   - Necessario per funzionalità AI (embeddings, chat completion)

6. **Redis** (Opzionale, per caching distribuito)
   - Download: https://redis.io/download
   - Oppure: Azure Cache for Redis

---

## 🚀 Installazione Passo-Passo

### Passo 1: Clonare il Repository

```bash
# Apri il terminale/PowerShell
cd C:\Projects  # O la cartella dove vuoi il progetto

# Clona il repository
git clone https://github.com/Moncymr/DocN.git
cd DocN
```

### Passo 2: Installare le Dipendenze

```bash
# Ripristina i pacchetti NuGet
dotnet restore

# Verifica che tutto sia OK
dotnet build
```

**Output atteso:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Passo 3: Configurare il Database

#### Opzione A: SQL Server LocalDB (Sviluppo)

```bash
# Verifica che SQL Server LocalDB sia installato
sqllocaldb info

# Se non c'è, crealo
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

#### Opzione B: SQL Server 2025 (Produzione)

1. Installa SQL Server 2025 Preview
2. Crea un database vuoto chiamato `DocNDb`
3. Annota la stringa di connessione

### Passo 4: Configurare appsettings.json

Crea il file `DocN.Client/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.SemanticKernel": "Debug"
    }
  },
  "AllowedHosts": "*",
  
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true",
    "Redis": null
  },
  
  "FileStorage": {
    "UploadPath": "C:\\DocNData\\Uploads"
  },
  
  "AzureOpenAI": {
    "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
    "ApiKey": "YOUR-API-KEY-HERE",
    "ChatDeploymentName": "gpt-4",
    "EmbeddingDeploymentName": "text-embedding-ada-002"
  },
  
  "OpenTelemetry": {
    "Enabled": false,
    "Endpoint": "http://localhost:4317"
  }
}
```

#### 🔑 Ottenere le Credenziali Azure OpenAI

**📖 Per una guida completa e dettagliata sulla configurazione delle chiavi API, consulta:**
**[CONFIGURAZIONE_API_KEYS.md](CONFIGURAZIONE_API_KEYS.md)**

**Guida rapida:**

1. Vai su [Azure Portal](https://portal.azure.com)
2. Crea una risorsa "Azure OpenAI"
3. Vai su "Keys and Endpoint"
4. Copia:
   - **Endpoint**: https://your-resource.openai.azure.com/
   - **Key**: La chiave API
5. Crea i deployment:
   - **GPT-4** per chat (nome: `gpt-4`)
   - **text-embedding-ada-002** per embeddings (nome: `text-embedding-ada-002`)

### Passo 5: Creare la Directory Upload

```bash
# Windows
mkdir C:\DocNData\Uploads

# Linux/Mac
mkdir -p ~/DocNData/Uploads
```

Oppure cambia il path in `appsettings.json` se preferisci un'altra posizione.

### Passo 6: Applicare le Migrazioni Database

```bash
# Torna alla cartella root del progetto
cd DocN

# Crea la migrazione iniziale (solo se non esiste già)
dotnet ef migrations add InitialCreate --project DocN.Data/DocN.Data.csproj --startup-project DocN.Client/DocN.Client.csproj

# Applica le migrazioni al database
dotnet ef database update --project DocN.Data/DocN.Data.csproj --startup-project DocN.Client/DocN.Client.csproj
```

**Output atteso:**
```
Applying migration '20250101000000_InitialCreate'.
Done.
```

> **Nota**: L'applicazione applica automaticamente le migrazioni all'avvio, ma è meglio farlo manualmente la prima volta per verificare che tutto funzioni.

### Passo 7: Eseguire l'Applicazione

```bash
cd DocN.Client
dotnet run
```

**Output atteso:**
```
🚀 Avvio DocN - Sistema RAG Aziendale
✅ Connessione database stabilita
✅ Database già aggiornato
⚡ Configurazione Microsoft Semantic Kernel...
✅ Semantic Memory configurata
✨ DocN avviato con successo!
🌐 URL: http://localhost:5000, https://localhost:5001
```

### Passo 8: Accedere all'Applicazione

1. Apri il browser
2. Vai su: `http://localhost:5000` o `https://localhost:5001`
3. Dovresti vedere la homepage di DocN

---

## 👤 Primo Accesso - Creare un Account

### Registrazione

1. Clicca su **"Register"** nella homepage
2. Compila il form:
   - **First Name**: Il tuo nome
   - **Last Name**: Il tuo cognome
   - **Email**: la tua email aziendale
   - **Password**: Almeno 6 caratteri con maiuscole, minuscole e numeri
   - **Confirm Password**: Ripeti la password
3. Clicca su **"Create Account"**
4. Verrai automaticamente loggato e reindirizzato alla dashboard

### Login (Per Accessi Successivi)

1. Clicca su **"Login"**
2. Inserisci:
   - **Email**: L'email con cui ti sei registrato
   - **Password**: La tua password
3. (Opzionale) Spunta **"Remember me"** per rimanere loggato
4. Clicca su **"Sign In"**

---

## 📄 Caricare il Primo Documento

### Passo 1: Vai su Upload

1. Clicca su **"Upload"** nel menu di navigazione
2. Vedrai la pagina di caricamento

### Passo 2: Seleziona un File

1. Clicca su **"Choose File"** o trascina un file
2. Formati supportati:
   - PDF
   - Word (.docx, .doc)
   - Excel (.xlsx, .xls)
   - Testo (.txt)
   - Immagini (.jpg, .png) con OCR

### Passo 3: Carica

1. Clicca su **"Upload Document"**
2. Attendi l'elaborazione:
   - Estrazione testo
   - Generazione embedding vettoriale
   - Classificazione automatica
3. Riceverai una conferma di successo

---

## 🔍 Usare il Sistema RAG

### Ricerca Semantica

1. Vai su **"Documents"**
2. Usa la barra di ricerca in alto
3. Scrivi una domanda in linguaggio naturale:
   - ❌ **NON**: "contratto 2024"
   - ✅ **SÌ**: "Qual è la policy sulle ferie nel contratto 2024?"
4. Il sistema:
   - Trova i documenti rilevanti
   - Genera una risposta basata sul contenuto
   - Mostra le fonti

### Chat Conversazionale

1. Vai su **"Chat"** (se implementato)
2. Fai una domanda sui tuoi documenti
3. Il sistema ricorda il contesto
4. Puoi fare domande di follow-up:
   - "Spiegami meglio"
   - "Ci sono eccezioni?"
   - "Cosa dice il documento 2 su questo?"

---

## ⚙️ Configurazione Avanzata

### Configurare Redis (Cache Distribuito)

#### Installazione Redis

**Windows (con Chocolatey):**
```bash
choco install redis-64
redis-server
```

**Linux/Mac:**
```bash
sudo apt-get install redis-server  # Ubuntu
brew install redis                 # Mac

redis-server
```

#### Configurazione in appsettings.json

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### Configurare OpenTelemetry (Monitoring)

#### Installare Jaeger (per vedere le traces)

```bash
# Docker
docker run -d --name jaeger \
  -e COLLECTOR_OTLP_ENABLED=true \
  -p 16686:16686 \
  -p 4317:4317 \
  jaegertracing/all-in-one:latest
```

#### Abilitare in appsettings.json

```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "http://localhost:4317"
  }
}
```

#### Visualizzare le Traces

1. Apri browser: `http://localhost:16686`
2. Seleziona service: **"DocN"**
3. Cerca le traces delle tue richieste

### Configurare SQL Server 2025 con VECTOR

#### 1. Verifica Supporto VECTOR

```sql
-- Esegui in SQL Server Management Studio
SELECT SERVERPROPERTY('IsVectorSupported') as VectorSupported;
```

Se ritorna `1`, il supporto è abilitato.

#### 2. Configura Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER\\SQL2025;Database=DocNDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

#### 3. (Futuro) Usa VECTOR Nativo

Attualmente usiamo `nvarchar(max)` con value converter.
Quando EF Core supporterà VECTOR nativo:

```csharp
// In ApplicationDbContext.cs
entity.Property(e => e.EmbeddingVector)
    .HasColumnType("VECTOR(1536)");  // ← Nativo!
```

---

## 🔧 Risoluzione Problemi

### Problema: "Database does not exist"

**Soluzione:**
```bash
dotnet ef database update --project DocN.Data/DocN.Data.csproj --startup-project DocN.Client/DocN.Client.csproj
```

### Problema: "Azure OpenAI authentication failed"

**Soluzione:**
1. Verifica che l'endpoint sia corretto
2. Verifica che l'API key sia valida
3. Verifica che i deployment names siano corretti
4. Controlla i logs: `logs/docn-YYYYMMDD.txt`

### Problema: "Port 5000 is already in use"

**Soluzione:**
```bash
# Cambia porta in launchSettings.json
# Oppure usa:
dotnet run --urls "http://localhost:5050"
```

### Problema: "Cannot find Semantic Kernel package"

**Soluzione:**
```bash
# Assicurati di usare le fonti NuGet giuste
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
dotnet restore
```

### Problema: "Upload directory does not exist"

**Soluzione:**
```bash
# Crea manualmente la directory
mkdir C:\DocNData\Uploads

# Oppure cambia il path in appsettings.json
```

---

## 📊 Verifica Installazione

### Checklist Post-Installazione

✅ **Database**
```bash
# Test connessione
dotnet ef database update --project DocN.Data/DocN.Data.csproj
```

✅ **Azure OpenAI**
```bash
# Controlla i logs all'avvio
# Dovresti vedere: "✅ Semantic Memory configurata"
```

✅ **Upload Documenti**
- [ ] Carica un PDF di test
- [ ] Verifica che l'embedding sia generato
- [ ] Verifica che la ricerca funzioni

✅ **Autenticazione**
- [ ] Crea un account
- [ ] Fai login
- [ ] Fai logout
- [ ] Test "Remember me"

---

## 🎓 Tutorial Rapido

### Scenario: Sistema Documentale HR

#### 1. Preparazione

```bash
# Avvia l'applicazione
cd DocN.Client
dotnet run
```

#### 2. Carica Documenti HR

Carica questi documenti (o simili):
1. `Policy Ferie 2024.pdf`
2. `Contratto Tipo Dipendente.docx`
3. `Regolamento Interno.pdf`
4. `Benefits Package.pdf`

#### 3. Fai Domande

Prova queste query:
- "Quanti giorni di ferie ho all'anno?"
- "Qual è la policy sul lavoro remoto?"
- "Come funziona il piano sanitario?"
- "Quali sono i requisiti per il bonus performance?"

#### 4. Osserva i Risultati

- Il sistema trova i documenti rilevanti
- Genera una risposta precisa
- Cita le fonti ([DOCUMENTO 1], [DOCUMENTO 2])

---

## 📚 Risorse Aggiuntive

### Documentazione

- **Microsoft Semantic Kernel**: https://learn.microsoft.com/semantic-kernel/
- **Azure OpenAI**: https://learn.microsoft.com/azure/ai-services/openai/
- **SQL Server 2025 VECTOR**: https://learn.microsoft.com/sql/relational-databases/vectors/
- **.NET 10**: https://learn.microsoft.com/dotnet/

### Community

- **GitHub Issues**: https://github.com/Moncymr/DocN/issues
- **Discussions**: https://github.com/Moncymr/DocN/discussions

### Video Tutorial

- [TODO] Installazione e Setup (10 min)
- [TODO] Primo Documento e Ricerca (5 min)
- [TODO] Configurazione Avanzata (15 min)

---

## 🔐 Sicurezza e Best Practices

### Produzione

1. **Mai committare le chiavi API**
   ```bash
   # Usa Azure Key Vault o variabili ambiente
   export AzureOpenAI__ApiKey="YOUR_KEY"
   ```

2. **HTTPS in produzione**
   ```json
   {
     "Kestrel": {
       "Endpoints": {
         "Https": {
           "Url": "https://*:443",
           "Certificate": {
             "Path": "cert.pfx",
             "Password": "..."
           }
         }
       }
     }
   }
   ```

3. **Backup Database Regolari**
   ```sql
   -- SQL Server
   BACKUP DATABASE DocNDb 
   TO DISK = 'C:\Backups\DocNDb.bak'
   WITH INIT;
   ```

4. **Rate Limiting**
   ```csharp
   // In Program.cs
   builder.Services.AddRateLimiter(options => {
       options.AddFixedWindowLimiter("api", opt => {
           opt.PermitLimit = 100;
           opt.Window = TimeSpan.FromMinutes(1);
       });
   });
   ```

---

## 🚀 Prossimi Passi

Dopo l'installazione:

1. ✅ **Carica 10-20 documenti** di test
2. ✅ **Prova diverse query** per testare la qualità
3. ✅ **Invita colleghi** a testare il sistema
4. ✅ **Monitora i logs** per identificare problemi
5. ✅ **Configura backup** automatici
6. ✅ **Pianifica manutenzione** regolare

---

## 📞 Supporto

Se riscontri problemi:

1. **Controlla i logs**: `logs/docn-YYYYMMDD.txt`
2. **Apri un issue**: https://github.com/Moncymr/DocN/issues
3. **Consulta la documentazione**: `API_DOCUMENTATION.md`
4. **Chiedi nella community**: Discussions tab

---

**Ultima revisione**: Dicembre 2024  
**Versione Guida**: 1.0  
**Compatibile con**: DocN v1.0, .NET 10.0, SQL Server 2025
