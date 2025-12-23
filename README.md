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
Il sistema è ora completamente implementato e funzionante. Per dettagli tecnici completi, vedere [IMPLEMENTATION.md](IMPLEMENTATION.md).

### ✅ Problema Risolto
L'endpoint `/documents` ora visualizza **tutti i documenti** indipendentemente dallo stato del campo vettore, risolvendo il problema dove documenti senza embedding vettoriali non venivano mostrati.

### Avvio Rapido
```bash
# Build del progetto
dotnet build

# Eseguire il server API (http://localhost:5210)
cd DocN.Server && dotnet run

# Eseguire i test
dotnet test
```
