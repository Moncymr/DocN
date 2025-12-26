using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocN.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixCategoryColumnLengths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alter ActualCategory column from nvarchar(max) to nvarchar(200)
            migrationBuilder.AlterColumn<string>(
                name: "ActualCategory",
                table: "Documents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // Alter SuggestedCategory column from nvarchar(max) to nvarchar(200)
            migrationBuilder.AlterColumn<string>(
                name: "SuggestedCategory",
                table: "Documents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert ActualCategory column back to nvarchar(max)
            migrationBuilder.AlterColumn<string>(
                name: "ActualCategory",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            // Revert SuggestedCategory column back to nvarchar(max)
            migrationBuilder.AlterColumn<string>(
                name: "SuggestedCategory",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
