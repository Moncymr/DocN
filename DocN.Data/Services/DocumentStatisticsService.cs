using DocN.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DocN.Data.Services;

public interface IDocumentStatisticsService
{
    Task<DocumentStatistics> GetStatisticsAsync(string userId);
    Task UpdateDocumentAccessAsync(int documentId);
}

public class DocumentStatisticsService : IDocumentStatisticsService
{
    private readonly ApplicationDbContext _context;

    public DocumentStatisticsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentStatistics> GetStatisticsAsync(string userId)
    {
        // Use the same logic as DocumentService.GetUserDocumentsAsync to get accessible documents
        IQueryable<Document> userDocs;
        
        if (string.IsNullOrEmpty(userId))
        {
            // If no user ID, get all public documents and documents without owner
            userDocs = _context.Documents.Where(d => d.Visibility == DocumentVisibility.Public || d.OwnerId == null);
        }
        else
        {
            // Get user's tenant once upfront to avoid repeated queries
            var user = await _context.Users.FindAsync(userId);
            var userTenantId = user?.TenantId;
            
            // Get documents owned by user, shared with user, OR in the same tenant (if user has a tenant)
            // Use Include to avoid N+1 query issues with Shares
            userDocs = _context.Documents
                .Include(d => d.Shares)
                .Where(d => 
                    d.OwnerId == userId ||  // Owned by user
                    d.OwnerId == null ||    // No owner (accessible to all in tenant)
                    d.Shares.Any(s => s.SharedWithUserId == userId) ||  // Shared with user
                    (userTenantId != null && d.TenantId == userTenantId) // Same tenant
                );
        }
        
        var totalDocs = await userDocs.CountAsync();
        var totalStorage = await userDocs.SumAsync(d => (long?)d.FileSize) ?? 0;
        
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddMonths(-1);
        
        var docsToday = await userDocs.CountAsync(d => d.UploadedAt >= today);
        var docsThisWeek = await userDocs.CountAsync(d => d.UploadedAt >= weekAgo);
        var docsThisMonth = await userDocs.CountAsync(d => d.UploadedAt >= monthAgo);
        
        // Documents by category
        var docsByCategory = await userDocs
            .Where(d => !string.IsNullOrEmpty(d.ActualCategory))
            .GroupBy(d => d.ActualCategory!)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Category, x => x.Count);
        
        // Documents by type (file extension)
        var docsByType = await userDocs
            .GroupBy(d => d.ContentType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);
        
        // Most accessed documents
        var topDocs = await userDocs
            .OrderByDescending(d => d.AccessCount)
            .Take(10)
            .Select(d => new TopDocument
            {
                Id = d.Id,
                FileName = d.FileName,
                AccessCount = d.AccessCount,
                LastAccessedAt = d.LastAccessedAt ?? d.UploadedAt
            })
            .ToListAsync();
        
        // Optimization suggestions
        var optimizations = GenerateOptimizationSuggestions(docsByCategory, totalDocs);
        
        return new DocumentStatistics
        {
            TotalDocuments = totalDocs,
            TotalStorageBytes = totalStorage,
            DocumentsUploadedToday = docsToday,
            DocumentsUploadedThisWeek = docsThisWeek,
            DocumentsUploadedThisMonth = docsThisMonth,
            DocumentsByCategory = docsByCategory,
            DocumentsByType = docsByType,
            MostAccessedDocuments = topDocs,
            OptimizationSuggestions = optimizations
        };
    }

    public async Task UpdateDocumentAccessAsync(int documentId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document != null)
        {
            document.AccessCount++;
            document.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private List<CategoryOptimization> GenerateOptimizationSuggestions(
        Dictionary<string, int> docsByCategory, int totalDocs)
    {
        var suggestions = new List<CategoryOptimization>();
        
        // Find overcrowded categories
        foreach (var category in docsByCategory)
        {
            var percentage = (category.Value * 100.0) / totalDocs;
            
            if (percentage > 40)
            {
                suggestions.Add(new CategoryOptimization
                {
                    Category = category.Key,
                    DocumentCount = category.Value,
                    Suggestion = "Consider creating sub-categories",
                    Reason = $"This category contains {percentage:F1}% of all documents and may benefit from further organization"
                });
            }
            else if (category.Value < 3 && totalDocs > 20)
            {
                suggestions.Add(new CategoryOptimization
                {
                    Category = category.Key,
                    DocumentCount = category.Value,
                    Suggestion = "Consider merging with similar category",
                    Reason = "Low document count - may be too specific"
                });
            }
        }
        
        return suggestions;
    }
}
