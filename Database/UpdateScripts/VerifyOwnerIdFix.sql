-- ================================================
-- Verification Script: Check OwnerId Foreign Key Fix
-- Database: DocNDb
-- Description: Verifies that the OwnerId fix has been applied correctly
-- ================================================

USE DocNDb;
GO

PRINT '';
PRINT '================================================';
PRINT '🔍 Verifying OwnerId Foreign Key Fix';
PRINT '================================================';
PRINT '';

-- Check 1: Verify OwnerId is nullable
PRINT '✓ Check 1: Verifying OwnerId column is nullable...';
DECLARE @IsNullable NVARCHAR(3);
SELECT @IsNullable = IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Documents' AND COLUMN_NAME = 'OwnerId';

IF @IsNullable = 'YES'
BEGIN
    PRINT '  ✅ SUCCESS: OwnerId column is nullable';
END
ELSE
BEGIN
    PRINT '  ❌ FAILED: OwnerId column is NOT nullable';
    PRINT '  ⚠️  ACTION REQUIRED: Run migration script 005_FixOwnerIdForeignKeyConstraint.sql';
END
PRINT '';

-- Check 2: Verify FK constraint behavior
PRINT '✓ Check 2: Verifying FK constraint delete behavior...';
DECLARE @DeleteAction NVARCHAR(60);
SELECT @DeleteAction = fk.delete_referential_action_desc
FROM sys.foreign_keys AS fk
WHERE (fk.name = 'FK_Documents_Owner' OR fk.name LIKE '%OwnerId%')
  AND fk.parent_object_id = OBJECT_ID('Documents');

IF @DeleteAction = 'SET_NULL'
BEGIN
    PRINT '  ✅ SUCCESS: FK constraint uses SET_NULL on delete';
END
ELSE IF @DeleteAction = 'CASCADE'
BEGIN
    PRINT '  ❌ FAILED: FK constraint still uses CASCADE on delete';
    PRINT '  ⚠️  ACTION REQUIRED: Run migration script 005_FixOwnerIdForeignKeyConstraint.sql';
END
ELSE IF @DeleteAction IS NULL
BEGIN
    PRINT '  ⚠️  WARNING: FK constraint not found';
    PRINT '  ⚠️  ACTION REQUIRED: Run migration script 005_FixOwnerIdForeignKeyConstraint.sql';
END
ELSE
BEGIN
    PRINT '  ⚠️  WARNING: FK constraint has unexpected behavior: ' + @DeleteAction;
END
PRINT '';

-- Check 3: Count documents with invalid OwnerId
PRINT '✓ Check 3: Checking for documents with invalid OwnerId...';
DECLARE @InvalidCount INT;
SELECT @InvalidCount = COUNT(*)
FROM Documents 
WHERE OwnerId IS NOT NULL 
  AND OwnerId NOT IN (SELECT Id FROM AspNetUsers);

IF @InvalidCount = 0
BEGIN
    PRINT '  ✅ SUCCESS: No documents with invalid OwnerId found';
END
ELSE
BEGIN
    PRINT '  ⚠️  WARNING: Found ' + CAST(@InvalidCount AS NVARCHAR(10)) + ' document(s) with invalid OwnerId';
    PRINT '  ℹ️  These documents reference users that don''t exist in AspNetUsers table';
    PRINT '  ℹ️  Consider setting OwnerId to NULL for these documents:';
    PRINT '     UPDATE Documents SET OwnerId = NULL WHERE OwnerId NOT IN (SELECT Id FROM AspNetUsers)';
END
PRINT '';

-- Summary
PRINT '================================================';
PRINT '📊 Verification Summary';
PRINT '================================================';
IF @IsNullable = 'YES' AND @DeleteAction = 'SET_NULL' AND @InvalidCount = 0
BEGIN
    PRINT '✅ ALL CHECKS PASSED!';
    PRINT '   The OwnerId fix has been applied correctly.';
    PRINT '   Your application should no longer experience database save errors.';
END
ELSE
BEGIN
    PRINT '⚠️  SOME CHECKS FAILED!';
    PRINT '   Please review the results above and take appropriate action.';
    PRINT '';
    PRINT '   To fix the issues, run:';
    PRINT '   • Migration script: Database/UpdateScripts/005_FixOwnerIdForeignKeyConstraint.sql';
    PRINT '   • Or restart your application (automatic migration will run)';
    PRINT '   • Or use: dotnet ef database update --project DocN.Data --startup-project DocN.Server';
END
PRINT '';
