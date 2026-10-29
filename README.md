# 在线商店系统

基于 **ASP.NET Core 8 MVC** 的在线商店演示项目，采用分层架构与 SQL Server 数据库。系统围绕五项质量属性设计：**可用性（Availability）、可修改性（Modifiability）、可测试性（Testability）、易用性（Usability）、安全性（Security）**。

## 在线演示

- Azure App Service：<https://zhy-onlinestore-31072.azurewebsites.net/>
- 部署环境：Azure App Service（Linux F1）+ Azure SQL Database（Basic）
- 首次访问可能需要等待片刻（F1 免费档冷启动）；是否在启动时应用迁移和写入示例目录由 `Initialization` 配置控制。
- 资源组：`rg-onlinestore-demo`；删除该资源组会同时删除 Azure 网站和 Azure SQL，停止相关计费（本地数据库不受影响）。
- 推送到 `main` 后由 GitHub Actions 自动发布，也可在 Actions 页面手动运行工作流。

```bash
# 在 Azure Cloud Shell 中重新部署（先完成 az login）
az webapp deploy \
  --resource-group rg-onlinestore-demo \
  --name zhy-onlinestore-31072 \
  --src-path ./app.zip \
  --type zip

# 回滚：删除整个演示资源组
az group delete --name rg-onlinestore-demo --yes --no-wait
```

### 自动化部署

- 推送到 `main` 后，GitHub Actions 会依次执行依赖审计、还原、构建、172 个后端测试、6 个前端回归测试，并发布到 Azure App Service。
- 后端测试使用 Coverlet 采集覆盖率；报告排除自动生成的 Razor、迁移和演示数据初始化代码，并作为构建产物上传。
- 工作流使用生产并发锁，旧运行会被新运行取消；部署完成后自动验证 `/health`，未恢复健康会使工作流失败。
- 仓库的 Actions Secrets 需要配置 `AZURE_WEBAPP_PUBLISH_PROFILE`，内容来自 Azure App Service 概览页的「获取发布配置文件 / Get publish profile」。
- 本地仓库配置了 `post-commit` hook：在 `main` 分支提交后会自动执行 `git push origin main`；未提交的修改不会自动推送。

## 技术栈

- ASP.NET Core 8 MVC（Razor 视图）
- Entity Framework Core 8.0.33 + **Microsoft.EntityFrameworkCore.SqlServer**（SQL Server Express / Azure SQL，SSMS 管理）
- Azure SQL Database（Azure 演示环境，通过 App Service 连接串配置）
- ASP.NET Core Identity（Cookie 认证 + 角色授权）
- Bootstrap 5.3 + Bootstrap Icons（本地静态资源，不依赖运行时 CDN）
- ASP.NET Core Health Checks（健康检查）
- ASP.NET Core Session（开发环境内存缓存，生产环境 SQL Server 分布式缓存）
- xUnit + EF Core InMemory（单元测试）
- Coverlet 测试覆盖率采集
- NuGet Audit（构建时检查已知漏洞）

## 已实现功能

| 功能 | 说明 |
| --- | --- |
| 商品/产品的创建与管理 | 后台管理区：商品的增删改查、上/下架、搜索、分类筛选、图片上传（本地上传或外链 URL） |
| 分类管理 | 分类的增删改查、排序（删除前校验是否仍有商品） |
| 商品浏览 | 前台商品目录、商品详情页（店铺评分、多分类、SKU 规格选择、多图画廊、产品参数、快递信息、用户评价） |
| 筛选/分类/目录展示 | 按分类筛选、关键词搜索、价格区间筛选、排序（最新/价格/名称）、分页；热门搜索词、搜索无结果的「猜你喜欢」推荐 |
| 收藏夹 | 登录用户可收藏/取消收藏商品，查看「我的收藏」 |
| 商品规格（SKU） | 颜色 × 规格两维变体，独立价格/库存/图片；前台规格选择器联动价格与库存 |
| 店铺管理 | 店铺增删改查（名称/Logo/评分/销量/简介），Logo 支持选择图片、复制粘贴上传，并显示在店铺页与商品详情；商品归属店铺 |
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
| 账号注册与登录 | 支持邮箱验证码注册，注册后自动登录；登录界面只展示邮箱登录文案；短信服务尚未接入；登录后返回原站内页面，后台通过管理员角色授权限制访问；登出使用统一账号流程（ASP.NET Core Identity，密码策略+失败锁定） |
| 记住账号密码 | 统一登录页「记住账号密码」勾选后，账号（邮箱或手机号）与密码经 Data Protection 加密写入 HttpOnly 持久化 Cookie，下次打开登录页自动回填；取消勾选即清除 |
| 数据库连接与数据操作 | EF Core + SQL Server，首次运行经 EF Migrations 自动建库建表并写入种子数据 |
| 商店主题界面 | 简洁黑白响应式主题、固定分类导航与下拉菜单、三图自动轮播、商品卡片、自定义细线购物袋图标、完整商店页脚、商品详情与评价区域 |
| 验证码注册/找回 | 邮箱注册、找回密码与修改资料通过 6 位邮件验证码验证；仅邮件发送成功后返回成功提示，接口和页面不回显验证码；发送失败保留错误提示 |
| 促销价（划线价） | 商品可设「原价」，前端显示划线原价 + 折扣标签，首页「限时折扣」专区 |
| 优惠券 | 支持百分比、固定金额、免运费、最高优惠、最低消费、日期范围、全局/每用户次数、首单限制、排除促销商品、全店/分类/商品范围；后台支持优惠券名称、批量创建、CSV 导出与核销记录，系统内部唯一标识不在页面展示；个人中心“我的优惠券”展示可用/未开始/已用完/已过期状态，结算页可直接选择优惠券并默认选择最优方案 |
| 取消订单与退款 | 用户取消自己的订单；后台订单管理（发货/完成/取消/退款）；取消/退款自动恢复库存 |
| 完整中英双语 | 界面文案 + 商品/分类/店铺/评价/参数/SKU 颜色规格/快递/图文详情等数据库内容全部随语言切换 |
| 单元测试 | xUnit 测试项目：商品筛选/分页/排序、购物车增删改清空、图片上传校验、优惠券计算/校验/核销、订单评价关联、邮箱验证码错误锁定、订单状态流转与库存扣减/恢复、记住账号密码、支付方式目录、Header 服务、可注入时间与订单号、中英文资源一致性（共 172 个 .NET 用例，另有 6 个前端规格选择与自定义选择器回归用例） |

## 目录结构

```
OnlineStore/
├── Models/         领域实体（Product、Category、Shop、ProductSku、ProductSpec、Review、CartItem、Cart、Order、Coupon、DeliveryAddress、HeroSlide、ApplicationUser）
├── Data/           数据上下文（ApplicationDbContext）、初始化配置与数据库初始化（DbInitializer）
├── Services/       业务抽象层（产品、购物车、订单、优惠券、商城 Header、图片存储等接口 + 实现）
├── ViewModels/     表单与列表视图模型
├── ViewComponents/ 购物车徽标、首页轮播与分类导航组件（CartSummary、HeroCarousel、CategoryNavigation）
├── Controllers/    前台控制器（Home、Products、Cart、Reviews、Account、Language、Shops、Favorites、Orders）
├── Areas/Admin/    后台管理区（Dashboard、Products、Categories、Shops、ProductSkus、ProductSpecs、Reviews、Orders、Coupons、Images、HeroSlides，管理员角色授权）
├── Views/          Razor 视图（前台，含 Home/_HeroCarousel、Products/_ProductReviews、_ProductStory、Shared 反馈与确认组件）
└── wwwroot/        静态资源（本地 Bootstrap/Bootstrap Icons、商城主题 CSS 与对应 JS）

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
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=OnlineStore_Main;Trusted_Connection=True;TrustServerCertificate=True;"
```

首次运行会经 EF Migrations 自动创建 `OnlineStore_Main` 数据库与全部表。示例目录、管理员和演示买家由 `Initialization` 配置控制。用 SSMS 连接 `localhost\SQLEXPRESS` 即可查看/管理数据库。

> `appsettings.json` 与 `appsettings.Development.json` 默认都指向 `OnlineStore_Main`。如果你使用其他数据库名，请同时修改这两个文件，或通过环境变量覆盖连接串。

## 配置真实邮箱发送验证码

项目支持两种真实邮件发送方式：个人邮箱 SMTP 和 Brevo API。当前默认使用 SMTP；不要把邮箱密码或授权码写入 `appsettings.json` 或提交到 GitHub。

在 Azure App Service 的 **Configuration → Environment variables** 中添加以下应用设置（双下划线对应配置层级）：

| 应用设置 | 示例 | 说明 |
| --- | --- | --- |
| `Email__Provider` | `Smtp` | 使用个人邮箱 SMTP |
| `Email__Host` | `smtp.gmail.com` | 邮箱服务商的 SMTP 地址 |
| `Email__Port` | `587` | 通常使用 STARTTLS 的 587 端口 |
| `Email__EnableSsl` | `true` | 启用 TLS 加密 |
| `Email__SenderEmail` | `yourname@gmail.com` | 收件人看到的发件邮箱 |
| `Email__SenderName` | `ZHY Store` | 收件人看到的发件人名称 |
| `Email__Username` | `yourname@gmail.com` | SMTP 登录账号 |
| `Email__Password` | 邮箱授权码 | Gmail/QQ 邮箱应填写应用专用密码，不是网页登录密码 |

常见服务商配置：

- Gmail：`smtp.gmail.com`、端口 `587`，需先在 Google 账号中开启两步验证并创建“应用专用密码”。
- QQ 邮箱：`smtp.qq.com`、端口 `587`，需在邮箱设置中开启 SMTP 服务并生成“授权码”。
- Outlook/学校邮箱：`smtp-mail.outlook.com`、端口 `587`；部分学校或组织会禁止 SMTP 登录，需要管理员开放。
- 如继续使用 Brevo：把 `Email__Provider` 设为 `Brevo`，并配置 `Email__ApiKey`、`Email__SenderEmail` 和 `Email__SenderName`。

配置后，注册、找回密码和修改资料时发送的 6 位验证码会从所配置的邮箱发出。发件凭据只存放在部署环境的 App Settings 中，不会随源码上传。

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

生成核心业务覆盖率报告：

```bash
dotnet test OnlineStore.Tests/OnlineStore.Tests.csproj \
  --settings coverlet.runsettings \
  --collect:"XPlat Code Coverage"
```

前端回归测试：`node --test OnlineStore.Tests/Frontend/*.test.cjs`。

## 初始化与管理员配置

源码不再包含管理员或演示买家的默认凭据。通过环境变量、Azure App Service 配置或用户机密提供初始化值：

| 配置项 | 默认值 | 说明 |
| --- | --- | --- |
| `Initialization__ApplyMigrationsOnStartup` | `true` | 启动时是否执行 EF Migrations |
| `Initialization__RunOnStartup` | `true` | 是否在应用启动时执行数据库初始化；关闭后可先启动静态页面 |
| `Initialization__InitializationRetryCount` | `2` | 初始化整体重试次数 |
| `Initialization__InitializationRetryDelaySeconds` | `2` | 每次初始化重试间隔 |
| `Initialization__FailFastOnInitializationFailure` | Development 为 `false`，Production 为 `true` | 初始化最终失败后是否终止启动 |
| `Initialization__SeedSampleCatalogOnEmpty` | `true` | 空数据库是否写入示例分类、商品和店铺 |
| `Initialization__AdminEmail` | 空 | 首次创建管理员时使用的邮箱 |
| `Initialization__AdminPassword` | 空 | 首次创建管理员时使用的密码 |
| `Initialization__SeedDemoData` | `false` | 是否创建演示买家与演示订单 |
| `Initialization__DemoBuyerEmail` | 空 | 演示买家邮箱 |
| `Initialization__DemoBuyerPassword` | 空 | 演示买家密码 |

本地开发可使用 .NET User Secrets：

```bash
dotnet user-secrets set "Initialization:AdminEmail" "admin@example.com" --project OnlineStore
dotnet user-secrets set "Initialization:AdminPassword" "<strong-password>" --project OnlineStore
```

管理员只在对应邮箱不存在时创建，应用不会在后续启动中覆盖已有密码。密码策略要求至少 6 位，并包含大小写字母、数字和特殊字符。

## 可用性配置

- `Database__RetryCount`：SQL Server 瞬时故障重试次数，默认 `3`。
- `Database__MaxRetryDelaySeconds`：单次数据库重试最大等待时间，默认 `3` 秒。
- `Storage__UploadsPath`：用户上传文件保存目录。留空时，生产环境使用 App Service 的 `$HOME/data/uploads`，开发环境使用 `wwwroot/uploads`。启动时会自动复制旧目录中尚未迁移的文件。
- `Session__Provider`：`Memory` 或 `SqlServer`。生产环境默认使用 SQL Server 分布式 Session，使购物车在重启和多实例部署后仍可恢复。
- `Session__SchemaName`、`Session__TableName`：数据库 Session 表结构，默认 `dbo.SessionCache`。
- Bootstrap CSS、JavaScript 和图标字体已经保存在 `wwwroot/lib`，Azure 运行时不再依赖 jsDelivr。
- `Security__AuthenticationRateLimit__PermitLimit`：认证相关接口在单个窗口内的允许请求数，默认 `10`。
- `Security__AuthenticationRateLimit__WindowSeconds`：认证限流窗口，默认 `60` 秒。

验证码连续错误 5 次后会立即失效，需要重新申请；登录、注册、找回密码和验证码接口同时使用 IP 固定窗口限流。

## 质量属性如何落地（属性 → 关键代码片段）

| 质量属性 | 关键代码片段（位置） | 如何保证 |
| --- | --- | --- |
| **可用性 Availability** | `Program.cs` 的 `/health`、可配置 SQL Server 重试、初始化重试与生产失败保护；SQL Server 分布式 Session；持久化上传目录；本地 Bootstrap；统一的异常处理器和状态码页面 | 应用可被探活，数据库瞬断可恢复，购物车和用户图片不因重启或部署丢失，启动失败不会以部分初始化状态继续运行 |
| **可修改性 Modifiability** | `Services/*` 接口抽象经 DI 注入；`PaymentMethods` 集中支付方式定义；`StorefrontHeaderService` 隔离导航数据查询；`ViewModels` 与 `Models` 分离；`SessionCartStore` 和 `IImageStorageService` 可替换实现 | 面向接口和单一配置源组织代码，新增支付方式或替换存储时不改动多个页面 |
| **可测试性 Testability** | 覆盖面包括产品、购物车、订单、优惠券、账户安全、验证码错误锁定、图片上传、上传目录迁移、支付方式、Header、时间与订单号、资源一致性；使用 EF Core InMemory、服务替身、固定 `TimeProvider` 和前端 Node 回归测试 | `dotnet test` 运行 172 个 .NET 用例；Coverlet 输出过滤后的覆盖率并上传 CI 产物；`node --test` 运行 6 个前端用例 |
| **易用性 Usability** | 响应式主题与本地 Bootstrap；统一表单提交锁定和 `aria-busy` 状态；商品卡片、数量步进器、中英多语言、购物车徽标和自定义确认对话框 | 减少重复提交和误操作，界面反馈清晰，关键资源不受 CDN 中断影响 |
| **安全性 Security** | Identity 密码哈希 + Cookie 认证；后台 Admin 角色；认证与验证码接口 IP 限流；验证码错误 5 次失效；统一安全响应头；Razor 编码、参数化查询、CSRF 和站内重定向校验；上传文件多重校验；外部秘密配置；NuGet Audit | 认证、授权、限流、输入编码、上传校验、秘密管理和依赖审计形成纵深防御 |

> **答辩提示（Question Handling）**：上述表格即"每个属性由哪些代码体现"的索引。被问及时，建议按「**属性 → 落在哪个文件哪一段 → 解决了什么问题 → 若去掉会怎样**」的思路回答，例如：
> - 问"如何保证可修改性"→ 指 `Services/ICartStore.cs` + `SessionCartStore.cs`：换存储只需新增一个 `ICartStore` 实现并在 `Program.cs` 改一行注册，业务层零改动。
> - 问"购物车怎么防越权/防篡改"→ 会话隔离 + 防伪令牌 + `AddAsync` 内校验商品是否上架、库存是否大于 0。
> - 问"如何证明可测试"→ `dotnet test` 运行 172 个用例；固定 `TimeProvider` 与订单号生成器可验证时间敏感逻辑；另有 6 个前端回归用例。

## 后续扩展

当前未包含（可按需加入，服务层与模型已预留扩展点）：

- 支付宝、微信、银行卡和货到付款流程已提供演示选项；真实支付网关与回调验签仍需按商户配置接入
- 分类层级（父子分类）
- 购物车从 Session 持久化到数据库/Redis（实现 `ICartStore` 接口即可）
- 真实邮件发送需要在 Azure App Settings 中配置个人邮箱 SMTP 授权码，或配置 Brevo API

## 数据库迁移（EF Core Migrations）

项目已采用 EF Core Migrations（`Data/DbInitializer.cs` 中调用 `Database.MigrateAsync()`），首个迁移为 `Migrations/*_InitialCreate`。模型变更时：

1. 安装工具（如未安装）：`dotnet tool install --global dotnet-ef`
2. 新增迁移：在 `OnlineStore/` 目录下执行 `dotnet ef migrations add Xxx`
3. 应用迁移：`dotnet ef database update`（或直接 `dotnet run`，启动时会自动 `MigrateAsync()`）
