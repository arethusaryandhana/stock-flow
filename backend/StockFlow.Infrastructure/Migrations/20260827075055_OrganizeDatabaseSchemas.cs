using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrganizeDatabaseSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "master");

            migrationBuilder.EnsureSchema(
                name: "purchasing");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.EnsureSchema(
                name: "sales");

            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.RenameTable(
                name: "users_set",
                schema: "stockflow",
                newName: "users_set",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "suppliers_set",
                schema: "stockflow",
                newName: "suppliers_set",
                newSchema: "master");

            migrationBuilder.RenameTable(
                name: "stock_movements",
                schema: "stockflow",
                newName: "stock_movements",
                newSchema: "inventory");

            migrationBuilder.RenameTable(
                name: "stock_adjustments",
                schema: "stockflow",
                newName: "stock_adjustments",
                newSchema: "inventory");

            migrationBuilder.RenameTable(
                name: "sales_orders",
                schema: "stockflow",
                newName: "sales_orders",
                newSchema: "sales");

            migrationBuilder.RenameTable(
                name: "sales_order_items",
                schema: "stockflow",
                newName: "sales_order_items",
                newSchema: "sales");

            migrationBuilder.RenameTable(
                name: "roles",
                schema: "stockflow",
                newName: "roles",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "report_export_jobs",
                schema: "stockflow",
                newName: "report_export_jobs",
                newSchema: "reporting");

            migrationBuilder.RenameTable(
                name: "purchase_orders",
                schema: "stockflow",
                newName: "purchase_orders",
                newSchema: "purchasing");

            migrationBuilder.RenameTable(
                name: "purchase_order_items",
                schema: "stockflow",
                newName: "purchase_order_items",
                newSchema: "purchasing");

            migrationBuilder.RenameTable(
                name: "products_set",
                schema: "stockflow",
                newName: "products_set",
                newSchema: "master");

            migrationBuilder.RenameTable(
                name: "password_reset_tokens",
                schema: "stockflow",
                newName: "password_reset_tokens",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "notifications",
                schema: "stockflow",
                newName: "notifications",
                newSchema: "identity");

            migrationBuilder.RenameTable(
                name: "goods_receipts",
                schema: "stockflow",
                newName: "goods_receipts",
                newSchema: "purchasing");

            migrationBuilder.RenameTable(
                name: "goods_receipt_items",
                schema: "stockflow",
                newName: "goods_receipt_items",
                newSchema: "purchasing");

            migrationBuilder.RenameTable(
                name: "customers_set",
                schema: "stockflow",
                newName: "customers_set",
                newSchema: "master");

            migrationBuilder.RenameTable(
                name: "categories_set",
                schema: "stockflow",
                newName: "categories_set",
                newSchema: "master");

            // The legacy schema is no longer used. Do not cascade so unknown
            // objects in it are preserved and make the migration fail safely.
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS stockflow;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "stockflow");

            migrationBuilder.RenameTable(
                name: "users_set",
                schema: "identity",
                newName: "users_set",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "suppliers_set",
                schema: "master",
                newName: "suppliers_set",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "stock_movements",
                schema: "inventory",
                newName: "stock_movements",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "stock_adjustments",
                schema: "inventory",
                newName: "stock_adjustments",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "sales_orders",
                schema: "sales",
                newName: "sales_orders",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "sales_order_items",
                schema: "sales",
                newName: "sales_order_items",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "roles",
                schema: "identity",
                newName: "roles",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "report_export_jobs",
                schema: "reporting",
                newName: "report_export_jobs",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "purchase_orders",
                schema: "purchasing",
                newName: "purchase_orders",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "purchase_order_items",
                schema: "purchasing",
                newName: "purchase_order_items",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "products_set",
                schema: "master",
                newName: "products_set",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "password_reset_tokens",
                schema: "identity",
                newName: "password_reset_tokens",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "notifications",
                schema: "identity",
                newName: "notifications",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "goods_receipts",
                schema: "purchasing",
                newName: "goods_receipts",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "goods_receipt_items",
                schema: "purchasing",
                newName: "goods_receipt_items",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "customers_set",
                schema: "master",
                newName: "customers_set",
                newSchema: "stockflow");

            migrationBuilder.RenameTable(
                name: "categories_set",
                schema: "master",
                newName: "categories_set",
                newSchema: "stockflow");
        }
    }
}
