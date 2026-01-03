# Multi-File Upload Feature

## Overview
The Multi-File Upload feature allows users to upload multiple documents simultaneously with shared processing options, significantly improving efficiency when dealing with bulk document uploads.

## Features

### 📁 Category Management
- **Single Category Selection**: All uploaded documents are automatically categorized under the same category
- **Required Field**: Users must select a category before proceeding with the upload

### 📤 File Selection
- **Multiple File Support**: Users can select up to 100 files in a single operation
- **Drag & Drop**: Supports drag-and-drop file selection
- **File Validation**: Automatically validates file types and sizes
  - Maximum file size: 50MB (configurable)
  - Supported formats: PDF, DOCX, XLSX, TXT, images, and more

### 👁️ Visibility Settings
- **Shared Settings**: All documents receive the same visibility level
- **Options**:
  - 🔒 Private - Only visible to the uploader
  - 👥 Shared - Share with specific users
  - 🏢 Organization - Visible to all organization members
  - 🌐 Public - Visible to everyone

### 🏷️ Metadata
- **Tags**: Apply common tags to all documents (comma-separated)
- **Notes**: Add shared notes to all uploaded documents

### ⚙️ Processing Options

All processing options from the single-file upload are available:

1. **📝 Estrazione testo automatica** (Text Extraction)
   - Automatically extracts text content from documents
   - Supports OCR for images
   - Works with multiple formats: PDF, DOCX, images, etc.

2. **🏷️ Estrai tag automaticamente con AI** (AI Tag Extraction)
   - Uses AI to automatically extract relevant tags and keywords
   - Complements manually entered tags

3. **📋 Estrai metadati strutturati con AI** (AI Metadata Extraction)
   - Extracts structured metadata like:
     - Invoice numbers
     - Dates
     - Authors
     - Contract details
     - And more

4. **🧠 Genera embeddings (Gemini)** (Generate Embeddings)
   - Creates vector embeddings for semantic search
   - Enables intelligent document discovery
   - Supports similarity search

5. **⚡ Genera embeddings chunks immediatamente** (Immediate Chunk Embeddings)
   - **Default (Unchecked)**: Creates chunks immediately but generates embeddings in background (faster uploads)
   - **Checked**: Generates all embeddings during upload (slower but complete immediately)

### 📊 Progress Tracking

The upload process includes real-time progress tracking:

- **Overall Progress Bar**: Shows percentage of completed files
- **Per-File Status**: Individual status for each file:
  - ⏳ **Pending**: Waiting to be processed
  - 🔄 **Processing**: Currently being processed with detailed step information
  - ✅ **Completed**: Successfully uploaded and processed
  - ❌ **Error**: Failed with error details

### 🚀 Asynchronous Processing

- **Non-Blocking**: Files are processed asynchronously without blocking the UI
- **Parallel Processing**: Multiple files can be processed simultaneously
- **Error Isolation**: If one file fails, others continue processing
- **Detailed Logging**: All operations are logged for troubleshooting

## Usage

### Step-by-Step Guide

1. **Navigate to Multi-File Upload**
   - Click on "📤📤 Carica Multiplo" in the navigation menu

2. **Select Category**
   - Enter a category name (required)
   - This category will be applied to all documents

3. **Choose Files**
   - Click the upload area or drag-and-drop files
   - Select multiple files from your file system
   - Review the list of selected files

4. **Configure Options**
   - Set visibility level for all documents
   - Add optional tags (comma-separated)
   - Add optional notes
   - Enable/disable processing options:
     - Text extraction (recommended)
     - AI tag extraction
     - AI metadata extraction
     - Vector embeddings generation
     - Immediate chunk embedding generation

5. **Upload**
   - Click "📤 Carica X Documenti" button
   - Monitor progress for each file
   - Wait for completion or review any errors

6. **Review Results**
   - Success: Automatically redirected to Documents page
   - Partial success: Review error details for failed files
   - All failed: Check error messages and retry

## Processing Flow

For each file, the system performs the following steps:

1. **File Validation**: Checks file size and format
2. **File Storage**: Saves file to configured upload directory
3. **Text Extraction**: Extracts text content (if enabled)
4. **Embedding Generation**: Creates vector embeddings (if enabled)
5. **Tag Extraction**: Uses AI to extract tags (if enabled)
6. **Metadata Extraction**: Uses AI to extract structured metadata (if enabled)
7. **Database Save**: Creates document record with all data
8. **Chunk Processing**: Creates chunks and optionally generates embeddings

## Error Handling

### File-Level Errors
- Errors are isolated per file
- Failed files don't affect successful uploads
- Detailed error messages provided for troubleshooting

### Common Issues
- **File too large**: Reduce file size or split into smaller files
- **Unsupported format**: Convert to supported format
- **AI processing failed**: Check AI provider configuration
- **Network timeout**: Retry upload or check connection

## Configuration

### appsettings.json

```json
{
  "FileStorage": {
    "UploadPath": "C:\\DocumentArchive\\Uploads",
    "MaxFileSizeMB": 50,
    "AllowedExtensions": [
      ".pdf", ".doc", ".docx", ".xlsx", ".xls", 
      ".pptx", ".ppt", ".txt", ".md", ".csv",
      ".json", ".xml", ".html", ".htm",
      ".png", ".jpg", ".jpeg", ".bmp", ".tiff", 
      ".tif", ".gif", ".webp"
    ]
  }
}
```

## Performance Considerations

### Optimal Settings
- **Small batches**: 10-20 files at a time for best performance
- **Background embeddings**: Leave "Immediate chunk embeddings" unchecked for faster uploads
- **File size**: Keep files under 10MB when possible
- **Network**: Ensure stable internet connection for AI processing

### Large Batches
- Files are processed asynchronously
- UI remains responsive during processing
- Server resources are managed efficiently
- Progress updates every few seconds

## Access Control

- **Authentication**: Users must be authenticated to upload
- **Ownership**: Uploaded documents are owned by the uploader
- **Visibility**: Controlled by selected visibility setting
- **Multi-tenancy**: Documents are isolated by tenant/organization

## Comparison with Single Upload

| Feature | Single Upload | Multi-File Upload |
|---------|--------------|-------------------|
| Files per operation | 1 | Up to 100 |
| Category per file | Individual | Shared |
| Processing options | Per file | Shared across all |
| Progress tracking | Simple | Detailed per file |
| AI analysis | Optional per file | Optional shared |
| Upload time | Fast | Depends on file count |
| Error handling | Single point | Isolated per file |

## Technical Details

### Technologies Used
- **Frontend**: Blazor Server with InteractiveServer render mode
- **File Upload**: ASP.NET Core InputFile component
- **Async Processing**: Task.WhenAll for parallel processing
- **Progress Updates**: StateHasChanged for real-time UI updates
- **Logging**: ILogService for comprehensive logging

### Code Structure
- **Component**: `/DocN.Client/Components/Pages/UploadMultiple.razor`
- **Processing**: Individual file processing in separate tasks
- **State Management**: Per-file status tracking with FileUploadStatus class
- **Error Isolation**: Try-catch blocks per file operation

## Future Enhancements

Potential improvements:
- [ ] Pause/Resume functionality
- [ ] Retry failed files individually
- [ ] Cancel individual files
- [ ] Upload queue management
- [ ] File preview before upload
- [ ] Duplicate detection
- [ ] Auto-categorization based on file name patterns
- [ ] Bulk editing after upload
- [ ] Export upload report

## Support

For issues or questions:
1. Check the logs modal in the upload page
2. Review error messages for specific files
3. Verify AI provider configuration
4. Check file format and size requirements
5. Contact system administrator if problems persist

## See Also
- [Single File Upload](./UPLOAD.md)
- [Document Management](./DOCUMENTS.md)
- [AI Configuration](./AI_CONFIG.md)
- [Search Features](./SEARCH.md)
