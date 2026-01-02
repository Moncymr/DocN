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
        _logger.LogInformation("Batch Embedding Processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingDocumentsAsync(stoppingToken);
                await ProcessPendingChunksAsync(stoppingToken);
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
            // Find documents without embeddings
            var pendingDocuments = await context.Documents
                .Where(d => d.EmbeddingVector768 == null && d.EmbeddingVector1536 == null && !string.IsNullOrEmpty(d.ExtractedText))
                .Take(10) // Process 10 at a time to avoid overload
                .ToListAsync(cancellationToken);

            if (!pendingDocuments.Any())
                return;

            _logger.LogInformation("Processing {Count} documents for embeddings", pendingDocuments.Count);

            foreach (var document in pendingDocuments)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // Generate embedding for document
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

                    // Create chunks for the document
                    var chunks = chunkingService.ChunkDocument(document);
                    
                    // Generate embeddings for chunks
                    foreach (var chunk in chunks)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        var chunkEmbedding = await embeddingService.GenerateEmbeddingAsync(chunk.ChunkText);
                        if (chunkEmbedding != null)
                        {
                            // Validate embedding dimensions
                            EmbeddingValidationHelper.ValidateEmbeddingDimensions(chunkEmbedding, _logger);
                            chunk.ChunkEmbedding = chunkEmbedding;
                            chunk.EmbeddingDimension = chunkEmbedding.Length;
                        }
                        
                        context.DocumentChunks.Add(chunk);
                    }

                    await context.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogInformation("Created {ChunkCount} chunks for document {Id}", 
                        chunks.Count, document.Id);
                }
                catch (DbUpdateException ex)
                {
                    // Extract the inner exception details for better error reporting
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    
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
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing document {Id}: {FileName}", 
                        document.Id, document.FileName);
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
            // Find chunks without embeddings
            var pendingChunks = await context.DocumentChunks
                .Where(c => c.ChunkEmbedding == null)
                .Take(20) // Process 20 chunks at a time
                .ToListAsync(cancellationToken);

            if (!pendingChunks.Any())
                return;

            _logger.LogInformation("Processing {Count} chunks for embeddings", pendingChunks.Count);

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
                        _logger.LogDebug("Generated embedding for chunk {Id} of document {DocumentId}", 
                            chunk.Id, chunk.DocumentId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing chunk {Id}", chunk.Id);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
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
            .Where(d => d.EmbeddingVector768 == null && d.EmbeddingVector1536 == null && !string.IsNullOrEmpty(d.ExtractedText))
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

        var totalChunks = await _context.DocumentChunks.CountAsync();
        var chunksWithoutEmbeddings = await _context.DocumentChunks
            .Where(c => c.ChunkEmbedding == null)
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
