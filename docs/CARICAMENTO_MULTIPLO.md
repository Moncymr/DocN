# Funzionalità Caricamento Multiplo Documenti

## Panoramica
La funzionalità di Caricamento Multiplo consente di caricare simultaneamente più documenti con opzioni di elaborazione condivise, migliorando significativamente l'efficienza nella gestione di caricamenti di documenti in blocco.

## Accesso alla Funzionalità
- **Menu di navigazione**: Cliccare su "📤📤 Carica Multiplo"
- **URL diretto**: `/upload-multiple`

## Caratteristiche Principali

### 📁 Gestione Categoria
- **Categoria Unica**: Tutti i documenti caricati vengono automaticamente categorizzati nella stessa categoria
- **Campo Obbligatorio**: È necessario selezionare una categoria prima di procedere

### 📤 Selezione File
- **Supporto Multiplo**: È possibile selezionare fino a 100 file in una singola operazione
- **Drag & Drop**: Supporta la selezione dei file tramite trascinamento
- **Validazione Automatica**: Valida automaticamente tipo e dimensione dei file
  - Dimensione massima: 50MB (configurabile)
  - Formati supportati: PDF, DOCX, XLSX, TXT, immagini e altri

### 👁️ Impostazioni Visibilità
Tutti i documenti ricevono lo stesso livello di visibilità:
- 🔒 **Privato** - Visibile solo al caricatore
- 👥 **Condiviso** - Condiviso con utenti specifici
- 🏢 **Organizzazione** - Visibile a tutti i membri dell'organizzazione
- 🌐 **Pubblico** - Visibile a tutti

### 🏷️ Metadati Condivisi
- **Tag**: Applicare tag comuni a tutti i documenti (separati da virgola)
- **Note**: Aggiungere note condivise a tutti i documenti caricati

### ⚙️ Opzioni di Elaborazione

Tutte le opzioni di elaborazione disponibili nel caricamento singolo sono disponibili:

1. **📝 Estrazione testo automatica**
   - Estrae automaticamente il contenuto testuale dai documenti
   - Supporta OCR per le immagini
   - Funziona con più formati: PDF, DOCX, immagini, ecc.

2. **🏷️ Estrai tag automaticamente con AI**
   - Utilizza l'AI per estrarre automaticamente tag e parole chiave rilevanti
   - Integra i tag inseriti manualmente

3. **📋 Estrai metadati strutturati con AI**
   - Estrae metadati strutturati come:
     - Numeri di fattura
     - Date
     - Autori
     - Dettagli contrattuali
     - E altro ancora

4. **🧠 Genera embeddings (Gemini)**
   - Crea embeddings vettoriali per la ricerca semantica
   - Abilita la scoperta intelligente dei documenti
   - Supporta la ricerca per similarità

5. **⚡ Genera embeddings chunks immediatamente**
   - **Predefinito (Non selezionato)**: Crea i chunks immediatamente ma genera gli embeddings in background (caricamenti più veloci)
   - **Selezionato**: Genera tutti gli embeddings durante il caricamento (più lento ma completo subito)

### 📊 Tracciamento Progresso

Il processo di caricamento include il tracciamento del progresso in tempo reale:

- **Barra di Progresso Generale**: Mostra la percentuale di file completati
- **Stato per File**: Stato individuale per ogni file:
  - ⏳ **In attesa**: In attesa di essere elaborato
  - 🔄 **Elaborazione**: Attualmente in elaborazione con informazioni dettagliate sullo step
  - ✅ **Completato**: Caricato ed elaborato con successo
  - ❌ **Errore**: Fallito con dettagli dell'errore

### 🚀 Elaborazione Asincrona

- **Non Bloccante**: I file vengono elaborati in modo asincrono senza bloccare l'interfaccia
- **Elaborazione Parallela**: Più file possono essere elaborati contemporaneamente
- **Isolamento Errori**: Se un file fallisce, gli altri continuano l'elaborazione
- **Logging Dettagliato**: Tutte le operazioni vengono registrate per la risoluzione dei problemi

## Guida Passo-Passo

1. **Naviga a Caricamento Multiplo**
   - Clicca su "📤📤 Carica Multiplo" nel menu di navigazione

2. **Seleziona Categoria**
   - Inserisci un nome di categoria (obbligatorio)
   - Questa categoria verrà applicata a tutti i documenti

3. **Scegli i File**
   - Clicca sull'area di caricamento o trascina i file
   - Seleziona più file dal tuo file system
   - Rivedi l'elenco dei file selezionati

4. **Configura le Opzioni**
   - Imposta il livello di visibilità per tutti i documenti
   - Aggiungi tag opzionali (separati da virgola)
   - Aggiungi note opzionali
   - Abilita/disabilita le opzioni di elaborazione:
     - Estrazione testo (consigliato)
     - Estrazione tag AI
     - Estrazione metadati AI
     - Generazione embeddings vettoriali
     - Generazione immediata embeddings chunks

5. **Carica**
   - Clicca sul pulsante "📤 Carica X Documenti"
   - Monitora il progresso per ogni file
   - Attendi il completamento o rivedi eventuali errori

6. **Rivedi i Risultati**
   - Successo: Reindirizzamento automatico alla pagina Documenti
   - Successo parziale: Rivedi i dettagli degli errori per i file falliti
   - Tutti falliti: Controlla i messaggi di errore e riprova

## Flusso di Elaborazione

Per ogni file, il sistema esegue i seguenti passaggi:

1. **Validazione File**: Verifica dimensione e formato del file
2. **Archiviazione File**: Salva il file nella directory di caricamento configurata
3. **Estrazione Testo**: Estrae il contenuto testuale (se abilitato)
4. **Generazione Embeddings**: Crea embeddings vettoriali (se abilitato)
5. **Estrazione Tag**: Utilizza l'AI per estrarre i tag (se abilitato)
6. **Estrazione Metadati**: Utilizza l'AI per estrarre metadati strutturati (se abilitato)
7. **Salvataggio Database**: Crea il record del documento con tutti i dati
8. **Elaborazione Chunks**: Crea chunks e opzionalmente genera embeddings

## Gestione Errori

### Errori a Livello di File
- Gli errori sono isolati per file
- I file falliti non influenzano i caricamenti riusciti
- Messaggi di errore dettagliati forniti per la risoluzione dei problemi

### Problemi Comuni
- **File troppo grande**: Riduci la dimensione del file o suddividilo in file più piccoli
- **Formato non supportato**: Converti in un formato supportato
- **Elaborazione AI fallita**: Verifica la configurazione del provider AI
- **Timeout di rete**: Riprova il caricamento o verifica la connessione

## Considerazioni sulle Prestazioni

### Impostazioni Ottimali
- **Piccoli batch**: 10-20 file alla volta per prestazioni ottimali
- **Embeddings in background**: Lascia "Embeddings chunks immediati" deselezionato per caricamenti più veloci
- **Dimensione file**: Mantieni i file sotto i 10MB quando possibile
- **Rete**: Assicurati di avere una connessione internet stabile per l'elaborazione AI

### Batch Grandi
- I file vengono elaborati in modo asincrono
- L'interfaccia rimane reattiva durante l'elaborazione
- Le risorse del server sono gestite in modo efficiente
- Aggiornamenti del progresso ogni pochi secondi

## Confronto con Caricamento Singolo

| Funzionalità | Caricamento Singolo | Caricamento Multiplo |
|--------------|-------------------|---------------------|
| File per operazione | 1 | Fino a 100 |
| Categoria per file | Individuale | Condivisa |
| Opzioni di elaborazione | Per file | Condivise tra tutti |
| Tracciamento progresso | Semplice | Dettagliato per file |
| Analisi AI | Opzionale per file | Opzionale condivisa |
| Tempo di caricamento | Veloce | Dipende dal numero di file |
| Gestione errori | Singolo punto | Isolato per file |

## Suggerimenti per l'Uso

### Quando Usare il Caricamento Multiplo
- ✅ Caricamento di più documenti della stessa categoria
- ✅ Importazione di archivi documentali
- ✅ Migrazione da sistemi precedenti
- ✅ Caricamento batch di fatture, contratti, ecc.

### Quando Usare il Caricamento Singolo
- ✅ Documenti che richiedono categorie diverse
- ✅ Documenti che richiedono revisione AI dettagliata
- ✅ File singoli con tag e note specifici
- ✅ Documenti che richiedono analisi approfondita

## Risoluzione Problemi

### Il caricamento è lento
- Riduci il numero di file per batch
- Disabilita "Genera embeddings chunks immediatamente"
- Verifica la velocità della connessione internet
- Controlla le risorse del server

### Alcuni file falliscono
- Verifica il formato e la dimensione dei file
- Controlla i messaggi di errore specifici
- Rivedi la configurazione AI
- Verifica i permessi di accesso

### L'AI non estrae metadati
- Verifica la configurazione del provider AI (Gemini/OpenAI/Azure)
- Controlla le chiavi API
- Assicurati che i documenti contengano testo estraibile
- Rivedi i log per dettagli specifici

## Supporto

Per problemi o domande:
1. Controlla i messaggi di errore per file specifici
2. Verifica la configurazione del provider AI
3. Controlla i requisiti di formato e dimensione del file
4. Contatta l'amministratore di sistema se i problemi persistono

## Vedi Anche
- [Caricamento Singolo File](../DocN.Client/Components/Pages/Upload.razor)
- [Gestione Documenti](../DocN.Client/Components/Pages/Documents.razor)
- [Configurazione AI](../DocN.Client/Components/Pages/Config.razor)
- [Ricerca Documenti](../DocN.Client/Components/Pages/Search.razor)
