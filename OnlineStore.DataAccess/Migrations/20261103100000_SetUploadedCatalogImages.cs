using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OnlineStore.Data;

namespace OnlineStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261103100000_SetUploadedCatalogImages")]
public partial class SetUploadedCatalogImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @Images TABLE
            (
                [Sku] nvarchar(50) NOT NULL PRIMARY KEY,
                [ImageUrl] nvarchar(500) NOT NULL
            );

            INSERT INTO @Images ([Sku], [ImageUrl])
            VALUES
                (N'EXT-002', N'/images/products/ext-002.jpg'),
                (N'EXT-003', N'/images/products/ext-003.jpg'),
                (N'EXT-004', N'/images/products/ext-004.jpg'),
                (N'EXT-005', N'/images/products/ext-005.jpg'),
                (N'EXT-006', N'/images/products/ext-006.jpg'),
                (N'EXT-007', N'/images/products/ext-007.jpg'),
                (N'EXT-051', N'/images/products/ext-051.jpg'),
                (N'EXT-052', N'/images/products/ext-052.jpg'),
                (N'EXT-053', N'/images/products/ext-053.jpg'),
                (N'EXT-054', N'/images/products/ext-054.jpg'),
                (N'EXT-055', N'/images/products/ext-055.jpg'),
                (N'EXT-056', N'/images/products/ext-056.jpg'),
                (N'EXT-071', N'/images/products/ext-071.jpg'),
                (N'EXT-072', N'/images/products/ext-072.jpg'),
                (N'EXT-073', N'/images/products/ext-073.jpg'),
                (N'EXT-074', N'/images/products/ext-074.jpg'),
                (N'EXT-075', N'/images/products/ext-075.jpg'),
                (N'EXT-076', N'/images/products/ext-076.jpg'),
                (N'EXT-077', N'/images/products/ext-077.jpg'),
                (N'EXT-078', N'/images/products/ext-078.jpg');

            UPDATE [p]
            SET [ImageUrl] = [i].[ImageUrl]
            FROM [Products] AS [p]
            INNER JOIN @Images AS [i] ON [i].[Sku] = [p].[Sku];

            UPDATE [pi]
            SET [ImageUrl] = [i].[ImageUrl]
            FROM [ProductImages] AS [pi]
            INNER JOIN [Products] AS [p] ON [p].[Id] = [pi].[ProductId]
            INNER JOIN @Images AS [i] ON [i].[Sku] = [p].[Sku];

            UPDATE [ps]
            SET [ImageUrl] = [i].[ImageUrl]
            FROM [ProductSkus] AS [ps]
            INNER JOIN [Products] AS [p] ON [p].[Id] = [ps].[ProductId]
            INNER JOIN @Images AS [i] ON [i].[Sku] = [p].[Sku];
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE [p]
            SET [ImageUrl] = N'https://picsum.photos/seed/' + [p].[Sku] + N'/600/400'
            FROM [Products] AS [p]
            WHERE [p].[Sku] IN (
                N'EXT-002', N'EXT-003', N'EXT-004', N'EXT-005',
                N'EXT-006', N'EXT-007', N'EXT-051', N'EXT-052',
                N'EXT-053', N'EXT-054', N'EXT-055', N'EXT-056',
                N'EXT-071', N'EXT-072', N'EXT-073', N'EXT-074',
                N'EXT-075', N'EXT-076', N'EXT-077', N'EXT-078'
            );

            UPDATE [pi]
            SET [ImageUrl] = N'https://picsum.photos/seed/' + [p].[Sku]
                + CASE [pi].[SortOrder]
                    WHEN 1 THEN N'-b/600/400'
                    WHEN 2 THEN N'-c/600/400'
                    ELSE N'/600/400'
                  END
            FROM [ProductImages] AS [pi]
            INNER JOIN [Products] AS [p] ON [p].[Id] = [pi].[ProductId]
            WHERE [p].[Sku] IN (
                N'EXT-002', N'EXT-003', N'EXT-004', N'EXT-005',
                N'EXT-006', N'EXT-007', N'EXT-051', N'EXT-052',
                N'EXT-053', N'EXT-054', N'EXT-055', N'EXT-056',
                N'EXT-071', N'EXT-072', N'EXT-073', N'EXT-074',
                N'EXT-075', N'EXT-076', N'EXT-077', N'EXT-078'
            );

            UPDATE [ps]
            SET [ImageUrl] = N'https://picsum.photos/seed/' + [p].[Sku] + N'-s0/600/400'
            FROM [ProductSkus] AS [ps]
            INNER JOIN [Products] AS [p] ON [p].[Id] = [ps].[ProductId]
            WHERE [p].[Sku] IN (
                N'EXT-002', N'EXT-003', N'EXT-004', N'EXT-005',
                N'EXT-006', N'EXT-007', N'EXT-051', N'EXT-052',
                N'EXT-053', N'EXT-054', N'EXT-055', N'EXT-056',
                N'EXT-071', N'EXT-072', N'EXT-073', N'EXT-074',
                N'EXT-075', N'EXT-076', N'EXT-077', N'EXT-078'
            );
            """);
    }
}
