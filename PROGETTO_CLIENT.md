# DocN.Client - Documentazione Tecnica

## Indice
1. [Panoramica Progetto](#panoramica-progetto)
2. [Scopo e Funzionalità](#scopo-e-funzionalità)
3. [Architettura](#architettura)
4. [Tecnologie Utilizzate](#tecnologie-utilizzate)
5. [Struttura del Progetto](#struttura-del-progetto)
6. [Componenti Principali](#componenti-principali)
7. [Routing e Navigazione](#routing-e-navigazione)
8. [Integrazione con Backend](#integrazione-con-backend)

---

## Panoramica Progetto

**DocN.Client** è l'applicazione frontend della soluzione DocN, realizzata con **Blazor Server**. Fornisce l'interfaccia utente completa per la gestione documentale, la ricerca semantica e le conversazioni AI con i documenti.

### Informazioni di Base
- **Tipo**: Blazor Server Application
- **Target Framework**: .NET 10.0
- **Porta Predefinita**: 7114 (HTTPS)
- **Ruolo**: Presentation Layer + User Interface
- **Dipendenze**: DocN.Data (che include DocN.Core)

---

## Scopo e Funzionalità

### Scopo Principale

DocN.Client serve come **interfaccia utente principale** dell'applicazione, offrendo:

1. **Interfaccia Web Moderna**
   - UI responsive basata su Bootstrap 5
   - Componenti interattivi real-time con SignalR
   - Progressive Web App capabilities

2. **Gestione Utenti**
   - Registrazione e login con ASP.NET Core Identity
   - Gestione profilo e organizzazione
   - Multi-tenancy awareness

3. **Document Management UI**
   - Upload documenti con drag & drop
   - Lista e ricerca documenti
   - Visualizzazione dettagli e metadati
   - Modifica e eliminazione documenti

4. **Ricerca e Chat**
   - Interfaccia ricerca semantica e ibrida
   - Chat conversazionale con RAG
   - Visualizzazione risultati con highlighting

5. **Configurazione Sistema**
   - UI amministrazione provider AI
   - Configurazione parametri RAG
   - Gestione agenti AI

### Funzionalità Specifiche

#### 1. Dashboard Interattiva
- Statistiche real-time documenti
- Grafici visualizzazione dati (Chart.js integration)
- Widget documenti recenti
- Notifiche sistema

#### 2. Upload Documenti Avanzato
- Drag & drop multi-file
- Progress bar upload
- Elaborazione AI opzionale (tag, metadata extraction)
- Preview file prima upload

#### 3. Ricerca Intelligente
- Search-as-you-type con debouncing
- Filtri avanzati (categoria, data, tipo file)
- Switch tra modalità ricerca (text, semantic, hybrid)
- Highlighting keywords nei risultati

#### 4. Chat RAG Interattiva
- Real-time streaming responses (SignalR)
- Cronologia conversazioni
- Citazioni clickabili ai documenti fonte
- Configurazione parametri chat runtime

#### 5. Gestione Agenti
- Wizard creazione agente step-by-step
- Test agente interattivo
- Template predefiniti
- Gestione agenti esistenti

---

## Architettura

### Blazor Server Architecture

DocN.Client utilizza **Blazor Server**, che offre:

**Vantaggi:**
- **Full .NET**: Codice C# sia client che server
- **Real-time**: SignalR per comunicazione bidirezionale
- **Performance**: Rendering server-side, banda ridotta
- **Sicurezza**: Business logic rimane sul server

**Flusso:**
```
Browser ←--SignalR WebSocket--→ Blazor Server
                                      ↓
                                 Razor Components
                                      ↓
                                 Services (DI)
                                      ↓
                              DocN.Data Layer
                                      ↓
                                  Database
```

### Component-Based Architecture

```
App.razor (Root)
├── MainLayout.razor
│   ├── NavMenu.razor
│   └── LoginDisplay.razor
├── Pages/
│   ├── Home.razor
│   ├── Documents.razor
│   ├── Upload.razor
│   ├── Search.razor
│   ├── Chat.razor
│   ├── AIConfig.razor
│   └── Agents.razor
└── Components/
    ├── AgentWizard/
    └── Shared/
```

### Pattern Implementati

1. **Component-Based**: UI divisa in componenti riutilizzabili
2. **Dependency Injection**: Servizi iniettati in componenti
3. **Code-Behind**: Logic separata da UI (partial classes)
4. **State Management**: Cascading parameters per stato condiviso
5. **Authorization**: Attribute-based authorization

---

## Tecnologie Utilizzate

### Framework Core

#### 1. Blazor Server (.NET 10.0)
**Scopo**: Framework UI interattivo con C#

**Caratteristiche:**
- Component model simile a React
- Two-way data binding
- Event handling in C#
- No JavaScript required (opzionale per interop)

**Esempio componente:**
```razor
@page "/documents"
@inject IDocumentService DocumentService

<h3>Documenti</h3>

@if (documents == null)
{
    <p>Caricamento...</p>
}
else
{
    <table class="table">
        @foreach (var doc in documents)
        {
            <tr>
                <td>@doc.Title</td>
                <td>@doc.UploadedAt.ToShortDateString()</td>
            </tr>
        }
    </table>
}

@code {
    private List<Document>? documents;
    
    protected override async Task OnInitializedAsync()
    {
        documents = await DocumentService.GetDocumentsAsync();
    }
}
```

#### 2. ASP.NET Core Identity (v10.0.0)
**Scopo**: Autenticazione e autorizzazione

**Componenti utilizzati:**
- `SignInManager<User>`: Gestione login/logout
- `UserManager<User>`: Gestione utenti
- `RoleManager<Role>`: Gestione ruoli
- `AuthenticationStateProvider`: Stato autenticazione nei componenti

**Implementazione:**
```csharp
// In Razor component
[Authorize]
public partial class Documents : ComponentBase
{
    [CascadingParameter]
    private Task<AuthenticationState> AuthenticationStateTask { get; set; }
    
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;
        var user = authState.User;
        
        if (user.Identity.IsAuthenticated)
        {
            // Carica documenti utente
        }
    }
}
```

#### 3. Entity Framework Core (v10.0.1)
**Scopo**: ORM per accesso database da UI

**Utilizzo in Client:**
- DbContext injection per operazioni CRUD dirette
- Operazioni semplici senza chiamare backend API
- Per operazioni complesse, delega a DocN.Server API

### UI Framework

#### 4. Bootstrap 5
**Scopo**: Framework CSS per UI responsive

**Componenti utilizzati:**
- Grid system (container, row, col)
- Cards e panels
- Forms e input groups
- Modals e alerts
- Navigation (navbar, sidebar)
- Buttons e icons

**Esempio:**
```razor
<div class="container">
    <div class="row">
        <div class="col-md-6">
            <div class="card">
                <div class="card-header">Upload Documento</div>
                <div class="card-body">
                    <input type="file" class="form-control" />
                </div>
            </div>
        </div>
    </div>
</div>
```

#### 5. Font Awesome / Bootstrap Icons
**Scopo**: Icone per UI

**Utilizzo:**
```razor
<button class="btn btn-primary">
    <i class="fas fa-upload"></i> Upload
</button>
```

### Client-Side Libraries (wwwroot)

#### 6. SignalR Client
**Scopo**: Comunicazione real-time WebSocket

**Utilizzo:**
- Già integrato in Blazor Server
- Gestisce automaticamente riconnessione
- Usato per streaming chat responses

#### 7. JavaScript Interop (opzionale)
**Scopo**: Funzionalità JavaScript quando necessario

**Esempi uso:**
- Drag & drop file avanzato
- Chart.js per grafici
- Clipboard API
- Local storage

**Implementazione:**
```csharp
@inject IJSRuntime JSRuntime

private async Task CopyToClipboard(string text)
{
    await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
}
```

---

## Struttura del Progetto

```
DocN.Client/
│
├── Program.cs                           # Entry point e configurazione
│
├── Components/
│   ├── App.razor                        # Root component
│   ├── Routes.razor                     # Routing configuration
│   ├── _Imports.razor                   # Using statements globali
│   │
│   ├── Layout/
│   │   ├── MainLayout.razor             # Layout principale
│   │   ├── NavMenu.razor                # Menu navigazione laterale
│   │   └── LoginDisplay.razor           # Widget login/logout
│   │
│   ├── Pages/
│   │   ├── Home.razor                   # Homepage/Landing
│   │   ├── Dashboard.razor              # Dashboard admin
│   │   │
│   │   ├── Auth/
│   │   │   ├── Login.razor              # Pagina login
│   │   │   ├── Register.razor           # Registrazione utente
│   │   │   ├── ForgotPassword.razor     # Recupero password
│   │   │   └── ResetPassword.razor      # Reset password
│   │   │
│   │   ├── Documents.razor              # Lista documenti
│   │   ├── Upload.razor                 # Upload documenti
│   │   ├── Search.razor                 # Ricerca documenti
│   │   │
│   │   ├── Chat.razor                   # Chat con documenti
│   │   │
│   │   ├── Config/
│   │   │   ├── AIConfig.razor           # Configurazione AI
│   │   │   └── Agents.razor             # Gestione agenti
│   │   │
│   │   └── AgentWizard/
│   │       ├── Step1_ChooseTemplate.razor
│   │       ├── Step2_ConfigureProvider.razor
│   │       ├── Step3_Customize.razor
│   │       ├── Step4_Test.razor
│   │       └── Step5_Complete.razor
│   │
│   └── Shared/
│       ├── DocumentCard.razor           # Card singolo documento
│       ├── SearchFilters.razor          # Componente filtri ricerca
│       ├── ChatMessage.razor            # Messaggio chat singolo
│       └── LoadingSpinner.razor         # Spinner caricamento
│
├── wwwroot/                             # Static files
│   ├── css/
│   │   ├── bootstrap/
│   │   ├── app.css                      # Custom styles
│   │   └── site.css
│   ├── js/
│   │   ├── site.js                      # Custom JavaScript
│   │   └── interop.js                   # JS Interop functions
│   ├── images/
│   └── favicon.ico
│
├── appsettings.json                     # Configurazione applicazione
├── appsettings.Development.json         # Config development
│
└── DocN.Client.csproj                   # Project file
```

---

## Componenti Principali

### 1. Home.razor

**Scopo:** Landing page dell'applicazione con panoramica funzionalità.

**Funzionalità:**
- Hero section con CTA
- Features overview
- Quick links per funzionalità principali
- Statistics overview (se autenticato)

**Struttura:**
```razor
@page "/"
@inject AuthenticationStateProvider AuthenticationStateProvider

<PageTitle>DocN - Sistema RAG Documentale</PageTitle>

@if (isAuthenticated)
{
    <!-- Dashboard per utenti autenticati -->
    <Dashboard />
}
else
{
    <!-- Landing page per visitatori -->
    <div class="hero">
        <h1>Benvenuto in DocN</h1>
        <p>Sistema intelligente di gestione documentale con AI</p>
        <a href="/login" class="btn btn-primary">Accedi</a>
        <a href="/register" class="btn btn-secondary">Registrati</a>
    </div>
    
    <div class="features">
        <FeatureCard 
            Icon="fas fa-search" 
            Title="Ricerca Semantica"
            Description="Trova documenti usando linguaggio naturale" />
        <!-- Altri features -->
    </div>
}

@code {
    private bool isAuthenticated;
    
    /// <summary>
    /// Inizializza la home page verificando stato autenticazione
    /// </summary>
    /// <output>Rendering condizionale basato su autenticazione</output>
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;
    }
}
```

### 2. Documents.razor

**Scopo:** Lista e gestione documenti utente.

**Funzionalità:**
- Tabella documenti con sorting e paging
- Filtri (categoria, data, visibilità)
- Azioni: View, Edit, Delete, Share, Download
- Selezione multipla per azioni batch

**Implementazione chiave:**
```razor
@page "/documents"
@inject IDocumentService DocumentService
@inject NavigationManager Navigation
@attribute [Authorize]

<PageTitle>Documenti</PageTitle>

<div class="documents-container">
    <!-- Header con azioni -->
    <div class="d-flex justify-content-between mb-3">
        <h3>I Miei Documenti</h3>
        <button class="btn btn-primary" @onclick="NavigateToUpload">
            <i class="fas fa-upload"></i> Nuovo Documento
        </button>
    </div>
    
    <!-- Filtri -->
    <div class="filters mb-3">
        <input type="text" class="form-control" 
               placeholder="Cerca..." 
               @bind="searchQuery" 
               @bind:event="oninput"
               @oninput="OnSearchChanged" />
        
        <select class="form-select" @bind="selectedCategory">
            <option value="">Tutte le categorie</option>
            @foreach (var cat in categories)
            {
                <option value="@cat.Id">@cat.Name</option>
            }
        </select>
    </div>
    
    <!-- Tabella documenti -->
    @if (isLoading)
    {
        <LoadingSpinner />
    }
    else if (documents == null || !documents.Any())
    {
        <div class="alert alert-info">Nessun documento trovato</div>
    }
    else
    {
        <table class="table table-hover">
            <thead>
                <tr>
                    <th><input type="checkbox" @onchange="ToggleSelectAll" /></th>
                    <th @onclick='() => Sort("Title")'>
                        Titolo <i class="fas fa-sort"></i>
                    </th>
                    <th>Categoria</th>
                    <th @onclick='() => Sort("UploadedAt")'>
                        Data Upload <i class="fas fa-sort"></i>
                    </th>
                    <th>Dimensione</th>
                    <th>Azioni</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var doc in documents)
                {
                    <tr class="@(selectedDocuments.Contains(doc.Id) ? "selected" : "")">
                        <td>
                            <input type="checkbox" 
                                   checked="@selectedDocuments.Contains(doc.Id)"
                                   @onchange="e => ToggleDocument(doc.Id, e.Value)" />
                        </td>
                        <td>
                            <a href="/document/@doc.Id">@doc.Title</a>
                        </td>
                        <td>
                            <span class="badge bg-secondary">@doc.Category?.Name</span>
                        </td>
                        <td>@doc.UploadedAt.ToString("dd/MM/yyyy HH:mm")</td>
                        <td>@FormatFileSize(doc.FileSize)</td>
                        <td>
                            <button class="btn btn-sm btn-primary" 
                                    @onclick="() => ViewDocument(doc.Id)">
                                <i class="fas fa-eye"></i>
                            </button>
                            <button class="btn btn-sm btn-danger" 
                                    @onclick="() => DeleteDocument(doc.Id)">
                                <i class="fas fa-trash"></i>
                            </button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
        
        <!-- Paginazione -->
        <nav>
            <ul class="pagination">
                <li class="page-item @(currentPage == 1 ? "disabled" : "")">
                    <a class="page-link" @onclick="() => ChangePage(currentPage - 1)">
                        Previous
                    </a>
                </li>
                @for (int i = 1; i <= totalPages; i++)
                {
                    var page = i;
                    <li class="page-item @(currentPage == page ? "active" : "")">
                        <a class="page-link" @onclick="() => ChangePage(page)">
                            @page
                        </a>
                    </li>
                }
                <li class="page-item @(currentPage == totalPages ? "disabled" : "")">
                    <a class="page-link" @onclick="() => ChangePage(currentPage + 1)">
                        Next
                    </a>
                </li>
            </ul>
        </nav>
    }
</div>

@code {
    private List<Document>? documents;
    private List<Category> categories = new();
    private HashSet<int> selectedDocuments = new();
    private string searchQuery = "";
    private int? selectedCategory;
    private bool isLoading = true;
    
    private int currentPage = 1;
    private int pageSize = 20;
    private int totalPages;
    private string sortColumn = "UploadedAt";
    private bool sortAscending = false;
    
    /// <summary>
    /// Inizializza pagina documenti caricando lista e categorie
    /// </summary>
    /// <output>Lista documenti paginata e filtrata</output>
    protected override async Task OnInitializedAsync()
    {
        await LoadCategoriesAsync();
        await LoadDocumentsAsync();
    }
    
    /// <summary>
    /// Carica documenti con filtri e ordinamento applicati
    /// </summary>
    private async Task LoadDocumentsAsync()
    {
        isLoading = true;
        
        var result = await DocumentService.GetDocumentsAsync(new DocumentFilter
        {
            SearchQuery = searchQuery,
            CategoryId = selectedCategory,
            PageNumber = currentPage,
            PageSize = pageSize,
            SortBy = sortColumn,
            SortAscending = sortAscending
        });
        
        documents = result.Items;
        totalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize);
        isLoading = false;
        
        StateHasChanged();
    }
    
    /// <summary>
    /// Gestisce ricerca con debouncing per evitare troppe chiamate
    /// </summary>
    private Timer? debounceTimer;
    
    private void OnSearchChanged(ChangeEventArgs e)
    {
        searchQuery = e.Value?.ToString() ?? "";
        
        // Debounce: attendi 500ms dopo ultima digitazione
        debounceTimer?.Dispose();
        debounceTimer = new Timer(async _ =>
        {
            await InvokeAsync(async () =>
            {
                currentPage = 1;
                await LoadDocumentsAsync();
            });
        }, null, 500, Timeout.Infinite);
    }
    
    /// <summary>
    /// Ordina documenti per colonna specificata
    /// </summary>
    private async Task Sort(string column)
    {
        if (sortColumn == column)
        {
            sortAscending = !sortAscending;
        }
        else
        {
            sortColumn = column;
            sortAscending = true;
        }
        
        await LoadDocumentsAsync();
    }
    
    /// <summary>
    /// Elimina documento con conferma utente
    /// </summary>
    private async Task DeleteDocument(int id)
    {
        var confirmed = await JSRuntime.InvokeAsync<bool>(
            "confirm", 
            "Sei sicuro di voler eliminare questo documento?"
        );
        
        if (confirmed)
        {
            await DocumentService.DeleteDocumentAsync(id);
            await LoadDocumentsAsync();
        }
    }
    
    /// <summary>
    /// Formatta dimensione file in KB/MB/GB
    /// </summary>
    /// <output>Stringa formattata (es: "2.5 MB")</output>
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }
}
```

### 3. Upload.razor

**Scopo:** Upload documenti con elaborazione AI opzionale.

**Funzionalità:**
- Drag & drop multi-file
- Preview file selezionati
- Form metadati (titolo, descrizione, categoria, tag)
- Opzioni AI (extract tags, metadata, generate embeddings)
- Progress bar upload
- Validation input

**Implementazione chiave:**
```razor
@page "/upload"
@inject IDocumentService DocumentService
@inject NavigationManager Navigation
@attribute [Authorize]

<PageTitle>Upload Documento</PageTitle>

<div class="upload-container">
    <h3>Carica Nuovo Documento</h3>
    
    <EditForm Model="uploadModel" OnValidSubmit="HandleUpload">
        <DataAnnotationsValidator />
        <ValidationSummary />
        
        <!-- File Input con Drag & Drop -->
        <div class="drop-zone @dragClass"
             @ondrop="HandleDrop"
             @ondragenter="HandleDragEnter"
             @ondragleave="HandleDragLeave"
             @ondragover:preventDefault>
            
            <InputFile OnChange="HandleFileSelected" multiple />
            
            <div class="drop-zone-content">
                <i class="fas fa-cloud-upload fa-3x"></i>
                <p>Trascina file qui o clicca per selezionare</p>
                <small>Formati supportati: PDF, DOCX, XLSX, TXT, Immagini</small>
            </div>
        </div>
        
        <!-- File selezionati -->
        @if (selectedFiles.Any())
        {
            <div class="selected-files mt-3">
                <h5>File selezionati:</h5>
                @foreach (var file in selectedFiles)
                {
                    <div class="file-item">
                        <i class="fas fa-file"></i>
                        <span>@file.Name (@FormatFileSize(file.Size))</span>
                        <button type="button" class="btn btn-sm btn-danger"
                                @onclick="() => RemoveFile(file)">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>
                }
            </div>
        }
        
        <!-- Metadata Form -->
        <div class="metadata-form mt-4">
            <div class="mb-3">
                <label class="form-label">Titolo *</label>
                <InputText class="form-control" @bind-Value="uploadModel.Title" />
            </div>
            
            <div class="mb-3">
                <label class="form-label">Descrizione</label>
                <InputTextArea class="form-control" @bind-Value="uploadModel.Description" 
                               rows="3" />
            </div>
            
            <div class="mb-3">
                <label class="form-label">Categoria</label>
                <InputSelect class="form-select" @bind-Value="uploadModel.CategoryId">
                    <option value="">Seleziona categoria...</option>
                    @foreach (var cat in categories)
                    {
                        <option value="@cat.Id">@cat.Name</option>
                    }
                </InputSelect>
            </div>
            
            <div class="mb-3">
                <label class="form-label">Tag (separati da virgola)</label>
                <InputText class="form-control" @bind-Value="uploadModel.Tags" 
                           placeholder="es: contratto, 2024, importante" />
            </div>
            
            <div class="mb-3">
                <label class="form-label">Visibilità</label>
                <InputSelect class="form-select" @bind-Value="uploadModel.Visibility">
                    <option value="0">Privato</option>
                    <option value="2">Organizzazione</option>
                    <option value="3">Pubblico</option>
                </InputSelect>
            </div>
        </div>
        
        <!-- AI Processing Options -->
        <div class="ai-options mt-4">
            <h5>Elaborazione AI</h5>
            <div class="form-check">
                <InputCheckbox class="form-check-input" @bind-Value="uploadModel.ExtractTags" />
                <label class="form-check-label">
                    Estrai tag automaticamente con AI
                </label>
            </div>
            <div class="form-check">
                <InputCheckbox class="form-check-input" @bind-Value="uploadModel.ExtractMetadata" />
                <label class="form-check-label">
                    Estrai metadata (categoria, entità) con AI
                </label>
            </div>
            <div class="form-check">
                <InputCheckbox class="form-check-input" @bind-Value="uploadModel.GenerateEmbeddings" />
                <label class="form-check-label">
                    Genera embeddings per ricerca semantica
                </label>
            </div>
        </div>
        
        <!-- Upload Progress -->
        @if (isUploading)
        {
            <div class="progress mt-4">
                <div class="progress-bar progress-bar-striped progress-bar-animated"
                     style="width: @uploadProgress%">
                    @uploadProgress%
                </div>
            </div>
        }
        
        <!-- Actions -->
        <div class="mt-4">
            <button type="submit" class="btn btn-primary" disabled="@isUploading">
                @if (isUploading)
                {
                    <span class="spinner-border spinner-border-sm"></span>
                    <text>Upload in corso...</text>
                }
                else
                {
                    <i class="fas fa-upload"></i>
                    <text>Carica Documento</text>
                }
            </button>
            <button type="button" class="btn btn-secondary" @onclick="Cancel">
                Annulla
            </button>
        </div>
    </EditForm>
</div>

@code {
    private UploadModel uploadModel = new();
    private List<IBrowserFile> selectedFiles = new();
    private List<Category> categories = new();
    private bool isUploading;
    private int uploadProgress;
    private string dragClass = "";
    
    /// <summary>
    /// Inizializza form caricando categorie disponibili
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        categories = await DocumentService.GetCategoriesAsync();
    }
    
    /// <summary>
    /// Gestisce selezione file da input o drag&drop
    /// </summary>
    /// <param name="e">Event contenente file selezionati</param>
    /// <output>Aggiorna lista selectedFiles</output>
    private void HandleFileSelected(InputFileChangeEventArgs e)
    {
        foreach (var file in e.GetMultipleFiles(maxAllowedFiles: 10))
        {
            if (file.Size <= 50 * 1024 * 1024) // Max 50MB
            {
                selectedFiles.Add(file);
            }
        }
        
        // Se singolo file, usa nome come titolo default
        if (selectedFiles.Count == 1 && string.IsNullOrEmpty(uploadModel.Title))
        {
            uploadModel.Title = Path.GetFileNameWithoutExtension(selectedFiles[0].Name);
        }
    }
    
    /// <summary>
    /// Gestisce upload file con progress tracking
    /// </summary>
    /// <output>File caricato nel sistema con metadata ed elaborazione AI</output>
    private async Task HandleUpload()
    {
        if (!selectedFiles.Any())
        {
            // Mostra errore: nessun file selezionato
            return;
        }
        
        isUploading = true;
        uploadProgress = 0;
        
        try
        {
            foreach (var file in selectedFiles)
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
                
                var document = new Document
                {
                    Title = uploadModel.Title,
                    Description = uploadModel.Description,
                    FileName = file.Name,
                    ContentType = file.ContentType,
                    CategoryId = uploadModel.CategoryId,
                    Tags = uploadModel.Tags,
                    Visibility = uploadModel.Visibility
                };
                
                await DocumentService.UploadDocumentAsync(
                    document, 
                    stream,
                    new DocumentProcessingOptions
                    {
                        ExtractTags = uploadModel.ExtractTags,
                        ExtractMetadata = uploadModel.ExtractMetadata,
                        GenerateEmbeddings = uploadModel.GenerateEmbeddings
                    },
                    progressCallback: (progress) =>
                    {
                        uploadProgress = progress;
                        StateHasChanged();
                    }
                );
                
                uploadProgress = 100;
            }
            
            // Successo - naviga a lista documenti
            Navigation.NavigateTo("/documents");
        }
        catch (Exception ex)
        {
            // Mostra errore
            isUploading = false;
        }
    }
    
    private void HandleDragEnter() => dragClass = "drag-over";
    private void HandleDragLeave() => dragClass = "";
    private void HandleDrop() => dragClass = "";
}
```

### 4. Chat.razor

**Scopo:** Interfaccia chat conversazionale con RAG.

**Funzionalità:**
- Input query con submit on Enter
- Streaming responses real-time
- Visualizzazione messaggi (user/assistant)
- Citazioni documenti fonte
- Sidebar con cronologia conversazioni
- Configurazione parametri RAG

**Key features:**
```csharp
/// <summary>
/// Invia messaggio e riceve risposta RAG streaming
/// </summary>
/// <param name="query">Query utente</param>
/// <output>Risposta generata con citazioni documenti fonte</output>
private async Task SendMessage(string query)
{
    // Aggiunge messaggio utente
    messages.Add(new ChatMessage
    {
        Role = "user",
        Content = query,
        Timestamp = DateTime.Now
    });
    
    isGenerating = true;
    StateHasChanged();
    
    // Placeholder per risposta assistant
    var assistantMessage = new ChatMessage
    {
        Role = "assistant",
        Content = "",
        Timestamp = DateTime.Now
    };
    messages.Add(assistantMessage);
    
    try
    {
        // Streaming response via SignalR
        await foreach (var chunk in chatService.StreamQueryAsync(query, chatHistory))
        {
            assistantMessage.Content += chunk;
            StateHasChanged();  // Aggiorna UI con chunk
        }
        
        // Carica citazioni e sources
        var response = await chatService.GetLastResponseMetadataAsync();
        assistantMessage.Sources = response.Sources;
        assistantMessage.Citations = response.Citations;
    }
    finally
    {
        isGenerating = false;
        StateHasChanged();
    }
}
```

---

## Routing e Navigazione

### Routes Configuration

```csharp
// Routes.razor
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
            <NotAuthorized>
                @if (context.User.Identity?.IsAuthenticated != true)
                {
                    <RedirectToLogin />
                }
                else
                {
                    <p>Non autorizzato ad accedere a questa pagina.</p>
                }
            </NotAuthorized>
        </AuthorizeRouteView>
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout="@typeof(MainLayout)">
            <p>Pagina non trovata.</p>
        </LayoutView>
    </NotFound>
</Router>
```

### Navigation Service

```csharp
// Programmatic navigation
@inject NavigationManager Navigation

private void NavigateToDocument(int id)
{
    Navigation.NavigateTo($"/document/{id}");
}

private void NavigateBack()
{
    Navigation.NavigateTo("/documents");
}
```

---

## Integrazione con Backend

### Chiamate a DocN.Server API

```csharp
@inject HttpClient Http

/// <summary>
/// Esegue ricerca ibrida chiamando API backend
/// </summary>
private async Task<SearchResults> SearchDocumentsAsync(string query)
{
    var response = await Http.PostAsJsonAsync(
        "https://localhost:5211/api/search/hybrid",
        new { query, topK = 10 }
    );
    
    return await response.Content.ReadFromJsonAsync<SearchResults>();
}
```

### Direct Database Access

```csharp
@inject ApplicationDbContext DbContext

/// <summary>
/// Operazioni semplici accedono direttamente al database
/// </summary>
private async Task<List<Category>> GetCategoriesAsync()
{
    return await DbContext.Categories
        .OrderBy(c => c.Name)
        .ToListAsync();
}
```

---

## Per Analisti

### Cosa Offre DocN.Client?

DocN.Client è l'**interfaccia principale** con cui gli utenti interagiscono:

1. **User-Friendly**: UI moderna e intuitiva
2. **Real-Time**: Aggiornamenti istantanei con SignalR
3. **Responsive**: Funziona su desktop, tablet, mobile
4. **Secure**: Autenticazione integrata, autorizzazioni a livello componente

### Vantaggi Business

- **Adozione Rapida**: UI familiare (Bootstrap) riduce curva apprendimento
- **Produttività**: Workflow ottimizzati per task comuni
- **Accessibilità**: Web-based, nessuna installazione richiesta
- **Customizzabile**: Facile aggiungere nuove pagine o modificare esistenti

---

## Per Sviluppatori

### Come Estendere DocN.Client

**Aggiungere Nuova Pagina:**

```razor
@page "/my-new-page"
@inject IMyService MyService
@attribute [Authorize]

<PageTitle>My New Page</PageTitle>

<h3>Nuova Funzionalità</h3>

@code {
    protected override async Task OnInitializedAsync()
    {
        // Inizializzazione
    }
}
```

**Creare Componente Riutilizzabile:**

```razor
<!-- Components/Shared/MyComponent.razor -->
<div class="my-component">
    @ChildContent
</div>

@code {
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    
    [Parameter]
    public EventCallback<string> OnAction { get; set; }
}
```

**Uso:**
```razor
<MyComponent OnAction="HandleAction">
    <p>Contenuto dinamico</p>
</MyComponent>
```

### Best Practices

1. **StateHasChanged()** dopo modifiche asincrone
2. **Dispose** resources in `IDisposable` implementation
3. **Debouncing** per input search
4. **Loading states** per feedback utente
5. **Error boundaries** per gestione errori

---

**Versione Documento**: 1.0  
**Data Aggiornamento**: Dicembre 2024  
**Autori**: Team DocN  
**Target Audience**: Analisti e Sviluppatori
