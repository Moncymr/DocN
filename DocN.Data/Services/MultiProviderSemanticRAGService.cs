using DocN.Core.Interfaces;
using DocN.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DocN.Data.Services;

/// <summary>
/// Implementation of ISemanticRAGService using MultiProviderAIService
/// Supports Gemini, OpenAI, and Azure OpenAI configured in the database
/// </summary>
public class MultiProviderSemanticRAGService : ISemanticRAGService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MultiProviderSemanticRAGService> _logger;
    private readonly IMultiProviderAIService _aiService;
    private readonly DocN.Data.Services.IEmbeddingService _embeddingService;

    public MultiProviderSemanticRAGService(
        ApplicationDbContext context,
        ILogger<MultiProviderSemanticRAGService> logger,
        IMultiProviderAIService aiService,
        DocN.Data.Services.IEmbeddingService embeddingService)
    {
        _context = context;
        _logger = logger;
        _aiService = aiService;
        _embeddingService = embeddingService;
    }

    public async Task<SemanticRAGResponse> GenerateResponseAsync(
        string query,
        string userId,
        int? conversationId = null,
        List<int>? specificDocumentIds = null,
        int topK = 5)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Generating RAG response using MultiProvider for query: {Query}, User: {UserId}",
                query, userId);

            // Step 1: Search for relevant documents
            var relevantDocs = await SearchDocumentsAsync(query, userId, topK, 0.7);

            if (!relevantDocs.Any())
            {
                _logger.LogWarning("No relevant documents found for query: {Query}", query);
                return new SemanticRAGResponse
                {
                    Answer = "I couldn't find any relevant documents to answer your question. Please try rephrasing or upload more documents.",
                    SourceDocuments = new List<RelevantDocumentResult>(),
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            // Step 2: Load conversation history
            var conversationHistory = await LoadConversationHistoryAsync(conversationId);

            // Step 3: Build context from relevant documents
            var documentContext = BuildDocumentContext(relevantDocs);

            // Step 4: Generate response using MultiProviderAIService
            var systemPrompt = CreateSystemPrompt();
            var userPrompt = BuildUserPrompt(query, documentContext, conversationHistory);
            var answer = await _aiService.GenerateChatCompletionAsync(systemPrompt, userPrompt);

            // Step 5: Save conversation
            var savedConversationId = await SaveConversationAsync(
                conversationId,
                userId,
                query,
                answer,
                relevantDocs.Select(d => d.DocumentId).ToList());

            stopwatch.Stop();

            var response = new SemanticRAGResponse
            {
                Answer = answer,
                SourceDocuments = relevantDocs,
                ConversationId = savedConversationId,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                FromCache = false,
                Metadata = new Dictionary<string, object>
                {
                    ["documentsRetrieved"] = relevantDocs.Count,
                    ["topSimilarityScore"] = relevantDocs.FirstOrDefault()?.SimilarityScore ?? 0,
                    ["hasConversationHistory"] = conversationHistory.Any(),
                    ["provider"] = _aiService.GetCurrentChatProvider()
                }
            };

            _logger.LogInformation(
                "RAG response generated in {ElapsedMs}ms with {DocCount} documents using {Provider}",
                stopwatch.ElapsedMilliseconds, relevantDocs.Count, _aiService.GetCurrentChatProvider());

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating RAG response for query: {Query}", query);
            return new SemanticRAGResponse
            {
                Answer = $"An error occurred while processing your request: {ex.Message}",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamingResponseAsync(
        string query,
        string userId,
        int? conversationId = null,
        List<int>? specificDocumentIds = null)
    {
        _logger.LogInformation("Streaming not supported in MultiProvider mode, using non-streaming response");

        // Fallback to non-streaming
        var response = await GenerateResponseAsync(query, userId, conversationId, specificDocumentIds, 5);
        yield return response.Answer;
    }

    public async Task<List<RelevantDocumentResult>> SearchDocumentsAsync(
        string query,
        string userId,
        int topK = 10,
        double minSimilarity = 0.7)
    {
        try
        {
            _logger.LogDebug("Searching documents for: {Query}", query);

            // Generate query embedding using MultiProviderAIService
            var queryEmbedding = await _aiService.GenerateEmbeddingAsync(query);
            if (queryEmbedding == null)
            {
                _logger.LogWarning("Failed to generate query embedding");
                return new List<RelevantDocumentResult>();
            }

            return await SearchDocumentsWithEmbeddingAsync(queryEmbedding, userId, topK, minSimilarity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents for query: {Query}", query);
            return new List<RelevantDocumentResult>();
        }
    }

    public async Task<List<RelevantDocumentResult>> SearchDocumentsWithEmbeddingAsync(
        float[] queryEmbedding,
        string userId,
        int topK = 10,
        double minSimilarity = 0.7)
    {
        try
        {
            _logger.LogDebug("Searching documents with embedding for user: {UserId}", userId);

            if (queryEmbedding == null || queryEmbedding.Length == 0)
            {
                _logger.LogWarning("Query embedding is null or empty");
                return new List<RelevantDocumentResult>();
            }

            // Get all documents with embeddings for the user
            var documents = await _context.Documents
                .Where(d => d.OwnerId == userId && d.EmbeddingVector != null)
                .ToListAsync();

            _logger.LogInformation("Found {Count} documents with embeddings for user {UserId}", documents.Count, userId);

            // Calculate similarity scores for documents
            var scoredDocs = new List<(Document doc, double score)>();
            foreach (var doc in documents)
            {
                if (doc.EmbeddingVector == null) continue;

                var similarity = CalculateCosineSimilarity(queryEmbedding, doc.EmbeddingVector);
                if (similarity >= minSimilarity)
                {
                    scoredDocs.Add((doc, similarity));
                }
            }

            // Get chunks for better precision
            var chunks = await _context.DocumentChunks
                .Include(c => c.Document)
                .Where(c => c.Document!.OwnerId == userId && c.ChunkEmbedding != null)
                .ToListAsync();

            var scoredChunks = new List<(DocumentChunk chunk, double score)>();
            foreach (var chunk in chunks)
            {
                if (chunk.ChunkEmbedding == null) continue;

                var similarity = CalculateCosineSimilarity(queryEmbedding, chunk.ChunkEmbedding);
                if (similarity >= minSimilarity)
                {
                    scoredChunks.Add((chunk, similarity));
                }
            }

            // Combine document-level and chunk-level results
            var results = new List<RelevantDocumentResult>();

            // Add chunk-based results (higher priority)
            var topChunks = scoredChunks.OrderByDescending(x => x.score).Take(topK).ToList();
            var existingDocIds = new HashSet<int>();

            foreach (var (chunk, score) in topChunks)
            {
                if (chunk.Document == null) continue;

                results.Add(new RelevantDocumentResult
                {
                    DocumentId = chunk.DocumentId,
                    FileName = chunk.Document.FileName,
                    Category = chunk.Document.ActualCategory,
                    SimilarityScore = score,
                    RelevantChunk = chunk.ChunkText,
                    ChunkIndex = chunk.ChunkIndex
                });
                existingDocIds.Add(chunk.DocumentId);
            }

            // Add document-level results if we don't have enough chunks
            if (results.Count < topK)
            {
                foreach (var (doc, score) in scoredDocs.OrderByDescending(x => x.score))
                {
                    if (results.Count >= topK)
                        break;

                    if (existingDocIds.Contains(doc.Id))
                        continue;

                    results.Add(new RelevantDocumentResult
                    {
                        DocumentId = doc.Id,
                        FileName = doc.FileName,
                        Category = doc.ActualCategory,
                        SimilarityScore = score,
                        ExtractedText = doc.ExtractedText
                    });
                    existingDocIds.Add(doc.Id);
                }
            }

            _logger.LogDebug("Returning {Count} total results", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents with embedding for user: {UserId}", userId);
            return new List<RelevantDocumentResult>();
        }
    }

    private string BuildDocumentContext(List<RelevantDocumentResult> documents)
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== RELEVANT DOCUMENTS ===");
        builder.AppendLine();

        for (int i = 0; i < documents.Count; i++)
        {
            var doc = documents[i];
            builder.AppendLine($"[DOCUMENT {i + 1}]");
            builder.AppendLine($"File: {doc.FileName}");
            builder.AppendLine($"Category: {doc.Category ?? "Uncategorized"}");
            builder.AppendLine($"Relevance: {doc.SimilarityScore:P0}");
            builder.AppendLine();
            builder.AppendLine("Content:");
            builder.AppendLine(doc.RelevantChunk ?? doc.ExtractedText ?? "No content available");
            builder.AppendLine();
            builder.AppendLine("---");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private string CreateSystemPrompt()
    {
        return @"You are an intelligent document assistant powered by RAG (Retrieval-Augmented Generation).
Your role is to answer questions accurately based on the provided documents.

GUIDELINES:
- Use ONLY information from the provided documents
- Cite sources using [Document N] format
- If information is not in the documents, clearly state that
- Be concise but thorough
- Maintain professional and helpful tone
- If asked about multiple documents, synthesize information appropriately

RESPONSE FORMAT:
1. Provide a direct answer to the question
2. Support with relevant details from documents
3. Cite sources clearly
4. If uncertain, acknowledge limitations";
    }

    private string BuildUserPrompt(string query, string documentContext, List<Message> conversationHistory)
    {
        var builder = new StringBuilder();

        builder.AppendLine(documentContext);
        builder.AppendLine();

        if (conversationHistory.Any())
        {
            builder.AppendLine("=== CONVERSATION HISTORY ===");
            foreach (var msg in conversationHistory)
            {
                builder.AppendLine($"{msg.Role.ToUpper()}: {msg.Content}");
            }
            builder.AppendLine();
        }

        builder.AppendLine("=== CURRENT QUESTION ===");
        builder.AppendLine(query);

        return builder.ToString();
    }

    private async Task<List<Message>> LoadConversationHistoryAsync(int? conversationId)
    {
        if (!conversationId.HasValue)
            return new List<Message>();

        try
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId.Value)
                .OrderByDescending(m => m.Timestamp)
                .Take(10)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading conversation history");
            return new List<Message>();
        }
    }

    private async Task<int> SaveConversationAsync(
        int? conversationId,
        string userId,
        string query,
        string answer,
        List<int> documentIds)
    {
        try
        {
            Conversation conversation;

            if (conversationId.HasValue)
            {
                conversation = await _context.Conversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == conversationId.Value)
                    ?? throw new InvalidOperationException($"Conversation {conversationId} not found");
            }
            else
            {
                conversation = new Conversation
                {
                    UserId = userId,
                    Title = TruncateText(query, 60),
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow,
                    Messages = new List<Message>()
                };
                _context.Conversations.Add(conversation);
            }

            // Add user message
            conversation.Messages.Add(new Message
            {
                Role = "user",
                Content = query,
                Timestamp = DateTime.UtcNow
            });

            // Add assistant message
            conversation.Messages.Add(new Message
            {
                Role = "assistant",
                Content = answer,
                ReferencedDocumentIds = documentIds,
                Timestamp = DateTime.UtcNow
            });

            conversation.LastMessageAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return conversation.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving conversation");
            return conversationId ?? 0;
        }
    }

    private double CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
        {
            _logger.LogWarning(
                "Vector dimension mismatch: vector1 has {Length1} dimensions, vector2 has {Length2} dimensions",
                vector1.Length, vector2.Length);
            return 0;
        }

        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            magnitude1 += vector1[i] * vector1[i];
            magnitude2 += vector2[i] * vector2[i];
        }

        magnitude1 = Math.Sqrt(magnitude1);
        magnitude2 = Math.Sqrt(magnitude2);

        if (magnitude1 == 0 || magnitude2 == 0)
            return 0;

        return dotProduct / (magnitude1 * magnitude2);
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;

        return text.Substring(0, maxLength - 3) + "...";
    }
}
