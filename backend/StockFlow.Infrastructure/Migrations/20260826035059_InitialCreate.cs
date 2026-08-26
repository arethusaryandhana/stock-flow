using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "stockflow");

            migrationBuilder.CreateTable(
                name: "categories_set",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories_set", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customers_set",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers_set", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers_set",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers_set", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products_set",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    selling_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    stock_on_hand = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reorder_level = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products_set", x => x.id);
                    table.ForeignKey(
                        name: "FK_products_set_categories_set_category_id",
                        column: x => x.category_id,
                        principalSchema: "stockflow",
                        principalTable: "categories_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_orders",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_sales_orders_customers_set_customer_id",
                        column: x => x.customer_id,
                        principalSchema: "stockflow",
                        principalTable: "customers_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users_set",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users_set", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_set_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "stockflow",
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "text", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    order_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expected_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_orders_suppliers_set_supplier_id",
                        column: x => x.supplier_id,
                        principalSchema: "stockflow",
                        principalTable: "suppliers_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustments",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "text", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_delta = table.Column<decimal>(type: "numeric", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_adjustments", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_adjustments_products_set_product_id",
                        column: x => x.product_id,
                        principalSchema: "stockflow",
                        principalTable: "products_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_movements",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric", nullable: false),
                    reference_number = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_movements_products_set_product_id",
                        column: x => x.product_id,
                        principalSchema: "stockflow",
                        principalTable: "products_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_items",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sales_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_order_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_sales_order_items_products_set_product_id",
                        column: x => x.product_id,
                        principalSchema: "stockflow",
                        principalTable: "products_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sales_order_items_sales_orders_sales_order_id",
                        column: x => x.sales_order_id,
                        principalSchema: "stockflow",
                        principalTable: "sales_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_users_set_user_id",
                        column: x => x.user_id,
                        principalSchema: "stockflow",
                        principalTable: "users_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "report_export_jobs",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_number = table.Column<string>(type: "text", nullable: false),
                    report_type = table.Column<string>(type: "text", nullable: false),
                    parameters = table.Column<string>(type: "text", nullable: false),
                    format = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    progress = table.Column<int>(type: "integer", nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    requested_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_export_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_export_jobs_users_set_requested_by_id",
                        column: x => x.requested_by_id,
                        principalSchema: "stockflow",
                        principalTable: "users_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipts",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "text", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    received_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipts", x => x.id);
                    table.ForeignKey(
                        name: "FK_goods_receipts_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "stockflow",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_users_set_received_by_id",
                        column: x => x.received_by_id,
                        principalSchema: "stockflow",
                        principalTable: "users_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_items",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_purchase_order_items_products_set_product_id",
                        column: x => x.product_id,
                        principalSchema: "stockflow",
                        principalTable: "products_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_purchase_order_items_purchase_orders_purchase_order_id",
                        column: x => x.purchase_order_id,
                        principalSchema: "stockflow",
                        principalTable: "purchase_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_items",
                schema: "stockflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goods_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipt_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_goods_receipt_items_goods_receipts_goods_receipt_id",
                        column: x => x.goods_receipt_id,
                        principalSchema: "stockflow",
                        principalTable: "goods_receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_goods_receipt_items_products_set_product_id",
                        column: x => x.product_id,
                        principalSchema: "stockflow",
                        principalTable: "products_set",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categories_set_name",
                schema: "stockflow",
                table: "categories_set",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_customers_set_code",
                schema: "stockflow",
                table: "customers_set",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_items_goods_receipt_id",
                schema: "stockflow",
                table: "goods_receipt_items",
                column: "goods_receipt_id");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_items_product_id",
                schema: "stockflow",
                table: "goods_receipt_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_purchase_order_id",
                schema: "stockflow",
                table: "goods_receipts",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_received_by_id",
                schema: "stockflow",
                table: "goods_receipts",
                column: "received_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                schema: "stockflow",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_set_category_id_is_active",
                schema: "stockflow",
                table: "products_set",
                columns: new[] { "category_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_products_set_sku",
                schema: "stockflow",
                table: "products_set",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_items_product_id",
                schema: "stockflow",
                table: "purchase_order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_items_purchase_order_id",
                schema: "stockflow",
                table: "purchase_order_items",
                column: "purchase_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_status_order_date",
                schema: "stockflow",
                table: "purchase_orders",
                columns: new[] { "status", "order_date" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_supplier_id",
                schema: "stockflow",
                table: "purchase_orders",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_export_jobs_job_number",
                schema: "stockflow",
                table: "report_export_jobs",
                column: "job_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_export_jobs_requested_by_id",
                schema: "stockflow",
                table: "report_export_jobs",
                column: "requested_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_export_jobs_status_requested_at",
                schema: "stockflow",
                table: "report_export_jobs",
                columns: new[] { "status", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_items_product_id",
                schema: "stockflow",
                table: "sales_order_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_items_sales_order_id",
                schema: "stockflow",
                table: "sales_order_items",
                column: "sales_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_customer_id",
                schema: "stockflow",
                table: "sales_orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_status_order_date",
                schema: "stockflow",
                table: "sales_orders",
                columns: new[] { "status", "order_date" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_adjustments_product_id",
                schema: "stockflow",
                table: "stock_adjustments",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_product_id_created_at",
                schema: "stockflow",
                table: "stock_movements",
                columns: new[] { "product_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_set_code",
                schema: "stockflow",
                table: "suppliers_set",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_set_email",
                schema: "stockflow",
                table: "users_set",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_set_role_id",
                schema: "stockflow",
                table: "users_set",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "goods_receipt_items",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "purchase_order_items",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "report_export_jobs",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "sales_order_items",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "stock_adjustments",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "stock_movements",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "goods_receipts",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "sales_orders",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "products_set",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "purchase_orders",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "users_set",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "customers_set",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "categories_set",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "suppliers_set",
                schema: "stockflow");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "stockflow");
        }
    }
}
