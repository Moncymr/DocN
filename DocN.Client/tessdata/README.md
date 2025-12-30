# Tesseract OCR Language Data Files

This directory contains language data files for Tesseract OCR.

## Installation

To enable OCR text extraction from images, you need to download the Tesseract language data files:

1. **Italian Language (recommended for this application)**:
   - Download `ita.traineddata` from: https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata
   - Place it in this directory

2. **English Language (for English documents)**:
   - Download `eng.traineddata` from: https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
   - Place it in this directory

3. **Other Languages**:
   - Visit https://github.com/tesseract-ocr/tessdata for more language files
   - Download the `.traineddata` file for your language
   - Place it in this directory

## Quick Installation Commands

### Windows PowerShell:
```powershell
cd DocN.Client/tessdata
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata" -OutFile "ita.traineddata"
Invoke-WebRequest -Uri "https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata" -OutFile "eng.traineddata"
```

### Linux/macOS:
```bash
cd DocN.Client/tessdata
curl -L -o ita.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/ita.traineddata
curl -L -o eng.traineddata https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata
```

## Configuration

The OCR language can be configured in `appsettings.json`:

```json
{
  "Tesseract": {
    "DataPath": "./tessdata",
    "Language": "ita"
  }
}
```

- `DataPath`: Path to the tessdata directory (relative to the application root)
- `Language`: Language code (e.g., "ita" for Italian, "eng" for English, "eng+ita" for multiple languages)

## Supported Languages

Common language codes:
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

For a complete list, see: https://tesseract-ocr.github.io/tessdoc/Data-Files

## Notes

- The application will work without OCR data files, but text extraction from images will be skipped
- Multiple languages can be specified with "+" (e.g., "eng+ita")
- The first run with a new language file may be slower as Tesseract initializes
- Language files are approximately 10-15 MB each
