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
| 多收货地址 | 个人中心地址簿的新增、编辑、删除；结算页选择默认地址并支持保存为默认地址 |
| 首页轮播管理 | 后台 HeroSlides 管理轮播图、中英文标题/说明、跳转地址、排序与显示开关；首页自动轮播 |
| 订单明细与结算校验 | 订单明细保存 SKU 颜色/规格快照；下单时复核价格、库存、商品状态和优惠券，取消/退款恢复库存 |
| 订单物流与售后 | 买家可查看物流进度、确认收货、申请退货/换货并从列表隐藏已结束订单；后台支持填写承运商与运单号、审核售后、接收退货和完成退款流程 |
| 订单评价 | 买家可在已完成订单中为对应商品写评价；同一订单商品再次提交会更新原评价，评分、评论、图片和日期直接显示在“我的订单”中 |
| 多分类 | 一个商品可属于多个分类（多对多） |
| 购物车 | 加入（支持所选颜色/规格）、查看、更新数量、移除、清空（基于 Session） |
| 账号注册与登录 | 支持邮箱验证码注册，注册后自动登录；短信服务尚未接入，手机号注册明确提示暂不可用，已有手机号账号仍可登录；所有账号共用邮箱或手机号登录入口，登录后返回原站内页面，后台通过管理员角色授权限制访问；登出使用统一账号流程（ASP.NET Core Identity，密码策略+失败锁定） |
| 记住账号密码 | 统一登录页「记住账号密码」勾选后，账号（邮箱或手机号）与密码经 Data Protection 加密写入 HttpOnly 持久化 Cookie，下次打开登录页自动回填；取消勾选即清除 |
| 数据库连接与数据操作 | EF Core + SQL Server，首次运行经 EF Migrations 自动建库建表并写入种子数据 |
| 商店主题界面 | 简洁黑白响应式主题、固定分类导航与下拉菜单、三图自动轮播、商品卡片、详情页购买框，以及评分概览、评价筛选与宽幅图文详情 |
| 验证码注册/找回 | 邮箱注册、找回密码与修改资料通过 6 位邮件验证码验证；仅邮件发送成功后返回成功提示，接口和页面不回显验证码；发送失败保留错误提示 |
| 促销价（划线价） | 商品可设「原价」，前端显示划线原价 + 折扣标签，首页「限时折扣」专区 |
| 优惠券 | 支持百分比、固定金额、免运费、最高优惠、最低消费、日期范围、全局/每用户次数、首单限制、排除促销商品、全店/分类/商品范围；后台支持优惠券名称、批量创建、CSV 导出与核销记录，系统内部唯一标识不在页面展示；个人中心“我的优惠券”展示可用/未开始/已用完/已过期状态，结算页可直接选择优惠券并默认选择最优方案 |
| 取消订单与退款 | 用户取消自己的订单；后台订单管理（发货/完成/取消/退款）；取消/退款自动恢复库存 |
| 完整中英双语 | 界面文案 + 商品/分类/店铺/评价/参数/SKU 颜色规格/快递/图文详情等数据库内容全部随语言切换 |
| 单元测试 | xUnit 测试项目：商品筛选/分页/排序、购物车增删改清空、图片上传校验、优惠券计算/校验/核销、订单评价关联、邮箱/手机号验证码、订单状态流转与库存扣减/恢复、记住账号密码（共 155 个 .NET 用例，另有 4 个前端规格选择与中文库存回归用例） |

## 目录结构

```
OnlineStore/
├── Models/         领域实体（Product、Category、Shop、ProductSku、ProductSpec、Review、CartItem、Cart、ApplicationUser）
├── Data/           数据上下文（ApplicationDbContext）与数据库初始化（DbInitializer）
├── Services/       业务抽象层（IProductService、ICategoryService、IShopService、IProductSkuService、IProductSpecService、IReviewService、ICartService、ICartStore 等接口 + 实现）
├── ViewModels/     表单与列表视图模型
├── ViewComponents/ 购物车徽标组件（CartSummary）
├── Controllers/    前台控制器（Home、Products、Cart、Reviews、Account、Language）
├── Areas/Admin/    后台管理区（Dashboard、Products、Categories、Shops、ProductSkus、ProductSpecs、Reviews、Orders、Coupons、Images、HeroSlides，管理员角色授权）
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

> 后台商品、分类和店铺的名称输入只保留中文；已存在的英文名称数据继续用于前台英文页面，新名称缺失英文时自动回退显示中文。描述、参数和规格等其他翻译字段不受影响。

## 环境要求与前置步骤

1. **安装支持 .NET 8 项目的 SDK**：
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
- 首页轮播图管理：`/Admin/HeroSlides`。可上传 JPG、PNG、WebP 或 GIF（最大 5 MB），编辑中英文标题与说明、图片描述、Shop 站内跳转地址、显示顺序及显示开关，也可删除轮播项。未填写英文时使用主要内容；保存后首页立即读取最新数据。

轮播图迁移 `20261025170000_AddHeroSlides` 会在首次应用时导入现有三张宣传图。管理员删除后，重新启动不会恢复已删除的轮播项；全部隐藏或删除时，首页直接展示商品区。编辑时未选择新图片会保留原图。

订单迁移 `20261025180000_AddOrderItemSku` 为订单明细保留规格编号。结算时重新验证价格、商品状态、库存与优惠券；订单、商品/规格库存及优惠券用量在同一事务中保存。价格变化时会刷新购物车并要求重新确认，取消或退款会恢复对应库存。

优惠券迁移 `20261026001008_AddCouponRules` 新增活动备注、最高优惠、每用户限用、首单限制、促销商品排除、分类/商品适用范围和核销记录；既有优惠券会回填为每人限用 1 次，结束日期会规范为当天 23:59:59。

订单物流与评价迁移新增承运商、运单号、发货/送达/签收时间、物流信息快照、售后请求，以及评价与订单商品的关联索引。

运行单元测试（在解决方案根目录）：

```bash
dotnet test
```

前端规格选择回归测试：`node --test OnlineStore.Tests/Frontend/product-details.test.cjs`。

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
| **可测试性 Testability** | `OnlineStore.Tests/` 测试项目：`ProductServiceTests`（分页/分类/关键词/价格筛选/排序/促销商品）、`CartServiceTests`（加入/累加/库存上限/更新/移除/清空）、`ImageStorageServiceTests`（上传校验）、`CouponCalculatorTests`/`CouponServiceTests`（优惠券计算、范围、首单、每用户限制、批量生成、核销与钱包状态）、`ReviewOrderTests`（订单评价归属与更新）、`VerificationCodeServiceTests`（邮箱/手机号验证码生成/校验/过期/冷却）、`OrderServiceTests`（取消/退款/发货/完成与库存扣减/恢复）、`RememberedLoginCookieServiceTests`（记住账号密码的保存/读取/清除与密码不明文落盘），用 EF Core InMemory 与 `FakeCartStore` 假存储替代真实依赖 | 业务逻辑不依赖 HTTP/数据库细节，可独立实例化并快速验证；`dotnet test` 一键运行 155 个用例 |
| **易用性 Usability** | Bootstrap 5.3 响应式主题（`Views/Shared/_Layout.cshtml`、`wwwroot/css/site.css`）；商品卡片 `_ProductCard.cshtml`、详情页数量步进器 `js/site.js` 的 `stepQty`；中英多语言（`Resources/*.resx`）；购物车徽标 `ViewComponents/CartSummaryViewComponent` | 界面一致美观、导航清晰、交互直观，减少用户操作成本 |
| **安全性 Security** | Identity 密码哈希 + Cookie 认证（`Program.cs` 密码策略/锁定）；统一账号登录，后台控制器使用 `[Authorize(Roles="Admin")]` 校验管理员角色；Razor 默认 HTML 编码 + EF Core 参数化查询防 XSS/SQL 注入；表单 `[ValidateAntiForgeryToken]` 防 CSRF；`RedirectToLocal` 校验 `Url.IsLocalUrl` 防开放重定向；`ImageStorageService` 校验图片扩展名/大小/Content-Type 并 GUID 命名防路径注入；「记住账号密码」凭据经 Data Protection 加密后写入 HttpOnly Cookie，密码不以明文存储；登录失败统一提示不泄露账号是否存在 | 纵深防御：认证、授权、输入编码、防伪令牌、上传校验、开放重定向防护层层把关 |

> **答辩提示（Question Handling）**：上述表格即"每个属性由哪些代码体现"的索引。被问及时，建议按「**属性 → 落在哪个文件哪一段 → 解决了什么问题 → 若去掉会怎样**」的思路回答，例如：
> - 问"如何保证可修改性"→ 指 `Services/ICartStore.cs` + `SessionCartStore.cs`：换存储只需新增一个 `ICartStore` 实现并在 `Program.cs` 改一行注册，业务层零改动。
> - 问"购物车怎么防越权/防篡改"→ 会话隔离 + 防伪令牌 + `AddAsync` 内校验商品是否上架、库存是否大于 0。
> - 问"如何证明可测试"→ `dotnet test` 运行 155 个用例，均不依赖真实数据库与 HTTP 会话。

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
