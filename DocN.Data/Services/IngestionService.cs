using DocN.Core.Interfaces;
using DocN.Data;
using DocN.Data.Models;
using DocN.Data.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Cronos;

namespace DocN.Data.Services;

/// <summary>
/// Service for managing ingestion schedules and executing ingestion tasks
/// </summary>
public class IngestionService : IIngestionService
{
    private readonly DocArcContext _context;
    private readonly ILogger<IngestionService> _logger;
    private readonly IConnectorService _connectorService;
    private readonly IDocumentService _documentService;

    public IngestionService(
        DocArcContext context, 
        ILogger<IngestionService> logger,
        IConnectorService connectorService,
        IDocumentService documentService)
    {
        _context = context;
        _logger = logger;
        _connectorService = connectorService;
        _documentService = documentService;
    }

    public async Task<List<IngestionSchedule>> GetUserSchedulesAsync(string userId)
    {
        try
        {
            return await _context.IngestionSchedules
                .Include(s => s.Connector)
                .Where(s => s.OwnerId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schedules for user {UserId}", userId);
            throw;
        }
    }

    public async Task<IngestionSchedule?> GetScheduleAsync(int scheduleId, string userId)
    {
        try
        {
            return await _context.IngestionSchedules
                .Include(s => s.Connector)
                .FirstOrDefaultAsync(s => s.Id == scheduleId && s.OwnerId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving schedule {ScheduleId} for user {UserId}", scheduleId, userId);
            throw;
        }
    }

    public async Task<IngestionSchedule> CreateScheduleAsync(IngestionSchedule schedule)
    {
        try
        {
            schedule.CreatedAt = DateTime.UtcNow;
            schedule.UpdatedAt = DateTime.UtcNow;
            
            // Calculate next execution time if schedule is enabled
            if (schedule.IsEnabled && schedule.ScheduleType == ScheduleTypes.Scheduled)
            {
                schedule.NextExecutionAt = CalculateNextExecutionTime(schedule.CronExpression);
            }
            
            _context.IngestionSchedules.Add(schedule);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created ingestion schedule {ScheduleId} for user {UserId}", schedule.Id, schedule.OwnerId);
            return schedule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating schedule for user {UserId}", schedule.OwnerId);
            throw;
        }
    }

    public async Task<IngestionSchedule> UpdateScheduleAsync(IngestionSchedule schedule, string userId)
    {
        try
        {
            var existing = await _context.IngestionSchedules
                .FirstOrDefaultAsync(s => s.Id == schedule.Id && s.OwnerId == userId);
            
            if (existing == null)
            {
                throw new UnauthorizedAccessException("Schedule not found or access denied");
            }
            
            existing.Name = schedule.Name;
            existing.ScheduleType = schedule.ScheduleType;
            existing.CronExpression = schedule.CronExpression;
            existing.IntervalMinutes = schedule.IntervalMinutes;
            existing.IsEnabled = schedule.IsEnabled;
            existing.DefaultCategory = schedule.DefaultCategory;
            existing.DefaultVisibility = schedule.DefaultVisibility;
            existing.FilterConfiguration = schedule.FilterConfiguration;
            existing.GenerateEmbeddingsImmediately = schedule.GenerateEmbeddingsImmediately;
            existing.EnableAIAnalysis = schedule.EnableAIAnalysis;
            existing.Description = schedule.Description;
            existing.UpdatedAt = DateTime.UtcNow;
            
            // Recalculate next execution time if schedule is enabled and type is Scheduled
            if (existing.IsEnabled && existing.ScheduleType == ScheduleTypes.Scheduled)
            {
                existing.NextExecutionAt = CalculateNextExecutionTime(existing.CronExpression);
            }
            else
            {
                existing.NextExecutionAt = null;
            }
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Updated schedule {ScheduleId}", schedule.Id);
            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating schedule {ScheduleId}", schedule.Id);
            throw;
        }
    }

    public async Task<bool> DeleteScheduleAsync(int scheduleId, string userId)
    {
        try
        {
            var schedule = await _context.IngestionSchedules
                .FirstOrDefaultAsync(s => s.Id == scheduleId && s.OwnerId == userId);
            
            if (schedule == null)
            {
                return false;
            }
            
            _context.IngestionSchedules.Remove(schedule);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Deleted schedule {ScheduleId}", scheduleId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting schedule {ScheduleId}", scheduleId);
            throw;
        }
    }

    public async Task<IngestionLog> ExecuteIngestionAsync(int scheduleId, string userId)
    {
        var log = new IngestionLog
        {
            IngestionScheduleId = scheduleId,
            StartedAt = DateTime.UtcNow,
            Status = IngestionStatus.Running,
            IsManualExecution = true,
            TriggeredByUserId = userId
        };
        
        try
        {
            _context.IngestionLogs.Add(log);
            await _context.SaveChangesAsync();
            
            var schedule = await GetScheduleAsync(scheduleId, userId);
            if (schedule == null)
            {
                throw new UnauthorizedAccessException("Schedule not found or access denied");
            }
            
            _logger.LogInformation("Starting manual ingestion for schedule {ScheduleId}", scheduleId);
            
            // Get files from connector
            var files = await _connectorService.ListFilesAsync(schedule.ConnectorId, userId);
            log.DocumentsDiscovered = files.Count;
            
            // Process each file
            foreach (var file in files)
            {
                try
                {
                    // TODO: Download file and create document
                    // This is a placeholder - actual implementation would download the file,
                    // process it, and create a document entry
                    
                    log.DocumentsProcessed++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file {FileName}", file.Name);
                    log.DocumentsFailed++;
                }
            }
            
            log.CompletedAt = DateTime.UtcNow;
            log.Status = IngestionStatus.Completed;
            log.DurationSeconds = (int)(log.CompletedAt.Value - log.StartedAt).TotalSeconds;
            
            // Update schedule statistics
            schedule.LastExecutedAt = DateTime.UtcNow;
            schedule.LastExecutionDocumentCount = log.DocumentsProcessed;
            schedule.LastExecutionStatus = IngestionStatus.Completed;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Completed ingestion for schedule {ScheduleId}. Processed: {Processed}, Failed: {Failed}", 
                scheduleId, log.DocumentsProcessed, log.DocumentsFailed);
            
            return log;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing ingestion for schedule {ScheduleId}", scheduleId);
            
            log.CompletedAt = DateTime.UtcNow;
            log.Status = IngestionStatus.Failed;
            log.ErrorMessage = ex.Message;
            log.DurationSeconds = (int)(log.CompletedAt.Value - log.StartedAt).TotalSeconds;
            
            await _context.SaveChangesAsync();
            
            throw;
        }
    }

    public async Task<List<IngestionLog>> GetIngestionLogsAsync(int scheduleId, string userId, int count = 20)
    {
        try
        {
            // Verify user owns the schedule
            var schedule = await GetScheduleAsync(scheduleId, userId);
            if (schedule == null)
            {
                throw new UnauthorizedAccessException("Schedule not found or access denied");
            }
            
            return await _context.IngestionLogs
                .Where(l => l.IngestionScheduleId == scheduleId)
                .OrderByDescending(l => l.StartedAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving logs for schedule {ScheduleId}", scheduleId);
            throw;
        }
    }

    public async Task UpdateNextExecutionTimeAsync(int scheduleId)
    {
        try
        {
            var schedule = await _context.IngestionSchedules.FindAsync(scheduleId);
            if (schedule == null || !schedule.IsEnabled || schedule.ScheduleType != ScheduleTypes.Scheduled)
            {
                return;
            }
            
            schedule.NextExecutionAt = CalculateNextExecutionTime(schedule.CronExpression);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Updated next execution time for schedule {ScheduleId} to {NextExecution}", 
                scheduleId, schedule.NextExecutionAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating next execution time for schedule {ScheduleId}", scheduleId);
            throw;
        }
    }

    private DateTime? CalculateNextExecutionTime(string? cronExpression)
    {
        if (string.IsNullOrEmpty(cronExpression))
        {
            return null;
        }
        
        try
        {
            var expression = CronExpression.Parse(cronExpression);
            return expression.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid cron expression: {CronExpression}", cronExpression);
            return null;
        }
    }
}
