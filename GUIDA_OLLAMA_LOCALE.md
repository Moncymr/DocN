# Guida: Installazione Ollama in Locale

Questa guida ti mostra come installare e configurare Ollama sul tuo computer per eseguire modelli AI localmente.

## 🎯 Vantaggi

- ✅ **Privacy totale**: I dati non lasciano il tuo computer
- ✅ **Nessun costo API**: Gratuito dopo l'installazione
- ✅ **Offline**: Funziona senza connessione internet
- ✅ **Veloce**: Nessuna latenza di rete
- ✅ **Controllo completo**: Scegli i modelli che preferisci

## 📋 Requisiti di Sistema

### Minimi (Chat di base)
- **RAM**: 8GB
- **Storage**: 10GB liberi
- **CPU**: Qualsiasi CPU moderna

### Raccomandati (Performance ottimali)
- **RAM**: 16GB+
- **Storage**: 50GB+ liberi
- **GPU**: NVIDIA con 6GB+ VRAM (opzionale ma consigliato)

## 🚀 Installazione

### Windows

1. **Scarica Ollama**
   - Vai su [ollama.com/download](https://ollama.com/download)
   - Scarica il file `.exe` per Windows
   - Esegui l'installer e segui le istruzioni

2. **Verifica l'installazione**
   ```powershell
   ollama --version
   ```

3. **Avvia Ollama**
   - Ollama si avvia automaticamente come servizio
   - Verifica che sia in esecuzione: http://localhost:11434

### macOS

1. **Scarica Ollama**
   - Vai su [ollama.com/download](https://ollama.com/download)
   - Scarica il file `.dmg` per macOS
   - Trascina Ollama nella cartella Applicazioni

2. **Verifica l'installazione**
   ```bash
   ollama --version
   ```

3. **Avvia Ollama**
   - Ollama si avvia automaticamente
   - Verifica: http://localhost:11434

### Linux

1. **Installa con script automatico**
   ```bash
   curl -fsSL https://ollama.com/install.sh | sh
   ```

2. **Verifica l'installazione**
   ```bash
   ollama --version
   ```

3. **Avvia Ollama**
   ```bash
   ollama serve
   ```

### Docker (Qualsiasi OS)

1. **Esegui Ollama in Docker**
   ```bash
   docker run -d \
     -v ollama:/root/.ollama \
     -p 11434:11434 \
     --name ollama \
     ollama/ollama
   ```

2. **Con supporto GPU NVIDIA**
   ```bash
   docker run -d \
     --gpus all \
     -v ollama:/root/.ollama \
     -p 11434:11434 \
     --name ollama \
     ollama/ollama
   ```

## 📦 Download Modelli

### Modelli per Chat

```bash
# Llama 3.1 - Modello più recente di Meta (consigliato)
ollama pull llama3.1:8b      # 4.7GB - Ottimo bilanciamento

# Llama 3 - Versione precedente
ollama pull llama3           # 4.7GB - Molto buono

# Mistral - Veloce ed efficiente
ollama pull mistral          # 4.1GB - Ottimo per chat

# Gemma - Modello di Google
ollama pull gemma:7b         # 5GB - Buono per conversazioni

# Phi-3 - Microsoft, molto leggero
ollama pull phi3:mini        # 2.3GB - Per computer con poca RAM
```

### Modelli per Embeddings

```bash
# Nomic Embed Text - Il migliore per embeddings (consigliato)
ollama pull nomic-embed-text  # 274MB

# All-MiniLM - Alternativa leggera
ollama pull all-minilm        # 46MB
```

### Modelli Multimodali (Immagini + Testo)

```bash
# LLaVA - Comprende immagini
ollama pull llava             # 4.7GB

# Bakllava - Alternativa a LLaVA
ollama pull bakllava          # 4.5GB
```

## ⚙️ Configurazione per DocN

### 1. Verifica che Ollama sia in esecuzione

Apri un browser e vai su: http://localhost:11434

Dovresti vedere: `Ollama is running`

### 2. Configura appsettings.json

Nel file `DocN.Server/appsettings.json` o `appsettings.Development.json`:

```json
{
  "AIProvider": {
    "DefaultProvider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "EmbeddingModel": "nomic-embed-text",
      "ChatModel": "llama3.1:8b"
    }
  }
}
```

### 3. Oppure usa User Secrets (consigliato per sviluppo)

```bash
cd DocN.Server
dotnet user-secrets set "AIProvider:DefaultProvider" "Ollama"
dotnet user-secrets set "AIProvider:Ollama:Endpoint" "http://localhost:11434"
dotnet user-secrets set "AIProvider:Ollama:EmbeddingModel" "nomic-embed-text"
dotnet user-secrets set "AIProvider:Ollama:ChatModel" "llama3.1:8b"
```

## 🎮 Comandi Utili

### Gestione Modelli

```bash
# Lista modelli installati
ollama list

# Rimuovi un modello
ollama rm llama3

# Mostra info su un modello
ollama show llama3

# Aggiorna un modello
ollama pull llama3
```

### Test Modelli

```bash
# Test chat interattiva
ollama run llama3

# Test con un prompt singolo
ollama run llama3 "Spiegami cos'è un RAG system"

# Test embeddings (via API)
curl http://localhost:11434/api/embeddings -d '{
  "model": "nomic-embed-text",
  "prompt": "Hello world"
}'
```

### Gestione Servizio

**Linux/macOS:**
```bash
# Avvia Ollama
ollama serve

# Stop (Ctrl+C nel terminale)
```

**Windows:**
```powershell
# Ollama gira come servizio, controlla con:
Get-Service ollama

# Stop servizio
Stop-Service ollama

# Start servizio
Start-Service ollama
```

**Docker:**
```bash
# Stop container
docker stop ollama

# Start container
docker start ollama

# Rimuovi container
docker rm ollama

# Vedi logs
docker logs ollama
```

## 📊 Tabella Comparativa Modelli

| Modello | Dimensione | RAM Minima | Velocità | Qualità | Uso Consigliato |
|---------|-----------|-----------|----------|---------|-----------------|
| `phi3:mini` | 2.3GB | 4GB | ⚡⚡⚡⚡ | ⭐⭐⭐ | Computer vecchi |
| `mistral` | 4.1GB | 8GB | ⚡⚡⚡ | ⭐⭐⭐⭐ | Bilanciato |
| `llama3.1:8b` | 4.7GB | 8GB | ⚡⚡⚡ | ⭐⭐⭐⭐⭐ | Migliore generale |
| `gemma:7b` | 5GB | 10GB | ⚡⚡ | ⭐⭐⭐⭐ | Conversazioni |
| `llama3:70b` | 40GB | 64GB | ⚡ | ⭐⭐⭐⭐⭐ | Server potenti |

**Embeddings:**
| Modello | Dimensione | Dimensioni Vector | Qualità |
|---------|-----------|-------------------|---------|
| `nomic-embed-text` | 274MB | 768 | ⭐⭐⭐⭐⭐ (consigliato) |
| `all-minilm` | 46MB | 384 | ⭐⭐⭐ |

## 🔧 Ottimizzazioni

### Accelerazione GPU (NVIDIA)

**Linux:**
```bash
# Installa CUDA toolkit
# Ollama rileva automaticamente la GPU
ollama run llama3
```

**Windows:**
```powershell
# Assicurati di avere i driver NVIDIA aggiornati
# Ollama usa automaticamente la GPU
```

### Configurazione Memoria

Limita la memoria usata da Ollama:

**Linux/macOS:**
```bash
# ~/.ollama/config.json
{
  "gpu_memory_fraction": 0.8  # Usa solo 80% della GPU
}
```

**Variabili d'ambiente:**
```bash
export OLLAMA_HOST=0.0.0.0:11434  # Esponi su rete
export OLLAMA_MODELS=/path/to/models  # Cambia directory modelli
```

## ❓ Troubleshooting

### Problema: "Ollama is not running"

**Soluzione:**
```bash
# Verifica che il servizio sia attivo
ollama serve

# Oppure riavvia il computer (Windows/macOS)
```

### Problema: "Out of memory"

**Soluzione:**
1. Usa modelli più piccoli (`phi3:mini`)
2. Chiudi altre applicazioni
3. Aumenta la RAM del sistema

### Problema: Lento senza GPU

**Soluzione:**
1. Usa modelli più piccoli
2. Considera l'uso di Groq API per velocità cloud (vedi `GUIDA_GROQ.md`)
3. Aggiorna a un computer con GPU

### Problema: "Model not found"

**Soluzione:**
```bash
# Scarica il modello prima
ollama pull nome-modello

# Verifica che sia installato
ollama list
```

### Problema: Docker non trova GPU

**Soluzione:**
```bash
# Installa NVIDIA Container Toolkit
distribution=$(. /etc/os-release;echo $ID$VERSION_ID)
curl -s -L https://nvidia.github.io/nvidia-docker/gpgkey | sudo apt-key add -
curl -s -L https://nvidia.github.io/nvidia-docker/$distribution/nvidia-docker.list | \
  sudo tee /etc/apt/sources.list.d/nvidia-docker.list

sudo apt-get update && sudo apt-get install -y nvidia-container-toolkit
sudo systemctl restart docker
```

## 🔗 Accesso da Rete Locale

Per usare Ollama da altri computer della tua rete:

**1. Esponi Ollama sulla rete**
```bash
# Linux/macOS
OLLAMA_HOST=0.0.0.0:11434 ollama serve

# Windows (variabile d'ambiente)
$env:OLLAMA_HOST = "0.0.0.0:11434"
```

**2. Configura il firewall**
- Apri la porta 11434
- Windows: Settings → Network → Firewall
- Linux: `sudo ufw allow 11434`

**3. Usa da un altro computer**
```json
{
  "AIProvider": {
    "Ollama": {
      "Endpoint": "http://192.168.1.100:11434"
    }
  }
}
```

## 📚 Risorse Utili

- [Ollama Documentation](https://github.com/ollama/ollama/tree/main/docs)
- [Model Library](https://ollama.com/library)
- [API Reference](https://github.com/ollama/ollama/blob/main/docs/api.md)
- [FAQ](https://github.com/ollama/ollama#faq)

## 🆘 Supporto

Problemi? Apri un issue su [GitHub](https://github.com/Moncymr/DocN/issues) con:
- Output di `ollama list`
- Output di `ollama --version`
- Sistema operativo e versione
- Log di errore completo

## 💡 Prossimi Passi

- ✅ [Guida Groq](GUIDA_GROQ.md) - API cloud velocissima gratuita
- ✅ [Guida Colab](GUIDA_OLLAMA_COLAB.md) - Ollama su Google Colab
- ✅ [README principale](README.md) - Torna alla documentazione
