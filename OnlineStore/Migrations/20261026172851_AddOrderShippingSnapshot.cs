using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderShippingSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DispatchTimeText",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispatchTimeTextEn",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpectedDispatchAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingFeeText",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingFeeTextEn",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingOrigin",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingOriginEn",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DispatchTimeText",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DispatchTimeTextEn",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExpectedDispatchAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingFeeText",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingFeeTextEn",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingOrigin",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingOriginEn",
                table: "Orders");
        }
    }
}
