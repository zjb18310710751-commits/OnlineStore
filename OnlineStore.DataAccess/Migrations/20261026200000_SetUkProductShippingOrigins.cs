using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OnlineStore.Data;

namespace OnlineStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261026200000_SetUkProductShippingOrigins")]
public partial class SetUkProductShippingOrigins : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE [Products]
            SET [ShippingFrom] = CASE (([Id] - 1) % 10)
                WHEN 0 THEN N'伦敦'
                WHEN 1 THEN N'曼彻斯特'
                WHEN 2 THEN N'伯明翰'
                WHEN 3 THEN N'利兹'
                WHEN 4 THEN N'格拉斯哥'
                WHEN 5 THEN N'布里斯托尔'
                WHEN 6 THEN N'利物浦'
                WHEN 7 THEN N'爱丁堡'
                WHEN 8 THEN N'谢菲尔德'
                ELSE N'卡迪夫'
            END,
            [ShippingFromEn] = CASE (([Id] - 1) % 10)
                WHEN 0 THEN N'London'
                WHEN 1 THEN N'Manchester'
                WHEN 2 THEN N'Birmingham'
                WHEN 3 THEN N'Leeds'
                WHEN 4 THEN N'Glasgow'
                WHEN 5 THEN N'Bristol'
                WHEN 6 THEN N'Liverpool'
                WHEN 7 THEN N'Edinburgh'
                WHEN 8 THEN N'Sheffield'
                ELSE N'Cardiff'
            END;

            UPDATE [o]
            SET [ShippingOrigin] = [p].[ShippingFrom],
                [ShippingOriginEn] = [p].[ShippingFromEn],
                [ShippingFeeText] = [p].[ShippingFee],
                [ShippingFeeTextEn] = [p].[ShippingFeeEn],
                [DispatchTimeText] = [p].[DeliveryTime],
                [DispatchTimeTextEn] = [p].[DeliveryTimeEn],
                [ExpectedDispatchAt] = DATEADD(HOUR, 48, [o].[CreatedAt])
            FROM [Orders] AS [o]
            CROSS APPLY (
                SELECT TOP (1) [p].[ShippingFrom], [p].[ShippingFromEn],
                    [p].[ShippingFee], [p].[ShippingFeeEn],
                    [p].[DeliveryTime], [p].[DeliveryTimeEn]
                FROM [OrderItems] AS [i]
                INNER JOIN [Products] AS [p] ON [p].[Id] = [i].[ProductId]
                WHERE [i].[OrderId] = [o].[Id]
                ORDER BY [i].[Id]
            ) AS [p]
            WHERE [o].[OrderNo] LIKE N'DEMO-%';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
