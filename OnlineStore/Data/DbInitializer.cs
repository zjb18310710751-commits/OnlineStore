using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OnlineStore.Models;

namespace OnlineStore.Data;

public static class DbInitializer
{
    private const string AdminRole = "Admin";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));
        var options = services.GetRequiredService<IOptions<InitializationOptions>>().Value;
        var attempts = Math.Max(1, options.InitializationRetryCount);

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                await InitializeOnceAsync(services);
                return;
            }
            catch (Exception ex) when (attempt < attempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Max(0, options.InitializationRetryDelaySeconds));
                logger.LogWarning(
                    ex,
                    "Database initialization attempt {Attempt}/{Attempts} failed; retrying in {DelaySeconds} seconds.",
                    attempt,
                    attempts,
                    delay.TotalSeconds);
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay);
                }
            }
        }
    }

    private static async Task InitializeOnceAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<InitializationOptions>>().Value;

        if (options.ApplyMigrationsOnStartup)
        {
            await context.Database.MigrateAsync();
        }

        await SeedAsync(context, userManager, roleManager, options);
    }

    private static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        InitializationOptions options)
    {

        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(AdminRole));
        }

        var adminEmail = options.AdminEmail?.Trim();
        if (!string.IsNullOrWhiteSpace(adminEmail)
            && !string.IsNullOrWhiteSpace(options.AdminPassword)
            && await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, options.AdminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AdminRole);
            }
        }

        await BackfillContentTranslationsAsync(context);

        if (await context.Categories.AnyAsync())
        {
            if (options.SeedDemoData)
            {
                await SeedDemoOrdersAsync(context, userManager, options);
            }
            return;
        }

        if (!options.SeedSampleCatalogOnEmpty)
        {
            return;
        }

        var outdoor = new Shop { Name = "山野户外旗舰店", NameEn = "Mountain Outdoors Flagship Store", Rating = 4.9m, SalesCount = 12800, Description = "专注户外装备 12 年，只卖靠谱的。", DescriptionEn = "12 years of outdoor gear expertise — only dependable gear." };
        var digital = new Shop { Name = "极客数码自营店", NameEn = "Geek Digital Store", Rating = 4.8m, SalesCount = 8600, Description = "正品数码，官方直营。", DescriptionEn = "Genuine digital products, direct from official channels." };
        var bookstore = new Shop { Name = "悦读图书专营店", NameEn = "Joyful Reading Bookstore", Rating = 4.9m, SalesCount = 32000, Description = "好书，认真选每一本。", DescriptionEn = "Good books, carefully selected." };
        var homeStore = new Shop { Name = "温暖家居生活馆", NameEn = "Warm Home Living", Rating = 4.7m, SalesCount = 5600, Description = "把日子过得舒服一点。", DescriptionEn = "Make everyday life a little more comfortable." };
        var clothing = new Shop { Name = "简素服饰官方店", NameEn = "Simple Fashion Official Store", Rating = 4.8m, SalesCount = 21000, Description = "基础款，耐穿好搭。", DescriptionEn = "Basics that last and match everything." };

        var shops = new List<Shop> { outdoor, digital, bookstore, homeStore, clothing };

        var electronics = new Category { Name = "电子产品", NameEn = "Electronics", Slug = "电子产品", Description = "手机、电脑、数码配件", DescriptionEn = "Phones, computers and digital accessories", DisplayOrder = 1 };
        var fashion = new Category { Name = "服装服饰", NameEn = "Fashion", Slug = "服装服饰", Description = "男女装、鞋帽配饰", DescriptionEn = "Men's and women's clothing, shoes and accessories", DisplayOrder = 2 };
        var books = new Category { Name = "图书", NameEn = "Books", Slug = "图书", Description = "文学、科技、教育", DescriptionEn = "Literature, technology and education", DisplayOrder = 3 };
        var home = new Category { Name = "家居生活", NameEn = "Home & Living", Slug = "家居生活", Description = "家纺、厨具、日用", DescriptionEn = "Textiles, kitchenware and daily necessities", DisplayOrder = 4 };
        var sports = new Category { Name = "运动户外", NameEn = "Sports & Outdoors", Slug = "运动户外", Description = "健身、户外装备", DescriptionEn = "Fitness and outdoor gear", DisplayOrder = 5 };

        var categories = new List<Category> { electronics, fashion, books, home, sports };

        var products = new List<Product>
        {
            new()
            {
                Name = "无线蓝牙耳机", NameEn = "Wireless Bluetooth Earbuds", Slug = "无线蓝牙耳机",
                Description = "主动降噪，30 小时续航，支持快充，通勤运动都好用。", DescriptionEn = "Active noise cancelling, 30h battery, fast charging — great for commute and workouts.",
                Price = 299.00m, OriginalPrice = 399.00m, Stock = 130, Sku = "ELEC-001", Shop = digital,
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
                Name = "机械键盘 87 键", NameEn = "Mechanical Keyboard 87-Key", Slug = "机械键盘-87-键",
                Description = "热插拔轴体，RGB 背光，PBT 键帽，办公游戏两相宜。", DescriptionEn = "Hot-swappable switches, RGB backlight, PBT keycaps — great for work and gaming.",
                Price = 459.00m, OriginalPrice = 549.00m, Stock = 90, Sku = "ELEC-002", Shop = digital,
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
                Name = "27 英寸 2K 显示器", NameEn = "27\" 2K Monitor", Slug = "27-英寸-2k-显示器",
                Description = "IPS 面板，144Hz 刷新率，广色域，办公游戏都合适。", DescriptionEn = "IPS panel, 144Hz refresh, wide color gamut — great for work and gaming.",
                Price = 1299.00m, OriginalPrice = 1599.00m, Stock = 35, Sku = "ELEC-003", Shop = digital,
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
                Name = "纯棉基础款 T 恤", NameEn = "Cotton Basic T-Shirt", Slug = "纯棉基础款-t-恤",
                Description = "100% 精梳棉，多色可选，透气舒适，一年四季都能穿。", DescriptionEn = "100% combed cotton, breathable and comfortable — all-season wear.",
                Price = 79.00m, OriginalPrice = 99.00m, Stock = 300, Sku = "CLOTH-001", Shop = clothing,
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
                Name = "休闲运动卫衣", NameEn = "Casual Sports Hoodie", Slug = "休闲运动卫衣",
                Description = "宽松版型，加绒加厚，秋冬百搭，运动休闲都合适。", DescriptionEn = "Loose fit, fleece-lined and thick — perfect for fall and winter.",
                Price = 199.00m, OriginalPrice = 259.00m, Stock = 150, Sku = "CLOTH-002", Shop = clothing,
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
                Name = "《深入理解计算机系统》", NameEn = "Computer Systems: A Programmer's Perspective", Slug = "深入理解计算机系统",
                Description = "计算机科学经典教材（原书第 3 版），程序员进阶必读。", DescriptionEn = "The classic CS textbook (3rd edition) — a must-read for every programmer.",
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
                Name = "《百年孤独》", NameEn = "One Hundred Years of Solitude", Slug = "百年孤独",
                Description = "加西亚·马尔克斯代表作，魔幻现实主义经典。", DescriptionEn = "Gabriel García Márquez's masterpiece of magical realism.",
                Price = 55.00m, Stock = 200, Sku = "BOOK-002", Shop = bookstore,
                Categories = new List<Category> { books },
                ShippingFrom = "北京", ShippingFee = "运费 ¥6", DeliveryTime = "72小时内发货",
                Specs = Specs(("作者", "加西亚·马尔克斯"), ("出版社", "南海出版公司"), ("装帧", "精装")),
                Reviews = new List<Review> { Review("书虫阿兰", 5, "翻译很流畅，一口气读完，意犹未尽。", 20) }
            },
            new()
            {
                Name = "北欧风陶瓷马克杯", NameEn = "Nordic Ceramic Mug", Slug = "北欧风陶瓷马克杯",
                Description = "简约设计，450ml 大容量，可微波加热，日常喝水很合适。", DescriptionEn = "Minimalist design, 450ml capacity, microwave safe.",
                Price = 39.90m, OriginalPrice = 49.90m, Stock = 500, Sku = "HOME-001", Shop = homeStore,
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
                Name = "记忆棉护颈枕", NameEn = "Memory Foam Neck Pillow", Slug = "记忆棉护颈枕",
                Description = "慢回弹，符合人体工学，改善睡眠，枕套可拆洗。", DescriptionEn = "Slow rebound, ergonomic, improves sleep; removable washable cover.",
                Price = 129.00m, OriginalPrice = 159.00m, Stock = 90, Sku = "HOME-002", Shop = homeStore,
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
                Name = "瑜伽垫 加厚防滑", NameEn = "Yoga Mat Extra Thick Non-Slip", Slug = "瑜伽垫-加厚防滑",
                Description = "10mm 加厚，TPE 环保材质，附赠绑带，居家健身好帮手。", DescriptionEn = "10mm thick, TPE eco-friendly material, with carrying strap.",
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
                Name = "便携运动水壶", NameEn = "Portable Sports Water Bottle", Slug = "便携运动水壶",
                Description = "食品级材质，一按弹盖，750ml，健身出行都方便。", DescriptionEn = "Food-grade material, one-press flip lid, 750ml.",
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
                Name = "户外露营帐篷", NameEn = "Outdoor Camping Tent", Slug = "户外露营帐篷",
                Description = "双层防雨，速开设计，3-6 人空间，周末露营首选。", DescriptionEn = "Double-layer waterproof, quick setup, fits 3-6 people.",
                Price = 389.00m, OriginalPrice = 499.00m, Stock = 59, Sku = "SPORT-003", Shop = outdoor,
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
                    Review("露营新手", 5, "周末露营用了一次，搭起来很快，空间也够大。", 2, "https://picsum.photos/seed/SPORT-003-review/400/400"),
                    Review("户外老张", 4, "防水不错，就是收起来要费点劲。", 10),
                    Review("一家三口", 5, "客服很耐心，教我怎么搭，孩子很喜欢。", 18)
                }
            }
        };

        var extraItems = new (string Name, string NameEn, Category Category, Shop Shop, decimal Price)[]
        {
            ("手机支架", "Phone Holder", electronics, digital, 19.90m), ("三合一数据线", "3-in-1 Data Cable", electronics, digital, 29.00m),
            ("蓝牙自拍杆", "Bluetooth Selfie Stick", electronics, digital, 39.00m), ("无线充电器", "Wireless Charger", electronics, digital, 89.00m),
            ("移动电源 20000mAh", "Power Bank 20000mAh", electronics, digital, 129.00m), ("键鼠套装", "Keyboard & Mouse Combo", electronics, digital, 129.00m),
            ("显示器支架", "Monitor Stand", electronics, digital, 149.00m), ("蓝牙音箱", "Bluetooth Speaker", electronics, digital, 159.00m),
            ("电竞鼠标", "Gaming Mouse", electronics, digital, 199.00m), ("路由器", "Router", electronics, digital, 199.00m),
            ("游戏手柄", "Game Controller", electronics, digital, 269.00m), ("智能音箱", "Smart Speaker", electronics, digital, 299.00m),
            ("机械硬盘 2TB", "HDD 2TB", electronics, digital, 499.00m), ("智能手表", "Smartwatch", electronics, digital, 599.00m),
            ("固态硬盘 1TB", "SSD 1TB", electronics, digital, 699.00m), ("电纸书阅读器", "E-Reader", electronics, digital, 899.00m),
            ("投影仪", "Projector", electronics, digital, 1899.00m), ("平板电脑", "Tablet", electronics, digital, 2499.00m),
            ("无人机", "Drone", electronics, digital, 2999.00m), ("微单相机", "Mirrorless Camera", electronics, digital, 3999.00m),
            ("棉袜五双装", "Cotton Socks (5-Pack)", fashion, clothing, 29.00m), ("棒球帽", "Baseball Cap", fashion, clothing, 39.00m),
            ("围巾", "Scarf", fashion, clothing, 49.00m), ("短裤", "Shorts", fashion, clothing, 59.00m),
            ("皮带", "Belt", fashion, clothing, 69.00m), ("睡衣", "Pajamas", fashion, clothing, 89.00m),
            ("帆布鞋", "Canvas Shoes", fashion, clothing, 99.00m), ("泳衣", "Swimsuit", fashion, clothing, 99.00m),
            ("衬衫", "Shirt", fashion, clothing, 129.00m), ("双肩包", "Backpack", fashion, clothing, 129.00m),
            ("牛仔裤", "Jeans", fashion, clothing, 159.00m), ("毛衣", "Sweater", fashion, clothing, 179.00m),
            ("连衣裙", "Dress", fashion, clothing, 199.00m), ("运动鞋", "Sneakers", fashion, clothing, 299.00m),
            ("皮鞋", "Leather Shoes", fashion, clothing, 299.00m), ("风衣", "Trench Coat", fashion, clothing, 399.00m),
            ("冲锋衣", "Outdoor Jacket", fashion, clothing, 499.00m), ("羽绒服", "Down Jacket", fashion, clothing, 599.00m),
            ("《小王子》", "\"The Little Prince\"", books, bookstore, 19.00m), ("英语词汇书", "English Vocabulary Book", books, bookstore, 35.00m),
            ("《活着》", "\"To Live\"", books, bookstore, 39.00m), ("《白夜行》", "\"Journey Under the Midnight Sun\"", books, bookstore, 39.50m),
            ("《时间简史》", "\"A Brief History of Time\"", books, bookstore, 45.00m), ("《红楼梦》", "\"Dream of the Red Chamber\"", books, bookstore, 49.00m),
            ("《平凡的世界》", "\"Ordinary World\"", books, bookstore, 59.00m), ("《人类简史》", "\"Sapiens\"", books, bookstore, 68.00m),
            ("《经济学原理》", "\"Principles of Economics\"", books, bookstore, 98.00m), ("《算法导论》", "\"Introduction to Algorithms\"", books, bookstore, 128.00m),
            ("儿童绘本套装", "Children's Picture Book Set", books, bookstore, 129.00m), ("《三体》全集", "\"The Three-Body Problem\" Trilogy", books, bookstore, 168.00m),
            ("花瓶", "Vase", home, homeStore, 29.00m), ("香薰蜡烛", "Scented Candle", home, homeStore, 39.00m),
            ("抱枕", "Throw Pillow", home, homeStore, 39.00m), ("毛巾三件套", "Towel Set (3-Pack)", home, homeStore, 49.00m),
            ("收纳箱三件套", "Storage Box Set (3-Pack)", home, homeStore, 59.00m), ("挂钟", "Wall Clock", home, homeStore, 69.00m),
            ("保温杯", "Thermos", home, homeStore, 89.00m), ("电热水壶", "Electric Kettle", home, homeStore, 99.00m),
            ("落地灯", "Floor Lamp", home, homeStore, 159.00m), ("刀具套装", "Knife Set", home, homeStore, 159.00m),
            ("不锈钢锅", "Stainless Steel Pot", home, homeStore, 199.00m), ("窗帘", "Curtains", home, homeStore, 199.00m),
            ("床品四件套", "Bedding Set (4-Pack)", home, homeStore, 299.00m), ("电饭煲", "Rice Cooker", home, homeStore, 299.00m),
            ("地毯", "Rug", home, homeStore, 399.00m), ("咖啡机", "Coffee Maker", home, homeStore, 599.00m),
            ("空气净化器", "Air Purifier", home, homeStore, 899.00m), ("吸尘器", "Vacuum Cleaner", home, homeStore, 1299.00m),
            ("扫地机器人", "Robot Vacuum", home, homeStore, 1499.00m), ("智能马桶盖", "Smart Toilet Seat", home, homeStore, 1999.00m),
            ("跳绳", "Jump Rope", sports, outdoor, 19.00m), ("冰袖", "Cooling Arm Sleeves", sports, outdoor, 19.00m),
            ("泳镜", "Swim Goggles", sports, outdoor, 29.00m), ("护膝", "Knee Brace", sports, outdoor, 39.00m),
            ("帐篷灯", "Tent Lamp", sports, outdoor, 39.00m), ("野餐垫", "Picnic Mat", sports, outdoor, 49.00m),
            ("瑜伽球", "Yoga Ball", sports, outdoor, 59.00m), ("登山杖", "Trekking Poles", sports, outdoor, 59.00m),
            ("折叠椅", "Folding Chair", sports, outdoor, 79.00m), ("篮球", "Basketball", sports, outdoor, 89.00m),
            ("足球", "Soccer Ball", sports, outdoor, 99.00m), ("羽毛球拍", "Badminton Racket", sports, outdoor, 99.00m),
            ("骑行头盔", "Bike Helmet", sports, outdoor, 129.00m), ("哑铃", "Dumbbell", sports, outdoor, 129.00m),
            ("望远镜", "Binoculars", sports, outdoor, 259.00m), ("筋膜枪", "Massage Gun", sports, outdoor, 399.00m),
            ("山地自行车", "Mountain Bike", sports, outdoor, 1299.00m), ("跑步机", "Treadmill", sports, outdoor, 1999.00m)
        };

        for (var i = 0; i < extraItems.Length; i++)
        {
            var (name, nameEn, category, shop, price) = extraItems[i];
            var stock = Random.Shared.Next(30, 500);
            var sku = $"EXT-{i + 1:000}";
            var originalPrice = Random.Shared.NextDouble() < 0.25
                ? Math.Round(price * (decimal)(1.1 + Random.Shared.NextDouble() * 0.4), 2)
                : (decimal?)null;
            products.Add(new Product
            {
                Name = name,
                NameEn = nameEn,
                Slug = name,
                Description = BuildDescription(name),
                DescriptionEn = BuildDescriptionEn(nameEn),
                Price = price,
                OriginalPrice = originalPrice,
                Stock = stock,
                Sku = sku,
                Shop = shop,
                Categories = new List<Category> { category },
                ShippingFrom = PickOne("广东深圳", "浙江杭州", "江苏苏州", "上海", "北京"),
                ShippingFee = "包邮",
                DeliveryTime = PickOne("24小时内发货", "48小时内发货", "72小时内发货"),
                Specs = BuildExtraSpecs(category.Name),
                Reviews = BuildExtraReviews(sku),
                Skus = ShouldHaveSku(category.Name) ? BuildExtraSkus(category.Name, price, stock) : new List<ProductSku>()
            });
        }

        foreach (var p in products)
        {
            p.ImageUrl = $"https://picsum.photos/seed/{p.Sku}/600/400";
            var images = new List<ProductImage>
            {
                new() { ImageUrl = p.ImageUrl, SortOrder = 0 },
                new() { ImageUrl = $"https://picsum.photos/seed/{p.Sku}-b/600/400", SortOrder = 1 },
                new() { ImageUrl = $"https://picsum.photos/seed/{p.Sku}-c/600/400", SortOrder = 2 }
            };
            p.Images = images;
            p.DetailHtml = BuildDetailHtml(p.Name, p.Description ?? "", images[0].ImageUrl, images[1].ImageUrl);
            p.DetailHtmlEn = BuildDetailHtmlEn(p.NameEn ?? p.Name, p.DescriptionEn ?? p.Description ?? "", images[0].ImageUrl, images[1].ImageUrl);

            var skuIndex = 0;
            foreach (var sku in p.Skus)
            {
                sku.ImageUrl = $"https://picsum.photos/seed/{p.Sku}-s{skuIndex++}/600/400";
            }
        }

        ApplyEnglishTranslations(products);

        var ukOrigins = new[]
        {
            ("伦敦", "London"),
            ("曼彻斯特", "Manchester"),
            ("伯明翰", "Birmingham"),
            ("利兹", "Leeds"),
            ("格拉斯哥", "Glasgow"),
            ("布里斯托尔", "Bristol"),
            ("利物浦", "Liverpool"),
            ("爱丁堡", "Edinburgh"),
            ("谢菲尔德", "Sheffield"),
            ("卡迪夫", "Cardiff")
        };
        for (var i = 0; i < products.Count; i++)
        {
            products[i].ShippingFrom = ukOrigins[i % ukOrigins.Length].Item1;
            products[i].ShippingFromEn = ukOrigins[i % ukOrigins.Length].Item2;
        }

        context.Shops.AddRange(shops);
        context.Categories.AddRange(categories);
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        if (options.SeedDemoData)
        {
            await SeedDemoOrdersAsync(context, userManager, options);
        }
    }

    private static async Task SeedDemoOrdersAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        InitializationOptions options)
    {
        var demoBuyerEmail = options.DemoBuyerEmail?.Trim();
        var demoBuyerPassword = options.DemoBuyerPassword;
        if (string.IsNullOrWhiteSpace(demoBuyerEmail)
            || string.IsNullOrWhiteSpace(demoBuyerPassword))
        {
            return;
        }

        var buyer = await userManager.FindByEmailAsync(demoBuyerEmail);
        if (buyer is null)
        {
            buyer = new ApplicationUser
            {
                UserName = demoBuyerEmail,
                Email = demoBuyerEmail,
                EmailConfirmed = true,
                FirstName = "Demo",
                LastName = "Buyer",
                PhoneNumber = "13800138000"
            };
            var createResult = await userManager.CreateAsync(buyer, demoBuyerPassword);
            if (!createResult.Succeeded)
            {
                return;
            }
        }

        await BackfillDemoShippingSnapshotsAsync(context);

        if (await context.Orders.AnyAsync(o => o.OrderNo.StartsWith("DEMO-")))
        {
            return;
        }

        var products = await context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .Take(5)
            .ToListAsync();
        if (products.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var orders = new List<Order>
        {
            DemoOrder("DEMO-1001", OrderStatus.PendingShipment, now.AddHours(-1), products[0], 1, buyer.Id),
            DemoOrder("DEMO-1002", OrderStatus.Shipped, now.AddHours(-8), products[1 % products.Count], 2, buyer.Id),
            DemoOrder("DEMO-1003", OrderStatus.Delivered, now.AddHours(-30), products[2 % products.Count], 1, buyer.Id),
            DemoOrder("DEMO-1004", OrderStatus.Completed, now.AddDays(-3), products[3 % products.Count], 1, buyer.Id),
            DemoOrder("DEMO-1005", OrderStatus.Completed, now.AddDays(-7), products[4 % products.Count], 2, buyer.Id)
        };

        var shipped = orders[1];
        shipped.Carrier = "顺丰速运";
        shipped.TrackingNumber = "SF1234567890123";
        shipped.ShippedAt = now.AddSeconds(-10);

        var delivered = orders[2];
        delivered.Carrier = "中通快递";
        delivered.TrackingNumber = "ZT9876543210987";
        delivered.ShippedAt = now.AddHours(-20);
        delivered.DeliveredAt = now.AddHours(-2);

        orders[3].Carrier = "邮政 EMS";
        orders[3].TrackingNumber = "EMS202610260001";
        orders[3].ShippedAt = now.AddDays(-4);
        orders[3].DeliveredAt = now.AddDays(-2);
        orders[3].SignedAt = now.AddDays(-2).AddHours(-1);

        orders[4].Carrier = "圆通速递";
        orders[4].TrackingNumber = "YT202610100002";
        orders[4].ShippedAt = now.AddDays(-8);
        orders[4].DeliveredAt = now.AddDays(-6);
        orders[4].SignedAt = now.AddDays(-6).AddHours(-2);
        orders[4].AfterSalesRequests.Add(new AfterSalesRequest
        {
            UserId = buyer.Id,
            Type = AfterSalesType.Exchange,
            Reason = "商品外包装破损，申请换货。",
            Status = AfterSalesStatus.Approved,
            RequestedAt = now.AddDays(-1),
            ReviewedAt = now.AddHours(-6)
        });

        orders[3].AfterSalesRequests.Add(new AfterSalesRequest
        {
            UserId = buyer.Id,
            Type = AfterSalesType.Return,
            Reason = "收到的商品与预期不符，申请退货。",
            Status = AfterSalesStatus.Pending,
            RequestedAt = now.AddHours(-12)
        });

        context.Orders.AddRange(orders);
        await context.SaveChangesAsync();
    }

    private static Order DemoOrder(
        string orderNo,
        string status,
        DateTime createdAt,
        Product product,
        int quantity,
        string userId)
    {
        var total = product.Price * quantity;
        return new Order
        {
            OrderNo = orderNo,
            UserId = userId,
            UserName = "Demo Buyer",
            Phone = "13800138000",
            Address = "广东省深圳市南山区科技园, 518000, 中国",
            Total = total,
            Status = status,
            CreatedAt = createdAt,
            Items = new List<OrderItem>
            {
                new()
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductNameEn = product.NameEn,
                    Price = product.Price,
                    Quantity = quantity
                }
            }
        };
    }

    private static async Task BackfillDemoShippingSnapshotsAsync(ApplicationDbContext context)
    {
        var orders = await context.Orders
            .Include(o => o.Items)
            .Where(o => o.OrderNo.StartsWith("DEMO-") && o.ShippingOrigin == null)
            .ToListAsync();
        if (orders.Count == 0)
        {
            return;
        }

        var productIds = orders.SelectMany(o => o.Items).Select(i => i.ProductId).Distinct().ToList();
        var products = await context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var order in orders)
        {
            var productId = order.Items.Select(i => i.ProductId).FirstOrDefault();
            if (!products.TryGetValue(productId, out var product))
            {
                continue;
            }

            order.ShippingOrigin = product.ShippingFrom;
            order.ShippingOriginEn = product.ShippingFromEn;
            order.ShippingFeeText = product.ShippingFee;
            order.ShippingFeeTextEn = product.ShippingFeeEn;
            order.DispatchTimeText = product.DeliveryTime;
            order.DispatchTimeTextEn = product.DeliveryTimeEn;

            var hours = product.DeliveryTime?.Contains("24") == true ? 24
                : product.DeliveryTime?.Contains("48") == true ? 48
                : 72;
            order.ExpectedDispatchAt = order.CreatedAt.AddHours(hours);
        }

        await context.SaveChangesAsync();
    }

    private static readonly Dictionary<string, (string NameEn, string DescriptionEn)> CategoryTranslations = new()
    {
        ["电子产品"] = ("Electronics", "Phones, computers and digital accessories"),
        ["服装服饰"] = ("Fashion", "Men's and women's clothing, shoes and accessories"),
        ["图书"] = ("Books", "Literature, technology and education"),
        ["家居生活"] = ("Home & Living", "Textiles, kitchenware and daily necessities"),
        ["运动户外"] = ("Sports & Outdoors", "Fitness and outdoor gear")
    };

    private static readonly Dictionary<string, (string NameEn, string DescriptionEn)> ShopTranslations = new()
    {
        ["山野户外旗舰店"] = ("Mountain Outdoors Flagship Store", "12 years of outdoor gear expertise — only dependable gear."),
        ["极客数码自营店"] = ("Geek Digital Store", "Genuine digital products, direct from official channels."),
        ["悦读图书专营店"] = ("Joyful Reading Bookstore", "Good books, carefully selected."),
        ["温暖家居生活馆"] = ("Warm Home Living", "Make everyday life a little more comfortable."),
        ["简素服饰官方店"] = ("Simple Fashion Official Store", "Basics that last and match everything.")
    };

    private static async Task BackfillContentTranslationsAsync(ApplicationDbContext context)
    {

        var categories = await context.Categories
            .Where(c => c.NameEn == null || c.DescriptionEn == null)
            .ToListAsync();
        foreach (var category in categories)
        {
            if (CategoryTranslations.TryGetValue(category.Name, out var translation))
            {
                category.NameEn ??= translation.NameEn;
                category.DescriptionEn ??= translation.DescriptionEn;
            }
        }

        var shops = await context.Shops.ToListAsync();
        foreach (var shop in shops)
        {
            if (ShopTranslations.TryGetValue(shop.Name, out var translation))
            {
                shop.NameEn ??= translation.NameEn;
                shop.DescriptionEn ??= translation.DescriptionEn;
            }
        }

        var products = await context.Products
            .Include(p => p.Specs)
            .Include(p => p.Skus)
            .Include(p => p.Reviews)
            .ToListAsync();
        ApplyEnglishTranslations(products);

        await context.SaveChangesAsync();
    }

    private static string PickOne(params string[] options) =>
        options[Random.Shared.Next(options.Length)];

    private static string BuildDescription(string name) =>
        PickOne(
            $"{name}，精选好物，品质有保证。",
            $"{name}，人气之选，买了不后悔。",
            $"{name}，实用耐用，日常必备。",
            $"{name}，口碑很好，回购率高。");

    private static string BuildDescriptionEn(string name) =>
        PickOne(
            $"{name}, a quality pick you can count on.",
            $"{name}, a popular choice you won't regret.",
            $"{name}, practical and durable for everyday use.",
            $"{name}, well-reviewed and loved by repeat buyers.");

    private static string BuildDetailHtml(string name, string description, string img1, string img2) =>
        $"<p>{description}</p>" +
        $"<p><img src=\"{img1}\" class=\"img-fluid rounded\" alt=\"{name}\" /></p>" +
        "<p>精选材质，做工扎实，细节到位。支持七天无理由退换，买得放心。</p>" +
        $"<p><img src=\"{img2}\" class=\"img-fluid rounded\" alt=\"{name}\" /></p>";

    private static string BuildDetailHtmlEn(string name, string description, string img1, string img2) =>
        $"<p>{description}</p>" +
        $"<p><img src=\"{img1}\" class=\"img-fluid rounded\" alt=\"{name}\" /></p>" +
        "<p>Carefully selected materials and solid craftsmanship. 7-day no-reason returns for peace of mind.</p>" +
        $"<p><img src=\"{img2}\" class=\"img-fluid rounded\" alt=\"{name}\" /></p>";

    private static List<ProductSpec> BuildExtraSpecs(string categoryName)
    {
        var pool = categoryName switch
        {
            "电子产品" => new (string, string)[] { ("品牌", "知名品牌"), ("质保", "一年"), ("接口", "USB-C"), ("续航", "长续航"), ("兼容", "全兼容"), ("材质", "铝合金") },
            "服装服饰" => new (string, string)[] { ("面料", "纯棉"), ("版型", "标准"), ("洗涤", "可机洗"), ("季节", "四季"), ("弹性", "微弹"), ("领型", "圆领") },
            "图书" => new (string, string)[] { ("装帧", "平装"), ("语言", "中文"), ("出版社", "正规出版"), ("版次", "第 1 版"), ("纸张", "胶版纸"), ("页数", "300 页左右") },
            "家居生活" => new (string, string)[] { ("材质", "优质耐用"), ("风格", "简约"), ("清洁", "易清洁"), ("容量", "适中"), ("尺寸", "标准"), ("产地", "中国") },
            _ => new (string, string)[] { ("材质", "耐磨耐用"), ("重量", "轻便"), ("场景", "户外"), ("防水", "防泼水"), ("防滑", "防滑设计"), ("承重", "承重强") }
        };

        var picked = pool.OrderBy(_ => Random.Shared.Next()).Take(Random.Shared.Next(3, 5)).ToArray();
        return Specs(picked);
    }

    private static List<Review> BuildExtraReviews(string sku)
    {
        var count = Random.Shared.Next(1, 3);
        var reviews = new List<Review>();
        for (var i = 0; i < count; i++)
        {
            var hasImage = Random.Shared.NextDouble() < 0.4;
            reviews.Add(Review(
                PickOne("老王", "小美", "阿杰", "静静", "大熊", "路过的风", "阿蓝", "小鹿", "爱吃鱼", "老张头"),
                Random.Shared.Next(4, 6),
                PickOne(
                    "质量不错，做工精细，值得回购。",
                    "性价比很高，用了几天很满意。",
                    "物流快，包装严实，没有破损。",
                    "和描述一致，用起来很顺手。",
                    "已经第二次买了，家里人都说好。",
                    "客服态度很好，有问题及时解决。",
                    "价格实惠，日常用足够了。",
                    "做工可以，就是发货稍慢，总体满意。",
                    "买来送人的，对方很喜欢。",
                    "东西是好东西，就是包装简单了点。"),
                Random.Shared.Next(1, 60),
                hasImage ? $"https://picsum.photos/seed/{sku}-r{i}/400/400" : null));
        }

        return reviews;
    }

    private static bool ShouldHaveSku(string categoryName) =>
        categoryName switch
        {
            "图书" => false,
            "家居生活" => Random.Shared.NextDouble() < 0.3,
            _ => Random.Shared.NextDouble() < 0.6
        };

    private static List<ProductSku> BuildExtraSkus(string categoryName, decimal basePrice, int stock)
    {
        var (colors, specs) = categoryName switch
        {
            "电子产品" => (new[] { "黑色", "白色", "银色", "深空灰" }, new[] { "标配", "高配" }),
            "服装服饰" => (new[] { "黑色", "白色", "灰色", "藏青", "卡其" }, new[] { "S", "M", "L", "XL" }),
            "运动户外" => (new[] { "黑色", "军绿", "橙色", "蓝色" }, new[] { "标准", "加大" }),
            _ => (new[] { "米白", "雾灰", "深灰" }, new[] { "标准款" })
        };

        var usedColors = colors.OrderBy(_ => Random.Shared.Next()).Take(Random.Shared.Next(2, 4)).ToArray();
        var usedSpecs = specs.OrderBy(_ => Random.Shared.Next()).Take(Random.Shared.Next(1, 3)).ToArray();

        var skus = new List<ProductSku>();
        foreach (var color in usedColors)
        {
            foreach (var spec in usedSpecs)
            {
                var delta = Random.Shared.Next(-5, 11);
                skus.Add(new ProductSku
                {
                    Color = color,
                    Spec = spec,
                    Price = Math.Max(1m, basePrice + delta),
                    Stock = Math.Max(3, stock / (usedColors.Length * usedSpecs.Length))
                });
            }
        }

        return skus;
    }

    private static List<ProductSpec> Specs(params (string Name, string Value)[] items)
        => items.Select((s, i) => new ProductSpec { Name = s.Name, Value = s.Value, DisplayOrder = i }).ToList();

    private static Review Review(string userName, int rating, string comment, int daysAgo = 0, string? imageUrl = null)
        => new() { UserName = userName, Rating = rating, Comment = comment, ImageUrl = imageUrl, CreatedAt = DateTime.UtcNow.AddDays(-daysAgo) };

    private static void ApplyEnglishTranslations(List<Product> products)
    {
        var specName = new Dictionary<string, string>
        {
            ["蓝牙版本"] = "Bluetooth Version", ["续航"] = "Battery Life", ["降噪"] = "Noise Cancelling",
            ["单耳重量"] = "Weight per Bud", ["配列"] = "Layout", ["轴体"] = "Switch Type", ["背光"] = "Backlight",
            ["键帽"] = "Keycaps", ["尺寸"] = "Size", ["分辨率"] = "Resolution", ["刷新率"] = "Refresh Rate",
            ["面板"] = "Panel", ["材质"] = "Material", ["克重"] = "Weight", ["版型"] = "Fit", ["领型"] = "Collar",
            ["厚度"] = "Thickness", ["作者"] = "Author", ["出版社"] = "Publisher", ["版本"] = "Edition",
            ["页数"] = "Pages", ["装帧"] = "Binding", ["容量"] = "Capacity", ["可微波"] = "Microwave Safe",
            ["填充"] = "Filling", ["枕套"] = "Cover", ["附赠"] = "Included", ["人数"] = "Capacity",
            ["重量"] = "Weight", ["搭建"] = "Setup", ["防水"] = "Waterproof", ["品牌"] = "Brand",
            ["质保"] = "Warranty", ["接口"] = "Interface", ["兼容"] = "Compatibility", ["面料"] = "Fabric",
            ["洗涤"] = "Care", ["季节"] = "Season", ["弹性"] = "Stretch", ["语言"] = "Language",
            ["版次"] = "Edition", ["纸张"] = "Paper", ["风格"] = "Style", ["清洁"] = "Care",
            ["产地"] = "Origin", ["场景"] = "Use Case", ["防滑"] = "Non-slip", ["承重"] = "Load Capacity"
        };

        var specValue = new Dictionary<string, string>
        {
            ["主动降噪"] = "Active Noise Cancelling", ["30 小时"] = "30 Hours", ["热插拔"] = "Hot-swappable",
            ["PBT 材质"] = "PBT Material", ["100% 精梳棉"] = "100% Combed Cotton", ["直筒"] = "Straight",
            ["圆领"] = "Crew Neck", ["棉 + 涤纶"] = "Cotton + Polyester", ["加绒加厚"] = "Fleece-lined",
            ["宽松"] = "Loose", ["陶瓷"] = "Ceramic", ["可拆洗"] = "Removable & Washable",
            ["记忆棉"] = "Memory Foam", ["精装"] = "Hardcover", ["第 3 版"] = "3rd Edition",
            ["机械工业出版社"] = "China Machine Press", ["南海出版公司"] = "Nan Hai Publishing",
            ["210T 涤纶"] = "210T Polyester", ["速开"] = "Quick Setup", ["是"] = "Yes",
            ["知名品牌"] = "Well-known Brand", ["一年"] = "1 Year", ["长续航"] = "Long Battery Life",
            ["全兼容"] = "Universal Compatibility", ["铝合金"] = "Aluminum Alloy", ["纯棉"] = "Cotton",
            ["标准"] = "Standard", ["可机洗"] = "Machine Washable", ["四季"] = "All Seasons",
            ["微弹"] = "Slight Stretch", ["平装"] = "Paperback", ["中文"] = "Chinese",
            ["正规出版"] = "Official Publication", ["第 1 版"] = "1st Edition", ["胶版纸"] = "Offset Paper",
            ["300 页左右"] = "~300 Pages", ["优质耐用"] = "Durable", ["简约"] = "Minimalist",
            ["易清洁"] = "Easy to Clean", ["适中"] = "Medium", ["中国"] = "China", ["耐磨耐用"] = "Wear-resistant",
            ["轻便"] = "Lightweight", ["户外"] = "Outdoor", ["防泼水"] = "Water-repellent",
            ["防滑设计"] = "Non-slip Design", ["承重强"] = "High Load Capacity",
            ["加西亚·马尔克斯"] = "Gabriel García Márquez",
            ["TPE 环保"] = "TPE Eco-friendly", ["收纳绑带"] = "Carrying Strap",
            ["食品级 Tritan"] = "Food-grade Tritan", ["广口易清洗"] = "Wide Mouth, Easy to Clean",
            ["3-6 人"] = "3-6 People", ["27 英寸"] = "27-inch"
        };

        var color = new Dictionary<string, string>
        {
            ["黑色"] = "Black", ["白色"] = "White", ["灰色"] = "Gray", ["藏青"] = "Navy",
            ["卡其"] = "Khaki", ["军绿"] = "Army Green", ["橙色"] = "Orange", ["蓝色"] = "Blue",
            ["银色"] = "Silver", ["深空灰"] = "Space Gray", ["米白"] = "Cream", ["雾灰"] = "Mist Gray",
            ["深灰"] = "Dark Gray", ["曜石黑"] = "Obsidian Black", ["云雾白"] = "Cloud White",
            ["深海蓝"] = "Deep Sea Blue", ["静谧紫"] = "Serene Purple", ["抹茶绿"] = "Matcha Green",
            ["透明"] = "Transparent", ["墨绿"] = "Dark Green", ["天蓝"] = "Sky Blue"
        };

        var specOption = new Dictionary<string, string>
        {
            ["标准版"] = "Standard", ["高配"] = "Pro", ["标配"] = "Standard", ["均码"] = "One Size",
            ["加厚 10mm"] = "Thickened 10mm", ["3-4 人"] = "3-4 People", ["5-6 人"] = "5-6 People",
            ["加大"] = "Large", ["标准款"] = "Standard", ["茶轴"] = "Brown Switch", ["红轴"] = "Red Switch"
        };

        var shipping = new Dictionary<string, string>
        {
            ["广东深圳"] = "Guangdong Shenzhen", ["浙江杭州"] = "Zhejiang Hangzhou", ["江苏苏州"] = "Jiangsu Suzhou",
            ["上海"] = "Shanghai", ["北京"] = "Beijing", ["江苏南通"] = "Jiangsu Nantong",
            ["江西景德镇"] = "Jiangxi Jingdezhen", ["广东惠州"] = "Guangdong Huizhou",
            ["广东东莞"] = "Guangdong Dongguan", ["浙江宁波"] = "Zhejiang Ningbo",
            ["包邮"] = "Free Shipping", ["运费 ¥6"] = "Shipping ¥6",
            ["24小时内发货"] = "Ships in 24 Hours", ["48小时内发货"] = "Ships in 48 Hours",
            ["72小时内发货"] = "Ships in 72 Hours"
        };

        var userName = new Dictionary<string, string>
        {
            ["老王"] = "Old Wang", ["小美"] = "Xiao Mei", ["阿杰"] = "A Jie", ["静静"] = "Jing Jing",
            ["大熊"] = "Big Bear", ["路过的风"] = "Passing Wind", ["阿蓝"] = "A Lan", ["小鹿"] = "Little Deer",
            ["爱吃鱼"] = "Fish Lover", ["老张头"] = "Old Zhang", ["通勤小李"] = "Commuter Xiao Li",
            ["阿健"] = "A Jian", ["码字老张"] = "Typing Old Zhang", ["设计小刘"] = "Designer Xiao Liu",
            ["老周"] = "Old Zhou", ["老白"] = "Old Bai", ["小江"] = "Xiao Jiang",
            ["晨跑的小陈"] = "Morning Runner Xiao Chen", ["计算机研一"] = "CS Grad Student",
            ["啃书人"] = "Book Worm", ["书虫阿兰"] = "Bookworm A Lan", ["阿暖"] = "A Nuan",
            ["落枕星人"] = "Stiff Neck", ["在家练瑜伽"] = "Home Yogi", ["跑者小林"] = "Runner Xiao Lin",
            ["露营新手"] = "Camping Newbie", ["户外老张"] = "Outdoor Old Zhang", ["一家三口"] = "Family of Three"
        };

        var comment = new Dictionary<string, string>
        {
            ["质量不错，做工精细，值得回购。"] = "Good quality, well made, worth repurchasing.",
            ["性价比很高，用了几天很满意。"] = "Great value, very satisfied after a few days.",
            ["物流快，包装严实，没有破损。"] = "Fast shipping, well packed, no damage.",
            ["和描述一致，用起来很顺手。"] = "Matches the description, works great.",
            ["已经第二次买了，家里人都说好。"] = "Second purchase, the whole family loves it.",
            ["客服态度很好，有问题及时解决。"] = "Great customer service, issues resolved quickly.",
            ["价格实惠，日常用足够了。"] = "Affordable, good enough for daily use.",
            ["做工可以，就是发货稍慢，总体满意。"] = "Decent build, slightly slow shipping, overall satisfied.",
            ["买来送人的，对方很喜欢。"] = "Bought as a gift, the recipient loved it.",
            ["东西是好东西，就是包装简单了点。"] = "Good product, just simple packaging.",
            ["降噪确实不错，地铁上戴着安静多了，续航也够用。"] = "Great noise cancelling — much quieter on the subway, battery lasts.",
            ["音质一般，但胜在便宜，日常听听够用。"] = "Average sound, but cheap and fine for daily use.",
            ["红轴打字很舒服，键帽手感也不错。"] = "Red switches feel great for typing, keycaps are nice too.",
            ["色彩很好，做设计够用，性价比高。"] = "Great colors, good enough for design, great value.",
            ["边框稍微有点宽，其他都满意。"] = "Slightly thick bezel, otherwise satisfied.",
            ["面料舒服，洗了几次也没变形。"] = "Comfortable fabric, no deformation after several washes.",
            ["尺码偏小，建议买大一码。"] = "Runs small, size up.",
            ["加绒很暖和，冬天出门跑步正好。"] = "Warm fleece, perfect for winter runs.",
            ["经典中的经典，学计算机底层必读。"] = "A classic, must-read for CS fundamentals.",
            ["内容很深，需要慢慢啃，收获很大。"] = "Deep content, worth the slow read.",
            ["翻译很流畅，一口气读完，意犹未尽。"] = "Smooth translation, read in one sitting.",
            ["手感温润，颜色很治愈，喝水都勤快了。"] = "Warm feel, soothing color.",
            ["睡了一周，脖子舒服多了。"] = "Neck feels much better after a week.",
            ["回弹有点慢，习惯就好。"] = "Slow rebound, gets used to it.",
            ["防滑不错，做动作很稳，没异味。"] = "Good grip, stable, no smell.",
            ["一按弹盖很方便，装包里不漏水。"] = "One-press lid is convenient, no leaks.",
            ["周末露营用了一次，搭起来很快，空间也够大。"] = "Used once for weekend camping, quick setup, spacious.",
            ["防水不错，就是收起来要费点劲。"] = "Good waterproofing, a bit hard to pack.",
            ["客服很耐心，教我怎么搭，孩子很喜欢。"] = "Patient support, kids loved it."
        };

        string? Tr(Dictionary<string, string> map, string? key) =>
            key is not null && map.TryGetValue(key, out var v) ? v : null;

        foreach (var p in products)
        {
            p.ShippingFromEn ??= Tr(shipping, p.ShippingFrom);
            p.ShippingFeeEn ??= Tr(shipping, p.ShippingFee);
            p.ShippingFeeAmount = ParseShippingFeeAmount(p.ShippingFee);
            p.DeliveryTimeEn ??= Tr(shipping, p.DeliveryTime);

            foreach (var spec in p.Specs)
            {
                spec.NameEn ??= Tr(specName, spec.Name);
                spec.ValueEn ??= Tr(specValue, spec.Value);
            }

            foreach (var sku in p.Skus)
            {
                sku.ColorEn ??= Tr(color, sku.Color);
                sku.SpecEn ??= Tr(specOption, sku.Spec);
            }

            foreach (var r in p.Reviews)
            {
                r.UserNameEn ??= Tr(userName, r.UserName);
                r.CommentEn ??= Tr(comment, r.Comment);
            }
        }
    }

    private static decimal ParseShippingFeeAmount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)
            || text.Contains("包邮", StringComparison.Ordinal)
            || text.Contains("free", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var numeric = new string(text
            .Where(character => char.IsDigit(character) || character is '.' or ',')
            .Select(character => character == ',' ? '.' : character)
            .ToArray());
        return decimal.TryParse(
            numeric,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out var amount)
            ? Math.Max(0, amount)
            : 0;
    }
}
