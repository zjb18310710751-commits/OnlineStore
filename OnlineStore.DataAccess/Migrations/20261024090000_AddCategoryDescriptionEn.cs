using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OnlineStore.Data;

#nullable disable

namespace OnlineStore.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261024090000_AddCategoryDescriptionEn")]
    public partial class AddCategoryDescriptionEn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Categories");
        }
    }
}
