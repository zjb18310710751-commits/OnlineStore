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

        // 追加 88 件商品：覆盖各类目、价格从低到高，模拟真实商城
        var extraItems = new (string Name, Category Category, Shop Shop, decimal Price)[]
        {
            ("手机支架", electronics, digital, 19.90m), ("三合一数据线", electronics, digital, 29.00m),
            ("蓝牙自拍杆", electronics, digital, 39.00m), ("无线充电器", electronics, digital, 89.00m),
            ("移动电源 20000mAh", electronics, digital, 129.00m), ("键鼠套装", electronics, digital, 129.00m),
            ("显示器支架", electronics, digital, 149.00m), ("蓝牙音箱", electronics, digital, 159.00m),
            ("电竞鼠标", electronics, digital, 199.00m), ("路由器", electronics, digital, 199.00m),
            ("游戏手柄", electronics, digital, 269.00m), ("智能音箱", electronics, digital, 299.00m),
            ("机械硬盘 2TB", electronics, digital, 499.00m), ("智能手表", electronics, digital, 599.00m),
            ("固态硬盘 1TB", electronics, digital, 699.00m), ("电纸书阅读器", electronics, digital, 899.00m),
            ("投影仪", electronics, digital, 1899.00m), ("平板电脑", electronics, digital, 2499.00m),
            ("无人机", electronics, digital, 2999.00m), ("微单相机", electronics, digital, 3999.00m),
            ("棉袜五双装", fashion, clothing, 29.00m), ("棒球帽", fashion, clothing, 39.00m),
            ("围巾", fashion, clothing, 49.00m), ("短裤", fashion, clothing, 59.00m),
            ("皮带", fashion, clothing, 69.00m), ("睡衣", fashion, clothing, 89.00m),
            ("帆布鞋", fashion, clothing, 99.00m), ("泳衣", fashion, clothing, 99.00m),
            ("衬衫", fashion, clothing, 129.00m), ("双肩包", fashion, clothing, 129.00m),
            ("牛仔裤", fashion, clothing, 159.00m), ("毛衣", fashion, clothing, 179.00m),
            ("连衣裙", fashion, clothing, 199.00m), ("运动鞋", fashion, clothing, 299.00m),
            ("皮鞋", fashion, clothing, 299.00m), ("风衣", fashion, clothing, 399.00m),
            ("冲锋衣", fashion, clothing, 499.00m), ("羽绒服", fashion, clothing, 599.00m),
            ("《小王子》", books, bookstore, 19.00m), ("英语词汇书", books, bookstore, 35.00m),
            ("《活着》", books, bookstore, 39.00m), ("《白夜行》", books, bookstore, 39.50m),
            ("《时间简史》", books, bookstore, 45.00m), ("《红楼梦》", books, bookstore, 49.00m),
            ("《平凡的世界》", books, bookstore, 59.00m), ("《人类简史》", books, bookstore, 68.00m),
            ("《经济学原理》", books, bookstore, 98.00m), ("《算法导论》", books, bookstore, 128.00m),
            ("儿童绘本套装", books, bookstore, 129.00m), ("《三体》全集", books, bookstore, 168.00m),
            ("花瓶", home, homeStore, 29.00m), ("香薰蜡烛", home, homeStore, 39.00m),
            ("抱枕", home, homeStore, 39.00m), ("毛巾三件套", home, homeStore, 49.00m),
            ("收纳箱三件套", home, homeStore, 59.00m), ("挂钟", home, homeStore, 69.00m),
            ("保温杯", home, homeStore, 89.00m), ("电热水壶", home, homeStore, 99.00m),
            ("落地灯", home, homeStore, 159.00m), ("刀具套装", home, homeStore, 159.00m),
            ("不锈钢锅", home, homeStore, 199.00m), ("窗帘", home, homeStore, 199.00m),
            ("床品四件套", home, homeStore, 299.00m), ("电饭煲", home, homeStore, 299.00m),
            ("地毯", home, homeStore, 399.00m), ("咖啡机", home, homeStore, 599.00m),
            ("空气净化器", home, homeStore, 899.00m), ("吸尘器", home, homeStore, 1299.00m),
            ("扫地机器人", home, homeStore, 1499.00m), ("智能马桶盖", home, homeStore, 1999.00m),
            ("跳绳", sports, outdoor, 19.00m), ("冰袖", sports, outdoor, 19.00m),
            ("泳镜", sports, outdoor, 29.00m), ("护膝", sports, outdoor, 39.00m),
            ("帐篷灯", sports, outdoor, 39.00m), ("野餐垫", sports, outdoor, 49.00m),
            ("瑜伽球", sports, outdoor, 59.00m), ("登山杖", sports, outdoor, 59.00m),
            ("折叠椅", sports, outdoor, 79.00m), ("篮球", sports, outdoor, 89.00m),
            ("足球", sports, outdoor, 99.00m), ("羽毛球拍", sports, outdoor, 99.00m),
            ("骑行头盔", sports, outdoor, 129.00m), ("哑铃", sports, outdoor, 129.00m),
            ("望远镜", sports, outdoor, 259.00m), ("筋膜枪", sports, outdoor, 399.00m),
            ("山地自行车", sports, outdoor, 1299.00m), ("跑步机", sports, outdoor, 1999.00m)
        };

        for (var i = 0; i < extraItems.Length; i++)
        {
            var (name, category, shop, price) = extraItems[i];
            var stock = Random.Shared.Next(30, 500);
            products.Add(new Product
            {
                Name = name,
                Slug = name,
                Description = BuildDescription(name),
                Price = price,
                Stock = stock,
                Sku = $"EXT-{i + 1:000}",
                Shop = shop,
                Categories = new List<Category> { category },
                ShippingFrom = PickOne("广东深圳", "浙江杭州", "江苏苏州", "上海", "北京"),
                ShippingFee = "包邮",
                DeliveryTime = PickOne("24小时内发货", "48小时内发货", "72小时内发货"),
                Specs = BuildExtraSpecs(category.Name),
                Reviews = BuildExtraReviews(),
                Skus = ShouldHaveSku(category.Name) ? BuildExtraSkus(price, stock) : new List<ProductSku>()
            });
        }

        // 为种子商品配占位图（每件商品固定一张，可随后台图片上传替换为真实商品图）
        foreach (var p in products)
        {
            p.ImageUrl = $"https://picsum.photos/seed/{p.Sku}/600/400";
            p.Images = new List<ProductImage>
            {
                new() { ImageUrl = p.ImageUrl, SortOrder = 0 },
                new() { ImageUrl = $"https://picsum.photos/seed/{p.Sku}-b/600/400", SortOrder = 1 },
                new() { ImageUrl = $"https://picsum.photos/seed/{p.Sku}-c/600/400", SortOrder = 2 }
            };
        }

        context.Shops.AddRange(shops);
        context.Categories.AddRange(categories);
        context.Products.AddRange(products);
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

    private static List<ProductSpec> BuildExtraSpecs(string categoryName) =>
        categoryName switch
        {
            "电子产品" => Specs(("品牌", "知名品牌"), ("质保", "一年"), ("接口", "通用")),
            "服装服饰" => Specs(("材质", "舒适面料"), ("版型", "标准"), ("洗涤", "可机洗")),
            "图书" => Specs(("装帧", "平装"), ("语言", "中文"), ("出版", "正规出版")),
            "家居生活" => Specs(("材质", "优质耐用"), ("风格", "简约"), ("清洁", "易清洁")),
            _ => Specs(("材质", "耐磨耐用"), ("场景", "户外/室内"), ("重量", "轻便"))
        };

    private static List<Review> BuildExtraReviews()
    {
        var count = Random.Shared.Next(1, 3);
        var reviews = new List<Review>();
        for (var i = 0; i < count; i++)
        {
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
                Random.Shared.Next(1, 60)));
        }

        return reviews;
    }

    private static bool ShouldHaveSku(string categoryName) =>
        categoryName is "电子产品" or "服装服饰" or "运动户外"
            && Random.Shared.NextDouble() < 0.6;

    private static List<ProductSku> BuildExtraSkus(decimal basePrice, int stock)
    {
        var colors = new[] { "黑色", "白色", "灰色", "蓝色", "粉色", "绿色", "橙色" };
        var specs = new[] { "标准版", "升级版", "均码", "M", "L" };
        var count = Random.Shared.Next(2, 4);
        var skus = new List<ProductSku>();
        for (var i = 0; i < count; i++)
        {
            var delta = Random.Shared.Next(-5, 11);
            skus.Add(new ProductSku
            {
                Color = colors[Random.Shared.Next(colors.Length)],
                Spec = specs[Random.Shared.Next(specs.Length)],
                Price = Math.Max(1m, basePrice + delta),
                Stock = Math.Max(5, stock / count)
            });
        }

        return skus;
    }

    private static List<ProductSpec> Specs(params (string Name, string Value)[] items)
        => items.Select((s, i) => new ProductSpec { Name = s.Name, Value = s.Value, DisplayOrder = i }).ToList();

    private static Review Review(string userName, int rating, string comment, int daysAgo = 0)
        => new() { UserName = userName, Rating = rating, Comment = comment, CreatedAt = DateTime.UtcNow.AddDays(-daysAgo) };
}
