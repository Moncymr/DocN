namespace DocN.Data.Models;

/// <summary>
/// Represents AI provider types supported by the system
/// </summary>
public enum AIProviderType
{
    Gemini = 1,
    OpenAI = 2,
    AzureOpenAI = 3
}

/// <summary>
/// Represents different AI service types that can use different providers
/// </summary>
public enum AIServiceType
{
    Chat = 1,
    Embeddings = 2,
    TagExtraction = 3,
    RAG = 4
}

public class AIConfiguration
{
    public int Id { get; set; }
    public string ConfigurationName { get; set; } = string.Empty;
    
    // Provider Information
    public AIProviderType ProviderType { get; set; } = AIProviderType.Gemini;
    public string? ProviderEndpoint { get; set; }
    public string? ProviderApiKey { get; set; }
    
    // Model Configuration
    public string? ChatModelName { get; set; }
    public string? EmbeddingModelName { get; set; }
    
    // Azure OpenAI Specific (backward compatibility)
    public string? AzureOpenAIEndpoint { get; set; }
    public string? AzureOpenAIKey { get; set; }
    public string? EmbeddingDeploymentName { get; set; }
    public string? ChatDeploymentName { get; set; }
    
    // Service-specific provider assignments
    public AIProviderType? ChatProvider { get; set; }
    public AIProviderType? EmbeddingsProvider { get; set; }
    public AIProviderType? TagExtractionProvider { get; set; }
    public AIProviderType? RAGProvider { get; set; }
    
    // Configuration for each provider type
    // Gemini Settings
    public string? GeminiApiKey { get; set; }
    public string? GeminiChatModel { get; set; } = "gemini-1.5-flash";
    public string? GeminiEmbeddingModel { get; set; } = "text-embedding-004";
    
    // OpenAI Settings
    public string? OpenAIApiKey { get; set; }
    public string? OpenAIChatModel { get; set; } = "gpt-4";
    public string? OpenAIEmbeddingModel { get; set; } = "text-embedding-ada-002";
    
    // Azure OpenAI Settings (additional fields)
    public string? AzureOpenAIChatModel { get; set; } = "gpt-4";
    public string? AzureOpenAIEmbeddingModel { get; set; } = "text-embedding-ada-002";
    
    // RAG Configuration
    public int MaxDocumentsToRetrieve { get; set; } = 5;
    public double SimilarityThreshold { get; set; } = 0.7;
    public int MaxTokensForContext { get; set; } = 4000;
    public string? SystemPrompt { get; set; }
    
    // Embedding Settings
    public int EmbeddingDimensions { get; set; } = 1536;
    public string? EmbeddingModel { get; set; } = "text-embedding-ada-002";
    
    // Chunking Configuration
    public bool EnableChunking { get; set; } = true;
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
    
    // Enable fallback to other providers
    public bool EnableFallback { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
