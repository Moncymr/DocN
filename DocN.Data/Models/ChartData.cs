namespace DocN.Data.Models;

/// <summary>
/// Represents chart data for visualization
/// </summary>
public class ChartData
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ChartType Type { get; set; }
    public List<ChartSeries> Series { get; set; } = new();
    public List<string> Labels { get; set; } = new();
    public ChartOptions Options { get; set; } = new();
}

/// <summary>
/// Chart series data
/// </summary>
public class ChartSeries
{
    public string Name { get; set; } = string.Empty;
    public List<double> Data { get; set; } = new();
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// Chart options for rendering
/// </summary>
public class ChartOptions
{
    public bool ShowLegend { get; set; } = true;
    public bool ShowGrid { get; set; } = true;
    public bool Responsive { get; set; } = true;
    public string XAxisLabel { get; set; } = string.Empty;
    public string YAxisLabel { get; set; } = string.Empty;
}

/// <summary>
/// Types of charts supported
/// </summary>
public enum ChartType
{
    Line,
    Bar,
    Pie,
    Doughnut,
    Area,
    Radar
}

/// <summary>
/// Time-based chart data point
/// </summary>
public class TimeSeriesDataPoint
{
    public DateTime Date { get; set; }
    public double Value { get; set; }
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Category distribution data
/// </summary>
public class CategoryDistribution
{
    public string Category { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
}
