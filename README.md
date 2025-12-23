# DocN

DocN è una soluzione web modulare basata su .NET e Blazor, progettata per l’archiviazione intelligente e la consultazione di documenti, con ricerca semantica AI e integrazione Azure OpenAI/Microsoft Agent Framework.

## Funzionalità principali
- Archiviazione documenti e metadati in SQL Server 2025.
- Estrazione automatica testo/metadati dai documenti caricati.
- Proposta categoria tramite AI al caricamento documento.
- Calcolo embedding vettoriali e ricerca semantica.
- Orchestrazione retrieval e generazione risposte tramite Microsoft Agent Framework.
- Interfaccia Blazor per upload, ricerca e consultazione documenti.

## Architettura
- Progetti separati per:
  - Accesso ai dati (Data)
  - Server logic (Server, ASP.NET Core)
  - Client Blazor (Client)
  - Interfacce AI (.cs dedicati)
- Integrazione chatbot AI.

## Note iniziali
Effettuare il primo commit con questo file. Successivamente saranno aggiunti progetti, logica, database e documentazione tecnica dettagliata.