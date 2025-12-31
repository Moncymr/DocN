using Azure.AI.OpenAI;
using Azure;
using DocN.Data.Models;
using System.Text;
using OpenAI.Chat;
using System.ClientModel;

namespace DocN.Data.Services;

/// <summary>
/// Interfaccia per il servizio RAG (Retrieval-Augmented Generation)
/// Fornisce funzionalità per generare risposte basate su documenti rilevanti
/// </summary>
public interface IRAGService
{
    /// <summary>
    /// Genera una risposta AI basata sulla query e sui documenti rilevanti forniti
    /// </summary>
    /// <param name="query">Domanda o query dell'utente</param>
    /// <param name="relevantDocuments">Lista di documenti rilevanti da utilizzare come contesto</param>
    /// <returns>Risposta generata dall'AI basata sui documenti forniti</returns>
    Task<string> GenerateResponseAsync(string query, List<Document> relevantDocuments);
}

/// <summary>
/// Implementazione del servizio RAG utilizzando Azure OpenAI
/// Gestisce la generazione di risposte basate su documenti attraverso l'integrazione con Azure OpenAI
/// </summary>
public class RAGService : IRAGService
{
    private readonly ApplicationDbContext _context;
    private ChatClient? _client;

    /// <summary>
    /// Inizializza una nuova istanza di RAGService
    /// </summary>
    /// <param name="context">Contesto del database per accedere alle configurazioni AI</param>
    /// <exception cref="ArgumentNullException">Lanciato se context è null</exception>
    public RAGService(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        InitializeClient();
    }

    /// <summary>
    /// Inizializza il client Azure OpenAI utilizzando la configurazione attiva dal database
    /// Carica endpoint, chiave API e deployment name dalla configurazione AI attiva
    /// </summary>
    private void InitializeClient()
    {
        var config = _context.AIConfigurations.FirstOrDefault(c => c.IsActive);
        if (config != null && !string.IsNullOrEmpty(config.AzureOpenAIEndpoint) && !string.IsNullOrEmpty(config.AzureOpenAIKey))
        {
            var azureClient = new AzureOpenAIClient(new Uri(config.AzureOpenAIEndpoint), new AzureKeyCredential(config.AzureOpenAIKey));
            _client = azureClient.GetChatClient(config.ChatDeploymentName ?? "gpt-4");
        }
    }

    /// <inheritdoc/>
    public async Task<string> GenerateResponseAsync(string query, List<Document> relevantDocuments)
    {
        if (_client == null)
            return "AI service not configured.";

        try
        {
            var config = _context.AIConfigurations.FirstOrDefault(c => c.IsActive);
            var systemPrompt = config?.SystemPrompt ?? "You are a helpful assistant that answers questions based on provided documents.";

            // Build context from relevant documents
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Use the following documents to answer the question:");
            contextBuilder.AppendLine();

            foreach (var doc in relevantDocuments)
            {
                contextBuilder.AppendLine($"Document: {doc.FileName}");
                contextBuilder.AppendLine($"Category: {doc.ActualCategory ?? doc.SuggestedCategory}");
                contextBuilder.AppendLine($"Content: {TruncateText(doc.ExtractedText, 1000)}");
                contextBuilder.AppendLine();
            }

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(contextBuilder.ToString()),
                new UserChatMessage(query)
            };

            var response = await _client.CompleteChatAsync(messages);
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            return $"Error generating response: {ex.Message}";
        }
    }

    /// <summary>
    /// Tronca il testo alla lunghezza massima specificata
    /// Aggiunge "..." alla fine se il testo è stato troncato
    /// </summary>
    /// <param name="text">Testo da troncare</param>
    /// <param name="maxLength">Lunghezza massima desiderata</param>
    /// <returns>Testo troncato con "..." se necessario, altrimenti il testo originale</returns>
    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }
}
