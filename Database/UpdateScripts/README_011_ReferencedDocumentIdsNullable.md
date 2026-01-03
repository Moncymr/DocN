# Fix ReferencedDocumentIds NULL Constraint

## Overview
This update script fixes a database constraint issue where user messages couldn't be saved because the ReferencedDocumentIds column was incorrectly configured as NOT NULL.

## Problem
The application was failing with SQL Server error when saving conversations:
```
❌ Microsoft.Data.SqlClient.SqlException (0x80131904): 
Non è possibile inserire il valore NULL nella colonna 'ReferencedDocumentIds' 
della tabella 'DocNDb.dbo.Messages'. La colonna non ammette valori Null.
```

Translation: "Cannot insert NULL value into the 'ReferencedDocumentIds' column of the 'DocNDb.dbo.Messages' table. The column does not allow NULL values."

The root cause was:
- The migration `20251227115401_AddDocumentMetadataFields` created the column with `NOT NULL` constraint
- Entity Framework configuration expects it to be nullable (`.IsRequired(false)`)
- The model's backing field `ReferencedDocumentIdsJson` is declared as nullable (`string?`)
- **User messages don't reference documents** - only assistant messages do
- When creating user messages without setting `ReferencedDocumentIds`, the backing field remained NULL, violating the database constraint

## Solution
This script makes the `ReferencedDocumentIds` column nullable to match the intended design:

1. **Changes column from NOT NULL to NULL** - Allows user messages to be saved without referenced documents
2. **Safe idempotent operation** - Can be run multiple times without issues
3. **Includes verification** - Confirms the change was successful

## Background
In chat conversations, there are two types of messages:
- **User messages** - Questions from users (no documents referenced)
- **Assistant messages** - AI responses that reference documents used to generate the answer

Example from `SaveConversationAsync`:
```csharp
// User message - no documents referenced
conversation.Messages.Add(new Message
{
    Role = "user",
    Content = query,
    // ReferencedDocumentIds not set → NULL in backing field
});

// Assistant message - documents referenced
conversation.Messages.Add(new Message
{
    Role = "assistant",
    Content = answer,
    ReferencedDocumentIds = documentIds  // List serialized to JSON
});
```

## How to Apply

### Option 1: Using SQL Server Management Studio (SSMS)
1. Open SQL Server Management Studio
2. Connect to your SQL Server instance
3. Open the file `011_MakeReferencedDocumentIdsNullable.sql`
4. Make sure you're connected to the correct database (`DocNDb`)
5. Execute the script (F5)

### Option 2: Using sqlcmd (Command Line)
```bash
# Local SQL Server
sqlcmd -S localhost -d DocNDb -i 011_MakeReferencedDocumentIdsNullable.sql

# SQL Server Express
sqlcmd -S .\SQLEXPRESS -d DocNDb -i 011_MakeReferencedDocumentIdsNullable.sql

# Remote SQL Server with authentication
sqlcmd -S your-server.database.windows.net -d DocNDb -U your-username -P your-password -i 011_MakeReferencedDocumentIdsNullable.sql
```

### Option 3: Using Entity Framework Migrations (Recommended for development)
If you're using EF Core migrations, the migration `20260103172002_MakeReferencedDocumentIdsNullable` handles this automatically:
```bash
# From the solution root directory
dotnet ef database update --project DocN.Data --startup-project DocN.Server --context ApplicationDbContext
```

## Verification
The script includes automatic verification that prints the result. You should see:
```
=========================================
AGGIORNAMENTO COMPLETATO CON SUCCESSO!
=========================================
La colonna ReferencedDocumentIds ora accetta NULL
I messaggi utente possono essere salvati senza errori
```

### Manual Verification
If you want to verify manually:

```sql
-- Check that ReferencedDocumentIds is nullable
SELECT 
    c.name AS ColumnName,
    c.is_nullable AS IsNullable,
    t.name AS DataType
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
JOIN sys.tables tbl ON c.object_id = tbl.object_id
WHERE tbl.name = 'Messages' 
AND c.name = 'ReferencedDocumentIds';
-- Should show: ReferencedDocumentIds | 1 | nvarchar (1 = nullable)
```

### Test by Creating a Conversation
After applying the fix, test by creating a conversation in the application:
1. Navigate to the Chat page
2. Ask a question (e.g., "What is this system about?")
3. The conversation should save successfully without SQL errors

## Impact
After applying this fix:
- ✅ User messages can be saved without errors
- ✅ `SaveConversationAsync` in `MultiProviderSemanticRAGService` works correctly
- ✅ Both user and assistant messages are stored properly
- ✅ Database schema matches Entity Framework model configuration
- ✅ No application code changes needed

## Related Files
- **Migration**: `DocN.Data/Migrations/20260103172002_MakeReferencedDocumentIdsNullable.cs`
- **Model**: `DocN.Data/Models/Conversation.cs` (Message class)
- **Context**: `DocN.Data/ApplicationDbContext.cs` (line 198-201)
- **Service**: `DocN.Data/Services/MultiProviderSemanticRAGService.cs` (SaveConversationAsync method)

## Rollback
If you need to rollback this change (not recommended as it will cause the original error):

```sql
-- WARNING: This will cause issues with user messages!
-- Only rollback if you're sure you want to revert

-- First, update any NULL values to empty string
UPDATE Messages 
SET ReferencedDocumentIds = '' 
WHERE ReferencedDocumentIds IS NULL;

-- Then change column back to NOT NULL
ALTER TABLE [dbo].[Messages]
ALTER COLUMN [ReferencedDocumentIds] NVARCHAR(MAX) NOT NULL;
```

## Notes
- This fix is **backward compatible** - existing data is not affected
- The column already contained JSON arrays like `[1,2,3]` or `[]` for empty lists
- NULL is now a valid value representing "no documents referenced"
- The Entity Framework model handles NULL gracefully, returning an empty list
