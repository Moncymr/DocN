# Manuale Utente - Sistema Connettori e Ingestione Automatica

**DocN - Sistema di Gestione Documentale**  
**Versione**: 1.0  
**Data**: 15 Gennaio 2026

---

## Indice
1. [Introduzione](#introduzione)
2. [Cos'è il Sistema Connettori](#cosè-il-sistema-connettori)
3. [Accesso alle Funzionalità](#accesso-alle-funzionalità)
4. [Gestione Connettori](#gestione-connettori)
5. [Gestione Pianificazioni](#gestione-pianificazioni)
6. [Esempi Pratici](#esempi-pratici)
7. [Domande Frequenti](#domande-frequenti)

---

## 1. Introduzione

### Cosa Offre Questa Nuova Funzionalità

Il sistema di **Connettori e Ingestione Automatica** permette di importare automaticamente documenti da fonti esterne senza doverli caricare manualmente uno per uno. 

**Vantaggi principali:**
- ⏰ **Risparmio di tempo**: Importazione automatica invece di upload manuale
- 🔄 **Sempre aggiornato**: Sincronizzazione continua o programmata
- 📁 **Fonti multiple**: Supporta cartelle locali, SharePoint, OneDrive, Google Drive
- 🤖 **Analisi AI automatica**: I documenti vengono analizzati automaticamente
- 📊 **Tracciamento completo**: Log dettagliati di tutte le operazioni

---

## 2. Cos'è il Sistema Connettori

### Componenti Principali

Il sistema è composto da due elementi:

#### 2.1 Connettori
Un **connettore** è una configurazione che indica a DocN dove trovare i documenti da importare.

**Tipi di connettori supportati:**
- 📂 **Cartella Locale**: Cartelle sul computer o server
- 📊 **SharePoint**: Librerie documenti SharePoint
- ☁️ **OneDrive**: Cartelle OneDrive personali o aziendali
- 🌐 **Google Drive**: Cartelle Google Drive
- 🔌 **FTP/SFTP**: Server FTP per trasferimento file

#### 2.2 Pianificazioni di Ingestione
Una **pianificazione** definisce quando e come importare i documenti da un connettore.

**Modalità disponibili:**
- ✋ **Manuale**: Esegui quando vuoi tramite pulsante
- ⏰ **Programmata**: Esegui automaticamente a orari specifici (es. ogni notte alle 2:00)
- 🔄 **Continua**: Controlla continuamente nuovi file (es. ogni 30 minuti)

---

## 3. Accesso alle Funzionalità

### Menu di Navigazione

Nel menu laterale di DocN troverai due nuove voci:

```
📁 Documenti
🔍 Ricerca
💬 Chat AI
📤 Carica
📤📤 Carica Multiplo
🔌 Connettori          ← NUOVO
📅 Pianificazione      ← NUOVO
```

### 3.1 Pagina Connettori

**URL**: `https://tuoserver/connectors`

**Descrizione schermata:**

```
┌─────────────────────────────────────────────────────────────┐
│  Connettori Documenti                    [+ Nuovo Connettore]│
│  Gestisci connessioni ai repository esterni                  │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 📂 Cartella Progetti 2024           [✓ Attivo]       │  │
│  │                                                        │  │
│  │ Tipo: LocalFolder                                     │  │
│  │ Path: C:\Progetti\2024                               │  │
│  │ Ultima connessione: 15/01/2026 08:00 - OK           │  │
│  │                                                        │  │
│  │ [🔍 Test]  [✏️ Modifica]  [🗑️ Elimina]              │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 📊 SharePoint Aziendale             [✓ Attivo]       │  │
│  │                                                        │  │
│  │ Tipo: SharePoint                                      │  │
│  │ URL: https://azienda.sharepoint.com                  │  │
│  │ Ultima sincronizzazione: 14/01/2026 23:45           │  │
│  │                                                        │  │
│  │ [🔍 Test]  [✏️ Modifica]  [🗑️ Elimina]              │  │
│  └──────────────────────────────────────────────────────┘  │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

**Elementi della schermata:**
- **Titolo**: "Connettori Documenti" con sottotitolo esplicativo
- **Pulsante "+ Nuovo Connettore"**: In alto a destra (arancione)
- **Card connettori**: Una card per ogni connettore con:
  - Nome del connettore e badge stato (Attivo/Inattivo)
  - Icona che rappresenta il tipo
  - Informazioni principali (tipo, percorso/URL)
  - Stato ultima connessione o sincronizzazione
  - Pulsanti azione: Test, Modifica, Elimina

**Colori:**
- Sfondo: Gradiente blu chiaro
- Card: Bianco con ombra
- Badge "Attivo": Verde con segno di spunta
- Badge "Inattivo": Grigio
- Pulsanti azione: Grigio chiaro che diventa più scuro al passaggio del mouse

### 3.2 Pagina Pianificazione

**URL**: `https://tuoserver/ingestion`

**Descrizione schermata:**

```
┌─────────────────────────────────────────────────────────────┐
│  Pianificazione Ingestione            [+ Nuova Pianificazione]│
│  Gestisci l'importazione automatica dei documenti            │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 📅 Import Notturno Progetti         [✓ Abilitato]    │  │
│  │                                                        │  │
│  │ Tipo: Scheduled                                       │  │
│  │ Cron: 0 2 * * * (Ogni giorno alle 02:00)            │  │
│  │ Prossima esecuzione: 16/01/2026 02:00               │  │
│  │ Ultima esecuzione: 15/01/2026 02:00 (145 docs)      │  │
│  │                                                        │  │
│  │ [▶️ Esegui Ora] [📋 Log] [✏️ Modifica] [🗑️ Elimina]│  │
│  └──────────────────────────────────────────────────────┘  │
│                                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ 🔄 Sync Continuo SharePoint         [✓ Abilitato]    │  │
│  │                                                        │  │
│  │ Tipo: Continuous                                      │  │
│  │ Intervallo: Ogni 30 minuti                           │  │
│  │ Prossima esecuzione: 15/01/2026 08:30               │  │
│  │ Ultima esecuzione: 15/01/2026 08:00 (23 docs)       │  │
│  │                                                        │  │
│  │ [▶️ Esegui Ora] [📋 Log] [✏️ Modifica] [🗑️ Elimina]│  │
│  └──────────────────────────────────────────────────────┘  │
│                                                               │
│  ┌──────────────────────────────────────────────────────┐  │
│  │ ✋ Import Manuale Archivio           [○ Disabilitato] │  │
│  │                                                        │  │
│  │ Tipo: Manual                                          │  │
│  │ Stato: Pronto per esecuzione manuale                 │  │
│  │ Ultima esecuzione: 10/01/2026 15:30 (5 docs)        │  │
│  │                                                        │  │
│  │ [▶️ Esegui Ora] [📋 Log] [✏️ Modifica] [🗑️ Elimina]│  │
│  └──────────────────────────────────────────────────────┘  │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

**Elementi della schermata:**
- **Titolo**: "Pianificazione Ingestione" con sottotitolo
- **Pulsante "+ Nuova Pianificazione"**: In alto a destra (arancione)
- **Card pianificazioni**: Una card per ogni pianificazione con:
  - Nome e badge stato (Abilitato/Disabilitato)
  - Tipo di pianificazione con icona
  - Dettagli specifici (cron expression, intervallo)
  - Informazioni su prossima e ultima esecuzione
  - Numero documenti elaborati nell'ultima esecuzione
  - Pulsanti azione: Esegui Ora, Log, Modifica, Elimina

**Badge Stato:**
- "✓ Abilitato": Verde con segno di spunta
- "○ Disabilitato": Grigio

**Pulsanti Azione:**
- **▶️ Esegui Ora**: Avvia immediatamente l'importazione
- **📋 Log**: Visualizza lo storico delle esecuzioni
- **✏️ Modifica**: Modifica configurazione (mostra alert con istruzioni API)
- **🗑️ Elimina**: Elimina la pianificazione

---

## 4. Gestione Connettori

### 4.1 Creare un Nuovo Connettore

**NOTA IMPORTANTE**: Attualmente la creazione di connettori è disponibile tramite API. L'interfaccia UI è in fase di sviluppo.

**Passi per creare un connettore tramite API:**

#### Esempio: Connettore Cartella Locale

1. **Preparare i dati JSON:**

```json
{
  "name": "Documenti Lavoro 2024",
  "connectorType": "LocalFolder",
  "configuration": "{\"folderPath\":\"C:\\\\Documenti\\\\Lavoro\\\\2024\",\"recursive\":true,\"filePattern\":\"*.pdf,*.docx,*.xlsx\"}",
  "isActive": true,
  "description": "Cartella principale documenti di lavoro"
}
```

2. **Inviare richiesta API:**

```
POST https://tuoserver/Connectors
Content-Type: application/json
Authorization: Bearer [token]

[JSON sopra]
```

3. **Risultato:**
Il connettore apparirà nella pagina Connettori con stato "Attivo".

### 4.2 Testare un Connettore

1. Nella pagina **Connettori**, individua il connettore da testare
2. Clicca sul pulsante **🔍 Test**
3. Il sistema verificherà:
   - ✅ Connessione al repository
   - ✅ Permessi di accesso
   - ✅ Validità configurazione
4. Vedrai un messaggio di successo o errore

**Messaggi possibili:**
- ✅ "Connessione riuscita - 45 file trovati"
- ❌ "Errore: Percorso non accessibile"
- ❌ "Errore: Credenziali non valide"

### 4.3 Modificare un Connettore

**NOTA**: Modifica tramite API (UI in sviluppo)

```
PUT https://tuoserver/Connectors/{id}
Content-Type: application/json
Authorization: Bearer [token]

{
  "name": "Documenti Lavoro 2024 - Aggiornato",
  "connectorType": "LocalFolder",
  "configuration": "{\"folderPath\":\"C:\\\\Documenti\\\\Lavoro\\\\2024\",\"recursive\":true,\"filePattern\":\"*.*\"}",
  "isActive": true
}
```

### 4.4 Eliminare un Connettore

1. Nella pagina **Connettori**, trova il connettore da eliminare
2. Clicca sul pulsante **🗑️ Elimina**
3. Conferma l'operazione

**ATTENZIONE**: Eliminando un connettore, tutte le pianificazioni associate verranno disabilitate.

---

## 5. Gestione Pianificazioni

### 5.1 Creare una Nuova Pianificazione

**NOTA**: Creazione tramite API (UI in sviluppo)

#### Esempio 1: Pianificazione Notturna (ogni notte alle 2:00)

```json
{
  "connectorId": 1,
  "name": "Import Notturno Documenti",
  "scheduleType": "Scheduled",
  "cronExpression": "0 2 * * *",
  "isEnabled": true,
  "defaultCategory": "Documenti Importati",
  "defaultVisibility": 0,
  "enableAIAnalysis": true,
  "generateEmbeddingsImmediately": false,
  "description": "Importazione automatica documenti ogni notte"
}
```

**API:**
```
POST https://tuoserver/Ingestion/schedules
Content-Type: application/json
Authorization: Bearer [token]

[JSON sopra]
```

#### Esempio 2: Pianificazione Continua (ogni 30 minuti)

```json
{
  "connectorId": 1,
  "name": "Sync Continuo",
  "scheduleType": "Continuous",
  "intervalMinutes": 30,
  "isEnabled": true,
  "defaultCategory": "Auto-Import",
  "enableAIAnalysis": true
}
```

#### Esempio 3: Pianificazione Manuale

```json
{
  "connectorId": 1,
  "name": "Import Manuale On-Demand",
  "scheduleType": "Manual",
  "isEnabled": true,
  "defaultCategory": "Import Manuali",
  "enableAIAnalysis": true
}
```

### 5.2 Eseguire Manualmente una Pianificazione

1. Vai alla pagina **Pianificazione**
2. Trova la pianificazione da eseguire
3. Clicca sul pulsante **▶️ Esegui Ora**
4. L'importazione parte immediatamente
5. Il contatore documenti si aggiornerà al completamento

**Cosa succede:**
- Il sistema si connette al repository
- Elenca tutti i file disponibili
- Importa i nuovi documenti o quelli modificati
- Analizza automaticamente i documenti con AI (se abilitato)
- Genera embeddings per la ricerca semantica (se abilitato)
- Registra l'operazione nei log

### 5.3 Visualizzare i Log

1. Nella pagina **Pianificazione**, trova la pianificazione
2. Clicca sul pulsante **📋 Log**
3. Verrà mostrato un alert con istruzioni per accedere ai log tramite API

**Informazioni nei log:**
- Data e ora esecuzione
- Stato: Completato, Fallito, In Corso
- Numero documenti trovati
- Numero documenti importati
- Numero documenti saltati (già presenti)
- Numero documenti falliti
- Dettagli errori (se presenti)
- Durata esecuzione

**Esempio visualizzazione log tramite API:**
```
GET https://tuoserver/Ingestion/schedules/1/logs?count=50
Authorization: Bearer [token]
```

**Risposta:**
```json
[
  {
    "id": 123,
    "startedAt": "2026-01-15T02:00:00Z",
    "completedAt": "2026-01-15T02:15:32Z",
    "status": "Completed",
    "documentsDiscovered": 150,
    "documentsProcessed": 145,
    "documentsSkipped": 5,
    "documentsFailed": 0,
    "durationSeconds": 932
  }
]
```

### 5.4 Disabilitare/Abilitare una Pianificazione

**Tramite API:**

```
PUT https://tuoserver/Ingestion/schedules/{id}
Content-Type: application/json
Authorization: Bearer [token]

{
  "isEnabled": false
}
```

Quando disabilitata:
- ❌ Non verrà eseguita automaticamente
- ✅ Può ancora essere eseguita manualmente con "Esegui Ora"

---

## 6. Esempi Pratici

### Scenario 1: Importare Documenti da Cartella di Rete

**Obiettivo**: Importare automaticamente ogni notte i nuovi PDF dalla cartella progetti

**Passi:**

1. **Creare Connettore** (via API):
```json
{
  "name": "Cartella Progetti Rete",
  "connectorType": "LocalFolder",
  "configuration": "{\"folderPath\":\"\\\\\\\\server\\\\progetti\\\\2024\",\"recursive\":true,\"filePattern\":\"*.pdf\"}",
  "isActive": true
}
```

2. **Creare Pianificazione** (via API):
```json
{
  "connectorId": 1,
  "name": "Import Notturno Progetti",
  "scheduleType": "Scheduled",
  "cronExpression": "0 2 * * *",
  "isEnabled": true,
  "defaultCategory": "Progetti",
  "enableAIAnalysis": true
}
```

3. **Risultato**:
   - Ogni notte alle 2:00 AM
   - Sistema importa nuovi PDF
   - Documenti categorizzati automaticamente come "Progetti"
   - AI analizza contenuto e genera embeddings

### Scenario 2: Sincronizzazione Continua SharePoint

**Obiettivo**: Mantenere sempre sincronizzati i documenti da SharePoint

**Passi:**

1. **Creare Connettore SharePoint** (via API):
```json
{
  "name": "SharePoint Documenti",
  "connectorType": "SharePoint",
  "configuration": "{\"siteUrl\":\"https://azienda.sharepoint.com\",\"folderPath\":\"/Shared Documents\"}",
  "encryptedCredentials": "{\"clientSecret\":\"xxx\"}",
  "isActive": true
}
```

2. **Creare Pianificazione Continua** (via API):
```json
{
  "connectorId": 2,
  "name": "Sync SharePoint",
  "scheduleType": "Continuous",
  "intervalMinutes": 15,
  "isEnabled": true,
  "defaultCategory": "SharePoint",
  "enableAIAnalysis": true
}
```

3. **Risultato**:
   - Ogni 15 minuti controlla nuovi file
   - Importazione automatica immediata
   - Sempre aggiornato in tempo quasi reale

### Scenario 3: Import Manuale per Archivi Storici

**Obiettivo**: Importare documenti vecchi solo quando necessario

**Passi:**

1. **Creare Connettore Archivio**
2. **Creare Pianificazione Manuale**
3. **Quando serve**, vai su Pianificazione → **▶️ Esegui Ora**
4. Attendi completamento (vedrai aggiornamento contatore)

---

## 7. Domande Frequenti

### Q: Posso importare documenti da più cartelle contemporaneamente?
**R**: Sì, crea un connettore per ogni cartella e associa pianificazioni diverse o usa `recursive: true` per includere sottocartelle.

### Q: I documenti importati vengono duplicati?
**R**: No, il sistema controlla se il documento è già presente e salta l'importazione se non è cambiato.

### Q: Posso filtrare per tipo di file?
**R**: Sì, nella configurazione del connettore usa `filePattern`: `"*.pdf,*.docx,*.xlsx"` per importare solo certi tipi.

### Q: Cosa significa "generateEmbeddingsImmediately"?
**R**: Se `true`, gli embeddings per la ricerca semantica vengono generati durante l'importazione (più lento ma documenti subito ricercabili). Se `false`, vengono generati in background dopo (più veloce ma documenti ricercabili dopo).

### Q: Come funzionano le espressioni cron?
**R**: Formato: `minuto ora giorno mese giorno_settimana`

Esempi:
- `0 2 * * *` = Ogni giorno alle 2:00
- `0 */6 * * *` = Ogni 6 ore
- `0 9 * * 1-5` = Alle 9:00 dal lunedì al venerdì
- `*/30 * * * *` = Ogni 30 minuti

### Q: Posso modificare una pianificazione dalla UI?
**R**: Attualmente no, usa l'API. L'interfaccia UI completa è in sviluppo.

### Q: I log vengono conservati?
**R**: Sì, tutti i log di esecuzione sono permanenti nel database e consultabili via API.

### Q: Cosa succede se l'importazione fallisce?
**R**: Il log registra l'errore dettagliato. La pianificazione continuerà a tentare nelle esecuzioni successive.

### Q: Posso importare da Google Drive personale?
**R**: Sì, ma richiede configurazione OAuth. Contatta l'amministratore di sistema.

### Q: Quanto tempo richiede un'importazione?
**R**: Dipende dal numero di file e dalla dimensione. In media:
- 100 documenti piccoli: 2-5 minuti
- 1000 documenti: 20-30 minuti
- Con analisi AI: aggiungere 30-50%

### Q: Posso sospendere temporaneamente una pianificazione?
**R**: Sì, disabilitala tramite API (`isEnabled: false`). Puoi sempre riattivarla dopo.

### Q: I documenti importati mantengono i metadati originali?
**R**: Dipende dal tipo di connettore. I metadati base (data modifica, dimensione) sono sempre preservati.

---

## Riepilogo

### Funzionalità Chiave

✅ **Importazione Automatica**: Niente più upload manuali  
✅ **Fonti Multiple**: Cartelle locali, SharePoint, OneDrive, Google Drive  
✅ **Pianificazioni Flessibili**: Manuale, programmata o continua  
✅ **Analisi AI Automatica**: Documenti analizzati all'importazione  
✅ **Log Completi**: Tracciamento di tutte le operazioni  
✅ **Filtri Configurabili**: Importa solo i file che ti servono  

### Come Iniziare

1. **Crea un connettore** specificando dove si trovano i documenti
2. **Crea una pianificazione** per definire quando importarli
3. **Monitora i log** per verificare il successo delle operazioni
4. **I tuoi documenti** sono automaticamente disponibili in DocN!

### Supporto

Per assistenza o domande:
- Consulta la documentazione tecnica: `CONFIGURATION_EXAMPLES.md`
- Contatta l'amministratore di sistema
- Verifica i log per dettagli su eventuali errori

---

**Fine Manuale Utente**

*Questo documento può essere convertito in formato Word utilizzando strumenti di conversione Markdown to Word*

*Per screenshot aggiornati, contattare l'amministratore di sistema*
