-- Migration: Update ReferencedDocumentIds column to allow NULL values
-- This is needed after refactoring to use NotMapped pattern with backing field
-- Date: 2025-12-30

USE [GestDoc]
GO

-- Check if the column exists and if it's NOT NULL
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Messages' 
    AND COLUMN_NAME = 'ReferencedDocumentIds'
    AND IS_NULLABLE = 'NO'
)
BEGIN
    PRINT 'Updating ReferencedDocumentIds column to allow NULL values...'
    
    -- Alter the column to allow NULL
    ALTER TABLE [dbo].[Messages]
    ALTER COLUMN [ReferencedDocumentIds] nvarchar(max) NULL
    
    PRINT 'Column updated successfully!'
END
ELSE
BEGIN
    PRINT 'Column is already nullable or does not exist. No action needed.'
END
GO
