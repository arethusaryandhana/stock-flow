using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "created_by_id",
                schema: "inventory",
                table: "stock_movements",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "created_by_id",
                schema: "inventory",
                table: "stock_adjustments",
                newName: "created_by");

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "master",
                table: "suppliers_set",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "master",
                table: "suppliers_set",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "inventory",
                table: "stock_movements",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "inventory",
                table: "stock_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by",
                schema: "inventory",
                table: "stock_adjustments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "inventory",
                table: "stock_adjustments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "sales",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "sales",
                table: "sales_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "sales",
                table: "sales_order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "sales",
                table: "sales_order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "roles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "reporting",
                table: "report_export_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "reporting",
                table: "report_export_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "purchasing",
                table: "purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "purchasing",
                table: "purchase_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "purchasing",
                table: "purchase_order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "purchasing",
                table: "purchase_order_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "master",
                table: "products_set",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "master",
                table: "products_set",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "password_reset_tokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "identity",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "identity",
                table: "notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "purchasing",
                table: "goods_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "purchasing",
                table: "goods_receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "purchasing",
                table: "goods_receipt_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "purchasing",
                table: "goods_receipt_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "master",
                table: "customers_set",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "master",
                table: "customers_set",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                schema: "master",
                table: "categories_set",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "updated_by",
                schema: "master",
                table: "categories_set",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "master",
                table: "suppliers_set");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "master",
                table: "suppliers_set");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "inventory",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "inventory",
                table: "stock_adjustments");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "sales",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "sales",
                table: "sales_orders");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "sales",
                table: "sales_order_items");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "sales",
                table: "sales_order_items");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "identity",
                table: "roles");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "reporting",
                table: "report_export_jobs");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "reporting",
                table: "report_export_jobs");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "purchasing",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "purchasing",
                table: "purchase_orders");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "purchasing",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "purchasing",
                table: "purchase_order_items");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "master",
                table: "products_set");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "master",
                table: "products_set");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "identity",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "identity",
                table: "password_reset_tokens");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "identity",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "identity",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "purchasing",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "purchasing",
                table: "goods_receipts");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "purchasing",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "purchasing",
                table: "goods_receipt_items");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "master",
                table: "customers_set");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "master",
                table: "customers_set");

            migrationBuilder.DropColumn(
                name: "created_by",
                schema: "master",
                table: "categories_set");

            migrationBuilder.DropColumn(
                name: "updated_by",
                schema: "master",
                table: "categories_set");

            migrationBuilder.RenameColumn(
                name: "created_by",
                schema: "inventory",
                table: "stock_movements",
                newName: "created_by_id");

            migrationBuilder.RenameColumn(
                name: "created_by",
                schema: "inventory",
                table: "stock_adjustments",
                newName: "created_by_id");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_id",
                schema: "inventory",
                table: "stock_movements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_id",
                schema: "inventory",
                table: "stock_adjustments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
