using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StockFlow.Infrastructure.Migrations;

[DbContext(typeof(StockFlowDbContext))]
[Migration("20261005090000_AddSessionTokenRevocation")]
public sealed class AddSessionTokenRevocation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "revoked_session_tokens",
            schema: "identity",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                token_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_revoked_session_tokens", x => x.id));

        migrationBuilder.CreateIndex(
            name: "IX_revoked_session_tokens_expires_at",
            schema: "identity",
            table: "revoked_session_tokens",
            column: "expires_at");

        migrationBuilder.CreateIndex(
            name: "IX_revoked_session_tokens_token_id",
            schema: "identity",
            table: "revoked_session_tokens",
            column: "token_id",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "revoked_session_tokens", schema: "identity");
}
