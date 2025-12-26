using DocN.Core.AI.Interfaces;
using DocN.Core.AI.Models;
using Microsoft.Extensions.Logging;

namespace DocN.Core.AI.Providers;

/// <summary>
/// Classe base astratta per i provider AI
/// </summary>
public abstract class BaseAIProvider : IDocumentAIProvider
{
    protected readonly ILogger _logger;

    protected BaseAIProvider(ILogger logger)
    {
        _logger = logger;
    }

    public abstract AIProviderType ProviderType { get; }
    public abstract string ProviderName { get; }

    public abstract Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    public abstract Task<List<CategorySuggestion>> SuggestCategoriesAsync(
        string documentText, 
        List<string> availableCategories, 
        CancellationToken cancellationToken = default);

    public virtual async Task<DocumentAnalysisResult> AnalyzeDocumentAsync(
        string documentText, 
        List<string> availableCategories, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing document with {Provider}", ProviderName);
        
        var result = new DocumentAnalysisResult
        {
            ExtractedText = documentText
        };

        // Genera embedding
        try
        {
            var embedding = await GenerateEmbeddingAsync(documentText, cancellationToken);
            result.Embedding = new DocumentEmbedding
            {
                Vector = embedding,
                Model = ProviderName,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
        }

        // Suggerisci categorie
        try
        {
            result.CategorySuggestions = await SuggestCategoriesAsync(documentText, availableCategories, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting categories");
        }

        return result;
    }

    protected string BuildCategorySuggestionPrompt(string documentText, List<string> availableCategories)
    {
        var categoriesList = string.Join(", ", availableCategories);
        return $@"Analizza il seguente documento e suggerisci le categorie più appropriate tra quelle disponibili.

Categorie disponibili: {categoriesList}

Documento:
{documentText}

Fornisci un JSON con un array 'suggestions' contenente oggetti con:
- categoryName: nome della categoria
- confidence: valore da 0 a 1
- reasoning: breve motivazione

Rispondi SOLO con JSON valido, senza altri commenti.";
    }
}
