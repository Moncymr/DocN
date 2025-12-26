# 📋 Cosa Devi Impostare per il Corretto Funzionamento di DocN

Questa è la risposta completa alla domanda: **"scrivi tutto quello che devo impostare per vere il correto funzionamanetoo"**  
*(tradotto: "scrivi tutto quello che devo impostare per avere il corretto funzionamento")*

---

## 🎯 Risposta Breve

Per far funzionare DocN correttamente devi configurare:

1. **.NET 9.0 SDK** - Il framework di sviluppo
2. **SQL Server o LocalDB** - Il database per archiviare i documenti
3. **Chiavi API AI** - Per embeddings e ricerca semantica (Azure OpenAI, OpenAI o Gemini)
4. **Directory Upload** - Dove salvare i file caricati
5. **Stringa di Connessione Database** - Per collegare l'app al database

---

## 📖 Guide Disponibili

Abbiamo creato 3 guide per aiutarti, scegli quella più adatta:

### 🚀 [AVVIO_RAPIDO.md](AVVIO_RAPIDO.md) - **15 minuti**
**Usa questa se**: Vuoi iniziare subito con il minimo indispensabile

Contiene:
- ✅ Checklist di 6 passi
- ✅ Comandi copy-paste pronti
- ✅ Setup base funzionante
- ✅ Test rapidi

### 📘 [CONFIGURAZIONE_COMPLETA.md](CONFIGURAZIONE_COMPLETA.md) - **30 minuti**
**Usa questa se**: Vuoi capire tutto in dettaglio

Contiene:
- ✅ Spiegazione completa di ogni passo
- ✅ Configurazioni avanzate (Redis, monitoring, multi-provider)
- ✅ Troubleshooting dettagliato con 10+ problemi
- ✅ Best practices di sicurezza
- ✅ Checklist finale completa

### 🔑 [CONFIGURAZIONE_API_KEYS.md](CONFIGURAZIONE_API_KEYS.md) - **10 minuti**
**Usa questa se**: Devi solo configurare le chiavi API

Contiene:
- ✅ Come ottenere chiavi Azure OpenAI
- ✅ Come ottenere chiavi OpenAI
- ✅ Come ottenere chiavi Google Gemini
- ✅ Dove metterle (file, variabili ambiente, Azure Key Vault)
- ✅ Come testarle

---

## ⚡ Setup Minimo (5 Comandi)

Se vuoi solo vedere se funziona, ecco il minimo:

```bash
# 1. Clona repository
git clone https://github.com/Moncymr/DocN.git
cd DocN

# 2. Installa dipendenze
dotnet restore

# 3. Crea database
dotnet ef database update --project src/DocN.Data/DocN.Data.csproj --startup-project src/DocN.Server/DocN.Server.csproj

# 4. Copia e modifica configurazione
cp src/DocN.Server/appsettings.Development.example.json src/DocN.Server/appsettings.Development.json
# Poi modifica il file con le tue chiavi API

# 5. Avvia
cd src/DocN.Server
dotnet run
```

Apri: **http://localhost:5000**

---

## 🔧 Configurazioni Obbligatorie

### 1. 🗄️ Database

**File**: `src/DocN.Server/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

**Comando per creare database**:
```bash
dotnet ef database update --project src/DocN.Data/DocN.Data.csproj --startup-project src/DocN.Server/DocN.Server.csproj
```

---

### 2. 🔑 Chiavi API

**File**: `src/DocN.Server/appsettings.Development.json` (da creare)

```json
{
  "SemanticKernel": {
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "LA_TUA_CHIAVE_API_QUI",
      "EmbeddingDeployment": "text-embedding-ada-002",
      "ChatDeployment": "gpt-4"
    }
  }
}
```

**Dove ottenere le chiavi**:
- **Azure OpenAI**: [portal.azure.com](https://portal.azure.com) → Azure OpenAI → Keys and Endpoint
- **OpenAI**: [platform.openai.com](https://platform.openai.com) → API Keys
- **Gemini**: [makersuite.google.com](https://makersuite.google.com/app/apikey)

📖 **Dettagli**: Leggi [CONFIGURAZIONE_API_KEYS.md](CONFIGURAZIONE_API_KEYS.md)

---

### 3. 📁 Directory Upload

**Crea la cartella**:
```bash
# Windows
mkdir C:\DocNData\Uploads

# Linux/Mac
mkdir -p ~/DocNData/Uploads
```

**Configura il percorso** in `src/DocN.Server/appsettings.json`:
```json
{
  "Storage": {
    "DocumentsPath": "C:\\DocNData\\Uploads"
  }
}
```

---

## ✅ Come Verificare che Funzioni

### Test 1: Applicazione Avviata
```bash
cd src/DocN.Server
dotnet run
```

Cerca nei log:
```
✅ Connessione database stabilita
✅ Semantic Memory configurata
🌐 URL: http://localhost:5000
```

### Test 2: Registrazione Utente
1. Vai su `http://localhost:5000/register`
2. Crea un account
3. Dovresti essere loggato automaticamente

### Test 3: Carica Documento
1. Vai su "Upload"
2. Carica un file PDF
3. Dovresti vedere: "✅ Documento caricato con successo"

### Test 4: Ricerca
1. Vai su "Documents"
2. Cerca qualcosa nei documenti
3. Dovresti vedere risultati rilevanti

---

## 🚨 Problemi Più Comuni

### ❌ "Cannot open database"

**Causa**: Database non creato

**Soluzione**:
```bash
dotnet ef database update --project src/DocN.Data/DocN.Data.csproj --startup-project src/DocN.Server/DocN.Server.csproj
```

---

### ❌ "Azure OpenAI authentication failed"

**Causa**: Chiave API errata

**Soluzione**:
1. Verifica la chiave in Azure Portal
2. Verifica che l'endpoint termini con `/`
3. Verifica che i deployment esistano (`gpt-4` e `text-embedding-ada-002`)

---

### ❌ "Port already in use"

**Causa**: Porta 5000 occupata

**Soluzione**:
```bash
dotnet run --urls "http://localhost:5050"
```

---

## 📚 Tutte le Guide

| Guida | Quando Usarla | Tempo |
|-------|---------------|-------|
| **[AVVIO_RAPIDO.md](AVVIO_RAPIDO.md)** | Vuoi iniziare subito | 15 min |
| **[CONFIGURAZIONE_COMPLETA.md](CONFIGURAZIONE_COMPLETA.md)** | Vuoi capire tutto | 30 min |
| **[CONFIGURAZIONE_API_KEYS.md](CONFIGURAZIONE_API_KEYS.md)** | Solo chiavi API | 10 min |
| **[SETUP.md](SETUP.md)** | Setup generale | 30 min |
| **[GUIDA_INSTALLAZIONE.md](GUIDA_INSTALLAZIONE.md)** | Installazione dettagliata | 40 min |
| **[QUICK_REFERENCE_API_KEYS.md](QUICK_REFERENCE_API_KEYS.md)** | Riferimento rapido | 2 min |

---

## 🎯 Riepilogo

### Devi Installare
- ✅ .NET 9.0 SDK
- ✅ SQL Server o LocalDB
- ✅ Git

### Devi Configurare
- ✅ Stringa connessione database
- ✅ Chiavi API (Azure OpenAI, OpenAI o Gemini)
- ✅ Directory upload documenti

### Devi Eseguire
- ✅ `dotnet restore` - Installa dipendenze
- ✅ `dotnet ef database update` - Crea database
- ✅ `dotnet run` - Avvia applicazione

### Tempo Totale
- **Minimo**: 15 minuti (con [AVVIO_RAPIDO.md](AVVIO_RAPIDO.md))
- **Completo**: 30 minuti (con [CONFIGURAZIONE_COMPLETA.md](CONFIGURAZIONE_COMPLETA.md))

---

## 💡 Consiglio

1. **Principiante?** Inizia con [AVVIO_RAPIDO.md](AVVIO_RAPIDO.md)
2. **Vuoi produzione?** Leggi [CONFIGURAZIONE_COMPLETA.md](CONFIGURAZIONE_COMPLETA.md)
3. **Problemi?** Controlla la sezione Troubleshooting nella guida completa

---

## ❓ Hai Ancora Dubbi?

1. **Leggi la guida completa**: [CONFIGURAZIONE_COMPLETA.md](CONFIGURAZIONE_COMPLETA.md)
2. **Controlla API keys**: [CONFIGURAZIONE_API_KEYS.md](CONFIGURAZIONE_API_KEYS.md)
3. **Apri un issue**: [GitHub Issues](https://github.com/Moncymr/DocN/issues)

---

**Creato**: Dicembre 2024  
**Per**: DocN v1.0  
**Compatibile**: .NET 9.0+, SQL Server 2019+
