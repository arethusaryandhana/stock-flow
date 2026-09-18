using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StockFlow.Infrastructure.Migrations;

[DbContext(typeof(StockFlowDbContext))]
[Migration("20260918090000_AddAccessHistoryPermission")]
public sealed class AddAccessHistoryPermission : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO identity.permissions (code, "group", name, kind)
            VALUES ('menu.access-history', 'access', 'Riwayat akses', 0)
            ON CONFLICT (code) DO NOTHING;

            INSERT INTO identity.role_permissions (role_id, permission_code)
            SELECT id, 'menu.access-history' FROM identity.roles WHERE name = 'Admin'
            ON CONFLICT (role_id, permission_code) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM identity.role_permissions WHERE permission_code = 'menu.access-history';
            DELETE FROM identity.permissions WHERE code = 'menu.access-history';
            """);
    }
}
