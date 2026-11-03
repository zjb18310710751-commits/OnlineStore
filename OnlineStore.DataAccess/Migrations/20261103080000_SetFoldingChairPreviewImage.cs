using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OnlineStore.Data;

namespace OnlineStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261103080000_SetFoldingChairPreviewImage")]
public partial class SetFoldingChairPreviewImage : Migration
{
    private const string PreviewImageUrl = "/images/products/folding-chair-preview.jpg";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            DECLARE @ProductId int = (
                SELECT TOP (1) [Id]
                FROM [Products]
                WHERE [Sku] = N'EXT-079'
            );

            IF @ProductId IS NOT NULL
            BEGIN
                UPDATE [Products]
                SET [ImageUrl] = N'{PreviewImageUrl}'
                WHERE [Id] = @ProductId;

                UPDATE [ProductImages]
                SET [ImageUrl] = N'{PreviewImageUrl}'
                WHERE [ProductId] = @ProductId;

                UPDATE [ProductSkus]
                SET [ImageUrl] = N'{PreviewImageUrl}'
                WHERE [ProductId] = @ProductId;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @ProductId int = (
                SELECT TOP (1) [Id]
                FROM [Products]
                WHERE [Sku] = N'EXT-079'
            );

            IF @ProductId IS NOT NULL
            BEGIN
                UPDATE [Products]
                SET [ImageUrl] = N'https://picsum.photos/seed/EXT-079/600/400'
                WHERE [Id] = @ProductId;

                UPDATE [ProductImages]
                SET [ImageUrl] = CASE [SortOrder]
                    WHEN 0 THEN N'https://picsum.photos/seed/EXT-079/600/400'
                    WHEN 1 THEN N'https://picsum.photos/seed/EXT-079-b/600/400'
                    ELSE N'https://picsum.photos/seed/EXT-079-c/600/400'
                END
                WHERE [ProductId] = @ProductId;

                UPDATE [ProductSkus]
                SET [ImageUrl] = N'https://picsum.photos/seed/EXT-079-s0/600/400'
                WHERE [ProductId] = @ProductId;
            END;
            """);
    }
}
