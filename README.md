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

```
DocN/
├── DocN.Server/          # API Backend (ASP.NET Core)
├── DocN.Client/          # Frontend (Blazor WebAssembly)
├── DocN.Data/            # Data Layer, Services, Migrations
├── DocN.Core/            # Domain Models, Interfaces
├── tests/                # Unit e Integration Tests
└── Database/             # Script SQL, Migrations
```

### Stack Tecnologico

- **Framework**: .NET 10.0
- **Frontend**: Blazor WebAssembly
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
   # Modifica appsettings.json con la tua connection string
   cd Database
   sqlcmd -S localhost -U sa -P YourPassword -i SqlServer2025_Schema.sql
   ```

3. **Configurazione AI Providers**
   ```bash
   cd DocN.Server
   dotnet user-secrets init
   dotnet user-secrets set "Gemini:ApiKey" "your-gemini-key"
   dotnet user-secrets set "OpenAI:ApiKey" "your-openai-key"
   ```

4. **Avvio Applicazione**
   ```bash
   dotnet run --project DocN.Server
   ```

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

## 📚 Documentazione

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

## 🗺️ Roadmap

Vedi [ENTERPRISE_RAG_ROADMAP.md](ENTERPRISE_RAG_ROADMAP.md) per le funzionalità pianificate.

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
