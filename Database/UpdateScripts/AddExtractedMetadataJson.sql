-- Migration script to add ExtractedMetadataJson field to Documents table
-- Date: 2024-12-28
-- Description: Adds a new column to store AI-extracted metadata in JSON format
-- This enables extraction of structured data like invoice numbers, dates, authors, etc.

-- Add the ExtractedMetadataJson column to Documents table
ALTER TABLE Documents 
ADD COLUMN ExtractedMetadataJson TEXT NULL;

-- Add comment to explain the column purpose
COMMENT ON COLUMN Documents.ExtractedMetadataJson IS 
'JSON object containing AI-extracted metadata from the document. 
Examples: invoice_number, invoice_date, author, creation_date, contract_number, etc.
Format: {"field_name": "value", ...}';

-- Create an index on the JSONB column for better query performance (PostgreSQL specific)
-- If using SQLite or other databases, you may need to adjust or skip this
-- CREATE INDEX IF NOT EXISTS idx_documents_extracted_metadata 
-- ON Documents USING GIN (CAST(ExtractedMetadataJson AS jsonb));

-- Display confirmation
SELECT 'Migration completed: ExtractedMetadataJson column added to Documents table' AS Status;
