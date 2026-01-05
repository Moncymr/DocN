# 📘 Guida Utente DocN

## Documento Word Completo Disponibile

È stata creata una **guida utente completa in formato Word** che spiega in dettaglio tutte le funzionalità di DocN e l'utilizzo dei provider AI.

📄 **File**: `GUIDA_UTENTE_COMPLETA.docx` (in questa cartella)

## Contenuto del Documento

Il documento Word include:

### 1. 🎯 Introduzione a DocN
- Panoramica del sistema
- Funzionalità principali
- Architettura generale

### 2. 🤖 Provider AI e Loro Utilizzo
Spiega in dettaglio quando e come vengono utilizzati i diversi provider:

- **🧠 Embedding Provider** (Gemini, OpenAI, Azure OpenAI, Ollama)
  - **Quando**: Durante il caricamento documenti
  - **Scopo**: Generare vettori per ricerca semantica
  
- **🏷️ Tag Provider** (Tutti i provider)
  - **Quando**: Dopo estrazione testo dal documento
  - **Scopo**: Suggerire categoria e tag automaticamente
  
- **💬 Chat Provider** (Tutti i provider)
  - **Quando**: Durante conversazioni in /chat
  - **Scopo**: Gestire dialogo con l'utente
  
- **🔍 RAG Provider** (Tutti i provider)
  - **Quando**: Chat con documenti
  - **Scopo**: Generare risposte basate sui documenti trovati

> **⚠️ Nota**: Groq NON supporta embeddings, può essere usato solo per Chat e Tag

### 3. 📱 Tutte le Pagine Spiegate

Descrizione dettagliata di ogni pagina:

- **Home** (`/`) - Dashboard principale
- **Upload** (`/upload`) - Caricamento singolo con analisi AI
- **Upload Multiple** (`/uploadmultiple`) - Caricamento batch
- **Documents** (`/documents`) - Gestione biblioteca
- **Search** (`/search`) - Ricerca avanzata (vettoriale/testuale/ibrida)
- **Chat** (`/chat`) - Conversazione con documenti
- **Dashboard** (`/dashboard`) - Statistiche e monitoraggio
- **AI Config** (`/config`) - Configurazione provider
- **Agents** (`/agents`) - Assistenti AI personalizzati
- **Monitoring** - Alert, Qualità RAG, Diagnostica

### 4. 🔄 Workflow Completi

Flussi operativi passo-passo con indicazione precisa dei provider utilizzati:

#### Workflow Upload:
1. Selezione file → Nessun provider
2. Estrazione testo → OCR/FileProcessing
3. Analisi contenuto → **Tag Provider**
4. Generazione embeddings documento → **Embedding Provider**
5. Creazione chunks → ChunkingService
6. Embeddings chunks → **Embedding Provider**
7. Ricerca simili → **RAG Service**

#### Workflow Ricerca:
1. Query utente → Nessun provider
2. Conversione query in vettore → **Embedding Provider**
3. Ricerca database vettoriale → PostgreSQL pgvector

#### Workflow Chat:
1. Domanda utente → Nessun provider
2. Conversione in vettore → **Embedding Provider**
3. Ricerca documenti → **RAG Service**
4. Generazione risposta → **RAG Provider** + **Chat Provider**

### 5. 💡 Best Practices

Consigli per:
- Scelta del provider giusto per scenario
- Ottimizzazione ricerca
- Gestione documenti
- Monitoraggio sistema

### 6. 🔧 Troubleshooting

Soluzioni a problemi comuni:
- Embeddings non generati
- Ricerca non funziona
- Chat produce risposte errate
- Provider non risponde

### 7. 📖 Glossario

Spiegazione termini tecnici:
- Embedding, Vector, Chunk
- RAG, Semantic Search
- Top K, OCR, Similarity
- RAGAS, Hallucination

### 8. 📎 Appendice

- Credenziali default (admin@docn.local / Admin@123)
- Link a documentazione avanzata
- Informazioni supporto

## Come Usare Questo Documento

1. **Scarica** il file `GUIDA_UTENTE_COMPLETA.docx`
2. **Apri** con Microsoft Word, LibreOffice, o Google Docs
3. **Consulta** le sezioni rilevanti per le tue esigenze
4. **Condividi** con gli utenti che devono utilizzare DocN

## Formato

- **Tipo**: Microsoft Word (.docx)
- **Dimensione**: ~43 KB
- **Pagine**: ~15-20 pagine
- **Lingua**: Italiano
- **Versione**: 1.0 (Gennaio 2026)

## Per Chi È Questo Documento

✅ **Utenti finali** - Imparare a usare DocN  
✅ **Amministratori** - Configurare provider AI  
✅ **Team tecnico** - Capire architettura e workflow  
✅ **Nuovi utenti** - Guida completa per iniziare  

## Documentazione Aggiuntiva

Per approfondimenti tecnici, consulta anche:

- `ALERTING_RUNBOOK.md` - Gestione alert
- `RAG_QUALITY_GUIDE.md` - Metriche qualità
- `MONITORING_INTEGRATION_GUIDE.md` - Setup monitoraggio
- `MULTI_FILE_UPLOAD.md` - Caricamento multiplo
- `DATABASE-SETUP-COMPLETO.md` - Setup database

---

**💡 Suggerimento**: Stampa il documento o condividilo con il tuo team per facilitare l'onboarding su DocN!
