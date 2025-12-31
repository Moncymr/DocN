# Manuale Utente DocN
## Guida Completa all'Utilizzo del Sistema

**Versione**: 2.0.0  
**Data**: Dicembre 2024  
**Destinatari**: Utenti finali

---

## 📋 Indice

1. [Introduzione](#introduzione)
2. [Registrazione e Accesso](#registrazione-e-accesso)
3. [Dashboard Principale](#dashboard-principale)
4. [Upload Documenti](#upload-documenti)
5. [Ricerca Documenti](#ricerca-documenti)
6. [Chat con Documenti](#chat-con-documenti)
7. [Gestione Documenti](#gestione-documenti)
8. [Configurazione AI](#configurazione-ai)
9. [Risoluzione Problemi](#risoluzione-problemi)

---

## Introduzione

DocN è un sistema avanzato di gestione documentale che utilizza l'intelligenza artificiale per aiutarti a organizzare, ricercare e interagire con i tuoi documenti in modo naturale. Il sistema supporta:

- **Upload di documenti** in vari formati (PDF, DOCX, XLSX, immagini)
- **Ricerca intelligente** utilizzando linguaggio naturale
- **Chat con i documenti** per ottenere risposte dalle tue informazioni
- **Categorizzazione automatica** dei documenti tramite AI
- **Estrazione OCR** da immagini e documenti scansionati

---

## Registrazione e Accesso

### Prima Registrazione

1. **Accedere all'applicazione**
   - Aprire il browser e navigare all'URL: `https://localhost:7114` (o l'URL fornito dall'amministratore)
   
2. **Cliccare su "Registrati"**
   - Nella pagina di login, cliccare sul link "Registrati" in basso

3. **Compilare il modulo di registrazione**
   - **Email**: Inserire un indirizzo email valido (sarà il tuo username)
   - **Password**: Creare una password sicura (minimo 6 caratteri, almeno una maiuscola, un numero e un carattere speciale)
   - **Conferma Password**: Ripetere la password
   - **Nome Organizzazione** (opzionale): Se sei il primo utente, puoi creare una nuova organizzazione

4. **Completare la registrazione**
   - Cliccare sul pulsante "Registrati"
   - Il primo utente registrato diventa automaticamente amministratore
   - Verrai reindirizzato alla dashboard principale

### Login

1. **Accedere alla pagina di login**
   - Navigare all'URL dell'applicazione

2. **Inserire le credenziali**
   - **Email**: Il tuo indirizzo email registrato
   - **Password**: La tua password

3. **Opzioni aggiuntive**
   - **Ricordami**: Spunta questa casella per rimanere autenticato
   - **Password dimenticata?**: Link per recuperare la password

4. **Accedere**
   - Cliccare sul pulsante "Accedi"
   - Verrai reindirizzato alla dashboard

### Recupero Password

1. **Dalla pagina di login**, cliccare su "Password dimenticata?"

2. **Inserire la tua email** e cliccare su "Invia link di reset"

3. **Controllare la email** per il link di reset password

4. **Seguire il link** e inserire la nuova password

---

## Dashboard Principale

La dashboard è la schermata principale dell'applicazione e mostra:

### Statistiche Rapide

- **Totale Documenti**: Numero di documenti caricati
- **Documenti Recenti**: Ultimi documenti aggiunti
- **Categorie Principali**: Categorie più utilizzate
- **Spazio Utilizzato**: Spazio di archiviazione utilizzato

### Menu di Navigazione

Il menu laterale permette di accedere alle diverse funzionalità:

- **Dashboard**: Panoramica generale
- **Documenti**: Gestione documenti
- **Upload**: Caricamento nuovi documenti
- **Chat**: Conversazioni con i documenti
- **Configurazione**: Impostazioni AI (solo amministratori)

---

## Upload Documenti

### Procedura di Upload

1. **Accedere alla pagina Upload**
   - Dal menu laterale, cliccare su "Upload"

2. **Selezionare i documenti**
   - **Metodo 1**: Cliccare su "Seleziona File" e scegliere i file dal tuo computer
   - **Metodo 2**: Trascinare e rilasciare i file nell'area designata (drag & drop)

3. **Formati supportati**
   - Documenti: PDF, DOCX, XLSX, TXT
   - Immagini: PNG, JPG, JPEG, TIFF, BMP
   - Dimensione massima: 50 MB per file

4. **Configurare le opzioni**
   - **Categoria**: Selezionare una categoria esistente o lasciare che l'AI la suggerisca
   - **Tag**: Aggiungere tag manuali o lasciare che l'AI li estragga
   - **Visibilità**:
     - **Privato**: Solo tu puoi vedere il documento
     - **Condiviso**: Condiviso con utenti specifici
     - **Organizzazione**: Visibile a tutta la tua organizzazione
     - **Pubblico**: Visibile a tutti gli utenti

5. **Avviare l'upload**
   - Cliccare su "Carica Documenti"
   - Il sistema elaborerà i documenti:
     - Estrazione del testo (incluso OCR per immagini)
     - Generazione embeddings per ricerca semantica
     - Estrazione automatica di metadati, categorie e tag
     - Chunking intelligente del contenuto

6. **Monitorare l'elaborazione**
   - Una barra di progresso mostrerà lo stato dell'elaborazione
   - Al termine, vedrai un messaggio di conferma
   - I documenti saranno immediatamente disponibili per la ricerca

### Best Practices per l'Upload

- **Qualità delle immagini**: Per l'OCR, utilizzare immagini ad alta risoluzione e ben illuminate
- **Organizzazione**: Scegliere nomi file descrittivi
- **Tag significativi**: Aggiungere tag specifici per facilitare la ricerca
- **Batch upload**: È possibile caricare più documenti contemporaneamente

---

## Ricerca Documenti

### Pagina Documenti

1. **Accedere alla pagina Documenti**
   - Dal menu laterale, cliccare su "Documenti"

2. **Visualizzare i documenti**
   - Elenco di tutti i documenti accessibili
   - Per ogni documento vengono mostrati:
     - Titolo/Nome file
     - Categoria
     - Tag
     - Data di caricamento
     - Dimensione

### Ricerca Base

1. **Utilizzare la barra di ricerca**
   - Nella parte superiore della pagina, inserire il termine di ricerca
   - La ricerca funziona su:
     - Nome del file
     - Contenuto del documento
     - Tag
     - Categoria

2. **Premere Invio** o cliccare sull'icona di ricerca

3. **Visualizzare i risultati**
   - I documenti vengono ordinati per rilevanza
   - I risultati evidenziano le corrispondenze trovate

### Ricerca Avanzata

1. **Ricerca Semantica**
   - Utilizzare frasi in linguaggio naturale
   - Esempio: "contratti firmati nel 2024"
   - Il sistema comprende il significato della query, non solo le parole chiave

2. **Filtri**
   - **Per Categoria**: Selezionare una o più categorie dal menu a tendina
   - **Per Tag**: Cliccare sui tag per filtrare
   - **Per Data**: Utilizzare il selettore di date per limitare il periodo
   - **Per Visibilità**: Filtrare in base al livello di accesso

3. **Ordinamento**
   - Rilevanza (predefinito)
   - Data (più recenti prima)
   - Nome (alfabetico)
   - Dimensione

### Azioni sui Documenti

Per ogni documento, sono disponibili le seguenti azioni:

- **Visualizza**: Apre il documento per la visualizzazione
- **Scarica**: Scarica il file originale
- **Modifica**: Modifica metadati, tag, categoria, visibilità
- **Condividi**: Condividi con altri utenti (se hai i permessi)
- **Elimina**: Rimuovi il documento (solo per documenti di tua proprietà)
- **Dettagli**: Visualizza informazioni complete (data caricamento, dimensione, tipo, embeddings, ecc.)

---

## Chat con Documenti

La funzionalità Chat permette di porre domande ai tuoi documenti e ottenere risposte intelligenti basate sul contenuto.

### Avviare una Chat

1. **Accedere alla pagina Chat**
   - Dal menu laterale, cliccare su "Chat"

2. **Interfaccia Chat**
   - Area messaggi: Mostra la conversazione
   - Campo di input: Per scrivere le tue domande
   - Pulsante Invia: Per inviare la domanda

### Fare Domande

1. **Scrivere una domanda**
   - Utilizzare linguaggio naturale
   - Esempi:
     - "Quali sono i contratti scaduti?"
     - "Riassumi i documenti relativi al progetto X"
     - "Trova le fatture del cliente Y"

2. **Inviare la domanda**
   - Premere Invio o cliccare sul pulsante "Invia"

3. **Ricevere la risposta**
   - Il sistema elabora la domanda:
     - Ricerca i documenti più rilevanti
     - Estrae le informazioni pertinenti
     - Genera una risposta coerente e contestuale
   - La risposta include:
     - **Testo della risposta**: Risposta generata dall'AI
     - **Documenti citati**: Link ai documenti utilizzati per la risposta
     - **Estratti rilevanti**: Porzioni di testo dai documenti

### Conversazioni Contestuali

- Il sistema mantiene il contesto della conversazione
- Puoi fare domande di follow-up
- Esempio:
  ```
  Tu: "Quali sono i contratti attivi?"
  AI: [Risposta con elenco contratti]
  Tu: "Quali di questi scadono quest'anno?"
  AI: [Risposta filtrata basata sul contesto precedente]
  ```

### Nuova Conversazione

- Per iniziare una nuova conversazione, cliccare su "Nuova Chat"
- Questo pulirà il contesto conversazionale precedente

### Best Practices per la Chat

- **Domande specifiche**: Più la domanda è specifica, migliore sarà la risposta
- **Contesto**: Fornisci contesto nelle domande (es. "nel 2024", "per il cliente X")
- **Verifica le fonti**: Controlla sempre i documenti citati per validare le informazioni
- **Iterazione**: Se la risposta non è soddisfacente, riformula la domanda

---

## Gestione Documenti

### Modificare Metadati

1. **Dalla pagina Documenti**, cliccare su "Modifica" sul documento desiderato

2. **Modificare i campi**:
   - **Titolo**: Cambiare il nome visualizzato
   - **Categoria**: Selezionare una nuova categoria
   - **Tag**: Aggiungere o rimuovere tag
   - **Descrizione**: Aggiungere note o descrizioni

3. **Salvare le modifiche**
   - Cliccare su "Salva"
   - I cambiamenti saranno immediatamente effettivi

### Condividere Documenti

1. **Cliccare su "Condividi"** sul documento

2. **Selezionare il livello di condivisione**:
   - **Privato**: Solo tu
   - **Utenti specifici**: Inserire gli indirizzi email
   - **Organizzazione**: Tutti nella tua organizzazione
   - **Pubblico**: Tutti gli utenti del sistema

3. **Impostare i permessi**:
   - **Visualizzazione**: L'utente può solo vedere il documento
   - **Modifica**: L'utente può modificare i metadati
   - **Gestione completa**: L'utente può condividere ed eliminare

4. **Confermare** la condivisione

### Eliminare Documenti

1. **Cliccare su "Elimina"** sul documento

2. **Confermare l'eliminazione**
   - Verrà mostrato un messaggio di conferma
   - L'eliminazione è permanente e non reversibile

3. **Il documento viene rimosso**:
   - Dal database
   - Dai risultati di ricerca
   - Dalle chat (non sarà più utilizzato per generare risposte)

---

## Configurazione AI

**Nota**: Questa sezione è accessibile solo agli amministratori del sistema.

### Accedere alle Configurazioni

1. **Dal menu laterale**, cliccare su "Configurazione"

2. **Pagina di configurazione AI**
   - Mostra tutti i provider AI configurati
   - Permette di aggiungere, modificare, attivare/disattivare provider

### Provider AI Supportati

#### Google Gemini

- **Modelli Chat**: gemini-2.0-flash-exp, gemini-1.5-pro, gemini-1.5-flash
- **Modelli Embedding**: text-embedding-004
- **Configurazione**:
  - API Key: Chiave API Google Gemini
  - Modello chat preferito
  - Modello embedding preferito
  - Attivo/Inattivo

#### OpenAI

- **Modelli Chat**: gpt-4, gpt-4-turbo, gpt-3.5-turbo
- **Modelli Embedding**: text-embedding-3-large, text-embedding-3-small, text-embedding-ada-002
- **Configurazione**:
  - API Key: Chiave API OpenAI
  - Modello chat preferito
  - Modello embedding preferito
  - Attivo/Inattivo

#### Azure OpenAI

- **Configurazione Enterprise**:
  - Endpoint Azure
  - API Key
  - Deployment names
  - API Version
  - Attivo/Inattivo

### Configurare un Provider

1. **Cliccare su "Aggiungi Provider"** o selezionare un provider esistente

2. **Compilare i campi**:
   - **Nome**: Nome identificativo del provider
   - **Tipo**: Gemini, OpenAI, o Azure OpenAI
   - **API Key**: La chiave API fornita dal provider
   - **Modelli**: Selezionare i modelli preferiti

3. **Assegnare i servizi**:
   - **Chat**: Quale provider usare per la chat
   - **Embeddings**: Quale provider per generare embeddings
   - **Tag Extraction**: Quale provider per estrarre tag
   - **RAG**: Quale provider per il sistema RAG

4. **Testare la configurazione**:
   - Cliccare su "Testa Connessione"
   - Verificare che il test sia positivo

5. **Attivare il provider**:
   - Spostare il toggle su "Attivo"
   - Cliccare su "Salva"

### Parametri Avanzati

- **Similarity Threshold**: Soglia di similarità per la ricerca semantica (0.0 - 1.0)
- **Max Documents**: Numero massimo di documenti da recuperare nelle ricerche
- **Chunk Size**: Dimensione dei chunk per l'elaborazione documenti
- **Chunk Overlap**: Sovrapposizione tra chunk consecutivi
- **Temperature**: Creatività delle risposte AI (0.0 - 1.0)
- **Fallback automatico**: Abilitare/disabilitare il fallback tra provider

---

## Risoluzione Problemi

### Problemi di Upload

**Problema**: Il documento non viene caricato

**Soluzioni**:
- Verificare che il formato sia supportato
- Controllare la dimensione del file (max 50 MB)
- Verificare la connessione internet
- Controllare che il nome file non contenga caratteri speciali

**Problema**: L'OCR non estrae correttamente il testo

**Soluzioni**:
- Utilizzare immagini ad alta risoluzione (300 DPI o superiore)
- Assicurarsi che il testo sia chiaro e leggibile
- Evitare immagini sfocate o con scarsa illuminazione
- Provare a ruotare l'immagine se necessario

### Problemi di Ricerca

**Problema**: I risultati di ricerca non sono rilevanti

**Soluzioni**:
- Utilizzare frasi più specifiche
- Provare con sinonimi o parole diverse
- Utilizzare i filtri (categoria, data, tag)
- Verificare che i documenti siano stati elaborati completamente

**Problema**: La ricerca semantica non funziona

**Soluzioni**:
- Verificare che sia configurato un provider AI per gli embeddings
- Attendere che i documenti siano completamente elaborati
- Contattare l'amministratore per verificare la configurazione

### Problemi di Chat

**Problema**: La chat non risponde o impiega molto tempo

**Soluzioni**:
- Verificare la connessione internet
- Attendere qualche secondo (le risposte AI richiedono tempo)
- Verificare che sia configurato un provider AI per la chat
- Contattare l'amministratore se il problema persiste

**Problema**: Le risposte non sono accurate

**Soluzioni**:
- Riformulare la domanda in modo più specifico
- Fornire più contesto nella domanda
- Verificare i documenti citati per validare le informazioni
- Segnalare risposte problematiche all'amministratore

### Problemi di Configurazione AI

**Problema**: "AI_PROVIDER_NOT_CONFIGURED"

**Soluzioni**:
- Accedere alle Configurazioni (se amministratore)
- Configurare almeno un provider AI
- Inserire una API Key valida
- Attivare il provider
- Salvare e riavviare l'applicazione se necessario

**Problema**: "ERRORE CRITICO: Il salvataggio nel database è fallito"

**Soluzioni**:
- Riavviare l'applicazione (le migrazioni vengono applicate automaticamente)
- Contattare l'amministratore di sistema
- Verificare i log per errori di migrazione database

### Ottenere Supporto

Se i problemi persistono:

1. **Controllare i log**:
   - Gli amministratori possono accedere ai log dal menu "Logs"

2. **Contattare l'amministratore**:
   - Fornire dettagli sul problema
   - Includere screenshot se possibile
   - Indicare quando si è verificato il problema

3. **Aprire un ticket**:
   - Email: support@docn.example.com
   - GitHub Issues: https://github.com/Moncymr/DocN/issues

---

## Appendice: Scorciatoie da Tastiera

- **Ctrl + U**: Vai alla pagina Upload
- **Ctrl + F**: Vai alla ricerca documenti
- **Ctrl + K**: Apri/Chiudi chat
- **Ctrl + N**: Nuova chat
- **Esc**: Chiudi finestre modali

---

## Appendice: Formati File Supportati

### Documenti

- **PDF** (.pdf): Adobe Portable Document Format
- **Word** (.docx, .doc): Microsoft Word
- **Excel** (.xlsx, .xls): Microsoft Excel
- **Testo** (.txt): File di testo semplice

### Immagini (con OCR)

- **PNG** (.png): Portable Network Graphics
- **JPEG** (.jpg, .jpeg): Joint Photographic Experts Group
- **TIFF** (.tiff, .tif): Tagged Image File Format
- **BMP** (.bmp): Bitmap

---

## Glossario

- **AI (Artificial Intelligence)**: Intelligenza Artificiale
- **RAG (Retrieval-Augmented Generation)**: Generazione Aumentata da Recupero
- **Embedding**: Rappresentazione vettoriale del testo
- **OCR (Optical Character Recognition)**: Riconoscimento Ottico dei Caratteri
- **Chunk**: Porzione di testo in cui viene suddiviso un documento
- **Semantic Search**: Ricerca semantica basata sul significato
- **Provider AI**: Servizio esterno che fornisce funzionalità AI (Gemini, OpenAI, etc.)
- **Multi-tenant**: Sistema che supporta più organizzazioni separate

---

**Fine del Manuale Utente**

Per domande o assistenza, contattare: support@docn.example.com
