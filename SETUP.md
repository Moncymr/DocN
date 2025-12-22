# DocN - Setup e Configurazione 🚀

## Panoramica

DocN è una soluzione web modulare basata su .NET 10 e Blazor, progettata per l'archiviazione intelligente e la consultazione di documenti, con ricerca semantica AI e integrazione Azure OpenAI.

## 🎯 Novità - Sistema di Autenticazione e Vector Embeddings

### Autenticazione Completa
- ✅ **Login** con email/password
- ✅ **Registrazione** utente con validazione
- ✅ **Recupero password** via email
- ✅ **Reset password** con token sicuro
- ✅ **UI moderna e veloce** con design minimalista
- ✅ **Mobile-friendly** e responsive

### Vector Embeddings Nativi SQL Server 2025
- ✅ **Tipo VECTOR(1536)** nativo per SQL Server 2025
- ✅ **Float array** in C# invece di stringhe JSON
- ✅ **Prestazioni ottimizzate** per ricerca semantica
- ✅ **Compatibilità** con text-embedding-ada-002 di Azure OpenAI

Per dettagli completi sull'API e configurazione embeddings, consulta [API_DOCUMENTATION.md](API_DOCUMENTATION.md).

## Prerequisiti

Prima di iniziare, assicurati di avere installato:

- **.NET 10.0 SDK** o versione successiva
- **SQL Server 2019** o versione successiva (oppure SQL Server LocalDB per lo sviluppo)
- **Visual Studio 2022** (v17.12+) o **Visual Studio Code** con C# Dev Kit
- **Azure OpenAI Service** (opzionale, per funzionalità AI)

## Passi per l'Installazione

### 1. Clonare il Repository

```bash
git clone <repository-url>
cd DocN
```

### 2. Configurare la Stringa di Connessione al Database

Apri il file `DocN.Client/appsettings.json` e modifica la stringa di connessione secondo il tuo ambiente:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

**Opzioni di configurazione:**

- **SQL Server LocalDB** (sviluppo): `Server=(localdb)\\mssqllocaldb;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true`
- **SQL Server Express**: `Server=.\\SQLEXPRESS;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true`
- **SQL Server con autenticazione**: `Server=your-server;Database=DocNDb;User Id=your-user;Password=your-password;MultipleActiveResultSets=true`

### 3. Installare le Dipendenze

```bash
dotnet restore
```

### 4. Creare il Database

**Opzione A: Usando lo script SQL (Raccomandato)**

```bash
# Eseguire lo script SQL sul server SQL Server
# Aprire SQL Server Management Studio (SSMS) o usare sqlcmd

sqlcmd -S NTSPJ-060-02\SQL2025 -i Database\CreateDatabase.sql
```

Lo script SQL include:
- Creazione di tutte le tabelle necessarie
- Creazione degli indici per le prestazioni
- **Migrazione automatica** per database esistenti (rende OwnerId nullable per supportare l'upload senza autenticazione)
- Configurazione AI di default

**Opzione B: Usando Entity Framework Migrations**

```bash
# Installare gli strumenti EF Core se non già presenti
dotnet tool install --global dotnet-ef

# Navigare alla cartella del progetto Client
cd DocN.Client

# Creare la migrazione iniziale
dotnet ef migrations add InitialCreate --project ../DocN.Data/DocN.Data.csproj

# Applicare la migrazione al database
dotnet ef database update --project ../DocN.Data/DocN.Data.csproj
```

**Nota per Database Esistenti:**
Se hai già creato il database con una versione precedente, lo script SQL include una sezione di migrazione che:
- Rende il campo `OwnerId` nullable nella tabella `Documents`
- Aggiorna il vincolo di chiave esterna per permettere upload senza autenticazione
- Mantiene tutti i dati esistenti intatti

### 5. Configurare le API Keys

Le chiavi API sono sensibili e non devono essere committate nel repository. Configurarle in uno dei seguenti modi:

**Opzione A: File appsettings.Development.json (Locale)**

Creare o modificare `DocN.Client/appsettings.Development.json`:

```json
{
  "OpenAI": {
    "ApiKey": "sk-proj-YOUR_OPENAI_KEY"
  },
  "Gemini": {
    "ApiKey": "AIzaSy-YOUR_GEMINI_KEY"
  },
  "Embeddings": {
    "ApiKey": "YOUR_AZURE_OPENAI_KEY"
  }
}
```

**Opzione B: Variabili d'Ambiente**

```bash
# Windows PowerShell
$env:OpenAI__ApiKey="sk-proj-YOUR_OPENAI_KEY"
$env:Gemini__ApiKey="AIzaSy-YOUR_GEMINI_KEY"
$env:Embeddings__ApiKey="YOUR_AZURE_OPENAI_KEY"

# Windows CMD
set OpenAI__ApiKey=sk-proj-YOUR_OPENAI_KEY
set Gemini__ApiKey=AIzaSy-YOUR_GEMINI_KEY
set Embeddings__ApiKey=YOUR_AZURE_OPENAI_KEY

# Linux/Mac
export OpenAI__ApiKey="sk-proj-YOUR_OPENAI_KEY"
export Gemini__ApiKey="AIzaSy-YOUR_GEMINI_KEY"
export Embeddings__ApiKey="YOUR_AZURE_OPENAI_KEY"
```

**Note:** Il file `appsettings.Development.json` è già in `.gitignore` per evitare il commit accidentale delle chiavi.

### 6. Creare la Cartella per i Documenti

Creare la cartella configurata per il salvataggio dei file caricati:

```bash
# Windows
mkdir C:\DocumentArchive\Uploads

# Linux/Mac  
mkdir -p ~/DocumentArchive/Uploads
```

### 7. Eseguire l'Applicazione

```bash
# Dalla cartella DocN.Client
dotnet run
```

L'applicazione sarà disponibile su: `http://localhost:5000`

## 🔐 Sistema di Autenticazione

### Primo Accesso

1. **Registrazione**: Vai su `/register` o clicca su "Register" nella homepage
   - Inserisci nome, cognome, email e password
   - La password deve contenere almeno 6 caratteri con maiuscole, minuscole e numeri
   - Dopo la registrazione, verrai automaticamente autenticato

2. **Login**: Vai su `/login` o clicca su "Login" 
   - Usa l'email e la password con cui ti sei registrato
   - Opzione "Remember me" per sessioni persistenti

3. **Recupero Password**: In caso di password dimenticata
   - Vai su `/forgot-password`
   - Inserisci la tua email
   - Segui le istruzioni per reimpostare la password (per ora il token viene stampato in console per testing)

### Gestione Utenti e Ruoli

Per estendere il sistema con ruoli personalizzati, modifica `Program.cs`:

```csharp
// Aggiungi ruoli personalizzati
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Configura policy password
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;  // Aumenta lunghezza minima
    options.Password.RequireNonAlphanumeric = true;  // Richiedi caratteri speciali
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
```

Per creare ruoli:

```csharp
// In Program.cs dopo app.Build()
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    
    string[] roles = { "Admin", "Editor", "Viewer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}
```

## 🔢 Configurazione Vector Embeddings

### Implementazione Corrente

**Nota Importante:** DocN attualmente utilizza un convertitore di valori per memorizzare gli embeddings come stringhe CSV in colonne `nvarchar(max)`. Questa è una soluzione temporanea fino a quando il tipo VECTOR di SQL Server 2025 non sarà completamente supportato da Entity Framework Core.

**Modello C# Aggiornato:**

Il campo `EmbeddingVector` è di tipo `float[]`:

```csharp
public class Document
{
    // ... altri campi
    
    // Vector embedding per ricerca semantica (1536 dimensioni per text-embedding-ada-002)
    public float[]? EmbeddingVector { get; set; }
}
```

**Configurazione Database:**

```sql
-- Colonna attuale
ALTER TABLE Documents 
ADD EmbeddingVector NVARCHAR(MAX) NULL;
```

**Conversione Automatica:**

Entity Framework gestisce automaticamente la conversione tra `float[]` in C# e stringhe CSV nel database:

```csharp
// Nel codice
document.EmbeddingVector = new float[] { 0.1f, 0.2f, 0.3f, ... };

// Nel database viene memorizzato come
// "0.1,0.2,0.3,..."
```

### SQL Server 2025 - Supporto Vettoriale Nativo (Futuro)

Quando Entity Framework Core supporterà nativamente il tipo VECTOR di SQL Server 2025, si potrà migrare a:

**Configurazione Futura:**
```sql
ALTER TABLE Documents 
ALTER COLUMN EmbeddingVector VECTOR(1536) NULL;
```

**Verifica Supporto Vettoriale:**
```sql
SELECT SERVERPROPERTY('IsVectorSupported') as VectorSupported;
```

**Configurazione Colonna Embedding:**
```sql
ALTER TABLE Documents 
ADD EmbeddingVector VECTOR(1536) NULL;
```

**Il modello C# rimane invariato** - continua ad utilizzare `float[]`:

```csharp
public class Document
{
    // ... altri campi
    
    // Vector embedding per ricerca semantica (1536 dimensioni per text-embedding-ada-002)
    public float[]? EmbeddingVector { get; set; }
}
```

### Generazione Embeddings

Gli embeddings vengono generati automaticamente durante l'upload:

```csharp
var embeddingService = serviceProvider.GetRequiredService<IEmbeddingService>();
float[]? embedding = await embeddingService.GenerateEmbeddingAsync(documentText);
```

### Ricerca Semantica

Cerca documenti simili usando linguaggio naturale:

```csharp
var query = "contratti di licenza software";
var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query);
var results = await embeddingService.SearchSimilarDocumentsAsync(queryEmbedding, topK: 10);
```

**Dettagli completi nell'[API Documentation](API_DOCUMENTATION.md)**.

## Configurazione delle Funzionalità AI

### Configurare Azure OpenAI

1. Accedi all'applicazione e vai su **Configurazione** (`/config`)
2. Compila i seguenti campi:

- **Azure OpenAI Endpoint**: `https://your-resource.openai.azure.com/`
- **Azure OpenAI API Key**: La tua chiave API
- **Chat Deployment Name**: Nome del deployment del modello GPT (es. `gpt-4`)
- **Embedding Deployment Name**: Nome del deployment del modello di embedding (es. `text-embedding-ada-002`)

3. Configura i parametri RAG:

- **Max Documents to Retrieve**: Numero di documenti rilevanti da recuperare (default: 5)
- **Similarity Threshold**: Soglia di similarità 0.0-1.0 (default: 0.7)
- **Max Tokens for Context**: Token massimi per le risposte (default: 4000)

4. Salva la configurazione e testa la connessione

## Funzionalità Principali

### 1. 👥 Gestione Utenti

- Registrazione e autenticazione utenti
- Profili utente personalizzabili
- Gestione ruoli e permessi

### 2. 📤 Caricamento e Organizzazione Documenti

- Upload di documenti multipli
- **Suggerimento automatico della categoria** con spiegazione AI del ragionamento
- Estrazione automatica del testo
- Tag e metadati personalizzabili

### 3. 🔍 Ricerca Semantica

- Ricerca basata su embeddings vettoriali
- Ricerca in linguaggio naturale
- Ranking dei risultati per rilevanza

### 4. 🤖 RAG (Retrieval Augmented Generation)

- Risposte AI basate sul contenuto dei documenti
- Citazione automatica delle fonti
- Contestualizzazione intelligente

### 5. 👁️ Gestione Visibilità Documenti

Ogni documento può avere diversi livelli di visibilità:

- **Private**: Solo il proprietario può accedere
- **Shared**: Condiviso con utenti specifici
- **Organization**: Accessibile a tutti i membri dell'organizzazione
- **Public**: Visibile a tutti

### 6. ⬇️ Download Documenti

- Download di documenti con controllo dei permessi
- Tracciamento degli accessi
- Statistiche di utilizzo

### 7. 📊 Dashboard e Analytics

- **Statistiche di utilizzo**: Totale documenti, storage utilizzato, upload recenti
- **Analisi per categoria**: Distribuzione dei documenti per categoria
- **Analisi per tipo**: Distribuzione per tipo di file
- **Documenti più acceduti**: Classifica dei documenti più consultati
- **Suggerimenti di ottimizzazione**: Raccomandazioni AI per migliorare l'organizzazione

### 8. ⚙️ Configurazione Servizi AI

Sezione dedicata alla configurazione di:

- Servizi di embedding (Azure OpenAI)
- Parametri RAG
- Prompt di sistema personalizzabili

## Struttura del Progetto

```
DocN/
├── DocN.sln                        # Solution file
├── DocN.Client/                    # Applicazione Blazor Server
│   ├── Components/
│   │   ├── Pages/                  # Pagine Razor
│   │   │   ├── Home.razor         # Homepage
│   │   │   ├── Documents.razor    # Lista documenti con paginazione
│   │   │   ├── Dashboard.razor    # Dashboard analytics
│   │   │   └── AIConfig.razor     # Configurazione AI
│   │   └── Layout/                 # Layout componenti
│   ├── Program.cs                  # Configurazione app
│   └── appsettings.json            # Configurazione
│
├── DocN.Data/                      # Data Layer
│   ├── Models/                     # Modelli di dominio
│   │   ├── ApplicationUser.cs     # Utente con Identity
│   │   ├── Document.cs            # Documento con visibilità
│   │   ├── DocumentShare.cs       # Condivisione documenti
│   │   ├── AIConfiguration.cs     # Configurazione AI
│   │   └── DocumentStatistics.cs  # Statistiche
│   ├── Services/                   # Servizi applicativi
│   │   ├── DocumentService.cs     # Gestione documenti e download
│   │   ├── EmbeddingService.cs    # Generazione embeddings
│   │   ├── RAGService.cs          # Retrieval Augmented Generation
│   │   ├── CategoryService.cs     # Suggerimento categoria con reasoning
│   │   └── DocumentStatisticsService.cs  # Analytics
│   └── ApplicationDbContext.cs     # Context EF Core
│
└── README.md                       # Documentazione
```

## Gestione Database con Molti Documenti

DocN è progettato per gestire **grandi quantità di documenti** in modo efficiente:

### Ottimizzazioni Implementate:

1. **Paginazione**: Lista documenti con paginazione (20 documenti per pagina)
2. **Indicizzazione**: Indici su colonne chiave per query veloci
3. **Query Ottimizzate**: Uso di `Skip()` e `Take()` per caricamento incrementale
4. **Lazy Loading**: Caricamento dati solo quando necessari
5. **Statistiche Efficienti**: Aggregazioni SQL ottimizzate

### Raccomandazioni per Database di Grandi Dimensioni:

- Utilizzare **SQL Server 2022** con supporto vettoriale nativo per embeddings
- Configurare **indici full-text** per ricerca testuale rapida
- Implementare **partitioning** per tabelle con milioni di record
- Usare **Azure SQL Database** con scalabilità automatica per carichi variabili

## Sicurezza

### Best Practices Implementate:

- ✅ **Autenticazione**: ASP.NET Core Identity
- ✅ **Password Policy**: Requisiti di complessità configurabili
- ✅ **Controllo Accessi**: Verifica permessi per ogni operazione
- ✅ **API Key Protection**: Configurazione sensibile in variabili d'ambiente
- ✅ **SQL Injection Prevention**: Entity Framework con parametrizzazione
- ✅ **HTTPS**: Configurabile per produzione

### Configurazione Produzione:

```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=DocNDb;User Id=app_user;Password=${DB_PASSWORD};Encrypt=True;TrustServerCertificate=False"
  }
}
```

Usa variabili d'ambiente per dati sensibili:

```bash
export DB_PASSWORD="your-secure-password"
export AZURE_OPENAI_KEY="your-api-key"
```

## Troubleshooting

### Errore di Connessione al Database

```
Cannot open database "DocNDb" requested by the login
```

**Soluzione**: Verifica che SQL Server sia in esecuzione e che la stringa di connessione sia corretta.

### Errore Migrazioni Entity Framework

```
Build failed
```

**Soluzione**: Assicurati che tutti i pacchetti NuGet siano installati:

```bash
dotnet restore
dotnet build
```

### Errore Azure OpenAI

```
AI service not configured
```

**Soluzione**: Vai su `/config` e configura correttamente endpoint e API key di Azure OpenAI.

### Performance Lente con Molti Documenti

**Soluzione**:
1. Verifica che gli indici siano creati correttamente
2. Aumenta il page size se necessario
3. Considera l'uso di caching per statistiche
4. Abilita query logging per identificare bottleneck

## Sviluppo

### Aggiungere una Nuova Funzionalità

1. Creare il modello in `DocN.Data/Models/`
2. Aggiornare `ApplicationDbContext.cs`
3. Creare migrazione: `dotnet ef migrations add NomeFunzionalita`
4. Applicare migrazione: `dotnet ef database update`
5. Creare servizio in `DocN.Data/Services/`
6. Creare pagina Razor in `DocN.Client/Components/Pages/`

### Eseguire i Test

```bash
dotnet test
```

## Supporto e Contributi

Per domande, problemi o suggerimenti:

- Aprire una Issue su GitHub
- Consultare la documentazione di Azure OpenAI
- Verificare la compatibilità con .NET 9.0

## Licenza

Questo progetto è rilasciato sotto licenza MIT.

---

**Nota**: Assicurati di configurare correttamente Azure OpenAI per utilizzare tutte le funzionalità AI. L'applicazione funziona anche senza AI, ma con funzionalità limitate.
