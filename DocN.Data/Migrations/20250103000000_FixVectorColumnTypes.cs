using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocN.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixVectorColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix Documents.EmbeddingVector column type from nvarchar(max) to varbinary(max)
            // This addresses the SqlException: type mismatch when saving binary data to nvarchar column
            migrationBuilder.AlterColumn<byte[]>(
                name: "EmbeddingVector",
                table: "Documents",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // Fix DocumentChunks.ChunkEmbedding column type from nvarchar(max) to varbinary(max)
            migrationBuilder.AlterColumn<byte[]>(
                name: "ChunkEmbedding",
                table: "DocumentChunks",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert Documents.EmbeddingVector column type back to nvarchar(max)
            migrationBuilder.AlterColumn<string>(
                name: "EmbeddingVector",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            // Revert DocumentChunks.ChunkEmbedding column type back to nvarchar(max)
            migrationBuilder.AlterColumn<string>(
                name: "ChunkEmbedding",
                table: "DocumentChunks",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);
        }
    }
}
