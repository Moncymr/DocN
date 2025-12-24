using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocN.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocN.Server.Services.DocumentProcessing;

/// <summary>
/// Excel document text extractor using OpenXML
/// </summary>
public class ExcelExtractor : IDocumentExtractor
{
    private readonly ILogger<ExcelExtractor> _logger;

    public ExcelExtractor(ILogger<ExcelExtractor> logger)
    {
        _logger = logger;
    }

    public bool SupportsFileType(string contentType, string fileName)
    {
        return contentType?.ToLower().Contains("spreadsheetml") == true ||
               contentType?.ToLower().Contains("excel") == true ||
               fileName?.ToLower().EndsWith(".xlsx") == true ||
               fileName?.ToLower().EndsWith(".xls") == true;
    }

    public async Task<string> ExtractTextAsync(Stream stream, string fileName)
    {
        try
        {
            _logger.LogInformation("Extracting text from Excel document: {FileName}", fileName);

            using var spreadsheetDocument = SpreadsheetDocument.Open(stream, false);
            var workbookPart = spreadsheetDocument.WorkbookPart;

            if (workbookPart == null)
            {
                _logger.LogWarning("No workbook part found in Excel document: {FileName}", fileName);
                return string.Empty;
            }

            var text = new System.Text.StringBuilder();
            var sheets = workbookPart.Workbook.Descendants<Sheet>();

            foreach (var sheet in sheets)
            {
                text.AppendLine($"=== Sheet: {sheet.Name} ===");
                text.AppendLine();

                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
                var sheetData = worksheetPart.Worksheet.Elements<SheetData>().FirstOrDefault();

                if (sheetData == null)
                    continue;

                foreach (var row in sheetData.Elements<Row>())
                {
                    var rowValues = new List<string>();
                    
                    foreach (var cell in row.Elements<Cell>())
                    {
                        var cellValue = GetCellValue(cell, workbookPart);
                        if (!string.IsNullOrWhiteSpace(cellValue))
                        {
                            rowValues.Add(cellValue);
                        }
                    }

                    if (rowValues.Any())
                    {
                        text.AppendLine(string.Join(" | ", rowValues));
                    }
                }

                text.AppendLine();
            }

            var extractedText = text.ToString().Trim();
            _logger.LogInformation("Successfully extracted {Length} characters from Excel document with {SheetCount} sheets",
                extractedText.Length, sheets.Count());

            return await Task.FromResult(extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from Excel document: {FileName}", fileName);
            throw new InvalidOperationException($"Failed to extract text from Excel document: {ex.Message}", ex);
        }
    }

    private string GetCellValue(Cell cell, WorkbookPart workbookPart)
    {
        if (cell.CellValue == null)
            return string.Empty;

        var value = cell.CellValue.Text;

        // If the cell is a shared string, get the actual value
        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            var stringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
            if (stringTable != null)
            {
                value = stringTable.ElementAt(int.Parse(value)).InnerText;
            }
        }

        return value ?? string.Empty;
    }
}
