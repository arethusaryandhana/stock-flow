using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameUsersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_goods_receipts_users_set_received_by_id",
                schema: "purchasing",
                table: "goods_receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_users_set_user_id",
                schema: "identity",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_password_reset_tokens_users_set_user_id",
                schema: "identity",
                table: "password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_report_export_jobs_users_set_requested_by_id",
                schema: "reporting",
                table: "report_export_jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_users_set_roles_role_id",
                schema: "identity",
                table: "users_set");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users_set",
                schema: "identity",
                table: "users_set");

            migrationBuilder.RenameTable(
                name: "users_set",
                schema: "identity",
                newName: "users",
                newSchema: "identity");

            migrationBuilder.RenameIndex(
                name: "IX_users_set_role_id",
                schema: "identity",
                table: "users",
                newName: "IX_users_role_id");

            migrationBuilder.RenameIndex(
                name: "IX_users_set_email",
                schema: "identity",
                table: "users",
                newName: "IX_users_email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                schema: "identity",
                table: "users",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_goods_receipts_users_received_by_id",
                schema: "purchasing",
                table: "goods_receipts",
                column: "received_by_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_users_user_id",
                schema: "identity",
                table: "notifications",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_password_reset_tokens_users_user_id",
                schema: "identity",
                table: "password_reset_tokens",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_report_export_jobs_users_requested_by_id",
                schema: "reporting",
                table: "report_export_jobs",
                column: "requested_by_id",
                principalSchema: "identity",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_users_roles_role_id",
                schema: "identity",
                table: "users",
                column: "role_id",
                principalSchema: "identity",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_goods_receipts_users_received_by_id",
                schema: "purchasing",
                table: "goods_receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_notifications_users_user_id",
                schema: "identity",
                table: "notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_password_reset_tokens_users_user_id",
                schema: "identity",
                table: "password_reset_tokens");

            migrationBuilder.DropForeignKey(
                name: "FK_report_export_jobs_users_requested_by_id",
                schema: "reporting",
                table: "report_export_jobs");

            migrationBuilder.DropForeignKey(
                name: "FK_users_roles_role_id",
                schema: "identity",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                schema: "identity",
                table: "users");

            migrationBuilder.RenameTable(
                name: "users",
                schema: "identity",
                newName: "users_set",
                newSchema: "identity");

            migrationBuilder.RenameIndex(
                name: "IX_users_role_id",
                schema: "identity",
                table: "users_set",
                newName: "IX_users_set_role_id");

            migrationBuilder.RenameIndex(
                name: "IX_users_email",
                schema: "identity",
                table: "users_set",
                newName: "IX_users_set_email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users_set",
                schema: "identity",
                table: "users_set",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_goods_receipts_users_set_received_by_id",
                schema: "purchasing",
                table: "goods_receipts",
                column: "received_by_id",
                principalSchema: "identity",
                principalTable: "users_set",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_notifications_users_set_user_id",
                schema: "identity",
                table: "notifications",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users_set",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_password_reset_tokens_users_set_user_id",
                schema: "identity",
                table: "password_reset_tokens",
                column: "user_id",
                principalSchema: "identity",
                principalTable: "users_set",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_report_export_jobs_users_set_requested_by_id",
                schema: "reporting",
                table: "report_export_jobs",
                column: "requested_by_id",
                principalSchema: "identity",
                principalTable: "users_set",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_users_set_roles_role_id",
                schema: "identity",
                table: "users_set",
                column: "role_id",
                principalSchema: "identity",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
