using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Models;
using DocN.Data.Services;

namespace DocN.Server.Controllers;

/// <summary>
/// Controller REST API per gestione completa documenti (CRUD, ricerca, elaborazione)
/// Espone endpoint per client frontend e integrazioni esterne
/// </summary>
/// <remarks>
/// Scopo: Fornire API RESTful per tutte le operazioni sui documenti
/// 
/// Operazioni supportate:
/// - GET /documents: Lista tutti documenti
/// - GET /documents/{id}: Dettagli documento specifico
/// - POST /documents: Crea nuovo documento
/// - PUT /documents/{id}: Aggiorna documento esistente
/// - DELETE /documents/{id}: Elimina documento
/// - GET /documents/search: Ricerca ibrida (vettoriale + full-text)
/// - POST /documents/{id}/process: Elabora documento (chunking + embeddings)
/// 
/// Dipendenze:
/// - DocArcContext: Accesso database documenti
/// - IChunkingService: Suddivisione documenti in chunk
/// - IBatchProcessingService: Elaborazione batch multipli documenti
/// - IEmbeddingService: Generazione embeddings vettoriali
/// 
/// Note:
/// - Tutti gli endpoint includono logging errori per diagnostica
/// - Response codes standard REST (200, 404, 500)
/// - Supporto paginazione per liste grandi (implementabile)
/// </remarks>
[ApiController]
[Route("[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocArcContext _context;
    private readonly ILogger<DocumentsController> _logger;
    private readonly IChunkingService _chunkingService;
    private readonly IBatchProcessingService _batchProcessingService;
    private readonly IEmbeddingService _embeddingService;

    public DocumentsController(
        DocArcContext context, 
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

    /// <summary>
    /// Ottiene lista completa di tutti i documenti ordinati per data upload (più recenti primi)
    /// </summary>
    /// <returns>Lista documenti con tutti i metadati eccetto i vettori embedding</returns>
    /// <response code="200">Ritorna la lista dei documenti (può essere vuota)</response>
    /// <response code="500">Errore interno del server durante recupero</response>
    /// <remarks>
    /// Scopo: Fornire lista completa documenti per visualizzazione in UI o export
    /// 
    /// Comportamento:
    /// - Recupera TUTTI i documenti (anche senza embeddings generati)
    /// - Ordinamento decrescente per data upload
    /// - Include metadati: nome file, categoria, tag, dimensione, owner, etc.
    /// - ESCLUDE vettori embedding per ottimizzazione performance
    /// 
    /// Output atteso:
    /// - Array JSON di oggetti Document
    /// - Ordinati da più recente a più vecchio
    /// - Lista vuota se nessun documento presente
    /// - Vettori embedding esclusi (non necessari per visualizzazione lista)
    /// 
    /// Performance:
    /// - Query ottimizzata con proiezione per escludere embeddings (768-1536 floats per documento)
    /// - Indice su UploadedAt per ordinamento veloce
    /// - Per grandi dataset (oltre 10k documenti), considerare paginazione
    /// 
    /// TODO (futuro):
    /// - Aggiungere paginazione (page, pageSize)
    /// - Aggiungere filtri (categoria, owner, dateRange)
    /// - Aggiungere sorting configurabile
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Document>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<Document>>> GetDocuments()
    {
        try
        {
            // PERFORMANCE FIX: Use projection to exclude embedding vectors which are not needed for list view
            // This significantly reduces data transfer and improves query speed
            // Embedding vectors (768 or 1536 floats per document) are only needed for semantic search, not for listing
            var documents = await _context.Documents
                .OrderByDescending(d => d.UploadedAt)
                .Select(d => new Document
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    ContentType = d.ContentType,
                    FileSize = d.FileSize,
                    ExtractedText = d.ExtractedText,
                    SuggestedCategory = d.SuggestedCategory,
                    CategoryReasoning = d.CategoryReasoning,
                    ActualCategory = d.ActualCategory,
                    AITagsJson = d.AITagsJson,
                    AIAnalysisDate = d.AIAnalysisDate,
                    ExtractedMetadataJson = d.ExtractedMetadataJson,
                    PageCount = d.PageCount,
                    DetectedLanguage = d.DetectedLanguage,
                    ProcessingStatus = d.ProcessingStatus,
                    ProcessingError = d.ProcessingError,
                    ChunkEmbeddingStatus = d.ChunkEmbeddingStatus,
                    Notes = d.Notes,
                    Visibility = d.Visibility,
                    // Exclude EmbeddingVector768 and EmbeddingVector1536 - not needed for list view
                    EmbeddingDimension = d.EmbeddingDimension,
                    UploadedAt = d.UploadedAt,
                    LastAccessedAt = d.LastAccessedAt,
                    AccessCount = d.AccessCount,
                    OwnerId = d.OwnerId,
                    TenantId = d.TenantId
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} documents (without embedding vectors for performance)", documents.Count);
            return Ok(documents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents");
            return StatusCode(500, "An error occurred while retrieving documents");
        }
    }

    /// <summary>
    /// Ottiene un singolo documento per ID con tutti i suoi metadati
    /// </summary>
    /// <param name="id">ID univoco del documento (chiave primaria)</param>
    /// <returns>Oggetto Document completo o 404 se non trovato</returns>
    /// <response code="200">Ritorna il documento richiesto</response>
    /// <response code="404">Documento con ID specificato non esiste</response>
    /// <response code="500">Errore interno del server</response>
    /// <remarks>
    /// Scopo: Recuperare dettagli completi di un documento specifico
    /// 
    /// Utilizzo tipico:
    /// - Visualizzazione dettagli documento in UI
    /// - Download documento
    /// - Modifica metadati (pre-popolamento form)
    /// 
    /// Output atteso:
    /// - Oggetto Document JSON con tutti i campi
    /// - Include: nome, path, categoria, tag, embeddings, owner, date, etc.
    /// - 404 se ID non esiste nel database
    /// 
    /// Performance:
    /// - Query ottimizzata con ricerca per chiave primaria (molto veloce)
    /// - Tipicamente meno di 10ms
    /// 
    /// Sicurezza:
    /// - TODO: Aggiungere controllo autorizzazione (solo owner o admin possono accedere)
    /// - TODO: Verificare visibilità documento (private/shared/public)
    /// </remarks>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Document), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Crea un nuovo documento
    /// </summary>
    /// <param name="document">Dati del documento da creare</param>
    /// <returns>Il documento creato con ID assegnato</returns>
    /// <response code="201">Documento creato con successo</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost]
    [ProducesResponseType(typeof(Document), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                    // Generate embeddings for chunks using parallel processing
                    var embeddedCount = await GenerateChunkEmbeddingsAsync(chunks, document.Id);
                    
                    _context.DocumentChunks.AddRange(chunks);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created {ChunkCount} chunks for document {Id}, {EmbeddedCount} with embeddings", 
                        chunks.Count, document.Id, embeddedCount);
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

    /// <summary>
    /// Aggiorna un documento esistente
    /// </summary>
    /// <param name="id">ID del documento da aggiornare</param>
    /// <param name="document">Dati aggiornati del documento</param>
    /// <returns>Il documento aggiornato</returns>
    /// <response code="200">Documento aggiornato con successo</response>
    /// <response code="400">Richiesta non valida (ID mismatch)</response>
    /// <response code="404">Documento non trovato</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(Document), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Document>> UpdateDocument(int id, Document document)
    {
        try
        {
            if (id != document.Id)
            {
                return BadRequest("Document ID mismatch");
            }

            var existingDocument = await _context.Documents.FindAsync(id);
            if (existingDocument == null)
            {
                return NotFound($"Document with ID {id} not found");
            }

            // Check if extracted text will change (before updating)
            bool extractedTextChanged = !string.IsNullOrEmpty(document.ExtractedText) && 
                                       document.ExtractedText != existingDocument.ExtractedText;

            // Update document properties
            existingDocument.FileName = document.FileName;
            existingDocument.ContentType = document.ContentType;
            existingDocument.FileSize = document.FileSize;
            existingDocument.ExtractedText = document.ExtractedText;
            existingDocument.SuggestedCategory = document.SuggestedCategory;
            existingDocument.CategoryReasoning = document.CategoryReasoning;
            existingDocument.ActualCategory = document.ActualCategory;
            existingDocument.Visibility = document.Visibility;
            existingDocument.Notes = document.Notes;
            existingDocument.PageCount = document.PageCount;
            existingDocument.DetectedLanguage = document.DetectedLanguage;
            existingDocument.ProcessingStatus = document.ProcessingStatus;
            existingDocument.ProcessingError = document.ProcessingError;
            existingDocument.AIAnalysisDate = document.AIAnalysisDate;
            existingDocument.AITagsJson = document.AITagsJson;
            existingDocument.ExtractedMetadataJson = document.ExtractedMetadataJson;

            // Update embedding if provided
            if (document.EmbeddingVector != null)
            {
                existingDocument.EmbeddingVector = document.EmbeddingVector;
                existingDocument.EmbeddingDimension = document.EmbeddingVector.Length;
            }

            // Update file path only if a new file was uploaded
            if (!string.IsNullOrEmpty(document.FilePath) && document.FilePath != existingDocument.FilePath)
            {
                existingDocument.FilePath = document.FilePath;
            }

            await _context.SaveChangesAsync();

            // Recreate chunks if extracted text changed
            if (extractedTextChanged)
            {
                // Delete existing chunks
                var existingChunks = await _context.DocumentChunks
                    .Where(c => c.DocumentId == id)
                    .ToListAsync();
                _context.DocumentChunks.RemoveRange(existingChunks);

                // Create new chunks with embeddings using parallel processing
                var newChunks = _chunkingService.ChunkDocument(existingDocument);
                if (newChunks.Any())
                {
                    var embeddedCount = await GenerateChunkEmbeddingsAsync(newChunks, id);
                    
                    _context.DocumentChunks.AddRange(newChunks);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated {ChunkCount} chunks for document {Id}, {EmbeddedCount} with embeddings", 
                        newChunks.Count, id, embeddedCount);
                }
            }

            _logger.LogInformation("Updated document {Id} - {FileName}", id, document.FileName);
            return Ok(existingDocument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {Id}", id);
            return StatusCode(500, "An error occurred while updating the document");
        }
    }

    /// <summary>
    /// Scarica il file di un documento
    /// </summary>
    /// <param name="id">ID del documento da scaricare</param>
    /// <returns>Il file del documento</returns>
    /// <response code="200">Ritorna il file del documento</response>
    /// <response code="404">Documento o file non trovato</response>
    /// <response code="500">Errore interno del server</response>
    [HttpGet("{id}/download")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Ottiene tutte le categorie uniche dei documenti
    /// </summary>
    /// <returns>Lista delle categorie disponibili</returns>
    /// <response code="200">Ritorna la lista delle categorie</response>
    /// <response code="500">Errore interno del server</response>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Ricrea gli embeddings per tutti i documenti
    /// </summary>
    /// <remarks>
    /// Questa operazione rigenerera gli embeddings vettoriali per tutti i documenti che hanno testo estratto.
    /// Può richiedere molto tempo per database di grandi dimensioni.
    /// </remarks>
    /// <returns>Statistiche dell'operazione di ricreazione</returns>
    /// <response code="200">Operazione completata con successo</response>
    /// <response code="500">Errore interno del server</response>
    [HttpPost("recreate-embeddings")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                        document.EmbeddingDimension = embedding.Length;
                        successCount++;
                    }

                    // Delete existing chunks and recreate them
                    var existingChunks = await _context.DocumentChunks
                        .Where(c => c.DocumentId == document.Id)
                        .ToListAsync();
                    
                    _context.DocumentChunks.RemoveRange(existingChunks);

                    // Create new chunks with embeddings using the same parallel processing helper
                    var newChunks = _chunkingService.ChunkDocument(document);
                    if (newChunks.Any())
                    {
                        var embeddedCount = await GenerateChunkEmbeddingsAsync(newChunks, document.Id);
                        _context.DocumentChunks.AddRange(newChunks);
                        _logger.LogInformation("Created {ChunkCount} chunks for document {Id}, {EmbeddedCount} with embeddings", 
                            newChunks.Count, document.Id, embeddedCount);
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

            var message = $"Embeddings recreated for {successCount} documents. Errors: {errorCount}";
            _logger.LogInformation(message);
            return Ok(new { message, successCount, errorCount, totalDocuments = documents.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recreating all embeddings");
            return StatusCode(500, "An error occurred while recreating embeddings");
        }
    }

    /// <summary>
    /// Helper method to generate embeddings for a list of document chunks
    /// Uses sequential processing with batching to avoid API rate limits and timeouts
    /// </summary>
    /// <param name="chunks">List of chunks to generate embeddings for</param>
    /// <param name="documentId">Document ID for logging purposes</param>
    /// <param name="maxConcurrency">Maximum number of concurrent embedding requests (default: 1 for reliability)</param>
    /// <param name="batchSize">Number of chunks to process in each batch (default: 10 for better progress feedback)</param>
    /// <returns>Number of chunks that successfully got embeddings</returns>
    private async Task<int> GenerateChunkEmbeddingsAsync(List<DocumentChunk> chunks, int documentId, int maxConcurrency = 1, int batchSize = 10)
    {
        var successCount = 0;
        var totalBatches = (int)Math.Ceiling(chunks.Count / (double)batchSize);
        var currentBatch = 0;
        
        // Reuse semaphore across all batches for efficiency
        using var semaphore = new SemaphoreSlim(maxConcurrency);
        
        // Process chunks in batches to avoid memory pressure with large documents
        for (int i = 0; i < chunks.Count; i += batchSize)
        {
            currentBatch++;
            var batch = chunks.Skip(i).Take(batchSize).ToList();
            
            _logger.LogInformation("Processing batch {CurrentBatch}/{TotalBatches} ({ChunkCount} chunks) for document {DocumentId}", 
                currentBatch, totalBatches, batch.Count, documentId);
            
            var tasks = new List<Task<bool>>();
            
            foreach (var chunk in batch)
            {
                tasks.Add(GenerateSingleChunkEmbeddingAsync(chunk, documentId, semaphore));
            }
            
            var results = await Task.WhenAll(tasks);
            var batchSuccess = results.Count(r => r);
            successCount += batchSuccess;
            
            _logger.LogInformation("Batch {CurrentBatch}/{TotalBatches} completed: {Success}/{Total} chunks embedded successfully", 
                currentBatch, totalBatches, batchSuccess, batch.Count);
        }
        
        return successCount;
    }

    /// <summary>
    /// Generate embedding for a single chunk with semaphore-controlled concurrency
    /// </summary>
    /// <param name="chunk">The document chunk to generate embedding for</param>
    /// <param name="documentId">Document ID for logging purposes</param>
    /// <param name="semaphore">Semaphore to control concurrency across multiple chunk operations</param>
    /// <returns>True if embedding was successfully generated, false otherwise</returns>
    private async Task<bool> GenerateSingleChunkEmbeddingAsync(DocumentChunk chunk, int documentId, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            var chunkEmbedding = await _embeddingService.GenerateEmbeddingAsync(chunk.ChunkText);
            if (chunkEmbedding != null)
            {
                // ChunkEmbedding setter automatically sets EmbeddingDimension
                chunk.ChunkEmbedding = chunkEmbedding;
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate embedding for chunk {ChunkIndex} of document {DocumentId}", 
                chunk.ChunkIndex, documentId);
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }
}
