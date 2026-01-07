using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DocN.Data.Models;
using DocN.Core.Interfaces;
using System.Text;
using System.Diagnostics;

#pragma warning disable SKEXP0110 // Agents are experimental

namespace DocN.Data.Services;

/// <summary>
/// Enhanced RAG service using Microsoft Agent Framework with AgentGroupChat
/// Provides optimized multi-agent collaboration for retrieval-augmented generation
/// </summary>
public class EnhancedAgentRAGService : ISemanticRAGService
{
    private readonly IKernelProvider _kernelProvider;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EnhancedAgentRAGService> _logger;
    private readonly IEmbeddingService _embeddingService;
    private readonly ICacheService _cacheService;
    
    // Agent definitions for specialized tasks
    private ChatCompletionAgent? _queryAnalyzerAgent;
    private ChatCompletionAgent? _retrievalAgent;
    private ChatCompletionAgent? _rerankingAgent;
    private ChatCompletionAgent? _synthesisAgent;
    
    // Performance tracking
    private readonly ActivitySource _activitySource = new("DocN.EnhancedAgentRAG");

    public EnhancedAgentRAGService(
        IKernelProvider kernelProvider,
        ApplicationDbContext context,
        ILogger<EnhancedAgentRAGService> logger,
        IEmbeddingService embeddingService,
        ICacheService cacheService)
    {
        _kernelProvider = kernelProvider ?? throw new ArgumentNullException(nameof(kernelProvider));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    /// <summary>
    /// Initialize specialized agents for the RAG pipeline
    /// </summary>
    private async Task InitializeAgentsAsync()
    {
        if (_queryAnalyzerAgent != null) return; // Already initialized
        
        var kernel = await _kernelProvider.GetKernelAsync();
        
        // Query Analyzer Agent - Understands and enhances user queries
        _queryAnalyzerAgent = new ChatCompletionAgent
        {
            Name = "QueryAnalyzer",
            Instructions = @"Sei un esperto analista di query. Il tuo compito è:
1. Comprendere l'intento della domanda dell'utente
2. Identificare concetti chiave, entità e relazioni
3. Espandere la query con sinonimi e termini correlati
4. Suggerire filtri di ricerca appropriati (date, categorie, tipi di documento)
5. Determinare se la query richiede informazioni specifiche o generali

Fornisci un'analisi strutturata in italiano che aiuti gli altri agenti a recuperare le informazioni più pertinenti.
Non rispondere alla domanda direttamente - il tuo ruolo è solo analizzare la query.",
            Kernel = kernel
        };

        // Retrieval Agent - Finds relevant documents
        _retrievalAgent = new ChatCompletionAgent
        {
            Name = "RetrievalAgent",
            Instructions = @"Sei un agente specializzato nel recupero documenti. Il tuo compito è:
1. Analizzare l'analisi della query fornita dal QueryAnalyzer
2. Identificare le migliori strategie di ricerca (semantica, keyword, ibrida)
3. Determinare i parametri ottimali (numero di documenti, soglia di similarità)
4. Estrarre le sezioni più rilevanti dai documenti trovati
5. Valutare la qualità e pertinenza dei risultati

Fornisci un report strutturato dei documenti trovati con punteggi di rilevanza.
Non generare risposte - il tuo ruolo è solo recuperare informazioni.",
            Kernel = kernel
        };

        // Reranking Agent - Optimizes document ordering
        _rerankingAgent = new ChatCompletionAgent
        {
            Name = "RerankingAgent",
            Instructions = @"Sei un esperto di riordino e valutazione della rilevanza. Il tuo compito è:
1. Analizzare i documenti recuperati dal RetrievalAgent
2. Valutare la rilevanza di ogni documento rispetto alla query originale
3. Considerare la qualità, freschezza e completezza delle informazioni
4. Riordinare i documenti per massimizzare la pertinenza
5. Identificare eventuali gap informativi o documenti ridondanti

Fornisci una lista riordinata con punteggi di rilevanza giustificati.
Non generare risposte finali - il tuo ruolo è solo ottimizzare l'ordine dei risultati.",
            Kernel = kernel
        };

        // Synthesis Agent - Generates final answers
        _synthesisAgent = new ChatCompletionAgent
        {
            Name = "SynthesisAgent",
            Instructions = @"Sei un agente esperto di sintesi e generazione di risposte. Il tuo compito è:
1. Analizzare i documenti riordinati dal RerankingAgent
2. Estrarre le informazioni più rilevanti per rispondere alla query
3. Generare una risposta chiara, accurata e ben strutturata in italiano
4. Includere citazioni specifiche ai documenti fonte (con ID e titolo)
5. Indicare il livello di confidenza della risposta
6. Identificare eventuali limitazioni o informazioni mancanti

La risposta deve essere:
- Accurata e basata solo sui documenti forniti
- Chiara e facile da comprendere
- Completa ma concisa
- Con citazioni appropriate
- In italiano

Se le informazioni non sono sufficienti, indica chiaramente cosa manca.",
            Kernel = kernel
        };

        _logger.LogInformation("Enhanced Agent RAG Service initialized with 4 specialized agents");
    }

    public async Task<SemanticRAGResponse> GenerateResponseAsync(
        string query,
        string userId,
        int? conversationId = null,
        List<int>? specificDocumentIds = null,
        int topK = 5)
    {
        using var activity = _activitySource.StartActivity("GenerateResponse");
        activity?.SetTag("query", query);
        activity?.SetTag("userId", userId);
        activity?.SetTag("topK", topK);
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Initialize agents if needed
            await InitializeAgentsAsync();

            _logger.LogInformation(
                "Starting Enhanced Agent RAG pipeline for query: {Query}, User: {UserId}",
                query, userId);

            // Create AgentGroupChat for multi-agent collaboration
            var agentChat = new AgentGroupChat(
                _queryAnalyzerAgent!,
                _retrievalAgent!,
                _rerankingAgent!,
                _synthesisAgent!)
            {
                ExecutionSettings = new AgentGroupChatSettings
                {
                    // Terminate when SynthesisAgent provides final answer
                    TerminationStrategy = new AgentTerminationStrategy
                    {
                        // Agents will process in sequence: Analyzer → Retrieval → Reranking → Synthesis
                        MaximumIterations = 10,
                        AutomaticReset = true
                    }
                }
            };

            // Step 1: Query Analysis Phase
            activity?.AddEvent(new ActivityEvent("QueryAnalysis.Start"));
            var queryAnalysisStopwatch = Stopwatch.StartNew();
            
            await agentChat.AddChatMessageAsync(new ChatMessageContent(
                AuthorRole.User,
                $"Analizza questa query dell'utente: {query}"
            ));

            // Get query analysis from QueryAnalyzer
            string? queryAnalysis = null;
            await foreach (var message in agentChat.InvokeAsync(_queryAnalyzerAgent!))
            {
                queryAnalysis = message.Content;
                _logger.LogDebug("Query Analysis: {Analysis}", queryAnalysis);
            }
            
            queryAnalysisStopwatch.Stop();
            activity?.SetTag("queryAnalysisTimeMs", queryAnalysisStopwatch.ElapsedMilliseconds);

            // Step 2: Document Retrieval Phase
            activity?.AddEvent(new ActivityEvent("DocumentRetrieval.Start"));
            var retrievalStopwatch = Stopwatch.StartNew();
            
            // Perform actual vector search
            var relevantDocs = await SearchDocumentsAsync(query, userId, topK * 2, 0.5, specificDocumentIds);
            
            if (!relevantDocs.Any())
            {
                _logger.LogInformation("No documents found with semantic search, trying keyword fallback");
                relevantDocs = await FallbackKeywordSearch(query, userId, topK);
            }

            retrievalStopwatch.Stop();
            activity?.SetTag("retrievalTimeMs", retrievalStopwatch.ElapsedMilliseconds);
            activity?.SetTag("documentsRetrieved", relevantDocs.Count);

            if (!relevantDocs.Any())
            {
                return CreateNoDocumentsResponse(stopwatch);
            }

            // Step 3: Reranking Phase
            activity?.AddEvent(new ActivityEvent("Reranking.Start"));
            var rerankingStopwatch = Stopwatch.StartNew();
            
            var documentContext = BuildDocumentContext(relevantDocs);
            await agentChat.AddChatMessageAsync(new ChatMessageContent(
                AuthorRole.User,
                $"Query analysis: {queryAnalysis}\n\nDocumenti trovati:\n{documentContext}\n\nRiordina questi documenti per rilevanza rispetto alla query originale."
            ));

            // Get reranking from RerankingAgent
            await foreach (var message in agentChat.InvokeAsync(_rerankingAgent!))
            {
                _logger.LogDebug("Reranking Result: {Result}", message.Content);
            }
            
            rerankingStopwatch.Stop();
            activity?.SetTag("rerankingTimeMs", rerankingStopwatch.ElapsedMilliseconds);

            // Step 4: Synthesis Phase
            activity?.AddEvent(new ActivityEvent("Synthesis.Start"));
            var synthesisStopwatch = Stopwatch.StartNew();
            
            // Load conversation history for context
            var conversationHistory = await LoadConversationHistoryAsync(conversationId);
            var historyContext = BuildConversationHistoryContext(conversationHistory);

            await agentChat.AddChatMessageAsync(new ChatMessageContent(
                AuthorRole.User,
                $"Query originale: {query}\n\n{historyContext}\n\nGenera una risposta completa basata sui documenti riordinati."
            ));

            // Get final answer from SynthesisAgent
            string? answer = null;
            await foreach (var message in agentChat.InvokeAsync(_synthesisAgent!))
            {
                answer = message.Content;
                _logger.LogDebug("Synthesis Result: {Result}", answer);
            }
            
            synthesisStopwatch.Stop();
            activity?.SetTag("synthesisTimeMs", synthesisStopwatch.ElapsedMilliseconds);

            // Save conversation
            var savedConversationId = await SaveConversationAsync(
                conversationId,
                userId,
                query,
                answer ?? "No answer generated",
                relevantDocs.Select(d => d.DocumentId).ToList());

            stopwatch.Stop();

            var response = new SemanticRAGResponse
            {
                Answer = answer ?? "Unable to generate response",
                SourceDocuments = relevantDocs.Take(topK).ToList(),
                ConversationId = savedConversationId,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                FromCache = false,
                Metadata = new Dictionary<string, object>
                {
                    ["documentsRetrieved"] = relevantDocs.Count,
                    ["documentsUsed"] = topK,
                    ["topSimilarityScore"] = relevantDocs.FirstOrDefault()?.SimilarityScore ?? 0,
                    ["hasConversationHistory"] = conversationHistory.Any(),
                    ["pipelinePhases"] = new Dictionary<string, long>
                    {
                        ["queryAnalysis"] = queryAnalysisStopwatch.ElapsedMilliseconds,
                        ["retrieval"] = retrievalStopwatch.ElapsedMilliseconds,
                        ["reranking"] = rerankingStopwatch.ElapsedMilliseconds,
                        ["synthesis"] = synthesisStopwatch.ElapsedMilliseconds
                    },
                    ["agentFramework"] = "Microsoft.SemanticKernel.Agents"
                }
            };

            _logger.LogInformation(
                "Enhanced Agent RAG pipeline completed in {TotalMs}ms (Analysis: {AnalysisMs}ms, Retrieval: {RetrievalMs}ms, Reranking: {RerankingMs}ms, Synthesis: {SynthesisMs}ms)",
                stopwatch.ElapsedMilliseconds,
                queryAnalysisStopwatch.ElapsedMilliseconds,
                retrievalStopwatch.ElapsedMilliseconds,
                rerankingStopwatch.ElapsedMilliseconds,
                synthesisStopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Enhanced Agent RAG pipeline for query: {Query}", query);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            return new SemanticRAGResponse
            {
                Answer = $"Si è verificato un errore durante l'elaborazione della richiesta: {ex.Message}",
                ResponseTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async IAsyncEnumerable<string> GenerateStreamingResponseAsync(
        string query,
        string userId,
        int? conversationId = null,
        List<int>? specificDocumentIds = null,
        int topK = 5)
    {
        using var activity = _activitySource.StartActivity("GenerateStreamingResponse");
        
        try
        {
            await InitializeAgentsAsync();

            // For streaming, we'll use a simplified approach with direct synthesis
            var relevantDocs = await SearchDocumentsAsync(query, userId, topK, 0.5, specificDocumentIds);
            
            if (!relevantDocs.Any())
            {
                yield return "❌ Non ho trovato documenti rilevanti per rispondere alla tua domanda.\n";
                yield break;
            }

            var kernel = await _kernelProvider.GetKernelAsync();
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            
            var documentContext = BuildDocumentContext(relevantDocs);
            var conversationHistory = await LoadConversationHistoryAsync(conversationId);
            var historyContext = BuildConversationHistoryContext(conversationHistory);

            var systemPrompt = CreateSystemPromptForStreaming();
            var userPrompt = $"Query: {query}\n\n{historyContext}\n\nDocumenti:\n{documentContext}\n\nGenera una risposta completa con citazioni.";

            var chatHistory = new ChatHistory(systemPrompt);
            chatHistory.AddUserMessage(userPrompt);

            // Stream the response
            await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(chatHistory))
            {
                if (!string.IsNullOrEmpty(chunk.Content))
                {
                    yield return chunk.Content;
                }
            }

            // Save conversation after streaming completes
            var fullAnswer = new StringBuilder();
            await foreach (var chunk in GenerateStreamingResponseAsync(query, userId, conversationId, specificDocumentIds, topK))
            {
                fullAnswer.Append(chunk);
            }
            
            await SaveConversationAsync(
                conversationId,
                userId,
                query,
                fullAnswer.ToString(),
                relevantDocs.Select(d => d.DocumentId).ToList());
        }
        finally
        {
            activity?.Dispose();
        }
    }

    #region Helper Methods

    private async Task<List<RelevantDocumentInfo>> SearchDocumentsAsync(
        string query,
        string userId,
        int topK,
        double minSimilarity,
        List<int>? specificDocumentIds = null)
    {
        // Generate embedding for the query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

        // Build base query with proper vector search
        var documentsQuery = _context.Documents
            .Where(d => d.OwnerId == userId)
            .AsQueryable();

        // Filter by specific documents if provided
        if (specificDocumentIds?.Any() == true)
        {
            documentsQuery = documentsQuery.Where(d => specificDocumentIds.Contains(d.Id));
        }

        // Perform vector similarity search
        var results = await documentsQuery
            .Select(d => new
            {
                Document = d,
                // Calculate cosine similarity using stored embeddings
                Similarity = d.Embeddings != null && d.Embeddings.Length > 0
                    ? CalculateCosineSimilarity(queryEmbedding, d.Embeddings)
                    : 0.0
            })
            .Where(r => r.Similarity >= minSimilarity)
            .OrderByDescending(r => r.Similarity)
            .Take(topK)
            .ToListAsync();

        return results.Select(r => new RelevantDocumentInfo
        {
            DocumentId = r.Document.Id,
            Title = r.Document.Title,
            Content = r.Document.Content ?? string.Empty,
            Category = r.Document.Category,
            Tags = r.Document.Tags,
            SimilarityScore = r.Similarity,
            RelevantChunk = r.Document.Content?.Substring(0, Math.Min(500, r.Document.Content.Length ?? 0))
        }).ToList();
    }

    private async Task<List<RelevantDocumentInfo>> FallbackKeywordSearch(
        string query,
        string userId,
        int topK)
    {
        // Simple keyword-based fallback
        var keywords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var results = await _context.Documents
            .Where(d => d.OwnerId == userId)
            .Where(d => keywords.Any(k => (d.Content ?? "").ToLower().Contains(k) || d.Title.ToLower().Contains(k)))
            .OrderByDescending(d => keywords.Count(k => (d.Content ?? "").ToLower().Contains(k) || d.Title.ToLower().Contains(k)))
            .Take(topK)
            .ToListAsync();

        return results.Select(d => new RelevantDocumentInfo
        {
            DocumentId = d.Id,
            Title = d.Title,
            Content = d.Content ?? string.Empty,
            Category = d.Category,
            Tags = d.Tags,
            SimilarityScore = 0.6,
            RelevantChunk = d.Content?.Substring(0, Math.Min(500, d.Content.Length ?? 0))
        }).ToList();
    }

    private double CalculateCosineSimilarity(float[] vectorA, float[] vectorB)
    {
        if (vectorA.Length != vectorB.Length) return 0.0;

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (int i = 0; i < vectorA.Length; i++)
        {
            dotProduct += vectorA[i] * vectorB[i];
            magnitudeA += vectorA[i] * vectorA[i];
            magnitudeB += vectorB[i] * vectorB[i];
        }

        magnitudeA = Math.Sqrt(magnitudeA);
        magnitudeB = Math.Sqrt(magnitudeB);

        return magnitudeA > 0 && magnitudeB > 0 ? dotProduct / (magnitudeA * magnitudeB) : 0.0;
    }

    private string BuildDocumentContext(List<RelevantDocumentInfo> documents)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < documents.Count; i++)
        {
            var doc = documents[i];
            sb.AppendLine($"[Documento {i + 1}] ID: {doc.DocumentId}, Titolo: {doc.Title}, Categoria: {doc.Category}");
            sb.AppendLine($"Similarità: {doc.SimilarityScore:F2}");
            sb.AppendLine($"Contenuto: {doc.RelevantChunk}...");
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private async Task<List<Message>> LoadConversationHistoryAsync(int? conversationId)
    {
        if (!conversationId.HasValue) return new List<Message>();

        return await _context.Messages
            .Where(m => m.ConversationId == conversationId.Value)
            .OrderByDescending(m => m.Timestamp)
            .Take(5) // Last 5 messages for context
            .OrderBy(m => m.Timestamp)
            .ToListAsync();
    }

    private string BuildConversationHistoryContext(List<Message> history)
    {
        if (!history.Any()) return string.Empty;

        var sb = new StringBuilder("Cronologia conversazione:\n");
        foreach (var msg in history)
        {
            sb.AppendLine($"- {msg.Role}: {msg.Content}");
        }
        return sb.ToString();
    }

    private async Task<int> SaveConversationAsync(
        int? conversationId,
        string userId,
        string query,
        string answer,
        List<int> documentIds)
    {
        Conversation conversation;

        if (conversationId.HasValue)
        {
            conversation = await _context.Conversations.FindAsync(conversationId.Value)
                ?? throw new InvalidOperationException($"Conversation {conversationId} not found");
        }
        else
        {
            conversation = new Conversation
            {
                UserId = userId,
                Title = query.Substring(0, Math.Min(100, query.Length)),
                CreatedAt = DateTime.UtcNow
            };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
        }

        // Add user message
        _context.Messages.Add(new Message
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = query,
            Timestamp = DateTime.UtcNow
        });

        // Add assistant message
        _context.Messages.Add(new Message
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = answer,
            Timestamp = DateTime.UtcNow,
            ReferencedDocumentIds = documentIds
        });

        await _context.SaveChangesAsync();
        return conversation.Id;
    }

    private SemanticRAGResponse CreateNoDocumentsResponse(Stopwatch stopwatch)
    {
        return new SemanticRAGResponse
        {
            Answer = @"❌ Non ho trovato documenti rilevanti per rispondere alla tua domanda.

📄 Per ottenere risposte accurate:
1. Assicurati di aver caricato i documenti necessari
2. Verifica che i documenti siano stati elaborati correttamente
3. Riprova la domanda dopo aver caricato i documenti pertinenti

💡 Puoi verificare i tuoi documenti nella sezione 'Documents'.",
            ResponseTimeMs = stopwatch.ElapsedMilliseconds,
            SourceDocuments = new List<RelevantDocumentInfo>(),
            Metadata = new Dictionary<string, object>
            {
                ["documentsRetrieved"] = 0,
                ["reason"] = "no_documents_found"
            }
        };
    }

    private string CreateSystemPromptForStreaming()
    {
        return @"Sei un assistente AI esperto che genera risposte basate su documenti forniti.

ISTRUZIONI:
1. Analizza attentamente i documenti forniti
2. Genera una risposta accurata e completa in italiano
3. Includi citazioni ai documenti usando il formato: [Doc ID: X - Titolo]
4. Sii conciso ma completo
5. Se le informazioni non sono sufficienti, indicalo chiaramente

Genera SOLO risposte basate sui documenti forniti - non aggiungere conoscenze esterne.";
    }

    #endregion
}

/// <summary>
/// Agent termination strategy for sequential processing
/// </summary>
internal class AgentTerminationStrategy : TerminationStrategy
{
    public int MaximumIterations { get; set; } = 10;
    public bool AutomaticReset { get; set; } = true;

    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken = default)
    {
        // Terminate after synthesis agent completes
        return Task.FromResult(
            agent.Name == "SynthesisAgent" && 
            history.Any(m => m.AuthorName == "SynthesisAgent"));
    }
}

/// <summary>
/// Agent group chat settings
/// </summary>
internal class AgentGroupChatSettings
{
    public TerminationStrategy? TerminationStrategy { get; set; }
}

/// <summary>
/// Information about a relevant document
/// </summary>
public class RelevantDocumentInfo
{
    public int DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public double SimilarityScore { get; set; }
    public string? RelevantChunk { get; set; }
}
