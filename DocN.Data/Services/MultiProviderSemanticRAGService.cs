using DocN.Core.Interfaces;
using DocN.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DocN.Data.Services;

/// <summary>
/// Implementation of ISemanticRAGService using MultiProviderAIService.
/// Supports Gemini, OpenAI, and Azure OpenAI configured in the database.
/// </summary>
/// <remarks>
/// Provider AI viene iniettato via DI e carica configurazione automaticamente.
/// Per dettagli: Vedi RAG_PROVIDER_INITIALIZATION_GUIDE.md
/// </remarks>
public class MultiProviderSemanticRAGService : ISemanticRAGService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MultiProviderSemanticRAGService> _logger;
    private readonly IMultiProviderAIService _aiService;

    public MultiProviderSemanticRAGService(
        ApplicationDbContext context,
        ILogger<MultiProviderSemanticRAGService> logger,
        IMultiProviderAIService aiService)
    {
        _context = context;
        _logger = logger;
        _aiService = aiService;
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

            // Step 1: Search for relevant documents with improved strategy
            // Try with lower threshold first (0.5 instead of 0.7) to find more documents
            var relevantDocs = await SearchDocumentsAsync(query, userId, topK, 0.5);
            
            // If no results found, try keyword/fallback search
            if (!relevantDocs.Any())
            {
                _logger.LogInformation("No documents found with semantic search, trying keyword fallback for query: {Query}", query);
                relevantDocs = await FallbackKeywordSearch(query, userId, topK);
            }

            // Step 2: Load conversation history
            var conversationHistory = await LoadConversationHistoryAsync(conversationId);

            // Step 3: Generate response
            string answer;
            string documentContext;

            if (!relevantDocs.Any())
            {
                _logger.LogWarning("No relevant documents found for query: {Query}. Returning message about internal documents only.", query);

                // Don't generate AI response - inform user about internal document focus
                answer = @"❌ Non ho trovato documenti rilevanti nel sistema RAG interno per rispondere alla tua domanda.

Questo sistema di chat AI funziona PRINCIPALMENTE sui documenti interni che hai caricato nel sistema. 

📄 Per ottenere risposte accurate:
1. Assicurati di aver caricato i documenti necessari nella sezione 'Upload'
2. Verifica che i documenti siano stati elaborati correttamente (con embedding generati)
3. Riprova la tua domanda una volta caricati i documenti pertinenti

💡 Suggerimento: Puoi verificare i tuoi documenti caricati nella sezione 'Documents' del sistema.

Il sistema non fornisce risposte basate su conoscenze generali, ma solo su informazioni contenute nei documenti che hai caricato.";
                
                documentContext = "No relevant documents found";
            }
            else
            {
                // Build context from relevant documents
                documentContext = BuildDocumentContext(relevantDocs);

                // Step 4: Generate response using MultiProviderAIService with document context
                var systemPrompt = CreateSystemPrompt();
                var userPrompt = BuildUserPrompt(query, documentContext, conversationHistory);
                answer = await _aiService.GenerateChatCompletionAsync(systemPrompt, userPrompt);
            }

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
            _logger.LogInformation("Searching documents for query: '{Query}', User: {UserId}, TopK: {TopK}, MinSimilarity: {MinSimilarity}",
                query, userId, topK, minSimilarity);

            // Generate query embedding using MultiProviderAIService (with automatic fallback)
            var queryEmbedding = await _aiService.GenerateEmbeddingAsync(query);
            if (queryEmbedding == null)
            {
                _logger.LogError("Failed to generate query embedding - all providers failed or returned null");
                return new List<RelevantDocumentResult>();
            }

            _logger.LogInformation("Successfully generated query embedding with {Dimensions} dimensions using provider: {Provider}",
                queryEmbedding.Length, _aiService.GetCurrentEmbeddingProvider());

            var results = await SearchDocumentsWithEmbeddingAsync(queryEmbedding, userId, topK, minSimilarity);

            _logger.LogInformation("Document search completed: Found {Count} relevant documents", results.Count);

            return results;
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
            _logger.LogInformation("Searching documents with embedding for user: {UserId}, Embedding dimensions: {Dimensions}",
                userId, queryEmbedding?.Length ?? 0);

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

            if (!documents.Any())
            {
                _logger.LogWarning("No documents with embeddings found for user {UserId}. User needs to upload documents or wait for embeddings to be generated.", userId);
                
                // Check if user has ANY documents (even without embeddings)
                var totalDocs = await _context.Documents
                    .Where(d => d.OwnerId == userId)
                    .CountAsync();
                    
                if (totalDocs > 0)
                {
                    _logger.LogWarning("User {UserId} has {TotalDocs} documents but NONE have embeddings generated. Documents need to be processed.", userId, totalDocs);
                }
                
                return new List<RelevantDocumentResult>();
            }

            // Calculate similarity scores for documents
            var scoredDocs = new List<(Document doc, double score)>();
            var allScores = new List<double>(); // Track all scores for diagnostics
            
            foreach (var doc in documents)
            {
                if (doc.EmbeddingVector == null) continue;

                var similarity = CalculateCosineSimilarity(queryEmbedding, doc.EmbeddingVector);
                allScores.Add(similarity);
                
                if (similarity >= minSimilarity)
                {
                    scoredDocs.Add((doc, similarity));
                    _logger.LogDebug("Document {FileName} (ID: {DocId}) matched with similarity {Score:P1}", 
                        doc.FileName, doc.Id, similarity);
                }
            }

            // Log diagnostic info about scores
            if (allScores.Any())
            {
                _logger.LogInformation(
                    "Similarity scores - Min: {Min:P1}, Max: {Max:P1}, Avg: {Avg:P1}, Threshold: {Threshold:P0}", 
                    allScores.Min(), allScores.Max(), allScores.Average(), minSimilarity);
            }

            _logger.LogInformation("Found {Count} documents above similarity threshold {Threshold:P0}", scoredDocs.Count, minSimilarity);

            // Get chunks for better precision
            var chunks = await _context.DocumentChunks
                .Include(c => c.Document)
                .Where(c => c.Document!.OwnerId == userId && c.ChunkEmbedding != null)
                .ToListAsync();

            _logger.LogInformation("Found {Count} chunks with embeddings for user {UserId}", chunks.Count, userId);

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

            _logger.LogInformation("Found {Count} chunks above similarity threshold {Threshold:P0}", scoredChunks.Count, minSimilarity);

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

            _logger.LogInformation("Returning {Count} total results (threshold: {Threshold:P0})", results.Count, minSimilarity);

            if (!results.Any())
            {
                _logger.LogWarning("No documents matched the similarity threshold of {Threshold:P0}. Consider lowering the threshold or checking if document embeddings are compatible with query embeddings.", minSimilarity);
            }

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
        return @"Sei un assistente documentale intelligente basato su RAG (Retrieval-Augmented Generation).
Il tuo ruolo è rispondere accuratamente alle domande basandoti sui documenti forniti.

LINEE GUIDA:
- Usa SOLO le informazioni presenti nei documenti forniti
- Cita le fonti usando il formato [Documento N] e indica il nome del file tra parentesi: (nome_file.pdf)
- Se l'informazione non è presente nei documenti, dichiaralo chiaramente
- Sii conciso ma completo
- Mantieni un tono professionale e disponibile
- Se vengono richiesti più documenti, sintetizza le informazioni in modo appropriato
- IMPORTANTE: Rispondi sempre in italiano

FORMATO DELLA RISPOSTA:
1. Fornisci una risposta diretta alla domanda
2. Supporta con dettagli rilevanti dai documenti
3. Cita chiaramente le fonti con [Documento N] e il nome del file tra parentesi (nome_file.pdf)
4. Alla fine della risposta, elenca i documenti consultati in formato: 'Documenti consultati: (file1.pdf), (file2.docx)'
5. Se non sei sicuro, riconosci i limiti";
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

    /// <summary>
    /// Fallback keyword search quando la ricerca semantica non trova risultati.
    /// Cerca per nome file, numero documento, o parole chiave nel testo.
    /// </summary>
    private async Task<List<RelevantDocumentResult>> FallbackKeywordSearch(string query, string userId, int topK)
    {
        try
        {
            _logger.LogInformation("Executing fallback keyword search for query: {Query}", query);
            
            var queryLower = query.ToLower();
            var results = new List<RelevantDocumentResult>();
            
            // Estrai numeri dalla query (es: "documento 775" -> 775)
            var numbers = System.Text.RegularExpressions.Regex.Matches(query, @"\d+")
                .Select(m => m.Value)
                .ToList();
            
            // Cerca documenti per:
            // 1. Nome file che contiene la query o numeri
            // 2. Testo estratto che contiene le parole chiave
            // 3. ID documento se un numero è specificato
            
            var documents = await _context.Documents
                .Where(d => d.OwnerId == userId)
                .Where(d => 
                    // Nome file contiene query
                    d.FileName.ToLower().Contains(queryLower) ||
                    // Testo estratto contiene query
                    (d.ExtractedText != null && d.ExtractedText.ToLower().Contains(queryLower)) ||
                    // Nome file contiene uno dei numeri
                    numbers.Any(num => d.FileName.Contains(num)) ||
                    // Testo estratto contiene uno dei numeri
                    (d.ExtractedText != null && numbers.Any(num => d.ExtractedText.Contains(num))))
                .Take(topK)
                .ToListAsync();
            
            _logger.LogInformation("Fallback search found {Count} documents", documents.Count);
            
            foreach (var doc in documents)
            {
                // Cerca anche nei chunks per trovare il contenuto più rilevante
                var relevantChunk = await _context.DocumentChunks
                    .Where(c => c.DocumentId == doc.Id)
                    .Where(c => c.ChunkText.ToLower().Contains(queryLower) || 
                               numbers.Any(num => c.ChunkText.Contains(num)))
                    .OrderBy(c => c.ChunkIndex)
                    .FirstOrDefaultAsync();
                
                results.Add(new RelevantDocumentResult
                {
                    DocumentId = doc.Id,
                    FileName = doc.FileName,
                    Category = doc.ActualCategory,
                    SimilarityScore = 0.6, // Score fisso per keyword match
                    RelevantChunk = relevantChunk?.ChunkText,
                    ChunkIndex = relevantChunk?.ChunkIndex,
                    ExtractedText = doc.ExtractedText
                });
            }
            
            if (results.Any())
            {
                _logger.LogInformation("Fallback search successful: returning {Count} documents", results.Count);
            }
            else
            {
                _logger.LogWarning("Fallback search found no matching documents for query: {Query}", query);
            }
            
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback keyword search for query: {Query}", query);
            return new List<RelevantDocumentResult>();
        }
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;

        return text.Substring(0, maxLength - 3) + "...";
    }
}
