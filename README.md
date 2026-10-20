# 在线商店系统

基于 **ASP.NET Core 8 MVC** 的在线商店演示项目，采用分层架构与 SQL Server 数据库。系统围绕五项质量属性设计：**可用性（Availability）、可修改性（Modifiability）、可测试性（Testability）、易用性（Usability）、安全性（Security）**。

## 技术栈

- ASP.NET Core 8 MVC（Razor 视图）
- Entity Framework Core 8 + **Microsoft.EntityFrameworkCore.SqlServer**（SQL Server Express，SSMS 管理）
- ASP.NET Core Identity（Cookie 认证 + 角色授权）
- Bootstrap 5.3 + Bootstrap Icons（前端主题）
- ASP.NET Core Health Checks（健康检查）
- ASP.NET Core Session（购物车会话存储）
- xUnit + EF Core InMemory（单元测试）

## 已实现功能

| 功能 | 说明 |
| --- | --- |
| 商品/产品的创建与管理 | 后台管理区：商品的增删改查、上/下架、搜索、分类筛选、图片上传（本地上传或外链 URL） |
| 分类管理 | 分类的增删改查、排序（删除前校验是否仍有商品） |
| 商品浏览 | 前台商品目录、商品详情页（店铺评分、多分类、SKU 规格选择、多图画廊、产品参数、快递信息、用户评价） |
| 筛选/分类/目录展示 | 按分类筛选、关键词搜索、价格区间筛选、排序（最新/价格/名称）、分页；热门搜索词、搜索无结果的「猜你喜欢」推荐 |
| 收藏夹 | 登录用户可收藏/取消收藏商品，查看「我的收藏」 |
| 商品规格（SKU） | 颜色 × 规格两维变体，独立价格/库存/图片；前台规格选择器联动价格与库存 |
| 店铺管理 | 店铺增删改查（名称/评分/销量/简介），商品归属店铺 |
| 产品参数 | 商品参数（键值对）增删改查，详情页展示 |
| 用户评价 | 预置评价展示 + 登录用户可提交星级评价；后台可管理/删除；详情页评价分布（星级占比） |
| 下单结算 | 购物车 → 结算（预填收货信息）→ 提交订单 → 订单确认 → 我的订单 |
| 多分类 | 一个商品可属于多个分类（多对多） |
| 购物车 | 加入（支持所选颜色/规格）、查看、更新数量、移除、清空（基于 Session） |
| 账号注册与登录 | 前台支持「邮箱注册 / 手机号注册」双入口（各自验证码校验，注册后自动登录）、登录支持邮箱或手机号；后台管理员专属登录（仅管理员角色可登录，前后台入口分离）；登出（ASP.NET Core Identity，密码策略+失败锁定） |
| 记住账号密码 | 登录页「记住账号密码」勾选后，账号（邮箱或手机号）与密码经 Data Protection 加密写入 HttpOnly 持久化 Cookie，下次打开登录页自动回填（前后台登录均支持）；取消勾选即清除 |
| 数据库连接与数据操作 | EF Core + SQL Server，首次运行经 EF Migrations 自动建库建表并写入种子数据 |
| 商店主题界面 | 亚马逊风格响应式主题、导航分类菜单、商品卡片、首页 Hero、详情页购买框 |
| 验证码注册/找回 | 邮箱注册、手机号注册与忘记密码均通过 6 位验证码（演示模式：验证码直接显示在页面；邮箱渠道预留 Brevo 真实发信、短信渠道预留接口） |
| 促销价（划线价） | 商品可设「原价」，前端显示划线原价 + 折扣标签，首页「限时折扣」专区 |
| 优惠券 | 后台管理优惠券（百分比/固定金额、有效期、使用上限），下单输入优惠码抵扣订单金额 |
| 取消订单与退款 | 用户取消自己的订单；后台订单管理（发货/完成/取消/退款）；取消/退款自动恢复库存 |
| 完整中英双语 | 界面文案 + 商品/分类/店铺/评价/参数/SKU 颜色规格/快递/图文详情等数据库内容全部随语言切换 |
| 单元测试 | xUnit 测试项目：商品筛选/分页/排序、购物车增删改清空、图片上传校验、优惠券计算/校验、邮箱/手机号验证码、订单状态流转与库存扣减/恢复、记住账号密码（共 53 个用例） |

## 目录结构

```
OnlineStore/
├── Models/         领域实体（Product、Category、Shop、ProductSku、ProductSpec、Review、CartItem、Cart、ApplicationUser）
├── Data/           数据上下文（ApplicationDbContext）与数据库初始化（DbInitializer）
├── Services/       业务抽象层（IProductService、ICategoryService、IShopService、IProductSkuService、IProductSpecService、IReviewService、ICartService、ICartStore 等接口 + 实现）
├── ViewModels/     表单与列表视图模型
├── ViewComponents/ 购物车徽标组件（CartSummary）
├── Controllers/    前台控制器（Home、Products、Cart、Reviews、Account、Language）
├── Areas/Admin/    后台管理区（Dashboard、Products、Categories、Shops、ProductSkus、ProductSpecs、Reviews、Account，角色隔离）
├── Views/          Razor 视图（前台，含 Cart、Shared/Components）
└── wwwroot/        静态资源（css/site.css、js/site.js）

OnlineStore.Tests/  xUnit 单元测试项目（引用 OnlineStore，使用 EF Core InMemory）
```

## 多语言 / 国际化（中英切换）

- 采用 ASP.NET Core 本地化（`IStringLocalizer<SharedResource>` + 资源文件）。
- 资源文件位于 `Resources/SharedResource.resx`（中文/默认）与 `Resources/SharedResource.en.resx`（英文）。
- 前台导航栏与后台侧边栏均有「中文 / EN」切换按钮，点击写入本地化 Cookie 并刷新当前页。
- 也可通过查询串临时切换：`/?culture=en` 或 `/?culture=zh-CN`。
- 默认语言：中文（zh-CN）；支持语言列表在 `Program.cs` 的 `supportedCultures` 中配置。
- 新增语言：复制一份 `SharedResource.<culture>.resx` 翻译后，把 culture 加入 `Program.cs` 的 `supportedCultures` 即可。

> 说明：界面文案与数据库内容（商品/分类/店铺名称与描述、评价、参数、SKU 颜色规格、快递信息）均已支持中英双语，切换语言时整页内容随之切换；英文内容已预置于种子数据中。

## 环境要求与前置步骤

1. **安装 .NET 8 SDK**（本机当前仅有运行时，缺编译用的 SDK）：
   - 命令行：`winget install Microsoft.DotNet.SDK.8`
   - 或打开 Visual Studio Installer，为 VS 2022 勾选「ASP.NET 和 Web 开发」工作负载。
2. **需安装 SQL Server Express**：本机需有 SQL Server 实例（默认 `SQLEXPRESS`），首次运行会自动建库建表。

## 配置数据库连接（SQL Server）

默认连接串已在 `OnlineStore/appsettings.json` 中配置为 SQL Server Express（Windows 集成认证）：

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=OnlineStore;Trusted_Connection=True;TrustServerCertificate=True;"
```

首次运行会经 EF Migrations 自动创建 `OnlineStore` 数据库与全部表，并写入种子数据（5 个分类、100 个示例商品、5 家店铺与管理员账号）。用 SSMS 连接 `localhost\SQLEXPRESS` 即可查看/管理数据库；也可先在 SSMS 中建好空库 `OnlineStore`，应用首次启动时会自动建表并写入种子数据。

## 运行

命令行（在项目目录 `OnlineStore` 下）：

```bash
dotnet restore
dotnet build
dotnet run
```

或直接用 Visual Studio 打开 `OnlineStore.sln`，按 F5 运行。

启动后浏览器访问（默认 http 配置）：**http://localhost:5080**

- 首页/目录：`/`
- 全部商品与筛选：`/Products`
- 商品详情与加入购物车：`/Products/Details/{id}`
- 购物车：`/Cart`
- 健康检查：`/health`（返回 Healthy/Unhealthy）
- 后台管理：`/Admin`

运行单元测试（在解决方案根目录）：

```bash
dotnet test
```

## 默认管理员账号

首次运行会自动建库建表并写入种子数据（5 个分类、100 个示例商品、5 家店铺）与管理员账号：

- 邮箱：`admin@store.com`
- 密码：`Admin@123456`

> ⚠️ 首次登录后请尽快修改密码（生产环境务必修改）。密码策略要求：至少 6 位，含大小写字母、数字与特殊字符（注册/找回密码时前端即时校验，与 Identity 服务端规则一致）。

## 质量属性如何落地（属性 → 关键代码片段）

| 质量属性 | 关键代码片段（位置） | 如何保证 |
| --- | --- | --- |
| **可用性 Availability** | `Program.cs` 的 `/health` 健康检查（`AddHealthChecks().AddDbContextCheck`）；`DbInitializer` 用 try/catch 包裹，数据库异常仅记日志不阻塞启动；`UseExceptionHandler` + `UseStatusCodePagesWithReExecute` 友好错误页；全链路 `async/await` 避免阻塞线程 | 应用可被探活、故障可观测、异常有兜底页，单个依赖故障不致整体不可用 |
| **可修改性 Modifiability** | `Services/*` 接口抽象（`IProductService`、`ICategoryService`、`ICartService`、`ICartStore`、`IImageStorageService`、`IRememberedLoginService`）经 DI 注入；`Program.cs` 的 `AddScoped` 注册；`SessionCartStore` 可替换为数据库/Redis、`ImageStorageService` 可替换为 OSS/S3 而不改业务；`Areas/Admin` 区域隔离；`ViewModels` 与 `Models` 分离 | 面向接口编程 + 依赖注入，新增/替换实现、扩展功能时不改动调用方 |
| **可测试性 Testability** | `OnlineStore.Tests/` 测试项目：`ProductServiceTests`（分页/分类/关键词/价格筛选/排序/促销商品）、`CartServiceTests`（加入/累加/库存上限/更新/移除/清空）、`ImageStorageServiceTests`（上传校验）、`CouponCalculatorTests`/`CouponServiceTests`（优惠券计算与校验）、`VerificationCodeServiceTests`（邮箱/手机号验证码生成/校验/过期/冷却）、`OrderServiceTests`（取消/退款/发货/完成与库存扣减/恢复）、`RememberedLoginCookieServiceTests`（记住账号密码的保存/读取/清除与密码不明文落盘），用 EF Core InMemory 与 `FakeCartStore` 假存储替代真实依赖 | 业务逻辑不依赖 HTTP/数据库细节，可独立实例化并快速验证；`dotnet test` 一键运行 53 个用例 |
| **易用性 Usability** | Bootstrap 5.3 响应式主题（`Views/Shared/_Layout.cshtml`、`wwwroot/css/site.css`）；商品卡片 `_ProductCard.cshtml`、详情页数量步进器 `js/site.js` 的 `stepQty`；中英多语言（`Resources/*.resx`）；购物车徽标 `ViewComponents/CartSummaryViewComponent` | 界面一致美观、导航清晰、交互直观，减少用户操作成本 |
| **安全性 Security** | Identity 密码哈希 + Cookie 认证（`Program.cs` 密码策略/锁定）；`[Authorize(Roles="Admin")]` 角色授权；前后台登录入口分离 + 后台登录校验管理员角色；Razor 默认 HTML 编码 + EF Core 参数化查询防 XSS/SQL 注入；表单 `[ValidateAntiForgeryToken]` 防 CSRF；`RedirectToLocal` 校验 `Url.IsLocalUrl` 防开放重定向；`ImageStorageService` 校验图片扩展名/大小/Content-Type 并 GUID 命名防路径注入；「记住账号密码」凭据经 Data Protection 加密后写入 HttpOnly Cookie，密码不以明文存储；登录失败统一提示不泄露账号是否存在 | 纵深防御：认证、授权、入口隔离、输入编码、防伪令牌、上传校验、开放重定向防护层层把关 |

> **答辩提示（Question Handling）**：上述表格即"每个属性由哪些代码体现"的索引。被问及时，建议按「**属性 → 落在哪个文件哪一段 → 解决了什么问题 → 若去掉会怎样**」的思路回答，例如：
> - 问"如何保证可修改性"→ 指 `Services/ICartStore.cs` + `SessionCartStore.cs`：换存储只需新增一个 `ICartStore` 实现并在 `Program.cs` 改一行注册，业务层零改动。
> - 问"购物车怎么防越权/防篡改"→ 会话隔离 + 防伪令牌 + `AddAsync` 内校验商品是否上架、库存是否大于 0。
> - 问"如何证明可测试"→ `dotnet test` 运行 53 个用例，均不依赖真实数据库与 HTTP 会话。

## 后续扩展

当前未包含（可按需加入，服务层与模型已预留扩展点）：

- 支付（下单已实现，支付环节为模拟，未接入真实支付网关）
- 分类层级（父子分类）
- 购物车从 Session 持久化到数据库/Redis（实现 `ICartStore` 接口即可）
- 真实邮件发送需配置发件域名（当前为演示模式，代码已预留 Brevo API）

## 数据库迁移（EF Core Migrations）

项目已采用 EF Core Migrations（`Data/DbInitializer.cs` 中调用 `Database.MigrateAsync()`），首个迁移为 `Migrations/*_InitialCreate`。模型变更时：

1. 安装工具（如未安装）：`dotnet tool install --global dotnet-ef`
2. 新增迁移：在 `OnlineStore/` 目录下执行 `dotnet ef migrations add Xxx`
3. 应用迁移：`dotnet ef database update`（或直接 `dotnet run`，启动时会自动 `MigrateAsync()`）
