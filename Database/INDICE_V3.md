# 📚 Guida ai File Database V3

## 🎯 Inizio Rapido

**Se vuoi solo lo script completo:**
👉 **Usa: `CreateDatabase_Complete_V3.sql`** 

Questo è TUTTO quello che ti serve per creare un database completo con tutte le funzionalità.

## 📁 File Disponibili

### Script SQL Principali

#### ⭐ CreateDatabase_Complete_V3.sql (CONSIGLIATO)
**File:** `Database/CreateDatabase_Complete_V3.sql`
- **Dimensione:** 40 KB (1147 righe)
- **Cosa fa:** Crea database completo con TUTTE le funzionalità
- **Include:** V2 + tutti gli aggiornamenti consolidati
- **Quando usare:** Nuovo database o ricreazione completa

#### CreateDatabase_Complete_V2.sql
**File:** `Database/CreateDatabase_Complete_V2.sql`
- **Dimensione:** 34 KB (985 righe)
- **Cosa fa:** Versione precedente (legacy)
- **Quando usare:** Solo se hai vincoli su Azure OpenAI

#### CreateDatabase.sql
**File:** `Database/CreateDatabase.sql`
- **Dimensione:** 13 KB (342 righe)
- **Cosa fa:** Versione base iniziale (legacy)
- **Quando usare:** Non usare più, obsoleto

### Script di Aggiornamento Incrementali

Nella cartella `Database/UpdateScripts/`:

1. **001_AddMultiProviderAIConfiguration.sql**
   - Aggiunge supporto multi-provider (Gemini, OpenAI, Azure)
   
2. **002_AddSimilarDocumentsTable.sql**
   - Crea tabella per similarità documenti
   
3. **003_AddLogEntriesTable.sql**
   - Crea tabella per logging centralizzato
   
4. **004_UpdateGeminiDefaultModel.sql**
   - Aggiorna modello a gemini-2.0-flash-exp
   
5. **005_FixOwnerIdForeignKeyConstraint.sql**
   - Corregge vincolo FK per OwnerId
   
6. **AddExtractedMetadataJson.sql**
   - Aggiunge campo per metadata AI

**Quando usare:** Solo se hai già un database V2 e vuoi aggiornarlo a V3

### Documentazione

#### 📖 RIEPILOGO_V3.md (LEGGI PRIMA)
**File:** `Database/RIEPILOGO_V3.md`
- **Lingua:** 🇮🇹 Italiano
- **Cosa contiene:**
  - Panoramica completa V3
  - Come usare lo script
  - Credenziali predefinite
  - Guida configurazione
  - Verifica installazione

#### 📖 README_V3.md (DOCUMENTAZIONE COMPLETA)
**File:** `Database/README_V3.md`
- **Lingua:** 🇮🇹 Italiano
- **Cosa contiene:**
  - Documentazione tecnica dettagliata
  - Lista completa tabelle e campi
  - Stored procedures e views
  - Troubleshooting
  - Manutenzione database
  - Query di esempio

#### 📊 V2_vs_V3_COMPARISON.md
**File:** `Database/V2_vs_V3_COMPARISON.md`
- **Lingua:** 🇮🇹 Italiano
- **Cosa contiene:**
  - Confronto dettagliato V2 vs V3
  - Tabella differenze
  - Statistiche file
  - Guida migrazione

## 🚦 Quale File Usare?

### Scenario 1: Nuovo Database
```
✅ USA: CreateDatabase_Complete_V3.sql
📖 LEGGI: RIEPILOGO_V3.md
```

### Scenario 2: Ho già V2, voglio aggiornare a V3
```
✅ USA: Tutti gli script in UpdateScripts/ (001-006)
📖 LEGGI: V2_vs_V3_COMPARISON.md
```

### Scenario 3: Voglio capire cosa contiene V3
```
📖 LEGGI: RIEPILOGO_V3.md (panoramica)
📖 POI LEGGI: README_V3.md (dettagli)
```

### Scenario 4: Voglio confrontare V2 e V3
```
📖 LEGGI: V2_vs_V3_COMPARISON.md
```

## 📋 Ordine di Lettura Consigliato

1. **`RIEPILOGO_V3.md`** ⭐ (questo documento)
   - Panoramica veloce di cosa hai e come usarlo

2. **`CreateDatabase_Complete_V3.sql`** ⭐
   - Script SQL da eseguire

3. **`README_V3.md`**
   - Solo se hai bisogno di dettagli tecnici

4. **`V2_vs_V3_COMPARISON.md`**
   - Solo se stai migrando da V2

## ✅ Checklist Setup

- [ ] Leggere `RIEPILOGO_V3.md`
- [ ] Verificare SQL Server 2025+ installato
- [ ] Eseguire `CreateDatabase_Complete_V3.sql`
- [ ] Verificare database creato (18 tabelle)
- [ ] Configurare `appsettings.json`
- [ ] Testare login con admin@docn.local / Admin@123
- [ ] Cambiare password admin
- [ ] Configurare API keys (Gemini, OpenAI, etc.)
- [ ] Avviare applicazione

## 🎁 Cosa Ottieni

Eseguendo **CreateDatabase_Complete_V3.sql** ottieni:

✅ **18 Tabelle** (Identity, Documents, Conversations, AI Config, Audit, Logging)
✅ **6 Stored Procedures** (RAG, Search, Maintenance)
✅ **2 Views** (Statistics, User Activity)
✅ **~45 Indici** ottimizzati
✅ **1 Utente Admin** predefinito
✅ **1 Tenant** predefinito
✅ **3 Ruoli** (Admin, User, Manager)
✅ **1 Configurazione AI** multi-provider
✅ **Full-text Search** abilitato
✅ **Vector Support** (SQL Server 2025)

## 🆘 Aiuto

**Problema con l'installazione?**
→ Leggi sezione "Troubleshooting" in `README_V3.md`

**Errori durante l'esecuzione script?**
→ Verifica SQL Server 2025+ e tipo VECTOR supportato

**Database già esistente?**
→ Lo script verifica e salta se già presente

**Voglio aggiornare da V2?**
→ Leggi `V2_vs_V3_COMPARISON.md` sezione "Migrazione"

**Non capisco cosa fa una tabella?**
→ Leggi `README_V3.md` sezione "Tabelle"

## 🌟 Funzionalità V3

### 🆕 Novità rispetto a V2

1. **Multi-Provider AI**
   - Gemini, OpenAI, Azure OpenAI
   - Scegli provider per ogni servizio
   - Fallback automatico

2. **Similarità Documenti**
   - Tabella SimilarDocuments
   - Top 5 documenti correlati
   - Score di similarità

3. **Logging Centralizzato**
   - Tabella LogEntries
   - Tutti i log in database
   - Facile debug e monitoring

4. **Metadata AI**
   - ExtractedMetadataJson
   - Numeri fattura, date, autori
   - Ricerca strutturata

5. **Gemini 2.0**
   - Modello aggiornato
   - Non più deprecato
   - Migliori performance

6. **OwnerId Fix**
   - Nullable
   - ON DELETE SET NULL
   - Supporto documenti pubblici

## 📞 Contatti e Riferimenti

- **Repository:** [Moncymr/DocN](https://github.com/Moncymr/DocN)
- **Documentazione Principale:** `README.md` (root del progetto)
- **API Documentation:** `API_DOCUMENTATION.md`
- **Multi-Provider Guide:** `MULTI_PROVIDER_CONFIG.md`

---

**Versione Guida:** 1.0  
**Data:** 29 Dicembre 2024  
**Lingua:** 🇮🇹 Italiano  

**Buon lavoro con DocN V3!** 🚀
