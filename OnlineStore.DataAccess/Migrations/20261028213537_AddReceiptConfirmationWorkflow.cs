using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptConfirmationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AutoConfirmAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReceiptConfirmedAutomatically",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptExceptionReason",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptExceptionReportedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptExceptionReportedBy",
                table: "Orders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [Orders]
                SET [AutoConfirmAt] = DATEADD(day, 7, COALESCE([DeliveredAt], DATEADD(second, 9, [ShippedAt]), GETUTCDATE()))
                WHERE [Status] = N'已送达' AND [AutoConfirmAt] IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoConfirmAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReceiptConfirmedAutomatically",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReceiptExceptionReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReceiptExceptionReportedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReceiptExceptionReportedBy",
                table: "Orders");
        }
    }
}
