# DocN

DocN è una soluzione web modulare basata su .NET e Blazor, progettata per l’archiviazione intelligente e la consultazione di documenti, con ricerca semantica AI e integrazione Azure OpenAI/Microsoft Agent Framework.

## Funzionalità principali
- ✅ **Interfaccia di caricamento documenti con pulsante grande e visibile**.
- ✅ Upload di file con drag-and-drop e selezione multipla.
- ✅ Feedback visivo in tempo reale con Blazor Server.
- 🔄 Archiviazione documenti e metadati in SQL Server 2025 (in sviluppo).
- 🔄 Estrazione automatica testo/metadati dai documenti caricati (in arrivo).
- 🔄 Proposta categoria tramite AI al caricamento documento (in arrivo).
- 🔄 Calcolo embedding vettoriali e ricerca semantica (in arrivo).
- 🔄 Orchestrazione retrieval e generazione risposte tramite Microsoft Agent Framework (in arrivo).

## Architettura
- Progetti implementati:
  - ✅ **DocN.Web** - Blazor Server App per upload, ricerca e consultazione documenti
- Progetti futuri:
  - 🔄 Accesso ai dati (Data) - in arrivo
  - 🔄 Server logic (Server, ASP.NET Core) - in arrivo
  - 🔄 Interfacce AI (.cs dedicati) - in arrivo
  - 🔄 Integrazione chatbot AI - in arrivo

## Come eseguire l'applicazione

### Prerequisiti
- .NET 8.0 SDK o superiore

### Esecuzione
```bash
cd src/DocN.Web
dotnet run
```

L'applicazione sarà disponibile su `http://localhost:5000`

### Build
```bash
dotnet build
```

## Stato attuale
✅ **Implementato**: Interfaccia di caricamento documenti con pulsante grande e ben visibile  
🔄 **In sviluppo**: Database, AI, ricerca semantica e chatbot

## Note
Il pulsante "Carica Documento" è ora ben visibile e aggiornato correttamente con:
- Design grande e prominente con gradiente animato
- Effetti hover e pulsazione per attirare l'attenzione
- Feedback in tempo reale durante il caricamento
- Interfaccia utente completamente in italiano
- Supporto drag-and-drop per facilità d'uso
