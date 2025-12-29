using Microsoft.AspNetCore.Mvc;
using DocN.Data.Models;
using DocN.Data.Services;

namespace DocN.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogService logService, ILogger<LogsController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<LogEntry>>> GetLogs(
        [FromQuery] string? category = null,
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] int maxRecords = 100)
    {
        try
        {
            var logs = await _logService.GetLogsAsync(category, userId, fromDate, maxRecords);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving logs");
            return StatusCode(500, "An error occurred while retrieving logs");
        }
    }

    [HttpGet("upload")]
    public async Task<ActionResult<List<LogEntry>>> GetUploadLogs(
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] int maxRecords = 100)
    {
        try
        {
            var logs = await _logService.GetUploadLogsAsync(userId, fromDate, maxRecords);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upload logs");
            return StatusCode(500, "An error occurred while retrieving upload logs");
        }
    }

    [HttpPost]
    public async Task<ActionResult> CreateLog([FromBody] CreateLogRequest request)
    {
        try
        {
            switch (request.Level?.ToLower())
            {
                case "error":
                    await _logService.LogErrorAsync(request.Category, request.Message, request.Details, request.UserId, request.FileName, request.StackTrace);
                    break;
                case "warning":
                    await _logService.LogWarningAsync(request.Category, request.Message, request.Details, request.UserId, request.FileName);
                    break;
                case "debug":
                    await _logService.LogDebugAsync(request.Category, request.Message, request.Details, request.UserId, request.FileName);
                    break;
                default:
                    await _logService.LogInfoAsync(request.Category, request.Message, request.Details, request.UserId, request.FileName);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating log entry");
            return StatusCode(500, "An error occurred while creating log entry");
        }
    }
}

public class CreateLogRequest
{
    public string Level { get; set; } = "Info";
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? UserId { get; set; }
    public string? FileName { get; set; }
    public string? StackTrace { get; set; }
}
