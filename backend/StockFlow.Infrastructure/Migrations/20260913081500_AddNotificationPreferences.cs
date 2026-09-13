using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Migrations
{
    [DbContext(typeof(StockFlowDbContext))]
    [Migration("20260913081500_AddNotificationPreferences")]
    public partial class AddNotificationPreferences : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "in_app_notifications_enabled",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "low_stock_notifications_enabled",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "notification_polling_interval_seconds",
                schema: "identity",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<bool>(
                name: "notification_sound_enabled",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "report_ready_notifications_enabled",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "system_notifications_enabled",
                schema: "identity",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "in_app_notifications_enabled",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "low_stock_notifications_enabled",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "notification_polling_interval_seconds",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "notification_sound_enabled",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "report_ready_notifications_enabled",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "system_notifications_enabled",
                schema: "identity",
                table: "users");
        }
    }
}
