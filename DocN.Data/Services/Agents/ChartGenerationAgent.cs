using DocN.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocN.Data.Services.Agents;

/// <summary>
/// Agent responsible for generating charts and visualizations from document data
/// </summary>
public interface IChartGenerationAgent : IAgent
{
    /// <summary>
    /// Generate a time series chart showing document uploads over time
    /// </summary>
    Task<ChartData> GenerateDocumentUploadsOverTimeAsync(string? userId, TimeGranularity granularity = TimeGranularity.Daily, int days = 30);
    
    /// <summary>
    /// Generate a pie/doughnut chart showing category distribution
    /// </summary>
    Task<ChartData> GenerateCategoryDistributionAsync(string? userId);
    
    /// <summary>
    /// Generate a bar chart showing file type distribution
    /// </summary>
    Task<ChartData> GenerateFileTypeDistributionAsync(string? userId);
    
    /// <summary>
    /// Generate a line chart showing document access trends
    /// </summary>
    Task<ChartData> GenerateAccessTrendsAsync(string? userId, int days = 30);
    
    /// <summary>
    /// Generate a multi-series chart comparing different metrics
    /// </summary>
    Task<ChartData> GenerateComparativeMetricsAsync(string? userId, int days = 30);
}

/// <summary>
/// Time granularity for time series data
/// </summary>
public enum TimeGranularity
{
    Hourly,
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Implementation of chart generation agent
/// </summary>
public class ChartGenerationAgent : IChartGenerationAgent
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChartGenerationAgent> _logger;
    
    // Color palette for charts
    private readonly string[] _colorPalette = new[]
    {
        "#FF6B35", "#F7931E", "#FDC830", "#37B7C3",
        "#088395", "#071952", "#8E44AD", "#E74C3C",
        "#3498DB", "#2ECC71", "#F39C12", "#16A085"
    };
    
    public string Name => "Chart Generation Agent";
    public string Description => "Generates interactive charts and visualizations from document data for analytics and insights";
    
    public ChartGenerationAgent(
        ApplicationDbContext context,
        ILogger<ChartGenerationAgent> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    public async Task<ChartData> GenerateDocumentUploadsOverTimeAsync(
        string? userId, 
        TimeGranularity granularity = TimeGranularity.Daily, 
        int days = 30)
    {
        try
        {
            var documents = await GetUserDocumentsQueryable(userId).ToListAsync();
            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            
            // Group documents by time period
            var timeSeriesData = new List<TimeSeriesDataPoint>();
            
            switch (granularity)
            {
                case TimeGranularity.Daily:
                    timeSeriesData = documents
                        .Where(d => d.UploadedAt >= startDate)
                        .GroupBy(d => d.UploadedAt.Date)
                        .Select(g => new TimeSeriesDataPoint
                        {
                            Date = g.Key,
                            Value = g.Count(),
                            Label = g.Key.ToString("dd MMM")
                        })
                        .OrderBy(d => d.Date)
                        .ToList();
                    break;
                    
                case TimeGranularity.Weekly:
                    timeSeriesData = documents
                        .Where(d => d.UploadedAt >= startDate)
                        .GroupBy(d => GetWeekStart(d.UploadedAt))
                        .Select(g => new TimeSeriesDataPoint
                        {
                            Date = g.Key,
                            Value = g.Count(),
                            Label = $"Week {GetWeekNumber(g.Key)}"
                        })
                        .OrderBy(d => d.Date)
                        .ToList();
                    break;
                    
                case TimeGranularity.Monthly:
                    timeSeriesData = documents
                        .Where(d => d.UploadedAt >= startDate)
                        .GroupBy(d => new DateTime(d.UploadedAt.Year, d.UploadedAt.Month, 1))
                        .Select(g => new TimeSeriesDataPoint
                        {
                            Date = g.Key,
                            Value = g.Count(),
                            Label = g.Key.ToString("MMM yyyy")
                        })
                        .OrderBy(d => d.Date)
                        .ToList();
                    break;
            }
            
            return new ChartData
            {
                Title = "Caricamenti Documenti nel Tempo",
                Description = $"Documenti caricati negli ultimi {days} giorni",
                Type = ChartType.Line,
                Labels = timeSeriesData.Select(d => d.Label).ToList(),
                Series = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Documenti",
                        Data = timeSeriesData.Select(d => d.Value).ToList(),
                        Color = _colorPalette[0]
                    }
                },
                Options = new ChartOptions
                {
                    ShowLegend = true,
                    ShowGrid = true,
                    Responsive = true,
                    XAxisLabel = "Data",
                    YAxisLabel = "Numero Documenti"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating document uploads chart");
            return CreateErrorChart("Errore nella generazione del grafico caricamenti");
        }
    }
    
    public async Task<ChartData> GenerateCategoryDistributionAsync(string? userId)
    {
        try
        {
            var documents = await GetUserDocumentsQueryable(userId)
                .Where(d => !string.IsNullOrEmpty(d.ActualCategory))
                .ToListAsync();
                
            var categoryGroups = documents
                .GroupBy(d => d.ActualCategory!)
                .Select((g, index) => new CategoryDistribution
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Percentage = (g.Count() * 100.0) / documents.Count,
                    Color = _colorPalette[index % _colorPalette.Length]
                })
                .OrderByDescending(c => c.Count)
                .ToList();
            
            return new ChartData
            {
                Title = "Distribuzione per Categoria",
                Description = "Documenti raggruppati per categoria",
                Type = ChartType.Doughnut,
                Labels = categoryGroups.Select(c => c.Category).ToList(),
                Series = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Documenti",
                        Data = categoryGroups.Select(c => (double)c.Count).ToList(),
                        Color = string.Join(",", categoryGroups.Select(c => c.Color))
                    }
                },
                Options = new ChartOptions
                {
                    ShowLegend = true,
                    ShowGrid = false,
                    Responsive = true
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating category distribution chart");
            return CreateErrorChart("Errore nella generazione del grafico categorie");
        }
    }
    
    public async Task<ChartData> GenerateFileTypeDistributionAsync(string? userId)
    {
        try
        {
            var documents = await GetUserDocumentsQueryable(userId).ToListAsync();
            
            var fileTypeGroups = documents
                .GroupBy(d => GetFileExtension(d.FileName))
                .Select((g, index) => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Color = _colorPalette[index % _colorPalette.Length]
                })
                .OrderByDescending(t => t.Count)
                .Take(10) // Top 10 file types
                .ToList();
            
            return new ChartData
            {
                Title = "Distribuzione per Tipo File",
                Description = "Top 10 tipi di file più comuni",
                Type = ChartType.Bar,
                Labels = fileTypeGroups.Select(t => t.Type).ToList(),
                Series = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Documenti",
                        Data = fileTypeGroups.Select(t => (double)t.Count).ToList(),
                        Color = _colorPalette[1]
                    }
                },
                Options = new ChartOptions
                {
                    ShowLegend = false,
                    ShowGrid = true,
                    Responsive = true,
                    XAxisLabel = "Tipo File",
                    YAxisLabel = "Numero Documenti"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating file type distribution chart");
            return CreateErrorChart("Errore nella generazione del grafico tipi file");
        }
    }
    
    public async Task<ChartData> GenerateAccessTrendsAsync(string? userId, int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            var documents = await GetUserDocumentsQueryable(userId)
                .Where(d => d.LastAccessedAt != null && d.LastAccessedAt >= startDate)
                .ToListAsync();
            
            var accessTrends = documents
                .Where(d => d.LastAccessedAt.HasValue)
                .GroupBy(d => d.LastAccessedAt!.Value.Date)
                .Select(g => new TimeSeriesDataPoint
                {
                    Date = g.Key,
                    Value = g.Sum(d => d.AccessCount),
                    Label = g.Key.ToString("dd MMM")
                })
                .OrderBy(d => d.Date)
                .ToList();
            
            return new ChartData
            {
                Title = "Trend Accessi Documenti",
                Description = $"Accessi ai documenti negli ultimi {days} giorni",
                Type = ChartType.Area,
                Labels = accessTrends.Select(d => d.Label).ToList(),
                Series = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Accessi",
                        Data = accessTrends.Select(d => d.Value).ToList(),
                        Color = _colorPalette[2]
                    }
                },
                Options = new ChartOptions
                {
                    ShowLegend = true,
                    ShowGrid = true,
                    Responsive = true,
                    XAxisLabel = "Data",
                    YAxisLabel = "Numero Accessi"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating access trends chart");
            return CreateErrorChart("Errore nella generazione del grafico accessi");
        }
    }
    
    public async Task<ChartData> GenerateComparativeMetricsAsync(string? userId, int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);
            var documents = await GetUserDocumentsQueryable(userId).ToListAsync();
            
            // Get daily uploads
            var dailyUploads = documents
                .Where(d => d.UploadedAt >= startDate)
                .GroupBy(d => d.UploadedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new TimeSeriesDataPoint
                {
                    Date = g.Key,
                    Value = g.Count(),
                    Label = g.Key.ToString("dd MMM")
                })
                .ToList();
            
            // Get daily accesses
            var dailyAccesses = documents
                .Where(d => d.LastAccessedAt != null && d.LastAccessedAt >= startDate)
                .GroupBy(d => d.LastAccessedAt!.Value.Date)
                .OrderBy(g => g.Key)
                .Select(g => new TimeSeriesDataPoint
                {
                    Date = g.Key,
                    Value = g.Count(),
                    Label = g.Key.ToString("dd MMM")
                })
                .ToList();
            
            // Merge labels
            var allDates = dailyUploads.Select(d => d.Date)
                .Union(dailyAccesses.Select(d => d.Date))
                .Distinct()
                .OrderBy(d => d)
                .ToList();
            
            var labels = allDates.Select(d => d.ToString("dd MMM")).ToList();
            
            // Create aligned data series
            var uploadData = allDates.Select(date => 
                dailyUploads.FirstOrDefault(u => u.Date == date)?.Value ?? 0).ToList();
            
            var accessData = allDates.Select(date => 
                dailyAccesses.FirstOrDefault(a => a.Date == date)?.Value ?? 0).ToList();
            
            return new ChartData
            {
                Title = "Confronto Metriche",
                Description = "Caricamenti vs Accessi negli ultimi giorni",
                Type = ChartType.Line,
                Labels = labels,
                Series = new List<ChartSeries>
                {
                    new ChartSeries
                    {
                        Name = "Caricamenti",
                        Data = uploadData,
                        Color = _colorPalette[0]
                    },
                    new ChartSeries
                    {
                        Name = "Accessi",
                        Data = accessData,
                        Color = _colorPalette[3]
                    }
                },
                Options = new ChartOptions
                {
                    ShowLegend = true,
                    ShowGrid = true,
                    Responsive = true,
                    XAxisLabel = "Data",
                    YAxisLabel = "Conteggio"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comparative metrics chart");
            return CreateErrorChart("Errore nella generazione del grafico comparativo");
        }
    }
    
    // Helper methods
    
    private IQueryable<Document> GetUserDocumentsQueryable(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return _context.Documents.Where(d => d.Visibility == DocumentVisibility.Public);
        }
        
        var userTenantId = _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.TenantId)
            .FirstOrDefault();
        
        return _context.Documents
            .Include(d => d.Shares)
            .Where(d =>
                d.OwnerId == userId ||
                d.Shares.Any(s => s.SharedWithUserId == userId) ||
                (userTenantId != null && d.TenantId == userTenantId) ||
                (d.OwnerId == null && d.Visibility == DocumentVisibility.Public)
            );
    }
    
    private DateTime GetWeekStart(DateTime date)
    {
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
    
    private int GetWeekNumber(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
    }
    
    private string GetFileExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToUpperInvariant();
        return string.IsNullOrEmpty(extension) ? "UNKNOWN" : extension.TrimStart('.');
    }
    
    private ChartData CreateErrorChart(string errorMessage)
    {
        return new ChartData
        {
            Title = "Errore",
            Description = errorMessage,
            Type = ChartType.Bar,
            Labels = new List<string>(),
            Series = new List<ChartSeries>()
        };
    }
}
