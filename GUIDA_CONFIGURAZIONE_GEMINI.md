# 🚀 Guida Completa: Configurazione Gemini per DocN RAG

## 📋 Panoramica

Questa guida ti accompagnerà passo dopo passo nella configurazione di Google Gemini per utilizzare il sistema RAG (Retrieval-Augmented Generation) di DocN. Gemini è il modello di intelligenza artificiale di Google che offre capacità avanzate di embeddings e generazione di testo.

## 🎯 Cosa Otterrai

Dopo aver completato questa guida, il tuo sistema DocN sarà in grado di:
- ✅ Generare embeddings vettoriali da documenti usando Gemini
- ✅ Eseguire ricerca semantica avanzata
- ✅ Estrarre automaticamente tag e metadati dai documenti
- ✅ Suggerire categorie intelligenti
- ✅ Rispondere a domande sui documenti tramite chat RAG

## 🔀 Due Metodi di Configurazione

DocN supporta **due metodi** per configurare Gemini:

### Metodo 1: Configurazione Database (Raccomandato) 🎯
**Configura tramite interfaccia web** → Vai alla PARTE 1 per ottenere l'API Key, poi salta direttamente alla **PARTE 3** per l'avvio e configurazione tramite UI.

✅ **Vantaggi**: 
- Configurazione centralizzata nel database
- Modificabile in tempo reale senza riavviare l'applicazione
- Supporta configurazioni multiple e fallback automatico
- Ideale per ambienti di produzione

### Metodo 2: User Secrets / AppSettings
**Configura tramite file di configurazione** → Segui tutte le parti in ordine (PARTE 1 → PARTE 2 → PARTE 3).

✅ **Vantaggi**:
- Setup più veloce per sviluppo locale
- Non richiede database inizialmente configurato
- Ottimo per testing e sviluppo

**💡 Nota**: Se hai già configurato Gemini tramite l'interfaccia web (`/config`), hai completato il Metodo 1 e puoi saltare la PARTE 2!

---

## 📖 PARTE 1: Prerequisiti in Google AI Studio

### Passo 1.1: Accesso a Google AI Studio

1. **Vai su Google AI Studio**
   - Apri il browser e visita: [https://aistudio.google.com/](https://aistudio.google.com/)
   - Accedi con il tuo account Google

2. **Verifica l'accesso ai modelli**
   - Una volta effettuato l'accesso, dovresti vedere la dashboard di Google AI Studio
   - Verifica che tu abbia accesso ai modelli Gemini

### Passo 1.2: Ottenere l'API Key di Gemini

1. **Naviga alla sezione API Keys**
   - Nella dashboard di Google AI Studio, cerca il menu laterale
   - Clicca su "Get API Key" o "API Keys"

2. **Crea una nuova API Key**
   - Clicca sul pulsante "Create API Key"
   - Se hai un progetto Google Cloud esistente:
     - Seleziona "Create API key in existing project"
     - Scegli il progetto dalla lista
   - Se NON hai un progetto:
     - Seleziona "Create API key in new project"
     - Google creerà automaticamente un nuovo progetto per te

3. **Copia l'API Key**
   - Una volta creata, vedrai una finestra con la tua API Key
   - **IMPORTANTE**: Copia l'API Key e salvala in un posto sicuro
   - La chiave avrà un formato simile a: `AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX`
   - ⚠️ Non condividere mai questa chiave pubblicamente!

### Passo 1.3: Verificare i Modelli Disponibili

I modelli che DocN utilizza per default sono:
- **Embedding**: `text-embedding-004` (genera vettori a 768 dimensioni)
- **Generazione Testo**: `gemini-1.5-pro` (per chat, estrazione tag, categorizzazione)

Questi modelli sono disponibili gratuitamente (con limiti di rate) su Google AI Studio.

### Passo 1.4: Comprendere i Limiti (Opzionale ma Consigliato)

**Piano Gratuito di Google AI Studio:**
- ✅ 15 richieste al minuto (RPM)
- ✅ 1 milione di token al giorno (TPD)
- ✅ 1,500 richieste al giorno (RPD)

**Per uso enterprise:**
- Considera l'upgrade a Google Cloud Vertex AI per limiti più alti
- Maggiori informazioni su: [https://cloud.google.com/vertex-ai](https://cloud.google.com/vertex-ai)

---

## 🔧 PARTE 2: Configurazione nel Progetto DocN

### Passo 2.1: Configurare l'API Key nel Backend (DocN.Server)

#### Opzione A: User Secrets (Raccomandato per Sviluppo)

1. **Apri un terminale nella cartella del progetto**
   ```bash
   cd /percorso/verso/DocN/DocN.Server
   ```

2. **Inizializza User Secrets** (se non già fatto)
   ```bash
   dotnet user-secrets init
   ```

3. **Imposta l'API Key di Gemini**
   ```bash
   dotnet user-secrets set "Gemini:ApiKey" "LA_TUA_API_KEY_QUI"
   ```

4. **Verifica la configurazione**
   ```bash
   dotnet user-secrets list
   ```
   
   Dovresti vedere:
   ```
   Gemini:ApiKey = AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
   ```

#### Opzione B: File appsettings.Development.json (Solo per Test Locali)

⚠️ **ATTENZIONE**: Non committare MAI questo file su Git con chiavi reali!

1. **Apri o crea il file** `DocN.Server/appsettings.Development.json`

2. **Aggiungi la configurazione Gemini:**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "ConnectionStrings": {
       "DocArc": "Server=localhost;Database=DocN;Integrated Security=True;TrustServerCertificate=True;"
     },
     "Gemini": {
       "ApiKey": "LA_TUA_API_KEY_QUI",
       "EmbeddingModel": "text-embedding-004",
       "GenerationModel": "gemini-1.5-pro"
     }
   }
   ```

3. **Verifica che il file sia nel .gitignore**
   ```bash
   # Il file .gitignore dovrebbe contenere:
   appsettings.Development.json
   ```

### Passo 2.2: Configurare l'API Key nel Frontend (DocN.Client)

1. **Apri un terminale nella cartella del client**
   ```bash
   cd /percorso/verso/DocN/DocN.Client
   ```

2. **Inizializza User Secrets**
   ```bash
   dotnet user-secrets init
   ```

3. **Imposta l'API Key**
   ```bash
   dotnet user-secrets set "Gemini:ApiKey" "LA_TUA_API_KEY_QUI"
   ```

### Passo 2.3: Configurare la Connection String del Database

1. **Assicurati che il database sia configurato**
   
   Nel file `DocN.Server/appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DocArc": "Server=localhost;Database=DocN;Integrated Security=True;TrustServerCertificate=True;"
     }
   }
   ```

2. **Se usi SQL Server con autenticazione SQL:**
   ```json
   {
     "ConnectionStrings": {
       "DocArc": "Server=localhost;Database=DocN;User Id=sa;Password=TuaPassword;TrustServerCertificate=True;"
     }
   }
   ```

---

## 🚀 PARTE 3: Avvio e Configurazione dell'Applicazione

### Passo 3.1: Avviare il Backend (DocN.Server)

1. **Apri un terminale e naviga nella cartella del server**
   ```bash
   cd /percorso/verso/DocN/DocN.Server
   ```

2. **Avvia il backend**
   ```bash
   dotnet run
   ```

3. **Verifica l'avvio**
   - Dovresti vedere un messaggio simile a:
   ```
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: https://localhost:5211
   ```
   - Tieni questo terminale aperto!

### Passo 3.2: Avviare il Frontend (DocN.Client)

1. **Apri un NUOVO terminale e naviga nella cartella del client**
   ```bash
   cd /percorso/verso/DocN/DocN.Client
   ```

2. **Avvia il frontend**
   ```bash
   dotnet run
   ```

3. **Verifica l'avvio**
   - Dovresti vedere un messaggio simile a:
   ```
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: https://localhost:7114
   ```

### Passo 3.3: Accedere all'Applicazione

1. **Apri il browser**
   - Vai su: `https://localhost:7114`

2. **Prima registrazione (se necessario)**
   - Se è la prima volta che accedi, clicca su "Register"
   - Inserisci email e password
   - Il primo utente registrato diventa automaticamente amministratore

3. **Effettua il login**
   - Usa le credenziali appena create

### Passo 3.4: Configurare Gemini nell'Interfaccia Web

**🎯 METODO 1 (Raccomandato): Configurazione Database**

Se hai scelto il Metodo 1 (configurazione database), questa è la parte più importante!

1. **Naviga alla pagina di configurazione AI**
   - Nell'applicazione, vai su "Configurazione AI" o "AI Config" dal menu
   - Oppure vai direttamente a: `https://localhost:7114/config`

2. **Inserisci l'API Key di Gemini nella sezione "Gemini Configuration"**
   - Nel campo **Gemini API Key**, inserisci la tua API Key ottenuta da Google AI Studio
   - Esempio: `AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX`

3. **Seleziona Gemini come provider per tutti i servizi**
   - Nella sezione "Assegnazione Provider per Servizio":
     - **💬 Chat Provider**: Seleziona "Gemini"
     - **🧠 Embeddings Provider**: Seleziona "Gemini"
     - **🏷️ Tag Extraction Provider**: Seleziona "Gemini"
     - **🔍 RAG Provider**: Seleziona "Gemini"

4. **Verifica i modelli predefiniti (opzionale)**
   - **Gemini Chat Model**: `gemini-1.5-flash` o `gemini-1.5-pro` (default)
   - **Gemini Embedding Model**: `text-embedding-004` (default)
   - Questi modelli sono già configurati correttamente

5. **Salva la configurazione**
   - Clicca su "Salva" o "Save Configuration"
   - La configurazione viene salvata nel database e sarà attiva immediatamente
   - ✅ **Non è necessario riavviare l'applicazione!**

**💡 Nota importante**: Se hai già completato questi passaggi, la tua configurazione è pronta! Procedi alla PARTE 4 per verificare che tutto funzioni.

---

## ✅ PARTE 4: Verifica e Test della Configurazione

### Test 1: Upload di un Documento

1. **Naviga alla pagina di Upload**
   - Clicca su "Documenti" → "Carica Nuovo"

2. **Carica un documento di test**
   - Seleziona un file PDF, DOCX o TXT
   - Inserisci un titolo (es. "Test Gemini")
   - Clicca su "Carica"

3. **Verifica l'elaborazione**
   - Il sistema dovrebbe:
     - ✅ Estrarre il testo dal documento
     - ✅ Generare embeddings con Gemini
     - ✅ Suggerire tag automaticamente
     - ✅ Proporre categorie

4. **Controlla i log**
   - Nel terminale del backend, dovresti vedere messaggi come:
   ```
   info: DocN.Core.AI.Providers.GeminiProvider[0]
         Generating embedding with Gemini for text of length 1234
   info: DocN.Core.AI.Providers.GeminiProvider[0]
         Successfully generated embedding with 768 dimensions
   ```

### Test 2: Ricerca Semantica

1. **Naviga alla pagina di Ricerca**
   - Clicca su "Ricerca" o "Search"

2. **Esegui una ricerca semantica**
   - Inserisci una query in linguaggio naturale
   - Esempio: "Come configurare il sistema?"
   - Il sistema dovrebbe trovare documenti semanticamente simili

3. **Verifica i risultati**
   - Dovresti vedere documenti rilevanti anche se non contengono le parole esatte della query

### Test 3: Chat RAG con Documenti

1. **Naviga alla pagina di Chat**
   - Clicca su "Chat" o "RAG Chat"

2. **Inizia una conversazione**
   - Fai una domanda sui documenti caricati
   - Esempio: "Cosa dice il documento riguardo alla configurazione?"

3. **Verifica la risposta**
   - Il sistema dovrebbe:
     - ✅ Recuperare i documenti rilevanti
     - ✅ Generare una risposta basata sul contenuto
     - ✅ Mostrare le citazioni dai documenti

4. **Controlla i log**
   - Nel backend, dovresti vedere l'elaborazione RAG:
   ```
   info: DocN.Data.Services.SemanticRAGService[0]
         Processing RAG query with Semantic Kernel
   ```

---

## 🔧 PARTE 5: Configurazione Avanzata (Opzionale)

### Configurare Parametri RAG Personalizzati

1. **Vai alla pagina di configurazione**
   - Nell'app, vai su "Configurazione" → "RAG Settings"

2. **Personalizza i parametri:**
   - **Similarity Threshold**: Soglia di similarità (0.0 - 1.0)
     - Default: 0.7
     - Valori più alti = risultati più rilevanti ma meno risultati
   - **Max Documents**: Numero massimo di documenti da recuperare
     - Default: 5
     - Range consigliato: 3-10
   - **Chunk Size**: Dimensione dei chunk di testo
     - Default: 1000 caratteri
   - **Chunk Overlap**: Sovrapposizione tra chunk
     - Default: 200 caratteri

### Configurare Fallback Multi-Provider

Se vuoi configurare un fallback ad altri provider (es. OpenAI):

1. **Configura OpenAI come fallback**
   ```bash
   cd DocN.Server
   dotnet user-secrets set "OpenAI:ApiKey" "LA_TUA_OPENAI_API_KEY"
   ```

2. **Nell'interfaccia web**
   - Vai su "Configurazione AI"
   - Attiva "Enable Fallback"
   - Seleziona l'ordine dei provider

### Ottimizzare le Performance

1. **Batch Processing**
   - Il sistema processa automaticamente gli embeddings in batch
   - Default: 10 documenti per batch

2. **Caching**
   - Le configurazioni AI sono cachate per 5 minuti
   - Riduci il numero di chiamate API

---

## ❓ PARTE 6: Risoluzione Problemi Comuni

### Problema 1: "Gemini ApiKey is required"

**Causa**: L'API Key non è configurata correttamente

**Soluzione**:
```bash
cd DocN.Server
dotnet user-secrets set "Gemini:ApiKey" "LA_TUA_API_KEY"
# Verifica
dotnet user-secrets list
```

### Problema 2: "Failed to generate embedding with Gemini"

**Possibili cause e soluzioni**:

1. **API Key non valida**
   - Verifica che l'API Key sia corretta
   - Controlla su Google AI Studio che la chiave sia attiva

2. **Rate limit superato**
   - Attendi qualche minuto
   - Verifica i limiti su Google AI Studio
   - Considera l'upgrade a Vertex AI per limiti più alti

3. **Problema di rete**
   - Verifica la connessione internet
   - Controlla se ci sono firewall che bloccano le richieste a Google

### Problema 3: "Embedding dimension mismatch"

**Causa**: Il database ha vettori di dimensione diversa (es. 1536 da OpenAI vs 768 da Gemini)

**Soluzione**:
```bash
# Opzione A: Migrare tutti i vettori a 768 dimensioni (Gemini)
cd Database
sqlcmd -S localhost -d DocN -E -i Update_Vector_1536_to_768.sql

# Opzione B: Usare campi vettore duali (supporta entrambi)
sqlcmd -S localhost -d DocN -E -i UpdateScripts/008_AddDualVectorFields.sql
```

### Problema 4: Il Backend non si avvia

**Soluzione**:
```bash
# Verifica le migrazioni del database
cd DocN.Server
dotnet ef database update

# Verifica la connection string
dotnet user-secrets list

# Verifica che SQL Server sia in esecuzione
```

### Problema 5: "Cannot connect to Backend API" ⚠️

**Causa**: Il backend (DocN.Server sulla porta 5211) non è in esecuzione

**Sintomi**:
- Errore: "Unable to connect to the backend service"
- Errore: "Please ensure the DocN.Server is running on https://localhost:5211"
- Le funzionalità RAG, chat e ricerca semantica non funzionano

**Soluzione**:

1. **Verifica che il backend sia in esecuzione**
   ```bash
   # Controlla se c'è un processo in ascolto sulla porta 5211
   # Windows PowerShell:
   netstat -ano | findstr :5211
   
   # Linux/Mac:
   lsof -i :5211
   ```

2. **Avvia il backend se non è in esecuzione**
   ```bash
   cd DocN.Server
   dotnet run
   ```
   
   Dovresti vedere:
   ```
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: https://localhost:5211
   ```

3. **Assicurati di avviare PRIMA il backend, POI il frontend**
   - Terminal 1: `cd DocN.Server && dotnet run` ✅
   - Terminal 2: `cd DocN.Client && dotnet run` ✅

4. **Verifica la connection string del database**
   - Il backend richiede una connessione al database per avviarsi
   - Controlla `appsettings.Development.json` o i user secrets

5. **Controlla i log del backend per errori**
   - Errori di migrazione database
   - Errori di connessione SQL Server
   - Errori di configurazione

**💡 Suggerimento**: Usa gli script automatici per avviare entrambi i server:
```bash
# Linux/Mac
./start-dev.sh

# Windows PowerShell
.\start-dev.ps1
```

### Problema 6: La configurazione database non viene applicata

**Causa**: L'applicazione sta usando la configurazione da user secrets invece che dal database

**Soluzione**:
1. Verifica che la configurazione sia salvata nel database:
   ```sql
   SELECT * FROM AIConfigurations WHERE IsActive = 1;
   ```

2. Riavvia l'applicazione per invalidare la cache

3. Verifica nei log che stia usando la configurazione database:
   ```
   info: DocN.Data.Services.MultiProviderAIService[0]
         Using database configuration: [nome configurazione]
   ```

---

## 📊 PARTE 7: Monitoraggio e Logging

### Visualizzare i Log in Tempo Reale

**Backend (DocN.Server)**:
```bash
# I log mostrano tutte le operazioni AI
# Cerca messaggi come:
# - "Generating embedding with Gemini"
# - "Processing RAG query"
# - "Successfully generated embedding with 768 dimensions"
```

**Frontend (DocN.Client)**:
```bash
# I log mostrano le interazioni utente
# Cerca messaggi come:
# - "Document uploaded successfully"
# - "Searching documents"
```

### Abilitare Log Dettagliati

Nel file `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DocN.Core.AI": "Debug",
      "DocN.Data.Services": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

---

## 🎓 PARTE 8: Best Practices

### Sicurezza

1. **NON committare MAI le API Key su Git**
   - Usa sempre User Secrets in sviluppo
   - Usa variabili d'ambiente in produzione

2. **Usa .gitignore correttamente**
   ```
   appsettings.Development.json
   **/appsettings.*.json
   !**/appsettings.example.json
   ```

3. **Ruota le API Key periodicamente**
   - Crea nuove chiavi ogni 3-6 mesi
   - Revoca le chiavi vecchie

### Performance

1. **Ottimizza il chunking**
   - Chunk più piccoli = ricerca più precisa ma più chiamate API
   - Chunk più grandi = meno chiamate API ma ricerca meno precisa

2. **Usa il batch processing**
   - Carica più documenti insieme quando possibile

3. **Monitora l'uso dell'API**
   - Tieni traccia delle richieste giornaliere
   - Configura alert per rate limits

### Manutenzione

1. **Backup regolari del database**
   ```bash
   # Backup del database
   sqlcmd -S localhost -Q "BACKUP DATABASE DocN TO DISK='C:\Backups\DocN.bak'"
   ```

2. **Aggiorna i modelli**
   - Controlla periodicamente nuovi modelli Gemini
   - Testa nuovi modelli in ambiente di sviluppo prima

3. **Monitora le metriche**
   - Tempo di risposta delle query
   - Qualità dei risultati di ricerca
   - Accuratezza delle categorizzazioni

---

## 📚 Risorse Utili

### Documentazione Ufficiale

- **Google AI Studio**: [https://aistudio.google.com/](https://aistudio.google.com/)
- **Gemini API Docs**: [https://ai.google.dev/](https://ai.google.dev/)
- **Vertex AI**: [https://cloud.google.com/vertex-ai](https://cloud.google.com/vertex-ai)

### Documentazione DocN

- **README principale**: [README.md](README.md)
- **Roadmap Enterprise**: [ENTERPRISE_RAG_ROADMAP.md](ENTERPRISE_RAG_ROADMAP.md)
- **Setup Database**: [Database/QUICK_START.md](Database/QUICK_START.md)

### Community e Supporto

- **GitHub Issues**: Per bug e feature request
- **Discussions**: Per domande e discussioni

---

## ✅ Checklist Finale

Verifica di aver completato tutti i passaggi:

### Google AI Studio
- [ ] Account Google creato/accesso effettuato
- [ ] Google AI Studio accessibile
- [ ] API Key di Gemini creata
- [ ] API Key copiata e salvata in modo sicuro

### Configurazione Progetto
- [ ] API Key configurata in DocN.Server (user secrets)
- [ ] API Key configurata in DocN.Client (user secrets)
- [ ] Connection string database configurata
- [ ] .gitignore verificato

### Avvio Applicazione
- [ ] Database creato e migrato
- [ ] Backend (DocN.Server) avviato sulla porta 5211
- [ ] Frontend (DocN.Client) avviato sulla porta 7114
- [ ] Login effettuato nell'applicazione

### Configurazione Web
- [ ] Gemini selezionato come provider AI
- [ ] Modelli configurati (text-embedding-004, gemini-1.5-pro)
- [ ] Configurazione salvata

### Test Funzionalità
- [ ] Documento caricato con successo
- [ ] Embeddings generati (verifica nei log)
- [ ] Tag estratti automaticamente
- [ ] Ricerca semantica funzionante
- [ ] Chat RAG funzionante

---

## 🎉 Congratulazioni!

Se hai completato tutti i passaggi della checklist, il tuo sistema DocN con Gemini è configurato e funzionante! 

Ora puoi:
- 📤 Caricare documenti aziendali
- 🔍 Eseguire ricerche semantiche avanzate
- 💬 Chattare con i tuoi documenti
- 🏷️ Ottenere categorizzazione automatica
- 🚀 Costruire la tua knowledge base enterprise

### Prossimi Passi Consigliati

1. **Carica più documenti** per costruire la knowledge base
2. **Sperimenta con la ricerca semantica** per capire le capacità
3. **Crea categorie personalizzate** per organizzare i documenti
4. **Invita altri utenti** per testare il multi-tenancy
5. **Monitora le performance** e ottimizza i parametri RAG

---

**Versione Guida**: 1.0  
**Ultimo Aggiornamento**: Dicembre 2024  
**Compatibilità**: DocN v2.0+, Gemini API v1

**Autore**: DocN Team  
**Licenza**: MIT

---

## 📧 Hai Bisogno di Aiuto?

Se incontri problemi non coperti in questa guida:

1. **Controlla i log** del backend e frontend
2. **Cerca negli Issues** di GitHub per problemi simili
3. **Apri un nuovo Issue** con:
   - Descrizione del problema
   - Log degli errori
   - Passi per riprodurre il problema
4. **Consulta la documentazione** di Google AI Studio

**Buon lavoro con DocN e Gemini! 🚀**
