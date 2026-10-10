# 在线商店系统

基于 **ASP.NET Core 8 MVC** 的在线商店演示项目，采用分层架构与 SQLite 数据库。系统围绕五项质量属性设计：**可用性（Availability）、可修改性（Modifiability）、可测试性（Testability）、易用性（Usability）、安全性（Security）**。

## 技术栈

- ASP.NET Core 8 MVC（Razor 视图）
- Entity Framework Core 8 + **Microsoft.EntityFrameworkCore.Sqlite**（文件型数据库，零配置）
- ASP.NET Core Identity（Cookie 认证 + 角色授权）
- Bootstrap 5.3 + Bootstrap Icons（前端主题）
- ASP.NET Core Health Checks（健康检查）
- ASP.NET Core Session（购物车会话存储）
- xUnit + EF Core InMemory（单元测试）

## 已实现功能

| 功能 | 说明 |
| --- | --- |
| 商品/产品的创建与管理 | 后台管理区：商品的增删改查、上/下架、搜索、分类筛选 |
| 分类管理 | 分类的增删改查、排序（删除前校验是否仍有商品） |
| 商品浏览 | 前台商品目录、商品详情页 |
| 筛选/分类/目录展示 | 按分类筛选、关键词搜索、价格区间筛选、排序（最新/价格/名称）、分页 |
| 购物车 | 加入、查看、更新数量、移除、清空（基于 Session，无需登录即可使用） |
| 数据库连接与数据操作 | EF Core + SQLite，首次运行自动创建数据库文件并写入种子数据 |
| 商店主题界面 | 响应式 Bootstrap 主题、导航分类菜单、商品卡片、首页 Hero |
| 单元测试 | xUnit 测试项目：商品筛选/分页/排序、购物车增删改清空 |

## 目录结构

```
OnlineStore/
├── Models/         领域实体（Product、Category、ApplicationUser、CartItem、Cart）
├── Data/           数据上下文（ApplicationDbContext）与数据库初始化（DbInitializer）
├── Services/       业务抽象层（IProductService、ICategoryService、ICartService、ICartStore 等接口 + 实现）
├── ViewModels/     表单与列表视图模型
├── ViewComponents/ 购物车徽标组件（CartSummary）
├── Controllers/    前台控制器（Home、Products、Cart、Account、Language）
├── Areas/Admin/    后台管理区（Dashboard、Products、Categories，角色隔离）
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

> 说明：当前仅本地化了界面文本；数据库中的商品/分类名称及表单校验提示仍为中文，可后续扩展。

## 环境要求与前置步骤

1. **安装 .NET 8 SDK**（本机当前仅有运行时，缺编译用的 SDK）：
   - 命令行：`winget install Microsoft.DotNet.SDK.8`
   - 或打开 Visual Studio Installer，为 VS 2022 勾选「ASP.NET 和 Web 开发」工作负载。
2. **无需额外数据库**：已改用 SQLite 文件型数据库，`dotnet run` 即可自动创建 `onlinestore.db`。

## 配置数据库连接（SQLite）

无需外部服务。默认连接串已在 `OnlineStore/appsettings.json` 中配置为文件型 SQLite：

```json
"DefaultConnection": "Data Source=onlinestore.db"
```

首次运行时会在项目目录自动生成 `onlinestore.db` 数据库文件（含 5 个分类、12 个示例商品与管理员账号）。如需改名/换路径，改 `Data Source=` 的值即可。

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

首次运行会自动建库建表并写入种子数据（5 个分类、12 个示例商品）与管理员账号：

- 邮箱：`admin@store.com`
- 密码：`Admin@123456`

> ⚠️ 首次登录后请尽快修改密码（生产环境务必修改）。密码策略默认要求：至少 6 位，含大小写字母、数字与特殊字符。

## 质量属性如何落地（属性 → 关键代码片段）

| 质量属性 | 关键代码片段（位置） | 如何保证 |
| --- | --- | --- |
| **可用性 Availability** | `Program.cs` 的 `/health` 健康检查（`AddHealthChecks().AddDbContextCheck`）；`DbInitializer` 用 try/catch 包裹，数据库异常仅记日志不阻塞启动；`UseExceptionHandler` + `UseStatusCodePagesWithReExecute` 友好错误页；全链路 `async/await` 避免阻塞线程 | 应用可被探活、故障可观测、异常有兜底页，单个依赖故障不致整体不可用 |
| **可修改性 Modifiability** | `Services/*` 接口抽象（`IProductService`、`ICategoryService`、`ICartService`、`ICartStore`）经 DI 注入；`Program.cs` 的 `AddScoped` 注册；`SessionCartStore` 可替换为数据库/Redis 实现而不改业务；`Areas/Admin` 区域隔离；`ViewModels` 与 `Models` 分离 | 面向接口编程 + 依赖注入，新增/替换实现、扩展功能时不改动调用方 |
| **可测试性 Testability** | `OnlineStore.Tests/` 测试项目：`ProductServiceTests`（分页/分类/关键词/价格筛选/排序）、`CartServiceTests`（加入/累加/库存上限/更新/移除/清空），用 EF Core InMemory 与 `FakeCartStore` 假存储替代真实依赖 | 业务逻辑不依赖 HTTP/数据库细节，可独立实例化并快速验证；`dotnet test` 一键运行 |
| **易用性 Usability** | Bootstrap 5.3 响应式主题（`Views/Shared/_Layout.cshtml`、`wwwroot/css/site.css`）；商品卡片 `_ProductCard.cshtml`、详情页数量步进器 `js/site.js` 的 `stepQty`；中英多语言（`Resources/*.resx`）；购物车徽标 `ViewComponents/CartSummaryViewComponent` | 界面一致美观、导航清晰、交互直观，减少用户操作成本 |
| **安全性 Security** | Identity 密码哈希 + Cookie 认证（`Program.cs` 密码策略/锁定）；`[Authorize(Roles="Admin")]` 角色授权；Razor 默认 HTML 编码 + EF Core 参数化查询防 XSS/SQL 注入；表单 `[ValidateAntiForgeryToken]` 防 CSRF；`CartController.RedirectToLocal` 校验 `Url.IsLocalUrl` 防开放重定向；登录失败统一提示不泄露账号是否存在 | 纵深防御：认证、授权、输入编码、防伪令牌、开放重定向防护层层把关 |

> **答辩提示（Question Handling）**：上述表格即"每个属性由哪些代码体现"的索引。被问及时，建议按「**属性 → 落在哪个文件哪一段 → 解决了什么问题 → 若去掉会怎样**」的思路回答，例如：
> - 问"如何保证可修改性"→ 指 `Services/ICartStore.cs` + `SessionCartStore.cs`：换存储只需新增一个 `ICartStore` 实现并在 `Program.cs` 改一行注册，业务层零改动。
> - 问"购物车怎么防越权/防篡改"→ 会话隔离 + 防伪令牌 + `AddAsync` 内校验商品是否上架、库存是否大于 0。
> - 问"如何证明可测试"→ `dotnet test` 运行 13 个用例，均不依赖真实数据库与 HTTP 会话。

## 后续扩展

当前未包含（可按需加入，服务层与模型已预留扩展点）：

- 订单 / 支付 / 结算（购物车数据已可经 `ICartService` 复用）
- 用户注册与个人中心
- 商品多图、规格（SKU 属性）
- 分类层级（父子分类）
- 购物车从 Session 持久化到数据库/Redis（实现 `ICartStore` 接口即可）

## 从 EnsureCreated 切换到 EF Migrations（正式环境建议）

当前为便于零工具启动，首次建表使用 `DbInitializer` 中的 `EnsureCreated()`。正式环境建议改用迁移：

1. 安装工具：`dotnet tool install --global dotnet-ef`
2. 生成首个迁移：`dotnet ef migrations add InitialCreate`
3. 将 `DbInitializer.InitializeAsync` 中的 `EnsureCreatedAsync()` 替换为 `MigrateAsync()`。
4. 后续模型变更通过 `dotnet ef migrations add Xxx` + 部署时 `MigrateAsync()` 演进结构。
