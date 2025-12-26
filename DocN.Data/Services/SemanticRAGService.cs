using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DocN.Data.Models;
using DocN.Core.Interfaces;
using System.Text;
using System.Runtime.CompilerServices;

#pragma warning disable SKEXP0110 // Agents are experimental
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only

namespace DocN.Data.Services;

/// <summary>
/// Advanced RAG service using Microsoft Semantic Kernel and Agent Framework
/// Implements vector-based retrieval and intelligent chat on uploaded documents
/// </summary>
public class SemanticRAGService : ISemanticRAGService
{
    private readonly Kernel _kernel;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SemanticRAGService> _logger;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChatCompletionService _chatService;
    private readonly ICacheService _cacheService;

    // Semantic Kernel Agents for RAG pipeline
    private ChatCompletionAgent? _retrievalAgent;
    private ChatCompletionAgent? _synthesisAgent;
    private AgentGroupChat? _agentChat;

    public SemanticRAGService(
        Kernel kernel,
        ApplicationDbContext context,
        ILogger<SemanticRAGService> logger,
        IEmbeddingService embeddingService,
        ICacheService cacheService)
    {
        _kernel = kernel;
        _context = context;
        _logger = logger;
        _embeddingService = embeddingService;
        _cacheService = cacheService;
        _chatService = kernel.GetRequiredService<IChatCompletionService>();
        
        InitializeAgents();
    }

    /// <summary>
    /// Initialize Semantic Kernel agents for RAG workflow
    /// </summary>
    private void InitializeAgents()
    {
        try
        {
            // Create Retrieval Agent - responsible for finding relevant documents
            _retrievalAgent = new ChatCompletionAgent
            {
                Name = "RetrievalAgent",
                Instructions = @"You are a specialized retrieval agent. Your role is to:
1. Understand the user's query intent
2. Identify key concepts and entities
3. Determine which documents are most relevant
4. Extract the most pertinent information from documents
5. Provide structured information to the synthesis agent

Always be precise and focus on relevance.",
                Kernel = _kernel
            };

            // Create Synthesis Agent - responsible for generating natural language answers
            _synthesisAgent = new ChatCompletionAgent
            {
                Name = "SynthesisAgent",
                Instructions = @"You are an expert synthesis agent. Your role is to:
1. Analyze information provided by the retrieval agent
2. Generate clear, accurate, and natural language answers
3. Cite sources appropriately using document references
4. Maintain conversation context and coherence
5. Be concise yet comprehensive

Always cite your sources using [Document N] format where N is the document number.",
                Kernel = _kernel
            };

            _logger.LogInformation("Semantic Kernel agents initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Semantic Kernel agents");
        }
    }

    /// <inheritdoc/>
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
                "Generating RAG response for query: {Query}, User: {UserId}, Conversation: {ConvId}",
                query, userId, conversationId);

            // Check cache first
            var cacheKey = $"rag:{userId}:{query}:{string.Join(",", specificDocumentIds ?? new List<int>())}";
            var cachedResponse = await _cacheService.GetAsync<SemanticRAGResponse>(cacheKey);
            if (cachedResponse != null)
            {
                _logger.LogInformation("Returning cached RAG response");
                cachedResponse.FromCache = true;
                return cachedResponse;
            }

            // Step 1: Vector-based document retrieval
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

            // Step 4: Generate response using Semantic Kernel
            var answer = await GenerateAnswerWithSemanticKernelAsync(
                query,
                documentContext,
                conversationHistory);

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
                    ["hasConversationHistory"] = conversationHistory.Any()
                }
            };

            // Cache the response for 5 minutes
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5));

            _logger.LogInformation(
                "RAG response generated in {ElapsedMs}ms with {DocCount} documents",
                stopwatch.ElapsedMilliseconds, relevantDocs.Count);

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

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> GenerateStreamingResponseAsync(
        string query,
        string userId,
        int? conversationId = null,
        List<int>? specificDocumentIds = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating streaming RAG response for query: {Query}", query);

        // Step 1: Retrieve relevant documents
        var relevantDocs = await SearchDocumentsAsync(query, userId, 5, 0.7);

        if (!relevantDocs.Any())
        {
            yield return "I couldn't find any relevant documents to answer your question.";
            yield break;
        }

        // Step 2: Build context
        var documentContext = BuildDocumentContext(relevantDocs);
        var conversationHistory = await LoadConversationHistoryAsync(conversationId);

        // Step 3: Create chat history
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(CreateSystemPrompt());
        chatHistory.AddSystemMessage($"DOCUMENT CONTEXT:\n{documentContext}");

        foreach (var msg in conversationHistory)
        {
            if (msg.Role == "user")
                chatHistory.AddUserMessage(msg.Content);
            else if (msg.Role == "assistant")
                chatHistory.AddAssistantMessage(msg.Content);
        }

        chatHistory.AddUserMessage(query);

        // Step 4: Stream the response
        var settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 2000,
            Temperature = 0.7,
            TopP = 0.9
        };

        var fullAnswer = new StringBuilder();

        await foreach (var chunk in _chatService.GetStreamingChatMessageContentsAsync(
            chatHistory, settings, _kernel, cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                fullAnswer.Append(chunk.Content);
                yield return chunk.Content;
            }
        }

        // Save conversation after streaming completes
        await SaveConversationAsync(
            conversationId,
            userId,
            query,
            fullAnswer.ToString(),
            relevantDocs.Select(d => d.DocumentId).ToList());
    }

    /// <inheritdoc/>
    public async Task<List<RelevantDocumentResult>> SearchDocumentsAsync(
        string query,
        string userId,
        int topK = 10,
        double minSimilarity = 0.7)
    {
        try
        {
            _logger.LogDebug("Searching documents with vector embeddings for: {Query}", query);

            // Generate query embedding
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
            if (queryEmbedding == null)
            {
                _logger.LogWarning("Failed to generate query embedding");
                return new List<RelevantDocumentResult>();
            }

            // Get all documents with embeddings for the user
            var documents = await _context.Documents
                .Where(d => d.OwnerId == userId && d.EmbeddingVector != null)
                .ToListAsync();

            // Calculate similarity scores
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
            foreach (var (chunk, score) in scoredChunks.OrderByDescending(x => x.score).Take(topK))
            {
                if (chunk.Document == null) continue;

                results.Add(new RelevantDocumentResult
                {
                    DocumentId = chunk.DocumentId,
                    FileName = chunk.Document.FileName,
                    Category = chunk.Document.ActualCategory,
                    SimilarityScore = score,
                    RelevantChunk = chunk.Content,
                    ChunkIndex = chunk.ChunkIndex
                });
            }

            // Add document-level results if we don't have enough chunks
            if (results.Count < topK)
            {
                var existingDocIds = results.Select(r => r.DocumentId).ToHashSet();
                foreach (var (doc, score) in scoredDocs.OrderByDescending(x => x.score))
                {
                    if (existingDocIds.Contains(doc.Id)) continue;

                    results.Add(new RelevantDocumentResult
                    {
                        DocumentId = doc.Id,
                        FileName = doc.FileName,
                        Category = doc.ActualCategory,
                        SimilarityScore = score,
                        RelevantChunk = TruncateText(doc.ExtractedText, 500),
                        ExtractedText = doc.ExtractedText
                    });

                    if (results.Count >= topK) break;
                }
            }

            _logger.LogDebug(
                "Found {Count} relevant documents/chunks for query: {Query}",
                results.Count, query);

            return results.Take(topK).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents for query: {Query}", query);
            return new List<RelevantDocumentResult>();
        }
    }

    /// <summary>
    /// Generate answer using Semantic Kernel with document context
    /// </summary>
    private async Task<string> GenerateAnswerWithSemanticKernelAsync(
        string query,
        string documentContext,
        List<Message> conversationHistory)
    {
        try
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(CreateSystemPrompt());
            chatHistory.AddSystemMessage($"DOCUMENT CONTEXT:\n{documentContext}");

            // Add conversation history
            foreach (var msg in conversationHistory)
            {
                if (msg.Role == "user")
                    chatHistory.AddUserMessage(msg.Content);
                else if (msg.Role == "assistant")
                    chatHistory.AddAssistantMessage(msg.Content);
            }

            // Add current query
            chatHistory.AddUserMessage(query);

            // Generate response
            var settings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 2000,
                Temperature = 0.7,
                TopP = 0.9
            };

            var result = await _chatService.GetChatMessageContentAsync(
                chatHistory, settings, _kernel);

            return result.Content ?? "I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating answer with Semantic Kernel");
            return $"Error generating response: {ex.Message}";
        }
    }

    /// <summary>
    /// Build formatted document context for the AI
    /// </summary>
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

    /// <summary>
    /// Create system prompt for the AI
    /// </summary>
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

    /// <summary>
    /// Load conversation history from database
    /// </summary>
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

    /// <summary>
    /// Save conversation to database
    /// </summary>
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

    /// <summary>
    /// Calculate cosine similarity between two vectors
    /// </summary>
    private double CalculateCosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            return 0;

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

    /// <summary>
    /// Truncate text to specified length
    /// </summary>
    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;

        return text.Substring(0, maxLength - 3) + "...";
    }
}
