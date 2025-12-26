# 🚀 Configurazione Completa DocN - Guida Passo-Passo

## 📋 Indice

1. [Prerequisiti Software](#-prerequisiti-software)
2. [Clonazione Repository](#-clonazione-repository)
3. [Configurazione Database](#-configurazione-database)
4. [Configurazione Chiavi API](#-configurazione-chiavi-api)
5. [Configurazione File Storage](#-configurazione-file-storage)
6. [Configurazione Autenticazione](#-configurazione-autenticazione)
7. [Primo Avvio](#-primo-avvio)
8. [Verifica Funzionamento](#-verifica-funzionamento)
9. [Configurazioni Avanzate](#-configurazioni-avanzate)
10. [Troubleshooting](#-troubleshooting)

---

## 🔧 Prerequisiti Software

### Software Necessario

#### 1. .NET SDK 9.0 o superiore
```bash
# Verifica versione installata
dotnet --version

# Se non installato, scarica da:
# https://dotnet.microsoft.com/download/dotnet/9.0
```

#### 2. SQL Server 2019+ o SQL Server LocalDB
```bash
# Verifica SQL Server LocalDB (per sviluppo)
sqllocaldb info

# Se non installato:
# Windows: Installa SQL Server Express o LocalDB da
# https://www.microsoft.com/sql-server/sql-server-downloads
```

#### 3. Editor/IDE (scegli uno)
- **Visual Studio 2022** (v17.8+) - Consigliato
- **Visual Studio Code** con C# Dev Kit
- **JetBrains Rider**

#### 4. Git
```bash
# Verifica installazione
git --version

# Se non installato: https://git-scm.com/downloads
```

---

## 📥 Clonazione Repository

### Passo 1: Clona il Repository
```bash
# Crea una directory per i progetti
mkdir C:\Projects  # Windows
# oppure
mkdir ~/Projects   # Linux/Mac

# Entra nella directory
cd C:\Projects     # Windows
cd ~/Projects      # Linux/Mac

# Clona il repository
git clone https://github.com/Moncymr/DocN.git
cd DocN
```

### Passo 2: Ripristina i Pacchetti NuGet
```bash
# Ripristina tutte le dipendenze
dotnet restore

# Verifica che non ci siano errori
dotnet build
```

**Output atteso:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## 🗄️ Configurazione Database

### Opzione A: SQL Server LocalDB (Sviluppo Locale - Consigliato)

#### Passo 1: Verifica LocalDB
```bash
# Verifica che LocalDB sia installato
sqllocaldb info

# Se serve, crea l'istanza
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

#### Passo 2: Configura Stringa di Connessione
Modifica `src/DocN.Server/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### Opzione B: SQL Server (Produzione)

#### Passo 1: Crea Database
```sql
-- In SQL Server Management Studio (SSMS)
CREATE DATABASE DocNDb;
GO
```

#### Passo 2: Configura Stringa di Connessione
Modifica `src/DocN.Server/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=DocNDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Esempi di stringhe di connessione:**
- **SQL Server Express**: `Server=.\\SQLEXPRESS;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true`
- **SQL Server remoto**: `Server=192.168.1.100;Database=DocNDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True`
- **Azure SQL**: `Server=tcp:yourserver.database.windows.net,1433;Database=DocNDb;User Id=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False`

### Passo 3: Applica le Migrazioni del Database

```bash
# Torna alla directory root del progetto
cd /path/to/DocN

# Installa gli strumenti EF Core (se non già installati)
dotnet tool install --global dotnet-ef

# Applica le migrazioni
dotnet ef database update --project src/DocN.Data/DocN.Data.csproj --startup-project src/DocN.Server/DocN.Server.csproj
```

**Output atteso:**
```
Applying migration '20240101000000_InitialCreate'.
Done.
```

### Passo 4: Verifica Database Creato
```sql
-- In SSMS o Azure Data Studio
USE DocNDb;
GO

-- Verifica le tabelle create
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE';
```

**Tabelle attese:**
- AspNetUsers
- AspNetRoles
- Documents
- Categories
- AIConfigurations
- DocumentShares
- DocumentStatistics

---

## 🔑 Configurazione Chiavi API

Le chiavi API sono **ESSENZIALI** per il funzionamento delle funzionalità AI.

### Opzione 1: File di Configurazione Locale (Sviluppo)

#### Passo 1: Crea File Development
```bash
# Copia il file template
cp src/DocN.Server/appsettings.Development.example.json src/DocN.Server/appsettings.Development.json
```

#### Passo 2: Ottieni le Chiavi API

##### Azure OpenAI (Consigliato)
1. Vai su [Azure Portal](https://portal.azure.com)
2. Cerca "Azure OpenAI" e crea una risorsa
3. Vai su **"Keys and Endpoint"**
4. Copia:
   - **Endpoint**: Es. `https://your-resource.openai.azure.com/`
   - **Key 1**: La tua chiave API
5. Vai su **"Model deployments"** e crea:
   - **Deployment GPT-4**: Nome `gpt-4`
   - **Deployment Embeddings**: Nome `text-embedding-ada-002`

##### OpenAI (Alternativo)
1. Vai su [platform.openai.com](https://platform.openai.com)
2. Accedi o crea account
3. Vai su **"API Keys"**
4. Clicca **"Create new secret key"**
5. Copia la chiave (inizia con `sk-proj-` o `sk-`)

##### Google Gemini (Opzionale)
1. Vai su [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Accedi con account Google
3. Clicca **"Get API Key"**
4. Copia la chiave (inizia con `AIzaSy`)

#### Passo 3: Configura il File
Modifica `src/DocN.Server/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "LA_TUA_CHIAVE_AZURE_OPENAI",
    "ChatDeploymentName": "gpt-4",
    "EmbeddingDeploymentName": "text-embedding-ada-002"
  },
  
  "OpenAI": {
    "ApiKey": "sk-proj-LA_TUA_CHIAVE_OPENAI"
  },
  
  "Gemini": {
    "ApiKey": "AIzaSy-LA_TUA_CHIAVE_GEMINI"
  },
  
  "Embeddings": {
    "Provider": "AzureOpenAI",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "LA_TUA_CHIAVE_AZURE_OPENAI",
    "Model": "text-embedding-ada-002",
    "DeploymentName": "text-embedding-ada-002"
  },
  
  "FileStorage": {
    "UploadPath": "C:\\DocNData\\Uploads"
  }
}
```

**⚠️ IMPORTANTE**: Questo file è già in `.gitignore` e non verrà committato.

### Opzione 2: Variabili d'Ambiente (Produzione - Consigliato)

#### Windows PowerShell
```powershell
# Azure OpenAI
$env:AzureOpenAI__Endpoint = "https://your-resource.openai.azure.com/"
$env:AzureOpenAI__ApiKey = "LA_TUA_CHIAVE"
$env:AzureOpenAI__ChatDeploymentName = "gpt-4"
$env:AzureOpenAI__EmbeddingDeploymentName = "text-embedding-ada-002"

# Embeddings
$env:Embeddings__ApiKey = "LA_TUA_CHIAVE_AZURE_OPENAI"
$env:Embeddings__Provider = "AzureOpenAI"
```

#### Linux/Mac
```bash
# Azure OpenAI
export AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/"
export AzureOpenAI__ApiKey="LA_TUA_CHIAVE"
export AzureOpenAI__ChatDeploymentName="gpt-4"
export AzureOpenAI__EmbeddingDeploymentName="text-embedding-ada-002"

# Embeddings
export Embeddings__ApiKey="LA_TUA_CHIAVE_AZURE_OPENAI"
export Embeddings__Provider="AzureOpenAI"
```

---

## 📁 Configurazione File Storage

### Passo 1: Crea Directory per i File
```bash
# Windows
mkdir C:\DocNData\Uploads

# Linux/Mac
mkdir -p ~/DocNData/Uploads
```

### Passo 2: Configura il Percorso
Nel file `src/DocN.Server/appsettings.json` o `appsettings.Development.json`:

```json
{
  "FileStorage": {
    "UploadPath": "C:\\DocNData\\Uploads"  // Windows
    // oppure
    "UploadPath": "/home/user/DocNData/Uploads"  // Linux/Mac
  }
}
```

### Passo 3: Imposta Permessi (Linux/Mac)
```bash
# Assicurati che la directory sia scrivibile
chmod 755 ~/DocNData/Uploads
```

---

## 🔐 Configurazione Autenticazione

L'autenticazione è già configurata con ASP.NET Core Identity. Non serve configurazione aggiuntiva.

### Funzionalità Incluse:
- ✅ Registrazione utenti
- ✅ Login/Logout
- ✅ Reset password
- ✅ Gestione sessioni
- ✅ Ruoli e permessi

### Requisiti Password (Configurabili)
Di default:
- Minimo 6 caratteri
- Almeno una lettera maiuscola
- Almeno una lettera minuscola
- Almeno un numero

Per modificare, edita `src/DocN.Client/Program.cs`:
```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;  // Aumenta a 8
    options.Password.RequireNonAlphanumeric = true;  // Richiedi caratteri speciali
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
```

---

## 🎬 Primo Avvio

### Passo 1: Avvia l'Applicazione
```bash
# Dalla directory root del progetto
cd src/DocN.Server

# Avvia l'applicazione
dotnet run
```

### Passo 2: Verifica l'Avvio
Cerca questi messaggi nei log:

```
✅ Connessione database stabilita
✅ Database già aggiornato
⚡ Configurazione Microsoft Semantic Kernel...
✅ Semantic Memory configurata
✨ DocN avviato con successo!
🌐 URL: http://localhost:5000, https://localhost:5001
```

### Passo 3: Apri il Browser
Vai su: `http://localhost:5000` o `https://localhost:5001`

Dovresti vedere la homepage di DocN.

---

## ✅ Verifica Funzionamento

### Test 1: Registrazione Utente

1. Clicca su **"Register"**
2. Compila il form:
   - **First Name**: Mario
   - **Last Name**: Rossi
   - **Email**: mario.rossi@example.com
   - **Password**: Test123!
   - **Confirm Password**: Test123!
3. Clicca **"Create Account"**
4. Dovresti essere automaticamente loggato

### Test 2: Caricamento Documento

1. Clicca su **"Upload"** nel menu
2. Seleziona un file PDF di test
3. Clicca **"Upload Document"**
4. Attendi l'elaborazione (estrazione testo + generazione embedding)
5. Dovresti vedere: **"✅ Documento caricato con successo"**

### Test 3: Ricerca Semantica

1. Vai su **"Documents"**
2. Usa la barra di ricerca
3. Scrivi una query in linguaggio naturale: "contratti di lavoro"
4. Dovresti vedere i documenti rilevanti anche senza match esatto

### Test 4: Sistema RAG

1. Carica 2-3 documenti di prova
2. Vai su **"Chat"** (se implementato) o homepage
3. Fai una domanda: "Quali documenti parlano di ferie?"
4. Dovresti ricevere una risposta con citazioni [DOCUMENTO 1], [DOCUMENTO 2]

### Checklist Verifica Completa

- [ ] ✅ Database connesso e tabelle create
- [ ] ✅ Utente registrato e login funzionante
- [ ] ✅ Documento caricato con successo
- [ ] ✅ Embedding vettoriale generato (verifica in DB)
- [ ] ✅ Ricerca semantica funzionante
- [ ] ✅ RAG risponde correttamente
- [ ] ✅ Download documento funziona

---

## ⚙️ Configurazioni Avanzate

### Configurazione Redis (Cache Distribuito)

#### Installazione Redis
```bash
# Windows (con Chocolatey)
choco install redis-64

# Linux (Ubuntu)
sudo apt-get install redis-server

# Mac
brew install redis

# Avvia Redis
redis-server
```

#### Configurazione
In `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### Configurazione OpenTelemetry (Monitoring)

#### Installazione Jaeger (per visualizzare traces)
```bash
# Usa Docker
docker run -d --name jaeger \
  -e COLLECTOR_OTLP_ENABLED=true \
  -p 16686:16686 \
  -p 4317:4317 \
  jaegertracing/all-in-one:latest
```

#### Configurazione
In `appsettings.json`:
```json
{
  "OpenTelemetry": {
    "Enabled": true,
    "Endpoint": "http://localhost:4317"
  }
}
```

#### Visualizzazione
Apri browser: `http://localhost:16686`

### Configurazione Multi-Provider AI

Per usare provider multipli con fallback:

```json
{
  "AI": {
    "Provider": "AzureOpenAI",
    "EnableFallback": true
  },
  
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "CHIAVE_AZURE"
  },
  
  "OpenAI": {
    "ApiKey": "CHIAVE_OPENAI"
  },
  
  "Gemini": {
    "ApiKey": "CHIAVE_GEMINI"
  }
}
```

### Configurazione HTTPS (Produzione)

```json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "certificate.pfx",
          "Password": "password-certificato"
        }
      }
    }
  }
}
```

---

## 🚨 Troubleshooting

### Problema: "Cannot open database 'DocNDb'"

**Causa**: Database non creato o SQL Server non in esecuzione

**Soluzione**:
```bash
# Verifica SQL Server
sqllocaldb info

# Riavvia LocalDB
sqllocaldb stop MSSQLLocalDB
sqllocaldb start MSSQLLocalDB

# Riapplica migrazioni
dotnet ef database update --project src/DocN.Data/DocN.Data.csproj --startup-project src/DocN.Server/DocN.Server.csproj
```

### Problema: "Azure OpenAI authentication failed"

**Causa**: Chiave API errata o endpoint sbagliato

**Soluzione**:
1. Verifica che l'endpoint termini con `/`
2. Verifica la chiave in Azure Portal
3. Controlla che i deployment esistano
4. Rigenera la chiave se necessario

### Problema: "Deployment 'gpt-4' not found"

**Causa**: Nome deployment errato

**Soluzione**:
1. Vai su Azure Portal → Azure OpenAI → Model deployments
2. Verifica il nome esatto del deployment
3. Aggiorna `ChatDeploymentName` in appsettings.json

### Problema: "Embeddings returning null"

**Causa**: Servizio embeddings non configurato

**Soluzione**:
Verifica che la sezione `Embeddings` sia configurata:
```json
{
  "Embeddings": {
    "Provider": "AzureOpenAI",
    "ApiKey": "LA_TUA_CHIAVE",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "text-embedding-ada-002"
  }
}
```

### Problema: "Port 5000 already in use"

**Causa**: Porta già occupata da altro processo

**Soluzione**:
```bash
# Usa porta diversa
dotnet run --urls "http://localhost:5050"

# Oppure modifica launchSettings.json
```

### Problema: "Unable to create upload directory"

**Causa**: Permessi insufficienti o percorso errato

**Soluzione**:
```bash
# Windows: esegui terminale come amministratore
mkdir C:\DocNData\Uploads

# Linux/Mac: imposta permessi
sudo mkdir -p /var/docn/uploads
sudo chmod 755 /var/docn/uploads
sudo chown $USER:$USER /var/docn/uploads
```

### Problema: Build fallisce con errori NuGet

**Causa**: Pacchetti non ripristinati o cache corrotta

**Soluzione**:
```bash
# Pulisci cache NuGet
dotnet nuget locals all --clear

# Ripristina pacchetti
dotnet restore

# Ricompila
dotnet build
```

### Logs e Diagnostica

#### Dove Trovare i Log
```
logs/docn-YYYYMMDD.txt
```

#### Aumentare Livello Log
In `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.SemanticKernel": "Debug"
    }
  }
}
```

---

## 📚 Documentazione Aggiuntiva

### Guide Dettagliate
- **[SETUP.md](SETUP.md)** - Setup generale e architettura
- **[GUIDA_INSTALLAZIONE.md](GUIDA_INSTALLAZIONE.md)** - Installazione completa
- **[CONFIGURAZIONE_API_KEYS.md](CONFIGURAZIONE_API_KEYS.md)** - Chiavi API dettagliate
- **[API_DOCUMENTATION.md](API_DOCUMENTATION.md)** - Documentazione API
- **[QUICK_REFERENCE_API_KEYS.md](QUICK_REFERENCE_API_KEYS.md)** - Riferimento rapido

### Link Utili
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [Microsoft Semantic Kernel](https://learn.microsoft.com/semantic-kernel/)
- [SQL Server 2025 Vector Support](https://learn.microsoft.com/sql/relational-databases/vectors/)
- [.NET 9.0 Documentation](https://learn.microsoft.com/dotnet/)

---

## ✨ Riepilogo Setup Rapido (5 Minuti)

```bash
# 1. Clona repository
git clone https://github.com/Moncymr/DocN.git
cd DocN

# 2. Ripristina dipendenze
dotnet restore

# 3. Crea file configurazione
cp src/DocN.Server/appsettings.Development.example.json src/DocN.Server/appsettings.Development.json

# 4. Modifica appsettings.Development.json con le tue chiavi

# 5. Crea directory upload
mkdir C:\DocNData\Uploads  # Windows
# mkdir -p ~/DocNData/Uploads  # Linux/Mac

# 6. Applica migrazioni database
dotnet ef database update --project src/DocN.Data/DocN.Data.csproj --startup-project src/DocN.Server/DocN.Server.csproj

# 7. Avvia l'applicazione
cd src/DocN.Server
dotnet run

# 8. Apri browser: http://localhost:5000
```

---

## 🎯 Checklist Finale

### Prima di Iniziare
- [ ] .NET 9.0 SDK installato
- [ ] SQL Server o LocalDB installato
- [ ] Git installato
- [ ] Editor/IDE configurato

### Configurazione Base
- [ ] Repository clonato
- [ ] Pacchetti NuGet ripristinati
- [ ] Database configurato e migrazioni applicate
- [ ] Chiavi API Azure OpenAI configurate
- [ ] Directory upload creata
- [ ] appsettings.Development.json configurato

### Test Funzionalità
- [ ] Applicazione avviata senza errori
- [ ] Utente registrato con successo
- [ ] Documento caricato e processato
- [ ] Embedding vettoriale generato
- [ ] Ricerca semantica funzionante
- [ ] Sistema RAG risponde correttamente

### Produzione (Opzionale)
- [ ] Variabili d'ambiente configurate
- [ ] HTTPS abilitato
- [ ] Database di produzione configurato
- [ ] Backup automatici configurati
- [ ] Monitoring abilitato
- [ ] Redis configurato (se necessario)

---

**Ultima revisione**: Dicembre 2024  
**Versione**: 1.0  
**Compatibile con**: DocN v1.0, .NET 9.0+, SQL Server 2019+

Per domande o problemi, consulta la sezione [Troubleshooting](#-troubleshooting) o apri un issue su GitHub.
