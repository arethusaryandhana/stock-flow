using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Migrations
{
    [DbContext(typeof(StockFlowDbContext))]
    [Migration("20260913103000_AddCompanyProfile")]
    public partial class AddCompanyProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "company_profile",
                schema: "master",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    logo_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_profile", x => x.id);
                });

            migrationBuilder.Sql("""
                INSERT INTO master.company_profile
                    (id, name, address, email, phone, currency, logo_url, created_at)
                VALUES
                    ('5f7b8c29-1a42-4d6e-9b10-2c3d4e5f6071', 'StockFlow Demo', NULL, NULL, NULL, 'IDR', NULL, NOW())
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(name: "company_profile", schema: "master");
    }
}
