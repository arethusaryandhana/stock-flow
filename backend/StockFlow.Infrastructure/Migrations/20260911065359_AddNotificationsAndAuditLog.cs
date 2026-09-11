using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_user_id",
                schema: "identity",
                table: "notifications");

            migrationBuilder.AddColumn<string>(
                name: "deduplication_key",
                schema: "identity",
                table: "notifications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "link",
                schema: "identity",
                table: "notifications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "read_at",
                schema: "identity",
                table: "notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    summary = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    changes = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_actor_id",
                        column: x => x.actor_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_deduplication_key",
                schema: "identity",
                table: "notifications",
                column: "deduplication_key",
                unique: true,
                filter: "deduplication_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id_is_read_created_at",
                schema: "identity",
                table: "notifications",
                columns: new[] { "user_id", "is_read", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_actor_id_created_at",
                schema: "identity",
                table: "audit_logs",
                columns: new[] { "actor_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at",
                schema: "identity",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_type_entity_id",
                schema: "identity",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_notifications_deduplication_key",
                schema: "identity",
                table: "notifications");

            migrationBuilder.DropIndex(
                name: "IX_notifications_user_id_is_read_created_at",
                schema: "identity",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "deduplication_key",
                schema: "identity",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "link",
                schema: "identity",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "read_at",
                schema: "identity",
                table: "notifications");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                schema: "identity",
                table: "notifications",
                column: "user_id");
        }
    }
}
