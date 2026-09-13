using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Migrations
{
    [DbContext(typeof(StockFlowDbContext))]
    [Migration("20260913093000_AddInventorySettings")]
    public partial class AddInventorySettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_products_non_negative_stock",
                schema: "master",
                table: "products_set");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_values",
                schema: "inventory",
                table: "stock_movements");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_non_negative_reorder_level",
                schema: "master",
                table: "products_set",
                sql: "reorder_level >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_values",
                schema: "inventory",
                table: "stock_movements",
                sql: "quantity > 0");

            migrationBuilder.CreateTable(
                name: "inventory_settings",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_reorder_level = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    default_unit = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    allow_negative_stock = table.Column<bool>(type: "boolean", nullable: false),
                    global_low_stock_threshold = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_settings", x => x.id);
                    table.CheckConstraint(
                        "ck_inventory_settings_non_negative_thresholds",
                        "default_reorder_level >= 0 AND global_low_stock_threshold >= 0");
                });

            migrationBuilder.Sql("""
                INSERT INTO inventory.inventory_settings
                    (id, default_reorder_level, default_unit, allow_negative_stock,
                     global_low_stock_threshold, created_at)
                VALUES
                    ('a13263ef-4e59-4c6e-8a9d-94652b5b2141', 5, 'pcs', FALSE, 0, NOW())
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inventory_settings",
                schema: "inventory");

            migrationBuilder.DropCheckConstraint(
                name: "ck_products_non_negative_reorder_level",
                schema: "master",
                table: "products_set");

            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_movements_values",
                schema: "inventory",
                table: "stock_movements");

            migrationBuilder.AddCheckConstraint(
                name: "ck_products_non_negative_stock",
                schema: "master",
                table: "products_set",
                sql: "stock_on_hand >= 0 AND reorder_level >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_movements_values",
                schema: "inventory",
                table: "stock_movements",
                sql: "quantity > 0 AND balance_after >= 0");
        }
    }
}
