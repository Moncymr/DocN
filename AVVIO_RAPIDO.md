# ⚡ DocN - Avvio Rapido

## 🎯 Cosa Devi Configurare per il Corretto Funzionamento

Questa è una **guida rapida** che ti dice esattamente cosa configurare per far funzionare DocN. Per istruzioni dettagliate, consulta la **[Guida Completa](CONFIGURAZIONE_COMPLETA.md)**.

---

## ✅ Checklist Minima (15 minuti)

### 1. ⚙️ Software Necessario
- [ ] **.NET 9.0 SDK** o superiore ([Download](https://dotnet.microsoft.com/download))
- [ ] **SQL Server LocalDB** o SQL Server ([Download](https://www.microsoft.com/sql-server/sql-server-downloads))
- [ ] **Git** ([Download](https://git-scm.com/downloads))

Verifica:
```bash
dotnet --version  # Deve essere >= 9.0
sqllocaldb info   # Deve mostrare istanze disponibili
git --version     # Deve mostrare versione
```

---

### 2. 📥 Clona e Compila

```bash
# Clona il repository
git clone https://github.com/Moncymr/DocN.git
cd DocN

# Ripristina dipendenze
dotnet restore

# Verifica che compili
dotnet build
```

---

### 3. 🗄️ Configura Database

**Stringa di connessione** da configurare in `src/DocN.Server/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocNDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

**Applica le migrazioni**:
```bash
dotnet ef database update --project src/DocN.Data/DocN.Data.csproj --startup-project src/DocN.Server/DocN.Server.csproj
```

---

### 4. 🔑 Configura Chiavi API

#### Opzione A: File Locale (Sviluppo)

Crea `src/DocN.Server/appsettings.Development.json`:

```json
{
  "SemanticKernel": {
    "DefaultEmbeddingProvider": "AzureOpenAI",
    "DefaultChatProvider": "AzureOpenAI",
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "LA_TUA_CHIAVE_API",
      "EmbeddingDeployment": "text-embedding-ada-002",
      "ChatDeployment": "gpt-4"
    }
  }
}
```

**Come ottenere le chiavi:**
1. Vai su [Azure Portal](https://portal.azure.com)
2. Crea una risorsa "Azure OpenAI"
3. Vai su "Keys and Endpoint"
4. Copia Endpoint e Key
5. Crea i deployment: `gpt-4` e `text-embedding-ada-002`

#### Opzione B: Variabili d'Ambiente (Produzione)

```bash
# Windows PowerShell
$env:SemanticKernel__AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/"
$env:SemanticKernel__AzureOpenAI__ApiKey="LA_TUA_CHIAVE"

# Linux/Mac
export SemanticKernel__AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/"
export SemanticKernel__AzureOpenAI__ApiKey="LA_TUA_CHIAVE"
```

📖 **Guida dettagliata**: [CONFIGURAZIONE_API_KEYS.md](CONFIGURAZIONE_API_KEYS.md)

---

### 5. 📁 Crea Directory Upload

```bash
# Windows
mkdir C:\DocNData\Uploads

# Linux/Mac
mkdir -p ~/DocNData/Uploads
```

Configura il percorso in `src/DocN.Server/appsettings.json`:
```json
{
  "Storage": {
    "DocumentsPath": "C:\\DocNData\\Uploads",
    "MaxFileSizeMB": 100
  }
}
```

---

### 6. 🚀 Avvia l'Applicazione

```bash
cd src/DocN.Server
dotnet run
```

**Output atteso:**
```
✅ Connessione database stabilita
✅ Semantic Memory configurata
🌐 URL: http://localhost:5000
```

Apri il browser: **http://localhost:5000**

---

## ✅ Test Funzionamento

### Test 1: Registra Utente
1. Vai su `/register`
2. Crea un account
3. Dovresti essere loggato automaticamente

### Test 2: Carica Documento
1. Vai su "Upload"
2. Seleziona un file PDF
3. Attendi elaborazione
4. Dovresti vedere: "✅ Documento caricato"

### Test 3: Ricerca Semantica
1. Vai su "Documents"
2. Cerca: "contratti di lavoro"
3. Dovresti vedere risultati rilevanti

---

## 🚨 Problemi Comuni

### ❌ "Cannot open database"
```bash
# Riavvia LocalDB
sqllocaldb stop MSSQLLocalDB
sqllocaldb start MSSQLLocalDB

# Riapplica migrazioni
dotnet ef database update --project src/DocN.Data/DocN.Data.csproj --startup-project src/DocN.Server/DocN.Server.csproj
```

### ❌ "Azure OpenAI authentication failed"
1. Verifica che l'endpoint termini con `/`
2. Verifica la chiave in Azure Portal
3. Verifica che i deployment esistano

### ❌ "Port already in use"
```bash
dotnet run --urls "http://localhost:5050"
```

---

## 📚 Documentazione Completa

Per configurazioni avanzate, troubleshooting dettagliato e tutte le opzioni:

### Guide Principali
- 📘 **[CONFIGURAZIONE_COMPLETA.md](CONFIGURAZIONE_COMPLETA.md)** - Guida passo-passo completa
- 🔑 **[CONFIGURAZIONE_API_KEYS.md](CONFIGURAZIONE_API_KEYS.md)** - Chiavi API dettagliate
- ⚙️ **[SETUP.md](SETUP.md)** - Setup e architettura
- 🚀 **[GUIDA_INSTALLAZIONE.md](GUIDA_INSTALLAZIONE.md)** - Installazione completa

### Guide Rapide
- ⚡ **[QUICK_REFERENCE_API_KEYS.md](QUICK_REFERENCE_API_KEYS.md)** - Riferimento rapido API
- 🤖 **[QUICK_START_RAG.md](QUICK_START_RAG.md)** - Quick start RAG
- 📖 **[API_DOCUMENTATION.md](API_DOCUMENTATION.md)** - Documentazione API

---

## 🎯 Riepilogo Configurazione

| Cosa | Dove | Valore di Default |
|------|------|-------------------|
| **Database** | `ConnectionStrings:DefaultConnection` | LocalDB |
| **API Keys** | `SemanticKernel:AzureOpenAI` | Devi configurare |
| **Upload Path** | `Storage:DocumentsPath` | `C:\DocNData\Uploads` |
| **Provider AI** | `SemanticKernel:DefaultEmbeddingProvider` | AzureOpenAI |

---

## ❓ Hai Bisogno di Aiuto?

1. **Consulta la guida completa**: [CONFIGURAZIONE_COMPLETA.md](CONFIGURAZIONE_COMPLETA.md)
2. **Troubleshooting**: Sezione dettagliata nella guida completa
3. **Apri un issue**: [GitHub Issues](https://github.com/Moncymr/DocN/issues)

---

**Tempo stimato setup completo**: 15-30 minuti  
**Ultima revisione**: Dicembre 2024  
**Compatibile con**: DocN v1.0, .NET 9.0+
