# 💬 Guida Completa: Come Avere una ChatAI Funzionante

## 📋 Indice

1. [Panoramica](#panoramica)
2. [Prerequisiti](#prerequisiti)
3. [Passo 1: Installazione Software](#passo-1-installazione-software)
4. [Passo 2: Configurazione Database](#passo-2-configurazione-database)
5. [Passo 3: Ottenere le API Keys](#passo-3-ottenere-le-api-keys)
6. [Passo 4: Configurazione AI Provider](#passo-4-configurazione-ai-provider)
7. [Passo 5: Avvio Applicazione](#passo-5-avvio-applicazione)
8. [Passo 6: Configurazione ChatAI](#passo-6-configurazione-chatai)
9. [Passo 7: Caricare Documenti](#passo-7-caricare-documenti)
10. [Passo 8: Utilizzare la ChatAI](#passo-8-utilizzare-la-chatai)
11. [Risoluzione Problemi](#risoluzione-problemi)
12. [Domande Frequenti](#domande-frequenti)

---

## Panoramica

DocN include un sistema di **ChatAI avanzato** che utilizza tecnologia RAG (Retrieval-Augmented Generation) per conversare con i tuoi documenti. La ChatAI può:

- ✅ Rispondere a domande sui tuoi documenti
- ✅ Riassumere contenuti
- ✅ Estrarre informazioni specifiche
- ✅ Mantenere il contesto della conversazione
- ✅ Citare le fonti nei documenti

### 🏗️ Architettura Dual-Server

**IMPORTANTE:** DocN utilizza **due server** che devono essere **entrambi in esecuzione**:

1. **DocN.Server** (Backend API) - porta `5211`
   - Gestisce la ChatAI e il sistema RAG
   - Esegue ricerche vettoriali
   - Comunica con i provider AI

2. **DocN.Client** (Frontend) - porta `7114`
   - Interfaccia utente web
   - Gestisce autenticazione
   - Si connette al Backend per la ChatAI

⚠️ **Se vedi l'errore "Unable to connect to backend service (localhost:5211)"**, significa che il DocN.Server non è in esecuzione.

---

## Prerequisiti

Prima di iniziare, assicurati di avere:

- [ ] Computer con Windows, Linux o macOS
- [ ] Connessione Internet
- [ ] Email per registrazione (se usi provider AI)
- [ ] Carta di credito (per API a pagamento come OpenAI) - OPZIONALE

### 📦 Software Richiesto

- **.NET 10.0 SDK** (obbligatorio)
- **SQL Server 2025** o **Azure SQL Database** (obbligatorio)
- **Visual Studio 2025** o **VS Code** (opzionale, ma consigliato)
- **Git** (per clonare il repository)

### 💰 Costi API

- **Gemini**: Offre un tier gratuito generoso (consigliato per iniziare)
- **OpenAI**: Richiede pagamento a consumo (~$0.002 per 1K token)
- **Azure OpenAI**: Solo per clienti enterprise Azure

---

## Passo 1: Installazione Software

### 1.1 Installare .NET 10.0 SDK

**Windows:**
1. Vai su https://dotnet.microsoft.com/download/dotnet/10.0
2. Scarica ".NET 10.0 SDK" (non Runtime)
3. Esegui l'installer e segui le istruzioni
4. Riavvia il computer

**Linux (Ubuntu/Debian):**
```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 10.0
```

**macOS:**
```bash
# Usando Homebrew
brew install dotnet@10
```

**Verifica installazione:**
```bash
dotnet --version
# Dovresti vedere: 10.0.x
```

### 1.2 Installare SQL Server

**Windows:**
1. Scarica SQL Server 2025 Express (gratuito) da:
   https://www.microsoft.com/sql-server/sql-server-downloads
2. Scegli "Express" > "Basic"
3. Segui l'installer, accetta i default
4. Annota la stringa di connessione mostrata alla fine

**Linux (Ubuntu):**
```bash
# Aggiungi repository Microsoft
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
sudo add-apt-repository "$(wget -qO- https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/mssql-server-2025.list)"

# Installa SQL Server
sudo apt-get update
sudo apt-get install -y mssql-server

# Configura SQL Server
sudo /opt/mssql/bin/mssql-conf setup
```

**Docker (Tutti i sistemi):**
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
   -p 1433:1433 --name sql2025 --hostname sql2025 \
   -d mcr.microsoft.com/mssql/server:2025-latest
```

**Verifica installazione:**
```bash
sqlcmd -S localhost -U sa -P YourPassword -Q "SELECT @@VERSION"
```

### 1.3 Installare Git

**Windows:**
1. Scarica da https://git-scm.com/download/win
2. Esegui l'installer con le opzioni default

**Linux:**
```bash
sudo apt-get install git
```

**macOS:**
```bash
brew install git
```

---

## Passo 2: Configurazione Database

### 2.1 Clonare il Repository

```bash
# Crea una cartella per il progetto
mkdir -p ~/progetti
cd ~/progetti

# Clona il repository
git clone https://github.com/Moncymr/DocN.git
cd DocN
```

### 2.2 Creare il Database

**Opzione A - Automatica (Consigliata):**
Il database viene creato automaticamente al primo avvio dell'applicazione.
Passa al [Passo 3](#passo-3-ottenere-le-api-keys).

**Opzione B - Manuale:**
```bash
cd Database

# Con autenticazione Windows
sqlcmd -S localhost -E -i SqlServer2025_Schema.sql

# Con autenticazione SQL Server
sqlcmd -S localhost -U sa -P YourPassword -i SqlServer2025_Schema.sql
```

### 2.3 Configurare Connection String

Crea il file `DocN.Server/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DocN;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

**Per SQL Server con password:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DocN;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
  }
}
```

**Per Docker:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=DocN;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
  }
}
```

---

## Passo 3: Ottenere le API Keys

Per usare la ChatAI, devi avere **almeno una API key** da uno di questi provider:

### 3.1 Gemini (Google AI) - CONSIGLIATO PER INIZIARE

**Perché Gemini:**
- ✅ Tier gratuito generoso
- ✅ Ottima qualità
- ✅ Facile da configurare
- ✅ Nessuna carta di credito richiesta

**Come ottenere la key:**
1. Vai su https://aistudio.google.com/apikey
2. Accedi con il tuo account Google
3. Clicca "Get API Key" o "Create API Key"
4. Copia la chiave (inizia con `AIza...`)
5. **SALVALA** in un posto sicuro

**Limiti tier gratuito Gemini:**
- 15 richieste al minuto
- 1500 richieste al giorno
- Sufficiente per testing e uso personale

### 3.2 OpenAI (Opzionale)

**Perché OpenAI:**
- ✅ Modelli GPT-4 molto potenti
- ✅ Embeddings di alta qualità
- ❌ Richiede carta di credito
- ❌ Pagamento a consumo

**Come ottenere la key:**
1. Vai su https://platform.openai.com/signup
2. Crea un account
3. Aggiungi metodo di pagamento in "Billing"
4. Vai su https://platform.openai.com/api-keys
5. Clicca "Create new secret key"
6. Copia la chiave (inizia con `sk-...`)
7. **SALVALA** (non potrai più vederla!)

**Costi indicativi:**
- GPT-4: ~$0.03 per 1K token (input) / $0.06 (output)
- GPT-3.5-turbo: ~$0.001 per 1K token
- Embeddings: ~$0.0001 per 1K token

### 3.3 Azure OpenAI (Solo Enterprise)

Per aziende che già usano Azure. Richiede:
- Sottoscrizione Azure attiva
- Richiesta di accesso ad Azure OpenAI
- Deploy dei modelli su Azure

**Non consigliato per uso personale.**

---

## Passo 4: Configurazione AI Provider

Ora devi configurare le API keys nell'applicazione.

### 4.1 Configurazione Sicura con User Secrets (CONSIGLIATA)

**Per DocN.Server:**
```bash
cd DocN.Server

# Inizializza user secrets
dotnet user-secrets init

# Configura Gemini (se usi Gemini)
dotnet user-secrets set "Gemini:ApiKey" "TUA_CHIAVE_GEMINI_QUI"

# Configura OpenAI (se usi OpenAI)
dotnet user-secrets set "OpenAI:ApiKey" "TUA_CHIAVE_OPENAI_QUI"
```

**Per DocN.Client:**
```bash
cd ../DocN.Client

dotnet user-secrets init

# Configura Gemini
dotnet user-secrets set "Gemini:ApiKey" "TUA_CHIAVE_GEMINI_QUI"

# Configura OpenAI  
dotnet user-secrets set "OpenAI:ApiKey" "TUA_CHIAVE_OPENAI_QUI"
```

### 4.2 Configurazione con File (Alternativa - Non per Produzione)

**Crea `DocN.Server/appsettings.Development.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DocN;Integrated Security=True;TrustServerCertificate=True;"
  },
  "Gemini": {
    "ApiKey": "TUA_CHIAVE_GEMINI_QUI"
  },
  "OpenAI": {
    "ApiKey": "TUA_CHIAVE_OPENAI_QUI"
  }
}
```

**Crea lo stesso file in `DocN.Client/appsettings.Development.json`**

⚠️ **IMPORTANTE:** Non committare mai le API keys su Git!

---

## Passo 5: Avvio Applicazione

DocN richiede **DUE SERVER** in esecuzione contemporaneamente.

### 5.1 Opzione A: Script Automatico (CONSIGLIATA)

**Linux/Mac:**
```bash
cd ~/progetti/DocN
chmod +x start-dev.sh
./start-dev.sh
```

**Windows PowerShell:**
```powershell
cd C:\progetti\DocN
.\start-dev.ps1
```

Lo script:
1. ✅ Compila entrambi i progetti
2. ✅ Avvia il Backend (porta 5211)
3. ✅ Avvia il Frontend (porta 7114)
4. ✅ Gestisce entrambi automaticamente

### 5.2 Opzione B: Avvio Manuale (Due Terminali)

**Terminal 1 - Backend API:**
```bash
cd ~/progetti/DocN/DocN.Server
dotnet run
```

Aspetta fino a vedere:
```
Now listening on: https://localhost:5211
Now listening on: http://localhost:5210
```

**Terminal 2 - Frontend (in una NUOVA finestra):**
```bash
cd ~/progetti/DocN/DocN.Client
dotnet run
```

Aspetta fino a vedere:
```
Now listening on: https://localhost:7114
```

### 5.3 Verifica che tutto funzioni

**Test 1: Backend è in esecuzione**
```bash
curl https://localhost:5211/api/health
```
Risposta attesa: `{"status":"healthy","service":"DocN.Server"}`

**Test 2: Frontend è accessibile**
Apri il browser su: https://localhost:7114

Dovresti vedere la pagina di login di DocN.

---

## Passo 6: Configurazione ChatAI

### 6.1 Primo Accesso

1. Apri il browser su https://localhost:7114
2. Clicca su **"Registrati"**
3. Compila il form:
   - Email: `admin@docn.local`
   - Password: `Admin123!` (o una password forte)
   - Conferma password
4. Clicca "Registrati"

🎉 Il primo utente registrato diventa automaticamente **Amministratore**.

### 6.2 Configurare i Provider AI

1. Dopo il login, clicca su **"⚙️ Config"** nel menu (in alto a destra)
2. Compila il form di configurazione:

**Informazioni Base:**
- **Nome Configurazione**: `Produzione 2024` (o qualsiasi nome)

**Assegnazione Provider per Servizio:**
- **Chat**: Seleziona `Gemini` (o il provider che hai configurato)
- **Embeddings**: Seleziona `Gemini`
- **Tag Extraction**: Seleziona `Gemini`
- **RAG**: Seleziona `Gemini`

**Configurazione Gemini:**
- **API Key**: La tua chiave Gemini (già configurata nei secrets)
- **Chat Model**: `gemini-2.0-flash-exp` (default, va bene)
- **Embedding Model**: `text-embedding-004` (default)

**Configurazione RAG:**
- **Max Documenti da Recuperare**: `5` (default)
- **Soglia Similarità**: `0.7` (default, va bene)
- **Max Token per Contesto**: `4000` (default)

**Configurazione Chunking:**
- ✅ **Abilita Chunking**: Selezionato
- **Dimensione Chunk**: `1000` caratteri
- **Overlap Chunk**: `200` caratteri

**Impostazioni Avanzate:**
- ✅ **Abilita Fallback**: Selezionato (consigliato)
- ✅ **Imposta come Attiva**: Selezionato

3. Clicca **"💾 Salva Configurazione"**
4. Dovresti vedere: ✅ "Configurazione salvata con successo!"

### 6.3 Verifica Configurazione

Nell'elenco configurazioni dovresti vedere:
- 🟢 Badge verde "Attiva" sulla tua configurazione
- Chat Provider: Gemini
- Embedding Provider: Gemini

---

## Passo 7: Caricare Documenti

Prima di usare la ChatAI, devi caricare alcuni documenti.

### 7.1 Vai alla Pagina Caricamento

1. Clicca su **"📄 Documenti"** nel menu
2. Clicca su **"⬆️ Carica Documento"** (in alto a destra)

### 7.2 Carica un Documento di Test

**Opzione A - Crea un file di test:**
Crea un file `test.txt` con questo contenuto:
```
Benvenuto in DocN!

DocN è un sistema di gestione documentale con intelligenza artificiale.
Il sistema utilizza RAG (Retrieval-Augmented Generation) per permettere
di conversare con i documenti caricati.

Caratteristiche principali:
- Ricerca semantica con vettori
- Chat AI per domande sui documenti
- Supporto per PDF, DOCX, TXT
- Estrazione automatica di metadati
- Sistema multi-tenant

Per maggiori informazioni visita: https://github.com/Moncymr/DocN
```

**Opzione B - Usa un documento esistente:**
Puoi caricare qualsiasi file PDF, DOCX o TXT che hai.

### 7.3 Compila il Form di Caricamento

1. **Seleziona File**: Scegli il file appena creato o un tuo documento
2. **Categoria**: `Documentazione` (o lascia vuoto)
3. **Tags**: `test, demo, tutorial` (opzionale)
4. **Visibilità**: Seleziona `Organization` o `Private`
5. **Descrizione**: "Documento di test per ChatAI" (opzionale)
6. Clicca **"📤 Carica"**

### 7.4 Attendi l'Elaborazione

Il sistema farà automaticamente:
1. ✅ Upload del file
2. ✅ Estrazione del testo
3. ✅ Generazione embeddings vettoriali
4. ✅ Chunking del documento
5. ✅ Estrazione tag automatici (con AI)
6. ✅ Creazione indice ricerca

Dovresti vedere: ✅ "Documento caricato con successo!"

**Tempo elaborazione:** 5-15 secondi per documento.

### 7.5 Verifica Documento Caricato

Torna su **"📄 Documenti"** e dovresti vedere:
- Il documento appena caricato
- Categoria e tag estratti automaticamente
- Data caricamento

---

## Passo 8: Utilizzare la ChatAI

### 8.1 Aprire la Chat

1. Clicca su **"💬 Chat"** nel menu principale
2. Dovresti vedere la schermata di benvenuto con:
   - Icona del robot 🤖
   - "Welcome to Document Chat"
   - Suggerimenti di domande

### 8.2 Prima Domanda

Prova una di queste domande:

**Domanda Semplice:**
```
Quali documenti ho caricato?
```

**Domanda Specifica:**
```
Quali sono le caratteristiche principali di DocN?
```

**Domanda con Contesto:**
```
Come funziona la ricerca semantica in DocN?
```

### 8.3 Comprendere la Risposta

La ChatAI risponderà con:

1. **Risposta Generata**: Testo naturale che risponde alla tua domanda
2. **📚 Sources**: Documenti utilizzati per generare la risposta
3. **Document #X**: ID dei documenti di riferimento

**Esempio di risposta:**
```
📚 Risposta:
DocN ha diverse caratteristiche principali:
- Ricerca semantica con vettori per trovare documenti rilevanti
- Chat AI che permette di conversare con i documenti
- Supporto per vari formati: PDF, DOCX, TXT
- Estrazione automatica di metadati usando AI
- Sistema multi-tenant per organizzazioni

📚 Sources: Document #1, Document #2
```

### 8.4 Conversazione Continua

Puoi fare domande successive che mantengono il contesto:

```
Tu: Quali sono le caratteristiche principali?
AI: [risponde]

Tu: Parlami più in dettaglio della ricerca semantica
AI: [risponde usando il contesto della conversazione precedente]

Tu: Come si usa?
AI: [risponde sapendo che stai parlando della ricerca semantica]
```

### 8.5 Gestire Conversazioni

**Nuova Conversazione:**
Clicca su **"➕ New Chat"** per iniziare una conversazione vuota.

**Conversazioni Salvate:**
Nella sidebar a sinistra (clicca ☰ se non visibile) puoi:
- Vedere tutte le conversazioni passate
- Cliccare su una conversazione per riaprirla
- Vedere numero di messaggi e data

### 8.6 Testare con Più Documenti

Per risultati migliori:
1. Carica 3-5 documenti su argomenti simili
2. Fai domande che richiedono informazioni da più documenti
3. La ChatAI combinerà le informazioni

**Esempio:**
```
Carica: manuale_utente.pdf, faq.pdf, guida_tecnica.pdf

Domanda: "Come posso configurare l'autenticazione?"

La ChatAI cercherà in tutti e tre i documenti e creerà una risposta completa.
```

---

## Risoluzione Problemi

### ❌ Errore: "Unable to connect to backend service (localhost:5211)"

**Causa:** Il DocN.Server non è in esecuzione.

**Soluzione:**
1. Apri un terminale
2. Vai in `DocN.Server`:
   ```bash
   cd ~/progetti/DocN/DocN.Server
   dotnet run
   ```
3. Aspetta che vedi: `Now listening on: https://localhost:5211`
4. Ricarica la pagina della Chat

### ❌ Errore: "Failed to load conversations"

**Causa:** Problema di connessione o configurazione backend.

**Verifica:**
```bash
# Test 1: Backend è in esecuzione?
curl https://localhost:5211/api/health

# Test 2: Database è accessibile?
sqlcmd -S localhost -U sa -Q "SELECT TOP 1 * FROM DocN.dbo.Documents"
```

**Soluzione:**
1. Riavvia il DocN.Server
2. Verifica la connection string nel file `appsettings.Development.json`
3. Controlla i log del server nel terminale

### ❌ Errore: "API key not configured"

**Causa:** API key non impostata correttamente.

**Soluzione:**
```bash
cd DocN.Server
dotnet user-secrets set "Gemini:ApiKey" "TUA_CHIAVE_QUI"
dotnet user-secrets set "OpenAI:ApiKey" "TUA_CHIAVE_QUI"

cd ../DocN.Client
dotnet user-secrets set "Gemini:ApiKey" "TUA_CHIAVE_QUI"
```

Riavvia entrambi i server.

### ❌ Errore: "No documents found" ma ho caricato documenti

**Causa:** Embeddings non generati o problema visibilità.

**Verifica:**
```sql
-- Controlla documenti nel database
SELECT Id, FileName, Category, Visibility 
FROM DocN.dbo.Documents 
WHERE OwnerId = 'TUO_USER_ID';

-- Controlla chunks generati
SELECT COUNT(*) FROM DocN.dbo.DocumentChunks;
```

**Soluzione:**
1. Ricarica il documento
2. Verifica che la configurazione AI sia attiva (pagina /config)
3. Controlla i log per errori nell'elaborazione

### ❌ Errore di Build: "Project file not found"

**Soluzione:**
```bash
# Verifica di essere nella cartella giusta
pwd  # Dovresti vedere: /percorso/DocN

# Lista contenuto
ls -la
# Dovresti vedere: DocN.Server/ DocN.Client/ DocN.sln

# Prova a ricostruire
dotnet restore
dotnet build
```

### ❌ Errore Database: "Cannot open database 'DocN'"

**Soluzione:**
```bash
# Crea il database manualmente
sqlcmd -S localhost -U sa -P YourPassword -Q "CREATE DATABASE DocN"

# Esegui lo schema
cd Database
sqlcmd -S localhost -U sa -P YourPassword -i SqlServer2025_Schema.sql
```

### 🐛 Debug Avanzato

**Abilitare log dettagliati:**

Modifica `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning",
      "DocN": "Debug"
    }
  }
}
```

**Controllare log in tempo reale:**
I log appaiono nel terminale dove hai eseguito `dotnet run`.

**Verificare configurazione caricata:**
```bash
curl https://localhost:5211/api/SemanticChat/conversations?userId=demo-user
```

---

## Domande Frequenti

### Q: Quanto costa usare DocN?

**A:** DocN è gratuito e open source. I costi sono solo per le API AI:
- **Gemini**: Tier gratuito (15 req/min, 1500 req/giorno)
- **OpenAI**: A pagamento (~$0.002-0.03 per 1K token)

### Q: Posso usare DocN senza AI?

**A:** Sì, puoi usare DocN per gestione documenti base, ma la ChatAI richiede almeno un provider AI configurato.

### Q: Quale provider AI è meglio?

**A:** Dipende dall'uso:
- **Gemini**: Migliore per iniziare (gratuito, buona qualità)
- **OpenAI GPT-4**: Migliore qualità, più costoso
- **Azure OpenAI**: Per aziende già su Azure

### Q: Quanti documenti posso caricare?

**A:** Illimitati. Il limite è solo lo spazio sul database SQL Server.

### Q: I miei documenti sono privati?

**A:** Sì! I documenti:
- Sono salvati nel TUO database locale
- Non vengono inviati a terzi (solo il testo per l'AI)
- Supportano visibilità Private/Organization/Public

### Q: La ChatAI funziona offline?

**A:** No, richiede connessione Internet per comunicare con i provider AI (Gemini/OpenAI).

### Q: Posso usare modelli AI locali?

**A:** Attualmente no, ma è in roadmap il supporto per:
- Ollama (modelli locali)
- LM Studio
- Modelli Hugging Face

### Q: Come aggiorno DocN?

```bash
cd ~/progetti/DocN
git pull origin main
dotnet restore
dotnet build
```

### Q: Supporta lingue oltre l'italiano?

**A:** Sì! La ChatAI supporta:
- Italiano
- Inglese
- Spagnolo
- Francese
- Tedesco
- E molte altre (dipende dal provider AI)

### Q: Posso usare DocN in produzione?

**A:** Sì, ma segui le best practices:
1. Usa HTTPS con certificati validi
2. Configura autenticazione forte
3. Usa Azure SQL o SQL Server enterprise
4. Implementa backup automatici
5. Leggi [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)

### Q: Come ottengo supporto?

1. Consulta questa guida e [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
2. Cerca negli [Issues su GitHub](https://github.com/Moncymr/DocN/issues)
3. Apri un nuovo Issue descrivendo il problema

---

## 🎉 Congratulazioni!

Hai completato la configurazione di DocN ChatAI! 

### Prossimi Passi

1. ✅ **Carica più documenti** per sfruttare al meglio il RAG
2. ✅ **Sperimenta** con domande diverse
3. ✅ **Leggi** [INDICE_DOCUMENTAZIONE.md](INDICE_DOCUMENTAZIONE.md) per funzionalità avanzate
4. ✅ **Configura** OCR per documenti scansionati: [TESSERACT_SETUP.md](TESSERACT_SETUP.md)
5. ✅ **Personalizza** i system prompts per il tuo caso d'uso

### Funzionalità Avanzate

- 🔍 **Ricerca Ibrida**: Combina ricerca vettoriale e full-text
- 📊 **Dashboard Analytics**: Monitora utilizzo e performance
- 🔐 **Multi-Tenant**: Separa documenti per organizzazione
- 🏷️ **Tag Automatici**: AI estrae tag dai documenti
- 📸 **OCR**: Estrai testo da immagini e PDF scansionati

---

## 📚 Risorse Utili

- **README principale**: [README.md](README.md)
- **Indice documentazione**: [INDICE_DOCUMENTAZIONE.md](INDICE_DOCUMENTAZIONE.md)
- **Troubleshooting**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- **Configurazione AI**: [MULTI_PROVIDER_CONFIG.md](MULTI_PROVIDER_CONFIG.md)
- **API REST**: [API_DOCUMENTATION.md](API_DOCUMENTATION.md)
- **Deployment**: [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)
- **Repository GitHub**: https://github.com/Moncymr/DocN

---

## 💡 Suggerimenti Finali

### Per Ottenere Risposte Migliori

1. **Documenti di Qualità**: Carica documenti ben formattati con testo chiaro
2. **Domande Specifiche**: "Quali sono i requisiti?" è meglio di "Dimmi qualcosa"
3. **Contesto Conversazionale**: Fai domande successive per approfondire
4. **Chunk Size**: Documenti tecnici → chunk più piccoli (500-800 caratteri)
5. **Similarità Threshold**: Aumenta a 0.8 per risultati più precisi

### Best Practices

- 📁 **Organizza**: Usa categorie e tag coerenti
- 🔄 **Aggiorna**: Ricarica documenti modificati
- 💾 **Backup**: Salva regolarmente il database
- 📊 **Monitora**: Controlla log per errori
- 🔐 **Sicurezza**: Non condividere API keys

---

**Versione Guida**: 1.0  
**Data**: Dicembre 2024  
**Autore**: Team DocN  
**Licenza**: MIT

Buon utilizzo di DocN ChatAI! 🚀💬
