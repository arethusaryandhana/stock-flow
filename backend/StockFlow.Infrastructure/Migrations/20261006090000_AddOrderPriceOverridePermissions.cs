using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StockFlow.Infrastructure.Migrations;

[DbContext(typeof(StockFlowDbContext))]
[Migration("20261006090000_AddOrderPriceOverridePermissions")]
public sealed class AddOrderPriceOverridePermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO identity.permissions (code, "group", name, kind)
            VALUES
                ('action.purchasing.price-override', 'operations', 'Override harga purchase order', 1),
                ('action.sales.price-override', 'operations', 'Override harga sales order', 1)
            ON CONFLICT (code) DO NOTHING;

            INSERT INTO identity.role_permissions (role_id, permission_code)
            SELECT id, code
            FROM identity.roles
            CROSS JOIN (VALUES
                ('action.purchasing.price-override'),
                ('action.sales.price-override')) AS price_permissions(code)
            WHERE identity.roles.name = 'Admin'
            ON CONFLICT (role_id, permission_code) DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM identity.role_permissions
            WHERE permission_code IN (
                'action.purchasing.price-override',
                'action.sales.price-override');

            DELETE FROM identity.permissions
            WHERE code IN (
                'action.purchasing.price-override',
                'action.sales.price-override');
            """);
    }
}
