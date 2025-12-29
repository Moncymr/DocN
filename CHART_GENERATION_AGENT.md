# Chart Generation Agent - Documentazione

## Panoramica

Il **Chart Generation Agent** è un nuovo agente intelligente che genera grafici e visualizzazioni dai dati documentali. Permette di analizzare e visualizzare statistiche sui documenti attraverso diversi tipi di grafici interattivi.

## Caratteristiche

### 🎯 Tipi di Grafici Supportati

1. **Caricamenti nel Tempo** (Line Chart)
   - Visualizza il trend dei documenti caricati nel tempo
   - Granularità configurabile: giornaliera, settimanale, mensile
   - Periodo personalizzabile (7, 14, 30, 60, 90 giorni)

2. **Distribuzione per Categoria** (Doughnut Chart)
   - Mostra la distribuzione dei documenti tra le varie categorie
   - Percentuali calcolate automaticamente
   - Palette di colori distinti per ogni categoria

3. **Distribuzione Tipi di File** (Bar Chart)
   - Top 10 tipi di file più comuni
   - Conta dei documenti per estensione file
   - Utile per capire la composizione della documentazione

4. **Trend Accessi** (Area Chart)
   - Traccia gli accessi ai documenti nel tempo
   - Permette di identificare i periodi di maggiore utilizzo
   - Visualizzazione cumulativa degli accessi

5. **Metriche Comparative** (Multi-line Chart)
   - Confronta caricamenti vs accessi nel tempo
   - Due serie di dati sovrapposte
   - Utile per analizzare l'engagement

## Architettura

### Backend

#### ChartGenerationAgent
Posizione: `DocN.Data/Services/Agents/ChartGenerationAgent.cs`

```csharp
public interface IChartGenerationAgent : IAgent
{
    Task<ChartData> GenerateDocumentUploadsOverTimeAsync(string? userId, TimeGranularity granularity, int days);
    Task<ChartData> GenerateCategoryDistributionAsync(string? userId);
    Task<ChartData> GenerateFileTypeDistributionAsync(string? userId);
    Task<ChartData> GenerateAccessTrendsAsync(string? userId, int days);
    Task<ChartData> GenerateComparativeMetricsAsync(string? userId, int days);
}
```

**Funzionalità:**
- Genera dati per grafici basati sui documenti dell'utente
- Supporta multi-tenancy e controllo accessi
- Gestione errori robusta
- Performance ottimizzata con query EF Core

#### ChartsController
Posizione: `DocN.Server/Controllers/ChartsController.cs`

**Endpoints API:**
- `GET /api/charts/uploads-over-time?granularity=daily&days=30`
- `GET /api/charts/category-distribution`
- `GET /api/charts/file-type-distribution`
- `GET /api/charts/access-trends?days=30`
- `GET /api/charts/comparative-metrics?days=30`
- `GET /api/charts/dashboard?days=30` - Ritorna tutti i grafici in una singola chiamata

#### Modelli
Posizione: `DocN.Data/Models/ChartData.cs`

```csharp
public class ChartData
{
    public string Title { get; set; }
    public string Description { get; set; }
    public ChartType Type { get; set; }
    public List<ChartSeries> Series { get; set; }
    public List<string> Labels { get; set; }
    public ChartOptions Options { get; set; }
}

public enum ChartType
{
    Line, Bar, Pie, Doughnut, Area, Radar
}
```

### Frontend

#### Charts Page
Posizione: `DocN.Client/Components/Pages/Charts.razor`

**Caratteristiche:**
- Layout responsive con grid system
- Periodo selezionabile tramite dropdown
- Loading state con spinner
- Error handling con retry
- Design moderno con gradients e shadows
- Info cards esplicative

**Navigazione:**
- Menu principale: `📈 Grafici`
- Dashboard: Link prominente "Visualizza Grafici Avanzati"

#### Integrazione Dashboard
- Link diretto alla pagina Charts
- Button con gradient viola e hover effect
- Icona 📈 per identificazione visiva

## Utilizzo

### Da Codice

```csharp
// Inject il service
private readonly IChartGenerationAgent _chartAgent;

// Genera grafico caricamenti
var uploadsChart = await _chartAgent.GenerateDocumentUploadsOverTimeAsync(
    userId: "user123",
    granularity: TimeGranularity.Daily,
    days: 30
);

// Genera grafico categorie
var categoryChart = await _chartAgent.GenerateCategoryDistributionAsync("user123");
```

### Da API

```bash
# Ottieni caricamenti ultimi 30 giorni
curl https://localhost:7114/api/charts/uploads-over-time?days=30

# Ottieni tutti i grafici del dashboard
curl https://localhost:7114/api/charts/dashboard?days=30
```

### Da UI

1. Naviga su **Grafici** dal menu principale
2. Seleziona il periodo desiderato (7-90 giorni)
3. Visualizza i grafici generati automaticamente
4. Hover sui grafici per interazioni future

## Configurazione

### Registrazione Services
In `Program.cs`:

```csharp
builder.Services.AddScoped<IChartGenerationAgent, ChartGenerationAgent>();
```

### Personalizzazione Colori
In `ChartGenerationAgent.cs`:

```csharp
private readonly string[] _colorPalette = new[]
{
    "#FF6B35", "#F7931E", "#FDC830", "#37B7C3",
    "#088395", "#071952", "#8E44AD", "#E74C3C",
    "#3498DB", "#2ECC71", "#F39C12", "#16A085"
};
```

## Performance

### Ottimizzazioni Implementate

1. **Query Efficiency**
   - Query EF Core ottimizzate
   - Include/Select per evitare N+1
   - Filtri database-side

2. **Caching Potential**
   - Dati aggregati cacheable
   - Cache invalidation su upload/delete documenti
   - TTL configurabile

3. **Parallel Execution**
   - Endpoint `/charts/dashboard` esegue queries in parallelo
   - Task.WhenAll per multiple chart generation
   - Riduce latency complessiva

### Metriche Attese

- **Singolo grafico**: 100-300ms
- **Dashboard completo**: 300-600ms (5 grafici in parallelo)
- **Con cache**: 10-50ms

## Estensibilità

### Aggiungere Nuovi Grafici

1. Aggiungere metodo in `IChartGenerationAgent`
2. Implementare logica in `ChartGenerationAgent`
3. Aggiungere endpoint in `ChartsController`
4. Aggiungere visualizzazione in `Charts.razor`

### Esempio: Grafico Storage

```csharp
public async Task<ChartData> GenerateStorageUsageAsync(string? userId)
{
    var documents = await GetUserDocumentsQueryable(userId).ToListAsync();
    
    var storageByType = documents
        .GroupBy(d => GetFileExtension(d.FileName))
        .Select(g => new {
            Type = g.Key,
            Size = g.Sum(d => d.FileSize)
        })
        .OrderByDescending(x => x.Size)
        .ToList();
    
    return new ChartData
    {
        Title = "Utilizzo Storage per Tipo",
        Type = ChartType.Pie,
        Labels = storageByType.Select(s => s.Type).ToList(),
        Series = new List<ChartSeries>
        {
            new ChartSeries
            {
                Name = "Storage (MB)",
                Data = storageByType.Select(s => s.Size / 1024.0 / 1024.0).ToList()
            }
        }
    };
}
```

## Sicurezza

### Controllo Accessi

- Rispetta ownership documenti
- Supporta multi-tenancy
- Filtra per user ID automaticamente
- Public documents visibili a tutti

### Validazione Input

- Parametri query validati
- Range limits su days (7-90)
- Granularity enum validation
- Error handling robusto

## Testing

### Unit Tests Consigliati

```csharp
[Fact]
public async Task GenerateDocumentUploadsOverTime_ReturnsCorrectData()
{
    // Arrange
    var agent = new ChartGenerationAgent(context, logger);
    
    // Act
    var result = await agent.GenerateDocumentUploadsOverTimeAsync(
        userId: "test",
        TimeGranularity.Daily,
        days: 7
    );
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("Caricamenti Documenti nel Tempo", result.Title);
    Assert.Equal(ChartType.Line, result.Type);
}
```

### Integration Tests

```csharp
[Fact]
public async Task ChartsController_Dashboard_ReturnsAllCharts()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/charts/dashboard?days=30");
    
    // Assert
    response.EnsureSuccessStatusCode();
    var charts = await response.Content.ReadFromJsonAsync<DashboardCharts>();
    Assert.NotNull(charts.UploadsOverTime);
    Assert.NotNull(charts.CategoryDistribution);
}
```

## Roadmap

### v1.1 - Chart.js Integration
- [ ] Aggiungere Chart.js via CDN
- [ ] JavaScript interop per rendering
- [ ] Grafici interattivi (tooltip, zoom, pan)
- [ ] Export PNG/SVG

### v1.2 - Advanced Features
- [ ] Filtri temporali avanzati
- [ ] Drill-down capabilities
- [ ] Real-time updates via SignalR
- [ ] Custom chart builder

### v1.3 - Analytics
- [ ] Predizioni con ML.NET
- [ ] Trend analysis
- [ ] Anomaly detection
- [ ] Insights automatici

## Troubleshooting

### Grafici Vuoti
- Verificare che ci siano documenti nel database
- Controllare filtri temporali (potrebbero essere troppo restrittivi)
- Verificare permessi utente

### Performance Lente
- Controllare numero documenti (>10k potrebbero rallentare)
- Implementare caching
- Ridurre periodo temporale

### Errori API
- Verificare registrazione services in Program.cs
- Controllare connection string database
- Verificare logs per dettagli

## Conclusioni

Il Chart Generation Agent fornisce un sistema completo per visualizzare analytics documentali con:
- ✅ API RESTful ben strutturate
- ✅ Agent pattern per logica business
- ✅ UI responsive e moderna
- ✅ Performance ottimizzate
- ✅ Estensibilità per futuri grafici

Per domande o contributi, vedere [CONTRIBUTING.md](CONTRIBUTING.md)
