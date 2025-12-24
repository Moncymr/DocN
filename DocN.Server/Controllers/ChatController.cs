using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocN.Data;
using DocN.Data.Models;
using DocN.Data.Services.Agents;

namespace DocN.Server.Controllers;

/// <summary>
/// API endpoints for RAG chat functionality
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IAgentOrchestrator _orchestrator;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IAgentOrchestrator orchestrator,
        ApplicationDbContext context,
        ILogger<ChatController> logger)
    {
        _orchestrator = orchestrator;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Process a chat query using multi-agent RAG
    /// </summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ChatResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ChatResponse>> Query([FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message cannot be empty" });
            }

            _logger.LogInformation("Processing chat query: {Query}", request.Message);

            // Get or create conversation
            Conversation? conversation = null;
            if (request.ConversationId.HasValue)
            {
                conversation = await _context.Conversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value);
            }
            else if (!string.IsNullOrEmpty(request.UserId))
            {
                // Create new conversation
                conversation = new Conversation
                {
                    UserId = request.UserId,
                    Title = TruncateText(request.Message, 100),
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            // Process query using agent orchestrator
            var result = await _orchestrator.ProcessQueryAsync(
                request.Message,
                request.UserId,
                conversation?.Id);

            // Save messages to conversation
            if (conversation != null)
            {
                var userMessage = new Message
                {
                    ConversationId = conversation.Id,
                    Role = "user",
                    Content = request.Message,
                    Timestamp = DateTime.UtcNow
                };

                var docIds = result.RetrievedDocuments.Select(d => d.Id).ToList();
                var assistantMessage = new Message
                {
                    ConversationId = conversation.Id,
                    Role = "assistant",
                    Content = result.Answer,
                    ReferencedDocumentIds = docIds,
                    Timestamp = DateTime.UtcNow,
                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        retrieval_time_ms = result.RetrievalTime.TotalMilliseconds,
                        synthesis_time_ms = result.SynthesisTime.TotalMilliseconds,
                        total_time_ms = result.TotalTime.TotalMilliseconds,
                        retrieval_strategy = result.RetrievalStrategy,
                        documents_retrieved = result.RetrievedDocuments.Count,
                        chunks_retrieved = result.RetrievedChunks.Count
                    })
                };

                _context.Messages.Add(userMessage);
                _context.Messages.Add(assistantMessage);

                conversation.LastMessageAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new ChatResponse
            {
                ConversationId = conversation?.Id,
                Answer = result.Answer,
                ReferencedDocuments = result.RetrievedDocuments.Select(d => new DocumentReference
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    Category = d.ActualCategory ?? d.SuggestedCategory
                }).ToList(),
                Metadata = new ChatMetadata
                {
                    RetrievalTimeMs = result.RetrievalTime.TotalMilliseconds,
                    SynthesisTimeMs = result.SynthesisTime.TotalMilliseconds,
                    TotalTimeMs = result.TotalTime.TotalMilliseconds,
                    RetrievalStrategy = result.RetrievalStrategy,
                    DocumentsRetrieved = result.RetrievedDocuments.Count,
                    ChunksRetrieved = result.RetrievedChunks.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat query");
            return StatusCode(500, new { error = "An error occurred while processing your request" });
        }
    }

    /// <summary>
    /// Get all conversations for a user
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(List<ConversationSummary>), 200)]
    public async Task<ActionResult<List<ConversationSummary>>> GetConversations([FromQuery] string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new { error = "User ID is required" });
            }

            var conversations = await _context.Conversations
                .Where(c => c.UserId == userId && !c.IsArchived)
                .OrderByDescending(c => c.LastMessageAt)
                .Select(c => new ConversationSummary
                {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    LastMessageAt = c.LastMessageAt,
                    MessageCount = c.Messages.Count,
                    IsStarred = c.IsStarred
                })
                .ToListAsync();

            return Ok(conversations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversations");
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Get messages for a specific conversation
    /// </summary>
    [HttpGet("conversations/{id}/messages")]
    [ProducesResponseType(typeof(List<Message>), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<List<Message>>> GetMessages(int id)
    {
        try
        {
            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (conversation == null)
            {
                return NotFound(new { error = "Conversation not found" });
            }

            var messages = conversation.Messages
                .OrderBy(m => m.Timestamp)
                .ToList();

            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages");
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    /// <summary>
    /// Delete a conversation
    /// </summary>
    [HttpDelete("conversations/{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        try
        {
            var conversation = await _context.Conversations.FindAsync(id);
            if (conversation == null)
            {
                return NotFound(new { error = "Conversation not found" });
            }

            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conversation");
            return StatusCode(500, new { error = "An error occurred" });
        }
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength) + "...";
    }
}

/// <summary>
/// Request model for chat operations
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// The user's message/query
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID of the user sending the message
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// ID of the conversation (null for new conversation)
    /// </summary>
    public int? ConversationId { get; set; }
}

/// <summary>
/// Response model for chat operations
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// ID of the conversation
    /// </summary>
    public int? ConversationId { get; set; }

    /// <summary>
    /// The AI-generated answer
    /// </summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>
    /// Documents referenced in the answer
    /// </summary>
    public List<DocumentReference> ReferencedDocuments { get; set; } = new();

    /// <summary>
    /// Metadata about the query processing
    /// </summary>
    public ChatMetadata? Metadata { get; set; }
}

/// <summary>
/// Reference to a document used in the answer
/// </summary>
public class DocumentReference
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Category { get; set; }
}

/// <summary>
/// Metadata about chat query processing
/// </summary>
public class ChatMetadata
{
    public double RetrievalTimeMs { get; set; }
    public double SynthesisTimeMs { get; set; }
    public double TotalTimeMs { get; set; }
    public string RetrievalStrategy { get; set; } = string.Empty;
    public int DocumentsRetrieved { get; set; }
    public int ChunksRetrieved { get; set; }
}

/// <summary>
/// Summary of a conversation
/// </summary>
public class ConversationSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
    public bool IsStarred { get; set; }
}
