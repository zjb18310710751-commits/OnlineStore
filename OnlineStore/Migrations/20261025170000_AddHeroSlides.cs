using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OnlineStore.Data;

#nullable disable

namespace OnlineStore.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261025170000_AddHeroSlides")]
public class AddHeroSlides : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HeroSlides",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                TitleEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                Heading = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                HeadingEn = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                Description = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                DescriptionEn = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: true),
                ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                ImageAlt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                ImageAltEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                LinkUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                DisplayOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_HeroSlides", x => x.Id));

        migrationBuilder.CreateIndex("IX_HeroSlides_IsActive_DisplayOrder", "HeroSlides", new[] { "IsActive", "DisplayOrder" });

        migrationBuilder.InsertData(
            table: "HeroSlides",
            columns: new[] { "Id", "Title", "TitleEn", "Heading", "HeadingEn", "Description", "DescriptionEn", "ImageUrl", "ImageAlt", "ImageAltEn", "LinkUrl", "DisplayOrder", "IsActive" },
            columnTypes: new[] { "int", "nvarchar(100)", "nvarchar(100)", "nvarchar(120)", "nvarchar(120)", "nvarchar(240)", "nvarchar(240)", "nvarchar(500)", "nvarchar(200)", "nvarchar(200)", "nvarchar(500)", "int", "bit" },
            values: new object[,]
            {
                { 1, "数码精选", "Audio essentials", "好声音，随你出发。", "YOUR SOUND. YOUR WORLD.", "让喜欢的音乐，成为每一天的主角。", "Your favourite sound, wherever the day takes you.", "/images/campaign-earbuds.png", "冰蓝色光影中的黑色无线入耳式耳机与充电盒", "Graphite wireless earbuds and an open charging case against an ice-blue backdrop", "/Products?searchTerm=%E8%80%B3%E6%9C%BA", 1, true },
                { 2, "自在穿搭", "Everyday style", "松弛有型，自在每一刻。", "LESS EFFORT. MORE YOU.", "把舒适穿出自己的风格。", "Comfort with a little attitude. Make it your own.", "/images/campaign-style.png", "暖桃色布景中身穿深灰色卫衣的模特", "Model wearing a charcoal hoodie in a warm peach studio", "/Products?searchTerm=%E5%8D%AB%E8%A1%A3", 2, true },
                { 3, "山野出行", "Outside awaits", "把周末，交给山野。", "TAKE THE SCENIC ROUTE.", "带上露营装备，离城市远一点，离自在近一点。", "Pack for a change of scenery. Your next great weekend starts outside.", "/images/campaign-outdoor.png", "湖畔山野中的绿色露营帐篷", "Olive camping tent beside a mountain lake at sunrise", "/Products?searchTerm=%E5%B8%90%E7%AF%B7", 3, true }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("HeroSlides");
}
