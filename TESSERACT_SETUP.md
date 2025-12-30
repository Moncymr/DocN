# Tesseract OCR Setup Guide

## Prerequisites

To use OCR functionality in DocN, you need to install Tesseract native libraries on your system.

### Windows

1. **Download Tesseract Installer**:
   - Visit: https://github.com/UB-Mannheim/tesseract/wiki
   - Download the latest installer (e.g., `tesseract-ocr-w64-setup-5.3.3.exe`)
   - Run the installer

2. **Install Language Data**:
   - During installation, select the languages you want to support
   - Or manually download `.traineddata` files to `DocN.Client/tessdata`

**Download language files manually (PowerShell)**:
```powershell
cd DocN.Client/tessdata
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata" -OutFile "ita.traineddata"
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata" -OutFile "eng.traineddata"
```

### Linux (Ubuntu/Debian)

```bash
# Install Tesseract and dependencies
sudo apt-get update
sudo apt-get install -y tesseract-ocr libtesseract-dev libleptonica-dev

# Install Italian language data
sudo apt-get install -y tesseract-ocr-ita

# Or download manually
cd DocN.Client/tessdata
curl -L -o ita.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata
curl -L -o eng.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
```

### macOS

```bash
# Install Tesseract using Homebrew
brew install tesseract

# Install additional languages
brew install tesseract-lang

# Or download manually
cd DocN.Client/tessdata
curl -L -o ita.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata
curl -L -o eng.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
```

### Docker

If deploying with Docker, add to your Dockerfile:

```dockerfile
# For Debian/Ubuntu base images
RUN apt-get update && \
    apt-get install -y tesseract-ocr libtesseract-dev libleptonica-dev && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy language data
COPY DocN.Client/tessdata /app/DocN.Client/tessdata
```

## Configuration

### appsettings.json

```json
{
  "Tesseract": {
    "DataPath": "./tessdata",
    "Language": "ita"
  }
}
```

### Environment Variables (Alternative)

```bash
export Tesseract__DataPath="/path/to/tessdata"
export Tesseract__Language="ita"
```

## Verification

To verify OCR is working:

1. Start the application
2. Check the logs for: `Tesseract OCR initialized successfully`
3. Upload an image with text
4. View the document details - extracted text should appear

## Troubleshooting

### "Failed to find library libleptonica"

**Cause**: Native Tesseract libraries not installed

**Solution**:
- Windows: Reinstall Tesseract from official installer
- Linux: `sudo apt-get install libleptonica-dev`
- macOS: `brew install leptonica`

### "Tessdata directory not found"

**Cause**: Language data files missing

**Solution**:
1. Create directory: `mkdir -p DocN.Client/tessdata`
2. Download language files (see above)
3. Verify files exist: `ls -la DocN.Client/tessdata/*.traineddata`

### "No text detected"

**Possible causes**:
- Image quality too low
- Wrong language configured
- Text too small or unclear

**Solutions**:
- Use higher resolution images (300 DPI or higher)
- Verify `Tesseract:Language` matches the text language
- Try preprocessing images (contrast enhancement)

### OCR is slow

**Optimization tips**:
- Use smaller images (resize before upload)
- Use faster language data (tessdata_fast instead of tessdata)
- Consider caching OCR results
- Process images asynchronously in background

## Language Codes

Common language codes for `Tesseract:Language` setting:

- `eng` - English
- `ita` - Italian
- `fra` - French
- `deu` - German
- `spa` - Spanish
- `por` - Portuguese
- `rus` - Russian
- `jpn` - Japanese
- `chi_sim` - Chinese Simplified
- `chi_tra` - Chinese Traditional
- `ara` - Arabic
- `eng+ita` - Multiple languages (English + Italian)

## Performance Characteristics

- **First OCR**: 2-5 seconds (engine initialization + OCR)
- **Subsequent OCRs**: 1-3 seconds (OCR only)
- **Memory usage**: ~50-100 MB per Tesseract instance
- **CPU usage**: High during OCR, idle otherwise

## Security Considerations

1. **File size limits**: Configure maximum upload size to prevent resource exhaustion
2. **Timeout**: OCR operations have reasonable timeouts
3. **Temporary files**: Automatically cleaned up after processing
4. **Path validation**: All file paths are validated and sanitized

## Production Deployment

### Recommended Setup

1. **Install native libraries**: As shown above for your OS
2. **Configure language data**: Download and configure required languages
3. **Set up monitoring**: Monitor OCR success/failure rates
4. **Configure logging**: Enable detailed logging for troubleshooting
5. **Test thoroughly**: Upload various image types to verify OCR works

### High Availability

For high-traffic applications:
- Consider using a dedicated OCR service (Azure Computer Vision, Google Cloud Vision)
- Implement queue-based processing for OCR tasks
- Use multiple worker instances for parallel processing
- Cache OCR results to avoid reprocessing

## References

- [Tesseract OCR Official Documentation](https://tesseract-ocr.github.io/)
- [Tesseract.NET GitHub](https://github.com/charlesw/tesseract)
- [Language Data Files](https://github.com/tesseract-ocr/tessdata)
- [OCR Best Practices](https://tesseract-ocr.github.io/tessdoc/ImproveQuality.html)
