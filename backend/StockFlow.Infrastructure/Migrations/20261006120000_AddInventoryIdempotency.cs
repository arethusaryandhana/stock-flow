using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StockFlow.Infrastructure.Migrations;

[DbContext(typeof(StockFlowDbContext))]
[Migration("20261006120000_AddInventoryIdempotency")]
public sealed class AddInventoryIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "idempotency_key",
            schema: "inventory",
            table: "stock_adjustments",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "request_hash",
            schema: "inventory",
            table: "stock_adjustments",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_stock_adjustments_created_by_idempotency_key",
            schema: "inventory",
            table: "stock_adjustments",
            columns: new[] { "created_by", "idempotency_key" },
            unique: true,
            filter: "idempotency_key IS NOT NULL");

        migrationBuilder.AddColumn<Guid>(
            name: "idempotency_key",
            schema: "purchasing",
            table: "goods_receipts",
            type: "uuid",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "request_hash",
            schema: "purchasing",
            table: "goods_receipts",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_goods_receipts_received_by_idempotency_key",
            schema: "purchasing",
            table: "goods_receipts",
            columns: new[] { "received_by_id", "idempotency_key" },
            unique: true,
            filter: "idempotency_key IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_stock_adjustments_created_by_idempotency_key",
            schema: "inventory",
            table: "stock_adjustments");
        migrationBuilder.DropColumn(name: "idempotency_key", schema: "inventory", table: "stock_adjustments");
        migrationBuilder.DropColumn(name: "request_hash", schema: "inventory", table: "stock_adjustments");

        migrationBuilder.DropIndex(
            name: "IX_goods_receipts_received_by_idempotency_key",
            schema: "purchasing",
            table: "goods_receipts");
        migrationBuilder.DropColumn(name: "idempotency_key", schema: "purchasing", table: "goods_receipts");
        migrationBuilder.DropColumn(name: "request_hash", schema: "purchasing", table: "goods_receipts");
    }
}
