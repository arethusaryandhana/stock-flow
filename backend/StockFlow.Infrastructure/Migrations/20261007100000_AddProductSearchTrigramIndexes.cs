using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StockFlow.Infrastructure.Migrations;

[DbContext(typeof(StockFlowDbContext))]
[Migration("20261007100000_AddProductSearchTrigramIndexes")]
public sealed class AddProductSearchTrigramIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        migrationBuilder.Sql("""
            CREATE INDEX "IX_products_set_sku_trgm"
                ON master.products_set USING gin (lower(sku) gin_trgm_ops);
            CREATE INDEX "IX_products_set_name_trgm"
                ON master.products_set USING gin (lower(name) gin_trgm_ops);
            CREATE INDEX "IX_categories_set_name_trgm"
                ON master.categories_set USING gin (lower(name) gin_trgm_ops);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS master.\"IX_products_set_sku_trgm\";");
        migrationBuilder.Sql("DROP INDEX IF EXISTS master.\"IX_products_set_name_trgm\";");
        migrationBuilder.Sql("DROP INDEX IF EXISTS master.\"IX_categories_set_name_trgm\";");
    }
}
