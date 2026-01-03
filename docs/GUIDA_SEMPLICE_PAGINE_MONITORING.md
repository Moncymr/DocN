# Guida Semplice: Pagine di Monitoring

## 📋 Panoramica

DocN ora include due nuove pagine per monitorare il sistema:
1. **Alert System** - Gestione degli alert di sistema
2. **Qualità RAG** - Monitoraggio della qualità delle risposte AI

---

## 🚨 Pagina Alert System

**Percorso**: `/monitoring/alerts`  
**Menu**: 🚨 Alert System (sotto "Diagnostica AI")

### Cosa Fa Questa Pagina?

Mostra gli alert del sistema quando qualcosa non va bene. Gli alert ti avvisano quando:
- CPU troppo alta (>90%)
- Memoria troppo piena (>90%)
- API troppo lente (>2 secondi)
- Troppi errori (>5%)

### Come Si Usa?

#### 1. Vedere le Statistiche
Nella parte alta vedi 4 card:
- **Alert Totali** - Quanti alert ci sono stati in totale
- **Alert Attivi** - Quanti alert sono attivi ora (🔴 rosso)
- **Riconosciuti** - Quanti alert hai visto (👁️ giallo)
- **Risolti** - Quanti problemi hai risolto (✅ verde)

#### 2. Gestire Alert Attivi
Ogni alert mostra:
- **Nome** - Es: "HighCPU", "HighLatency"
- **Severità** - 🔴 Critical, ⚠️ Warning, ℹ️ Info
- **Descrizione** - Cosa è successo
- **Quando** - Ora dell'alert

**Azioni disponibili**:
- **👁️ Riconosci** - Dici "ho visto questo alert"
- **✅ Risolvi** - Dici "ho risolto il problema"

#### 3. Provare la Funzionalità

**Opzione A - Genera Esempi**:
1. Clicca "📊 Genera Alert di Esempio (5)"
2. Vengono creati 5 alert diversi per vedere come funzionano
3. Prova a riconoscere e risolvere gli alert

**Opzione B - Test Singolo**:
1. Clicca "📤 Invia Test Alert"
2. Viene creato 1 alert di test

### Esempi di Alert

Quando clicchi "Genera Alert di Esempio" vedi:

1. **🔴 HighCPU** (Critico)
   - "CPU usage è al 92% da 5 minuti"
   - Devi controllare quali processi usano troppa CPU

2. **⚠️ HighLatency** (Warning)
   - "Latenza API /api/search è 2.5s (P95)"
   - L'API di ricerca è lenta

3. **⚠️ LowRAGQuality** (Warning)
   - "Confidence score RAG è sceso a 0.65"
   - Le risposte AI sono meno sicure

4. **⚠️ HallucinationsDetected** (Warning)
   - "Rilevate 3 potenziali allucinazioni"
   - L'AI sta inventando informazioni

5. **ℹ️ DatabaseConnectionSlow** (Info)
   - "Connessioni database > 500ms"
   - Il database risponde lentamente

---

## ✅ Pagina Qualità RAG

**Percorso**: `/monitoring/rag-quality`  
**Menu**: ✅ Qualità RAG (sotto "Diagnostica AI")

### Cosa Fa Questa Pagina?

Controlla quanto sono buone le risposte che l'AI genera quando legge i tuoi documenti. Ti dice se l'AI:
- Dice la verità (Faithfulness)
- Risponde alla domanda (Relevancy)
- Usa le informazioni giuste (Precision)
- Trova tutte le informazioni (Recall)

### Come Si Usa?

#### 1. Metriche Qualità (Prima Sezione)

**5 Indicatori Principali**:

1. **🎯 Confidence Score Medio**
   - Da 0% a 100%
   - Verde (>80%) = Ottimo
   - Giallo (60-80%) = Accettabile
   - Rosso (<60%) = Problema
   
2. **📝 Risposte Totali**
   - Quante risposte AI sono state generate

3. **⚠️ Risposte Bassa Confidenza**
   - Quante risposte hanno confidence < 60%
   - Se questo numero è alto, l'AI non è sicura

4. **🔴 Allucinazioni Rilevate**
   - Quante volte l'AI ha inventato informazioni
   - Deve essere 0 o molto basso

5. **✅ Citazioni Verificate**
   - % di citazioni corrette
   - Deve essere vicino a 100%

#### 2. RAGAS Metrics (Seconda Sezione)

**4 Punteggi da 0.00 a 1.00**:

1. **Faithfulness** (Fedeltà)
   - La risposta è basata sui documenti?
   - Target: >0.75
   - Esempio: Se il documento dice "X" e l'AI risponde "X" = buono

2. **Answer Relevancy** (Rilevanza)
   - La risposta è pertinente alla domanda?
   - Target: >0.75
   - Esempio: Chiedi "Come?" e l'AI risponde "Come..." = buono

3. **Context Precision** (Precisione)
   - L'AI ha trovato i documenti giusti?
   - Target: >0.70
   - Esempio: 3 documenti utili su 5 trovati = 60% (migliorabile)

4. **Context Recall** (Completezza)
   - L'AI ha trovato tutti i documenti rilevanti?
   - Target: >0.70
   - Esempio: Trovati 4 documenti su 5 necessari = 80% (buono)

**Punteggio Complessivo**:
- Grande numero al centro
- Verde (>0.80) = Eccellente ✨
- Blu (0.70-0.80) = Buono 👍
- Giallo (0.60-0.70) = Da migliorare ⚠️
- Rosso (<0.60) = Critico 🔴

#### 3. Trend Qualità

Se vedi:
- **📈 Trend positivo: +5%** = La qualità sta migliorando
- **📉 Trend negativo: -5%** = La qualità sta peggiorando

#### 4. Alert Qualità

Se ci sono problemi, vedi avvisi come:
- "Faithfulness: 0.68 (soglia: 0.75)" = Le risposte non sono abbastanza basate sui documenti
- "Hallucination rate: 15%" = L'AI sta inventando troppo

### Cosa Fare con Questi Dati?

**Se tutto è verde/blu**:
- ✅ Sistema funziona bene
- Continua a monitorare

**Se vedi giallo/rosso**:
- ⚠️ Controlla i documenti caricati
- ⚠️ Verifica che i documenti siano di buona qualità
- ⚠️ Controlla la configurazione AI

**Se vedi allucinazioni**:
- 🔴 L'AI sta inventando informazioni
- Controlla che i documenti abbiano le informazioni necessarie
- Forse serve più contesto nei documenti

---

## 🎯 Quando Usare Queste Pagine?

### Alert System
**Usa quando**:
- Vuoi sapere se il sistema funziona bene
- Hai notato che qualcosa è lento
- Vuoi vedere se ci sono problemi
- Vuoi testare gli alert

**Controllo consigliato**: 1 volta al giorno o quando sospetti problemi

### Qualità RAG
**Usa quando**:
- Hai caricato nuovi documenti
- Gli utenti dicono che le risposte non sono buone
- Vuoi verificare la qualità delle risposte AI
- Hai cambiato configurazione AI

**Controllo consigliato**: 1 volta a settimana o dopo modifiche importanti

---

## 🚀 Quick Start

### Prima Volta - Test Alert System

1. Vai a `/monitoring/alerts`
2. Clicca "📊 Genera Alert di Esempio (5)"
3. Vedi gli alert apparire
4. Prova a cliccare "👁️ Riconosci" su un alert
5. Prova a cliccare "✅ Risolvi" su un alert
6. Clicca "🔄 Aggiorna" per vedere i cambiamenti

### Prima Volta - Test Qualità RAG

1. Vai a `/monitoring/rag-quality`
2. Se non ci sono dati, usa prima la Chat AI per generare risposte
3. Torna sulla pagina e clicca "🔄 Aggiorna Dashboard"
4. Vedi i punteggi di qualità
5. Se i punteggi sono bassi, controlla i documenti

---

## ❓ Domande Frequenti

### "Non vedo alert attivi"
- ✅ Buono! Significa che tutto funziona bene
- Se vuoi testare: clicca "Genera Alert di Esempio"

### "La pagina RAG Quality mostra errore"
- Assicurati che il server sia avviato
- Verifica che hai usato la Chat AI almeno una volta
- Prova a ricaricare la pagina

### "I punteggi RAGAS sono bassi"
- Controlla la qualità dei documenti caricati
- Verifica che i documenti contengano informazioni complete
- Aggiungi più documenti rilevanti

### "Come faccio sparire un alert?"
1. Clicca "👁️ Riconosci" per dire che lo hai visto
2. Risolvi il problema (es: riavvia, ottimizza, ecc.)
3. Clicca "✅ Risolvi" quando hai finito

### "Posso cancellare gli alert di esempio?"
Sì, usa il pulsante "✅ Risolvi" su tutti gli alert. Gli alert di esempio servono solo per capire come funziona il sistema.

---

## 📚 Documentazione Avanzata

Per informazioni più dettagliate:
- **Alert**: Vedi `docs/ALERTING_RUNBOOK.md`
- **RAG Quality**: Vedi `docs/RAG_QUALITY_GUIDE.md`
- **API**: Vedi `docs/MONITORING_API_REFERENCE.md`
- **Setup**: Vedi `docs/MONITORING_INTEGRATION_GUIDE.md`

---

**Versione**: 1.0  
**Ultimo Aggiornamento**: Gennaio 2026
