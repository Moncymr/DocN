# Documentazione Tecnica - Progetti DocN
## Analisi Completa per Analisti e Sviluppatori

**Versione**: 2.0.0  
**Data**: Dicembre 2024  
**Destinatari**: Analisti funzionali e sviluppatori tecnici

---

## 📋 Indice

1. [Panoramica Architetturale](#panoramica-architetturale)
2. [DocN.Client - Frontend Blazor](#docnclient---frontend-blazor)
3. [DocN.Server - Backend API](#docnserver---backend-api)
4. [DocN.Core - Domain Layer](#docncore---domain-layer)
5. [DocN.Data - Data Access Layer](#docndata---data-access-layer)
6. [Database - SQL Server 2025](#database---sql-server-2025)
7. [Flussi Principali](#flussi-principali)
8. [Tecnologie e Dipendenze](#tecnologie-e-dipendenze)

---

## Panoramica Architetturale

### Architettura Generale

DocN implementa un'architettura multi-tier con separazione delle responsabilità. L'applicazione è divisa in due server principali:

1. **DocN.Client (Blazor Server)** - Porta 7114: Frontend con UI e autenticazione
2. **DocN.Server (ASP.NET Core API)** - Porta 5211: Backend con servizi RAG e AI

Entrambi i server devono essere in esecuzione per utilizzare tutte le funzionalità.

### Principi Architetturali

1. **Separation of Concerns**: Ogni progetto ha una responsabilità specifica
2. **Dependency Injection**: Gestione delle dipendenze tramite DI container .NET
3. **Clean Architecture**: Domain-centric con dipendenze che puntano verso l'interno
4. **SOLID Principles**: Codice modulare, estensibile e testabile
5. **Multi-Tenancy**: Isolamento dati per organizzazione
6. **Security by Design**: Autenticazione, autorizzazione, validazione input

---

## DocN.Client - Frontend Blazor

### Scopo del Progetto

**Per l'Analista**: DocN.Client è l'interfaccia utente web attraverso cui gli utenti interagiscono con il sistema. Gestisce tutte le pagine visibili (dashboard, upload, ricerca, chat) e l'autenticazione degli utenti.

**Per lo Sviluppatore**: Applicazione Blazor Server che implementa il pattern Component-Based Architecture. Utilizza SignalR per la comunicazione real-time tra server e client, gestisce lo stato dell'applicazione lato server e fornisce un'esperienza utente reattiva.

### Funzionalità Principali

#### 1. Autenticazione e Autorizzazione
- **Tecnologia**: ASP.NET Core Identity
- **Componenti**: Login, Register, ForgotPassword, LoginDisplay

#### 2. Gestione Documenti
- Upload con drag & drop
- Validazione file (tipo, dimensione)
- Preview documenti
- Modifica metadati
- Eliminazione e condivisione

#### 3. Ricerca
- Full-text search
- Semantic search
- Filtri avanzati

#### 4. Chat RAG
- Conversazioni natural language
- Mantenimento contesto
- Citazioni documenti

### Stack Tecnologico

- .NET 10.0
- Blazor Server 10.0
- ASP.NET Core Identity 10.0
- SignalR 10.0
- Bootstrap 5.3

---

## DocN.Server - Backend API

### Scopo del Progetto

**Per l'Analista**: DocN.Server è il motore che alimenta le funzionalità avanzate di intelligenza artificiale. Gestisce la chat con i documenti, la ricerca semantica avanzata e tutte le operazioni RAG.

**Per lo Sviluppatore**: ASP.NET Core Web API che espone endpoint REST per servizi AI avanzati. Integra Microsoft Semantic Kernel per orchestrazione AI, gestisce provider multipli e implementa health checks.

### Funzionalità Principali

#### 1. API REST per Chat Semantica
- Endpoint: `/api/semantic-chat/query`
- Processo RAG completo
- Streaming risposte

#### 2. API Ricerca Documenti
- Ricerca ibrida (vector + full-text)
- Upload documenti
- Gestione metadati

#### 3. API Configurazione AI
- Gestione provider (Gemini, OpenAI, Azure)
- Test connessioni
- Attivazione/disattivazione

#### 4. Health Checks
- Verifica provider AI
- Verifica database
- Verifica OCR
- Endpoint: `/health`

### Stack Tecnologico

- ASP.NET Core 10.0
- Microsoft Semantic Kernel 1.x
- Swagger/OpenAPI
- Serilog
- Hangfire

---

## DocN.Core - Domain Layer

### Scopo del Progetto

**Per l'Analista**: DocN.Core contiene le regole di business, i modelli di dati e le interfacce che definiscono come il sistema dovrebbe comportarsi.

**Per lo Sviluppatore**: Domain layer che implementa i principi di Clean Architecture. Contiene interfacce di servizi, modelli di dominio ed estensioni.

### Componenti Principali

#### 1. Interfaces
- ISemanticRAGService
- IEmbeddingService
- IOCRService
- IChunkingService
- ICategoryService

#### 2. AI Models
- AIConfiguration
- AIProvider
- RAGConfiguration
- Chat models

#### 3. Extensions
- ServiceCollectionExtensions
- AIServiceExtensions

#### 4. Semantic Kernel Integration
- EmbeddingService wrapper
- Custom plugins

---

## DocN.Data - Data Access Layer

### Scopo del Progetto

**Per l'Analista**: DocN.Data gestisce tutta la comunicazione con il database e implementa la logica di business complessa.

**Per lo Sviluppatore**: Data Access Layer che implementa il pattern Repository. Contiene DbContext, migrazioni EF, e implementazioni concrete dei servizi.

### Componenti Principali

#### 1. DbContext
- ApplicationDbContext (Identity)
- DocArcContext (Documenti e RAG)

#### 2. Models
- Document
- DocumentChunk
- Embedding
- Category, Tag
- AIConfiguration
- ChatHistory
- AuditLog

#### 3. Services
- SemanticRAGService
- MultiProviderAIService
- EmbeddingService
- TesseractOCRService
- ChunkingService
- HybridSearchService

#### 4. Migrations
- Tracciamento evoluzione schema
- Applicazione automatica all'avvio

---

## Database - SQL Server 2025

### Scopo

**Per l'Analista**: Il database è il cuore del sistema dove vengono conservati tutti i dati con tecnologie avanzate per ricerca veloce.

**Per lo Sviluppatore**: SQL Server 2025 con supporto nativo VECTOR, stored procedures per ricerca ibrida, full-text indexes.

### Schema Database

#### Tabelle Principali
- Documents
- DocumentChunks
- Embeddings (con tipo VECTOR nativo)
- AIConfigurations
- ChatHistories
- AuditLogs
- Categories, Tags
- AspNetUsers (Identity)
- Organizations

#### Stored Procedures
- **SearchDocumentsByVector**: Ricerca semantica
- **HybridSearch**: Ricerca ibrida con RRF
- **GetUserDocuments**: Documenti utente con permessi

---

## Flussi Principali

### Flusso 1: Upload e Elaborazione Documento

1. Utente seleziona file
2. Validazione file
3. Upload a storage
4. FileProcessingService elabora:
   - Estrazione testo (OCR se immagine)
   - Chunking
   - Generazione embeddings
   - Estrazione categoria/tag AI
5. Salvataggio database
6. Conferma utente

**Tempo**: 2-5 secondi

### Flusso 2: Ricerca Semantica

1. Query utente
2. Generazione embedding query
3. HybridSearch stored procedure:
   - Vector search
   - Full-text search
   - Reciprocal Rank Fusion
4. Applicazione filtri permessi
5. Return risultati ordinati

**Tempo**: 100-300ms

### Flusso 3: Chat RAG

1. Domanda utente
2. SemanticRAGService:
   - Query understanding
   - Document retrieval (top K)
   - Context building
   - AI generation (Semantic Kernel)
   - Response formatting
3. Salvataggio ChatHistory
4. Return risposta con citazioni

**Tempo**: 2-4 secondi

---

## Tecnologie e Dipendenze

### Tecnologie Core

- .NET 10.0
- ASP.NET Core 10.0
- Blazor Server 10.0
- Entity Framework Core 10.0
- SQL Server 2025
- C# 12.0

### AI & ML

- Microsoft Semantic Kernel 1.x
- Google Gemini API 2.0
- OpenAI API
- Azure OpenAI
- Tesseract OCR 5.x

### Infrastructure

- Docker
- Kubernetes (production)
- Redis (caching)
- Nginx (reverse proxy)

---

## Conclusione

DocN è un sistema enterprise-grade con architettura pulita e scalabile per gestione documentale intelligente.

### Punti di Forza

1. Architettura Multi-Tier
2. AI Multi-Provider
3. RAG Avanzato con Semantic Kernel
4. Database Moderno con VECTOR nativo
5. Ricerca Ibrida ottimizzata
6. Multi-Tenancy
7. Security completa
8. Observability (health checks, audit)

---

**Per ulteriori informazioni**:
- Repository: https://github.com/Moncymr/DocN
- Documentazione: README.md e guide specifiche
