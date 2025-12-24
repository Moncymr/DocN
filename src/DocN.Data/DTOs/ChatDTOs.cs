namespace DocN.Data.DTOs;

/// <summary>
/// Request for RAG chat
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public int? ConversationId { get; set; }
    public List<int>? SpecificDocumentIds { get; set; }
    public bool StreamResponse { get; set; } = false;
}

/// <summary>
/// Response from RAG system
/// </summary>
public class ChatResponse
{
    public int ConversationId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public List<DocumentReference> ReferencedDocuments { get; set; } = new();
    public int TokensUsed { get; set; }
    public long ResponseTimeMs { get; set; }
    public float Confidence { get; set; }
}

/// <summary>
/// Document reference in chat response
/// </summary>
public class DocumentReference
{
    public int DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public float RelevanceScore { get; set; }
}

/// <summary>
/// Request to create a new conversation
/// </summary>
public class ConversationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? Title { get; set; }
}

/// <summary>
/// Conversation summary
/// </summary>
public class ConversationSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
    public bool IsActive { get; set; }
}
