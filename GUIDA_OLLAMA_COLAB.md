# Guida: Usare Ollama su Google Colab

Questa guida ti mostra come eseguire Ollama su Google Colab gratuitamente, senza dover installare nulla sul tuo computer locale.

## 🎯 Vantaggi

- ✅ **Gratuito**: Usa le GPU di Google Colab
- ✅ **Nessuna installazione locale**: Tutto nel cloud
- ✅ **Facile da configurare**: Pochi comandi
- ✅ **Modelli AI potenti**: Llama 3, Mistral, Gemma e altri

## 📋 Prerequisiti

- Account Google (gratuito)
- Browser web

## 🚀 Configurazione Passo-Passo

### 1. Apri Google Colab

Vai su [Google Colab](https://colab.research.google.com/) e crea un nuovo notebook.

### 2. Installa Ollama

Nel primo blocco di codice, esegui:

```python
# Installa Ollama
!curl -fsSL https://ollama.com/install.sh | sh

# Avvia il server Ollama in background
!nohup ollama serve > ollama.log 2>&1 &

# Aspetta che il server sia pronto
import time
time.sleep(5)

print("✅ Ollama installato e avviato!")
```

### 3. Scarica i Modelli

Scarica i modelli che ti servono:

```python
# Per la chat (scegli uno)
!ollama pull llama3          # Llama 3 (4.7GB)
!ollama pull llama3.1:8b     # Llama 3.1 8B (4.7GB)
!ollama pull mistral         # Mistral (4.1GB)
!ollama pull gemma:7b        # Gemma 7B (5GB)

# Per gli embeddings
!ollama pull nomic-embed-text  # Per embeddings (274MB)

print("✅ Modelli scaricati!")
```

### 4. Testa Ollama

Verifica che funzioni:

```python
# Test chat
!ollama run llama3 "Ciao! Come stai?"

# Test embeddings (in Python)
import requests
import json

def get_embedding(text):
    url = "http://localhost:11434/api/embeddings"
    data = {
        "model": "nomic-embed-text",
        "prompt": text
    }
    response = requests.post(url, json=data)
    return response.json()["embedding"]

embedding = get_embedding("Hello world")
print(f"✅ Embedding generato! Dimensione: {len(embedding)}")
```

### 5. Esponi Ollama con ngrok (Opzionale)

Se vuoi accedere a Ollama da fuori Colab:

```python
# Installa pyngrok
!pip install pyngrok -q

from pyngrok import ngrok

# Crea un tunnel pubblico
public_url = ngrok.connect(11434)
print(f"🌐 Ollama disponibile su: {public_url}")
print(f"📋 Usa questo endpoint in DocN: {public_url}")
```

### 6. Configurazione per DocN

Nel tuo `appsettings.json` o `appsettings.Development.json`:

**Se usi ngrok (accesso da fuori Colab):**

```json
{
  "AIProvider": {
    "DefaultProvider": "Ollama",
    "Ollama": {
      "Endpoint": "https://your-ngrok-url.ngrok-free.app",
      "EmbeddingModel": "nomic-embed-text",
      "ChatModel": "llama3"
    }
  }
}
```

**Se esegui DocN su Colab stesso:**

```json
{
  "AIProvider": {
    "DefaultProvider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "EmbeddingModel": "nomic-embed-text",
      "ChatModel": "llama3"
    }
  }
}
```

## 📝 Notebook Completo di Esempio

Ecco un notebook completo che puoi copiare e incollare:

```python
# ===== CELLA 1: Installazione =====
!curl -fsSL https://ollama.com/install.sh | sh
!nohup ollama serve > ollama.log 2>&1 &
import time
time.sleep(5)
print("✅ Ollama installato!")

# ===== CELLA 2: Download Modelli =====
!ollama pull llama3
!ollama pull nomic-embed-text
print("✅ Modelli pronti!")

# ===== CELLA 3: Test Chat =====
!ollama run llama3 "Spiegami cos'è un RAG system in modo semplice"

# ===== CELLA 4: Test Embeddings =====
import requests
import json

def generate_embedding(text):
    response = requests.post(
        "http://localhost:11434/api/embeddings",
        json={"model": "nomic-embed-text", "prompt": text}
    )
    return response.json()["embedding"]

# Prova
embedding = generate_embedding("DocN è un sistema RAG documentale")
print(f"✅ Embedding: {len(embedding)} dimensioni")

# ===== CELLA 5: Esponi con ngrok (opzionale) =====
!pip install pyngrok -q
from pyngrok import ngrok

public_url = ngrok.connect(11434)
print(f"🌐 Ollama URL pubblico: {public_url}")
```

## 🎓 Suggerimenti

### Gestione RAM e GPU

Google Colab Free offre:
- **RAM**: ~12GB
- **GPU**: T4 (quando disponibile)

**Suggerimenti per modelli:**
- Modelli fino a 7B parametri: Funzionano bene
- Modelli 13B+: Potrebbero essere lenti o non funzionare

### Modelli Consigliati per Colab Free

| Modello | Dimensione | Uso | RAM richiesta |
|---------|-----------|-----|---------------|
| `llama3.1:8b` | 4.7GB | Chat, analisi | ~6GB |
| `gemma:7b` | 5GB | Chat generale | ~6GB |
| `mistral` | 4.1GB | Chat veloce | ~5GB |
| `nomic-embed-text` | 274MB | Embeddings | ~500MB |
| `phi3:mini` | 2.3GB | Chat leggero | ~3GB |

### Persistenza

⚠️ **Importante**: Quando chiudi il notebook Colab, **tutto viene cancellato**. 

Per mantenere i modelli tra sessioni:
1. Usa Colab Pro (ha più persistenza)
2. Oppure scarica i modelli ogni volta (automatizzabile)

### Errori Comuni

**Errore: "connection refused"**
```python
# Aspetta di più dopo l'avvio
import time
time.sleep(10)  # Aumenta l'attesa
```

**Errore: "out of memory"**
```python
# Usa modelli più piccoli
!ollama pull phi3:mini  # Solo 2.3GB
```

**Ngrok non funziona**
```python
# Registrati su ngrok.com e ottieni un authtoken
!ngrok authtoken YOUR_TOKEN_HERE
```

## 🔗 Risorse Utili

- [Ollama Models](https://ollama.com/library) - Tutti i modelli disponibili
- [Ollama API Docs](https://github.com/ollama/ollama/blob/main/docs/api.md) - Documentazione API
- [Ngrok Setup](https://dashboard.ngrok.com/get-started/setup) - Setup ngrok

## 💡 Prossimi Passi

Dopo aver configurato Ollama su Colab:

1. ✅ Testa i modelli in Colab
2. ✅ Configura DocN per usare Ollama
3. ✅ Carica documenti e prova la ricerca semantica
4. ✅ Se hai bisogno di più potenza, considera:
   - Ollama locale (vedi `GUIDA_OLLAMA_LOCALE.md`)
   - Groq API (vedi `GUIDA_GROQ.md`)

## 🆘 Supporto

Hai problemi? Apri un issue su [GitHub](https://github.com/Moncymr/DocN/issues) con:
- Output del comando `!ollama list`
- Log di errore completo
- Modello che stai usando
