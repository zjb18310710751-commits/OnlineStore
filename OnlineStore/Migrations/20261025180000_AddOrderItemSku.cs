using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OnlineStore.Data;

#nullable disable

namespace OnlineStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261025180000_AddOrderItemSku")]
public class AddOrderItemSku : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<int>(name: "SkuId", table: "OrderItems", type: "int", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(name: "SkuId", table: "OrderItems");
}
