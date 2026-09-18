using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "identity",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_system",
                schema: "identity",
                table: "roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "identity",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    group = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "identity",
                columns: table => new
                {
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_code = table.Column<string>(type: "character varying(96)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => new { x.role_id, x.permission_code });
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_code",
                        column: x => x.permission_code,
                        principalSchema: "identity",
                        principalTable: "permissions",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_code",
                schema: "identity",
                table: "role_permissions",
                column: "permission_code");

            migrationBuilder.Sql("""
                UPDATE identity.roles SET is_system = TRUE
                WHERE name IN ('Admin', 'Manager', 'Staff');

                INSERT INTO identity.permissions (code, "group", name, kind) VALUES
                    ('menu.dashboard', 'workspace', 'Dashboard', 0),
                    ('menu.master.categories', 'administration', 'Kategori', 0),
                    ('menu.master.products', 'administration', 'Master produk', 0),
                    ('menu.master.suppliers', 'administration', 'Master supplier', 0),
                    ('menu.master.customers', 'administration', 'Master customer', 0),
                    ('menu.audit', 'administration', 'Riwayat audit', 0),
                    ('menu.users', 'access', 'Pengguna', 0),
                    ('menu.roles', 'access', 'Role', 0),
                    ('menu.access', 'access', 'Akses menu', 0),
                    ('menu.products', 'inventory', 'Produk dan stok', 0),
                    ('menu.movements', 'inventory', 'Pergerakan stok', 0),
                    ('menu.adjustments', 'inventory', 'Penyesuaian stok', 0),
                    ('menu.purchase-orders', 'operations', 'Purchase order', 0),
                    ('menu.sales-orders', 'operations', 'Sales order', 0),
                    ('menu.receiving', 'operations', 'Penerimaan barang', 0),
                    ('menu.suppliers', 'operations', 'Supplier', 0),
                    ('menu.reports', 'insight', 'Laporan', 0),
                    ('menu.settings', 'insight', 'Pengaturan', 0),
                    ('action.categories.manage', 'administration', 'Kelola kategori', 1),
                    ('action.products.manage', 'administration', 'Kelola master produk', 1),
                    ('action.suppliers.manage', 'administration', 'Kelola supplier', 1),
                    ('action.customers.manage', 'administration', 'Kelola customer', 1),
                    ('action.audit.view', 'administration', 'Lihat riwayat audit', 1),
                    ('action.users.manage', 'access', 'Kelola pengguna', 1),
                    ('action.roles.manage', 'access', 'Kelola role', 1),
                    ('action.access.manage', 'access', 'Kelola izin role', 1),
                    ('action.inventory.adjust', 'inventory', 'Buat penyesuaian stok', 1),
                    ('action.inventory.settings', 'inventory', 'Kelola pengaturan inventori', 1),
                    ('action.purchasing.manage', 'operations', 'Kelola purchase order', 1),
                    ('action.receiving.manage', 'operations', 'Catat penerimaan barang', 1),
                    ('action.sales.manage', 'operations', 'Kelola sales order', 1),
                    ('action.reports.export', 'insight', 'Ekspor laporan', 1),
                    ('action.company.manage', 'insight', 'Kelola profil perusahaan', 1);

                INSERT INTO identity.role_permissions (role_id, permission_code)
                SELECT role.id, permission.code
                FROM identity.roles role CROSS JOIN identity.permissions permission
                WHERE role.name = 'Admin';

                INSERT INTO identity.role_permissions (role_id, permission_code)
                SELECT role.id, permission.code
                FROM identity.roles role CROSS JOIN identity.permissions permission
                WHERE role.name = 'Manager' AND permission.code IN (
                    'menu.dashboard', 'menu.products', 'menu.movements', 'menu.adjustments',
                    'menu.purchase-orders', 'menu.sales-orders', 'menu.receiving',
                    'menu.suppliers', 'menu.reports', 'menu.settings',
                    'action.inventory.adjust', 'action.purchasing.manage',
                    'action.receiving.manage', 'action.sales.manage', 'action.reports.export');

                INSERT INTO identity.role_permissions (role_id, permission_code)
                SELECT role.id, permission.code
                FROM identity.roles role CROSS JOIN identity.permissions permission
                WHERE role.name = 'Staff' AND permission.code IN (
                    'menu.dashboard', 'menu.products', 'menu.movements', 'menu.adjustments',
                    'menu.purchase-orders', 'menu.sales-orders', 'menu.receiving',
                    'menu.suppliers', 'menu.reports', 'menu.settings', 'action.reports.export');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "is_system",
                schema: "identity",
                table: "roles");

        }
    }
}
