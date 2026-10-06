using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace StockFlow.Infrastructure.Migrations;

[DbContext(typeof(StockFlowDbContext))]
[Migration("20261006110000_AddReportJobLeases")]
public sealed class AddReportJobLeases : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<Guid>(
            name: "lease_token",
            schema: "reporting",
            table: "report_export_jobs",
            type: "uuid",
            nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(
            name: "lease_token",
            schema: "reporting",
            table: "report_export_jobs");
}
