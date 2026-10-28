using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineStore.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "OrderItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE orderItem
                SET orderItem.ImageUrl = COALESCE(NULLIF(sku.ImageUrl, N''), product.ImageUrl)
                FROM [OrderItems] AS orderItem
                INNER JOIN [Products] AS product ON product.Id = orderItem.ProductId
                LEFT JOIN [ProductSkus] AS sku ON sku.Id = orderItem.SkuId
                WHERE orderItem.ImageUrl IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "OrderItems");
        }
    }
}
