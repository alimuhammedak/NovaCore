using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaCore.Api.Tests.PostgreSql.Catalog.Migrations
{
    /// <inheritdoc />
    public partial class OutboxTenantAndTrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "catalog",
                table: "outbox_messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceParent",
                schema: "catalog",
                table: "outbox_messages",
                type: "character varying(55)",
                maxLength: 55,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "catalog",
                table: "outbox_messages");

            migrationBuilder.DropColumn(
                name: "TraceParent",
                schema: "catalog",
                table: "outbox_messages");
        }
    }
}
