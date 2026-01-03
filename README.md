# DocN - Sistema RAG Documentale Aziendale

## 📋 Panoramica

DocN è un sistema avanzato di gestione documentale enterprise con Retrieval-Augmented Generation (RAG) basato su intelligenza artificiale. Il sistema consente di archiviare, indicizzare, ricercare e interagire con documenti aziendali utilizzando tecnologie di AI all'avanguardia.

## ✨ Caratteristiche Principali

### 🤖 AI Multi-Provider
- **Supporto Multi-Provider**: Gemini, OpenAI, Azure OpenAI
- **Configurazione Flessibile**: Assegnazione provider specifica per servizio (Chat, Embeddings, Tag Extraction, RAG)
- **Fallback Automatico**: Ridondanza e alta disponibilità

### 🔍 Ricerca Avanzata
- **Ricerca Ibrida**: Combina ricerca vettoriale e full-text search con Reciprocal Rank Fusion (RRF)
- **Ricerca Semantica**: Embeddings vettoriali con similarità coseno
- **Full-Text Search**: Indici full-text SQL Server ottimizzati
- **Ricerca Multi-Lingua**: Supporto italiano, inglese e altre lingue

### 📄 Elaborazione Documenti
- **OCR Integrato**: Tesseract OCR per estrazione testo da immagini
- **Chunking Intelligente**: Suddivisione documenti in chunks ottimizzati per RAG
- **Estrazione Metadati**: AI-powered per categorie, tag, entità
- **Multi-Formato**: PDF, DOCX, XLSX, TXT, immagini (PNG, JPG, TIFF, etc.)

### 💬 RAG Conversazionale
- **Chat Intelligente**: Conversazioni naturali con i documenti
- **Semantic Kernel**: Orchestrazione AI avanzata con Microsoft Semantic Kernel
- **Agent Framework**: Sistema multi-agente per retrieval e sintesi
- **Contesto Conversazionale**: Mantenimento cronologia e contesto

### 🔐 Sicurezza e Multi-Tenancy
- **Autenticazione**: ASP.NET Core Identity con ruoli
- **Multi-Tenant**: Isolamento dati per organizzazione
- **Controllo Accessi**: Visibilità documenti (Private, Shared, Organization, Public)
- **Condivisione Documenti**: Gestione permessi granulare

### 📊 Database Avanzato
- **SQL Server 2025**: Supporto tipo VECTOR nativo
- **Embedding Vettoriali**: Vettori 768 dimensioni (Gemini) o 1536 (OpenAI)
- **Stored Procedures**: Ottimizzate per ricerca ibrida e semantica
- **Full-Text Indexing**: Ricerca testuale performante

## 🏗️ Architettura

DocN utilizza un'architettura multi-server per separare le responsabilità e ottimizzare le prestazioni:

```
DocN/
├── DocN.Server/          # API Backend (ASP.NET Core) - porta 5211
├── DocN.Client/          # Frontend (Blazor Server) - porta 7114
├── DocN.Data/            # Data Layer, Services, Migrations
├── DocN.Core/            # Domain Models, Interfaces
├── tests/                # Unit e Integration Tests
└── Database/             # Script SQL, Migrations
```

### Architettura di Runtime

**DocN.Client** (Blazor Server - porta 7114):
- Gestisce l'interfaccia utente e l'autenticazione
- Esegue operazioni di base sui documenti
- Comunica con DocN.Server per funzionalità RAG avanzate

**DocN.Server** (Backend API - porta 5211):
- Fornisce servizi RAG (Retrieval-Augmented Generation)
- Gestisce chat semantica con Semantic Kernel
- Elabora query vettoriali avanzate

**⚠️ Entrambi i server devono essere in esecuzione per utilizzare tutte le funzionalità dell'applicazione.**

### Stack Tecnologico

- **Framework**: .NET 10.0
- **Frontend**: Blazor Server
- **Backend**: ASP.NET Core Web API
- **Database**: SQL Server 2025 (con supporto VECTOR)
- **ORM**: Entity Framework Core 10.0
- **AI/ML**: 
  - Microsoft Semantic Kernel
  - Google Gemini API
  - OpenAI API
  - Azure OpenAI
  - Tesseract OCR

## 🚀 Quick Start

### Prerequisiti

- .NET 10.0 SDK o superiore
- SQL Server 2025 o Azure SQL Database
- Visual Studio 2025 o VS Code
- API keys per almeno un provider AI (Gemini, OpenAI, o Azure OpenAI)

### Installazione

1. **Clone del repository**
   ```bash
   git clone https://github.com/Moncymr/DocN.git
   cd DocN
   ```

2. **Configurazione Database**
   ```bash
   # Crea il database e lo schema iniziale
   cd Database
   sqlcmd -S localhost -U sa -P YourPassword -i SqlServer2025_Schema.sql
   
   # NOTA: Le migrazioni vengono applicate automaticamente all'avvio dell'applicazione
   # Non è necessario eseguire manualmente dotnet ef database update
   ```

3. **Configurazione AI Providers**
   
   **📘 Per una guida completa passo-passo in italiano, consulta: [GUIDA_CONFIGURAZIONE_GEMINI.md](GUIDA_CONFIGURAZIONE_GEMINI.md)**
   
   ```bash
   cd DocN.Server
   dotnet user-secrets init
   dotnet user-secrets set "Gemini:ApiKey" "your-gemini-key"
   dotnet user-secrets set "OpenAI:ApiKey" "your-openai-key"
   ```

4. **Avvio Applicazione**
   
   **⚠️ IMPORTANTE: DocN richiede due server in esecuzione contemporaneamente:**
   
   **Opzione 1: Script Automatico (Consigliato)**
   ```bash
   # Linux/Mac
   ./start-dev.sh
   
   # Windows PowerShell
   .\start-dev.ps1
   ```
   
   **Opzione 2: Manuale (Due terminali separati)**
   
   Terminal 1 - Backend API:
   ```bash
   cd DocN.Server
   dotnet run
   # Il server sarà disponibile su https://localhost:5211
   ```
   
   Terminal 2 - Frontend Client:
   ```bash
   cd DocN.Client
   dotnet run
   # L'applicazione sarà disponibile su https://localhost:7114
   ```
   
   **Nota:** Il Backend API (DocN.Server) deve essere avviato PRIMA del Frontend (DocN.Client). 
   Se vedi errori di connessione come "Impossibile stabilire la connessione (localhost:5211)", 
   verifica che il Backend API sia in esecuzione.

5. **Accedi all'applicazione**
   - Naviga su: https://localhost:7114 (URL predefinito in sviluppo)
   - Prima registrazione: l'utente diventa admin
   - Nota: La porta può variare in base alla configurazione in `launchSettings.json`

### Configurazione OCR (Opzionale)

Per abilitare l'estrazione testo da immagini:

**Linux/Ubuntu:**
```bash
sudo apt-get install tesseract-ocr libleptonica-dev
```

**Windows:**
Scarica e installa da: https://github.com/UB-Mannheim/tesseract/wiki

Vedi [TESSERACT_SETUP.md](TESSERACT_SETUP.md) per dettagli.

## 📊 Monitoring & Alerting

DocN include un sistema completo di monitoring e alerting:

### Prometheus & OpenTelemetry
- **Metrics Endpoint**: `/metrics` (formato Prometheus)
- **Health Checks**: `/health`, `/health/ready`, `/health/live`
- **Distributed Tracing**: Integrazione OpenTelemetry con Jaeger/Zipkin
- **Custom Metrics**: `/api/metrics/alerts`

### Alerting System
- **Prometheus AlertManager**: Integrazione nativa
- **Alert Routing**: Email, Slack, Webhook, PagerDuty, Opsgenie
- **Alert Rules**: CPU, memoria, latenza, error rate configurabili
- **Escalation Policies**: Basate su severity (Critical, Warning, Info)
- **Alert Dashboard**: `/api/alerts/active` e `/api/alerts/statistics`

### RAG Quality Monitoring
- **Automatic Verification**: Cross-reference con documenti source
- **Confidence Scoring**: Score per statement con threshold configurabili
- **Hallucination Detection**: Rilevamento automatico di contenuti non supportati
- **Citation Verification**: Verifica citazioni nelle risposte
- **RAGAS Metrics**: Faithfulness, Relevancy, Context Precision/Recall
- **Quality Dashboard**: `/api/rag-quality/dashboard`
- **A/B Testing**: Framework per testing configurazioni RAG

**📚 Guide Complete**:
- [Alerting Runbook](docs/ALERTING_RUNBOOK.md) - Procedures per gestione alert
- [RAG Quality Guide](docs/RAG_QUALITY_GUIDE.md) - Guida qualità RAG e RAGAS
- [Monitoring Integration](docs/MONITORING_INTEGRATION_GUIDE.md) - Setup Prometheus, Grafana, Slack

## 📚 Documentazione

### 🎯 Analisi Sistema RAG (NUOVO)
- [**RIEPILOGO_ANALISI_RAG.md**](RIEPILOGO_ANALISI_RAG.md) - 📊 Executive Summary per Decision Makers
- [**ANALISI_SISTEMA_RAG_AZIENDALE_IDEALE.md**](ANALISI_SISTEMA_RAG_AZIENDALE_IDEALE.md) - 🏆 Sistema RAG Ideale 2026
- [**ANALISI_IMPLEMENTAZIONE_DOCN.md**](ANALISI_IMPLEMENTAZIONE_DOCN.md) - 🔍 Analisi Dettagliata DocN v2.0
- [**GAP_ANALYSIS_E_RACCOMANDAZIONI.md**](GAP_ANALYSIS_E_RACCOMANDAZIONI.md) - 📈 Gap Analysis & Roadmap
- [**PROSSIME_FASI.md**](PROSSIME_FASI.md) - 🚀 Prossime Fasi di Sviluppo (Quick Reference)

### Guide Rapide
- [**GUIDA_CONFIGURAZIONE_GEMINI.md**](GUIDA_CONFIGURAZIONE_GEMINI.md) - 🇮🇹 Guida completa configurazione Gemini (italiano)
- [**docs/EMBEDDING_QUEUE_MONITORING.md**](docs/EMBEDDING_QUEUE_MONITORING.md) - 🇮🇹 Monitoraggio coda embeddings e troubleshooting
- [**docs/ALERTING_RUNBOOK.md**](docs/ALERTING_RUNBOOK.md) - 🚨 Runbook gestione alert e monitoring
- [**docs/RAG_QUALITY_GUIDE.md**](docs/RAG_QUALITY_GUIDE.md) - ✅ Guida qualità RAG e RAGAS metrics
- [**docs/MONITORING_INTEGRATION_GUIDE.md**](docs/MONITORING_INTEGRATION_GUIDE.md) - 📊 Integrazione Prometheus, Grafana, Slack

### Documentazione Avanzata
- [**ENTERPRISE_RAG_ROADMAP.md**](ENTERPRISE_RAG_ROADMAP.md) - Roadmap funzionalità enterprise
- [**SECURITY_BEST_PRACTICES.md**](SECURITY_BEST_PRACTICES.md) - Best practices sicurezza
- [**DEPLOYMENT_GUIDE.md**](DEPLOYMENT_GUIDE.md) - Guida deployment produzione
- [**MONITORING_OBSERVABILITY.md**](MONITORING_OBSERVABILITY.md) - Monitoring e observability
- [**API_DOCUMENTATION.md**](API_DOCUMENTATION.md) - Documentazione API REST
- [**MULTI_PROVIDER_CONFIG.md**](MULTI_PROVIDER_CONFIG.md) - Configurazione multi-provider AI
- [**OCR_IMPLEMENTATION.md**](OCR_IMPLEMENTATION.md) - Implementazione OCR
- [**VECTOR_TYPE_GUIDE.md**](VECTOR_TYPE_GUIDE.md) - Guida tipi vettoriali SQL Server

### Guide Database
- [**Database/README.md**](Database/README.md) - Configurazione database
- [**Database/QUICK_START.md**](Database/QUICK_START.md) - Quick start database
- [**MIGRATION_GUIDE.md**](MIGRATION_GUIDE.md) - Guida migrazione

## 🎯 Casi d'Uso

### 1. Ricerca Documentale Intelligente
Trova documenti rilevanti utilizzando linguaggio naturale. Il sistema combina ricerca semantica e full-text per risultati ottimali.

### 2. Chat con Documenti
Interagisci con i tuoi documenti ponendo domande. Il sistema RAG recupera informazioni rilevanti e genera risposte naturali con citazioni.

### 3. Categorizzazione Automatica
L'AI analizza i documenti e suggerisce automaticamente categorie e tag basandosi sul contenuto.

### 4. OCR e Digitalizzazione
Carica documenti scansionati o immagini. Il sistema estrae automaticamente il testo rendendolo ricercabile.

### 5. Knowledge Base Aziendale
Costruisci una knowledge base organizzativa con ricerca semantica e accesso controllato.

## 🔧 Configurazione

### Configurazione AI Providers

Accedi a `/config` nell'applicazione per configurare:
- Provider AI (Gemini, OpenAI, Azure OpenAI)
- Modelli per Chat, Embeddings, Tag Extraction
- Parametri RAG (similarity threshold, max documents)
- Chunking configuration
- Fallback automatico

**📚 Per capire come funziona l'inizializzazione del provider RAG, consulta:**
[**RAG_PROVIDER_INITIALIZATION_GUIDE.md**](RAG_PROVIDER_INITIALIZATION_GUIDE.md) - Guida completa che spiega dove e come viene inizializzato il provider RAG per i tuoi documenti.

### Configurazione Database

Modifica `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DocN;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

### Configurazione OCR

Modifica `appsettings.json`:
```json
{
  "Tesseract": {
    "DataPath": "./tessdata",
    "Language": "ita"
  }
}
```

## 🧪 Testing

```bash
# Run tutti i test
dotnet test

# Run test specifici
dotnet test --filter "FullyQualifiedName~DocN.Core.Tests"

# Con code coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsFormat=opencover
```

## 📈 Performance

### Metriche Tipiche
- **Upload documento**: 2-5 secondi (con AI processing)
- **Ricerca semantica**: 100-300ms
- **Ricerca ibrida**: 200-500ms
- **Chat RAG**: 2-4 secondi (dipende da provider AI)
- **OCR**: 1-3 secondi per immagine

### Ottimizzazioni
- Caching configurazioni AI (5 minuti)
- Connection pooling database
- Lazy loading componenti Blazor
- Batch processing embeddings

## 🤝 Contributi

Contributi sono benvenuti! Per favore:
1. Fork il repository
2. Crea un branch per la feature (`git checkout -b feature/AmazingFeature`)
3. Commit le modifiche (`git commit -m 'Add AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Apri una Pull Request

## 📝 Licenza

Questo progetto è distribuito sotto licenza MIT. Vedi file `LICENSE` per dettagli.

## 🙏 Crediti

- **Microsoft Semantic Kernel**: Orchestrazione AI
- **Tesseract OCR**: Estrazione testo da immagini
- **SQL Server 2025**: Supporto vettori nativi
- **Gemini AI**: Embeddings e chat di Google
- **OpenAI**: GPT models e embeddings
- **Azure OpenAI**: Enterprise AI services

## 📞 Supporto

Per domande, problemi o feature request:
- Apri un [Issue](https://github.com/Moncymr/DocN/issues)
- Consulta la [Wiki](https://github.com/Moncymr/DocN/wiki)
- Email: support@docn.example.com

### Problemi Comuni

**Domanda: "Dove inizializza il provider per RAG dei miei documenti?"**

Consulta la guida completa: [RAG_PROVIDER_INITIALIZATION_GUIDE.md](RAG_PROVIDER_INITIALIZATION_GUIDE.md)

In breve:
- Il provider viene inizializzato automaticamente in `DocN.Server/Program.cs` alla riga 324
- La configurazione viene caricata dal database (tabella `AIConfigurations`) o da `appsettings.json`
- Non è necessaria inizializzazione manuale - tutto è gestito da Dependency Injection

**Errore: "AI_PROVIDER_NOT_CONFIGURED"**

Il sistema non ha trovato nessun provider AI configurato:
1. Vai in Settings (`/config`) nell'applicazione
2. Configura almeno un provider (Gemini, OpenAI, o Azure OpenAI)
3. Inserisci una API key valida
4. Attiva la configurazione (toggle "Active")
5. Riprova la chat

**Errore: "ERRORE CRITICO: Il salvataggio nel database è fallito"**

Questo errore indica che la migrazione del database non è stata applicata. Soluzioni:
1. Riavvia l'applicazione (le migrazioni vengono applicate automaticamente)
2. Verifica i log per errori di migrazione
3. Controlla che l'utente del database abbia i permessi per modificare le tabelle
4. Consulta `Database/UpdateScripts/README_005_FixOwnerIdConstraint.md` per dettagli

**Errore: "Connection to database failed"**

- Verifica la connection string in `appsettings.json`
- Assicurati che SQL Server sia in esecuzione
- Verifica le credenziali di accesso
- Controlla il firewall per la porta 1433

**Errore: "OCR non disponibile"**

- Installa Tesseract OCR (vedi sezione Quick Start)
- Verifica che `tessdata` sia nella cartella corretta
- Controlla i permessi di lettura su `tessdata`

**Domanda: "Quanto tempo ci vuole per elaborare gli embeddings di un PDF?"**

Gli embeddings vengono elaborati in background per non rallentare il caricamento:
- **PDF semplice (10-20 pagine)**: ~2-5 minuti con Gemini
- **Documento lungo**: ~5-15 minuti
- Il processo avviene automaticamente ogni 30 secondi
- Monitora lo stato nel Dashboard o nella pagina Documenti (badge "⏳ Embeddings in coda")
- Consulta la guida completa: [docs/EMBEDDING_QUEUE_MONITORING.md](docs/EMBEDDING_QUEUE_MONITORING.md)

**Problema: "Gli embeddings non vengono mai completati"**

1. Verifica che il `BatchEmbeddingProcessor` sia attivo nei log
2. Controlla la configurazione AI in `/config`
3. Consulta la guida troubleshooting: [docs/EMBEDDING_QUEUE_MONITORING.md](docs/EMBEDDING_QUEUE_MONITORING.md)

## 🗺️ Roadmap

Vedi [PROSSIME_FASI.md](PROSSIME_FASI.md) per il piano dettagliato delle prossime fasi di sviluppo.

### Prossimi Rilasci

**v2.1 - Enterprise Features** (Q1 2025)
- Audit logging completo
- API REST documentata con OpenAPI
- Monitoring e metrics
- Rate limiting

**v2.2 - Advanced RAG** (Q2 2025)
- Query rewriting
- Multi-query retrieval
- Re-ranking avanzato
- Cache distribuita

**v3.0 - Scale & Performance** (Q3 2025)
- Supporto PostgreSQL + pgvector
- Deployment Kubernetes
- Load balancing
- Horizontal scaling

---

**Versione**: 2.0.0  
**Ultimo Aggiornamento**: Dicembre 2024  
**Stato**: Production Ready ✅
