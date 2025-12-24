using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocN.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocN.Server.Services.DocumentProcessing;

/// <summary>
/// Word document text extractor using OpenXML
/// </summary>
public class WordDocumentExtractor : IDocumentExtractor
{
    private readonly ILogger<WordDocumentExtractor> _logger;

    public WordDocumentExtractor(ILogger<WordDocumentExtractor> logger)
    {
        _logger = logger;
    }

    public bool SupportsFileType(string contentType, string fileName)
    {
        return contentType?.ToLower().Contains("wordprocessingml") == true ||
               contentType?.ToLower().Contains("msword") == true ||
               fileName?.ToLower().EndsWith(".docx") == true ||
               fileName?.ToLower().EndsWith(".doc") == true;
    }

    public async Task<string> ExtractTextAsync(Stream stream, string fileName)
    {
        try
        {
            _logger.LogInformation("Extracting text from Word document: {FileName}", fileName);

            using var wordDocument = WordprocessingDocument.Open(stream, false);
            var body = wordDocument.MainDocumentPart?.Document?.Body;

            if (body == null)
            {
                _logger.LogWarning("No body found in Word document: {FileName}", fileName);
                return string.Empty;
            }

            var text = new System.Text.StringBuilder();

            // Extract text from all paragraphs
            foreach (var paragraph in body.Descendants<Paragraph>())
            {
                var paragraphText = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(paragraphText))
                {
                    text.AppendLine(paragraphText);
                }
            }

            // Extract text from tables
            foreach (var table in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Table>())
            {
                foreach (var row in table.Descendants<TableRow>())
                {
                    var rowTexts = new List<string>();
                    foreach (var cell in row.Descendants<TableCell>())
                    {
                        var cellText = cell.InnerText.Trim();
                        if (!string.IsNullOrWhiteSpace(cellText))
                        {
                            rowTexts.Add(cellText);
                        }
                    }
                    if (rowTexts.Any())
                    {
                        text.AppendLine(string.Join(" | ", rowTexts));
                    }
                }
            }

            var extractedText = text.ToString().Trim();
            _logger.LogInformation("Successfully extracted {Length} characters from Word document",
                extractedText.Length);

            return await Task.FromResult(extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from Word document: {FileName}", fileName);
            throw new InvalidOperationException($"Failed to extract text from Word document: {ex.Message}", ex);
        }
    }
}
