using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaCore.Api.Payments.Migrations
{
    /// <summary>
    /// Keys <c>outbox_consumers</c> by the message row (<c>MessageId</c>, the outbox's bigint key) instead of the event
    /// id, with a cascading foreign key, so deleting a processed message deletes its handlers' records. Existing records
    /// are carried over through their event id; a record without a message is dropped.
    /// </summary>
    public partial class OutboxConsumersByMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MessageId",
                schema: "payments",
                table: "outbox_consumers",
                type: "bigint",
                nullable: true);

            // EXEC: compiled when it runs, after the column exists, also when a script runs the migration as one batch.
            migrationBuilder.Sql(
                "EXEC(N'UPDATE c SET c.MessageId = m.Id FROM [payments].[outbox_consumers] c "
                + "JOIN [payments].[outbox_messages] m ON m.EventId = c.EventId; "
                + "DELETE FROM [payments].[outbox_consumers] WHERE MessageId IS NULL;');");

            migrationBuilder.DropPrimaryKey(
                name: "PK_outbox_consumers",
                schema: "payments",
                table: "outbox_consumers");

            migrationBuilder.DropColumn(
                name: "EventId",
                schema: "payments",
                table: "outbox_consumers");

            migrationBuilder.AlterColumn<long>(
                name: "MessageId",
                schema: "payments",
                table: "outbox_consumers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_outbox_consumers",
                schema: "payments",
                table: "outbox_consumers",
                columns: new[] { "MessageId", "Handler" });

            migrationBuilder.AddForeignKey(
                name: "FK_outbox_consumers_outbox_messages_MessageId",
                schema: "payments",
                table: "outbox_consumers",
                column: "MessageId",
                principalSchema: "payments",
                principalTable: "outbox_messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EventId",
                schema: "payments",
                table: "outbox_consumers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                "EXEC(N'UPDATE c SET c.EventId = m.EventId FROM [payments].[outbox_consumers] c "
                + "JOIN [payments].[outbox_messages] m ON m.Id = c.MessageId;');");

            migrationBuilder.DropForeignKey(
                name: "FK_outbox_consumers_outbox_messages_MessageId",
                schema: "payments",
                table: "outbox_consumers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_outbox_consumers",
                schema: "payments",
                table: "outbox_consumers");

            migrationBuilder.DropColumn(
                name: "MessageId",
                schema: "payments",
                table: "outbox_consumers");

            migrationBuilder.AlterColumn<Guid>(
                name: "EventId",
                schema: "payments",
                table: "outbox_consumers",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_outbox_consumers",
                schema: "payments",
                table: "outbox_consumers",
                columns: new[] { "EventId", "Handler" });
        }
    }
}
