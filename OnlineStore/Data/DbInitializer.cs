using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineStore.Models;

namespace OnlineStore.Data;

/// <summary>
/// 首次运行初始化：按当前模型建库建表（EnsureCreated）并写入种子数据。
/// 生产环境建议改用 EF Core Migrations（见 README）。
/// </summary>
public static class DbInitializer
{
    public const string AdminEmail = "admin@store.com";
    public const string AdminPassword = "Admin@123456";
    public const string AdminRole = "Admin";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // 首次运行：直接按模型创建数据库结构，无需 dotnet-ef 工具。
        await context.Database.EnsureCreatedAsync();

        await SeedAsync(context, userManager, roleManager);
    }

    private static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        // 1) 角色
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(AdminRole));
        }

        // 2) 管理员账号（幂等）
        if (await userManager.FindByEmailAsync(AdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, AdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AdminRole);
            }
        }

        // 3) 分类与示例商品（幂等：仅在分类表为空时写入）
        if (await context.Categories.AnyAsync())
        {
            return;
        }

        var electronics = new Category { Name = "电子产品", Slug = "电子产品", Description = "手机、电脑、数码配件", DisplayOrder = 1 };
        var clothing = new Category { Name = "服装服饰", Slug = "服装服饰", Description = "男女装、鞋帽配饰", DisplayOrder = 2 };
        var books = new Category { Name = "图书", Slug = "图书", Description = "文学、科技、教育", DisplayOrder = 3 };
        var home = new Category { Name = "家居生活", Slug = "家居生活", Description = "家纺、厨具、日用", DisplayOrder = 4 };
        var sports = new Category { Name = "运动户外", Slug = "运动户外", Description = "健身、户外装备", DisplayOrder = 5 };

        var categories = new List<Category> { electronics, clothing, books, home, sports };

        var products = new List<Product>
        {
            new() { Name = "无线蓝牙耳机", Description = "主动降噪，30 小时续航，支持快充。", Price = 299.00m, Stock = 120, Sku = "ELEC-001", Category = electronics },
            new() { Name = "机械键盘 87 键", Description = "热插拔轴体，RGB 背光，PBT 键帽。", Price = 459.00m, Stock = 60, Sku = "ELEC-002", Category = electronics },
            new() { Name = "27 英寸 2K 显示器", Description = "IPS 面板，144Hz 刷新率，广色域。", Price = 1299.00m, Stock = 35, Sku = "ELEC-003", Category = electronics },
            new() { Name = "纯棉基础款 T 恤", Description = "100% 精梳棉，多色可选，透气舒适。", Price = 79.00m, Stock = 300, Sku = "CLOTH-001", Category = clothing },
            new() { Name = "休闲运动卫衣", Description = "宽松版型，加绒加厚，秋冬百搭。", Price = 199.00m, Stock = 150, Sku = "CLOTH-002", Category = clothing },
            new() { Name = "《深入理解计算机系统》", Description = "计算机科学经典教材（原书第 3 版）。", Price = 139.00m, Stock = 80, Sku = "BOOK-001", Category = books },
            new() { Name = "《百年孤独》", Description = "加西亚·马尔克斯代表作，经典文学。", Price = 55.00m, Stock = 200, Sku = "BOOK-002", Category = books },
            new() { Name = "北欧风陶瓷马克杯", Description = "简约设计，450ml 大容量，可微波加热。", Price = 39.90m, Stock = 500, Sku = "HOME-001", Category = home },
            new() { Name = "记忆棉护颈枕", Description = "慢回弹，符合人体工学，改善睡眠。", Price = 129.00m, Stock = 90, Sku = "HOME-002", Category = home },
            new() { Name = "瑜伽垫 加厚防滑", Description = "10mm 加厚，TPE 环保材质，附赠绑带。", Price = 89.00m, Stock = 110, Sku = "SPORT-001", Category = sports },
            new() { Name = "便携运动水壶", Description = "食品级材质，一按弹盖，750ml。", Price = 49.00m, Stock = 260, Sku = "SPORT-002", Category = sports },
            new() { Name = "户外露营帐篷", Description = "双层防雨，3-4 人空间，速开设计。", Price = 399.00m, Stock = 25, Sku = "SPORT-003", Category = sports }
        };

        context.Categories.AddRange(categories);
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }
}
