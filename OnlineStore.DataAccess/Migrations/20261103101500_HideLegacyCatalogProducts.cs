using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OnlineStore.Data;

namespace OnlineStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261103101500_HideLegacyCatalogProducts")]
public partial class HideLegacyCatalogProducts : Migration
{
    private const string LegacySkus = """
        N'EXT-008', N'EXT-009', N'EXT-010', N'EXT-011', N'EXT-012',
        N'EXT-013', N'EXT-014', N'EXT-015', N'EXT-016', N'EXT-017',
        N'EXT-018', N'EXT-019', N'EXT-020',
        N'EXT-029', N'EXT-030', N'EXT-031', N'EXT-032', N'EXT-033',
        N'EXT-034', N'EXT-035', N'EXT-036', N'EXT-037', N'EXT-038',
        N'EXT-047', N'EXT-048', N'EXT-049', N'EXT-050',
        N'EXT-057', N'EXT-058', N'EXT-059', N'EXT-060', N'EXT-061',
        N'EXT-062', N'EXT-063', N'EXT-064', N'EXT-065', N'EXT-066',
        N'EXT-067', N'EXT-068', N'EXT-069', N'EXT-070',
        N'EXT-080', N'EXT-081', N'EXT-082', N'EXT-083', N'EXT-084',
        N'EXT-085', N'EXT-086', N'EXT-087', N'EXT-088'
        """;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            UPDATE [Products]
            SET [IsActive] = CAST(0 AS bit),
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Sku] IN ({LegacySkus});
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            UPDATE [Products]
            SET [IsActive] = CAST(1 AS bit),
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [Sku] IN ({LegacySkus});
            """);
    }
}
