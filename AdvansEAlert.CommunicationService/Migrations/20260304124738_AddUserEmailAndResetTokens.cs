using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdvansEAlert.CommunicationService.Migrations
{
    /// <inheritdoc />
    public partial class AddUserEmailAndResetTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResetToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Email", "PasswordHash", "ResetToken", "ResetTokenExpiry" },
                values: new object[] { "admin@advansio.com", "$2a$11$Tvju8gDiPLEKM6GLZsa/UuKBnJl..mtPoqJLPq/dQqtebzIpNeoEi", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ResetTokenExpiry",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$.BF0s6bfmHFuI1rlVgfvhupbXaoFxf/bn5VVst3Acrp9w5yZHw22m");
        }
    }
}
