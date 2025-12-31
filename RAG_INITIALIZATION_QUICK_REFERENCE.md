# 🚀 RAG Provider Initialization - Quick Reference

## ❓ Domanda: "Dove inizializza il provider per RAG dei miei documenti?"

### 📍 Risposta Breve

**File**: `DocN.Server/Program.cs`  
**Riga**: 324  
**Codice**:
```csharp
builder.Services.AddScoped<ISemanticRAGService, MultiProviderSemanticRAGService>();
```

---

## 🔄 Flow di Inizializzazione (5 Passi)

```
┌─────────────────────────────────────────────────────────────┐
│  1️⃣  STARTUP - Program.cs (Riga 324)                       │
│     Registrazione servizio nel DI Container                │
│                                                             │
│     builder.Services.AddScoped<ISemanticRAGService,        │
│                   MultiProviderSemanticRAGService>();      │
└──────────────────────┬──────────────────────────────────────┘
                       │ Applicazione avviata
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  2️⃣  HTTP REQUEST - Client invia messaggio chat           │
│     POST https://localhost:5211/api/SemanticChat/query     │
│                                                             │
│     { message: "Che documenti ho?", userId: "demo-user" }  │
└──────────────────────┬──────────────────────────────────────┘
                       │ ASP.NET Core routing
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  3️⃣  CONTROLLER - SemanticChatController.Query()          │
│     Dependency Injection automatico                         │
│                                                             │
│     ISemanticRAGService _ragService  ← Iniettato qui!      │
└──────────────────────┬──────────────────────────────────────┘
                       │ _ragService.GenerateResponseAsync()
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  4️⃣  RAG SERVICE - MultiProviderSemanticRAGService        │
│     Usa IMultiProviderAIService                             │
│                                                             │
│     • SearchDocumentsAsync()     ← Vector search            │
│     • GenerateEmbeddingAsync()   ← Via AIService            │
│     • GenerateChatAsync()        ← Via AIService            │
└──────────────────────┬──────────────────────────────────────┘
                       │ Carica configurazione
                       ▼
┌─────────────────────────────────────────────────────────────┐
│  5️⃣  AI SERVICE - MultiProviderAIService                  │
│     GetActiveConfigurationAsync()                           │
│                                                             │
│     ✅ PRIORITÀ: Database AIConfigurations (IsActive=true) │
│     ⬇️ FALLBACK: appsettings.json (Gemini/OpenAI/Azure)   │
│     ⚡ CACHE: 5 minuti per performance                     │
└─────────────────────────────────────────────────────────────┘
```

---

## ⚙️ Configurazione Provider

### Opzione 1: Database (Consigliato) ✨

```sql
-- Tabella: AIConfigurations
SELECT 
    ConfigurationName,
    ChatProvider,          -- 0=Gemini, 1=OpenAI, 2=AzureOpenAI
    EmbeddingsProvider,
    RAGProvider,
    IsActive               -- ← DEVE essere TRUE!
FROM AIConfigurations
WHERE IsActive = 1;
```

**Come configurare**:
1. Vai su https://localhost:7114/config
2. Clicca "Add New Configuration"
3. Compila i campi (API Keys, modelli, ecc.)
4. Attiva con toggle "Active"
5. Salva → Configurazione applicata immediatamente!

### Opzione 2: appsettings.json (Fallback) 📄

```json
{
  "Gemini": {
    "ApiKey": "your-gemini-api-key",
    "Model": "gemini-2.0-flash-exp"
  },
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Model": "gpt-4"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-azure-key",
    "ChatDeployment": "gpt-4",
    "EmbeddingDeployment": "text-embedding-ada-002"
  },
  "AI": {
    "Provider": "Gemini",      ← Default provider
    "EnableFallback": true     ← Abilita fallback automatico
  }
}
```

**Quando viene usato**:
- ⚠️ Solo se NON c'è configurazione nel database
- ✅ Utile per setup iniziale
- ✅ Utile per testing/development

---

## 🔍 Verificare Provider Attivo

### Metodo 1: Logs del Server

Guarda i logs quando fai una query:

```
[INFO] Using Gemini for embedding generation
[INFO] RAG response generated in 1234ms with 3 documents using Gemini
```

### Metodo 2: API Response Metadata

La risposta include metadata sul provider:

```json
{
  "answer": "...",
  "metadata": {
    "provider": "Gemini",
    "documentsRetrieved": 3,
    "topSimilarityScore": 0.89
  }
}
```

### Metodo 3: Database Query

```sql
SELECT 
    ConfigurationName,
    ChatProvider,
    EmbeddingsProvider,
    IsActive,
    UpdatedAt
FROM AIConfigurations
WHERE IsActive = 1;
```

---

## 🐛 Troubleshooting

### Errore: "AI_PROVIDER_NOT_CONFIGURED"

**Causa**: Nessun provider configurato

**Soluzione**:
1. Vai in `/config` (Settings)
2. Aggiungi configurazione AI
3. Inserisci API key valida
4. Attiva configurazione
5. Riprova query

### Errore: "No relevant documents found"

**Causa**: Documenti non hanno embeddings

**Soluzione**:
1. Vai in `/upload`
2. Carica almeno un documento
3. Aspetta che venga processato (vedi logs)
4. Riprova query

### Errore: "Failed to generate query embedding"

**Causa**: API key non valida o provider non raggiungibile

**Soluzione**:
1. Verifica API key in Settings
2. Controlla connessione internet
3. Controlla limiti API (rate limits)
4. Prova con provider diverso (fallback)

### Nessun errore ma risposta vuota

**Causa**: Configurazione non attiva

**Soluzione**:
```sql
-- Verifica quale configurazione è attiva
SELECT * FROM AIConfigurations WHERE IsActive = 1;

-- Se nessuna, attiva una configurazione
UPDATE AIConfigurations 
SET IsActive = 1 
WHERE Id = <tuo-config-id>;
```

---

## 📚 File Chiave (Codice)

| File | Responsabilità | Riga Chiave |
|------|----------------|-------------|
| `DocN.Server/Program.cs` | Registrazione DI | 324 |
| `SemanticChatController.cs` | Endpoint API | 22-32 (constructor) |
| `MultiProviderSemanticRAGService.cs` | Logica RAG | 19-27 (constructor) |
| `MultiProviderAIService.cs` | Gestione provider | 45-68 (GetActiveConfiguration) |

---

## 💡 Domande Frequenti

### Q: Devo inizializzare manualmente il provider?
**A**: NO! Tutto è automatico tramite Dependency Injection.

### Q: Quando viene caricata la configurazione?
**A**: Alla prima richiesta, poi rimane in cache per 5 minuti.

### Q: Posso cambiare provider senza riavviare?
**A**: SÌ! Modifica configurazione in Settings, verrà applicata entro 5 minuti (durata cache).

### Q: Posso usare più provider contemporaneamente?
**A**: SÌ! Puoi assegnare provider diversi per Chat, Embeddings, e Tag Extraction.

### Q: Cosa succede se il provider primario fallisce?
**A**: Se `EnableFallback = true`, il sistema prova provider alternativi automaticamente.

### Q: Come forzo il ricaricamento della configurazione?
**A**: Riavvia il server oppure aspetta 5 minuti (durata cache).

---

## 📖 Documentazione Completa

Per approfondimenti: **[RAG_PROVIDER_INITIALIZATION_GUIDE.md](RAG_PROVIDER_INITIALIZATION_GUIDE.md)**

---

## ✅ Checklist Setup Rapido

- [ ] 1. Avvia SQL Server
- [ ] 2. Avvia DocN.Server (porta 5211)
- [ ] 3. Avvia DocN.Client (porta 7114)
- [ ] 4. Vai su https://localhost:7114/config
- [ ] 5. Aggiungi configurazione AI (Gemini/OpenAI/Azure)
- [ ] 6. Inserisci API key valida
- [ ] 7. Attiva configurazione (toggle "Active")
- [ ] 8. Salva configurazione
- [ ] 9. Vai su /upload e carica un documento
- [ ] 10. Attendi processamento (vedi logs)
- [ ] 11. Vai su /chat e fai una domanda
- [ ] 12. ✨ RAG funzionante!

---

**Creato il**: 2025-12-31  
**Autore**: GitHub Copilot  
**Versione**: 1.0
