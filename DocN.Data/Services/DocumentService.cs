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
    Task<Document> CreateDocumentAsync(Document document);
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

        // Owner always has access (if document has an owner)
        if (!string.IsNullOrEmpty(document.OwnerId) && document.OwnerId == userId)
            return true;

        // Documents without owner are accessible based on visibility
        if (string.IsNullOrEmpty(document.OwnerId))
        {
            return document.Visibility == DocumentVisibility.Public || 
                   document.Visibility == DocumentVisibility.Organization;
        }

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
        var query = _context.Documents.AsQueryable();
        
        if (string.IsNullOrEmpty(userId))
        {
            // If no user ID, return:
            // 1. All documents with Public visibility
            // 2. All documents without an owner (legacy documents - treat as public)
            query = query.Where(d => d.Visibility == DocumentVisibility.Public || d.OwnerId == null);
        }
        else
        {
            // Get documents for logged-in users:
            // 1. Documents owned by user (any visibility)
            // 2. Documents shared with user
            // 3. Documents without owner (legacy documents - treat as public)
            // 4. Documents with Public or Organization visibility
            query = query.Where(d => 
                d.OwnerId == userId || 
                d.Shares.Any(s => s.SharedWithUserId == userId) ||
                d.OwnerId == null ||
                d.Visibility == DocumentVisibility.Public || 
                d.Visibility == DocumentVisibility.Organization);
        }

        var allDocs = await query
            .OrderByDescending(d => d.UploadedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(d => d.Owner)
            .Include(d => d.Tags)
            .Distinct() // Ensure no duplicates
            .ToListAsync();

        return allDocs;
    }

    public async Task<int> GetTotalDocumentCountAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            // If no user ID, count public documents and unowned documents (legacy)
            return await _context.Documents.CountAsync(d => d.Visibility == DocumentVisibility.Public || d.OwnerId == null);
        }
        
        // Count documents for logged-in users:
        // 1. Documents owned by user
        // 2. Documents shared with user
        // 3. Documents without owner (legacy documents)
        // 4. Public or Organization documents
        var count = await _context.Documents
            .Where(d => 
                d.OwnerId == userId || 
                d.Shares.Any(s => s.SharedWithUserId == userId) ||
                d.OwnerId == null ||
                d.Visibility == DocumentVisibility.Public || 
                d.Visibility == DocumentVisibility.Organization)
            .Distinct()
            .CountAsync();
        
        return count;
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
        
        if (document == null)
            return false;

        // Only owner can share (documents without owner cannot be shared)
        if (string.IsNullOrEmpty(document.OwnerId) || document.OwnerId != currentUserId)
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
        
        if (document == null)
            return false;

        // Only owner can change visibility (documents without owner cannot have visibility changed)
        if (string.IsNullOrEmpty(document.OwnerId) || document.OwnerId != userId)
            return false;

        document.Visibility = visibility;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Document> CreateDocumentAsync(Document document)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync();
        return document;
    }
}
