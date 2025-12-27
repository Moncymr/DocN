# Fix Documentation: Documents Not Displaying

## Problem Description
User reported that 5 documents exist in the database `[DocumentArchive].[dbo].[Documents]` but were not visible in the UI when accessing the Documents page.

## Root Causes Identified

### 1. Database Connection Mismatch
The main client application (`DocN.Client`) was configured to connect to a different database:
- **Previous Configuration**: Connected to `DocNDb` database
- **Actual Data Location**: Documents exist in `DocumentArchive` database
- **Fix Applied**: Updated connection string in `DocN.Client/appsettings.json` to point to the correct database

### 2. Documents Without Owner Not Shown to Logged-In Users
The `DocumentService.GetUserDocumentsAsync()` method had logic that excluded documents without an owner (`OwnerId == null`) when a user was logged in. This meant:
- Anonymous users could see documents without an owner
- Logged-in users could NOT see documents without an owner
- **Fix Applied**: Modified the query to include documents without an owner for all users

### 3. Statistics Not Including Unowned Documents
Similar issue in `DocumentStatisticsService.GetStatisticsAsync()` where documents without an owner were not counted in statistics for logged-in users.

## Changes Made

### File: `DocN.Client/appsettings.json`
**Before:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DocNDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

**After:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=NTSPJ-060-02\\SQL2025;Database=DocumentArchive;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

### File: `DocN.Data/Services/DocumentService.cs`

#### Method: `GetUserDocumentsAsync()`
Added logic to include documents without an owner (legacy/public documents) for logged-in users:

```csharp
// OLD: Only owned and shared documents
var ownedDocs = _context.Documents.Where(d => d.OwnerId == userId);
var sharedDocs = _context.Documents.Where(d => d.Shares.Any(s => s.SharedWithUserId == userId));
query = ownedDocs.Union(sharedDocs);

// NEW: Include unowned documents too
var ownedDocs = _context.Documents.Where(d => d.OwnerId == userId);
var sharedDocs = _context.Documents.Where(d => d.Shares.Any(s => s.SharedWithUserId == userId));
var unownedDocs = _context.Documents.Where(d => d.OwnerId == null);
query = ownedDocs.Union(sharedDocs).Union(unownedDocs);
```

#### Method: `GetTotalDocumentCountAsync()`
Added count of unowned documents to the total for logged-in users.

### File: `DocN.Data/Services/DocumentStatisticsService.cs`
Modified `GetStatisticsAsync()` to include documents without an owner in statistics calculations.

### File: `DocN.Data/DocN.Data.csproj`
Added missing package reference to fix build error:
```xml
<PackageReference Include="Microsoft.Extensions.Hosting.Abstractions" Version="10.0.0" />
```

## Testing & Verification

### To Verify the Fix:

1. **Start the Application**
   ```bash
   cd DocN.Client
   dotnet run
   ```

2. **Navigate to Documents Page**
   - Go to `/documents` in your browser
   - You should now see all 5 documents from the DocumentArchive database

3. **Check Both Logged-In and Anonymous Access**
   - Test as anonymous user: Documents without owner should be visible
   - Test as logged-in user: Documents without owner should STILL be visible (this was the bug)

4. **Verify Dashboard Statistics**
   - Go to `/dashboard`
   - Statistics should include all documents (including those without an owner)

### Expected Results:
- ✅ All 5 documents from `[DocumentArchive].[dbo].[Documents]` should be visible
- ✅ Documents without an `OwnerId` should be visible to all users
- ✅ Dashboard statistics should reflect all documents
- ✅ No "Nessun documento trovato" (No documents found) message when documents exist

## Database Schema Notes

The `Document` table has an `OwnerId` field that can be NULL:
- `OwnerId = NULL`: Legacy documents or public documents without a specific owner
- `OwnerId = <userId>`: Documents owned by a specific user

Documents without an owner should be treated as public/shared documents accessible by all users.

## Additional Considerations

### If Documents Still Don't Appear:

1. **Verify Database Connection**
   - Ensure the SQL Server instance `NTSPJ-060-02\SQL2025` is running and accessible
   - Verify the database name is exactly `DocumentArchive` (not `DocumentArchive_new`)
   - Check if authentication (Trusted_Connection) works for your environment

2. **Check Database Records**
   Run this SQL query to verify documents exist:
   ```sql
   SELECT Id, FileName, OwnerId, Visibility, UploadedAt 
   FROM [DocumentArchive].[dbo].[Documents]
   ```

3. **Verify Entity Framework Migrations**
   - The `ApplicationDbContext` expects certain columns and relationships
   - Ensure the database schema matches the model definitions

4. **Check Application Logs**
   - Look for any database connection errors
   - Check for any EF Core query errors

## Security Implications

This change makes documents without an owner visible to all logged-in users. If this is not desired:
- Consider setting an appropriate `Visibility` level on documents without an owner
- Modify the query logic to check `Visibility` settings instead of just `OwnerId`
- Update existing NULL owner documents to have a proper owner or visibility setting
