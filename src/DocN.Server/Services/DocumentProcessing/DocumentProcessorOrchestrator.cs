using DocN.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocN.Server.Services.DocumentProcessing;

/// <summary>
/// Orchestrates document processing by selecting the appropriate extractor
/// </summary>
public class DocumentProcessorOrchestrator
{
    private readonly IEnumerable<IDocumentExtractor> _extractors;
    private readonly IChunkingService _chunkingService;
    private readonly ILogger<DocumentProcessorOrchestrator> _logger;

    public DocumentProcessorOrchestrator(
        IEnumerable<IDocumentExtractor> extractors,
        IChunkingService chunkingService,
        ILogger<DocumentProcessorOrchestrator> logger)
    {
        _extractors = extractors;
        _chunkingService = chunkingService;
        _logger = logger;
    }

    /// <summary>
    /// Extract text from document using the appropriate extractor
    /// </summary>
    public async Task<string> ExtractTextAsync(Stream stream, string fileName, string contentType)
    {
        try
        {
            _logger.LogInformation("Processing document: {FileName} ({ContentType})", fileName, contentType);

            // Find appropriate extractor
            var extractor = _extractors.FirstOrDefault(e => e.SupportsFileType(contentType, fileName));

            if (extractor == null)
            {
                _logger.LogWarning("No extractor found for {FileName} ({ContentType})", fileName, contentType);
                throw new NotSupportedException($"File type not supported: {contentType}");
            }

            // Extract text
            var text = await extractor.ExtractTextAsync(stream, fileName);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("No text extracted from {FileName}", fileName);
            }

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Extract text and create chunks
    /// </summary>
    public async Task<(string FullText, List<string> Chunks)> ExtractAndChunkAsync(
        Stream stream, 
        string fileName, 
        string contentType,
        int chunkSize = 1000,
        int overlap = 200,
        bool useSemanticChunking = true)
    {
        var fullText = await ExtractTextAsync(stream, fileName, contentType);

        if (string.IsNullOrWhiteSpace(fullText))
        {
            return (fullText, new List<string>());
        }

        var chunks = useSemanticChunking
            ? _chunkingService.ChunkDocumentSemantic(fullText, chunkSize)
            : _chunkingService.ChunkDocument(fullText, chunkSize, overlap);

        _logger.LogInformation("Extracted {TextLength} characters and created {ChunkCount} chunks from {FileName}",
            fullText.Length, chunks.Count, fileName);

        return (fullText, chunks);
    }

    /// <summary>
    /// Get list of supported file types
    /// </summary>
    public List<string> GetSupportedFileTypes()
    {
        return new List<string>
        {
            ".pdf",
            ".doc",
            ".docx",
            ".xls",
            ".xlsx",
            ".ppt",
            ".pptx"
        };
    }

    /// <summary>
    /// Check if a file type is supported
    /// </summary>
    public bool IsFileTypeSupported(string fileName, string contentType)
    {
        return _extractors.Any(e => e.SupportsFileType(contentType, fileName));
    }
}
