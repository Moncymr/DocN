# Manuale Utente DocN - Sistema RAG Documentale

## Indice
1. [Introduzione](#introduzione)
2. [Registrazione e Accesso](#registrazione-e-accesso)
3. [Dashboard Principale](#dashboard-principale)
4. [Gestione Documenti](#gestione-documenti)
5. [Ricerca Documenti](#ricerca-documenti)
6. [Chat con Documenti](#chat-con-documenti)
7. [Configurazione AI](#configurazione-ai)
8. [Gestione Agenti](#gestione-agenti)

---

## Introduzione

DocN è un sistema avanzato di gestione documentale enterprise con funzionalità di Retrieval-Augmented Generation (RAG) basato su intelligenza artificiale. Questo manuale guida l'utente attraverso tutte le funzionalità principali del sistema.

### Requisiti di Sistema
- Browser moderno (Chrome, Firefox, Edge, Safari)
- Connessione internet attiva
- Risoluzione minima: 1024x768

---

## Registrazione e Accesso

### Prima Registrazione

**Procedura:**

1. **Apertura dell'applicazione**
   - Aprire il browser e navigare all'URL dell'applicazione (es. `https://localhost:7114`)
   - Si visualizza la pagina di benvenuto con il logo DocN

2. **Avvio registrazione**
   - Cliccare sul pulsante "Register" in alto a destra
   - Si apre il modulo di registrazione

3. **Compilazione dati**
   - **Email**: Inserire un indirizzo email valido (sarà utilizzato per il login)
   - **Password**: Inserire una password sicura (minimo 6 caratteri, deve contenere maiuscole, minuscole e numeri)
   - **Confirm Password**: Reinserire la stessa password per conferma
   - **Organization Name**: Inserire il nome della propria organizzazione

4. **Completamento registrazione**
   - Cliccare sul pulsante "Register"
   - Il primo utente registrato diventa automaticamente amministratore
   - Si viene reindirizzati alla dashboard principale

**Note importanti:**
- Il primo utente registrato nel sistema riceve automaticamente i privilegi di amministratore
- La password deve rispettare i requisiti di sicurezza: almeno una maiuscola, una minuscola e un numero
- Ogni utente è associato a un'organizzazione per la gestione multi-tenant

---

### Login

**Procedura:**

1. **Accesso alla pagina di login**
   - Dalla homepage, cliccare su "Login" in alto a destra
   - Si apre il modulo di login

2. **Inserimento credenziali**
   - **Email**: Inserire l'email utilizzata in fase di registrazione
   - **Password**: Inserire la password
   - (Opzionale) Spuntare "Remember me" per mantenere la sessione attiva

3. **Accesso al sistema**
   - Cliccare sul pulsante "Log in"
   - Si viene reindirizzati alla dashboard principale

**Funzionalità aggiuntive:**
- **Forgot Password**: Link per recuperare la password tramite email
- **Remember me**: Mantiene la sessione attiva anche dopo la chiusura del browser

---

## Dashboard Principale

Dopo il login, si accede alla dashboard principale che fornisce una panoramica completa del sistema.

### Elementi della Dashboard

**Sezione superiore - Statistiche:**
- **Totale Documenti**: Numero complessivo di documenti caricati
- **Dimensione Totale**: Spazio occupato dai documenti (in MB/GB)
- **Categorie**: Numero di categorie utilizzate
- **Documenti Oggi**: Documenti caricati nelle ultime 24 ore

**Sezione centrale - Grafici:**
- **Documenti per Categoria**: Grafico a torta che mostra la distribuzione dei documenti per categoria
- **Caricamenti Recenti**: Timeline degli ultimi caricamenti

**Sezione inferiore - Documenti Recenti:**
- Lista degli ultimi 10 documenti caricati con:
  - Nome file
  - Data e ora di caricamento
  - Dimensione
  - Categoria
  - Azioni rapide (Visualizza, Elimina)

### Menu di Navigazione

**Menu laterale sinistro:**
- **Home**: Dashboard principale
- **Documents**: Gestione completa dei documenti
- **Upload**: Caricamento nuovi documenti
- **Search**: Ricerca avanzata
- **Chat**: Chat intelligente con i documenti
- **Agents**: Gestione agenti AI (solo amministratori)
- **Config**: Configurazione AI e sistema (solo amministratori)

**Menu superiore destro:**
- **Notifiche**: Icona campanello con notifiche di sistema
- **Profilo utente**: Menu dropdown con:
  - Nome utente e organizzazione
  - Settings
  - Logout

---

## Gestione Documenti

### Upload Documenti

**Procedura:**

1. **Accesso alla pagina Upload**
   - Dal menu laterale, cliccare su "Upload"
   - Si apre l'interfaccia di caricamento documenti

2. **Selezione file**
   - **Metodo 1 - Click**: Cliccare sull'area "Drop files here or click to browse"
   - **Metodo 2 - Drag & Drop**: Trascinare i file direttamente nell'area di upload
   - I file supportati vengono evidenziati in verde

3. **Configurazione metadati**
   - **Title**: Inserire un titolo descrittivo per il documento
   - **Description**: (Opzionale) Aggiungere una descrizione dettagliata
   - **Category**: Selezionare una categoria dal menu dropdown
     - Se la categoria non esiste, inserire un nuovo nome
   - **Tags**: Aggiungere tag separati da virgola per facilitare la ricerca
   - **Visibility**: Scegliere il livello di visibilità:
     - **Private**: Solo l'utente proprietario può vedere il documento
     - **Shared**: Condiviso con utenti specifici
     - **Organization**: Visibile a tutti gli utenti dell'organizzazione
     - **Public**: Accessibile a tutti

4. **Elaborazione AI (Opzionale)**
   - **Extract Tags with AI**: Spuntare per estrarre automaticamente i tag dal contenuto
   - **Extract Metadata with AI**: Spuntare per estrarre automaticamente categorie e metadati
   - **Generate Embeddings**: Spuntare per abilitare la ricerca semantica sul documento

5. **Avvio caricamento**
   - Cliccare sul pulsante "Upload Document"
   - Barra di progresso mostra l'avanzamento del caricamento
   - Messaggio di conferma al completamento

**Formati supportati:**
- **Documenti**: PDF, DOCX, DOC, TXT
- **Fogli di calcolo**: XLSX, XLS, CSV
- **Immagini**: PNG, JPG, JPEG, TIFF, BMP (con OCR)
- **Dimensione massima**: 50 MB per file

**Elaborazione automatica:**
- **OCR**: Se il file è un'immagine, viene estratto automaticamente il testo
- **Chunking**: Il documento viene suddiviso in chunk ottimizzati per la ricerca
- **Embedding**: Vengono generati vettori per la ricerca semantica
- **Tag Extraction**: L'AI estrae automaticamente tag rilevanti dal contenuto

---

### Visualizzazione Lista Documenti

**Procedura:**

1. **Accesso alla pagina Documents**
   - Dal menu laterale, cliccare su "Documents"
   - Si apre la lista completa dei documenti

2. **Navigazione lista**
   - **Tabella documenti** con colonne:
     - **Checkbox**: Selezione multipla documenti
     - **Title**: Titolo del documento (clickabile per dettagli)
     - **Category**: Categoria assegnata
     - **Tags**: Tag associati
     - **Size**: Dimensione del file
     - **Uploaded**: Data e ora di caricamento
     - **Actions**: Pulsanti azione (View, Edit, Delete, Share)

3. **Ordinamento**
   - Cliccare sull'intestazione di una colonna per ordinare
   - Freccia su/giù indica direzione ordinamento

4. **Paginazione**
   - Selettore "Items per page" (10, 25, 50, 100)
   - Navigazione tra pagine con pulsanti Previous/Next
   - Numero pagina corrente e totale

**Filtri disponibili:**
- **Search**: Campo di ricerca rapida per nome file
- **Category**: Filtro per categoria
- **Date Range**: Filtro per intervallo di date
- **Visibility**: Filtro per livello di visibilità

**Azioni sui documenti:**
- **View**: Visualizza dettagli completi e anteprima
- **Edit**: Modifica metadati del documento
- **Delete**: Elimina il documento (richiede conferma)
- **Share**: Condividi con altri utenti
- **Download**: Scarica il file originale

---

### Modifica Documenti

**Procedura:**

1. **Apertura editor**
   - Dalla lista documenti, cliccare sul pulsante "Edit" (icona matita)
   - Si apre il modulo di modifica

2. **Modifica metadati**
   - **Title**: Modificare il titolo
   - **Description**: Aggiornare la descrizione
   - **Category**: Cambiare categoria
   - **Tags**: Aggiungere o rimuovere tag
   - **Visibility**: Modificare il livello di visibilità

3. **Rielaborazione AI**
   - **Re-extract Tags**: Rigenera i tag con AI
   - **Re-extract Metadata**: Rigenera metadati con AI
   - **Regenerate Embeddings**: Ricalcola i vettori per ricerca semantica

4. **Salvataggio**
   - Cliccare su "Save Changes"
   - Messaggio di conferma al completamento

---

### Eliminazione Documenti

**Procedura:**

1. **Selezione documento**
   - Dalla lista, cliccare sul pulsante "Delete" (icona cestino)
   - Si apre un dialog di conferma

2. **Conferma eliminazione**
   - **Warning**: "Are you sure you want to delete this document? This action cannot be undone."
   - **Cancel**: Annulla l'operazione
   - **Delete**: Conferma ed elimina permanentemente

3. **Eliminazione multipla**
   - Spuntare le checkbox di più documenti
   - Cliccare su "Delete Selected" nella toolbar
   - Confermare l'operazione

**Nota:** L'eliminazione è permanente e non può essere annullata. Tutti i dati associati (embeddings, chunks, metadati) vengono rimossi.

---

## Ricerca Documenti

### Ricerca Semplice

**Procedura:**

1. **Accesso alla ricerca**
   - Dal menu laterale, cliccare su "Search"
   - Si apre l'interfaccia di ricerca

2. **Inserimento query**
   - Digitare il testo da cercare nel campo "Search documents"
   - La ricerca avviene in tempo reale (dopo 500ms dall'ultima digitazione)

3. **Visualizzazione risultati**
   - Lista documenti ordinati per rilevanza
   - Score di rilevanza visualizzato per ogni risultato
   - Highlights delle parole chiave trovate

**Modalità di ricerca:**
- **Text Search**: Ricerca tradizionale full-text
- **Semantic Search**: Ricerca semantica basata su significato (richiede embeddings)
- **Hybrid Search**: Combina text e semantic search per risultati ottimali

---

### Ricerca Avanzata

**Procedura:**

1. **Attivazione filtri avanzati**
   - Cliccare su "Advanced Filters"
   - Si espande il pannello con opzioni aggiuntive

2. **Configurazione filtri**
   - **Search Mode**: 
     - Text: Solo ricerca testuale
     - Semantic: Solo ricerca semantica
     - Hybrid: Combinata (consigliata)
   - **Categories**: Selezionare una o più categorie
   - **Tags**: Filtrare per tag specifici
   - **Date Range**: 
     - From: Data inizio
     - To: Data fine
   - **File Types**: PDF, DOCX, TXT, Images, etc.
   - **Visibility**: Private, Shared, Organization, Public

3. **Opzioni ricerca semantica**
   - **Similarity Threshold**: Soglia di similarità (0-1)
     - 0.7: Alta precisione
     - 0.5: Bilanciato
     - 0.3: Alta recall
   - **Max Results**: Numero massimo di risultati (5-50)

4. **Esecuzione ricerca**
   - Cliccare su "Search"
   - Attendere elaborazione risultati
   - I risultati vengono visualizzati con score e highlights

**Caratteristiche ricerca ibrida:**
- **Reciprocal Rank Fusion (RRF)**: Combina i risultati delle due modalità
- **Re-ranking**: Riordina i risultati per massimizzare la rilevanza
- **Context snippets**: Mostra estratti del contenuto con la query evidenziata

---

### Interpretazione Risultati

**Elementi visualizzati per ogni risultato:**

1. **Header**
   - **Title**: Titolo del documento (link per aprire)
   - **Score**: Punteggio di rilevanza (0-100)
   - **Match Type**: Text, Semantic, o Hybrid

2. **Body**
   - **Snippet**: Estratto rilevante con keywords evidenziate
   - **Category**: Categoria del documento
   - **Tags**: Tag associati

3. **Footer**
   - **File Info**: Tipo file, dimensione, data
   - **Actions**: View, Download, Add to Chat

**Ordinamento risultati:**
- I risultati sono ordinati per score decrescente
- I documenti con match multipli hanno score più alto
- La ricerca semantica privilegia il significato rispetto alle keyword esatte

---

## Chat con Documenti

La funzionalità Chat permette di interagire in linguaggio naturale con i documenti, ottenendo risposte contestualizzate e citazioni precise.

### Avvio Nuova Chat

**Procedura:**

1. **Accesso alla chat**
   - Dal menu laterale, cliccare su "Chat"
   - Si apre l'interfaccia di chat

2. **Selezione contesto**
   - **All Documents**: Chat su tutti i documenti accessibili
   - **Specific Category**: Chat su una categoria specifica
   - **Selected Documents**: Chat su documenti selezionati
     - Cliccare "Select Documents"
     - Spuntare i documenti desiderati
     - Cliccare "Confirm Selection"

3. **Prima domanda**
   - Digitare la domanda nel campo "Type your question..."
   - Cliccare su "Send" o premere Enter
   - Il sistema elabora la richiesta

**Esempi di domande:**
- "Quali sono i requisiti di sicurezza descritti nei documenti?"
- "Riassumi i principali punti del contratto con cliente X"
- "Trova informazioni sui costi operativi del 2024"
- "Confronta le proposte dei fornitori A e B"

---

### Conversazione con RAG

**Funzionamento:**

1. **Elaborazione query**
   - Il sistema analizza la domanda
   - Identifica i documenti più rilevanti
   - Recupera i chunk pertinenti

2. **Generazione risposta**
   - L'AI genera una risposta basata sui documenti
   - Include citazioni dai documenti fonte
   - Mantiene il contesto della conversazione

3. **Visualizzazione risposta**
   - **Messaggio utente**: Visualizzato a destra con sfondo blu
   - **Risposta AI**: Visualizzata a sinistra con sfondo grigio
   - **Sources**: Box con i documenti utilizzati per la risposta
   - **Citations**: Link clickabili ai punti specifici nei documenti

**Elementi della risposta:**
- **Answer**: Testo della risposta generata
- **Confidence**: Indicatore di confidenza (High, Medium, Low)
- **Sources Used**: Lista documenti consultati con:
  - Nome documento
  - Relevance score
  - Link per aprire
- **Citations**: Estratti specifici utilizzati con riferimenti

---

### Gestione Conversazioni

**Funzionalità disponibili:**

1. **Cronologia chat**
   - Sidebar sinistra mostra tutte le chat salvate
   - Ogni chat ha:
     - Titolo (prima domanda)
     - Data ultima interazione
     - Numero messaggi

2. **Azioni sulla chat corrente**
   - **Clear Chat**: Pulisce la conversazione corrente
   - **Export Chat**: Esporta in PDF o TXT
   - **Share Chat**: Condividi con altri utenti

3. **Nuova chat**
   - Cliccare su "New Chat" per iniziare una nuova conversazione
   - La chat precedente viene salvata automaticamente

4. **Eliminazione chat**
   - Hover su una chat nella sidebar
   - Cliccare sull'icona cestino
   - Confermare l'eliminazione

**Follow-up questions:**
- Il sistema mantiene il contesto delle domande precedenti
- È possibile fare domande di approfondimento
- Esempio sequenza:
  1. "Quali sono i fornitori menzionati nei contratti?"
  2. "Quali sono i termini di pagamento del fornitore X?"
  3. "Confrontali con il fornitore Y"

---

### Configurazione Parametri RAG

**Procedura:**

1. **Apertura impostazioni chat**
   - Cliccare sull'icona ingranaggio in alto a destra
   - Si apre il pannello configurazione

2. **Parametri disponibili**
   - **RAG Mode**:
     - Basic: Retrieval semplice
     - Advanced: Con re-ranking
     - HyDE: Hypothetical Document Embeddings
   - **Number of Documents**: Quanti documenti recuperare (1-20)
   - **Similarity Threshold**: Soglia similarità (0.3-0.9)
   - **Temperature**: Creatività risposta (0.0-1.0)
     - 0.0: Molto preciso e deterministico
     - 0.5: Bilanciato
     - 1.0: Più creativo e variabile
   - **Max Tokens**: Lunghezza massima risposta

3. **Salvataggio**
   - Le impostazioni vengono applicate immediatamente
   - Persistono per tutta la sessione

**Consiglio:**
- Per domande fattuali: Temperature bassa (0.1-0.3)
- Per riassunti creativi: Temperature media (0.5-0.7)
- Per brainstorming: Temperature alta (0.7-1.0)

---

## Configurazione AI

**Nota:** Questa sezione è accessibile solo agli amministratori.

### Accesso Configurazione

**Procedura:**

1. **Apertura Config**
   - Dal menu laterale, cliccare su "Config"
   - Si apre l'interfaccia di configurazione AI

2. **Sezioni disponibili**
   - AI Providers: Configurazione provider AI
   - RAG Settings: Impostazioni Retrieval-Augmented Generation
   - OCR Settings: Configurazione Tesseract OCR
   - System Settings: Impostazioni di sistema

---

### Configurazione AI Provider

**Procedura - Gemini:**

1. **Attivazione provider**
   - Nella sezione "AI Providers", trovare "Google Gemini"
   - Toggle "Enabled" su ON

2. **Inserimento API Key**
   - Cliccare su "Configure"
   - Inserire la Gemini API Key
   - La key viene validata automaticamente
   - Messaggio di conferma se valida

3. **Configurazione modelli**
   - **Chat Model**: Selezionare modello per chat (es. gemini-1.5-pro)
   - **Embedding Model**: Selezionare modello per embeddings (es. text-embedding-004)
   - **Tag Extraction Model**: Modello per estrazione tag

4. **Servizi assegnati**
   - Spuntare i servizi da assegnare a Gemini:
     - ☑ Chat
     - ☑ Embeddings
     - ☑ Tag Extraction
     - ☑ RAG

5. **Salvataggio**
   - Cliccare "Save Configuration"
   - Il sistema testa la connessione
   - Conferma se tutto OK

**Procedura - OpenAI:**

1. **Attivazione provider**
   - Nella sezione "AI Providers", trovare "OpenAI"
   - Toggle "Enabled" su ON

2. **Inserimento API Key**
   - Inserire OpenAI API Key
   - Validazione automatica

3. **Configurazione modelli**
   - **Chat Model**: gpt-4, gpt-3.5-turbo, etc.
   - **Embedding Model**: text-embedding-3-large, text-embedding-ada-002

4. **Servizi e salvataggio**
   - Assegnare servizi
   - Salvare configurazione

**Procedura - Azure OpenAI:**

1. **Configurazione endpoint**
   - **Endpoint URL**: URL del servizio Azure
   - **API Key**: Chiave di accesso Azure
   - **Deployment Names**: Nomi deployment per ogni servizio

2. **Salvataggio e test**

**Configurazione Multi-Provider:**
- È possibile attivare più provider contemporaneamente
- Ogni servizio (Chat, Embeddings, etc.) può usare un provider diverso
- Esempio setup consigliato:
  - Chat: OpenAI GPT-4 (qualità risposte)
  - Embeddings: Gemini (economico)
  - Tag Extraction: Gemini (veloce)

---

### Configurazione RAG

**Procedura:**

1. **Accesso RAG Settings**
   - Nella pagina Config, sezione "RAG Settings"

2. **Parametri generali**
   - **Default Similarity Threshold**: 0.7 (consigliato)
   - **Max Documents Retrieved**: 10
   - **Chunk Size**: 1000 caratteri
   - **Chunk Overlap**: 200 caratteri

3. **Modalità RAG**
   - **Enable HyDE**: Hypothetical Document Embeddings
   - **Enable Re-Ranking**: Riordino risultati
   - **Enable Query Rewriting**: Riscrittura query automatica

4. **Caching**
   - **Cache Embeddings**: Cache vettori (consigliato)
   - **Cache Duration**: 5 minuti
   - **Cache Provider**: Redis o In-Memory

5. **Salvataggio**
   - Cliccare "Save RAG Settings"

**Note:**
- HyDE migliora la recall ma aumenta i costi (query aggiuntive)
- Re-Ranking migliora la precisione ma aumenta la latenza
- Query Rewriting aiuta con query ambigue

---

### Configurazione OCR

**Procedura:**

1. **Accesso OCR Settings**
   - Sezione "OCR Settings"

2. **Configurazione Tesseract**
   - **Data Path**: Percorso tessdata (es. /usr/share/tesseract/tessdata)
   - **Default Language**: ita (italiano)
   - **Additional Languages**: eng,fra,deu (separati da virgola)

3. **Opzioni avanzate**
   - **Page Segmentation Mode**: Auto (consigliato)
   - **Engine Mode**: LSTM (migliore qualità)
   - **DPI**: 300 (per immagini ad alta risoluzione)

4. **Test OCR**
   - Cliccare "Test OCR"
   - Upload un'immagine di test
   - Verificare estrazione testo

5. **Salvataggio**
   - Cliccare "Save OCR Settings"

---

## Gestione Agenti

**Nota:** Funzionalità avanzata disponibile solo per amministratori.

### Creazione Nuovo Agente

**Procedura:**

1. **Accesso Agents**
   - Dal menu laterale, cliccare su "Agents"
   - Si apre la lista agenti esistenti

2. **Wizard nuovo agente**
   - Cliccare "Create New Agent"
   - Si avvia il wizard step-by-step

**Step 1 - Choose Template:**
- Selezionare un template predefinito:
  - **Customer Support**: Assistenza clienti
  - **Legal Document Analyzer**: Analisi documenti legali
  - **Technical Documentation**: Documentazione tecnica
  - **Research Assistant**: Assistente ricerca
  - **Custom**: Configurazione da zero

**Step 2 - Configure Provider:**
- **Name**: Nome identificativo dell'agente
- **Description**: Descrizione scopo e funzionalità
- **AI Provider**: Gemini, OpenAI, o Azure OpenAI
- **Model**: Selezionare modello specifico
- **Temperature**: Impostare creatività (0.0-1.0)

**Step 3 - Customize:**
- **System Prompt**: Istruzioni di sistema per l'agente
- **Specialization**: Area di specializzazione
- **Document Scope**: 
  - All Documents
  - Specific Categories (selezionare categorie)
  - Tagged Documents (selezionare tag)
- **Capabilities**: 
  - ☑ Document Retrieval
  - ☑ Summarization
  - ☑ Q&A
  - ☑ Comparison
  - ☑ Extraction

**Step 4 - Test:**
- **Test Query**: Inserire domanda di test
- **Run Test**: Eseguire test
- **Review Results**: Verificare qualità risposta
- Se necessario, tornare indietro e modificare configurazione

**Step 5 - Complete:**
- **Review Summary**: Revisione configurazione completa
- **Save Agent**: Salvataggio agente
- L'agente diventa disponibile nella lista

---

### Utilizzo Agenti

**Procedura:**

1. **Selezione agente**
   - Dalla pagina Chat, aprire dropdown "Select Agent"
   - Scegliere l'agente desiderato

2. **Interazione**
   - Le domande vengono processate dall'agente selezionato
   - L'agente applica la sua specializzazione
   - Le risposte riflettono il contesto e le capability configurate

3. **Cambio agente**
   - È possibile cambiare agente durante la conversazione
   - Il contesto viene mantenuto

---

### Gestione Agenti Esistenti

**Azioni disponibili:**

1. **Edit**: Modifica configurazione agente
2. **Clone**: Crea copia dell'agente da personalizzare
3. **Test**: Esegui test rapido
4. **View Stats**: Statistiche utilizzo
   - Numero conversazioni
   - Rating medio
   - Token utilizzati
5. **Delete**: Elimina agente (richiede conferma)

---

## Risoluzione Problemi Comuni

### Errore: "AI_PROVIDER_NOT_CONFIGURED"

**Causa:** Nessun provider AI configurato correttamente.

**Soluzione:**
1. Andare in Config → AI Providers
2. Configurare almeno un provider (Gemini o OpenAI)
3. Inserire API key valida
4. Attivare il provider (toggle ON)
5. Salvare la configurazione

---

### Errore: "Failed to upload document"

**Possibili cause e soluzioni:**

1. **File troppo grande**
   - Verificare che il file sia < 50 MB
   - Se necessario, comprimere o dividere il file

2. **Formato non supportato**
   - Verificare che il formato sia nella lista supportati
   - Convertire il file in un formato compatibile

3. **Spazio disco insufficiente**
   - Contattare l'amministratore di sistema

---

### Ricerca non restituisce risultati

**Soluzioni:**

1. **Verifica embeddings**
   - Assicurarsi che i documenti abbiano embeddings generati
   - Nella lista documenti, verificare la colonna "Embeddings"
   - Se mancanti, ri-processare i documenti

2. **Soglia similarità**
   - Abbassare la "Similarity Threshold" (es. da 0.7 a 0.5)
   - Provare con "Hybrid Search" invece di solo Semantic

3. **Query troppo generica**
   - Rendere la query più specifica
   - Aggiungere keywords rilevanti

---

### Chat non risponde o risponde male

**Soluzioni:**

1. **Verifica configurazione RAG**
   - Config → RAG Settings
   - Controllare parametri (numero documenti, threshold)

2. **Cambia modello AI**
   - Provare con un modello più avanzato (es. GPT-4)

3. **Aumenta numero documenti recuperati**
   - Impostazioni chat → Number of Documents → aumentare

4. **Verifica contesto**
   - Assicurarsi che i documenti rilevanti siano nel scope della chat

---

## Shortcuts Tastiera

- **Ctrl/Cmd + K**: Focus su search bar
- **Ctrl/Cmd + U**: Upload nuovo documento
- **Ctrl/Cmd + N**: Nuova chat
- **Ctrl/Cmd + Enter**: Invia messaggio chat
- **Esc**: Chiude dialog/modal attivo

---

## Best Practices

### Organizzazione Documenti

1. **Naming Convention**
   - Usare nomi file descrittivi
   - Includere date in formato YYYY-MM-DD
   - Esempio: "2024-12-31_Contratto_Cliente_XYZ.pdf"

2. **Categorizzazione**
   - Creare categorie significative e coerenti
   - Non creare troppe categorie (max 20-30)
   - Usare gerarchia se necessario

3. **Tagging**
   - Aggiungere 3-5 tag per documento
   - Usare tag consistenti (es. sempre minuscolo)
   - Includere: tipo, progetto, cliente, anno

4. **Metadata**
   - Compilare sempre la description
   - Essere precisi e concisi
   - Includere informazioni per la ricerca futura

### Utilizzo Chat RAG

1. **Domande efficaci**
   - Essere specifici e chiari
   - Includere contesto se necessario
   - Evitare domande troppo generiche

2. **Gestione contesto**
   - Selezionare solo documenti rilevanti
   - Non caricare troppi documenti (max 20-30)
   - Pulire la chat quando si cambia argomento

3. **Interpretazione risposte**
   - Verificare sempre le citazioni
   - Controllare confidence score
   - Consultare documenti fonte per conferma

### Configurazione AI

1. **Scelta provider**
   - OpenAI GPT-4: Massima qualità, costo alto
   - Gemini Pro: Buon compromesso qualità/costo
   - Multi-provider: Ottimizzare per servizio

2. **Parametri RAG**
   - Iniziare con valori default
   - Ottimizzare basandosi sui risultati
   - Testare modifiche su set di domande standard

---

## Glossario

- **RAG**: Retrieval-Augmented Generation - Tecnica che combina ricerca e generazione di testo
- **Embedding**: Rappresentazione vettoriale di un testo
- **Chunk**: Porzione di testo di dimensione ottimale per l'elaborazione
- **Similarity**: Misura di somiglianza tra due embeddings (0-1)
- **Temperature**: Parametro che controlla la creatività dell'AI
- **Token**: Unità di testo processata dall'AI (≈ 0.75 parole)
- **OCR**: Optical Character Recognition - Estrazione testo da immagini
- **HyDE**: Hypothetical Document Embeddings - Tecnica RAG avanzata
- **Re-ranking**: Riordinamento risultati per migliorare rilevanza

---

## Supporto

Per assistenza aggiuntiva:
- **Email**: support@docn.example.com
- **Documentazione tecnica**: Consultare i file .md nella repository
- **Issues**: Aprire un issue su GitHub per bug o feature request

---

**Versione Manuale**: 1.0  
**Ultimo Aggiornamento**: Dicembre 2024  
**Compatibilità**: DocN v2.0.0+
