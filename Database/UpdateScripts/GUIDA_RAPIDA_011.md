# Script di Aggiornamento - ReferencedDocumentIds Nullable

## 🎯 Scopo
Corregge l'errore SQL che impediva il salvataggio dei messaggi utente nelle conversazioni.

## 📋 File Forniti
1. **011_MakeReferencedDocumentIdsNullable.sql** - Script SQL di aggiornamento
2. **README_011_ReferencedDocumentIdsNullable.md** - Documentazione completa (in inglese)
3. **GUIDA_RAPIDA_011.md** - Questa guida rapida (in italiano)

## ⚡ Guida Rapida

### Passo 1: Identifica il tuo ambiente
Hai un database SQL Server per l'applicazione DocN che necessita di questo aggiornamento.

### Passo 2: Scegli il metodo di applicazione

#### **Metodo A: SQL Server Management Studio (Consigliato)**
```
1. Apri SQL Server Management Studio (SSMS)
2. Connettiti al tuo SQL Server
3. Apri il file: Database/UpdateScripts/011_MakeReferencedDocumentIdsNullable.sql
4. Assicurati di essere sul database corretto (DocNDb)
5. Premi F5 per eseguire lo script
6. Verifica il messaggio di successo nell'output
```

#### **Metodo B: Riga di Comando (sqlcmd)**
```bash
# SQL Server locale
sqlcmd -S localhost -d DocNDb -i 011_MakeReferencedDocumentIdsNullable.sql

# SQL Server Express
sqlcmd -S .\SQLEXPRESS -d DocNDb -i 011_MakeReferencedDocumentIdsNullable.sql

# SQL Server remoto
sqlcmd -S tuo-server.database.windows.net -d DocNDb -U username -P password -i 011_MakeReferencedDocumentIdsNullable.sql
```

#### **Metodo C: Entity Framework Migrations (Sviluppo)**
```bash
# Dalla directory root del progetto
dotnet ef database update --project DocN.Data --startup-project DocN.Server --context ApplicationDbContext
```

### Passo 3: Verifica il risultato
Dovresti vedere questo messaggio:
```
=========================================
AGGIORNAMENTO COMPLETATO CON SUCCESSO!
=========================================
La colonna ReferencedDocumentIds ora accetta NULL
I messaggi utente possono essere salvati senza errori
```

### Passo 4: Testa l'applicazione
1. Avvia l'applicazione DocN
2. Vai alla pagina Chat
3. Invia un messaggio
4. Verifica che non ci siano errori SQL

## ❓ Cosa fa questo script?

### Problema risolto
L'applicazione generava questo errore:
```
Microsoft.Data.SqlClient.SqlException: 
Non è possibile inserire il valore NULL nella colonna 'ReferencedDocumentIds'
```

### Causa
- La colonna `ReferencedDocumentIds` era configurata come NOT NULL
- I messaggi degli utenti non hanno documenti referenziati (solo le risposte dell'AI li hanno)
- Quando si salvava un messaggio utente, il campo rimaneva NULL causando l'errore

### Soluzione
Lo script modifica la colonna da NOT NULL a NULL, permettendo ai messaggi utente di essere salvati correttamente.

## 🔒 Sicurezza
- ✅ Lo script è **sicuro** e **idempotente** (può essere eseguito più volte)
- ✅ Non cancella o modifica dati esistenti
- ✅ Include controlli per verificare se l'aggiornamento è necessario
- ✅ Compatibile con dati esistenti

## 📊 Dettagli Tecnici

### Schema Database
```sql
-- Prima (causava errore):
ReferencedDocumentIds NVARCHAR(MAX) NOT NULL

-- Dopo (funzionante):
ReferencedDocumentIds NVARCHAR(MAX) NULL
```

### Esempio Codice
```csharp
// Messaggio utente - senza documenti
conversation.Messages.Add(new Message
{
    Role = "user",
    Content = query,
    // ReferencedDocumentIds non impostato → NULL nel database ✅
});

// Messaggio assistente - con documenti referenziati
conversation.Messages.Add(new Message
{
    Role = "assistant",
    Content = answer,
    ReferencedDocumentIds = documentIds  // Lista serializzata in JSON
});
```

## 🆘 Troubleshooting

### "Database DocNDb non trovato"
Verifica il nome del database. Potrebbe essere:
- `DocNDb` (default)
- `DocumentArchive` (versioni precedenti)

Modifica la prima riga dello script SQL se necessario:
```sql
USE [TuoNomeDatabase]
```

### "Colonna ReferencedDocumentIds non trovata"
Significa che la tabella Messages non ha ancora questa colonna. Verifica di aver applicato tutte le migrazioni precedenti.

### "Permessi insufficienti"
Hai bisogno di permessi ALTER TABLE sul database. Contatta l'amministratore del database.

## 📞 Supporto
Per problemi o domande:
1. Leggi la documentazione completa: `README_011_ReferencedDocumentIdsNullable.md`
2. Controlla i log dell'applicazione per errori dettagliati
3. Verifica che tutte le migrazioni precedenti siano state applicate

## ✅ Checklist Post-Aggiornamento
- [ ] Script eseguito con successo
- [ ] Messaggio di conferma visualizzato
- [ ] Applicazione riavviata (se necessario)
- [ ] Test creazione conversazione completato
- [ ] Nessun errore SQL nei log

---

**Versione Script**: 011  
**Data Creazione**: 2026-01-03  
**Migrazioni EF Core**: 20260103172002_MakeReferencedDocumentIdsNullable  
