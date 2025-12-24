# 🔑 Configurazione Chiavi API per Servizi AI

## 📋 Panoramica

Questa guida spiega **dove e come configurare le chiavi API** per i servizi di embedding utilizzati per:
- **Vettori di chat** (Chat Embeddings)
- **RAG** (Retrieval Augmented Generation)
- **Generazione risposte AI**

## 🎯 Servizi AI Supportati

DocN supporta tre provider AI principali:

1. **Azure OpenAI** - Raccomandato per produzione
2. **OpenAI** - Alternativa diretta
3. **Google Gemini** - Opzione aggiuntiva

## 📍 Dove Sono Configurate le Chiavi API?

Le chiavi API possono essere configurate in **4 modi diversi**:

### 1️⃣ File `appsettings.json` (Configurazione Base)

**Percorso**: `DocN.Client/appsettings.json` o `DocN.Server/appsettings.json`

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "YOUR_AZURE_OPENAI_API_KEY",
    "ChatDeploymentName": "gpt-4",
    "EmbeddingDeploymentName": "text-embedding-ada-002"
  }
}
```

⚠️ **ATTENZIONE**: Non committare mai questo file con chiavi reali nel repository!

### 2️⃣ File `appsettings.Development.json` (Sviluppo Locale)

**Percorso**: `DocN.Client/appsettings.Development.json`

**Questo file è già in `.gitignore` ed è sicuro per chiavi di sviluppo.**

Crea il file usando il template:

```bash
cp DocN.Client/appsettings.Development.example.json DocN.Client/appsettings.Development.json
```

Poi modifica con le tue chiavi:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  
  "OpenAI": {
    "ApiKey": "sk-proj-YOUR_OPENAI_API_KEY_HERE"
  },
  
  "Gemini": {
    "ApiKey": "AIzaSy-YOUR_GEMINI_API_KEY_HERE"
  },
  
  "Embeddings": {
    "Provider": "AzureOpenAI",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "YOUR_AZURE_OPENAI_API_KEY_HERE",
    "Model": "text-embedding-ada-002",
    "DeploymentName": "text-embedding-ada-002"
  }
}
```

### 3️⃣ Variabili d'Ambiente (Produzione Raccomandato)

**Il metodo più sicuro per produzione.**

#### Windows PowerShell:
```powershell
$env:AzureOpenAI__Endpoint = "https://your-resource.openai.azure.com/"
$env:AzureOpenAI__ApiKey = "YOUR_API_KEY"
$env:AzureOpenAI__ChatDeploymentName = "gpt-4"
$env:AzureOpenAI__EmbeddingDeploymentName = "text-embedding-ada-002"

$env:OpenAI__ApiKey = "sk-proj-YOUR_OPENAI_KEY"
$env:Gemini__ApiKey = "AIzaSy-YOUR_GEMINI_KEY"
$env:Embeddings__ApiKey = "YOUR_AZURE_OPENAI_KEY"
```

#### Linux/Mac:
```bash
export AzureOpenAI__Endpoint="https://your-resource.openai.azure.com/"
export AzureOpenAI__ApiKey="YOUR_API_KEY"
export AzureOpenAI__ChatDeploymentName="gpt-4"
export AzureOpenAI__EmbeddingDeploymentName="text-embedding-ada-002"

export OpenAI__ApiKey="sk-proj-YOUR_OPENAI_KEY"
export Gemini__ApiKey="AIzaSy-YOUR_GEMINI_KEY"
export Embeddings__ApiKey="YOUR_AZURE_OPENAI_KEY"
```

#### Docker:
```bash
docker run -e AzureOpenAI__ApiKey="YOUR_KEY" your-image
```

### 4️⃣ Database - Tabella `AIConfigurations` (Configurazione Runtime)

Le chiavi possono anche essere salvate nel database e modificate tramite l'interfaccia web.

**Percorso nell'app**: `/aiconfig` (Pagina Configurazione AI)

**Campi nella tabella `AIConfigurations`**:
- `AzureOpenAIEndpoint` - URL endpoint Azure OpenAI
- `AzureOpenAIKey` - Chiave API Azure OpenAI
- `EmbeddingDeploymentName` - Nome deployment embeddings (es. "text-embedding-ada-002")
- `ChatDeploymentName` - Nome deployment chat (es. "gpt-4")

## 🔧 Configurazione Dettagliata per Servizio

### Azure OpenAI (Raccomandato)

**Utilizzato per**:
- ✅ Embeddings vettoriali (ricerca semantica)
- ✅ Chat completion (generazione risposte)
- ✅ RAG (Retrieval Augmented Generation)

**Come ottenere le credenziali**:

1. Vai su [Azure Portal](https://portal.azure.com)
2. Crea una risorsa "Azure OpenAI Service"
3. Vai su **"Keys and Endpoint"**
4. Copia:
   - **Endpoint**: Es. `https://your-resource.openai.azure.com/`
   - **Key 1** o **Key 2**: La chiave API

5. Crea i **Deployment**:
   - Vai su **"Model deployments"**
   - Crea deployment per **GPT-4** (nome: `gpt-4`)
   - Crea deployment per **text-embedding-ada-002** (nome: `text-embedding-ada-002`)

**Configurazione**:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "abc123def456...",
    "ChatDeploymentName": "gpt-4",
    "EmbeddingDeploymentName": "text-embedding-ada-002"
  }
}
```

### OpenAI Diretto

**Utilizzato per**:
- ✅ Embeddings alternativi
- ✅ Chat completion (fallback)

**Come ottenere la chiave**:

1. Vai su [platform.openai.com](https://platform.openai.com)
2. Accedi o crea un account
3. Vai su **"API Keys"**
4. Clicca **"Create new secret key"**
5. Copia la chiave (inizia con `sk-proj-` o `sk-`)

**Configurazione**:

```json
{
  "OpenAI": {
    "ApiKey": "sk-proj-abc123...",
    "Model": "gpt-4",
    "MaxTokens": 1000
  }
}
```

### Google Gemini

**Utilizzato per**:
- ✅ Embeddings alternativi
- ✅ Chat completion (fallback)

**Come ottenere la chiave**:

1. Vai su [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Accedi con il tuo account Google
3. Clicca **"Get API Key"** o **"Create API Key"**
4. Copia la chiave (inizia con `AIzaSy`)

**Configurazione**:

```json
{
  "Gemini": {
    "ApiKey": "AIzaSy-abc123..."
  }
}
```

### Configurazione Embeddings (Vettori)

**Le chiavi per gli embeddings sono ESSENZIALI per**:
- 🔍 Ricerca semantica nei documenti
- 🤖 Sistema RAG
- 💬 Vettori di chat

**Configurazione dedicata**:

```json
{
  "Embeddings": {
    "Provider": "AzureOpenAI",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "YOUR_AZURE_OPENAI_KEY",
    "Model": "text-embedding-ada-002",
    "DeploymentName": "text-embedding-ada-002"
  }
}
```

**Provider supportati**:
- `"AzureOpenAI"` - Raccomandato
- `"OpenAI"` - Usa OpenAI diretto
- `"Gemini"` - Usa Google Gemini

## 📊 Quale Servizio Usa Quale Chiave?

### EmbeddingService (`DocN.Data.Services.EmbeddingService`)

**Legge da**: Tabella database `AIConfigurations`

**Chiavi usate**:
- `AzureOpenAIEndpoint`
- `AzureOpenAIKey`
- `EmbeddingDeploymentName`

**Codice di riferimento**: `DocN.Data/Services/EmbeddingService.cs` (righe 32-36)

### MultiProviderAIService (`DocN.Data.Services.MultiProviderAIService`)

**Legge da**: File `appsettings.json` sezioni:
- `AI`
- `Embeddings`
- `Gemini`
- `OpenAI`

**Chiavi usate**:
- `Embeddings:ApiKey` - Per embeddings vettoriali
- `OpenAI:ApiKey` - Per chat OpenAI
- `Gemini:ApiKey` - Per chat Gemini

**Codice di riferimento**: `DocN.Data/Services/MultiProviderAIService.cs` (righe 27-43)

### ModernRAGService (`DocN.Data.Services.ModernRAGService`)

**Legge da**: Microsoft Semantic Kernel configurato in `Program.cs`

**Chiavi usate**:
- `AzureOpenAI:Endpoint`
- `AzureOpenAI:ApiKey`
- `AzureOpenAI:ChatDeploymentName`
- `AzureOpenAI:EmbeddingDeploymentName`

**Codice di riferimento**: `MODERN_PROGRAM_CS.cs` (righe 98-125)

## ✅ Verifica Configurazione

### Test 1: Verifica Chiavi Caricate

Controlla i log all'avvio dell'applicazione:

```
✅ Semantic Memory configurata
⚡ Configurazione Microsoft Semantic Kernel...
Configurazione Azure OpenAI - Chat: gpt-4
Configurazione Azure OpenAI - Embeddings: text-embedding-ada-002
```

Se vedi `⚠️ Azure OpenAI non configurato`, le chiavi non sono state caricate.

### Test 2: Test Upload Documento

1. Vai su `/upload`
2. Carica un documento PDF
3. Se gli embeddings funzionano, vedrai:
   - ✅ Documento caricato con successo
   - ✅ Vettore generato (campo `EmbeddingVector` popolato)

### Test 3: Test Ricerca Semantica

1. Vai su `/documents`
2. Usa la barra di ricerca
3. Se funziona, vedrai documenti rilevanti anche senza corrispondenza esatta

### Test 4: Test RAG

1. Vai su `/chat` (se implementato) o `/home`
2. Fai una domanda sui documenti
3. Se funziona, riceverai una risposta contestualizzata

## 🔒 Sicurezza - Best Practices

### ✅ DA FARE

1. **Usa variabili d'ambiente in produzione**
   ```bash
   export AzureOpenAI__ApiKey="YOUR_KEY"
   ```

2. **Usa Azure Key Vault per produzione Azure**
   ```csharp
   builder.Configuration.AddAzureKeyVault(
       new Uri("https://your-keyvault.vault.azure.net/"),
       new DefaultAzureCredential());
   ```

3. **Ruota le chiavi regolarmente**
   - Azure Portal → Azure OpenAI → Chiavi → Rigenera

4. **Limita l'accesso alle chiavi**
   - Usa Azure RBAC
   - Assegna ruolo "Cognitive Services OpenAI User"

5. **Monitora l'uso delle API**
   - Azure Portal → Azure OpenAI → Metriche
   - Imposta alert per uso anomalo

### ❌ NON FARE

1. ❌ **NON committare chiavi nel repository**
   ```bash
   # Aggiungi al .gitignore
   appsettings.Development.json
   appsettings.Production.json
   *.secrets.json
   ```

2. ❌ **NON condividere chiavi via email/chat**
   - Usa Azure Key Vault o sistemi di gestione segreti

3. ❌ **NON usare la stessa chiave per dev e prod**
   - Crea risorse Azure separate

4. ❌ **NON loggare le chiavi**
   ```csharp
   // NO!
   Log.Information("API Key: {Key}", apiKey);
   
   // SI!
   Log.Information("API Key configurata: {Configured}", !string.IsNullOrEmpty(apiKey));
   ```

## 🚨 Troubleshooting

### Problema: "Azure OpenAI authentication failed"

**Causa**: Chiave API errata o scaduta

**Soluzione**:
1. Verifica la chiave in Azure Portal
2. Controlla che l'endpoint sia corretto (deve terminare con `/`)
3. Rigenera la chiave se necessario

### Problema: "Deployment not found"

**Causa**: Nome deployment errato

**Soluzione**:
1. Vai su Azure Portal → Azure OpenAI → Model deployments
2. Verifica i nomi esatti dei deployment
3. Aggiorna `ChatDeploymentName` e `EmbeddingDeploymentName`

### Problema: "Embeddings returning null"

**Causa**: Servizio embeddings non configurato

**Soluzione**:
1. Verifica che `Embeddings:ApiKey` sia configurato
2. Verifica che `Embeddings:Provider` sia impostato
3. Controlla i log per errori specifici

### Problema: "Rate limit exceeded"

**Causa**: Troppe richieste API

**Soluzione**:
1. Aumenta il quota in Azure Portal
2. Implementa retry logic con backoff
3. Usa caching per ridurre chiamate

## 📚 Riferimenti

### Documentazione Correlata

- **Setup Generale**: [SETUP.md](SETUP.md)
- **Guida Installazione**: [GUIDA_INSTALLAZIONE.md](GUIDA_INSTALLAZIONE.md)
- **API Documentation**: [API_DOCUMENTATION.md](API_DOCUMENTATION.md)
- **Implementazione RAG**: [RAG_ANALYSIS_AND_IMPROVEMENTS.md](RAG_ANALYSIS_AND_IMPROVEMENTS.md)

### Codice Sorgente

- **EmbeddingService**: `DocN.Data/Services/EmbeddingService.cs`
- **MultiProviderAIService**: `DocN.Data/Services/MultiProviderAIService.cs`
- **ModernRAGService**: `DocN.Data/Services/ModernRAGService.cs`
- **Configurazione App**: `MODERN_PROGRAM_CS.cs`
- **Modelli**: `DocN.Data/Models/AppSettings.cs`

### Link Esterni

- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [OpenAI API Documentation](https://platform.openai.com/docs)
- [Google Gemini API](https://ai.google.dev/docs)
- [Microsoft Semantic Kernel](https://learn.microsoft.com/semantic-kernel/)

## 🔄 Esempio Configurazione Completa

### Configurazione Minima (Solo Azure OpenAI)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "abc123def456...",
    "ChatDeploymentName": "gpt-4",
    "EmbeddingDeploymentName": "text-embedding-ada-002"
  }
}
```

### Configurazione Multi-Provider (Con Fallback)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  
  "AI": {
    "Provider": "Gemini",
    "EnableFallback": true
  },
  
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "abc123def456...",
    "ChatDeploymentName": "gpt-4",
    "EmbeddingDeploymentName": "text-embedding-ada-002"
  },
  
  "OpenAI": {
    "ApiKey": "sk-proj-abc123...",
    "Model": "gpt-4",
    "MaxTokens": 1000
  },
  
  "Gemini": {
    "ApiKey": "AIzaSy-abc123..."
  },
  
  "Embeddings": {
    "Provider": "AzureOpenAI",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "abc123def456...",
    "Model": "text-embedding-ada-002",
    "DeploymentName": "text-embedding-ada-002"
  }
}
```

### Configurazione Produzione (Con Azure Key Vault)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-sql.database.windows.net;Database=DocNDb;User Id=${DB_USER};Password=${DB_PASSWORD};Encrypt=True;"
  },
  
  "AzureKeyVault": {
    "VaultUri": "https://your-keyvault.vault.azure.net/"
  }
}
```

Poi in Key Vault:
- Secret `AzureOpenAI--ApiKey`
- Secret `AzureOpenAI--Endpoint`
- Secret `OpenAI--ApiKey`
- Secret `Gemini--ApiKey`

---

**Ultima revisione**: Dicembre 2024  
**Versione**: 1.0  
**Compatibile con**: DocN v1.0, .NET 10.0
