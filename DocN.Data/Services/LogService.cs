using Microsoft.EntityFrameworkCore;
using DocN.Data.Models;

namespace DocN.Data.Services;

public class LogService : ILogService
{
    private readonly DocArcContext _context;

    public LogService(DocArcContext context)
    {
        _context = context;
    }

    public async Task LogInfoAsync(string category, string message, string? details = null, string? userId = null, string? fileName = null)
    {
        await LogAsync("Info", category, message, details, userId, fileName, null);
    }

    public async Task LogWarningAsync(string category, string message, string? details = null, string? userId = null, string? fileName = null)
    {
        await LogAsync("Warning", category, message, details, userId, fileName, null);
    }

    public async Task LogErrorAsync(string category, string message, string? details = null, string? userId = null, string? fileName = null, string? stackTrace = null)
    {
        await LogAsync("Error", category, message, details, userId, fileName, stackTrace);
    }

    public async Task LogDebugAsync(string category, string message, string? details = null, string? userId = null, string? fileName = null)
    {
        await LogAsync("Debug", category, message, details, userId, fileName, null);
    }

    private async Task LogAsync(string level, string category, string message, string? details, string? userId, string? fileName, string? stackTrace)
    {
        try
        {
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Category = category,
                Message = message,
                Details = details,
                UserId = userId,
                FileName = fileName,
                StackTrace = stackTrace
            };

            _context.LogEntries.Add(logEntry);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Fallback to console if database logging fails
            Console.WriteLine($"[LOG FAILURE] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [{level}] {category}: {message}");
            Console.WriteLine($"[LOG FAILURE] Original error: {ex.Message}");
        }
    }

    public async Task<List<LogEntry>> GetLogsAsync(string? category = null, string? userId = null, DateTime? fromDate = null, int maxRecords = 100)
    {
        var query = _context.LogEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(l => l.Category == category);
        }

        // Only filter by userId if it's not null or empty
        // This ensures we don't filter for empty string which would match nothing
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(l => l.UserId == userId);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= fromDate.Value);
        }

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Take(maxRecords)
            .ToListAsync();
    }

    public async Task<List<LogEntry>> GetUploadLogsAsync(string? userId = null, DateTime? fromDate = null, int maxRecords = 100)
    {
        var uploadCategories = new[] { "Upload", "Embedding", "AI", "Tag", "Metadata", "Category", "SimilaritySearch", "OCR" };
        
        var query = _context.LogEntries
            .Where(l => uploadCategories.Contains(l.Category));

        // Only filter by userId if it's not null or empty
        // This ensures we don't filter for empty string which would match nothing
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(l => l.UserId == userId);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= fromDate.Value);
        }

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Take(maxRecords)
            .ToListAsync();
    }
}
