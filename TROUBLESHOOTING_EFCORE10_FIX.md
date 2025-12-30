# Troubleshooting Guide: EF Core 10 NullReferenceException Fix

## Il Problema
`System.NullReferenceException` in `FindCollectionMapping` durante l'inizializzazione del modello EF Core.

## Verifica che hai applicato TUTTE le modifiche

### 1. Verifica il codice Message.cs
Controlla che `DocN.Data/Models/Conversation.cs` contenga:

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

### 2. Verifica ApplicationDbContext.cs
Controlla che `DocN.Data/ApplicationDbContext.cs` nella sezione Message contenga:

```csharp
entity.Property(e => e.ReferencedDocumentIdsJson)
    .HasColumnName("ReferencedDocumentIds")
    .HasColumnType("nvarchar(max)")
    .IsRequired(false);
```

**NON** deve contenere:
- `entity.Property(e => e.ReferencedDocumentIds)` 
- `ValueConverter`
- `SetElementType`
- `SetProviderClrType`

### 3. Verifica ApplicationDbContextModelSnapshot.cs
In `DocN.Data/Migrations/ApplicationDbContextModelSnapshot.cs`, cerca "ReferencedDocumentIds":

```csharp
b.Property<string>("ReferencedDocumentIds")
    .HasColumnType("nvarchar(max)");  // NO .IsRequired()!
```

## Passi di Risoluzione COMPLETI

### Passo 1: Verifica versione repository
```bash
git status
git log --oneline -3
```

Dovresti vedere questi commit recenti:
- `0aa70fc` Add CreateDatabase_Complete_V4.sql
- `f31fcd7` Remove hardcoded database name
- `c030f56` Fix migration snapshot

Se NON li vedi, fai:
```bash
git pull origin nome-branch
```

### Passo 2: Pulizia COMPLETA
```bash
# Chiudi Visual Studio / Rider / VS Code completamente

# Pulisci la solution
dotnet clean

# Elimina TUTTE le cartelle bin e obj manualmente
# Windows PowerShell:
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | Remove-Item -Recurse -Force

# Linux/Mac:
find . -type d -name "bin" -o -name "obj" | xargs rm -rf

# Elimina anche la cache NuGet locale (opzionale ma consigliato)
dotnet nuget locals all --clear
```

### Passo 3: Ricompilazione COMPLETA
```bash
# Restore dei pacchetti
dotnet restore

# Build completo
dotnet build --no-incremental
```

### Passo 4: Verifica Database
Se usi SQL Server, connettiti al database e verifica:

```sql
-- Controlla se la colonna esiste ed è nullable
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length,
    c.is_nullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('Messages')
  AND c.name = 'ReferencedDocumentIds';
```

Se `is_nullable = 0`, devi eseguire:
```sql
ALTER TABLE [dbo].[Messages]
ALTER COLUMN [ReferencedDocumentIds] nvarchar(max) NULL;
```

### Passo 5: Elimina database e ricrea (se possibile)
Se stai in sviluppo e puoi perdere i dati:

```sql
DROP DATABASE DocNDb;
GO
```

Poi usa lo script completo:
```sql
-- Esegui Database/CreateDatabase_Complete_V4.sql
```

### Passo 6: Riavvio completo
1. Chiudi tutti i terminali
2. Chiudi l'IDE completamente
3. Riavvia il computer (seriamente, aiuta con cache di Visual Studio)
4. Riapri il progetto
5. Fai un build pulito

## Test di Verifica

Dopo aver fatto tutti i passi, testa:

```bash
cd DocN.Server.Tests
dotnet test --filter "ApplicationDbContext_CanBeInitialized_WithoutException"
```

Dovrebbe passare senza errori.

## Ancora Non Funziona?

### Verifica 1: Stai usando il progetto giusto?
Assicurati di eseguire `DocN.Client` o `DocN.Server`, non progetti vecchi.

### Verifica 2: Controlla la connection string
Nel file `appsettings.json`, verifica che punti al database corretto.

### Verifica 3: File DLL compilati in cache
```bash
# Elimina tutto nella cartella di pubblicazione
rm -rf bin/Release/*
rm -rf obj/Release/*
```

### Verifica 4: Compila in modalità Release
```bash
dotnet build -c Release
```

### Verifica 5: Usa ILSpy o dnSpy
Apri la DLL compilata (`DocN.Data.dll`) e verifica che:
1. `Message.ReferencedDocumentIds` abbia l'attributo `[NotMapped]`
2. `Message.ReferencedDocumentIdsJson` esista come proprietà

## Log Dettagliati

Se ancora non funziona, abilita il logging dettagliato di EF Core:

```csharp
// In Program.cs o Startup.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString)
           .EnableSensitiveDataLogging()
           .EnableDetailedErrors()
           .LogTo(Console.WriteLine, LogLevel.Debug);
});
```

Questo ti darà più informazioni su cosa sta succedendo.

## Contatta il Supporto

Se dopo TUTTI questi passi hai ancora l'errore, fornisci:
1. Output di `git log --oneline -5`
2. Contenuto completo di `Message.cs` (dalla riga 90 alla 125)
3. Screenshot della DLL aperta in ILSpy che mostra la proprietà `ReferencedDocumentIds`
4. Output completo dello stack trace dell'errore
5. Versione di .NET: `dotnet --version`
6. Versione di EF Core dal file `.csproj`

## Checklist Finale

- [ ] Ho fatto `git pull` per ottenere le ultime modifiche
- [ ] Ho eliminato TUTTE le cartelle bin/ e obj/
- [ ] Ho fatto `dotnet clean`
- [ ] Ho fatto `dotnet build --no-incremental`  
- [ ] Ho verificato che `Message.ReferencedDocumentIds` abbia `[NotMapped]`
- [ ] Ho verificato che `ReferencedDocumentIdsJson` esista in Message.cs
- [ ] Ho verificato che ApplicationDbContext NON configuri `ReferencedDocumentIds`
- [ ] Ho aggiornato il database con lo script SQL
- [ ] Ho riavviato l'IDE completamente
- [ ] Ho testato con un database nuovo (se possibile)
- [ ] Il test `ApplicationDbContext_CanBeInitialized_WithoutException` passa

Se tutti questi punti sono ✅ e hai ancora l'errore, c'è qualcosa di molto strano nel tuo ambiente.
