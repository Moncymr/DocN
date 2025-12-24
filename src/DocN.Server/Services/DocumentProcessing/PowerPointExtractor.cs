using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocN.Core.Interfaces;
using Microsoft.Extensions.Logging;
using A = DocumentFormat.OpenXml.Drawing;

namespace DocN.Server.Services.DocumentProcessing;

/// <summary>
/// PowerPoint text extractor using OpenXML
/// </summary>
public class PowerPointExtractor : IDocumentExtractor
{
    private readonly ILogger<PowerPointExtractor> _logger;

    public PowerPointExtractor(ILogger<PowerPointExtractor> logger)
    {
        _logger = logger;
    }

    public bool SupportsFileType(string contentType, string fileName)
    {
        return contentType?.ToLower().Contains("presentationml") == true ||
               contentType?.ToLower().Contains("powerpoint") == true ||
               fileName?.ToLower().EndsWith(".pptx") == true ||
               fileName?.ToLower().EndsWith(".ppt") == true;
    }

    public async Task<string> ExtractTextAsync(Stream stream, string fileName)
    {
        try
        {
            _logger.LogInformation("Extracting text from PowerPoint: {FileName}", fileName);

            using var presentationDocument = PresentationDocument.Open(stream, false);
            var presentationPart = presentationDocument.PresentationPart;

            if (presentationPart == null)
            {
                _logger.LogWarning("No presentation part found: {FileName}", fileName);
                return string.Empty;
            }

            var text = new System.Text.StringBuilder();
            var slideIdList = presentationPart.Presentation.SlideIdList;

            if (slideIdList == null)
            {
                _logger.LogWarning("No slides found: {FileName}", fileName);
                return string.Empty;
            }

            int slideNumber = 1;
            foreach (var slideId in slideIdList.ChildElements.OfType<SlideId>())
            {
                var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!);
                
                text.AppendLine($"=== Slide {slideNumber} ===");
                text.AppendLine();

                // Extract text from all text elements in the slide
                var slideText = ExtractTextFromSlide(slidePart);
                if (!string.IsNullOrWhiteSpace(slideText))
                {
                    text.AppendLine(slideText);
                }

                // Extract notes if present
                if (slidePart.NotesSlidePart != null)
                {
                    var notesText = ExtractTextFromNotes(slidePart.NotesSlidePart);
                    if (!string.IsNullOrWhiteSpace(notesText))
                    {
                        text.AppendLine();
                        text.AppendLine("Notes:");
                        text.AppendLine(notesText);
                    }
                }

                text.AppendLine();
                slideNumber++;
            }

            var extractedText = text.ToString().Trim();
            _logger.LogInformation("Successfully extracted {Length} characters from {SlideCount} slides",
                extractedText.Length, slideNumber - 1);

            return await Task.FromResult(extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from PowerPoint: {FileName}", fileName);
            throw new InvalidOperationException($"Failed to extract text from PowerPoint: {ex.Message}", ex);
        }
    }

    private string ExtractTextFromSlide(SlidePart slidePart)
    {
        var text = new System.Text.StringBuilder();

        // Extract all text from paragraph elements
        foreach (var paragraph in slidePart.Slide.Descendants<A.Paragraph>())
        {
            var paragraphText = paragraph.InnerText;
            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                text.AppendLine(paragraphText);
            }
        }

        return text.ToString().Trim();
    }

    private string ExtractTextFromNotes(NotesSlidePart notesSlidePart)
    {
        var text = new System.Text.StringBuilder();

        foreach (var paragraph in notesSlidePart.NotesSlide.Descendants<A.Paragraph>())
        {
            var paragraphText = paragraph.InnerText;
            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                text.AppendLine(paragraphText);
            }
        }

        return text.ToString().Trim();
    }
}
