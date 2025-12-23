using DocN.Data;
using DocN.Data.Entities;

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
                    ContentText = "This document contains the project requirements for DocN system...",
                    Category = "Requirements",
                    Vector = new byte[] { 1, 2, 3, 4, 5 }, // Simulated vector
                    UploadedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Document
                {
                    FileName = "Technical_Specification.docx",
                    FilePath = "/documents/Technical_Specification.docx",
                    ContentText = "Technical specifications for the DocN architecture...",
                    Category = "Technical",
                    Vector = null, // NO VECTOR - this should still be shown!
                    UploadedAt = DateTime.UtcNow.AddDays(-4)
                },
                new Document
                {
                    FileName = "User_Guide.pdf",
                    FilePath = "/documents/User_Guide.pdf",
                    ContentText = "User guide for the DocN application...",
                    Category = "Documentation",
                    Vector = null, // NO VECTOR - this should still be shown!
                    UploadedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Document
                {
                    FileName = "Meeting_Notes_2025.txt",
                    FilePath = "/documents/Meeting_Notes_2025.txt",
                    ContentText = "Meeting notes from December 2025...",
                    Category = "Notes",
                    Vector = new byte[] { 6, 7, 8, 9, 10 }, // Simulated vector
                    UploadedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Document
                {
                    FileName = "Budget_Report.xlsx",
                    FilePath = "/documents/Budget_Report.xlsx",
                    ContentText = "Budget report for Q4 2025...",
                    Category = "Financial",
                    Vector = null, // NO VECTOR - this should still be shown!
                    UploadedAt = DateTime.UtcNow.AddDays(-1)
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
