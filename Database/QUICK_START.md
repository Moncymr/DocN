# 🚀 Guida Rapida - Setup Database DocN

Questa guida ti aiuterà a configurare rapidamente il database DocN con supporto Full-Text Search.

## ⏱️ Setup in 5 Minuti

### Prerequisiti
- ✅ SQL Server 2019 o superiore (raccomandato 2022/2025)
- ✅ Full-Text Search installato
- ✅ Database DocN creato
- ✅ Permessi di creazione tabelle e indici

### Passo 1: Verifica Full-Text Search

```sql
SELECT FULLTEXTSERVICEPROPERTY('IsFullTextInstalled');
-- Risultato: 1 = OK, 0 = Installare Full-Text Search
```

### Passo 2: Crea il Database (se non esiste)

```sql
CREATE DATABASE DocN;
GO
USE DocN;
GO
```

### Passo 3: Esegui il Setup

**Opzione A - Script Master (Raccomandato):**
```bash
sqlcmd -S localhost -d DocN -E -i SetupDatabase.sql
```

**Opzione B - Script Individuali:**
```bash
sqlcmd -S localhost -d DocN -E -i 01_CreateIdentityTables.sql
sqlcmd -S localhost -d DocN -E -i 02_CreateDocumentTables.sql
sqlcmd -S localhost -d DocN -E -i 03_ConfigureFullTextSearch.sql
```

**Opzione C - PowerShell Helper:**
```powershell
.\RunSetup.ps1 -ServerName "localhost" -DatabaseName "DocN" -UseWindowsAuth
```

**Opzione D - Bash Helper:**
```bash
./run_setup.sh -s localhost -d DocN -u sa -p 'YourPassword'
```

### Passo 4: Verifica Setup

```bash
sqlcmd -S localhost -d DocN -E -i TestFullTextConfiguration.sql
```

Dovresti vedere: `✅ TUTTI I TEST SUPERATI!`

## 📋 Cosa Viene Creato

### Tabelle Identity (7 tabelle)
- AspNetRoles
- AspNetUsers
- AspNetUserClaims
- AspNetUserLogins
- AspNetUserTokens
- AspNetUserRoles
- AspNetRoleClaims

### Tabelle Documenti (3 tabelle)
- **Documents** - Tabella principale con full-text search
- **DocumentShares** - Condivisione documenti
- **DocumentTags** - Tag per categorizzazione

### Full-Text Search
- **Catalogo**: DocumentCatalog
- **Indice**: Su tabella Documents
- **Colonne indicizzate**: ExtractedText, Title, Description, Keywords, FileName
- **Lingua**: Italiano (LCID 1040)

## 🔍 Test Rapido Ricerca

Dopo il setup, prova la ricerca:

```sql
-- Inserisci un documento di test
INSERT INTO Documents (
    FileName, OriginalFileName, FilePath, FileSize, MimeType,
    ExtractedText, Title, Description, Category,
    UploadedBy, IsActive, IsPublic
)
VALUES (
    'test.pdf', 'Test.pdf', '/uploads/test.pdf', 1024, 'application/pdf',
    'Questo è un documento di test per la ricerca full-text',
    'Documento Test', 'Test della ricerca', 'Test',
    'user-id', 1, 0
);

-- Ricerca full-text
SELECT DocumentId, Title, FileName
FROM Documents
WHERE CONTAINS(ExtractedText, 'ricerca');
```

## 🐛 Problemi Comuni

### "Full-Text Search non installato"
```
Soluzione: Eseguire SQL Server Setup → Selezionare 
"Full-Text and Semantic Extractions for Search"
```

### "Errore 7653"
```
Causa: Chiave primaria non valida per full-text
Soluzione: Gli script forniti risolvono questo problema usando INT IDENTITY
```

### "Login failed"
```
Soluzione: Verificare credenziali o usare -E per Windows Authentication
```

### "Cannot open database"
```
Soluzione: Creare prima il database DocN
```

## 📚 Documentazione Completa

Per informazioni dettagliate:
- [README.md](README.md) - Documentazione completa
- [SOLUTION_EXPLAINED.md](SOLUTION_EXPLAINED.md) - Spiegazione approfondita della soluzione

## 💡 Suggerimenti

1. **Backup**: Fare sempre un backup prima di modifiche importanti
2. **Test**: Eseguire prima in ambiente di sviluppo
3. **Permessi**: Verificare di avere i permessi necessari
4. **Monitoraggio**: Controllare il popolamento dell'indice full-text

## ✅ Checklist Setup

- [ ] SQL Server installato e in esecuzione
- [ ] Full-Text Search installato
- [ ] Database DocN creato
- [ ] Script eseguiti senza errori
- [ ] Test di verifica superati
- [ ] Ricerca full-text funzionante

## 🎯 Prossimi Passi

Dopo il setup del database:
1. Creare i progetti .NET/Blazor
2. Configurare Entity Framework
3. Implementare l'upload documenti
4. Integrare l'estrazione testo
5. Configurare Azure OpenAI per embedding
6. Implementare la ricerca semantica

## 📧 Supporto

Per problemi o domande:
- Aprire un issue su GitHub
- Consultare la documentazione Microsoft
- Verificare i log di SQL Server

---

**Tempo stimato di setup**: 5-10 minuti  
**Difficoltà**: ⭐⭐☆☆☆ (Facile)  
**Prerequisiti**: Conoscenze base di SQL Server
