# 🔧 Troubleshooting Ollama e Groq

Guida per risolvere i problemi più comuni con Ollama e Groq.

## 🚨 Problema: Chat si blocca con tre puntini (timeout dopo 300s)

### Causa Principale
Il sistema non riesce a generare la risposta del provider AI configurato.

### Soluzioni per Provider

#### **Groq (Chat/RAG)**

1. **Verifica API Key**
   ```bash
   # Test manuale con curl
   curl -X POST "https://api.groq.com/openai/v1/chat/completions" \
     -H "Authorization: Bearer gsk_tua_key" \
     -H "Content-Type: application/json" \
     -d '{
       "model": "llama-3.1-8b-instant",
       "messages": [{"role": "user", "content": "Hello"}],
       "max_tokens": 100
     }'
   ```

2. **Controlla Limiti API**
   - Free tier: 14,400 richieste/giorno
   - Verifica su https://console.groq.com/settings/limits
   - Se superato, aspetta reset o usa altro provider

3. **Modello Corretto**
   - Verifica che il modello sia disponibile:
     - `llama-3.1-8b-instant` ✅ (consigliato)
     - `llama-3.1-70b-versatile` ✅
     - `mixtral-8x7b-32768` ✅
   - Lista completa: https://console.groq.com/docs/models

4. **Configurazione Database**
   ```sql
   -- Verifica configurazione
   SELECT 
       ChatProvider,
       RAGProvider,
       GroqApiKey,
       GroqChatModel,
       GroqEndpoint
   FROM AIConfigurations
   WHERE IsActive = 1;
   
   -- Deve mostrare:
   -- ChatProvider = 5 (Groq)
   -- RAGProvider = 5 (Groq)
   -- GroqApiKey = 'gsk_...'
   -- GroqChatModel = 'llama-3.1-8b-instant'
   -- GroqEndpoint = 'https://api.groq.com/openai/v1'
   ```

#### **Gemini (Embeddings/Tags)**

1. **Verifica API Key**
   ```bash
   # Test manuale
   curl "https://generativelanguage.googleapis.com/v1beta/models?key=tua_key"
   ```

2. **Controlla Quota**
   - Free tier: 60 richieste/minuto
   - Verifica su https://makersuite.google.com/app/apikey

3. **Configurazione Database**
   ```sql
   -- Verifica configurazione
   SELECT 
       EmbeddingsProvider,
       TagExtractionProvider,
       GeminiApiKey,
       GeminiEmbeddingModel
   FROM AIConfigurations
   WHERE IsActive = 1;
   
   -- Deve mostrare:
   -- EmbeddingsProvider = 2 (Gemini)
   -- TagExtractionProvider = 2 (Gemini)
   -- GeminiApiKey = 'AI...'
   -- GeminiEmbeddingModel = 'text-embedding-004'
   ```

#### **Ollama (Locale)**

1. **Verifica Server Attivo**
   ```bash
   # Deve rispondere con lista modelli
   curl http://localhost:11434/api/tags
   ```

2. **Avvia Ollama se non attivo**
   ```bash
   # Windows/macOS
   ollama serve
   
   # Linux (systemd)
   sudo systemctl start ollama
   ```

3. **Verifica Modelli Installati**
   ```bash
   ollama list
   
   # Se manca il modello per chat
   ollama pull llama3
   
   # Se manca il modello per embeddings
   ollama pull nomic-embed-text
   ```

## 🔍 Diagnostica Completa

### Passo 1: Test Configurazione UI

1. Vai su `/config` nel browser
2. Clicca "🔌 Testa Connessione"
3. Verifica che mostri:
   ```
   ✅ Gemini: Embeddings configurato
   ✅ Groq: Chat configurato
   ```

### Passo 2: Verifica Log Server

Cerca nel log del server (DocN.Server):

```bash
# Cerca errori AI
grep -i "error\|exception\|failed" logs/app.log | grep -i "ai\|groq\|gemini"

# Pattern da cercare:
# ❌ "API key non configurata"
# ❌ "Provider non supportato"
# ❌ "Quota exceeded"
# ❌ "Timeout"
```

### Passo 3: Test Manuale Provider

**Test Groq:**
```bash
curl -X POST "https://api.groq.com/openai/v1/chat/completions" \
  -H "Authorization: Bearer gsk_tua_key_qui" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.1-8b-instant",
    "messages": [
      {"role": "system", "content": "Sei un assistente utile"},
      {"role": "user", "content": "Ciao"}
    ],
    "temperature": 0.7,
    "max_tokens": 100
  }'
```

**Test Gemini:**
```bash
curl -X POST \
  "https://generativelanguage.googleapis.com/v1beta/models/text-embedding-004:embedContent?key=tua_key" \
  -H "Content-Type: application/json" \
  -d '{
    "content": {
      "parts": [{"text": "Hello world"}]
    }
  }'
```

## 🛠️ Soluzioni Rapide

### Soluzione 1: Cache Configuration
```bash
# Svuota cache configurazione
curl -X POST http://localhost:5000/api/config/clear-cache
```

### Soluzione 2: Riavvia Applicazione
```bash
# Ferma il server
# Riavvia il server
# Le configurazioni verranno ricaricate
```

### Soluzione 3: Fallback Automatico

Se Groq non funziona, abilita fallback:

```sql
UPDATE AIConfigurations
SET EnableFallback = 1
WHERE IsActive = 1;
```

Con fallback abilitato, se Groq fallisce proverà automaticamente:
1. Gemini
2. OpenAI (se configurato)
3. Azure OpenAI (se configurato)
4. Ollama (se configurato)

### Soluzione 4: Configurazione Mista Ottimale

**Setup consigliato per massima affidabilità:**

```json
{
  "AIProvider": {
    "ChatProvider": "Groq",
    "EmbeddingsProvider": "Gemini",
    "TagExtractionProvider": "Gemini",
    "RAGProvider": "Groq",
    "EnableFallback": true,
    "Groq": {
      "ApiKey": "gsk_...",
      "ChatModel": "llama-3.1-8b-instant"
    },
    "Gemini": {
      "ApiKey": "AI...",
      "EmbeddingModel": "text-embedding-004",
      "ChatModel": "gemini-2.0-flash-exp"
    }
  }
}
```

Con questa configurazione:
- Chat velocissima con Groq
- Embeddings accurati con Gemini
- Se Groq ha problemi, usa Gemini come fallback automatico

## 📊 Checklist Problemi Comuni

- [ ] API key corrette e valide
- [ ] Quota non superata
- [ ] Modelli esistenti e disponibili
- [ ] Endpoint corretti
- [ ] EnableFallback = true
- [ ] Database aggiornato con script 012
- [ ] Server riavviato dopo modifiche
- [ ] Cache svuotata
- [ ] Test di configurazione passa

## 🆘 Ancora Problemi?

### Verifica Logs Dettagliati

1. **Abilita logging verbose** in `appsettings.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "DocN.Data.Services": "Debug",
         "DocN.Core.AI": "Debug"
       }
     }
   }
   ```

2. **Riproduci errore** e cerca nei log:
   ```bash
   tail -f logs/app.log | grep -i "ai\|chat\|embedding"
   ```

### Report Bug

Se il problema persiste, apri una issue con:
1. ✅ Output di `/api/config/diagnostics`
2. ✅ Output di test configurazione UI
3. ✅ Logs rilevanti (ultimi 50 righe con errori)
4. ✅ Configurazione provider (senza API keys)
5. ✅ Query SQL risultati

## 💡 Tips Performance

1. **Groq è velocissimo** ma ha limiti
   - Se superi quota, passa a Gemini temporaneamente
   - Monitora uso su console.groq.com

2. **Gemini è gratuito** ma più lento
   - Ottimo per embeddings (alta qualità)
   - Usa per fallback chat se Groq non disponibile

3. **Ollama è locale** quindi illimitato
   - Richiede GPU per performance accettabili
   - Ottimo se hai hardware potente
   - Zero costi API

4. **Configurazione Ibrida** = Best Practice
   - Groq per chat (veloce)
   - Gemini per embeddings (accurato)
   - Ollama per sviluppo locale
   - Fallback abilitato sempre
