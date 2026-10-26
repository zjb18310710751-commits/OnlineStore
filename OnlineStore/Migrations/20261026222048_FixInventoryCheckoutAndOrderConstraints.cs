using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Migrations
{
    /// <inheritdoc />
    public partial class FixInventoryCheckoutAndOrderConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AfterSalesRequests_OrderId",
                table: "AfterSalesRequests");

            migrationBuilder.DropColumn(
                name: "ReturnCarrier",
                table: "AfterSalesRequests");

            migrationBuilder.DropColumn(
                name: "ReturnTrackingNumber",
                table: "AfterSalesRequests");

            migrationBuilder.AddColumn<double>(
                name: "ShippingFeeAmount",
                table: "Products",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "InventoryRestoredAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderNotes",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "cash_on_delivery");

            migrationBuilder.AddColumn<int>(
                name: "ReplacementForOrderId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ShippingAmount",
                table: "Orders",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.Sql("""
                ;WITH duplicateOrders AS (
                    SELECT [Id],
                           ROW_NUMBER() OVER (PARTITION BY [OrderNo] ORDER BY [Id]) AS [RowNumber]
                    FROM [Orders]
                )
                UPDATE [Orders]
                SET [OrderNo] = CONCAT(LEFT([Orders].[OrderNo], 18), '-', [Orders].[Id])
                FROM [Orders]
                INNER JOIN duplicateOrders AS [duplicates] ON [duplicates].[Id] = [Orders].[Id]
                WHERE [duplicates].[RowNumber] > 1;

                UPDATE [Products]
                SET [ShippingFeeAmount] = COALESCE(
                    TRY_CONVERT(float, REPLACE(REPLACE(REPLACE([ShippingFee], N'运费', N''), N'¥', N''), N' ', N'')),
                    0);

                UPDATE [Orders]
                SET [PaymentMethod] = N'cash_on_delivery'
                WHERE [PaymentMethod] = N'';

                ;WITH duplicateAfterSales AS (
                    SELECT [Id],
                           ROW_NUMBER() OVER (PARTITION BY [OrderId] ORDER BY [RequestedAt], [Id]) AS [RowNumber]
                    FROM [AfterSalesRequests]
                    WHERE [Status] <> N'已拒绝'
                )
                UPDATE [AfterSalesRequests]
                SET [Status] = N'已拒绝',
                    [AdminNote] = CONCAT(COALESCE([AdminNote], N''), N' [自动关闭重复申请]')
                FROM [AfterSalesRequests]
                INNER JOIN duplicateAfterSales AS [duplicates]
                    ON [duplicates].[Id] = [AfterSalesRequests].[Id]
                WHERE [duplicates].[RowNumber] > 1;

                UPDATE [Orders]
                SET [InventoryRestoredAt] = COALESCE(
                    (SELECT MAX([request].[CompletedAt])
                     FROM [AfterSalesRequests] AS [request]
                     WHERE [request].[OrderId] = [Orders].[Id]
                       AND [request].[Type] = N'退货'
                       AND [request].[Status] = N'已完成'),
                    GETUTCDATE())
                WHERE EXISTS (
                    SELECT 1
                    FROM [AfterSalesRequests] AS [request]
                    WHERE [request].[OrderId] = [Orders].[Id]
                      AND [request].[Type] = N'退货'
                      AND [request].[Status] = N'已完成'
                );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNo",
                table: "Orders",
                column: "OrderNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ReplacementForOrderId",
                table: "Orders",
                column: "ReplacementForOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_AfterSalesRequests_OrderId",
                table: "AfterSalesRequests",
                column: "OrderId",
                unique: true,
                filter: "[Status] <> N'已拒绝'");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Orders_ReplacementForOrderId",
                table: "Orders",
                column: "ReplacementForOrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Orders_ReplacementForOrderId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNo",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ReplacementForOrderId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_AfterSalesRequests_OrderId",
                table: "AfterSalesRequests");

            migrationBuilder.DropColumn(
                name: "ShippingFeeAmount",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "InventoryRestoredAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OrderNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReplacementForOrderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAmount",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "ReturnCarrier",
                table: "AfterSalesRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnTrackingNumber",
                table: "AfterSalesRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AfterSalesRequests_OrderId",
                table: "AfterSalesRequests",
                column: "OrderId");
        }
    }
}
