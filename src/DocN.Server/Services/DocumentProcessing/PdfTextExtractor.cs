using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using DocN.Core.Interfaces;
using Microsoft.Extensions.Logging;
using PdfTextExtractor = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor;

namespace DocN.Server.Services.DocumentProcessing;

/// <summary>
/// PDF text extractor using iText7
/// </summary>
public class PdfDocumentExtractor : IDocumentExtractor
{
    private readonly ILogger<PdfDocumentExtractor> _logger;

    public PdfDocumentExtractor(ILogger<PdfDocumentExtractor> logger)
    {
        _logger = logger;
    }

    public bool SupportsFileType(string contentType, string fileName)
    {
        return contentType?.ToLower().Contains("pdf") == true ||
               fileName?.ToLower().EndsWith(".pdf") == true;
    }

    public async Task<string> ExtractTextAsync(Stream stream, string fileName)
    {
        try
        {
            _logger.LogInformation("Extracting text from PDF: {FileName}", fileName);

            using var pdfReader = new PdfReader(stream);
            using var pdfDocument = new PdfDocument(pdfReader);
            
            var text = new System.Text.StringBuilder();
            var numberOfPages = pdfDocument.GetNumberOfPages();

            for (int i = 1; i <= numberOfPages; i++)
            {
                var page = pdfDocument.GetPage(i);
                var strategy = new SimpleTextExtractionStrategy();
                var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    text.AppendLine(pageText);
                    text.AppendLine(); // Add spacing between pages
                }
            }

            var extractedText = text.ToString().Trim();
            _logger.LogInformation("Successfully extracted {Length} characters from {Pages} pages",
                extractedText.Length, numberOfPages);

            return await Task.FromResult(extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PDF: {FileName}", fileName);
            throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}", ex);
        }
    }
}
