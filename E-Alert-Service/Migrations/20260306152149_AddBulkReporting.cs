using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EAlertService.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "SmsLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "EmailLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BulkMessageBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessagePreview = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalRecipients = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    PendingCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkMessageBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkMessageBatches_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$6gqOtxceC79Zt9cRbJgK5ugvI1Ha5WGduVHTZVQMKJFoGUBS0fr/O");

            migrationBuilder.CreateIndex(
                name: "IX_SmsLogs_BatchId",
                table: "SmsLogs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_BatchId",
                table: "EmailLogs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BulkMessageBatches_ApplicationId",
                table: "BulkMessageBatches",
                column: "ApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailLogs_BulkMessageBatches_BatchId",
                table: "EmailLogs",
                column: "BatchId",
                principalTable: "BulkMessageBatches",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SmsLogs_BulkMessageBatches_BatchId",
                table: "SmsLogs",
                column: "BatchId",
                principalTable: "BulkMessageBatches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailLogs_BulkMessageBatches_BatchId",
                table: "EmailLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_SmsLogs_BulkMessageBatches_BatchId",
                table: "SmsLogs");

            migrationBuilder.DropTable(
                name: "BulkMessageBatches");

            migrationBuilder.DropIndex(
                name: "IX_SmsLogs_BatchId",
                table: "SmsLogs");

            migrationBuilder.DropIndex(
                name: "IX_EmailLogs_BatchId",
                table: "EmailLogs");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "SmsLogs");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "EmailLogs");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$YqmGpyZ7/G1x4k1kIdz8WuVKB1OK0ulO4SLEJBLcdx/4D6K0dFzvu");
        }
    }
}
