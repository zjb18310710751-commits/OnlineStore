using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OnlineStore.Data;

#nullable disable

namespace OnlineStore.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261028231500_AddReturnShipmentTracking")]
    public partial class AddReturnShipmentTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnCarrier",
                table: "AfterSalesRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnShippedAt",
                table: "AfterSalesRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnTrackingNumber",
                table: "AfterSalesRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnCarrier",
                table: "AfterSalesRequests");

            migrationBuilder.DropColumn(
                name: "ReturnShippedAt",
                table: "AfterSalesRequests");

            migrationBuilder.DropColumn(
                name: "ReturnTrackingNumber",
                table: "AfterSalesRequests");
        }
    }
}
