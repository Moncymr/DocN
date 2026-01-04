using DocN.Data.Constants;
using DocN.Data.Models;
using DocN.Data.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocN.Data.Services;

/// <summary>
/// Background service for batch processing document embeddings
/// </summary>
public class BatchEmbeddingProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BatchEmbeddingProcessor> _logger;
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(30);

    public BatchEmbeddingProcessor(
        IServiceProvider serviceProvider,
        ILogger<BatchEmbeddingProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Batch Embedding Processor started - will run every {Interval} seconds", 
            _processingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("=== Starting batch processing cycle ===");
                await ProcessPendingDocumentsAsync(stoppingToken);
                await ProcessPendingChunksAsync(stoppingToken);
                _logger.LogDebug("=== Batch processing cycle complete ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch embedding processor");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }

        _logger.LogInformation("Batch Embedding Processor stopped");
    }

    /// <summary>
    /// Process documents that don't have embeddings yet
    /// </summary>
    private async Task ProcessPendingDocumentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var chunkingService = scope.ServiceProvider.GetRequiredService<IChunkingService>();

        try
        {
            // First, log the overall status distribution to help diagnose issues
            var statusStats = await context.Documents
                .GroupBy(d => d.ChunkEmbeddingStatus)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            
            _logger.LogInformation("Document status distribution: {Stats}", 
                string.Join(", ", statusStats.Select(s => $"{s.Status}={s.Count}")));
            
            // Count documents with Pending status but no ExtractedText
            var pendingWithoutText = await context.Documents
                .CountAsync(d => d.ChunkEmbeddingStatus == ChunkEmbeddingStatus.Pending && 
                                string.IsNullOrEmpty(d.ExtractedText), cancellationToken);
            
            if (pendingWithoutText > 0)
            {
                _logger.LogWarning("{Count} documents have Pending status but no ExtractedText (will be skipped)", 
                    pendingWithoutText);
            }
            
            // Find documents that need chunks created
            // Include both Pending documents AND Processing documents that have no chunks
            // (Processing documents without chunks are likely stuck from a previous failed attempt)
            var pendingDocuments = await context.Documents
                .Where(d => !string.IsNullOrEmpty(d.ExtractedText) && 
                           (d.ChunkEmbeddingStatus == ChunkEmbeddingStatus.Pending ||
                            (d.ChunkEmbeddingStatus == ChunkEmbeddingStatus.Processing &&
                             !context.DocumentChunks.Any(c => c.DocumentId == d.Id))))
                .Take(10) // Process 10 at a time to avoid overload
                .ToListAsync(cancellationToken);

            if (!pendingDocuments.Any())
            {
                _logger.LogDebug("No documents found that need chunks created (this is normal if all docs are Processing with chunks or Completed)");
                return;
            }

            // Count how many are Pending vs Processing for better diagnostics
            var pendingCount = pendingDocuments.Count(d => d.ChunkEmbeddingStatus == ChunkEmbeddingStatus.Pending);
            var processingCount = pendingDocuments.Count(d => d.ChunkEmbeddingStatus == ChunkEmbeddingStatus.Processing);
            
            _logger.LogInformation("Processing {Count} documents for chunk creation and embeddings ({PendingCount} Pending, {ProcessingCount} Processing without chunks)", 
                pendingDocuments.Count, pendingCount, processingCount);

            foreach (var document in pendingDocuments)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Log if this is a retry of a stuck Processing document
                    if (document.ChunkEmbeddingStatus == ChunkEmbeddingStatus.Processing)
                    {
                        _logger.LogWarning("Retrying document {Id}: {FileName} - was stuck in Processing status without chunks", 
                            document.Id, document.FileName);
                    }
                    
                    // Generate embedding for document only if it doesn't have one
                    if (document.EmbeddingVector == null && !string.IsNullOrWhiteSpace(document.ExtractedText))
                    {
                        var embedding = await embeddingService.GenerateEmbeddingAsync(document.ExtractedText);
                        if (embedding != null)
                        {
                            document.EmbeddingVector = embedding;
                            document.EmbeddingDimension = embedding.Length;
                            
                            // Log embedding info before saving
                            _logger.LogInformation("Generated embedding for document {Id}: {FileName}", 
                                document.Id, document.FileName);
                            _logger.LogInformation("Embedding details - Length: {Length}, First 5 values: [{Values}]",
                                embedding.Length, 
                                string.Join(", ", embedding.Take(5).Select(v => v.ToString("F6"))));
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Document {Id} already has embedding, skipping embedding generation", 
                            document.Id);
                    }

                    // Create chunks for the document
                    var chunks = chunkingService.ChunkDocument(document);
                    _logger.LogInformation("ChunkDocument returned {ChunkCount} chunks for document {Id}", 
                        chunks.Count, document.Id);
                    
                    if (chunks.Count == 0)
                    {
                        _logger.LogWarning("No chunks created for document {Id}: {FileName}. ExtractedText length: {Length}", 
                            document.Id, document.FileName, document.ExtractedText?.Length ?? 0);
                        
                        // Mark as NotRequired if no chunks can be created
                        document.ChunkEmbeddingStatus = ChunkEmbeddingStatus.NotRequired;
                        await context.SaveChangesAsync(cancellationToken);
                        continue;
                    }
                    
                    // Update status to Processing
                    document.ChunkEmbeddingStatus = ChunkEmbeddingStatus.Processing;
                    await context.SaveChangesAsync(cancellationToken);
                    
                    // Generate embeddings for chunks
                    var chunksWithEmbeddings = 0;
                    var failedChunks = 0;
                    foreach (var chunk in chunks)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        _logger.LogDebug("Processing chunk {ChunkIndex} of document {DocumentId}", 
                            chunk.ChunkIndex, document.Id);

                        try
                        {
                            var chunkEmbedding = await embeddingService.GenerateEmbeddingAsync(chunk.ChunkText);
                            if (chunkEmbedding != null)
                            {
                                // Validate embedding dimensions
                                EmbeddingValidationHelper.ValidateEmbeddingDimensions(chunkEmbedding, _logger);
                                chunk.ChunkEmbedding = chunkEmbedding;
                                chunk.EmbeddingDimension = chunkEmbedding.Length;
                                chunksWithEmbeddings++;
                                _logger.LogDebug("Successfully generated embedding for chunk {ChunkIndex} of document {DocumentId}: {Dimension} dimensions", 
                                    chunk.ChunkIndex, document.Id, chunkEmbedding.Length);
                            }
                            else
                            {
                                failedChunks++;
                                _logger.LogWarning("Embedding generation returned null for chunk {ChunkIndex} of document {DocumentId}", 
                                    chunk.ChunkIndex, document.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            failedChunks++;
                            _logger.LogError(ex, "Failed to generate embedding for chunk {ChunkIndex} of document {DocumentId}: {Error}", 
                                chunk.ChunkIndex, document.Id, ex.Message);
                        }
                        
                        context.DocumentChunks.Add(chunk);
                    }

                    // Only mark as Completed if ALL chunks have embeddings
                    // Otherwise keep as Processing so ProcessPendingChunksAsync can retry
                    if (chunksWithEmbeddings == chunks.Count)
                    {
                        document.ChunkEmbeddingStatus = ChunkEmbeddingStatus.Completed;
                        _logger.LogInformation("Created {ChunkCount} chunks for document {Id} - All chunks have embeddings - Status: Completed", 
                            chunks.Count, document.Id);
                    }
                    else
                    {
                        // Some chunks don't have embeddings, keep as Processing
                        // ProcessPendingChunksAsync will retry failed chunks
                        _logger.LogWarning("Created {TotalChunks} chunks for document {Id}, but only {SuccessCount} have embeddings ({FailedCount} failed). Status remains Processing for retry.",
                            chunks.Count, document.Id, chunksWithEmbeddings, failedChunks);
                    }
                    
                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    // Extract the inner exception details for better error reporting
                    if (ex is DbUpdateException dbEx)
                    {
                        var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                        
                        // Check for vector dimension mismatch error
                        if (EmbeddingValidationHelper.IsVectorDimensionMismatchError(innerMessage))
                        {
                            _logger.LogError(ex, "Vector dimension mismatch for document {Id}: {FileName}", 
                                document.Id, document.FileName);
                            _logger.LogError("Database save failed with dimension mismatch. Original error: {Error}", innerMessage);
                        }
                        else
                        {
                            _logger.LogError(ex, "Database error processing document {Id}: {FileName}", 
                                document.Id, document.FileName);
                        }
                    }
                    else
                    {
                        _logger.LogError(ex, "Error processing document {Id}: {FileName}", 
                            document.Id, document.FileName);
                    }
                    
                    // Reset status to Pending so it can be retried later
                    try
                    {
                        document.ChunkEmbeddingStatus = ChunkEmbeddingStatus.Pending;
                        await context.SaveChangesAsync(cancellationToken);
                    }
                    catch (Exception saveEx)
                    {
                        _logger.LogError(saveEx, "Failed to reset status for document {Id}", document.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessPendingDocumentsAsync");
        }
    }

    /// <summary>
    /// Process document chunks that don't have embeddings yet
    /// </summary>
    private async Task ProcessPendingChunksAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();

        try
        {
            // First, log overall statistics about chunks needing processing
            var totalChunksWithoutEmbeddings = await context.DocumentChunks
                .CountAsync(c => c.ChunkEmbedding768 == null && c.ChunkEmbedding1536 == null, cancellationToken);
            
            if (totalChunksWithoutEmbeddings == 0)
            {
                _logger.LogDebug("No chunks without embeddings found");
                return;
            }
            
            _logger.LogInformation("Found {TotalChunks} total chunks without embeddings, processing up to 50", 
                totalChunksWithoutEmbeddings);
            
            // Find chunks without embeddings - prioritize chunks from Processing documents
            // This ensures documents in Processing status get completed first
            // Note: OrderByDescending on boolean is acceptable for current scale (50 chunks per batch)
            // If performance becomes an issue, can split into two separate queries with UNION
            var pendingChunks = await context.DocumentChunks
                .Include(c => c.Document)
                .Where(c => c.ChunkEmbedding768 == null && c.ChunkEmbedding1536 == null)
                .OrderByDescending(c => c.Document!.ChunkEmbeddingStatus == ChunkEmbeddingStatus.Processing)
                .ThenBy(c => c.DocumentId)
                .ThenBy(c => c.ChunkIndex)
                .Take(50) // Increased from 20 to 50 for faster processing
                .ToListAsync(cancellationToken);

            if (!pendingChunks.Any())
            {
                _logger.LogWarning("Query returned 0 chunks but {TotalChunks} chunks without embeddings exist. Possible query issue!", 
                    totalChunksWithoutEmbeddings);
                return;
            }

            // Log statistics about what we're processing
            var processingDocCount = pendingChunks.Count(c => c.Document!.ChunkEmbeddingStatus == ChunkEmbeddingStatus.Processing);
            var pendingDocCount = pendingChunks.Count(c => c.Document!.ChunkEmbeddingStatus == ChunkEmbeddingStatus.Pending);
            _logger.LogInformation("Processing {Count} chunks for embeddings ({ProcessingDocs} from Processing docs, {PendingDocs} from Pending docs)", 
                pendingChunks.Count, processingDocCount, pendingDocCount);

            var successfulEmbeddings = 0;
            var failedEmbeddings = 0;

            foreach (var chunk in pendingChunks)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var embedding = await embeddingService.GenerateEmbeddingAsync(chunk.ChunkText);
                    if (embedding != null)
                    {
                        chunk.ChunkEmbedding = embedding;
                        chunk.EmbeddingDimension = embedding.Length;
                        successfulEmbeddings++;
                        _logger.LogDebug("Generated embedding for chunk {Id} of document {DocumentId}", 
                            chunk.Id, chunk.DocumentId);
                    }
                    else
                    {
                        failedEmbeddings++;
                        _logger.LogWarning("Embedding generation returned null for chunk {Id} of document {DocumentId}", 
                            chunk.Id, chunk.DocumentId);
                    }
                }
                catch (Exception ex)
                {
                    failedEmbeddings++;
                    _logger.LogError(ex, "Error processing chunk {Id} of document {DocumentId}: {Error}", 
                        chunk.Id, chunk.DocumentId, ex.Message);
                }
            }
            
            _logger.LogInformation("Embedding generation complete: {Success} successful, {Failed} failed out of {Total} chunks", 
                successfulEmbeddings, failedEmbeddings, pendingChunks.Count);

            try
            {
                var savedCount = await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully saved {Count} chunk embedding(s) to database (SaveChanges returned {SavedCount})", 
                    pendingChunks.Count, savedCount);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "CRITICAL: Failed to save chunk embeddings to database. Changes were not persisted!");
                throw; // Re-throw to be caught by outer exception handler
            }
            
            // Check if any documents now have all their chunks with embeddings and update their status
            var documentIds = pendingChunks.Select(c => c.DocumentId).Distinct().ToList();
            var updatedDocuments = 0;
            
            _logger.LogInformation("Checking {Count} document(s) to see if they can be marked as Completed", documentIds.Count);
            
            foreach (var documentId in documentIds)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                    
                var document = await context.Documents.FindAsync(new object[] { documentId }, cancellationToken);
                if (document == null)
                {
                    _logger.LogWarning("Document {DocumentId} not found when checking for completion", documentId);
                    continue;
                }
                
                if (document.ChunkEmbeddingStatus != ChunkEmbeddingStatus.Processing)
                {
                    _logger.LogDebug("Document {DocumentId} has status {Status}, skipping completion check", 
                        documentId, document.ChunkEmbeddingStatus);
                    continue;
                }
                
                // Count chunks directly in database to get accurate count (avoid EF Core cache issues)
                var totalChunks = await context.DocumentChunks
                    .Where(c => c.DocumentId == documentId)
                    .CountAsync(cancellationToken);
                    
                var chunksWithEmbeddings = await context.DocumentChunks
                    .Where(c => c.DocumentId == documentId && 
                               (c.ChunkEmbedding768 != null || c.ChunkEmbedding1536 != null))
                    .CountAsync(cancellationToken);
                
                _logger.LogInformation("Document {DocumentId}: {CompletedChunks}/{TotalChunks} chunks have embeddings", 
                    documentId, chunksWithEmbeddings, totalChunks);
                
                if (totalChunks > 0 && chunksWithEmbeddings == totalChunks)
                {
                    document.ChunkEmbeddingStatus = ChunkEmbeddingStatus.Completed;
                    _logger.LogInformation("✅ Document {Id}: {FileName} now has all {ChunkCount} chunks with embeddings - Status updated to Completed", 
                        documentId, document.FileName, totalChunks);
                    updatedDocuments++;
                }
                else if (chunksWithEmbeddings > 0)
                {
                    _logger.LogInformation("Document {DocumentId} still has {PendingCount} chunks without embeddings, will retry in next cycle", 
                        documentId, totalChunks - chunksWithEmbeddings);
                }
            }
            
            // Save all document status updates in a single batch
            if (updatedDocuments > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated {Count} document(s) status to Completed", updatedDocuments);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessPendingChunksAsync");
        }
    }
}

/// <summary>
/// Service for manually triggering batch processing
/// </summary>
public interface IBatchProcessingService
{
    /// <summary>
    /// Process a specific document immediately
    /// </summary>
    Task ProcessDocumentAsync(int documentId);

    /// <summary>
    /// Process all pending documents
    /// </summary>
    Task ProcessAllPendingAsync();

    /// <summary>
    /// Get statistics about pending processing
    /// </summary>
    Task<BatchProcessingStats> GetStatsAsync();
}

public class BatchProcessingStats
{
    public int DocumentsWithoutEmbeddings { get; set; }
    public int ChunksWithoutEmbeddings { get; set; }
    public int TotalDocuments { get; set; }
    public int TotalChunks { get; set; }
    public double EmbeddingCoveragePercentage { get; set; }
}

public class BatchProcessingService : IBatchProcessingService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChunkingService _chunkingService;
    private readonly ILogger<BatchProcessingService> _logger;

    public BatchProcessingService(
        ApplicationDbContext context,
        IEmbeddingService embeddingService,
        IChunkingService chunkingService,
        ILogger<BatchProcessingService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _chunkingService = chunkingService;
        _logger = logger;
    }

    public async Task ProcessDocumentAsync(int documentId)
    {
        var document = await _context.Documents.FindAsync(documentId);
        if (document == null)
        {
            _logger.LogWarning("Document {Id} not found", documentId);
            return;
        }

        try
        {
            // Generate embedding if not present
            if (document.EmbeddingVector == null && !string.IsNullOrEmpty(document.ExtractedText))
            {
                var embedding = await _embeddingService.GenerateEmbeddingAsync(document.ExtractedText);
                if (embedding != null)
                {
                    document.EmbeddingVector = embedding;
                    document.EmbeddingDimension = embedding.Length;
                    
                    // Log embedding info before saving
                    _logger.LogInformation("Generated embedding for document {Id}: {FileName} - Length: {Length}",
                        document.Id, document.FileName, embedding.Length);
                    _logger.LogInformation("Embedding first 5 values: [{Values}]",
                        string.Join(", ", embedding.Take(5).Select(v => v.ToString("F6"))));
                }
            }

            // Create chunks if they don't exist
            var existingChunks = await _context.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .CountAsync();

            if (existingChunks == 0)
            {
                var chunks = _chunkingService.ChunkDocument(document);
                foreach (var chunk in chunks)
                {
                    var chunkEmbedding = await _embeddingService.GenerateEmbeddingAsync(chunk.ChunkText);
                    if (chunkEmbedding != null)
                    {
                        // Validate embedding dimensions
                        EmbeddingValidationHelper.ValidateEmbeddingDimensions(chunkEmbedding, _logger);
                        chunk.ChunkEmbedding = chunkEmbedding;
                        chunk.EmbeddingDimension = chunkEmbedding.Length;
                    }
                    _context.DocumentChunks.Add(chunk);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully processed document {Id}", documentId);
        }
        catch (DbUpdateException ex)
        {
            // Extract the inner exception details for better error reporting
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            
            // Check for vector dimension mismatch error
            if (EmbeddingValidationHelper.IsVectorDimensionMismatchError(innerMessage))
            {
                _logger.LogError(ex, "Vector dimension mismatch for document {Id}", documentId);
                throw new InvalidOperationException(
                    EmbeddingValidationHelper.CreateDimensionMismatchErrorMessage(0, innerMessage),
                    ex);
            }
            
            _logger.LogError(ex, "Error processing document {Id}", documentId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {Id}", documentId);
            throw;
        }
    }

    public async Task ProcessAllPendingAsync()
    {
        var pendingDocuments = await _context.Documents
            .Where(d => !string.IsNullOrEmpty(d.ExtractedText) && 
                       ((d.EmbeddingVector768 == null && d.EmbeddingVector1536 == null) || 
                        !_context.DocumentChunks.Any(c => c.DocumentId == d.Id)))
            .Select(d => d.Id)
            .ToListAsync();

        _logger.LogInformation("Processing {Count} pending documents", pendingDocuments.Count);

        foreach (var docId in pendingDocuments)
        {
            try
            {
                await ProcessDocumentAsync(docId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {Id}", docId);
                // Continue with next document
            }
        }
    }

    public async Task<BatchProcessingStats> GetStatsAsync()
    {
        var totalDocs = await _context.Documents.CountAsync();
        var docsWithoutEmbeddings = await _context.Documents
            .Where(d => d.EmbeddingVector768 == null && d.EmbeddingVector1536 == null)
            .CountAsync();
        
        var docsWithoutChunks = await _context.Documents
            .Where(d => !_context.DocumentChunks.Any(c => c.DocumentId == d.Id))
            .CountAsync();

        var totalChunks = await _context.DocumentChunks.CountAsync();
        var chunksWithoutEmbeddings = await _context.DocumentChunks
            .Where(c => c.ChunkEmbedding768 == null && c.ChunkEmbedding1536 == null)
            .CountAsync();

        var docsWithEmbeddings = totalDocs - docsWithoutEmbeddings;
        var coveragePercentage = totalDocs > 0 
            ? (double)docsWithEmbeddings / totalDocs * 100 
            : 0;

        return new BatchProcessingStats
        {
            DocumentsWithoutEmbeddings = docsWithoutEmbeddings,
            ChunksWithoutEmbeddings = chunksWithoutEmbeddings,
            TotalDocuments = totalDocs,
            TotalChunks = totalChunks,
            EmbeddingCoveragePercentage = coveragePercentage
        };
    }
}
