using Microsoft.AspNetCore.Mvc;
using DocN.Server.Services.DocumentProcessing;
using DocN.Core.Interfaces;
using DocN.Data;
using DocN.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DocN.Server.Controllers;

/// <summary>
/// Controller for document management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentProcessorOrchestrator _documentProcessor;
    private readonly IEmbeddingService _embeddingService;
    private readonly DocNDbContext _dbContext;
    private readonly ILogger<DocumentsController> _logger;
    private readonly string _documentsPath;

    public DocumentsController(
        DocumentProcessorOrchestrator documentProcessor,
        IEmbeddingService embeddingService,
        DocNDbContext dbContext,
        ILogger<DocumentsController> logger,
        IConfiguration configuration)
    {
        _documentProcessor = documentProcessor;
        _embeddingService = embeddingService;
        _dbContext = dbContext;
        _logger = logger;
        _documentsPath = configuration["Storage:DocumentsPath"] ?? "./Documents";
        
        // Ensure documents directory exists
        Directory.CreateDirectory(_documentsPath);
    }

    /// <summary>
    /// Upload a document
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(100_000_000)] // 100MB limit
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file,
        [FromForm] string? category = null,
        [FromForm] string? tags = null,
        [FromForm] int visibility = 1,
        [FromForm] bool autoExtract = true,
        [FromForm] bool generateEmbeddings = true)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            // Save file
            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(_documentsPath, $"{Guid.NewGuid()}_{fileName}");
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create document record
            var document = new Document
            {
                FileName = fileName,
                FilePath = filePath,
                ContentType = file.ContentType,
                FileSize = file.Length,
                ActualCategory = category ?? "Altro",
                OwnerId = "default-user", // TODO: Get from auth
                Visibility = visibility,
                Tags = tags,
                UploadedAt = DateTime.UtcNow,
                IsProcessed = false
            };

            _dbContext.Documents.Add(document);
            await _dbContext.SaveChangesAsync();

            // Process document in background if requested
            if (autoExtract)
            {
                _ = Task.Run(async () => await ProcessDocumentAsync(document.Id, generateEmbeddings));
            }

            return Ok(new
            {
                id = document.Id,
                fileName = document.FileName,
                message = "Document uploaded successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { error = "Error uploading document", details = ex.Message });
        }
    }

    /// <summary>
    /// Get user documents
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDocuments(
        [FromQuery] string? search = null,
        [FromQuery] string? category = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _dbContext.Documents.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(d => d.FileName.Contains(search) || 
                                        (d.ExtractedText != null && d.ExtractedText.Contains(search)));
            }

            if (!string.IsNullOrEmpty(category) && category != "Tutte")
            {
                query = query.Where(d => d.ActualCategory == category);
            }

            var total = await query.CountAsync();
            var documents = await query
                .OrderByDescending(d => d.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new
                {
                    d.Id,
                    d.FileName,
                    d.ContentType,
                    Category = d.ActualCategory,
                    FileSize = FormatFileSize(d.FileSize),
                    UploadedAt = d.UploadedAt.ToString("yyyy-MM-dd HH:mm"),
                    Visibility = d.Visibility == 1 ? "Private" : d.Visibility == 2 ? "Department" : "Public",
                    d.IsProcessed
                })
                .ToListAsync();

            return Ok(new
            {
                documents,
                total,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents");
            return StatusCode(500, new { error = "Error getting documents" });
        }
    }

    /// <summary>
    /// Get document by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(int id)
    {
        try
        {
            var document = await _dbContext.Documents
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            return Ok(new
            {
                document.Id,
                document.FileName,
                document.ContentType,
                Category = document.ActualCategory,
                document.FileSize,
                document.ExtractedText,
                UploadedAt = document.UploadedAt.ToString("yyyy-MM-dd HH:mm"),
                document.IsProcessed,
                ChunkCount = document.Chunks.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document {Id}", id);
            return StatusCode(500, new { error = "Error getting document" });
        }
    }

    /// <summary>
    /// Download document
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        try
        {
            var document = await _dbContext.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            if (!System.IO.File.Exists(document.FilePath))
            {
                return NotFound(new { error = "File not found on disk" });
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(document.FilePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, document.ContentType, document.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {Id}", id);
            return StatusCode(500, new { error = "Error downloading document" });
        }
    }

    /// <summary>
    /// Delete document
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var document = await _dbContext.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound(new { error = "Document not found" });
            }

            // Delete file from disk
            if (System.IO.File.Exists(document.FilePath))
            {
                System.IO.File.Delete(document.FilePath);
            }

            // Delete from database
            _dbContext.Documents.Remove(document);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Document deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {Id}", id);
            return StatusCode(500, new { error = "Error deleting document" });
        }
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("~/api/dashboard/stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var total = await _dbContext.Documents.CountAsync();
            var today = await _dbContext.Documents
                .CountAsync(d => d.UploadedAt.Date == DateTime.UtcNow.Date);
            
            var categoryStats = await _dbContext.Documents
                .GroupBy(d => d.ActualCategory)
                .Select(g => new
                {
                    Name = g.Key ?? "Altro",
                    Count = g.Count()
                })
                .ToListAsync();

            var recentDocs = await _dbContext.Documents
                .OrderByDescending(d => d.UploadedAt)
                .Take(5)
                .Select(d => new
                {
                    d.Id,
                    Name = d.FileName,
                    UploadedAt = d.UploadedAt.ToString("yyyy-MM-dd HH:mm")
                })
                .ToListAsync();

            return Ok(new
            {
                totalDocuments = total,
                uploadedToday = today,
                totalConversations = 0, // TODO: Implement
                categoryStats,
                recentDocuments = recentDocs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats");
            return StatusCode(500, new { error = "Error getting statistics" });
        }
    }

    private async Task ProcessDocumentAsync(int documentId, bool generateEmbeddings)
    {
        try
        {
            var document = await _dbContext.Documents.FindAsync(documentId);
            if (document == null) return;

            using var fileStream = new FileStream(document.FilePath, FileMode.Open);
            
            // Extract text and chunks
            var (fullText, chunks) = await _documentProcessor.ExtractAndChunkAsync(
                fileStream,
                document.FileName,
                document.ContentType,
                useSemanticChunking: true);

            document.ExtractedText = fullText;
            
            // Generate embeddings if requested
            if (generateEmbeddings && !string.IsNullOrEmpty(fullText))
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(fullText);
                if (embedding != null)
                {
                    document.EmbeddingVector = string.Join(",", embedding);
                }

                // Create chunks with embeddings
                foreach (var (chunkText, index) in chunks.Select((c, i) => (c, i)))
                {
                    var chunkEmbedding = await _embeddingService.GenerateEmbeddingAsync(chunkText);
                    var documentChunk = new DocumentChunk
                    {
                        DocumentId = document.Id,
                        ChunkIndex = index,
                        ChunkText = chunkText,
                        ChunkEmbedding = chunkEmbedding != null ? string.Join(",", chunkEmbedding) : null,
                        TokenCount = chunkText.Length / 4 // Rough estimate
                    };
                    _dbContext.DocumentChunks.Add(documentChunk);
                }
            }

            document.IsProcessed = true;
            document.ProcessedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Document {Id} processed successfully", documentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {Id}", documentId);
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
