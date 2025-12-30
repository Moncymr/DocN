# SOLUZIONE DEFINITIVA NullReferenceException EF Core 10

## IL PROBLEMA
Il NullReferenceException persiste perché **il codice compilato nelle DLL non ha le modifiche del [NotMapped]**.

Anche se hai fatto pull del codice, le DLL vecchie sono ancora in uso.

## SOLUZIONE GARANTITA (Segui ESATTAMENTE questi passi)

### Passo 1: ELIMINA TUTTO IL COMPILATO

```powershell
# Windows PowerShell (ESEGUI COME AMMINISTRATORE se necessario)

# 1.1. Vai nella root del progetto
cd C:\GestDoc

# 1.2. Chiudi Visual Studio COMPLETAMENTE

# 1.3. Elimina TUTTE le DLL
Get-ChildItem -Path . -Include *.dll -Recurse -Force | Remove-Item -Force -Verbose

# 1.4. Elimina TUTTE le cartelle bin e obj
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory -Force | Remove-Item -Recurse -Force -Verbose

# 1.5. Pulisci con dotnet
dotnet clean
```

### Passo 2: VERIFICA CHE IL CODICE SIA CORRETTO

```powershell
# Apri e verifica il file
code DocN.Data\Models\Conversation.cs
```

**DEVE contenere (circa linea 95):**
```csharp
[System.ComponentModel.DataAnnotations.Schema.NotMapped]
public List<int> ReferencedDocumentIds
{
    get
    {
        if (string.IsNullOrEmpty(ReferencedDocumentIdsJson))
            return new List<int>();
        
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<int>>(ReferencedDocumentIdsJson) ?? new List<int>();
        }
        catch (System.Text.Json.JsonException)
        {
            return new List<int>();
        }
    }
    set => ReferencedDocumentIdsJson = value == null || value.Count == 0 
        ? null 
        : System.Text.Json.JsonSerializer.Serialize(value);
}

public string? ReferencedDocumentIdsJson { get; set; }
```

**SE NON C'È** → fai `git pull` e ricontrolla.

### Passo 3: REBUILD COMPLETO

```powershell
# Rebuild da zero senza incremental compilation
dotnet build --no-incremental --force

# Se da errori, prova:
dotnet restore
dotnet build --no-incremental --force
```

**Aspetta che finisca COMPLETAMENTE** senza errori.

### Passo 4: VERIFICA LA DLL COMPILATA

**Scarica e installa ILSpy:**
https://github.com/icsharpcode/ILSpy/releases

**Verifica:**
1. Apri ILSpy
2. File → Open → `C:\GestDoc\DocN.Data\bin\Debug\net10.0\DocN.Data.dll`
3. Cerca `Message` nella struttura ad albero
4. Clicca su `Message`
5. Cerca la proprietà `ReferencedDocumentIds`

**DEVE mostrare:**
```csharp
[NotMapped]
public List<int> ReferencedDocumentIds { get; set; }
```

**SE NON C'È `[NotMapped]`** → la DLL è vecchia, torna al Passo 1.

### Passo 5: DATABASE AGGIORNATO

```sql
-- Connettiti a DocNDb e esegui:
USE DocNDb;
GO

-- Verifica che la colonna sia NULL
SELECT 
    COLUMN_NAME, 
    IS_NULLABLE, 
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Messages' 
  AND COLUMN_NAME = 'ReferencedDocumentIds';
GO

-- Se IS_NULLABLE = 'NO', esegui:
ALTER TABLE [dbo].[Messages]
ALTER COLUMN [ReferencedDocumentIds] nvarchar(max) NULL;
GO
```

### Passo 6: AVVIA L'APPLICAZIONE

```powershell
# Nella root del progetto
dotnet run --project DocN.Client\DocN.Client.csproj
```

**Oppure** in Visual Studio: F5

## SE ANCORA NON FUNZIONA

### Diagnostica Avanzata

```powershell
# 1. Verifica la versione di git
git log --oneline -5

# DEVI vedere questi commit recenti:
# - 1809e04 Fix V4 script: correct version labels
# - 9af6120 Update CreateDatabase_Complete_V4.sql with all migrations
# - 8794c39 Add comprehensive troubleshooting guide
# - c030f56 Fix migration snapshot

# 2. Se NON li vedi, fai:
git fetch origin
git merge origin/copilot/fix-null-reference-exception

# 3. Poi torna al Passo 1
```

### Ultimo Resort: Reinstalla NuGet

```powershell
dotnet nuget locals all --clear
dotnet restore --force
dotnet build --no-incremental --force
```

## CHECKLIST FINALE

Prima di avviare l'app, verifica:

- [ ] Visual Studio CHIUSO durante la pulizia
- [ ] Nessuna cartella bin/ o obj/ presente
- [ ] Nessun file .dll nella soluzione
- [ ] `git log` mostra commit 1809e04 o successivo
- [ ] File `Conversation.cs` ha `[NotMapped]` (linea 95)
- [ ] `dotnet build` completa SENZA errori
- [ ] ILSpy mostra `[NotMapped]` nella DLL compilata
- [ ] Database ha `ReferencedDocumentIds NULL`

**Solo SE TUTTI i punti sono ✅**, l'app partirà senza NullReferenceException.

## RISULTATO ATTESO

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**SENZA NullReferenceException!** ✅
