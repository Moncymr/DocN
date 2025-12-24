# 🚀 Riferimento Rapido - Chiavi API

## 📍 Dove Configurare le Chiavi?

```
┌─────────────────────────────────────────────────────────────────┐
│                    CONFIGURAZIONE CHIAVI API                     │
└─────────────────────────────────────────────────────────────────┘

🔹 SVILUPPO LOCALE
   📄 DocN.Client/appsettings.Development.json
   ✅ File già in .gitignore (sicuro)

🔹 PRODUZIONE
   🔐 Variabili d'ambiente
   ☁️ Azure Key Vault

🔹 RUNTIME (UI)
   💾 Database → Tabella AIConfigurations
   🌐 Pagina: /aiconfig
```

## 🔑 Quali Chiavi Servono?

### Per EMBEDDINGS (Vettori di Ricerca)

```json
{
  "Embeddings": {
    "Provider": "AzureOpenAI",
    "ApiKey": "YOUR_KEY",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "DeploymentName": "text-embedding-ada-002"
  }
}
```

**Usato da**: `EmbeddingService`, `MultiProviderAIService`

### Per CHAT (Generazione Risposte)

```json
{
  "AzureOpenAI": {
    "ApiKey": "YOUR_KEY",
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ChatDeploymentName": "gpt-4"
  }
}
```

**Usato da**: `ModernRAGService`, `MultiProviderAIService`

### Per RAG (Sistema Completo)

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "YOUR_KEY",
    "ChatDeploymentName": "gpt-4",
    "EmbeddingDeploymentName": "text-embedding-ada-002"
  }
}
```

**Usato da**: Tutti i servizi AI

## 🎯 Setup Minimo (5 Minuti)

### Passo 1: Copia il Template
```bash
cp DocN.Client/appsettings.Development.example.json \
   DocN.Client/appsettings.Development.json
```

### Passo 2: Ottieni Chiave Azure
1. [portal.azure.com](https://portal.azure.com) → Azure OpenAI
2. Keys and Endpoint → Copia KEY 1
3. Copia ENDPOINT

### Passo 3: Configura
```json
{
  "AzureOpenAI": {
    "Endpoint": "https://TUORISORSA.openai.azure.com/",
    "ApiKey": "LA_TUA_CHIAVE",
    "ChatDeploymentName": "gpt-4",
    "EmbeddingDeploymentName": "text-embedding-ada-002"
  }
}
```

### Passo 4: Verifica
```bash
dotnet run
# Cerca nei log: "✅ Semantic Memory configurata"
```

## 🔍 Quale Servizio Legge Dove?

| Servizio | Legge Da | Chiavi Usate |
|----------|----------|--------------|
| **EmbeddingService** | Database `AIConfigurations` | `AzureOpenAIKey`, `AzureOpenAIEndpoint`, `EmbeddingDeploymentName` |
| **MultiProviderAIService** | `appsettings.json` | `Embeddings:ApiKey`, `OpenAI:ApiKey`, `Gemini:ApiKey` |
| **ModernRAGService** | `Program.cs` → Semantic Kernel | `AzureOpenAI:ApiKey`, `AzureOpenAI:Endpoint` |

## ✅ Test Rapidi

### Test 1: Chiavi Caricate?
```bash
# Avvia app e cerca:
✅ Configurazione Azure OpenAI - Chat: gpt-4
✅ Configurazione Azure OpenAI - Embeddings: text-embedding-ada-002
```

### Test 2: Embeddings Funzionano?
```bash
# Carica un documento PDF
# Verifica che abbia un vettore:
SELECT TOP 1 Id, FileName, 
       CASE WHEN EmbeddingVector IS NOT NULL 
            THEN 'Vettore presente' 
            ELSE 'Nessun vettore' 
       END 
FROM Documents
```

### Test 3: RAG Funziona?
```
1. Carica 2-3 documenti
2. Usa ricerca semantica
3. Fai una domanda
4. Dovresti ricevere risposta con citazioni
```

## 🚨 Problemi Comuni

### ❌ "Azure OpenAI authentication failed"
**Soluzione**: Verifica chiave e endpoint
```bash
# Controlla:
- Endpoint termina con "/"
- Chiave non ha spazi
- Chiave non è scaduta
```

### ❌ "Deployment not found"
**Soluzione**: Verifica nomi deployment
```bash
# In Azure Portal verifica che esistano:
- gpt-4 (o gpt-35-turbo)
- text-embedding-ada-002
```

### ❌ "Embeddings returning null"
**Soluzione**: Configura sezione Embeddings
```json
{
  "Embeddings": {
    "Provider": "AzureOpenAI",
    "ApiKey": "TUA_CHIAVE"
  }
}
```

## 📚 Documentazione Completa

Per la guida completa e dettagliata:
**[CONFIGURAZIONE_API_KEYS.md](CONFIGURAZIONE_API_KEYS.md)**

Include:
- ✅ Tutte le opzioni di configurazione
- ✅ Guide per ottenere chiavi (Azure, OpenAI, Gemini)
- ✅ Best practices di sicurezza
- ✅ Esempi avanzati (Key Vault, multi-provider)
- ✅ Troubleshooting completo

## 🔐 Sicurezza Veloce

### ✅ Fai Così
```bash
# Usa variabili ambiente
export AzureOpenAI__ApiKey="TUA_CHIAVE"
```

### ❌ NON Fare
```bash
# Non committare chiavi
git commit appsettings.json  # NO!
```

---

**Per domande**: Consulta [CONFIGURAZIONE_API_KEYS.md](CONFIGURAZIONE_API_KEYS.md)
