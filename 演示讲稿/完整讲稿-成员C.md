# 成员 C 完整演示讲稿（主功能 + 用户界面 + 架构质量属性）

> 覆盖范围：成员 C 负责的模块 18–25。交接点：成员 B 已创建一个"待发货"订单，从这一刻起进入 C 的履约、售后、评价与经营数据阶段。
> 本文是单一完整讲稿，分四部分：一、主要功能（六条线）；二、用户界面；三、架构质量属性；四、问答准备。所有代码定位均对照当前工作区真实文件与行号。

## 演示账号与预置数据（准备时确认）

- 管理员：`admin@store.com` / `Admin@123456`（Development 环境自动创建，DbInitializer.cs 第 10-11 行）。
- 演示买家：`buyer@store.example` / `Admin@123456`（DbInitializer.cs 第 445-460 行）。
- 预置订单：DEMO-1001 待发货、DEMO-1002 已送达、DEMO-1003 已送达、DEMO-1004 已完成（含待处理退货）、DEMO-1005 已完成（含已批准换货），见 DbInitializer.cs 第 485-489 行。
- 说明：DEMO-1002 播种时状态是"已发货"，但它的发货时间被设成"10 秒前"、超过演示环境 9 秒的送达阈值，所以应用启动后会被后台任务立即推进成"已送达"。因此现场演示"发货→物流→送达"这条线请用 B 段新创建的订单，DEMO-1002、DEMO-1003 用来演示已送达后的自动确认倒计时。
- 现场演示优先用 B 段新创建的待发货订单：它发货后约 9 秒由服务端推进到已送达（演示加速阈值），效果干净。

---

# 一、主要功能（六条功能线）

## 线 1：正常履约线（模块 18、19、20、21）

主线：我的订单看到待发货 → 管理员发货 → 买家看物流时间线 → 已送达 → 买家主动确认收货 → 已完成。

### 演示路径

1. 买家窗口 → 头像菜单 →「我的订单」，指向 B 刚创建的待发货订单卡片。
2. 管理员窗口 → 后台「订单管理」，在订单列表找到该订单，点「发货」→ 填承运商、运单号 → 提交。
3. 回买家窗口：订单状态自动变「已发货」（前端轮询更新，不手动刷新）。
4. 买家点「查看物流」：物流节点逐一亮起，全部到达后状态变「已送达」。
5. 买家点「确认收货」：状态变「已完成」。

### 口播台词

"我的订单页展示的是订单快照：商品图、名称、颜色规格、数量、金额、状态、配送信息，以及我自己的评价和售后进度。卡片上的按钮不是写死的，是按状态渲染的——待发货只有取消订单；已发货有查看物流和申请退款拦截；已送达有确认收货和申请退货退款。买家永远只看到当前状态允许做的操作。"

"发货在后台订单管理完成。后台分成订单管理和售后申请两个分区，各自分页。发货时要求订单当前是待发货，承运商和运单号非空且不超过 50 个字符，通过后写入承运商、运单号、发货时间，把状态推进到已发货。"

"物流时间线每秒轮询一次接口，左上角有'每秒实时更新'的诚实提示——因为物流是模拟的，节点由服务端时间驱动。真正推进状态的是服务端：查看物流时会顺手推进，后台每分钟也扫描一次。买家确认收货时，服务端要求三重条件都满足：本人订单、状态是已送达、没有处理中售后，然后才变已完成。"

### 关键实现怎么讲

- 我的订单归属：OnlineStore/Controllers/OrdersController.cs 第 322-340 行 `MyOrders`，按 `GetByUserAsync(user.Id)` 只查本人订单。
- 按状态渲染按钮：OnlineStore/Views/Orders/MyOrders.cshtml 第 231-275 行，`@if (o.Status == OrderStatus.PendingShipment)` 等分支逐状态渲染。
- 发货状态机：OnlineStore.Business/Services/OrderService.cs 第 312-338 行 `ShipAsync`——`Status != PendingShipment` 直接返回 false；参数不合法返回 false；通过后置 `Status = Shipped`。
- 发货入口：OnlineStore/Areas/Admin/Controllers/OrdersController.cs 第 45-49 行 `Ship`，控制器第 9-10 行 `[Area("Admin")]`、`[Authorize(Roles = "Admin")]`。
- 物流推进：OrderService.cs 第 343-365 行 `GetTrackingAsync`，返回结构化节点（每个节点带 `IsReached`、`IsCurrentActive`）。
- 主动确认：OrderService.cs 第 432-475 行 `ConfirmReceiptAsync`，Serializable 事务 + 三重校验后置 `Completed`。

### 异常场景

对"已发货"订单，界面不会出现取消按钮；即使绕过界面发请求，控制器第 378-389 行 `Cancel` 要求订单属于当前用户，服务层 `CancelAsync`（OrderService.cs 第 294 行）再校验状态，不满足就返回失败。两层挡同一套规则。

---

## 线 2：自动确认线（模块 21）

主线：已送达后买家一直不操作 → 后台每分钟扫描 → 7 天到点自动确认收货 → 已完成。

### 演示路径

1. 用一条已送达订单，展示物流弹窗底部的"X 天 X 小时 X 分 X 秒后自动确认"倒计时。
2. 口播说明后台扫描与 7 天窗口，不现场等待。

### 口播台词

"买家可以主动确认收货；如果一直不操作，送达七天后系统自动确认。后台注册了一个托管服务，每分钟扫描一次，把到点的已发货推进成已送达、把到 7 天期限的已送达自动确认成已完成。"

"有两个可靠性的设计要讲。第一，每次扫描都从依赖注入作用域里拿新实例，单次失败不会污染上下文；循环里记错误日志但不中断。第二，更重要的是兜底：所有订单查询入口在返回数据前都会先做一次状态推进。也就是说，即使后台任务短暂失败或重启，下一次打开订单页，状态也会被补上。定时任务是主动扫描，读取时兜底是被动补课，两层叠加。"

### 关键实现怎么讲

- 后台扫描：OnlineStore.Business/Services/OrderConfirmationBackgroundService.cs 第 5 行起（`SweepInterval = 1 分钟`）；OnlineStore/Program.cs 第 60 行注册。
- 推进逻辑：OrderService.cs 第 972-988 行 `AdvanceDeliveryAsync`（仅"已发货"且超阈值才推进，写入 `DeliveredAt` 和 `AutoConfirmAt`）；第 990-1008 行 `TryAutoConfirmAsync`（仅"已送达"且到点才推进，`ReceiptConfirmedAutomatically = true`）。
- 读取兜底：OrderService.cs 第 936 行起 `ProcessPendingOrderStatesAsync`；`GetByUserAsync`（36 行）、`GetAdminOrdersPagedAsync`（110 行）、`GetCustomerOrderUpdatesAsync`（47 行）都在查询前调用它。
- 倒计时只是显示层：MyOrders.cshtml 第 495-497、651 行把服务端写入的 `AutoConfirmAt` 每秒格式化成剩余时间。

### 老师可能追问

"后台任务挂了怎么办？"——不会卡住，因为读取时兜底会补推进。

---

## 线 3：异常拦截线（模块 22）

主线分两条子线：签收异常、运输中退款/拦截。

### 演示路径

1. 子线 a（签收异常）：管理员对已送达订单记录签收异常和原因，状态变"签收异常"，自动确认暂停。
2. 子线 b（运输中退款）：买家对已发货订单申请退款/拦截，管理员处理拦截成功或失败。

### 口播台词

"异常路径有两条。第一条是签收异常：物流显示已送达但买家反馈没收到，管理员记录异常和原因，订单进入签收异常状态，自动确认被暂停，后续一般走退款。第二条是运输中退款：包裹还在路上，买家申请退款或拦截，管理员处理拦截——成功就拦截并退款，失败则继续送达、买家可以在送达后再申请退货。"

"这里有一个关键的联动规则：只要订单存在处理中的售后，状态推进和自动确认就暂停。这样就不会出现一边拦截、一边系统自动把订单完成的矛盾。"

### 关键实现怎么讲

- 签收异常：OrderService.cs 第 478-496 行 `ReportReceiptExceptionAsync`——仅"已送达"可报，记录原因、操作人和时间，置 `ReceiptException`。
- 签收异常后续退款：OrderService.cs 第 297-310 行 `RefundAsync`——允许从"签收异常"或"已完成"退款，且要求无处理中售后。
- 运输中退款：OrderService.cs 第 547 行起 `CreateAfterSalesAsync`——类型为"运输中退款"时仅"已发货"可申请；第 671 行起 `ResolveTransitCancellationAsync` 按拦截成败推动后续状态。
- 暂停联动：OrderService.cs 第 1242 行起 `HasActiveAfterSales`——已发货 + 待审核运输退款、或已送达 + 处理中售后时暂停推进。

---

## 线 4：售后线（模块 23，退货、换货共用一个框架）

主线：买家申请 → 管理员审核 → 买家填退回物流 → 管理员确认收货 → 完成售后（退货恢复库存，换货生成补发订单）。

### 演示路径

1. 用 DEMO-1004（已完成 + 待处理退货）：买家窗口展示申请记录和原因。
2. 管理员窗口「售后申请」区同意退货并填备注。
3. 口播补充 DEMO-1005（已批准换货）的换货补发订单。

### 口播台词

"售后按整单处理，走五步：买家提交申请和原因；管理员同意或拒绝并填备注；买家填退回承运商和运单号；管理员确认仓库收货；最后完成售后。"

"退货和换货前四步完全一样，只在最后收口处分叉：退货是恢复库存、订单变已退款；换货是恢复库存、同时生成一条关联的补发订单，补发订单重新走发货和签收流程。"

"这里有两个必须讲清的核心点。第一，防重复恢复库存：库存恢复方法里有一个幂等标记，只有第一次调用才真正加库存，重复请求不会把库存加两次。第二，换货为什么生成补发订单而不是原地改单：因为要保留原始订单记录，补发单带着'换货补发'的关联标记，重新走完整履约。"

### 关键实现怎么讲

- 申请：OrderService.cs 第 547 行起 `CreateAfterSalesAsync`——退货/换货仅"已送达/已完成"可申请，校验本人订单、原因 1-1000 字、无有效售后。
- 审核：OrderService.cs 第 649-669 行 `ReviewAfterSalesAsync`——仅"待审核"可审，同意→已同意、拒绝→已拒绝 + 备注。
- 退回物流：OrderService.cs 第 732 行起 `SubmitReturnShipmentAsync`（→退回中）、第 773 行起 `GetReturnTrackingAsync`（4 节点退货时间线）。
- 收货：OrderService.cs 第 858 行起 `MarkAfterSalesReceivedAsync`——仅"退回中"→已收货。
- 完成：OrderService.cs 第 877 行起 `CompleteAfterSalesAsync`——Serializable 事务内完成恢复库存 + 写订单状态 + 生成补发单。
- 防重复恢复库存：OrderService.cs 第 1133 行起 `RestoreStockAsync`——`InventoryRestoredAt` 幂等 + `ReloadAsync` 拿最新库存 + `checked` 加法。
- 换货补发单：OrderService.cs 第 1034 行起 `BuildReplacementOrder`——`ReplacementForOrderId = source.Id`，状态待发货；第 1171 行起 `ReserveStockAsync` 为补发单扣库存。
- 状态机统一入口：OrderService.cs 第 1096 行起 `TransitionAsync`——允许的源状态白名单 + 恢复库存 + 释放优惠券。

### 老师可能追问

"并发重复退款怎么防？"——库存恢复有幂等标记，且完成售后在 Serializable 事务里，状态校验和库存变更一起提交，第二次会因状态不满足而失败。

---

## 线 5：评价线（模块 24）

主线：已完成订单提交评价 → 同订单同商品再提交更新原评价 → 前台聚合展示 → 后台列表与删除。

### 演示路径

1. 买家窗口对已完成订单点"评价"→ 提交星级、文字、图片。
2. 商品详情页评价区刷新，展示平均分、星级分布、有图筛选、排序、分页。
3. 管理员窗口「评价管理」列表展示，可删除。

### 口播台词

"评价的前置条件是严格校验的：只有已完成订单里的商品能评价，订单必须属于当前用户。评价内容校验评分 1 到 5 星、评论不超过 1000 字，图片上传复用共享的图片校验机制。"

"一个关键行为：同一订单、同一商品再次提交时，是更新原来的评价而不是新增一条——这是用订单号、商品、用户三元组做唯一性判断实现的。"

"前台展示有平均分、星级分布、有图筛选、排序和分页；后台评价管理只有列表和删除，没有审批发布流程，这是要如实说明的实现边界。"

### 关键实现怎么讲

- 提交入口：OnlineStore/Controllers/ReviewsController.cs 第 38 行起 `Create`——`[ValidateAntiForgeryToken]`，校验订单归属 + 状态已完成 + 订单含该商品 + 评分范围。
- 更新原评价：OnlineStore.Business/Services/ReviewService.cs 第 104 行起 `CreateAsync`——按订单 + 商品 + 用户查已有评价，存在则更新，否则新增。
- 前台聚合：ReviewService.cs 第 17 行起 `GetProductReviewsPagedAsync`（分页/有图筛选/排序）、第 55 行起 `GetProductSummaryAsync`（平均分/星级分布）。
- 后台管理：Areas/Admin/Controllers/ReviewsController.cs 第 21、26 行 `Index`、`Delete`；ReviewService.cs 第 83、129 行。

---

## 线 6：经营数据线（模块 25）

主线：前面各订单状态汇总 → 仪表盘展示经营结果。

### 演示路径

1. 管理员窗口 → 后台「仪表盘」。
2. 依次指向四个指标卡、待办入口、收入趋势、热销商品、最近订单、最新评价。

### 口播台词

"仪表盘把经营结果汇总：有效收入、订单数、成交客户数、售出件数；待发货和低库存提醒；7 天和 30 天收入趋势；热销商品、最近订单和最新评价。"

"统计口径要讲清楚：有效收入只算待发货、已发货、已送达、已完成四种状态，并且排除换货补发单，否则补发订单会把收入重复计一遍。成交客户数是对用户去重后的数量。仪表盘在页面加载时读取数据，7 天与 30 天切换用的是已加载数据，不重新请求。"

### 关键实现怎么讲

- 入口：Areas/Admin/Controllers/DashboardController.cs 第 18 行 `Index`。
- 统计：OnlineStore.Business/Services/AdminDashboardService.cs 第 28 行起 `GetAsync`；第 9 行 `ValidRevenueStatuses` 定义有效状态；第 36 行排除 `ReplacementForOrderId` 非空的补发单。
- 指标：`TotalRevenue`、`TotalOrderCount`、`CustomerCount`（distinct UserId）、`UnitsSold`、`PendingShipmentCount`、`LowStockCount`、30 天 `Trend`、`RecentOrders`、`TopProducts`、`LatestReviews`。
- 视图：Areas/Admin/Views/Dashboard/Index.cshtml 第 15 行指标卡、第 37 行待办、第 53 行趋势切换、第 77/112/135 行三个经营面板。

---

# 二、用户界面（User Interfaces，5 分）

## 2.1 买家侧：我的订单页

文件：OnlineStore/Views/Orders/MyOrders.cshtml

台词："我的订单页从账户中心进入，右上角是订单总数。另外，导航里'我的订单'后面的数字是进行中订单数——有几笔进行中订单就显示几，不再按商品件数累加；它由 `GetActiveOrderCountAsync` 计算，前端脚本定时刷新。每笔订单是一张卡片：订单号、时间、承运商运单号或预计发货时间、支付方式、运费、备注；中间是商品行（图片、名称、颜色规格、数量、小计），图片加载失败自动换占位图标。状态徽标用 `StatusBadge` 样式函数加 `L10n.Status` 文案，样式与文案分离。售后进度直接嵌在卡片里：类型加状态徽标、管理员备注、以及按状态出现的'填写退货物流'或'查看退货物流'按钮。"

代码定位：第 12 行 `StatusBadge`；第 86 行状态徽标；第 168-222 行售后区；第 231-275 行按状态渲染的操作按钮。导航徽标：Views/Shared/_Layout.cshtml 第 104-109 行；OrderService.cs 第 528 行 `GetActiveOrderCountAsync`（返回 `.CountAsync()`）；Controllers/OrdersController.cs 第 346 行 `ActiveOrderCount`；wwwroot/js/order-count-badge.js。

## 2.2 物流时间线与自动确认倒计时

台词："物流弹窗的时间线节点有三种视觉状态：已到达打勾、当前高亮、未来灰显，数据来自服务端结构化的 `IsReached`/`IsCurrentActive` 标志。弹窗左上角有'每秒实时更新'指示器，前端每秒轮询。送达后弹窗底部出现自动确认倒计时——倒计时只是显示层，权威时间是服务端写入的 `AutoConfirmAt`。"

代码定位：MyOrders.cshtml 第 309、445 行"每秒实时更新"；第 555-570 行节点渲染；第 289、494-497、650-651 行倒计时。

## 2.3 管理员侧：后台订单管理

文件：OnlineStore/Areas/Admin/Views/Orders/Index.cshtml

台词："后台订单管理分订单管理和售后申请两个分区，各自分页、带数量角标。订单表六列：订单号、收货人、金额、状态、物流、操作。用券订单显示划线原价加实付红字；换货补发订单带'换货补发'徽标；状态徽标带 `aria-live`，状态自动更新时读屏器会播报。签收异常在物流列用红色显示原因。"

代码定位：第 35-48 行分区导航；第 80 行换货补发徽标；第 99 行状态徽标；第 118-119 行签收异常备注；第 152、285 行两个独立分页。

## 2.4 后台评价管理

文件：OnlineStore/Areas/Admin/Views/Reviews/Index.cshtml

台词："评价管理是列表加删除：商品名、用户名、星级、评论前 30 字预览、时间，删除带确认弹窗。这里如实说明边界：评价管理只有列表和删除，没有审批发布流程。"

代码定位：第 15-27 行表格；第 41-50 行删除与确认；第 58 行分页。

## 2.5 管理员仪表盘

文件：OnlineStore/Areas/Admin/Views/Dashboard/Index.cshtml

台词："仪表盘四个指标卡结构一致：标签加图标、大数字、口径说明，比如'有效订单'写明'不含取消与退款订单'。中间是待发货和低库存两个待办入口。收入趋势支持 7 天/30 天切换，并且有降级回退——canvas 图不可用时自动显示每日数字明细。下半区是热销商品、最近订单、最新评价三个面板。"

代码定位：第 15-36 行指标卡；第 37-46 行待办；第 53-66 行趋势与回退；第 77-150 行三个面板。

## 2.6 一致性设计（对应评分标准的落点）

台词："统一性体现在三件事。第一，状态徽标统一：前后台都走 `StatusBadge` 加 `L10n.Status`，改文案不用动样式。第二，双语统一：所有文字走 `L10n.Text("中文","English")`，缺英文回退中文，导航右侧的语言按钮切换当前文化。第三，组件统一：买家用 storefront/site.css 卡片体系，后台用 admin-shell.css 管理壳，两边共用 Bootstrap 表格、徽标、弹窗；移动端用栅格和 `table-responsive`，操作按钮自动换行。"

代码定位：Views/Shared/_Layout.cshtml 第 146-149 行语言切换；OnlineStore.Business/FallbackStringLocalizer.cs 回退机制；wwwroot/css/ 下 member-orders.css、admin-shell.css、admin-dashboard.css、site.css。

## 2.7 自动更新体验

台词："动态内容用 `data-*` 属性声明，脚本轮询局部更新：买家侧 customer-order-updates.js、后台 admin-order-updates.js、导航订单数 order-count-badge.js。导航上的'我的订单'徽标按订单计数——有几笔进行中订单就显示几，不按商品件数累加。收到 503 的 Retry-After 就延后下一次轮询、最多 60 秒；用户正在弹窗或输入框时暂缓整页刷新、提示'立即刷新'。这三段行为都有 Node 前端测试覆盖。"

代码定位：wwwroot/js/customer-order-updates.js 第 94-96、118-125 行；wwwroot/js/order-count-badge.js（`data-active-order-count-url` / `data-active-order-count-badge`）；测试在 OnlineStore.Tests/Frontend/ 下。

---

# 三、架构质量属性（Architectural Quality Attributes，15 分）

开场："质量属性部分我负责讲可用性和可测试性两个主讲属性，再补订单归属检查和自动更新对易用性的影响。每个属性都讲三样：代码实现位置、可重复的证据、真实的边界。可修改性由其他成员主讲，我只用售后模块点一句。"

## 3.1 Availability（主讲）：探针、后台任务、故障恢复

### 探针：live 与 ready 分开

台词："我们有三个健康检查端点。`/health/live` 永远只表达进程是否活着，Predicate 恒为 false，不查数据库；`/health` 和 `/health/ready` 带 ready 标签，检查数据库 `CanConnectAsync`，3 秒超时。数据库断掉时 live=200、ready=503，运维能立刻区分进程挂了还是依赖挂了。"

代码定位：Program.cs 第 93 行 `AddHealthChecks().AddCheck<DatabaseReadinessCheck>(tags: ["ready"], timeout: 3秒)`；Infrastructure/QualityProtectionExtensions.cs 的 `MapQualityHealthChecks`；Infrastructure/DatabaseReadinessCheck.cs。

### 后台任务 + 读取兜底

台词："后台托管服务每分钟扫描推进订单状态，单次失败不中断循环。更重要是读取兜底：所有订单查询入口返回数据前都先做一次状态推进，后台任务短暂失败也不影响状态追平。"

代码定位：OrderConfirmationBackgroundService.cs；OrderService.cs 第 936 行起 `ProcessPendingOrderStatesAsync` 及各查询入口。

### 数据库故障：可诊断、可恢复、不泄漏

台词："`DatabaseFailureMiddleware` 只接管暂态 SQL 错误、且响应还没开始写时接管，返回 503、`Retry-After: 5` 和请求 ID，双语静态页面不依赖数据库和 CDN，响应里没有 SQL 内部细节。写请求绝不自动重放——下单会扣库存、核销优惠券，自动重放可能重复扣，所以引导用户先查订单状态。"

代码定位：Infrastructure/DatabaseFailureMiddleware.cs；Infrastructure/FailureResponse.cs；Program.cs 第 131 行注册；测试 QualityPipelineTests.cs 第 71、93 行。

### 现场证据

台词："我们做过一次真实恢复演练：把数据库 OFFLINE，OFFLINE 期间 live=200、ready=503、目录 503 带 5 秒 Retry-After；ONLINE 后 5.168 秒就绪探针和目录恢复 200，恢复前后稳定数据一致。边界要说清：5.168 秒是一次本机观察，不是 SLA；单进程单数据库仍是单点。"

代码定位与证据：docs/evidence/recovery-run-2/recovery-verification.json（`recoveryMilliseconds=5168`）；docs/evidence/browser/ 下故障与恢复截图。

## 3.2 Testability（主讲）：状态转换与库存补偿的测试证据

台词："可测试性用自己的模块讲：订单状态机和售后库存补偿分支最多，测试也最多。"

代码定位（OrderServiceTests.cs，全部真实用例）：
- 第 139 行 `ConfirmReceipt_RequiresDeliveredAndCompletesManually`：只有已送达能确认。
- 第 161 行 `ProcessPendingOrderStates_AutoConfirmsAfterSevenDayWindow`：七天自动确认。
- 第 181 行 `ReportReceiptException_RecordsAuditPausesAutoConfirmAndAllowsRefund`：签收异常暂停自动确认。
- 第 67、97 行：运输退款拦截成功与失败两条分支。
- 第 319 行 `ReturnAfterSales_RestoresStockOnlyOnce_AndRefundDoesNotDoubleIt`：退货恢复库存只一次。
- 第 350 行 `ExchangeAfterSales_CreatesReplacementOrderWithoutChangingStock`：换货生成补发单。

代码定位（OrderReceiptSafetyTests.cs）：第 16 行有售后时确认被拒、第 43 行并发状态变化失败、第 72 行决策前重载订单。

台词（三层验证）："项目用三层验证：.NET 261 项、Node 42 项、真实 SQL 9 组、安全输入检查 17 项。我模块直接相关的是 SQL 九组里的'订单库存取消退款'和'末件竞争'——末件竞争要求两个订单抢最后一件库存时只有一个成功、库存归零。三层不能互相替代：InMemory 不模拟 SQL 锁和约束。"

局限（诚实说明）："测试证明这些分支和边界被验证过，不证明系统没有别的问题；前端测试用受控 DOM 替身，不是真浏览器。"

## 3.3 Security 补充：订单归属检查

台词："安全性由 B 主讲，我补订单归属。买家能访问的每个入口都先做 `order.UserId != user.Id → NotFound`，数据查询按当前用户 id 过滤。服务层再校验一遍。所以把 URL 里的订单号改成别人的，返回 404，连订单存在与否都不暴露。"

代码定位：Controllers/OrdersController.cs 第 313、382、397 行归属检查、第 330 行 `GetByUserAsync(user.Id)`；OrderService.cs 的 `ConfirmReceiptAsync`、`DeleteForUserAsync`、`CreateAfterSalesAsync` 内 UserId 比对；管理员侧 `[Authorize(Roles = "Admin")]`。

## 3.4 Usability 补充：自动更新体验

台词："轮询尊重服务端退避（503 带 Retry-After 就延后、封顶 60 秒），尊重用户正在做的事（有弹窗或输入焦点时暂缓整页刷新、提示立即刷新）。可用性的故障反馈，到前端变成具体的交互细节。"

代码定位：wwwroot/js/customer-order-updates.js 第 94-96、118-125 行。

## 3.5 Modifiability：一句带过

台词："售后按退货、换货、运输退款三种类型分开处理，新增售后类型不改订单主流程。可修改性的主讲解由其他成员负责。"

## 3.6 当场展示证据

1. `dotnet test OnlineStore.Tests/OnlineStore.Tests.csproj --configuration Release --filter "FullyQualifiedName~OrderServiceTests|FullyQualifiedName~OrderReceiptSafetyTests|FullyQualifiedName~QualityPipelineTests"`
2. `node --test OnlineStore.Tests/Frontend/customer-order-updates.test.cjs OnlineStore.Tests/Frontend/admin-order-updates.test.cjs OnlineStore.Tests/Frontend/order-count-badge.test.cjs`
3. 打开 docs/evidence/recovery-run-2/recovery-verification.json，指出 `recoveryMilliseconds=5168`。
4. 打开 docs/evidence/sql-verification 里"订单库存取消退款""末件竞争"两组结果（不现场重跑真实 SQL）。

---

# 四、问答准备（汇总）

- 问："订单状态可以随便跳吗？" 答："不能。每个操作在服务端校验前置状态，界面按钮按同一套状态常量渲染，只是提前挡掉不可能的操作。"
- 问："后台任务挂了订单会卡住吗？" 答："不会。定时任务是主动扫描，读取时兜底会补推进。"
- 问："倒计时放前端，到点可靠吗？" 答："可靠。前端只是把服务端的 AutoConfirmAt 格式化成剩余时间，真正推进靠后台扫描。"
- 问："买家能操作别人的订单吗？" 答："不能。列表按当前用户过滤，每个入口都有 UserId 归属检查，不匹配直接失败。"
- 问："并发重复退款怎么防？" 答："库存恢复有幂等标记，完成售后在 Serializable 事务里，第二次因状态不满足而失败。"
- 问："换货为什么生成补发订单？" 答："保留原订单记录，补发单带关联标记重新走履约，收入和库存口径都能区分。"
- 问："数据库断了 live 为什么还 200？" 答："live 只表达进程活着，ready 才查数据库，两者分开才能定位问题。"
- 问："5.168 秒说明高可用吗？" 答："不能，它是一次本机恢复观察，不是 SLA，单点仍然存在。"
- 问："写请求失败为什么不自动重放？" 答："下单扣库存、核销优惠券，自动重放会重复扣。"
- 问："物流是模拟的为什么还要演示？" 答："节点结构、运单号字段、状态推进逻辑都真实，模拟的只是谁来推时间，将来接真实承运商 API 时替换触发源即可。"

---

# 演示边界（统一提醒）

- 支付与物流是模拟的；发货后约 9 秒自动送达是演示加速阈值（`TrackingStep = 3 秒`）。
- 退款只改系统状态与库存，不代表真实资金退回。
- 7 天是自动确认期限，售后按整单处理，不能说成"按单件退款"或"强制 7 天退货"。
- 我的订单没有分页；后台订单与售后分别分页；仪表盘加载时读数据，7/30 天切换用已加载数据。
- 评价管理只有列表和删除，没有审批发布流程。
