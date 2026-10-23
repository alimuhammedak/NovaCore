using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovaCore.Api.Payments.Migrations
{
    /// <inheritdoc />
    public partial class PaymentOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                schema: "payments",
                table: "payments",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_payments_OwnerId_CreatedAtUtc",
                schema: "payments",
                table: "payments",
                columns: new[] { "OwnerId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_OwnerId_CreatedAtUtc",
                schema: "payments",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                schema: "payments",
                table: "payments");
        }
    }
}
