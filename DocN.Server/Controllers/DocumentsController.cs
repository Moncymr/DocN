using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Models;

namespace DocN.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocArcContext _context;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(DocArcContext context, ILogger<DocumentsController> logger)
    {
        _context = context;
        _logger = logger;
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

            _logger.LogInformation("Created document {Id} - {FileName}", document.Id, document.FileName);
            return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating document");
            return StatusCode(500, "An error occurred while creating the document");
        }
    }
}
