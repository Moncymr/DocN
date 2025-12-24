using DocN.Data.Models;
using System.Text;

namespace DocN.Data.Services;

/// <summary>
/// Service for chunking documents into smaller pieces for better vector search
/// </summary>
public interface IChunkingService
{
    /// <summary>
    /// Chunk a document's text into smaller pieces with overlap
    /// </summary>
    /// <param name="text">The text to chunk</param>
    /// <param name="chunkSize">Maximum characters per chunk</param>
    /// <param name="overlap">Number of overlapping characters between chunks</param>
    /// <returns>List of text chunks</returns>
    List<string> ChunkText(string text, int chunkSize = 1000, int overlap = 200);

    /// <summary>
    /// Create DocumentChunk entities from a document
    /// </summary>
    /// <param name="document">The document to chunk</param>
    /// <param name="chunkSize">Maximum characters per chunk</param>
    /// <param name="overlap">Number of overlapping characters between chunks</param>
    /// <returns>List of DocumentChunk entities (without embeddings)</returns>
    List<DocumentChunk> ChunkDocument(Document document, int chunkSize = 1000, int overlap = 200);

    /// <summary>
    /// Estimate token count for a text (rough estimation)
    /// </summary>
    /// <param name="text">Text to estimate</param>
    /// <returns>Estimated token count</returns>
    int EstimateTokenCount(string text);
}

public class ChunkingService : IChunkingService
{
    /// <summary>
    /// Chunk text using a sliding window approach with overlap
    /// </summary>
    public List<string> ChunkText(string text, int chunkSize = 1000, int overlap = 200)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        if (overlap >= chunkSize)
            throw new ArgumentException("Overlap must be less than chunk size");

        var chunks = new List<string>();
        var position = 0;

        while (position < text.Length)
        {
            // Calculate chunk end position
            var endPosition = Math.Min(position + chunkSize, text.Length);
            
            // If not at the end, try to break at a sentence boundary
            if (endPosition < text.Length)
            {
                // Look for sentence boundaries (. ! ?) within the last 100 characters
                var searchStart = Math.Max(position, endPosition - 100);
                var lastPeriod = text.LastIndexOf('.', endPosition - 1, endPosition - searchStart);
                var lastExclamation = text.LastIndexOf('!', endPosition - 1, endPosition - searchStart);
                var lastQuestion = text.LastIndexOf('?', endPosition - 1, endPosition - searchStart);
                
                var boundary = Math.Max(lastPeriod, Math.Max(lastExclamation, lastQuestion));
                
                if (boundary > searchStart)
                {
                    endPosition = boundary + 1; // Include the punctuation
                }
                // If no sentence boundary, try to break at a word boundary
                else
                {
                    var lastSpace = text.LastIndexOf(' ', endPosition - 1, Math.Min(100, endPosition - position));
                    if (lastSpace > position)
                    {
                        endPosition = lastSpace;
                    }
                }
            }

            // Extract the chunk
            var chunkText = text.Substring(position, endPosition - position).Trim();
            
            if (!string.IsNullOrWhiteSpace(chunkText))
            {
                chunks.Add(chunkText);
            }

            // Move position forward, accounting for overlap
            position = endPosition - overlap;
            
            // Ensure we make progress
            if (position <= 0 || position >= text.Length)
                break;
        }

        return chunks;
    }

    /// <summary>
    /// Create DocumentChunk entities from a document
    /// </summary>
    public List<DocumentChunk> ChunkDocument(Document document, int chunkSize = 1000, int overlap = 200)
    {
        var textChunks = ChunkText(document.ExtractedText, chunkSize, overlap);
        var documentChunks = new List<DocumentChunk>();
        
        var currentPosition = 0;
        for (int i = 0; i < textChunks.Count; i++)
        {
            var chunk = textChunks[i];
            var endPosition = currentPosition + chunk.Length;
            
            documentChunks.Add(new DocumentChunk
            {
                DocumentId = document.Id,
                ChunkIndex = i,
                ChunkText = chunk,
                TokenCount = EstimateTokenCount(chunk),
                StartPosition = currentPosition,
                EndPosition = endPosition,
                CreatedAt = DateTime.UtcNow
            });
            
            // Update position for next chunk (accounting for overlap)
            currentPosition = endPosition - overlap;
        }

        return documentChunks;
    }

    /// <summary>
    /// Rough estimation of token count (1 token ≈ 4 characters for English text)
    /// This is a simple heuristic; for precise counts, use a tokenizer
    /// </summary>
    public int EstimateTokenCount(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        // Simple estimation: 1 token ≈ 4 characters
        // This is a rough approximation that works reasonably well for English
        return (int)Math.Ceiling(text.Length / 4.0);
    }
}
