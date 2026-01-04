# Guida: Configurazione Groq API

Groq è un servizio cloud che offre inferenza AI **estremamente veloce** con un tier gratuito generoso. È perfetto per chi vuole prestazioni elevate senza installare nulla localmente.

## 🎯 Vantaggi

- ✅ **Velocissimo**: Fino a 10x più veloce di OpenAI
- ✅ **Tier gratuito generoso**: 14,400 richieste/giorno
- ✅ **API compatibile con OpenAI**: Facile da integrare
- ✅ **Modelli open-source**: Llama 3, Mixtral, Gemma
- ✅ **Nessuna installazione**: Pronto in 5 minuti

## 🚀 Vantaggi vs Ollama

| Caratteristica | Groq | Ollama Locale |
|---------------|------|---------------|
| **Velocità** | ⚡⚡⚡⚡⚡ Velocissimo | ⚡⚡ Dipende dall'hardware |
| **Costo** | Gratuito (limiti) | Gratuito (illimitato) |
| **Setup** | 5 minuti | 30 minuti |
| **Privacy** | Cloud | 100% locale |
| **Hardware** | Non richiesto | GPU consigliata |
| **Embeddings** | ❌ Non supportati | ✅ Supportati |

**Quando usare Groq:**
- Vuoi velocità massima
- Non hai hardware potente
- Non ti servono embeddings (usa Gemini/OpenAI per quelli)

**Quando usare Ollama:**
- Vuoi privacy totale
- Ti servono embeddings
- Hai una buona GPU

## 📋 Registrazione

### 1. Crea un Account Groq

1. Vai su [console.groq.com](https://console.groq.com/)
2. Clicca su "Sign Up" (puoi usare Google)
3. Verifica la tua email

### 2. Ottieni l'API Key

1. Una volta loggato, vai su [API Keys](https://console.groq.com/keys)
2. Clicca su "Create API Key"
3. Dai un nome alla key (es. "DocN Development")
4. **Copia la key** (la vedrai solo una volta!)
5. Salva la key in un posto sicuro

## ⚙️ Configurazione per DocN

### Opzione 1: User Secrets (Consigliato per Sviluppo)

```bash
cd DocN.Server
dotnet user-secrets init
dotnet user-secrets set "AIProvider:DefaultProvider" "Groq"
dotnet user-secrets set "AIProvider:Groq:ApiKey" "gsk_tua_api_key_qui"
dotnet user-secrets set "AIProvider:Groq:ChatModel" "llama-3.1-8b-instant"
```

### Opzione 2: appsettings.json (NON committare!)

Nel file `DocN.Server/appsettings.Development.json`:

```json
{
  "AIProvider": {
    "DefaultProvider": "Groq",
    "Groq": {
      "ApiKey": "gsk_tua_api_key_qui",
      "ChatModel": "llama-3.1-8b-instant",
      "Endpoint": "https://api.groq.com/openai/v1"
    }
  }
}
```

⚠️ **IMPORTANTE**: Aggiungi `appsettings.Development.json` al `.gitignore`!

### Opzione 3: Variabili d'Ambiente

```bash
# Linux/macOS
export AIProvider__Groq__ApiKey="gsk_tua_api_key_qui"
export AIProvider__DefaultProvider="Groq"

# Windows PowerShell
$env:AIProvider__Groq__ApiKey = "gsk_tua_api_key_qui"
$env:AIProvider__DefaultProvider = "Groq"
```

## 🤖 Modelli Disponibili

### Modelli per Chat

| Modello | Velocità | Context Window | Descrizione |
|---------|----------|---------------|-------------|
| `llama-3.1-8b-instant` | ⚡⚡⚡⚡⚡ | 128K | **Consigliato** - Veloce e preciso |
| `llama-3.1-70b-versatile` | ⚡⚡⚡⚡ | 128K | Più potente ma più lento |
| `mixtral-8x7b-32768` | ⚡⚡⚡⚡ | 32K | Ottimo bilanciamento |
| `gemma2-9b-it` | ⚡⚡⚡⚡ | 8K | Modello Google |

### ⚠️ Nota sugli Embeddings

**Groq NON supporta embeddings nativamente.**

Per gli embeddings, devi usare un altro provider. Configurazione consigliata:

```json
{
  "AIProvider": {
    "DefaultProvider": "Groq",
    "Groq": {
      "ApiKey": "gsk_tua_groq_key",
      "ChatModel": "llama-3.1-8b-instant"
    },
    "Gemini": {
      "ApiKey": "tua_gemini_key",
      "EmbeddingModel": "text-embedding-004"
    }
  }
}
```

Oppure usa Ollama locale solo per gli embeddings:

```json
{
  "AIProvider": {
    "DefaultProvider": "Groq",
    "Groq": {
      "ApiKey": "gsk_tua_groq_key",
      "ChatModel": "llama-3.1-8b-instant"
    },
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "EmbeddingModel": "nomic-embed-text"
    }
  }
}
```

## 📊 Limiti del Tier Gratuito

| Risorsa | Limite Gratuito |
|---------|----------------|
| **Richieste/minuto** | 30 |
| **Richieste/giorno** | 14,400 |
| **Token/minuto** | 30,000 |
| **Crediti mensili** | Generosi |

💡 **Tip**: Questi limiti sono **molto generosi** per sviluppo e piccoli progetti!

## 🧪 Test dell'Integrazione

### Test 1: Verifica Connessione

```bash
# Con curl
curl https://api.groq.com/openai/v1/models \
  -H "Authorization: Bearer gsk_tua_api_key"
```

### Test 2: Chat Completion

```bash
curl https://api.groq.com/openai/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer gsk_tua_api_key" \
  -d '{
    "model": "llama-3.1-8b-instant",
    "messages": [
      {"role": "user", "content": "Ciao! Presentati brevemente."}
    ]
  }'
```

### Test 3: Da DocN

1. Avvia DocN con la configurazione Groq
2. Vai nella sezione Chat
3. Fai una domanda
4. Dovresti vedere risposte **molto veloci** (< 1 secondo)

## 🎯 Best Practices

### 1. Gestione Rate Limits

```csharp
// DocN gestisce automaticamente i rate limits
// Ma puoi ottimizzare le richieste:

// ✅ Buono: Una richiesta per documento
var result = await provider.AnalyzeDocumentAsync(text, categories);

// ❌ Evita: Troppe richieste in parallelo
// Groq ha limite di 30 req/min
```

### 2. Scelta del Modello

```json
{
  "Groq": {
    "ChatModel": "llama-3.1-8b-instant"  // ✅ Default consigliato
  }
}
```

**Quando usare altri modelli:**
- `llama-3.1-70b-versatile`: Documenti complessi, analisi avanzata
- `mixtral-8x7b-32768`: Documenti lunghi (32K context)
- `gemma2-9b-it`: Alternative Google

### 3. Gestione Errori

Groq può restituire questi errori comuni:

```
401 Unauthorized -> Controlla l'API key
429 Rate limit -> Aspetta 1 minuto
503 Service unavailable -> Riprova tra poco
```

DocN gestisce automaticamente questi errori con retry.

### 4. Monitoraggio Uso

Controlla il tuo uso su:
- [console.groq.com/settings/limits](https://console.groq.com/settings/limits)

## 🔄 Configurazione Multi-Provider (Consigliato)

La migliore configurazione combina Groq per chat + altro provider per embeddings:

### Setup Completo

```json
{
  "AIProvider": {
    "DefaultProvider": "Groq",
    
    "Groq": {
      "ApiKey": "gsk_tua_groq_key",
      "ChatModel": "llama-3.1-8b-instant"
    },
    
    "Gemini": {
      "ApiKey": "tua_gemini_key",
      "EmbeddingModel": "text-embedding-004"
    }
  },
  
  "Services": {
    "ChatProvider": "Groq",
    "EmbeddingProvider": "Gemini",
    "TagExtractionProvider": "Groq",
    "RAGProvider": "Groq"
  }
}
```

**Vantaggi:**
- ✅ Chat velocissima con Groq
- ✅ Embeddings di qualità con Gemini
- ✅ Tutto gratuito (nei limiti)

## 💰 Confronto Costi

| Provider | Tier Gratuito | Costo dopo gratuito |
|----------|--------------|---------------------|
| **Groq** | 14,400 req/giorno | Molto economico |
| **OpenAI** | $5 credito iniziale | Costoso |
| **Gemini** | 60 req/min gratis | Gratuito generoso |
| **Ollama** | Illimitato | Gratuito (hardware tuo) |

## ❓ FAQ

### Q: Groq supporta embeddings?
**A**: No, usa Gemini, OpenAI o Ollama per gli embeddings.

### Q: Quanto è veloce Groq?
**A**: 10x più veloce di OpenAI. Risposte in < 1 secondo.

### Q: Posso usare Groq in produzione?
**A**: Sì! Ma monitora i limiti. Considera un upgrade se superi il tier gratuito.

### Q: Groq è compatibile con OpenAI?
**A**: Sì, 100%. DocN usa lo stesso client.

### Q: I miei dati sono privati?
**A**: Groq è un servizio cloud. Per privacy totale usa Ollama locale.

### Q: Posso combinare Groq + Ollama?
**A**: Assolutamente! Usa Groq per chat (veloce) e Ollama per embeddings (privacy).

## 🔒 Sicurezza API Key

### ✅ DO:
- Usa User Secrets in sviluppo
- Usa variabili d'ambiente in produzione
- Rotazione periodica delle key
- Limita le key a IP specifici (quando possibile)

### ❌ DON'T:
- Committare API key nel codice
- Condividere key pubblicamente
- Usare la stessa key ovunque

### Rotazione Key

1. Vai su [console.groq.com/keys](https://console.groq.com/keys)
2. Crea una nuova key
3. Aggiorna la configurazione
4. Revoca la vecchia key

## 📈 Monitoraggio Performance

Groq fornisce statistiche dettagliate:

1. Vai su [console.groq.com](https://console.groq.com/)
2. Controlla:
   - Request count
   - Token usage
   - Latency
   - Error rate

## 🆘 Troubleshooting

### Errore: "Invalid API key"

```bash
# Verifica che la key sia corretta
dotnet user-secrets list

# Assicurati che inizi con "gsk_"
```

### Errore: "Rate limit exceeded"

```bash
# Aspetta 60 secondi e riprova
# Oppure implementa exponential backoff
```

### Errore: "Model not found"

```json
{
  "Groq": {
    "ChatModel": "llama-3.1-8b-instant"  // ✅ Nome corretto
    // "ChatModel": "llama3"  // ❌ Nome sbagliato
  }
}
```

### Performance lente

1. Verifica la tua connessione internet
2. Controlla se hai superato i rate limits
3. Prova un modello più piccolo (`llama-3.1-8b-instant`)

## 🔗 Link Utili

- [Groq Console](https://console.groq.com/)
- [Groq Documentation](https://console.groq.com/docs)
- [API Reference](https://console.groq.com/docs/api-reference)
- [Models](https://console.groq.com/docs/models)
- [Pricing](https://console.groq.com/settings/billing)

## 💡 Prossimi Passi

Dopo la configurazione:

1. ✅ Testa la chat in DocN
2. ✅ Configura un provider per embeddings (Gemini/Ollama)
3. ✅ Monitora l'uso su console.groq.com
4. ✅ Considera upgrade se necessario

Alternative:
- 📝 [Guida Ollama Locale](GUIDA_OLLAMA_LOCALE.md) - Per privacy totale
- 📝 [Guida Colab](GUIDA_OLLAMA_COLAB.md) - Ollama gratis nel cloud
- 📝 [README](README.md) - Torna alla documentazione
