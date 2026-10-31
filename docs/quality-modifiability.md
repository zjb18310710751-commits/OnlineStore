# 订单物流规则的可修改性

## 目标与设计

把下单时的物流快照、预计发货时间和运费规则从 `OrderService` 提取为纯 `IOrderShippingPolicy`。
现有 `IOrderService` 保持不变。订单服务负责读取并重新校验商品、事务、库存、优惠券和保存；策略只接收不可变物流事实、优惠券免邮结果与显式 UTC 时间，返回不可变报价。

选择这个边界，是因为修改配送收费或发货时效不应要求同时阅读订单取消、退款、售后与库存恢复逻辑。按 `partial` 文件拆分只能移动文字；把整个持久化工作流拆到多个有 DbContext 的服务则扩大事务协调风险。

## 文件和接口边界

| 文件 | 责任 |
| --- | --- |
| `OnlineStore.Business/Services/Shipping/IOrderShippingPolicy.cs` | 唯一策略入口：`Quote(IReadOnlyList<OrderShippingProduct>, bool freeShipping, DateTime utcNow)`。 |
| `OnlineStore.Business/Services/Shipping/OrderShippingProduct.cs` | 不可变的七个商品物流字段，不传入跟踪实体、库存或 DbContext。调用方保证每个商品只出现一次。 |
| `OnlineStore.Business/Services/Shipping/OrderShippingQuote.cs` | 不可变输出：六个物流文本、预计发货时间和运费。 |
| `OnlineStore.Business/Services/Shipping/OrderShippingPolicy.cs` | 默认规则；只处理输入，不查库、不保存、不读取系统时钟。 |
| `OnlineStore.Business/Services/OrderService.cs` | 将已从数据库重新加载的商品映射为物流事实；校验库存、优惠券后计算报价，扣库存前应用快照，继续原事务流程。 |
| `OnlineStore/Program.cs` | 将 `IOrderShippingPolicy` 注册到默认实现；订单服务构造器保留可选默认值，兼容既有直接构造调用。 |
| `OnlineStore.Tests/Services/OrderShippingPolicyTests.cs` | 纯规则测试；固定 UTC 时间，直接检查结果。 |
| `OnlineStore.Tests/Services/OrderShippingPolicyIntegrationTests.cs` | 替代策略通过 DI 进入真实下单服务，检查重新读取的订单和库存。 |

默认行为保持：按不同商品的正运费求和；免邮优惠券使运费为零；文本逐字段去空白、修剪并去重；发货地最长 200 字符、费用/时效最长 100 字符，截断不切断 UTF-16 代理对；从中文时效字段读取第一个数字作为小时数，未知内容回退 72 小时，多商品取最大值。

`OrderService` 从 1,298 行降至 1,262 行。行数下降有限；实际收益是物流规则可以在无数据库的环境下修改和验证，且替换规则不需要改订单服务入口、控制器或事务代码。

## 可验证的变更场景

| 需求 | 需要修改 | 无需修改 |
| --- | --- | --- |
| 把未知发货时间的默认值改为另一个时长 | 默认策略及其规则测试 | 控制器、IOrderService、数据库事务、库存更新 |
| 从逐商品运费相加改为合单最高运费 | 默认策略及其收费测试 | OrderService 的库存、优惠券和订单保存流程 |
| 用新的纯策略实现配送报价 | 新实现、DI 注册与策略测试 | IOrderService、订单控制器、数据库事务 |
| 按数量、目的地或重量计费 | 需要扩展输入合同、OrderService 映射与相应测试；若现有数据缺失还需模型修改 | 不能承诺只改一个文件 |
| 对接真实承运商实时 API | 另行设计异步调用、失败处理和事务时长；当前同步纯策略不直接承载网络 I/O | 不能套用纯规则替换的修改成本 |

换货单保留既有源订单快照及固定 48 小时预计发货逻辑，不在本次 checkout 策略范围内。物流轨迹文案、订单状态推进、售后规则等仍在订单服务中；本次没有宣称完成整个服务的职责拆解。

## 验证证据

先建立接口合同和替代策略测试，再修改生产规则。替代策略测试的 RED 在 `E:/DMU-Learning/.tmp/quality-uplift-20261029/pipeline-release.log`：期望运费 6、实际 3，证明旧服务忽略注册的策略；不是靠缺失类型或编译失败制造红测。首次 Debug 尝试因其它并行改动的待实现类型而未执行到测试，未算作行为证据。

替代策略测试使用同一商品两件、单商品原运费 3。它要求真实订单保存新运费 6、总额 26、全部替代快照和固定预计发货时间，并确认库存从 5 减为 3。测试还先污染跟踪实体的发货地，确认策略收到数据库重新加载的 Cambridge，而非未保存旧值。

八个纯规则测试用固定时间覆盖双语去重、最大时效、四种无法解析的时效、收费/免邮和所有快照字段的长度/Unicode 边界。既有 `OrderShippingSnapshotTests` 和 `CommerceRegressionTests` 覆盖真实下单快照、按商品收费和免邮优惠券等回归。

主代理以 Release 完成首轮 GREEN：42/42 定向测试通过，包含本次 9 个新增测试、既有订单快照与 commerce 回归，以及 4 个 HTTP 质量管线测试。结果文件为 `E:/DMU-Learning/.tmp/quality-uplift-20261029/test-results/quality-focused.trx`，已读取并核对 passed=42、failed=0。完整回归由主代理统一运行，最终结果见本任务验证记录。上述内存数据库集成测试验证结果与持久化流程，不替代 SQL Server 的并发事务测试。

## 约束

不改数据库结构、配置或迁移；不访问真实数据库。新策略不持有 `DbContext`、实体、库存或优惠券写入权。保留 Serializable 事务、商品重新加载、库存和优惠券修改、SaveChanges、提交以及并发失败处理。

策略是可信的内部实现，需返回非负运费以及符合订单列长度的快照。当前时效解析仍是旧的“首个数字表示小时”规则，不自动理解“天”、工作日或时区；本次只隔离规则，没有扩大业务语义。接口增加了一层映射与少量对象分配，以换取明确的规则边界。

修改前的订单服务备份：`E:/DMU-Learning/.tmp/quality-uplift-20261029/modifiability-before/OrderService.cs`。
