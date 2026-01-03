using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocN.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeReferencedDocumentIdsNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make ReferencedDocumentIds column nullable for existing databases
            // that were created with the incorrect NOT NULL constraint
            migrationBuilder.AlterColumn<string>(
                name: "ReferencedDocumentIds",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to NOT NULL (though this would cause issues with existing data)
            migrationBuilder.AlterColumn<string>(
                name: "ReferencedDocumentIds",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                defaultValue: "");
        }
    }
}
