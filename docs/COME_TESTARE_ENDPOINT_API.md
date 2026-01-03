# Come Testare gli Endpoint API di Qualità RAG

## 📋 Panoramica

Questa guida mostra come testare e vedere i risultati degli endpoint API per la qualità RAG.

---

## 🔧 Metodi per Testare gli Endpoint

### Metodo 1: Swagger UI (Più Facile) ✨

**Passo 1**: Avvia il server
```bash
cd DocN.Server
dotnet run
```

**Passo 2**: Apri Swagger nel browser
```
https://localhost:5211/swagger
```

**Passo 3**: Trova la sezione "RAGQuality"

**Passo 4**: Clicca su un endpoint per espanderlo

**Passo 5**: Clicca "Try it out"

**Passo 6**: Compila i parametri (vedi esempi sotto)

**Passo 7**: Clicca "Execute"

**Passo 8**: Vedi la risposta in basso

---

## 📍 Endpoint Disponibili

### 1. Verifica Qualità Risposta

**Endpoint**: `POST /api/rag-quality/verify`

**Cosa Fa**: Verifica la qualità di una risposta RAG specifica

**Come Testare in Swagger**:
1. Espandi "POST /api/rag-quality/verify"
2. Clicca "Try it out"
3. Inserisci questo JSON:

```json
{
  "query": "Cos'è DocN?",
  "response": "DocN è un sistema di gestione documenti con capacità RAG per ricerca semantica e chat con documenti.",
  "sourceDocumentIds": ["1", "2"]
}
```

4. Clicca "Execute"

**Risposta Esempio**:
```json
{
  "overallConfidenceScore": 0.85,
  "hasLowConfidenceWarnings": false,
  "lowConfidenceStatements": [],
  "hallucinationDetection": {
    "hasPotentialHallucinations": false,
    "hallucinations": [],
    "hallucinationScore": 0.0
  },
  "citationVerification": {
    "totalCitations": 2,
    "verifiedCitations": 2,
    "unverifiedCitations": 0
  },
  "qualityWarnings": [],
  "statementConfidenceScores": {
    "DocN è un sistema di gestione documenti": 0.92,
    "con capacità RAG per ricerca semantica": 0.88,
    "e chat con documenti": 0.85
  }
}
```

**Come Leggere la Risposta**:
- `overallConfidenceScore: 0.85` = 85% di confidenza (BUONO ✅)
- `hasPotentialHallucinations: false` = Nessuna allucinazione (OTTIMO ✨)
- `verifiedCitations: 2` = 2 citazioni verificate

---

### 2. Rilevare Allucinazioni

**Endpoint**: `POST /api/rag-quality/hallucinations`

**Cosa Fa**: Controlla se la risposta contiene informazioni inventate

**Come Testare in Swagger**:
1. Espandi "POST /api/rag-quality/hallucinations"
2. Clicca "Try it out"
3. Inserisci questo JSON:

```json
{
  "response": "DocN è stato creato nel 2020 e ha 1 milione di utenti attivi.",
  "sourceTexts": [
    "DocN è un sistema di gestione documenti",
    "DocN supporta ricerca semantica e RAG"
  ]
}
```

4. Clicca "Execute"

**Risposta Esempio**:
```json
{
  "hasPotentialHallucinations": true,
  "hallucinations": [
    {
      "text": "è stato creato nel 2020",
      "confidence": 0.15,
      "reason": "No supporting evidence found in source documents"
    },
    {
      "text": "ha 1 milione di utenti attivi",
      "confidence": 0.10,
      "reason": "No supporting evidence found in source documents"
    }
  ],
  "hallucinationScore": 0.87
}
```

**Come Leggere la Risposta**:
- `hasPotentialHallucinations: true` = CI SONO allucinazioni ⚠️
- `hallucinations` = Lista delle frasi inventate
- `confidence: 0.15` = Solo 15% di sicurezza (BASSO = probabile invenzione)
- `hallucinationScore: 0.87` = 87% di probabilità che sia un'allucinazione

---

### 3. Ottieni Metriche di Qualità

**Endpoint**: `GET /api/rag-quality/metrics`

**Cosa Fa**: Mostra statistiche generali sulla qualità delle risposte

**Come Testare in Swagger**:
1. Espandi "GET /api/rag-quality/metrics"
2. Clicca "Try it out"
3. (Opzionale) Inserisci date:
   - `from`: 2026-01-01T00:00:00Z
   - `to`: 2026-01-31T23:59:59Z
4. Clicca "Execute"

**Risposta Esempio**:
```json
{
  "totalResponses": 1523,
  "averageConfidenceScore": 0.83,
  "lowConfidenceResponses": 45,
  "hallucinationsDetected": 12,
  "citationVerificationRate": 0.96,
  "discrepanciesByType": {
    "QualityWarning": 45,
    "Hallucination": 12
  },
  "topWarnings": [
    "Low confidence responses detected",
    "Citations not verified"
  ]
}
```

**Come Leggere la Risposta**:
- `totalResponses: 1523` = 1523 risposte generate
- `averageConfidenceScore: 0.83` = 83% di confidenza media (BUONO ✅)
- `hallucinationsDetected: 12` = 12 allucinazioni trovate (su 1523 = 0.8% = OTTIMO ✨)
- `citationVerificationRate: 0.96` = 96% citazioni verificate (ECCELLENTE ✨)

---

### 4. Cruscotto Combinato

**Endpoint**: `GET /api/rag-quality/dashboard`

**Cosa Fa**: Mostra tutti i dati insieme (metriche qualità + RAGAS)

**Come Testare in Swagger**:
1. Espandi "GET /api/rag-quality/dashboard"
2. Clicca "Try it out"
3. Clicca "Execute"

**Risposta Esempio**:
```json
{
  "quality": {
    "totalResponses": 1523,
    "averageConfidenceScore": 0.83,
    "lowConfidenceResponses": 45,
    "hallucinationsDetected": 12,
    "citationVerificationRate": 0.96
  },
  "ragas": {
    "totalEvaluations": 1523,
    "averageScores": {
      "faithfulnessScore": 0.85,
      "answerRelevancyScore": 0.82,
      "contextPrecisionScore": 0.79,
      "contextRecallScore": 0.81,
      "overallRAGASScore": 0.82
    },
    "qualityTrend": 0.05
  },
  "timestamp": "2026-01-03T20:30:00Z"
}
```

**Come Leggere la Risposta**:

**Sezione Quality**:
- Stesse info del endpoint `/metrics`

**Sezione RAGAS**:
- `faithfulnessScore: 0.85` = 85% fedeltà ai documenti (BUONO ✅)
- `answerRelevancyScore: 0.82` = 82% rilevanza (BUONO ✅)
- `contextPrecisionScore: 0.79` = 79% precisione (BUONO ✅)
- `contextRecallScore: 0.81` = 81% completezza (BUONO ✅)
- `overallRAGASScore: 0.82` = 82% punteggio complessivo (BUONO ✅)
- `qualityTrend: 0.05` = +5% miglioramento (OTTIMO ✨)

---

## 🌐 Metodo 2: Browser (Semplice per GET)

Per endpoint GET (che non richiedono dati in input):

**Passo 1**: Avvia il server

**Passo 2**: Apri il browser e vai a:

```
https://localhost:5211/api/rag-quality/metrics
```

o

```
https://localhost:5211/api/rag-quality/dashboard
```

**Passo 3**: Vedi il JSON direttamente nel browser

---

## 💻 Metodo 3: cURL (Linea di Comando)

### Esempio 1: Verifica Qualità

```bash
curl -X POST "https://localhost:5211/api/rag-quality/verify" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Come funziona DocN?",
    "response": "DocN permette di caricare documenti e fare ricerche semantiche.",
    "sourceDocumentIds": ["1", "2"]
  }' \
  -k
```

### Esempio 2: Rilevare Allucinazioni

```bash
curl -X POST "https://localhost:5211/api/rag-quality/hallucinations" \
  -H "Content-Type: application/json" \
  -d '{
    "response": "DocN ha 5 milioni di utenti nel mondo.",
    "sourceTexts": [
      "DocN è un sistema di gestione documenti",
      "DocN supporta RAG e ricerca semantica"
    ]
  }' \
  -k
```

### Esempio 3: Metriche

```bash
curl "https://localhost:5211/api/rag-quality/metrics" -k
```

### Esempio 4: Dashboard

```bash
curl "https://localhost:5211/api/rag-quality/dashboard" -k
```

**Nota**: `-k` serve per ignorare il certificato SSL in sviluppo

---

## 🎨 Metodo 4: Pagina UI (Più Visuale)

**La pagina UI fa tutto automaticamente!**

### Vedere i Risultati nella UI

**Passo 1**: Avvia il server

**Passo 2**: Vai a `/monitoring/rag-quality` nel browser

**Passo 3**: La pagina chiama automaticamente:
- `/api/rag-quality/dashboard` quando carichi la pagina
- Mostra tutti i dati in forma grafica

**Vantaggi**:
- ✅ Non devi scrivere JSON
- ✅ Vedi grafici colorati
- ✅ Progress bar visive
- ✅ Colori che indicano se è buono/cattivo
- ✅ Pulsante aggiorna per ricaricare

---

## 📊 Come Interpretare i Risultati

### Confidence Score
- **0.90 - 1.00** = Eccellente ✨ (verde scuro)
- **0.80 - 0.90** = Ottimo ✅ (verde)
- **0.70 - 0.80** = Buono 👍 (blu)
- **0.60 - 0.70** = Accettabile ⚠️ (giallo)
- **< 0.60** = Problema 🔴 (rosso)

### Hallucination Score
- **< 0.30** = Probabilmente VERO ✅
- **0.30 - 0.60** = INCERTO ⚠️
- **> 0.60** = Probabilmente FALSO 🔴

### Citation Verification Rate
- **> 0.95** = Eccellente ✨
- **0.85 - 0.95** = Buono ✅
- **0.70 - 0.85** = Accettabile ⚠️
- **< 0.70** = Problema 🔴

### RAGAS Scores
- **> 0.80** = Eccellente ✨
- **0.70 - 0.80** = Buono ✅
- **0.60 - 0.70** = Accettabile ⚠️
- **< 0.60** = Problema 🔴

---

## 🔍 Esempi Pratici

### Scenario 1: Testare una Risposta Specifica

**Situazione**: Hai una risposta dalla Chat AI e vuoi sapere se è affidabile

**Endpoint da usare**: `/api/rag-quality/verify`

**Input**:
```json
{
  "query": "Quali documenti supporta DocN?",
  "response": "DocN supporta PDF, Word, Excel, PowerPoint e immagini con OCR.",
  "sourceDocumentIds": ["doc1", "doc2"]
}
```

**Output**:
```json
{
  "overallConfidenceScore": 0.92,
  "hasLowConfidenceWarnings": false
}
```

**Interpretazione**: 92% = OTTIMO ✅, puoi fidarti della risposta!

---

### Scenario 2: Controllare se l'AI Sta Inventando

**Situazione**: Pensi che l'AI stia dicendo cose non vere

**Endpoint da usare**: `/api/rag-quality/hallucinations`

**Input**:
```json
{
  "response": "DocN è usato da Google, Microsoft e Apple.",
  "sourceTexts": ["DocN è un sistema di gestione documenti"]
}
```

**Output**:
```json
{
  "hasPotentialHallucinations": true,
  "hallucinations": [
    {
      "text": "è usato da Google, Microsoft e Apple",
      "confidence": 0.05,
      "reason": "No supporting evidence"
    }
  ],
  "hallucinationScore": 0.95
}
```

**Interpretazione**: 95% probabilità di allucinazione = INVENTATO 🔴

---

### Scenario 3: Monitoraggio Generale

**Situazione**: Vuoi vedere come sta andando il sistema in generale

**Endpoint da usare**: `/api/rag-quality/dashboard`

**Output**:
```json
{
  "quality": {
    "averageConfidenceScore": 0.83,
    "hallucinationsDetected": 5
  },
  "ragas": {
    "averageScores": {
      "overallRAGASScore": 0.82
    },
    "qualityTrend": 0.03
  }
}
```

**Interpretazione**: 
- 83% confidence = BUONO ✅
- 5 allucinazioni = Poche, OK ✅
- 82% RAGAS = BUONO ✅
- +3% trend = Migliorando 📈

---

## ❓ Domande Frequenti

### "Come faccio a provare gli endpoint se non ho dati?"

Usa la pagina `/monitoring/alerts` per generare alert di esempio, poi vai su `/monitoring/rag-quality` per vedere i dati.

### "Swagger mi dice 'Failed to fetch'"

1. Assicurati che il server sia avviato
2. Controlla che l'URL sia corretto (https://localhost:5211)
3. Il browser potrebbe bloccare per il certificato SSL - accetta il warning

### "Gli endpoint restituiscono dati vuoti"

Normale se non hai ancora usato la Chat AI. Gli endpoint mostrano dati reali solo dopo aver generato risposte.

### "Qual è il modo più semplice per testare?"

1. **Per vedere**: Usa la pagina UI `/monitoring/rag-quality`
2. **Per testare**: Usa Swagger
3. **Per automatizzare**: Usa cURL

---

## 📚 Link Utili

- **Swagger UI**: https://localhost:5211/swagger
- **Pagina Alerts**: https://localhost:5211/monitoring/alerts
- **Pagina RAG Quality**: https://localhost:5211/monitoring/rag-quality
- **Documentazione API Completa**: `docs/MONITORING_API_REFERENCE.md`

---

**Versione**: 1.0  
**Ultimo Aggiornamento**: Gennaio 2026
