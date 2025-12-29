# Fix OwnerId Foreign Key Constraint

## Overview
This update script fixes a critical database save failure issue where documents couldn't be saved when the OwnerId references a user that doesn't exist in the AspNetUsers table.

## Problem
Documents were failing to save with the error:
```
❌ ⚠️ ERRORE CRITICO: Il salvataggio nel database è fallito durante la creazione.
```

The root cause was:
- OwnerId foreign key constraint was using `ON DELETE CASCADE`
- When authenticated users tried to upload documents but their user record didn't exist in AspNetUsers table, the FK constraint failed
- The constraint wasn't explicitly configured as nullable in Entity Framework

## Solution
This script makes three important changes:

1. **Makes OwnerId column nullable** - Allows documents to exist without an owner
2. **Changes FK constraint to SET NULL** - Safer than CASCADE delete (sets OwnerId to NULL when user is deleted instead of deleting all their documents)
3. **Ensures proper FK configuration** - Explicitly creates the constraint with correct settings

## How to Apply

### Option 1: Using SQL Server Management Studio (SSMS)
1. Open SQL Server Management Studio
2. Connect to your SQL Server instance
3. Open the file `005_FixOwnerIdForeignKeyConstraint.sql`
4. Make sure you're connected to the correct database (`DocNDb`)
5. Execute the script (F5)

### Option 2: Using sqlcmd (Command Line)
```bash
# Local SQL Server
sqlcmd -S localhost -d DocNDb -i 005_FixOwnerIdForeignKeyConstraint.sql

# SQL Server Express
sqlcmd -S .\SQLEXPRESS -d DocNDb -i 005_FixOwnerIdForeignKeyConstraint.sql

# Remote SQL Server with authentication
sqlcmd -S your-server.database.windows.net -d DocNDb -U your-username -P your-password -i 005_FixOwnerIdForeignKeyConstraint.sql
```

### Option 3: Using Entity Framework Migrations (Recommended for development)
If you're using EF Core migrations:
```bash
# From the solution root directory
dotnet ef database update --project DocN.Data --startup-project DocN.Server --context ApplicationDbContext
```

## Verification
After applying the script, verify the changes:

### Quick Verification (Recommended)
Run the verification script to check all aspects of the fix:
```bash
sqlcmd -S localhost -d DocNDb -i VerifyOwnerIdFix.sql
```

This script will check:
- ✅ OwnerId column is nullable
- ✅ FK constraint uses SET_NULL on delete
- ✅ No documents with invalid OwnerId references

### Manual Verification
If you prefer to check manually:

```sql
-- Check that OwnerId is nullable
SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Documents' AND COLUMN_NAME = 'OwnerId';
-- Should show: OwnerId | YES | nvarchar

-- Check the FK constraint
SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
    fk.delete_referential_action_desc AS DeleteAction
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.name = 'FK_Documents_Owner';
-- Should show: DELETE_ACTION = SET_NULL
```

## Impact
- ✅ Documents can be saved with null OwnerId (for unauthenticated uploads)
- ✅ Documents can be saved even if authenticated user doesn't exist in AspNetUsers
- ✅ User deletion no longer cascades to delete all their documents (safer)
- ✅ Existing data is preserved (script is non-destructive)

## Rollback
If you need to revert this change (not recommended):

```sql
-- Drop the new constraint
ALTER TABLE Documents DROP CONSTRAINT FK_Documents_Owner;

-- Recreate with CASCADE (original behavior)
ALTER TABLE Documents 
ADD CONSTRAINT FK_Documents_Owner 
FOREIGN KEY (OwnerId) 
REFERENCES AspNetUsers(Id) 
ON DELETE CASCADE;
```

## Related Changes
This fix is part of commit `6630553` which includes:
- Updated `ApplicationDbContext.cs` with `.IsRequired(false)` and `OnDelete(DeleteBehavior.SetNull)`
- EF Core migration: `20251229114000_MakeOwnerIdOptionalAndFixConstraint.cs`
- Enhanced error logging in `DocumentService.cs` and `Upload.razor`

## Questions?
If you encounter any issues applying this script, check:
1. Do you have appropriate permissions on the database?
2. Are there any documents with invalid OwnerId values? (Run: `SELECT COUNT(*) FROM Documents WHERE OwnerId IS NOT NULL AND OwnerId NOT IN (SELECT Id FROM AspNetUsers)`)
3. Is the database schema up to date with other migrations?
