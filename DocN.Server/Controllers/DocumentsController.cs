using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Models;
using DocN.Data.Services;

namespace DocN.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DocumentsController> _logger;
    private readonly IChunkingService _chunkingService;
    private readonly IBatchProcessingService _batchProcessingService;
    private readonly IEmbeddingService _embeddingService;

    public DocumentsController(
        ApplicationDbContext context, 
        ILogger<DocumentsController> logger,
        IChunkingService chunkingService,
        IBatchProcessingService batchProcessingService,
        IEmbeddingService embeddingService)
    {
        _context = context;
        _logger = logger;
        _chunkingService = chunkingService;
        _batchProcessingService = batchProcessingService;
        _embeddingService = embeddingService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Document>>> GetDocuments()
    {
        try
        {
            // KEY FIX: Return all documents regardless of whether Vector field is populated
            // This addresses the issue where documents without vectors were not being shown
            var documents = await _context.Documents
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} documents", documents.Count);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents");
            return StatusCode(500, "An error occurred while retrieving documents");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Document>> GetDocument(int id)
    {
        try
        {
            var document = await _context.Documents.FindAsync(id);

            if (document == null)
            {
                return NotFound();
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving document {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the document");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Document>> CreateDocument(Document document)
    {
        try
        {
            document.UploadedAt = DateTime.UtcNow;
            // Vector can be null initially and calculated later
            
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            // Create chunks for the document if it has extracted text
            if (!string.IsNullOrEmpty(document.ExtractedText))
            {
                var chunks = _chunkingService.ChunkDocument(document);
                if (chunks.Any())
                {
                    _context.DocumentChunks.AddRange(chunks);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created {ChunkCount} chunks for document {Id}", chunks.Count, document.Id);
                }
            }

            _logger.LogInformation("Created document {Id} - {FileName}", document.Id, document.FileName);
            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document");
            return StatusCode(500, "An error occurred while creating the document");
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDocument(int id)
    {
        try
        {
            var document = await _context.Documents.FindAsync(id);

            if (document == null)
            {
                return NotFound("Documento non trovato");
            }

            if (!System.IO.File.Exists(document.FilePath))
            {
                return NotFound("File non trovato sul server");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(document.FilePath);
            
            // Update access statistics
            document.AccessCount++;
            document.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return File(fileBytes, document.ContentType, document.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {Id}", id);
            return StatusCode(500, "Errore durante il download del documento");
        }
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        try
        {
            // Get all unique categories from documents in a single query
            var categories = await _context.Documents
                .Select(d => new { d.ActualCategory, d.SuggestedCategory })
                .ToListAsync();

            // Process in memory to get unique categories
            var uniqueCategories = categories
                .SelectMany(d => new[] { d.ActualCategory, d.SuggestedCategory })
                .Where(c => !string.IsNullOrEmpty(c) && c != "Uncategorized")
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            _logger.LogInformation("Retrieved {Count} unique categories", uniqueCategories.Count);
            return Ok(uniqueCategories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return StatusCode(500, "An error occurred while retrieving categories");
        }
    }

    [HttpPost("recreate-embeddings")]
    public async Task<ActionResult> RecreateAllEmbeddings()
    {
        try
        {
            _logger.LogInformation("Starting recreation of all embeddings");

            // Get all documents
            var documents = await _context.Documents
                .Where(d => !string.IsNullOrEmpty(d.ExtractedText))
                .ToListAsync();

            _logger.LogInformation("Found {Count} documents to process", documents.Count);

            int successCount = 0;
            int errorCount = 0;

            foreach (var document in documents)
            {
                try
                {
                    // Regenerate embedding for document
                    var embedding = await _embeddingService.GenerateEmbeddingAsync(document.ExtractedText);
                    if (embedding != null)
                    {
                        document.EmbeddingVector = embedding;
                        successCount++;
                    }

                    // Delete existing chunks and recreate them
                    var existingChunks = await _context.DocumentChunks
                        .Where(c => c.DocumentId == document.Id)
                        .ToListAsync();
                    
                    _context.DocumentChunks.RemoveRange(existingChunks);

                    // Create new chunks with embeddings
                    var newChunks = _chunkingService.ChunkDocument(document);
                    foreach (var chunk in newChunks)
                    {
                        var chunkEmbedding = await _embeddingService.GenerateEmbeddingAsync(chunk.ChunkText);
                        if (chunkEmbedding != null)
                        {
                            chunk.ChunkEmbedding = chunkEmbedding;
                        }
                        _context.DocumentChunks.Add(chunk);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Recreated embeddings for document {Id}: {FileName}", 
                        document.Id, document.FileName);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, "Error recreating embeddings for document {Id}", document.Id);
                }
            }

            var message = $"Embeddings ricreati per {successCount} documenti. Errori: {errorCount}";
            _logger.LogInformation(message);
            return Ok(new { message, successCount, errorCount, totalDocuments = documents.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recreating all embeddings");
            return StatusCode(500, "An error occurred while recreating embeddings");
        }
    }
}
