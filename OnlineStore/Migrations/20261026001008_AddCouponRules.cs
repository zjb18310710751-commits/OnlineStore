using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CampaignName",
                table: "Coupons",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExcludeDiscountedItems",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "FirstOrderOnly",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Coupons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "MaxDiscountAmount",
                table: "Coupons",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PerUserLimit",
                table: "Coupons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "Coupons",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                "UPDATE [Coupons] SET [PerUserLimit] = 1 WHERE [PerUserLimit] IS NULL;");
            migrationBuilder.Sql(
                "UPDATE [Coupons] SET [EndDate] = DATEADD(SECOND, 86399, CAST(CAST([EndDate] AS date) AS datetime2)) WHERE CAST([EndDate] AS time) = '00:00:00';");

            migrationBuilder.CreateTable(
                name: "CouponCategory",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    CouponId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponCategory", x => new { x.CategoryId, x.CouponId });
                    table.ForeignKey(
                        name: "FK_CouponCategory_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CouponCategory_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CouponProduct",
                columns: table => new
                {
                    CouponId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponProduct", x => new { x.CouponId, x.ProductId });
                    table.ForeignKey(
                        name: "FK_CouponProduct_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CouponProduct_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CouponRedemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CouponId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiscountAmount = table.Column<double>(type: "float", nullable: false),
                    ShippingWaived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CouponRedemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CouponRedemptions_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CouponRedemptions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CouponCategory_CouponId",
                table: "CouponCategory",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponProduct_ProductId",
                table: "CouponProduct",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemptions_CouponId_UserId_ReleasedAt",
                table: "CouponRedemptions",
                columns: new[] { "CouponId", "UserId", "ReleasedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemptions_OrderId",
                table: "CouponRedemptions",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CouponRedemptions_UserId",
                table: "CouponRedemptions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CouponCategory");
            migrationBuilder.DropTable(name: "CouponProduct");
            migrationBuilder.DropTable(name: "CouponRedemptions");

            migrationBuilder.DropColumn(name: "CampaignName", table: "Coupons");
            migrationBuilder.DropColumn(name: "ExcludeDiscountedItems", table: "Coupons");
            migrationBuilder.DropColumn(name: "FirstOrderOnly", table: "Coupons");
            migrationBuilder.DropColumn(name: "IsDeleted", table: "Coupons");
            migrationBuilder.DropColumn(name: "MaxDiscountAmount", table: "Coupons");
            migrationBuilder.DropColumn(name: "PerUserLimit", table: "Coupons");
            migrationBuilder.DropColumn(name: "Scope", table: "Coupons");
        }
    }
}
