# Documentazione Tecnica per Sviluppatori DocN

## 📄 Documento Word - Analisi Dettagliata Sistema

Il file **DocN_Documentazione_Sviluppatori.docx** contiene una documentazione tecnica completa del sistema DocN, progettata per fornire agli sviluppatori una comprensione approfondita dell'architettura, delle tecnologie e delle modalità di sviluppo.

## 📋 Contenuto del Documento

### 1. Panoramica del Sistema DocN
- Introduzione al sistema di gestione documentale con AI
- Caratteristiche principali (RAG, ricerca semantica, OCR, agenti AI)
- Obiettivi del sistema
- Casi d'uso principali

### 2. Architettura e Progetti
- Struttura della soluzione .NET
- Descrizione dettagliata dei progetti principali:
  - **DocN.Server** - Backend API (ASP.NET Core Web API)
  - **DocN.Client** - Frontend (Blazor Web App)
  - **DocN.Data** - Data Access Layer (Entity Framework)
  - **DocN.Core** - Core AI Logic (Semantic Kernel)
  - **DocN.Core.Tests** - Unit Tests (xUnit) - Test Project
- Relazioni e dipendenze tra progetti

### 3. Tecnologie e Stack Tecnologico
- Framework: .NET 10.0, ASP.NET Core, Blazor
- Database: SQL Server, Entity Framework Core 10.0
- AI/ML: Semantic Kernel, OpenAI, Azure OpenAI, Gemini, Ollama
- Document Processing: OpenXml, ClosedXML, iText7, Tesseract OCR
- Background Jobs: Hangfire
- Cache: Redis (StackExchange.Redis)
- Monitoring: OpenTelemetry, Prometheus, Serilog, App.Metrics
- API Documentation: Swagger/OpenAPI

### 4. Analisi Dettagliata per Progetto

#### DocN.Server
- Controllers principali e loro funzioni
- Middleware (alerting, exception handling, logging)
- Services (seeding, cache, health checks)
- Caratteristiche tecniche (rate limiting, CORS, auth)

#### DocN.Client
- Pagine principali dell'applicazione
- Componenti riutilizzabili
- Funzionalità UI (upload, search, chat, monitoring)

#### DocN.Data
- DbContext e modelli dati
- Servizi principali (AI, RAG, embeddings, OCR, alerting)
- Background services (batch processing, cleanup, monitoring)
- Utilities (chunking, vector similarity)

#### DocN.Core
- Provider AI supportati (OpenAI, Azure, Gemini, Ollama, Groq)
- Integrazione Semantic Kernel
- Agent Framework

### 5. Database e Modelli Dati
- Schema database completo
- Tabelle documenti, Identity, AI, sistema
- Vector embeddings storage (768/1536 dimensioni)
- Migrations e gestione schema

### 6. API e Servizi
- Endpoints API completi:
  - Documents API (upload, download, metadata)
  - Search API (full-text, semantic, hybrid)
  - Chat/RAG API (message, streaming, history)
  - Agents API (templates, create, execute)
  - RAG Quality API (verify, RAGAS, dashboard, A/B test)
  - Monitoring & Alerting API
- Servizi AI dettagliati:
  - RAG Service (6-step pipeline)
  - Embedding Service (multi-provider, batch, cache)
  - Quality Monitoring (RAGAS metrics)

### 7. Configurazione e Deployment
- Configurazione AI providers (OpenAI, Azure, Gemini, Ollama)
- Database setup e migrations
- Credenziali default (admin@docn.local / Admin@123)
- Storage configuration
- Background jobs (Hangfire)
- Monitoring setup (OpenTelemetry, Prometheus)
- Opzioni deployment (IIS, Docker, Kubernetes, Azure)

### 8. Guide per Sviluppatori
- Setup ambiente di sviluppo
- Come aggiungere un nuovo provider AI
- Strategie di testing (unit, integration, API, load)
- Best practices
- Troubleshooting comune:
  - Embeddings non generati
  - RAG inaccurato
  - Performance lente
- Documentazione aggiuntiva nella cartella /docs

## 🎯 Scopo del Documento

Questo documento è stato creato per:

1. **Onboarding Sviluppatori** - Fornire una comprensione completa del sistema a nuovi sviluppatori
2. **Riferimento Tecnico** - Servire come guida di riferimento per l'architettura e le tecnologie
3. **Documentazione Progetti** - Spiegare lo scopo e le responsabilità di ogni progetto
4. **Guida Sviluppo** - Fornire best practices e guide per lo sviluppo
5. **Analisi Dettagliata** - Offrire una visione approfondita di tutti i componenti del sistema

## 📊 Formato e Struttura

- **Formato**: Microsoft Word (.docx)
- **Dimensione**: ~9KB
- **Pagine**: Circa 15-20 pagine formattate
- **Stile**: Professionale con intestazioni colorate, bullet points, e sezioni ben organizzate
- **Generazione**: Automatica tramite DocumentFormat.OpenXml

## 🔧 Come è Stato Generato

Il documento è stato generato programmaticamente utilizzando:
- **DocumentFormat.OpenXml 3.2.0** - Library per creazione documenti Word
- **Linguaggio**: C# / .NET 10.0
- **Metodo**: Generazione automatica basata sull'analisi del repository

## 📚 Documentazione Correlata

Per approfondimenti specifici, consultare anche:
- `RAG_QUALITY_GUIDE.md` - Guida completa qualità RAG e metriche RAGAS
- `ALERTING_RUNBOOK.md` - Runbook alerting e monitoraggio
- `MULTI_FILE_UPLOAD.md` - Guida upload multiplo documenti
- `MONITORING_INTEGRATION_GUIDE.md` - Setup monitoring e integrations
- `DATABASE-SETUP-COMPLETO.md` - Setup database dettagliato
- `COME_TESTARE_ENDPOINT_API.md` - Testing API endpoints

## ✅ Utilizzo

Il documento Word può essere:
- Aperto con Microsoft Word 2007 o superiore
- Aperto con LibreOffice Writer
- Aperto con Google Docs
- Utilizzato per onboarding team
- Stampato per reference fisica
- Condiviso con stakeholders tecnici

## 🔄 Aggiornamenti

Per mantenere il documento aggiornato con future modifiche al sistema:
1. Modificare il codice generatore se necessario
2. Rigenerare il documento con le informazioni aggiornate
3. Verificare che tutte le sezioni riflettano lo stato corrente del sistema

---

**Versione Documento**: 1.0  
**Data Creazione**: Gennaio 2026  
**Stato**: Completo e Aggiornato
