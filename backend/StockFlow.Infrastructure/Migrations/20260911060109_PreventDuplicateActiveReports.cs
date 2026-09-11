using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateActiveReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_report_export_jobs_requested_by_id",
                schema: "reporting",
                table: "report_export_jobs");

            migrationBuilder.CreateIndex(
                name: "IX_report_export_jobs_requested_by_id_report_type",
                schema: "reporting",
                table: "report_export_jobs",
                columns: new[] { "requested_by_id", "report_type" },
                unique: true,
                filter: "status IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_report_export_jobs_requested_by_id_report_type",
                schema: "reporting",
                table: "report_export_jobs");

            migrationBuilder.CreateIndex(
                name: "IX_report_export_jobs_requested_by_id",
                schema: "reporting",
                table: "report_export_jobs",
                column: "requested_by_id");
        }
    }
}
