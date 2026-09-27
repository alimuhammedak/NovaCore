using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaCore.Modules.Payments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OutboxDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                schema: "payments",
                table: "outbox_messages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                schema: "payments",
                table: "outbox_messages",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessedOnUtc",
                schema: "payments",
                table: "outbox_messages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "outbox_consumers",
                schema: "payments",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Handler = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_consumers", x => new { x.EventId, x.Handler });
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOnUtc_Id",
                schema: "payments",
                table: "outbox_messages",
                columns: new[] { "ProcessedOnUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_consumers",
                schema: "payments");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedOnUtc_Id",
                schema: "payments",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "Attempts",
                schema: "payments",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "Error",
                schema: "payments",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "ProcessedOnUtc",
                schema: "payments",
                table: "outbox_messages");
        }
    }
}
