using DocN.Data;
using DocN.Data.Models;

namespace DocN.Server.Services;

public class DatabaseSeeder
{
    private readonly DocArcContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(DocArcContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Check if we already have data
            if (_context.Documents.Any())
            {
                _logger.LogInformation("Database already has documents, skipping seeding");
                return;
            }

            // Add sample documents - some with vectors, some without
            var documents = new List<Document>
            {
                new Document
                {
                    FileName = "Project_Requirements.pdf",
                    FilePath = "/documents/Project_Requirements.pdf",
                    ContentType = "application/pdf",
                    FileSize = 1024000,
                    ExtractedText = "This document contains the project requirements for DocN system...",
                    SuggestedCategory = "Requirements",
                    ActualCategory = "Requirements",
                    EmbeddingVector = null, // Simulated vector would go here
                    UploadedAt = DateTime.UtcNow.AddDays(-5),
                    Visibility = DocumentVisibility.Private
                },
                new Document
                {
                    FileName = "Technical_Specification.docx",
                    FilePath = "/documents/Technical_Specification.docx",
                    ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    FileSize = 512000,
                    ExtractedText = "Technical specifications for the DocN architecture...",
                    SuggestedCategory = "Technical",
                    ActualCategory = "Technical",
                    EmbeddingVector = null, // NO VECTOR - this should still be shown!
                    UploadedAt = DateTime.UtcNow.AddDays(-4),
                    Visibility = DocumentVisibility.Private
                },
                new Document
                {
                    FileName = "User_Guide.pdf",
                    FilePath = "/documents/User_Guide.pdf",
                    ContentType = "application/pdf",
                    FileSize = 768000,
                    ExtractedText = "User guide for the DocN application...",
                    SuggestedCategory = "Documentation",
                    ActualCategory = "Documentation",
                    EmbeddingVector = null, // NO VECTOR - this should still be shown!
                    UploadedAt = DateTime.UtcNow.AddDays(-3),
                    Visibility = DocumentVisibility.Private
                },
                new Document
                {
                    FileName = "Meeting_Notes_2025.txt",
                    FilePath = "/documents/Meeting_Notes_2025.txt",
                    ContentType = "text/plain",
                    FileSize = 102400,
                    ExtractedText = "Meeting notes from December 2025...",
                    SuggestedCategory = "Notes",
                    ActualCategory = "Notes",
                    EmbeddingVector = null, // Simulated vector would go here
                    UploadedAt = DateTime.UtcNow.AddDays(-2),
                    Visibility = DocumentVisibility.Private
                },
                new Document
                {
                    FileName = "Budget_Report.xlsx",
                    FilePath = "/documents/Budget_Report.xlsx",
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    FileSize = 256000,
                    ExtractedText = "Budget report for Q4 2025...",
                    SuggestedCategory = "Financial",
                    ActualCategory = "Financial",
                    EmbeddingVector = null, // NO VECTOR - this should still be shown!
                    UploadedAt = DateTime.UtcNow.AddDays(-1),
                    Visibility = DocumentVisibility.Private
                }
            };

            _context.Documents.AddRange(documents);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} sample documents (some with vectors, some without)", documents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
        }
    }
}
