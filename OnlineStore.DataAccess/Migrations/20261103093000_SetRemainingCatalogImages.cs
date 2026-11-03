using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OnlineStore.Data;

namespace OnlineStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261103093000_SetRemainingCatalogImages")]
public partial class SetRemainingCatalogImages : Migration
{
    private const string ImagePairs = """
        (N'ELEC-001', N'/images/products/elec-001.jpg'),
        (N'ELEC-002', N'/images/products/elec-002.jpg'),
        (N'ELEC-003', N'/images/products/elec-003.jpg'),
        (N'CLOTH-001', N'/images/products/cloth-001.jpg'),
        (N'CLOTH-002', N'/images/products/cloth-002.jpg'),
        (N'BOOK-001', N'/images/products/book-001.jpg'),
        (N'BOOK-002', N'/images/products/book-002.jpg'),
        (N'HOME-001', N'/images/products/home-001.jpg'),
        (N'HOME-002', N'/images/products/home-002.jpg'),
        (N'SPORT-001', N'/images/products/sport-001.jpg'),
        (N'SPORT-002', N'/images/products/sport-002.jpg'),
        (N'SPORT-003', N'/images/products/sport-003.jpg'),
        (N'EXT-001', N'/images/products/ext-001.jpg'),
        (N'EXT-021', N'/images/products/ext-021.jpg'),
        (N'EXT-022', N'/images/products/ext-022.jpg'),
        (N'EXT-023', N'/images/products/ext-023.jpg'),
        (N'EXT-024', N'/images/products/ext-024.jpg'),
        (N'EXT-025', N'/images/products/ext-025.jpg'),
        (N'EXT-026', N'/images/products/ext-026.jpg'),
        (N'EXT-027', N'/images/products/ext-027.jpg'),
        (N'EXT-028', N'/images/products/ext-028.jpg'),
        (N'EXT-039', N'/images/products/ext-039.jpg'),
        (N'EXT-040', N'/images/products/ext-040.jpg'),
        (N'EXT-041', N'/images/products/ext-041.jpg'),
        (N'EXT-042', N'/images/products/ext-042.jpg'),
        (N'EXT-043', N'/images/products/ext-043.jpg'),
        (N'EXT-044', N'/images/products/ext-044.jpg'),
        (N'EXT-045', N'/images/products/ext-045.jpg'),
        (N'EXT-046', N'/images/products/ext-046.jpg')
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            DECLARE @Images TABLE
            (
                [Sku] nvarchar(50) NOT NULL PRIMARY KEY,
                [ImageUrl] nvarchar(500) NOT NULL
            );

            INSERT INTO @Images ([Sku], [ImageUrl])
            VALUES {ImagePairs};

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
                N'ELEC-001', N'ELEC-002', N'ELEC-003',
                N'CLOTH-001', N'CLOTH-002', N'BOOK-001', N'BOOK-002',
                N'HOME-001', N'HOME-002', N'SPORT-001', N'SPORT-002',
                N'SPORT-003', N'EXT-001', N'EXT-021', N'EXT-022',
                N'EXT-023', N'EXT-024', N'EXT-025', N'EXT-026',
                N'EXT-027', N'EXT-028', N'EXT-039', N'EXT-040',
                N'EXT-041', N'EXT-042', N'EXT-043', N'EXT-044',
                N'EXT-045', N'EXT-046'
            );

            UPDATE [pi]
            SET [ImageUrl] = N'https://picsum.photos/seed/' + [p].[Sku] + N'-b/600/400'
            FROM [ProductImages] AS [pi]
            INNER JOIN [Products] AS [p] ON [p].[Id] = [pi].[ProductId]
            WHERE [pi].[SortOrder] = 1
              AND [p].[Sku] IN (
                N'ELEC-001', N'ELEC-002', N'ELEC-003',
                N'CLOTH-001', N'CLOTH-002', N'BOOK-001', N'BOOK-002',
                N'HOME-001', N'HOME-002', N'SPORT-001', N'SPORT-002',
                N'SPORT-003', N'EXT-001', N'EXT-021', N'EXT-022',
                N'EXT-023', N'EXT-024', N'EXT-025', N'EXT-026',
                N'EXT-027', N'EXT-028', N'EXT-039', N'EXT-040',
                N'EXT-041', N'EXT-042', N'EXT-043', N'EXT-044',
                N'EXT-045', N'EXT-046'
            );

            UPDATE [pi]
            SET [ImageUrl] = N'https://picsum.photos/seed/' + [p].[Sku] + N'-c/600/400'
            FROM [ProductImages] AS [pi]
            INNER JOIN [Products] AS [p] ON [p].[Id] = [pi].[ProductId]
            WHERE [pi].[SortOrder] = 2
              AND [p].[Sku] IN (
                N'ELEC-001', N'ELEC-002', N'ELEC-003',
                N'CLOTH-001', N'CLOTH-002', N'BOOK-001', N'BOOK-002',
                N'HOME-001', N'HOME-002', N'SPORT-001', N'SPORT-002',
                N'SPORT-003', N'EXT-001', N'EXT-021', N'EXT-022',
                N'EXT-023', N'EXT-024', N'EXT-025', N'EXT-026',
                N'EXT-027', N'EXT-028', N'EXT-039', N'EXT-040',
                N'EXT-041', N'EXT-042', N'EXT-043', N'EXT-044',
                N'EXT-045', N'EXT-046'
            );

            UPDATE [ps]
            SET [ImageUrl] = N'https://picsum.photos/seed/' + [p].[Sku] + N'-s0/600/400'
            FROM [ProductSkus] AS [ps]
            INNER JOIN [Products] AS [p] ON [p].[Id] = [ps].[ProductId]
            WHERE [p].[Sku] IN (
                N'ELEC-001', N'ELEC-002', N'ELEC-003',
                N'CLOTH-001', N'CLOTH-002', N'BOOK-001', N'BOOK-002',
                N'HOME-001', N'HOME-002', N'SPORT-001', N'SPORT-002',
                N'SPORT-003', N'EXT-001', N'EXT-021', N'EXT-022',
                N'EXT-023', N'EXT-024', N'EXT-025', N'EXT-026',
                N'EXT-027', N'EXT-028', N'EXT-039', N'EXT-040',
                N'EXT-041', N'EXT-042', N'EXT-043', N'EXT-044',
                N'EXT-045', N'EXT-046'
            );
            """);
    }
}
