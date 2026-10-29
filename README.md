# 在线商店系统

基于 **ASP.NET Core 8 MVC** 的在线商店演示项目，采用分层架构与 SQL Server 数据库。系统围绕五项质量属性设计：**可用性（Availability）、可修改性（Modifiability）、可测试性（Testability）、易用性（Usability）、安全性（Security）**。

## 部署配置

版本化配置以 `.github/workflows/deploy-azure.yml` 为准：推送到 `HUZHANHAO` 或手动触发工作流后，执行还原、构建、全部 .NET 与前端测试，再发布到 `onlinestore-cc1f53` 并检查 `/health`。配置的目标地址为 https://onlinestore-cc1f53.azurewebsites.net；本地测试不代表已部署或已核验线上状态。

工作流需要仓库 Secret `AZURE_WEBAPP_PUBLISH_PROFILE`。应用数据库连接、邮件提供商与首次管理员凭据通过部署环境配置；不要把密码或发布配置文件加入源码。

HTTPS 由反向代理终止时，在部署环境设置 `ReverseProxy__KnownProxies__0`（后续条目按序编号）为实际可信代理 IP。应用默认只信任回环代理，并只处理一层转发；上线前检查 HTTPS 跳转及客户端来源解析，避免重定向循环或把所有用户合并进同一个限流额度。不要为方便而清空代理白名单。需要自定义 Data Protection 密钥目录时使用 `DataProtection__KeyDirectory`，并为部署实例配置持久化及目录权限。

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
| 记住账号 | 统一登录页勾选后，仅将账号（邮箱或手机号）经 Data Protection 加密写入 HttpOnly 持久化 Cookie，下次打开登录页自动回填；密码不会写入 Cookie，取消勾选即清除 |
| 数据库连接与数据操作 | EF Core + SQL Server，首次运行经 EF Migrations 自动建库建表并写入种子数据 |
| 商店主题界面 | 简洁黑白响应式主题、固定分类导航与下拉菜单、三图自动轮播、商品卡片、自定义细线购物袋图标、完整商店页脚、商品详情与评价区域 |
| 验证码注册/找回 | 6 位邮件验证码，10 分钟有效、60 秒重发间隔、最多 5 次验证尝试；并发请求共享数据库计数，正确码仅可使用一次。邮件发送失败会提示错误，接口不回显验证码 |
| 促销价（划线价） | 商品可设「原价」，前端显示划线原价 + 折扣标签，首页「限时折扣」专区 |
| 优惠券 | 支持百分比、固定金额、免运费、最高优惠、最低消费、日期范围、全局/每用户次数、首单限制、排除促销商品、全店/分类/商品范围；后台支持优惠券名称、批量创建、CSV 导出与核销记录，系统内部唯一标识不在页面展示；个人中心“我的优惠券”展示可用/未开始/已用完/已过期状态，结算页可直接选择优惠券并默认选择最优方案 |
| 取消订单与退款 | 买家可取消未发货订单，运输中申请拦截；后台发货、处理签收异常、审核退换货与物流拦截。退款/取消按状态恢复库存且只恢复一次；买家确认与七天自动确认在活动售后期间暂停 |
| 完整中英双语 | 界面文案 + 商品/分类/店铺/评价/参数/SKU 颜色规格/快递/图文详情等数据库内容全部随语言切换 |
| 自动化测试 | xUnit 覆盖商品、购物车、优惠券、订单/售后、身份与验证码、上传、查询边界及架构；Node 测试覆盖规格选择、实时更新与表单提示。最新实测结果见 [五项质量属性证据索引](docs/quality-evidence.md) |

## 目录结构

```
OnlineStore/             Web 启动项目：Controllers、ViewComponents、Areas/Admin、Views、wwwroot
OnlineStore.Business/    业务类库：Services 接口和实现、备用本地化器
OnlineStore.DataAccess/  数据类库：ApplicationDbContext、初始化、EF Migrations
OnlineStore.Models/      共享模型类库：实体、DTO/表单模型、验证规则、中英文资源
OnlineStore.Tests/       xUnit 服务、HTTP 与架构回归测试；Frontend 下为 Node 测试
```

引用方向为 Web → Business → DataAccess → Models，Web 与业务层也直接使用共享模型。业务类库不引用 Web，原命名空间保持不变。这些是独立编译的层；运行时仍由一个 ASP.NET Core 进程配合 SQL Server 提供服务，不代表分布式多服务器部署。

## 多语言 / 国际化（中英切换）

- 采用 ASP.NET Core 本地化（`IStringLocalizer<SharedResource>` + 资源文件）。
- 资源文件位于 `OnlineStore.Models/Resources/SharedResource.resx`（中文/默认）与 `SharedResource.en.resx`（英文），资源标记类保留 `OnlineStore.SharedResource` 命名空间。
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

版本化配置使用 `localhost` 实例。本机 SQL Server Express 实例如为 `localhost\SQLEXPRESS`，需要用开发配置或 User Secrets 覆盖；连接到学校电脑时应使用该电脑的实际实例和权限。示例：

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=OnlineStore;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;"
```

应用启动会执行 EF Migrations，因此连接账号需要相应建库/更新表结构权限。已有数据库保留数据，未应用的迁移按顺序补齐；首次初始化加入示例目录。开发演示账号与生产管理员初始化规则见下文。本机未提交的数据库配置不能作为其他组员电脑的通用配置。

## 运行

命令行（在解决方案根目录）：

```bash
dotnet restore OnlineStore.sln
dotnet build OnlineStore.sln
dotnet run --project OnlineStore/OnlineStore.csproj --launch-profile http
```

或用 Visual Studio 打开 `OnlineStore.sln`，将 **OnlineStore** Web 项目设为启动项目，选择 `http` 后按 F5。其余项目为类库或测试项目。

启动后浏览器访问（默认 http 配置）：**http://localhost:5080**

- 首页/目录：`/`
- 全部商品与筛选：`/Products`
- 商品详情与加入购物车：`/Products/Details/{id}`
- 购物车：`/Cart`
- 健康检查：`/health`（返回 Healthy/Unhealthy）
- 存活探针：`/health/live`（进程开始接收请求后，不依赖数据库）
- 就绪探针：`/health/ready`（数据库可连接时 200，不可用时 503；`/health` 保留同样的数据库检查）
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

全部前端回归测试：`node --test OnlineStore.Tests/Frontend/*.test.cjs`。服务及控制器测试使用隔离的内存数据库，HTTP 质量测试使用受控的本地请求管线；这些检查不能替代 SQL Server 迁移、事务与并发验证。

独立的 [CI 工作流](.github/workflows/ci.yml) 在 `Ethan-Yang`、`HUZHANHAO`、`main` 的 push 和目标 PR 上执行 .NET 构建与测试、Node 回归以及 SQL 工具的无数据库安全检查，上传 TRX/TAP。它不使用生产数据库或部署密钥；Azure 发布仍由单独的部署工作流负责。远程运行是否成功应查看对应 Actions 结果。

真实 SQL 的九组回归、专用验证库保护及重复运行方式见 [验证工具说明](docs/verification.md)。2026-10-29 本机结果为 .NET 261/261、Node 42/42、SQL 9/9、安全输入检查 17/17；隔离数据库故障演练十个步骤通过，恢复 ONLINE 后 5.168 秒观测到就绪探针及目录恢复 200。完整结果、实际页面截图和测量边界见 [中央证据索引](docs/quality-evidence.md)。

## 账号初始化

仅 `Development` 环境在账号不存在时创建本地演示管理员：

- 邮箱：`admin@store.com`
- 密码：`Admin@123456`

新开发库的演示买家为 `buyer@store.example`，初始密码同上。重启不会重置任何已有账号的密码，也不会删除或重新分配旧演示订单。

非开发环境不自动创建上述演示账号或虚构订单。首次创建管理员需要在受保护的环境配置中同时提供 `Bootstrap:AdminEmail` 与 `Bootstrap:AdminPassword`（环境变量名称为 `Bootstrap__AdminEmail` / `Bootstrap__AdminPassword`）；缺少任一项就跳过创建。已存在的同邮箱账号不会被自动授予管理员角色或重置密码。已有部署中的旧演示账号不会因更新代码自动删除，应由部署管理员检查并更换其公开初始密码。

密码策略：至少 6 位，含大小写字母、数字与特殊字符；验证发生在页面和 Identity 服务端。真实邮件仍需配置 SMTP 或 Brevo，缺少有效配置时不会伪造发送成功。

## 质量属性的代码依据与边界

| 属性 | 实现依据 | 实际边界 |
| --- | --- | --- |
| Availability（可用性） | 区分存活和数据库就绪；识别暂态数据库故障并返回安全 503、`Retry-After: 5`；十步骤隔离演练验证故障、恢复及稳定数据保持 | 单进程、单数据库仍有单点故障；5.168 秒是一次本机观察，不是 SLA。启动迁移完成前尚未监听 HTTP；写操作不自动重放 |
| Modifiability（可修改性） | 四个应用项目形成编译依赖边界；纯 `IOrderShippingPolicy` 接收物流事实、免邮结果和时间，替代策略可改变报价，库存及事务仍由 `OrderService` 管理 | 新增目的地、重量等计费因素仍需扩展输入；业务层仍依赖 EF，类库拆分不等于独立部署。详见 [物流策略说明](docs/quality-modifiability.md) |
| Testability（可测试性） | 纯规则、xUnit/HTTP、Node、九组真实 SQL 分工；仓库有验证工具和 CI；旧结构升级、末件竞争及验证码单次消费有可重复案例 | InMemory、受控 DOM、真实 SQL 和浏览器各自覆盖不同边界；本地通过不代表远程 CI 或生产运行通过 |
| Usability（易用性） | 自定义下拉保留 ARIA 名称、禁用项边界及错误焦点；订单故障有双语反馈，恢复后清除；交互控件聚焦时延后整页刷新；实际买家页及手机视口已核对 | 暂缓刷新可能延迟看到最新状态；未进行参与者实验或完整 WCAG 审计。详见 [交互验证说明](docs/quality-usability.md) |
| Security（安全性） | 保留 Identity、角色、防伪及归属校验；七个账户写入口共享每来源每分钟十次额度；可信代理、一层转发、安全响应头；验证码最多五次预留尝试且只能成功消费一次 | 限流为单进程状态，共享出口共用额度；代理与 HTTPS 需按部署环境配置。验证码仍明文存储，富文本清洗及完整图片解码仍待完善 |

每项属性的场景、可观察度量、代码与测试定位，以及答辩练习示例见 [五项质量属性证据索引](docs/quality-evidence.md)。个人理解、反思和贡献必须由成员本人提供；测试结果不代表已经验证学生掌握。

## 后续扩展

当前未包含（可按需加入，服务层与模型已预留扩展点）：

- 支付（下单已实现，支付环节为模拟，未接入真实支付网关）
- 分类层级（父子分类）
- 购物车从 Session 持久化到数据库/Redis（实现 `ICartStore` 接口即可）
- 真实邮件发送需配置 SMTP 服务器/凭据，或 Brevo API 与已验证发件人；不提供绕过验证码的生产注册入口

## 数据库迁移（EF Core Migrations）

数据库上下文与迁移位于 `OnlineStore.DataAccess`，启动配置位于 `OnlineStore`。迁移编号保持不变；不要因为分层移动文件而删除旧迁移或重建现有数据库。使用 EF Core 8 对应工具，并在解决方案目录显式指定项目：

```bash
dotnet ef migrations add ChangeName --project OnlineStore.DataAccess --startup-project OnlineStore -- --environment Development
dotnet ef database update --project OnlineStore.DataAccess --startup-project OnlineStore -- --environment Development
```

网站启动时也会执行 `MigrateAsync()`。首次运行新版本前应备份有价值的数据库；新字段迁移只在尚未应用时执行。2026-10-29 的验证码迁移为 `20261029140000_LimitVerificationCodeAttempts`，新增尝试次数，不清空已有业务表。

课程依据及上一轮修复记录见 [课程代码审查记录](docs/coursework-audit.md)；本轮新增机制与最终验证见 [五项质量属性证据索引](docs/quality-evidence.md)。
