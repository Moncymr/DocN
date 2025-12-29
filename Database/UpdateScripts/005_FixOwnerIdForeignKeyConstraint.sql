-- ================================================
-- Update Script: Fix OwnerId Foreign Key Constraint
-- Database: DocNDb
-- Date: 2024-12-29
-- Description: Makes OwnerId nullable and changes foreign key to ON DELETE SET NULL
-- ================================================

USE DocNDb;
GO

PRINT '';
PRINT '================================================';
PRINT '🔧 Fixing OwnerId Foreign Key Constraint';
PRINT '================================================';
PRINT '';

-- Step 1: Drop existing foreign key constraint if it exists
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys 
    WHERE name = 'FK_Documents_Owner' 
    AND parent_object_id = OBJECT_ID('Documents')
)
BEGIN
    PRINT '  🗑️  Dropping existing FK_Documents_Owner constraint...';
    ALTER TABLE Documents DROP CONSTRAINT FK_Documents_Owner;
    PRINT '  ✓ Constraint dropped';
END
ELSE IF EXISTS (
    SELECT 1 FROM sys.foreign_keys 
    WHERE name = 'FK_Documents_AspNetUsers_OwnerId' 
    AND parent_object_id = OBJECT_ID('Documents')
)
BEGIN
    PRINT '  🗑️  Dropping existing FK_Documents_AspNetUsers_OwnerId constraint...';
    ALTER TABLE Documents DROP CONSTRAINT FK_Documents_AspNetUsers_OwnerId;
    PRINT '  ✓ Constraint dropped';
END
ELSE
BEGIN
    PRINT '  ℹ️  No existing foreign key constraint found';
END
GO

-- Step 2: Ensure OwnerId column is nullable
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Documents' 
    AND COLUMN_NAME = 'OwnerId' 
    AND IS_NULLABLE = 'NO'
)
BEGIN
    PRINT '  🔄 Making OwnerId column nullable...';
    ALTER TABLE Documents ALTER COLUMN OwnerId NVARCHAR(450) NULL;
    PRINT '  ✓ OwnerId is now nullable';
END
ELSE
BEGIN
    PRINT '  ✓ OwnerId is already nullable';
END
GO

-- Step 3: Re-create foreign key constraint with SET NULL on delete
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys 
    WHERE name = 'FK_Documents_Owner' 
    AND parent_object_id = OBJECT_ID('Documents')
)
BEGIN
    PRINT '  ➕ Creating new FK_Documents_Owner constraint with ON DELETE SET NULL...';
    ALTER TABLE Documents 
    ADD CONSTRAINT FK_Documents_Owner 
    FOREIGN KEY (OwnerId) 
    REFERENCES AspNetUsers(Id) 
    ON DELETE SET NULL;
    PRINT '  ✓ New constraint created successfully';
END
ELSE
BEGIN
    PRINT '  ✓ Constraint already exists';
END
GO

PRINT '';
PRINT '✅ OwnerId Foreign Key Constraint fixed successfully!';
PRINT '';
PRINT 'Changes applied:';
PRINT '  • OwnerId column is now nullable (allows documents without owner)';
PRINT '  • Foreign key constraint uses ON DELETE SET NULL (safer than CASCADE)';
PRINT '  • Documents can now be created by unauthenticated users or users not in DB';
PRINT '';
