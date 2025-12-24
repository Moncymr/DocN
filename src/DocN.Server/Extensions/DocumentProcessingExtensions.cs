using DocN.Core.Interfaces;
using DocN.Server.Services.DocumentProcessing;

namespace DocN.Server.Extensions;

/// <summary>
/// Extension methods for registering document processing services
/// </summary>
public static class DocumentProcessingExtensions
{
    /// <summary>
    /// Add document processing services (extractors, chunking, orchestration)
    /// </summary>
    public static IServiceCollection AddDocumentProcessing(this IServiceCollection services)
    {
        // Register all document extractors
        services.AddScoped<IDocumentExtractor, PdfDocumentExtractor>();
        services.AddScoped<IDocumentExtractor, WordDocumentExtractor>();
        services.AddScoped<IDocumentExtractor, ExcelExtractor>();
        services.AddScoped<IDocumentExtractor, PowerPointExtractor>();

        // Register chunking service
        services.AddScoped<IChunkingService, ChunkingService>();

        // Register orchestrator
        services.AddScoped<DocumentProcessorOrchestrator>();

        return services;
    }
}
