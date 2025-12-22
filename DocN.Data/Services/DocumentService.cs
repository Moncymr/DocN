using DocN.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DocN.Data.Services;

public interface IDocumentService
{
    Task<Document?> GetDocumentAsync(int documentId, string userId);
    Task<bool> CanUserAccessDocument(int documentId, string userId);
    Task<List<Document>> GetUserDocumentsAsync(string userId, int page = 1, int pageSize = 20);
    Task<int> GetTotalDocumentCountAsync(string userId);
    Task<byte[]?> DownloadDocumentAsync(int documentId, string userId);
    Task<bool> ShareDocumentAsync(int documentId, string shareWithUserId, DocumentPermission permission, string currentUserId);
    Task<bool> UpdateDocumentVisibilityAsync(int documentId, DocumentVisibility visibility, string userId);
}

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;

    public DocumentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetDocumentAsync(int documentId, string userId)
    {
        var document = await _context.Documents
            .Include(d => d.Owner)
            .Include(d => d.Shares)
            .Include(d => d.Tags)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return null;

        // Check access permissions
        if (!await CanUserAccessDocument(documentId, userId))
            return null;

        return document;
    }

    public async Task<bool> CanUserAccessDocument(int documentId, string userId)
    {
        var document = await _context.Documents
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null)
            return false;

        // Owner always has access
        if (document.OwnerId == userId)
            return true;

        // Check visibility settings
        if (document.Visibility == DocumentVisibility.Public)
            return true;

        if (document.Visibility == DocumentVisibility.Organization)
            return true; // In a real app, check if user is in same organization

        // Check if document is shared with user
        if (document.Visibility == DocumentVisibility.Shared)
        {
            return document.Shares.Any(s => s.SharedWithUserId == userId);
        }

        return false;
    }

    public async Task<List<Document>> GetUserDocumentsAsync(string userId, int page = 1, int pageSize = 20)
    {
        // Get documents owned by user or shared with user - optimized for large datasets
        var ownedDocs = _context.Documents
            .Where(d => d.OwnerId == userId);

        var sharedDocs = _context.Documents
            .Where(d => d.Shares.Any(s => s.SharedWithUserId == userId));

        var allDocs = await ownedDocs
            .Union(sharedDocs)
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(d => d.Owner)
            .Include(d => d.Tags)
            .ToListAsync();

        return allDocs;
    }

    public async Task<int> GetTotalDocumentCountAsync(string userId)
    {
        var ownedCount = await _context.Documents.CountAsync(d => d.OwnerId == userId);
        var sharedCount = await _context.Documents.CountAsync(d => d.Shares.Any(s => s.SharedWithUserId == userId));
        
        return ownedCount + sharedCount;
    }

    public async Task<byte[]?> DownloadDocumentAsync(int documentId, string userId)
    {
        // Check if user has access
        if (!await CanUserAccessDocument(documentId, userId))
            return null;

        var document = await _context.Documents.FindAsync(documentId);
        if (document == null || !File.Exists(document.FilePath))
            return null;

        try
        {
            return await File.ReadAllBytesAsync(document.FilePath);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ShareDocumentAsync(int documentId, string shareWithUserId, DocumentPermission permission, string currentUserId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        
        // Only owner can share
        if (document == null || document.OwnerId != currentUserId)
            return false;

        // Check if already shared
        var existingShare = await _context.DocumentShares
            .FirstOrDefaultAsync(s => s.DocumentId == documentId && s.SharedWithUserId == shareWithUserId);

        if (existingShare != null)
        {
            existingShare.Permission = permission;
        }
        else
        {
            var share = new DocumentShare
            {
                DocumentId = documentId,
                SharedWithUserId = shareWithUserId,
                Permission = permission,
                SharedByUserId = currentUserId,
                SharedAt = DateTime.UtcNow
            };
            _context.DocumentShares.Add(share);
        }

        // Update document visibility to Shared if it's Private
        if (document.Visibility == DocumentVisibility.Private)
        {
            document.Visibility = DocumentVisibility.Shared;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateDocumentVisibilityAsync(int documentId, DocumentVisibility visibility, string userId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        
        // Only owner can change visibility
        if (document == null || document.OwnerId != userId)
            return false;

        document.Visibility = visibility;
        await _context.SaveChangesAsync();
        return true;
    }
}
