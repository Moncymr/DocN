# Testing Plan - Document Upload Improvements

## Overview
This document provides a comprehensive testing plan to verify the fixes for document upload issues.

## Prerequisites
- DocN application running locally
- Database connection configured
- AI provider configured (Gemini/OpenAI/Azure OpenAI)

## Test Scenarios

### Scenario 1: Verify UI Error Display (Critical Fix)

**Purpose**: Ensure UI no longer freezes and displays errors properly

**Steps**:
1. Navigate to Upload page (`/upload`)
2. Select a PDF file
3. Click "Analizza Documento" - wait for processing
4. **Simulate DB Error** (one of these methods):
   - Temporarily disconnect database
   - OR modify connection string to be invalid
   - OR stop SQL Server service
5. Fill in category and tags
6. Click "Carica Documento"

**Expected Result**:
- ✅ Error message appears immediately (not after timeout)
- ✅ Error message is in Italian and user-friendly
- ✅ Error mentions file was saved but not in database
- ✅ Error provides filename for admin reference
- ✅ Button re-enables after error
- ✅ NO infinite loading spinner

**Previous Behavior**:
- ❌ Loading spinner continues forever
- ❌ No error message shown
- ❌ UI appears frozen

---

### Scenario 2: AI Category Suggestion - Happy Path

**Purpose**: Verify AI proposes meaningful categories

**Steps**:
1. Navigate to Upload page
2. Select a test file (e.g., "contract_example.pdf")
3. Enable "🤖 Suggerisci categoria con AI" checkbox
4. Click "Analizza Documento"

**Expected Result**:
- ✅ Category field populated with specific category (e.g., "Legal Contracts")
- ✅ Reasoning shown explaining why
- ✅ Category is NOT "Uncategorized"

**Test Files**:
- `invoice_2024.pdf` → Should suggest "Financial Documents"
- `meeting_minutes.docx` → Should suggest "Meeting Minutes" 
- `technical_guide.pdf` → Should suggest "Documentation"

---

### Scenario 3: AI Category Suggestion - Fallback (Key Improvement)

**Purpose**: Verify fallback works when AI fails/unavailable

**Steps**:
1. Navigate to Upload page
2. **Disable AI** (one of these):
   - Remove/invalidate API key in configuration
   - OR disconnect internet
   - OR use AI Configuration page to clear keys
3. Select a file named "report_annual_2024.xlsx"
4. Enable "🤖 Suggerisci categoria con AI" checkbox
5. Click "Analizza Documento"

**Expected Result**:
- ✅ Category field shows fallback category (e.g., "Reports" from filename OR "Spreadsheets" from extension)
- ✅ NO "Uncategorized" appears
- ✅ Reasoning explains AI failed but category inferred
- ✅ User can still save document

**Previous Behavior**:
- ❌ Category showed "Uncategorized"
- ❌ No helpful suggestion

---

### Scenario 4: Tag Extraction Success

**Purpose**: Verify tag extraction works when AI is available

**Steps**:
1. Navigate to Upload page
2. Select a document with clear content (e.g., invoice with company name, dates)
3. Enable "🏷️ Estrai tag automaticamente con AI" checkbox
4. Click "Analizza Documento"

**Expected Result**:
- ✅ Green badge with "✓ Successo"
- ✅ Tags displayed as green pills/badges
- ✅ Tags automatically filled in the tag field
- ✅ User can edit/add more tags

---

### Scenario 5: Tag Extraction Failure

**Purpose**: Verify graceful handling when tag extraction fails

**Steps**:
1. Navigate to Upload page
2. **Disable AI** or select file with no extractable text
3. Enable "🏷️ Estrai tag automaticamente con AI" checkbox
4. Click "Analizza Documento"

**Expected Result**:
- ✅ Red/orange warning badge
- ✅ Message: "❌ L'AI non è riuscita a estrarre i tag dal documento"
- ✅ Guidance: "👇 Inserisci manualmente i tag nel campo qui sotto"
- ✅ Tag field remains empty and editable
- ✅ User can still save document

---

### Scenario 6: Complete Upload Success

**Purpose**: Verify end-to-end happy path

**Steps**:
1. Navigate to Upload page
2. Select a valid document
3. Enable all processing options
4. Click "Analizza Documento"
5. Wait for all processing to complete
6. Verify suggested category (edit if needed)
7. Verify/edit tags
8. Select visibility level
9. Add optional notes
10. Click "Carica Documento"

**Expected Result**:
- ✅ Success message appears
- ✅ After 2 seconds, redirected to `/documents`
- ✅ New document appears in list
- ✅ Document has correct category, tags, visibility

---

### Scenario 7: Edge Cases

**Purpose**: Test unusual scenarios

#### 7.1 File without extension
1. Upload a file named "myfile" (no extension)
2. Process document

**Expected**: Category shows "Unknown Files" (not " Files")

#### 7.2 Very long text
1. Upload large PDF (>10MB)
2. Process document

**Expected**: Text truncated, no crashes, processing completes

#### 7.3 Empty file
1. Upload empty text file
2. Process document

**Expected**: Graceful handling, appropriate category inferred

---

## Verification Checklist

After running all scenarios, verify:

- [ ] No UI freezes observed in any scenario
- [ ] All error messages appear immediately
- [ ] All error messages are user-friendly (Italian)
- [ ] Categories are always meaningful (never "Uncategorized")
- [ ] Tag extraction failures handled gracefully
- [ ] Database saves work correctly
- [ ] File saves work correctly
- [ ] Console logs show detailed errors for debugging

## Console Log Inspection

For each test, check browser console (`F12`) for:
- No unhandled exceptions
- Appropriate error logging
- Stack traces present for debugging (but not shown to user)

## Database Verification

After successful uploads, verify in database:
```sql
SELECT TOP 10 
    FileName, 
    ActualCategory, 
    SuggestedCategory,
    CategoryReasoning,
    UploadedAt
FROM Documents
ORDER BY UploadedAt DESC
```

Check:
- [ ] ActualCategory is never NULL
- [ ] ActualCategory is never "Uncategorized"
- [ ] CategoryReasoning provides context

## Performance Check

Measure time for each step:
- File selection: Instant
- Text extraction: < 5 seconds
- Embedding generation: < 10 seconds
- Category suggestion: < 5 seconds
- Tag extraction: < 5 seconds
- Database save: < 2 seconds

## Rollback Plan

If issues found:
1. Revert to previous commit: `git revert HEAD~3..HEAD`
2. Or checkout previous branch
3. Investigate issue
4. Apply fix incrementally

## Success Criteria

All scenarios must pass for deployment:
- ✅ No UI freezes
- ✅ Errors always visible
- ✅ Categories always meaningful
- ✅ No "Uncategorized" categories
- ✅ Graceful fallbacks working
- ✅ Database saves successful
- ✅ No security vulnerabilities (CodeQL passed)
- ✅ Code compiles without errors

## Notes

- Test with different file types: PDF, DOCX, XLSX, TXT
- Test with different AI providers if available
- Test with Italian and English file names
- Test with very long file names
- Test with special characters in file names

## Support

If you encounter issues during testing:
1. Check browser console for errors
2. Check server logs
3. Review IMPLEMENTATION_SUMMARY.md for technical details
4. Check connection strings in appsettings.json
5. Verify AI provider configuration in AI Config page
