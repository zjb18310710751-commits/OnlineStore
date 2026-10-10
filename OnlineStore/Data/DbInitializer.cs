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

        // 店铺
        var outdoor = new Shop { Name = "山野户外旗舰店", Rating = 4.9m, SalesCount = 12800, Description = "专注户外装备 12 年，只卖靠谱的。" };
        var digital = new Shop { Name = "极客数码自营店", Rating = 4.8m, SalesCount = 8600, Description = "正品数码，官方直营。" };
        var bookstore = new Shop { Name = "悦读图书专营店", Rating = 4.9m, SalesCount = 32000, Description = "好书，认真选每一本。" };
        var homeStore = new Shop { Name = "温暖家居生活馆", Rating = 4.7m, SalesCount = 5600, Description = "把日子过得舒服一点。" };
        var clothing = new Shop { Name = "简素服饰官方店", Rating = 4.8m, SalesCount = 21000, Description = "基础款，耐穿好搭。" };

        var shops = new List<Shop> { outdoor, digital, bookstore, homeStore, clothing };

        // 分类
        var electronics = new Category { Name = "电子产品", Slug = "电子产品", Description = "手机、电脑、数码配件", DisplayOrder = 1 };
        var fashion = new Category { Name = "服装服饰", Slug = "服装服饰", Description = "男女装、鞋帽配饰", DisplayOrder = 2 };
        var books = new Category { Name = "图书", Slug = "图书", Description = "文学、科技、教育", DisplayOrder = 3 };
        var home = new Category { Name = "家居生活", Slug = "家居生活", Description = "家纺、厨具、日用", DisplayOrder = 4 };
        var sports = new Category { Name = "运动户外", Slug = "运动户外", Description = "健身、户外装备", DisplayOrder = 5 };

        var categories = new List<Category> { electronics, fashion, books, home, sports };

        var products = new List<Product>
        {
            new()
            {
                Name = "无线蓝牙耳机", Slug = "无线蓝牙耳机",
                Description = "主动降噪，30 小时续航，支持快充，通勤运动都好用。",
                Price = 299.00m, Stock = 130, Sku = "ELEC-001", Shop = digital,
                Categories = new List<Category> { electronics },
                ShippingFrom = "广东深圳", ShippingFee = "包邮", DeliveryTime = "48小时内发货",
                Skus = new List<ProductSku>
                {
                    new() { Color = "曜石黑", Spec = "标准版", Price = 299.00m, Stock = 60 },
                    new() { Color = "云雾白", Spec = "标准版", Price = 299.00m, Stock = 40 },
                    new() { Color = "深海蓝", Spec = "标准版", Price = 309.00m, Stock = 30 }
                },
                Specs = Specs(("蓝牙版本", "5.3"), ("续航", "30 小时"), ("降噪", "主动降噪"), ("单耳重量", "4.2g")),
                Reviews = new List<Review>
                {
                    Review("通勤小李", 5, "降噪确实不错，地铁上戴着安静多了，续航也够用。", 3),
                    Review("阿健", 4, "音质一般，但胜在便宜，日常听听够用。", 9)
                }
            },
            new()
            {
                Name = "机械键盘 87 键", Slug = "机械键盘-87-键",
                Description = "热插拔轴体，RGB 背光，PBT 键帽，办公游戏两相宜。",
                Price = 459.00m, Stock = 90, Sku = "ELEC-002", Shop = digital,
                Categories = new List<Category> { electronics },
                ShippingFrom = "江苏苏州", ShippingFee = "包邮", DeliveryTime = "24小时内发货",
                Skus = new List<ProductSku>
                {
                    new() { Color = "黑色", Spec = "茶轴", Price = 459.00m, Stock = 30 },
                    new() { Color = "黑色", Spec = "红轴", Price = 459.00m, Stock = 25 },
                    new() { Color = "白色", Spec = "茶轴", Price = 479.00m, Stock = 20 },
                    new() { Color = "白色", Spec = "红轴", Price = 479.00m, Stock = 15 }
                },
                Specs = Specs(("配列", "87 键"), ("轴体", "热插拔"), ("背光", "RGB"), ("键帽", "PBT 材质")),
                Reviews = new List<Review> { Review("码字老张", 5, "红轴打字很舒服，键帽手感也不错。", 5) }
            },
            new()
            {
                Name = "27 英寸 2K 显示器", Slug = "27-英寸-2k-显示器",
                Description = "IPS 面板，144Hz 刷新率，广色域，办公游戏都合适。",
                Price = 1299.00m, Stock = 35, Sku = "ELEC-003", Shop = digital,
                Categories = new List<Category> { electronics },
                ShippingFrom = "广东惠州", ShippingFee = "包邮", DeliveryTime = "48小时内发货",
                Specs = Specs(("尺寸", "27 英寸"), ("分辨率", "2560×1440"), ("刷新率", "144Hz"), ("面板", "IPS")),
                Reviews = new List<Review>
                {
                    Review("设计小刘", 5, "色彩很好，做设计够用，性价比高。", 6),
                    Review("老周", 4, "边框稍微有点宽，其他都满意。", 12)
                }
            },
            new()
            {
                Name = "纯棉基础款 T 恤", Slug = "纯棉基础款-t-恤",
                Description = "100% 精梳棉，多色可选，透气舒适，一年四季都能穿。",
                Price = 79.00m, Stock = 300, Sku = "CLOTH-001", Shop = clothing,
                Categories = new List<Category> { fashion },
                ShippingFrom = "浙江杭州", ShippingFee = "包邮", DeliveryTime = "24小时内发货",
                Skus = new List<ProductSku>
                {
                    new() { Color = "白色", Spec = "S", Price = 79.00m, Stock = 60 },
                    new() { Color = "白色", Spec = "M", Price = 79.00m, Stock = 80 },
                    new() { Color = "白色", Spec = "L", Price = 79.00m, Stock = 50 },
                    new() { Color = "黑色", Spec = "M", Price = 79.00m, Stock = 60 },
                    new() { Color = "黑色", Spec = "L", Price = 79.00m, Stock = 30 },
                    new() { Color = "藏青", Spec = "L", Price = 79.00m, Stock = 20 }
                },
                Specs = Specs(("材质", "100% 精梳棉"), ("克重", "180g"), ("版型", "直筒"), ("领型", "圆领")),
                Reviews = new List<Review>
                {
                    Review("老白", 5, "面料舒服，洗了几次也没变形。", 4),
                    Review("小江", 4, "尺码偏小，建议买大一码。", 8)
                }
            },
            new()
            {
                Name = "休闲运动卫衣", Slug = "休闲运动卫衣",
                Description = "宽松版型，加绒加厚，秋冬百搭，运动休闲都合适。",
                Price = 199.00m, Stock = 150, Sku = "CLOTH-002", Shop = clothing,
                Categories = new List<Category> { fashion, sports },
                ShippingFrom = "浙江杭州", ShippingFee = "包邮", DeliveryTime = "24小时内发货",
                Skus = new List<ProductSku>
                {
                    new() { Color = "深灰", Spec = "均码", Price = 199.00m, Stock = 80 },
                    new() { Color = "黑色", Spec = "均码", Price = 199.00m, Stock = 70 }
                },
                Specs = Specs(("材质", "棉 + 涤纶"), ("厚度", "加绒加厚"), ("版型", "宽松")),
                Reviews = new List<Review> { Review("晨跑的小陈", 5, "加绒很暖和，冬天出门跑步正好。", 7) }
            },
            new()
            {
                Name = "《深入理解计算机系统》", Slug = "深入理解计算机系统",
                Description = "计算机科学经典教材（原书第 3 版），程序员进阶必读。",
                Price = 139.00m, Stock = 80, Sku = "BOOK-001", Shop = bookstore,
                Categories = new List<Category> { books },
                ShippingFrom = "北京", ShippingFee = "运费 ¥6", DeliveryTime = "72小时内发货",
                Specs = Specs(("作者", "Randal E. Bryant"), ("出版社", "机械工业出版社"), ("版本", "第 3 版"), ("页数", "776")),
                Reviews = new List<Review>
                {
                    Review("计算机研一", 5, "经典中的经典，学计算机底层必读。", 15),
                    Review("啃书人", 5, "内容很深，需要慢慢啃，收获很大。", 30)
                }
            },
            new()
            {
                Name = "《百年孤独》", Slug = "百年孤独",
                Description = "加西亚·马尔克斯代表作，魔幻现实主义经典。",
                Price = 55.00m, Stock = 200, Sku = "BOOK-002", Shop = bookstore,
                Categories = new List<Category> { books },
                ShippingFrom = "北京", ShippingFee = "运费 ¥6", DeliveryTime = "72小时内发货",
                Specs = Specs(("作者", "加西亚·马尔克斯"), ("出版社", "南海出版公司"), ("装帧", "精装")),
                Reviews = new List<Review> { Review("书虫阿兰", 5, "翻译很流畅，一口气读完，意犹未尽。", 20) }
            },
            new()
            {
                Name = "北欧风陶瓷马克杯", Slug = "北欧风陶瓷马克杯",
                Description = "简约设计，450ml 大容量，可微波加热，日常喝水很合适。",
                Price = 39.90m, Stock = 500, Sku = "HOME-001", Shop = homeStore,
                Categories = new List<Category> { home },
                ShippingFrom = "江西景德镇", ShippingFee = "包邮", DeliveryTime = "48小时内发货",
                Skus = new List<ProductSku>
                {
                    new() { Color = "米白", Spec = "450ml", Price = 39.90m, Stock = 260 },
                    new() { Color = "雾灰", Spec = "450ml", Price = 39.90m, Stock = 240 }
                },
                Specs = Specs(("材质", "陶瓷"), ("容量", "450ml"), ("可微波", "是")),
                Reviews = new List<Review> { Review("阿暖", 5, "手感温润，颜色很治愈，喝水都勤快了。", 11) }
            },
            new()
            {
                Name = "记忆棉护颈枕", Slug = "记忆棉护颈枕",
                Description = "慢回弹，符合人体工学，改善睡眠，枕套可拆洗。",
                Price = 129.00m, Stock = 90, Sku = "HOME-002", Shop = homeStore,
                Categories = new List<Category> { home },
                ShippingFrom = "江苏南通", ShippingFee = "包邮", DeliveryTime = "48小时内发货",
                Specs = Specs(("填充", "记忆棉"), ("尺寸", "50×30×10cm"), ("枕套", "可拆洗")),
                Reviews = new List<Review>
                {
                    Review("落枕星人", 5, "睡了一周，脖子舒服多了。", 6),
                    Review("小美", 4, "回弹有点慢，习惯就好。", 13)
                }
            },
            new()
            {
                Name = "瑜伽垫 加厚防滑", Slug = "瑜伽垫-加厚防滑",
                Description = "10mm 加厚，TPE 环保材质，附赠绑带，居家健身好帮手。",
                Price = 89.00m, Stock = 110, Sku = "SPORT-001", Shop = outdoor,
                Categories = new List<Category> { sports, home },
                ShippingFrom = "广东东莞", ShippingFee = "包邮", DeliveryTime = "48小时内发货",
                Skus = new List<ProductSku>
                {
                    new() { Color = "静谧紫", Spec = "加厚 10mm", Price = 89.00m, Stock = 40 },
                    new() { Color = "雾灰", Spec = "加厚 10mm", Price = 89.00m, Stock = 35 },
                    new() { Color = "抹茶绿", Spec = "加厚 10mm", Price = 89.00m, Stock = 35 }
                },
                Specs = Specs(("厚度", "10mm"), ("材质", "TPE 环保"), ("尺寸", "183×61cm"), ("附赠", "收纳绑带")),
                Reviews = new List<Review> { Review("在家练瑜伽", 5, "防滑不错，做动作很稳，没异味。", 9) }
            },
            new()
            {
                Name = "便携运动水壶", Slug = "便携运动水壶",
                Description = "食品级材质，一按弹盖，750ml，健身出行都方便。",
                Price = 49.00m, Stock = 260, Sku = "SPORT-002", Shop = outdoor,
                Categories = new List<Category> { sports, home },
                ShippingFrom = "广东东莞", ShippingFee = "包邮", DeliveryTime = "48小时内发货",
                Skus = new List<ProductSku>
                {
                    new() { Color = "透明", Spec = "750ml", Price = 49.00m, Stock = 140 },
                    new() { Color = "墨绿", Spec = "750ml", Price = 49.00m, Stock = 120 }
                },
                Specs = Specs(("容量", "750ml"), ("材质", "食品级 Tritan"), ("口径", "广口易清洗")),
                Reviews = new List<Review> { Review("跑者小林", 5, "一按弹盖很方便，装包里不漏水。", 5) }
            },
            new()
            {
                Name = "户外露营帐篷", Slug = "户外露营帐篷",
                Description = "双层防雨，速开设计，3-6 人空间，周末露营首选。",
                Price = 389.00m, Stock = 59, Sku = "SPORT-003", Shop = outdoor,
                Categories = new List<Category> { sports },
                ShippingFrom = "浙江宁波", ShippingFee = "包邮", DeliveryTime = "48小时内发货",
                Skus = new List<ProductSku>
                {
                    new() { Color = "军绿", Spec = "3-4 人", Price = 399.00m, Stock = 15 },
                    new() { Color = "军绿", Spec = "5-6 人", Price = 499.00m, Stock = 10 },
                    new() { Color = "橙色", Spec = "3-4 人", Price = 399.00m, Stock = 8 },
                    new() { Color = "橙色", Spec = "5-6 人", Price = 499.00m, Stock = 6 },
                    new() { Color = "天蓝", Spec = "3-4 人", Price = 389.00m, Stock = 12 },
                    new() { Color = "天蓝", Spec = "5-6 人", Price = 489.00m, Stock = 8 }
                },
                Specs = Specs(("材质", "210T 涤纶"), ("防水", "PU2000mm"), ("人数", "3-6 人"), ("重量", "3.2kg"), ("搭建", "速开")),
                Reviews = new List<Review>
                {
                    Review("露营新手", 5, "周末露营用了一次，搭起来很快，空间也够大。", 2),
                    Review("户外老张", 4, "防水不错，就是收起来要费点劲。", 10),
                    Review("一家三口", 5, "客服很耐心，教我怎么搭，孩子很喜欢。", 18)
                }
            }
        };

        context.Shops.AddRange(shops);
        context.Categories.AddRange(categories);
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static List<ProductSpec> Specs(params (string Name, string Value)[] items)
        => items.Select((s, i) => new ProductSpec { Name = s.Name, Value = s.Value, DisplayOrder = i }).ToList();

    private static Review Review(string userName, int rating, string comment, int daysAgo = 0)
        => new() { UserName = userName, Rating = rating, Comment = comment, CreatedAt = DateTime.UtcNow.AddDays(-daysAgo) };
}
