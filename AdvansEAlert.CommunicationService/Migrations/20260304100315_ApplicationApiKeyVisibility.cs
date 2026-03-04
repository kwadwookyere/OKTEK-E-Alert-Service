using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdvansEAlert.CommunicationService.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationApiKeyVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKeyEncrypted",
                table: "Applications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$.BF0s6bfmHFuI1rlVgfvhupbXaoFxf/bn5VVst3Acrp9w5yZHw22m");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKeyEncrypted",
                table: "Applications");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$uNY0QW2tVQHovJzI7FjJVeY8/JEswqNsg/hGixrnKPxjz3HbG3tbC");
        }
    }
}
