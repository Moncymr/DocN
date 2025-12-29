using Microsoft.AspNetCore.Mvc;
using DocN.Data.Models;
using DocN.Data.Services.Agents;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DocN.Server.Controllers;

/// <summary>
/// API Controller for chart and visualization data
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChartsController : ControllerBase
{
    private readonly IChartGenerationAgent _chartAgent;
    private readonly ILogger<ChartsController> _logger;

    public ChartsController(
        IChartGenerationAgent chartAgent,
        ILogger<ChartsController> logger)
    {
        _chartAgent = chartAgent;
        _logger = logger;
    }

    /// <summary>
    /// Get document uploads over time chart
    /// </summary>
    [HttpGet("uploads-over-time")]
    public async Task<ActionResult<ChartData>> GetDocumentUploadsOverTime(
        [FromQuery] string? granularity = "daily",
        [FromQuery] int days = 30)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var timeGranularity = granularity?.ToLower() switch
            {
                "hourly" => TimeGranularity.Hourly,
                "weekly" => TimeGranularity.Weekly,
                "monthly" => TimeGranularity.Monthly,
                _ => TimeGranularity.Daily
            };
            
            var chartData = await _chartAgent.GenerateDocumentUploadsOverTimeAsync(
                userId, 
                timeGranularity, 
                days);
            
            return Ok(chartData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating uploads over time chart");
            return StatusCode(500, "Error generating chart data");
        }
    }

    /// <summary>
    /// Get category distribution chart
    /// </summary>
    [HttpGet("category-distribution")]
    public async Task<ActionResult<ChartData>> GetCategoryDistribution()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chartData = await _chartAgent.GenerateCategoryDistributionAsync(userId);
            return Ok(chartData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating category distribution chart");
            return StatusCode(500, "Error generating chart data");
        }
    }

    /// <summary>
    /// Get file type distribution chart
    /// </summary>
    [HttpGet("file-type-distribution")]
    public async Task<ActionResult<ChartData>> GetFileTypeDistribution()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chartData = await _chartAgent.GenerateFileTypeDistributionAsync(userId);
            return Ok(chartData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating file type distribution chart");
            return StatusCode(500, "Error generating chart data");
        }
    }

    /// <summary>
    /// Get document access trends chart
    /// </summary>
    [HttpGet("access-trends")]
    public async Task<ActionResult<ChartData>> GetAccessTrends([FromQuery] int days = 30)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chartData = await _chartAgent.GenerateAccessTrendsAsync(userId, days);
            return Ok(chartData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating access trends chart");
            return StatusCode(500, "Error generating chart data");
        }
    }

    /// <summary>
    /// Get comparative metrics chart (uploads vs accesses)
    /// </summary>
    [HttpGet("comparative-metrics")]
    public async Task<ActionResult<ChartData>> GetComparativeMetrics([FromQuery] int days = 30)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var chartData = await _chartAgent.GenerateComparativeMetricsAsync(userId, days);
            return Ok(chartData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comparative metrics chart");
            return StatusCode(500, "Error generating chart data");
        }
    }

    /// <summary>
    /// Get all dashboard charts in a single call
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardCharts>> GetDashboardCharts([FromQuery] int days = 30)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Generate all charts in parallel for efficiency
            var uploadsTask = _chartAgent.GenerateDocumentUploadsOverTimeAsync(userId, TimeGranularity.Daily, days);
            var categoryTask = _chartAgent.GenerateCategoryDistributionAsync(userId);
            var fileTypeTask = _chartAgent.GenerateFileTypeDistributionAsync(userId);
            var accessTask = _chartAgent.GenerateAccessTrendsAsync(userId, days);
            var comparativeTask = _chartAgent.GenerateComparativeMetricsAsync(userId, days);
            
            await Task.WhenAll(uploadsTask, categoryTask, fileTypeTask, accessTask, comparativeTask);
            
            var dashboardCharts = new DashboardCharts
            {
                UploadsOverTime = await uploadsTask,
                CategoryDistribution = await categoryTask,
                FileTypeDistribution = await fileTypeTask,
                AccessTrends = await accessTask,
                ComparativeMetrics = await comparativeTask
            };
            
            return Ok(dashboardCharts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating dashboard charts");
            return StatusCode(500, "Error generating chart data");
        }
    }
}

/// <summary>
/// Collection of charts for dashboard
/// </summary>
public class DashboardCharts
{
    public ChartData UploadsOverTime { get; set; } = null!;
    public ChartData CategoryDistribution { get; set; } = null!;
    public ChartData FileTypeDistribution { get; set; } = null!;
    public ChartData AccessTrends { get; set; } = null!;
    public ChartData ComparativeMetrics { get; set; } = null!;
}
