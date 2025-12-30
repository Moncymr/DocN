# OCR Implementation with Tesseract

## Overview

This document describes the OCR (Optical Character Recognition) implementation for extracting text from images in the DocN application.

## Features

- **Automatic text extraction from images**: When an image file is uploaded (PNG, JPG, JPEG, BMP, TIFF, GIF, WEBP), the system automatically attempts to extract text using Tesseract OCR.
- **Multi-language support**: Supports Italian (default), English, and many other languages.
- **Seamless integration**: Extracted text is stored alongside image metadata and is fully searchable.
- **Fallback handling**: If OCR is not available or fails, the system continues to work normally without OCR text extraction.

## Technical Implementation

### Components

1. **IOCRService Interface** (`DocN.Core/Interfaces/IOCRService.cs`)
   - Defines the contract for OCR services
   - Methods: `ExtractTextFromImageAsync`, `IsAvailable`

2. **TesseractOCRService** (`DocN.Data/Services/TesseractOCRService.cs`)
   - Implements OCR using Tesseract library
   - Handles image preprocessing and OCR execution
   - Provides confidence scoring for extracted text

3. **FileProcessingService Integration** (`DocN.Data/Services/FileProcessingService.cs`)
   - Automatically invokes OCR when processing image files
   - Stores OCR results in document metadata
   - Appends extracted text to the document's searchable content

### Configuration

OCR settings are configured in `appsettings.json`:

```json
{
  "Tesseract": {
    "DataPath": "./tessdata",
    "Language": "ita"
  }
}
```

- **DataPath**: Directory containing Tesseract language data files
- **Language**: Language code(s) for OCR (e.g., "ita" for Italian, "eng" for English, "eng+ita" for multiple)

### Language Data Files

Tesseract requires language data files to perform OCR. These files are stored in the `tessdata` directory:

**To set up OCR:**

1. Download language data files from: https://github.com/tesseract-ocr/tessdata
2. Place `.traineddata` files in the `DocN.Client/tessdata` directory
3. Common files:
   - `ita.traineddata` - Italian (16 MB)
   - `eng.traineddata` - English (23 MB)

**Quick download commands:**

```bash
# Italian
curl -L -o DocN.Client/tessdata/ita.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata

# English
curl -L -o DocN.Client/tessdata/eng.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
```

### Supported Image Formats

The OCR service works with all image formats supported by the FileProcessingService:
- PNG
- JPG/JPEG
- BMP
- TIFF/TIF
- GIF
- WEBP

## Usage

### Automatic Processing

OCR is automatically invoked when images are uploaded through the document upload interface. No manual intervention is required.

### Extracted Information

For each image, the following information is extracted and stored:

1. **Image Metadata**:
   - Dimensions (width × height)
   - Format
   - EXIF data (camera info, GPS coordinates, etc.)

2. **OCR Results**:
   - Extracted text content
   - OCR language used
   - Character count
   - Confidence level (logged)

3. **Searchability**:
   - All extracted text is indexed
   - Searchable via vector search, text search, and hybrid search

## Example Output

When an image containing text is processed, the extracted text appears in the document's content:

```
[Immagine: document.png]
Formato: PNG
Dimensioni: 1920 x 1080 pixels
Proporzioni: 1.78:1

[Testo estratto con OCR]
Questo è un esempio di testo
estratto da un'immagine tramite OCR.
Il sistema riconosce automaticamente
il testo in italiano.
```

## Error Handling

The OCR implementation includes robust error handling:

1. **OCR Not Available**: If tessdata files are missing, the system logs a warning and continues without OCR
2. **OCR Extraction Fails**: Individual OCR failures are logged but don't prevent document processing
3. **Metadata Tracking**: OCR status is stored in document metadata:
   - `OCR_Enabled`: "true" or "false"
   - `OCR_Language`: Language code used
   - `OCR_CharCount`: Number of characters extracted
   - `OCR_Error`: Error message if extraction failed

## Performance Considerations

- **Initial Load**: First OCR operation may take longer as Tesseract initializes
- **Processing Time**: OCR typically takes 1-5 seconds per image depending on size and complexity
- **Memory Usage**: Tesseract uses ~50-100 MB of memory per active instance
- **Concurrent Processing**: Each request creates its own Tesseract engine instance

## Troubleshooting

### OCR Not Working

1. **Check tessdata directory exists**: `DocN.Client/tessdata/`
2. **Verify language files**: Ensure `.traineddata` files are present
3. **Check logs**: Look for warnings about Tesseract availability
4. **Verify configuration**: Check `Tesseract:DataPath` and `Tesseract:Language` settings

### Low Accuracy

- Use high-resolution images (300 DPI or higher)
- Ensure text is clear and not skewed
- Use appropriate language data file
- Consider preprocessing images (contrast enhancement, deskewing)

### Language Not Recognized

- Download the correct `.traineddata` file for your language
- Update `Tesseract:Language` configuration
- Restart the application

## Security Considerations

1. **File Size Limits**: Large images are handled via temporary files with secure random names
2. **Cleanup**: Temporary files are automatically deleted after processing
3. **Path Traversal Protection**: All file paths are validated and sanitized
4. **Resource Limits**: OCR processing is bounded by request timeout
5. **Image Processing Security**: Uses SixLabors.ImageSharp 3.1.12+ which includes fixes for known vulnerabilities (CVE-2025-54575)

## Future Enhancements

Potential improvements for future releases:

1. **Preprocessing**: Image enhancement for better OCR accuracy
2. **Layout Analysis**: Preserve document structure (tables, columns)
3. **PDF OCR**: Extract text from scanned PDFs
4. **Batch Processing**: Process multiple images in parallel
5. **Custom Training**: Fine-tune Tesseract for specific document types
6. **Cloud OCR**: Integration with Azure Computer Vision or Google Cloud Vision API

## References

- [Tesseract OCR Documentation](https://tesseract-ocr.github.io/)
- [Tesseract Language Data Files](https://github.com/tesseract-ocr/tessdata)
- [Tesseract.NET GitHub](https://github.com/charlesw/tesseract)

## Related Files

- `DocN.Core/Interfaces/IOCRService.cs` - OCR service interface
- `DocN.Data/Services/TesseractOCRService.cs` - Tesseract implementation
- `DocN.Data/Services/FileProcessingService.cs` - Integration point
- `DocN.Client/tessdata/README.md` - Language data setup instructions
