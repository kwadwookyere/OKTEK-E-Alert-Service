using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EAlertService.Migrations
{
    /// <inheritdoc />
    public partial class LinkScheduledToBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchId",
                table: "ScheduledMessages",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$..O5/wToUwhftqpPsDT7YeW0KeY9kVJnbcewtzU4qnuZR7bfrlGje");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledMessages_BatchId",
                table: "ScheduledMessages",
                column: "BatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledMessages_BulkMessageBatches_BatchId",
                table: "ScheduledMessages",
                column: "BatchId",
                principalTable: "BulkMessageBatches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledMessages_BulkMessageBatches_BatchId",
                table: "ScheduledMessages");

            migrationBuilder.DropIndex(
                name: "IX_ScheduledMessages_BatchId",
                table: "ScheduledMessages");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "ScheduledMessages");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$rmx02CfSkZ.3l8N0yItv7eKlOwjZpGl2Vqm/0vZuWw.Xq6dao9oWi");
        }
    }
}
