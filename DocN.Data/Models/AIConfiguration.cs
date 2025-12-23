namespace DocN.Data.Models;

public class AIConfiguration
{
    public int Id { get; set; }
    public string ConfigurationName { get; set; } = string.Empty;
    
    // Azure OpenAI Settings
    public string? AzureOpenAIEndpoint { get; set; }
    public string? AzureOpenAIKey { get; set; }
    public string? EmbeddingDeploymentName { get; set; }
    public string? ChatDeploymentName { get; set; }
    
    // RAG Configuration
    public int MaxDocumentsToRetrieve { get; set; } = 5;
    public double SimilarityThreshold { get; set; } = 0.7;
    public int MaxTokensForContext { get; set; } = 4000;
    public string? SystemPrompt { get; set; }
    
    // Embedding Settings
    public int EmbeddingDimensions { get; set; } = 1536;
    public string? EmbeddingModel { get; set; } = "text-embedding-ada-002";
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
