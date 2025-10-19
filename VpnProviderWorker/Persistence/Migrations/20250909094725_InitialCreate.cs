using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VpnProviderWorker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inbox",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    content = table.Column<string>(type: "TEXT", nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox", x => x.event_id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    content = table.Column<string>(type: "TEXT", nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    error = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.event_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_unprocessed",
                table: "inbox",
                columns: new[] { "occurred_on_utc", "processed_on_utc", "event_id", "type" },
                filter: "processed_on_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_unprocessed",
                table: "outbox",
                column: "occurred_on_utc",
                filter: "processed_on_utc IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inbox");

            migrationBuilder.DropTable(
                name: "outbox");
        }
    }
}
