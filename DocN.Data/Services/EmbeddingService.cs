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

public class EmbeddingService : IEmbeddingService
{
    private readonly ApplicationDbContext _context;
    private EmbeddingClient? _client;
    private bool _initialized = false;

    public EmbeddingService(ApplicationDbContext context)
    {
        _context = context;
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

    public async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        EnsureInitialized();
        
        if (_client == null)
            return null;

        try
        {
            var response = await _client.GenerateEmbeddingAsync(text);
            return response.Value.ToFloats().ToArray();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Document>> SearchSimilarDocumentsAsync(float[] queryEmbedding, int topK = 5)
    {
        // This is a simplified version - in production you'd use vector database or SQL Server vector search
        var documents = await Task.Run(() => _context.Documents.ToList());
        
        var scoredDocuments = documents
            .Where(d => d.EmbeddingVector != null && d.EmbeddingVector.Length > 0)
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
