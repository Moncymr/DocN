using Azure.AI.OpenAI;
using Azure;
using DocN.Data.Models;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace DocN.Data.Services;

public interface IEmbeddingService
{
    Task<float[]?> GenerateEmbeddingAsync(string text);
    Task<List<Document>> SearchSimilarDocumentsAsync(float[] queryEmbedding, int topK = 5);
}

/// <summary>
/// Servizio per generazione di embeddings vettoriali da testo utilizzando modelli AI
/// Supporta caching per ottimizzare performance e ridurre chiamate API
/// </summary>
/// <remarks>
/// Scopo: Convertire testo in rappresentazioni vettoriali numeriche (embeddings) per ricerca semantica
/// Gli embeddings permettono di calcolare similarità semantica tra testi tramite operazioni vettoriali
/// </remarks>
public class EmbeddingService : IEmbeddingService
{
    private readonly ApplicationDbContext _context;
    private readonly ICacheService? _cacheService;
    private EmbeddingClient? _client;
    private bool _initialized = false;

    public EmbeddingService(ApplicationDbContext context, ICacheService? cacheService = null)
    {
        _context = context;
        _cacheService = cacheService;
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;
        
        try
        {
            var config = _context.AIConfigurations.FirstOrDefault(c => c.IsActive);
            if (config != null && !string.IsNullOrEmpty(config.AzureOpenAIEndpoint) && !string.IsNullOrEmpty(config.AzureOpenAIKey))
            {
                var azureClient = new AzureOpenAIClient(new Uri(config.AzureOpenAIEndpoint), new AzureKeyCredential(config.AzureOpenAIKey));
                _client = azureClient.GetEmbeddingClient(config.EmbeddingDeploymentName ?? "text-embedding-ada-002");
            }
        }
        catch
        {
            // Initialization can fail if database doesn't exist yet or AIConfigurations table is empty
            // This is OK - the service will work without AI features
        }
        finally
        {
            _initialized = true;
        }
    }

    /// <summary>
    /// Genera un embedding vettoriale per il testo fornito
    /// </summary>
    /// <param name="text">Testo da convertire in embedding</param>
    /// <returns>Array di float rappresentante l'embedding (768 o 1536 dimensioni) o null se fallisce</returns>
    /// <remarks>
    /// Scopo: Convertire testo in vettore numerico per calcoli di similarità semantica
    /// 
    /// Processo:
    /// 1. Verifica inizializzazione client AI (Azure OpenAI)
    /// 2. Controlla cache per evitare rigenerazioni costose
    /// 3. Chiama API AI per generare embedding se non in cache
    /// 4. Salva in cache per riutilizzo futuro
    /// 
    /// Output atteso:
    /// - Array float[] di 768 dimensioni (text-embedding-004 Gemini) 
    ///   o 1536 dimensioni (text-embedding-ada-002 OpenAI)
    /// - null se: client non inizializzato, errore API, testo vuoto
    /// 
    /// Note:
    /// - Usa caching aggressivo: stesso testo = stesso embedding (deterministico)
    /// - Cache riduce costi API e latenza (embedding generation è costoso)
    /// </remarks>
    public async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        EnsureInitialized();
        
        if (_client == null)
            return null;

        // Check cache first if available
        if (_cacheService != null)
        {
            var cachedEmbedding = await _cacheService.GetCachedEmbeddingAsync(text);
            if (cachedEmbedding != null)
                return cachedEmbedding;
        }

        try
        {
            var response = await _client.GenerateEmbeddingAsync(text);
            var embedding = response.Value.ToFloats().ToArray();
            
            // Cache the result if caching is available
            if (_cacheService != null && embedding != null)
            {
                await _cacheService.SetCachedEmbeddingAsync(text, embedding);
            }
            
            return embedding;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Cerca documenti simili al vettore embedding fornito utilizzando similarità coseno
    /// </summary>
    /// <param name="queryEmbedding">Vettore embedding della query di ricerca</param>
    /// <param name="topK">Numero massimo di documenti da restituire (default: 5)</param>
    /// <returns>Lista di documenti ordinati per similarità decrescente (più simile primo)</returns>
    /// <remarks>
    /// Scopo: Implementare ricerca semantica calcolando similarità tra embeddings
    /// 
    /// ⚠️ ATTENZIONE: Implementazione semplificata per dimostrazione
    /// 
    /// Limitazioni attuali:
    /// - Carica TUTTI i documenti in memoria (non scalabile per grandi dataset)
    /// - Calcolo similarità in-memory (lento con molti documenti)
    /// 
    /// Per produzione, usare:
    /// 1. SQL Server 2025 con tipo VECTOR nativo e VECTOR_DISTANCE
    /// 2. Azure Cognitive Search con vector search
    /// 3. Vector database dedicati (Pinecone, Weaviate, Qdrant)
    /// 
    /// Output atteso:
    /// - Lista documenti con score similarità più alto
    /// - Ordinamento decrescente per rilevanza
    /// - Massimo topK risultati
    /// </remarks>
    public async Task<List<Document>> SearchSimilarDocumentsAsync(float[] queryEmbedding, int topK = 5)
    {
        // WARNING: This is a simplified version for demonstration purposes only
        // In production, you should use:
        // 1. SQL Server 2025 native vector search with VECTOR data type
        // 2. Azure Cognitive Search with vector search
        // 3. A dedicated vector database like Pinecone, Weaviate, or Qdrant
        // Loading all documents into memory is NOT scalable for large datasets
        // Query the actual mapped fields: EmbeddingVector768 or EmbeddingVector1536
        var documents = await Task.Run(() => _context.Documents
            .Where(d => (d.EmbeddingVector768 != null && d.EmbeddingVector768.Length > 0) ||
                        (d.EmbeddingVector1536 != null && d.EmbeddingVector1536.Length > 0))
            .ToList());
        
        var scoredDocuments = documents
            .Where(d => d.EmbeddingVector != null) // Use the property getter
            .Select(d => new
            {
                Document = d,
                Score = CosineSimilarity(queryEmbedding, d.EmbeddingVector!)
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Document)
            .ToList();

        return scoredDocuments;
    }

    /// <summary>
    /// Calcola la similarità coseno tra due vettori
    /// </summary>
    /// <param name="a">Primo vettore</param>
    /// <param name="b">Secondo vettore</param>
    /// <returns>Score di similarità tra 0 e 1 (1 = identici, 0 = ortogonali, <0 = opposti)</returns>
    /// <remarks>
    /// Scopo: Misurare quanto due vettori sono semanticamente simili
    /// 
    /// Formula: cosine_similarity = (a · b) / (||a|| * ||b||)
    /// - a · b = prodotto scalare (dot product)
    /// - ||a|| = magnitudine (norma euclidea) del vettore a
    /// - ||b|| = magnitudine del vettore b
    /// 
    /// Interpretazione risultato:
    /// - 1.0 = vettori identici (stessa direzione, massima similarità)
    /// - 0.7-0.9 = molto simili (tipicamente rilevanti per RAG)
    /// - 0.5-0.7 = moderatamente simili
    /// - 0.0 = ortogonali (nessuna relazione)
    /// - <0.0 = opposti (raramente accade con embeddings normalizzati)
    /// 
    /// Output atteso:
    /// - Double tra -1 e 1 (tipicamente 0-1 per embeddings normalizzati)
    /// - 0 se i vettori hanno dimensioni diverse (non confrontabili)
    /// </remarks>
    private double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;

        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
