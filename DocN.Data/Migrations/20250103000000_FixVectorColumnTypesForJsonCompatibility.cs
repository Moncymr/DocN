using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocN.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixVectorColumnTypesForJsonCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration ensures columns can accept JSON format data for VECTOR compatibility
            // If columns are already VECTOR(1536), no changes are needed - they accept JSON format
            // If columns are varbinary(max), they need to be changed to nvarchar(max) to accept JSON
            
            // Note: This migration is safe to run even if columns are already VECTOR(1536)
            // SQL Server will accept JSON string data in VECTOR columns
            
            // Only alter if the column is NOT already a VECTOR type
            // Since we can't easily check column type in migration, we use a conditional approach
            migrationBuilder.Sql(@"
                -- Check and alter Documents.EmbeddingVector if needed
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'Documents' 
                    AND COLUMN_NAME = 'EmbeddingVector' 
                    AND DATA_TYPE = 'varbinary'
                )
                BEGIN
                    ALTER TABLE Documents ALTER COLUMN EmbeddingVector NVARCHAR(MAX) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                -- Check and alter DocumentChunks.ChunkEmbedding if needed
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'DocumentChunks' 
                    AND COLUMN_NAME = 'ChunkEmbedding' 
                    AND DATA_TYPE = 'varbinary'
                )
                BEGIN
                    ALTER TABLE DocumentChunks ALTER COLUMN ChunkEmbedding NVARCHAR(MAX) NULL;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // WARNING: Rolling back this migration will cause data loss
            // JSON string data cannot be safely converted back to binary format
            // Existing vector data will be lost during the conversion
            
            // Revert to varbinary(max) - DATA WILL BE LOST
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'Documents' 
                    AND COLUMN_NAME = 'EmbeddingVector' 
                    AND DATA_TYPE = 'nvarchar'
                )
                BEGIN
                    -- Clear existing data to avoid conversion errors
                    UPDATE Documents SET EmbeddingVector = NULL WHERE EmbeddingVector IS NOT NULL;
                    ALTER TABLE Documents ALTER COLUMN EmbeddingVector VARBINARY(MAX) NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = 'DocumentChunks' 
                    AND COLUMN_NAME = 'ChunkEmbedding' 
                    AND DATA_TYPE = 'nvarchar'
                )
                BEGIN
                    -- Clear existing data to avoid conversion errors
                    UPDATE DocumentChunks SET ChunkEmbedding = NULL WHERE ChunkEmbedding IS NOT NULL;
                    ALTER TABLE DocumentChunks ALTER COLUMN ChunkEmbedding VARBINARY(MAX) NULL;
                END
            ");
        }
    }
}
