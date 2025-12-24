using DocN.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocN.Server.Services.DocumentProcessing;

/// <summary>
/// Document chunking service with sliding window and semantic boundaries
/// </summary>
public class ChunkingService : IChunkingService
{
    private readonly ILogger<ChunkingService> _logger;

    public ChunkingService(ILogger<ChunkingService> logger)
    {
        _logger = logger;
    }

    public List<string> ChunkDocument(string text, int chunkSize = 1000, int overlap = 200)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        var chunks = new List<string>();
        var currentPosition = 0;

        while (currentPosition < text.Length)
        {
            var remainingLength = text.Length - currentPosition;
            var currentChunkSize = Math.Min(chunkSize, remainingLength);

            var chunk = text.Substring(currentPosition, currentChunkSize);
            chunks.Add(chunk.Trim());

            // Move forward by chunk size minus overlap
            currentPosition += chunkSize - overlap;

            // Stop if we're at or past the end
            if (currentPosition >= text.Length - overlap)
            {
                break;
            }
        }

        _logger.LogInformation("Created {ChunkCount} chunks from {TextLength} characters",
            chunks.Count, text.Length);

        return chunks;
    }

    public List<string> ChunkDocumentSemantic(string text, int maxChunkSize = 1000)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string>();
        }

        var chunks = new List<string>();
        
        // Split by paragraphs first
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        var currentChunk = new System.Text.StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            var trimmedParagraph = paragraph.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmedParagraph))
            {
                continue;
            }

            // If adding this paragraph would exceed the max size
            if (currentChunk.Length + trimmedParagraph.Length > maxChunkSize)
            {
                // If we have content, save current chunk
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }

                // If the paragraph itself is larger than maxChunkSize, split it by sentences
                if (trimmedParagraph.Length > maxChunkSize)
                {
                    var sentenceChunks = SplitLongParagraph(trimmedParagraph, maxChunkSize);
                    chunks.AddRange(sentenceChunks);
                }
                else
                {
                    currentChunk.AppendLine(trimmedParagraph);
                    currentChunk.AppendLine();
                }
            }
            else
            {
                currentChunk.AppendLine(trimmedParagraph);
                currentChunk.AppendLine();
            }
        }

        // Add the last chunk if it has content
        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        _logger.LogInformation("Created {ChunkCount} semantic chunks from {TextLength} characters",
            chunks.Count, text.Length);

        return chunks;
    }

    private List<string> SplitLongParagraph(string paragraph, int maxChunkSize)
    {
        var chunks = new List<string>();
        
        // Split by sentences (simple approach using period, exclamation, question mark)
        var sentences = System.Text.RegularExpressions.Regex.Split(
            paragraph, @"(?<=[.!?])\s+");

        var currentChunk = new System.Text.StringBuilder();

        foreach (var sentence in sentences)
        {
            var trimmedSentence = sentence.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmedSentence))
            {
                continue;
            }

            // If adding this sentence would exceed max size
            if (currentChunk.Length + trimmedSentence.Length > maxChunkSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }

                // If sentence itself is too long, split by words
                if (trimmedSentence.Length > maxChunkSize)
                {
                    var wordChunks = SplitByWords(trimmedSentence, maxChunkSize);
                    chunks.AddRange(wordChunks);
                }
                else
                {
                    currentChunk.Append(trimmedSentence);
                    currentChunk.Append(" ");
                }
            }
            else
            {
                currentChunk.Append(trimmedSentence);
                currentChunk.Append(" ");
            }
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    private List<string> SplitByWords(string text, int maxChunkSize)
    {
        var chunks = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new System.Text.StringBuilder();

        foreach (var word in words)
        {
            if (currentChunk.Length + word.Length + 1 > maxChunkSize)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                    currentChunk.Clear();
                }
            }

            currentChunk.Append(word);
            currentChunk.Append(" ");
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }
}
