using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedReferenceRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO identity.roles (id, name, created_at)
                SELECT '11111111-1111-1111-1111-111111111111'::uuid, 'Admin', NOW()
                WHERE NOT EXISTS (SELECT 1 FROM identity.roles WHERE name = 'Admin');

                INSERT INTO identity.roles (id, name, created_at)
                SELECT '22222222-2222-2222-2222-222222222222'::uuid, 'Manager', NOW()
                WHERE NOT EXISTS (SELECT 1 FROM identity.roles WHERE name = 'Manager');

                INSERT INTO identity.roles (id, name, created_at)
                SELECT '33333333-3333-3333-3333-333333333333'::uuid, 'Staff', NOW()
                WHERE NOT EXISTS (SELECT 1 FROM identity.roles WHERE name = 'Staff');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                schema: "identity",
                table: "roles",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_roles_name",
                schema: "identity",
                table: "roles");
        }
    }
}
