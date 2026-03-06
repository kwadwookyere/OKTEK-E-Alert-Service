using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EAlertService.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseResilience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptedAt",
                table: "SmsLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "SmsLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptedAt",
                table: "EmailLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "EmailLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$nRd9XjgHxdWN8g6Jtr.hz.H3cj2KKg.wCZIgbP9FupeIThXDA4zH2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAttemptedAt",
                table: "SmsLogs");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "SmsLogs");

            migrationBuilder.DropColumn(
                name: "LastAttemptedAt",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "EmailLogs");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$..O5/wToUwhftqpPsDT7YeW0KeY9kVJnbcewtzU4qnuZR7bfrlGje");
        }
    }
}
