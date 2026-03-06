using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EAlertService.Migrations
{
    /// <inheritdoc />
    public partial class UnifyBulkScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledFor",
                table: "BulkMessageBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$rmx02CfSkZ.3l8N0yItv7eKlOwjZpGl2Vqm/0vZuWw.Xq6dao9oWi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledFor",
                table: "BulkMessageBatches");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$6gqOtxceC79Zt9cRbJgK5ugvI1Ha5WGduVHTZVQMKJFoGUBS0fr/O");
        }
    }
}
