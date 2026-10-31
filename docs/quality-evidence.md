# 五项质量属性实现与验证证据

2026-10-29（Europe/London）的本机验证结果为 .NET 261/261、Node 42/42、真实 SQL 9/9、SQL 安全输入检查 17/17。隔离数据库故障演练十个步骤全部通过：数据库 OFFLINE 时，存活探针仍返回 200，就绪探针和商品目录返回 503；恢复 ONLINE 后 5.168 秒观测到就绪与目录恢复 200，稳定数据对比相同。

本索引对应基线 `4bfdc48` 及本轮质量改动。最终提交身份以 Git 历史为准。这里记录本地已执行结果；远程 CI 需要对应 GitHub Actions 运行记录，不能由本地测试推断。原始产物根目录为 `E:/DMU-Learning/.tmp/quality-uplift-20261029/`，下文简称 Q；便于仓库审阅的副本集中在 [docs/evidence](evidence/)。

## 最终结果索引

| 验证层 | 已观察结果 | 仓库证据 | 可支持的结论 |
| --- | --- | --- | --- |
| Release .NET 回归 | 推送前复跑 261 执行、261 通过、0 失败 | [推送前 TRX](evidence/test-results/push-final.trx)；[上一轮 261 项 TRX](evidence/test-results/quality-final-261.trx) | 本轮服务、控制器、架构及 HTTP 测试案例通过 |
| Node 前端回归 | 42 执行、42 通过、0 失败 | [Node 输出](evidence/frontend-final.txt) | 真实脚本在受控 DOM/网络输入下满足断言 |
| SQL Server 回归 | 9 组完成、0 失败、`passed=true`；15:50:07 至 15:50:14 UTC | [SQL JSON](evidence/sql-repository-run-2/sql-verification.json) | 真实关系数据库的迁移、并发和持久化案例通过 |
| SQL 工具安全输入 | 推送前复跑 17/17 通过，不连接数据库 | [安全检查输出](evidence/safety-checks.txt) | 对日常库名、非 master 连接、远程主机、非 E 盘路径等的拒绝规则通过 |
| 实际发布的故障恢复 | 10 步骤全通过、`recoveryMilliseconds=5168`、`dataComparison.equal=true` | [恢复演练 JSON](evidence/recovery-run-2/recovery-verification.json) | 单次本机隔离数据库 OFFLINE/ONLINE 后，读取和稳定数据恢复符合检查 |
| 真实浏览器页面 | 发布地址 5097：排序 End/Enter 生效；买家订单故障提示自动出现，ONLINE 后自动消失；390×844 下图片、总价、按钮可见 | [故障状态](evidence/browser/order-service-unavailable.png)、[恢复状态](evidence/browser/order-recovered.png)、[手机视口](evidence/browser/order-mobile.png) | 对应实际页面与交互已观察，不能覆盖全站所有状态 |
| 组件浏览器补充 | 统一 CUA 检查选择框必填焦点、Home/End/Enter/Escape/Tab，503 恢复及操作焦点保持 | [组件验证说明](quality-usability.md)；原图保留于 `Q/usability-browser/` | 真实共享脚本与 CSS 的交互补充；该 5119 夹具不运行 ASP.NET 或 SQL |

首轮恢复演练保存在 `Q/recovery-run/recovery-verification.json`，因基线数值读取发生 `InvalidCastException` 而失败，尚未执行数据库 OFFLINE。它是失败历史，不计入通过结果。修复 SQL 数值读取后使用新目录完成第二轮，旧结果未覆盖。

## Availability 可用性

场景：进程已经开始接收请求时，数据库暂时不可连接。应用应保持可诊断的失败状态，在依赖恢复后允许后续读取成功，并让用户知道订单状态需要重新核对。

实现位于 [DatabaseReadinessCheck](../OnlineStore/Infrastructure/DatabaseReadinessCheck.cs)、[DatabaseFailureMiddleware](../OnlineStore/Infrastructure/DatabaseFailureMiddleware.cs)、[FailureResponse](../OnlineStore/Infrastructure/FailureResponse.cs) 和 [QualityProtectionExtensions](../OnlineStore/Infrastructure/QualityProtectionExtensions.cs)。`/health/live` 不检查数据库；`/health/ready` 与兼容入口 `/health` 检查数据库。响应尚未开始时，已识别的暂态数据库故障得到通用 503、请求标识和 `Retry-After: 5`，约束错误保留原错误处理路径。没有在服务器自动重放下单 POST。

可观察度量与本次结果：OFFLINE 期间 live=200、ready=503、`/Products`=503；目录响应带 5 秒 Retry-After，响应检查没有发现 SQL 内部细节。`ALTER DATABASE ONLINE` 完成后开始计时，5168 ms 后就绪探针和目录均返回 200，低于演练设置的本地 20 秒目标。稳定数据对比相同；测试数据库已恢复 ONLINE，演练启动的子进程已结束。

`QualityPipelineTests.DatabaseFailure_Is503WithoutSecrets_AndNextRequestRecovers`、`FailedWrite_IsNeverAutomaticallyRetried_AndJsonHasNoException` 及独立恢复报告共同提供证据。数据对比排除了后台任务可以合法推进的订单状态和收货时间；不能将 `equal=true` 解释为数据库每个字节都未变化。三秒探针取消预算依赖 provider 响应取消，启动迁移完成前尚未监听 HTTP。单进程和单数据库仍有单点故障，本次耗时不构成长期可用率或 SLA。

## Modifiability 可修改性

场景：调整下单配送收费或默认发货时长，同时保留库存、优惠券和订单事务的处理方式。[IOrderShippingPolicy](../OnlineStore.Business/Services/Shipping/IOrderShippingPolicy.cs) 定义纯计算边界，输入为物流事实、免邮结果和显式 UTC 时间，输出为不可变报价。[OrderService](../OnlineStore.Business/Services/OrderService.cs) 重新加载商品后调用策略，继续在原 Serializable 事务内扣库存并保存。

可观察度量：替代策略只通过 DI 替换，原运费 3 的商品购买两件后，保存的运费为 6、订单总额为 26、库存由 5 减至 3，并保存替代物流快照。`OrderShippingPolicyIntegrationTests.CreateAsync_UsesReplacementShippingPolicy_AndPersistsItsQuoteWithStockChange` 覆盖这一结果；纯策略测试覆盖固定时间、去重、免邮及 UTF-16 截断边界，均纳入 261 项最终回归。原实现忽略策略的 RED 结果及具体定位见 [物流策略说明](quality-modifiability.md)。

修改范围能缩小，是因为报价计算与持久化职责有清楚边界。目的地、重量、购买数量尚未进入策略合同，加入这些因素仍需改输入和映射；真实承运商 API 还需要异步失败处理。四个应用项目仍运行于一个 Web 进程，Business 仍依赖 EF，不能由此推断可独立部署或无成本替换数据库。

## Testability 可测试性

场景：另一名开发者能从仓库重复运行检查，并区分规则错误、浏览器交互错误和关系数据库行为。快速层用固定输入及隔离状态，SQL 层使用工具自行生成的验证库，避免把日常库当作测试目标。

本轮度量为 261 项 .NET、42 项 Node、9 组真实 SQL 和 17 个安全输入检查通过。SQL 九组覆盖全新迁移、旧迁移升级、开发种子、并发错误验证码、正确验证码单次消费、商品 CRUD/筛选分页、订单库存取消退款、末件竞争和双语物流快照。末件竞争要求仅一单成功且库存归零；旧结构升级保留旧验证码行，默认尝试次数为零并可成功消费。

源码入口为 [测试项目](../OnlineStore.Tests/)、[SQL 工具](../tools/verification/SqlChecks/)、[PowerShell 入口](../tools/verification/run-sql-checks.ps1) 和 [CI 工作流](../.github/workflows/ci.yml)。CI 在指定分支的 push/PR 执行构建、.NET/Node 回归和无数据库安全检查，上传 TRX/TAP；真实 SQL 仍由本机显式运行。操作步骤见 [验证工具说明](verification.md)。InMemory 不模拟 SQL 锁与全部约束，Node 替身也不模拟全部浏览器事件，因此实际 SQL 和浏览器证据单独列出。

## Usability 易用性

场景：用户用键盘选择选项、修正必填字段，并在订单服务短暂失败时继续理解当前页面。`site.js` 保留原字段的可访问名称和错误描述，Home/End 跳过禁用项；自定义 select 验证失败时焦点落到可见 combobox。只有实际指针移动才改变选项高亮，避免展开菜单时静止鼠标覆盖键盘选择。

订单轮询在 `customer-order-updates.js` 中给出双语恢复操作。收到数字 Retry-After 时延后自动请求；普通网络异常保留一秒轮询。状态变化采用整页重载，弹窗或交互控件有焦点时暂缓，并提供立即刷新。恢复后清除失败提示；重试按钮仍聚焦时先保留恢复说明，避免焦点随按钮一起消失。

42 项 Node 回归包含新增 13 项交互检查。真实发布版本已完成排序 End/Enter、测试数据库断开时自动显示 Retry now、ONLINE 后提示自动消失，以及 390×844 买家订单页观察。组件夹具另验证必填错误、Escape 取消、Tab 退出及焦点保持。详细步骤和截图定位见 [易用性说明](quality-usability.md)。暂缓刷新可能延迟新状态出现；当前没有通用草稿持久化。没有参与者任务完成率、满意度或完整 WCAG 合规结论。

## Security 安全性

场景：同一来源密集访问账户入口，或伪造 X-Forwarded-For 尝试取得新额度。`QualityProtectionExtensions` 限定可信代理及一层转发，七个账户写入口共享每来源每分钟十次、无排队的固定窗口策略，超额返回 429 及等待时间。`SecurityResponseHeadersMiddleware` 增加 nosniff、同源嵌入及有限 CSP；原 Identity、Admin 角色、防伪和资源归属校验保持。

可观察度量：`QualityPipelineTests.AccountLimit_RejectsEleventhRequest_DespiteSpoofedForwardedAddresses` 要求前十次接受、第十一次 429；可信代理测试检查不同真实来源拥有不同额度。相应测试已在最终 TRX 中通过。SQL 并发验证码场景中，十二次错误请求全部失败、尝试计数止于五；八次正确并发请求仅一次成功。验证工具的安全输入检查拒绝日常库连接和非法数据库名，真实 SQL 操作只进入工具创建的库。

限流状态仅存在于单进程，共享出口共用额度；部署必须配置实际可信代理并核对 HTTPS。验证码仍明文保存在数据库，管理员富文本仍需清洗，图片签名检查不替代完整解码或恶意内容扫描。错误响应不返回内部异常，但服务器日志仍需受控保存。具体测试证明的是这些入口的覆盖行为，不构成全应用安全认证。

## 五项属性如何共同工作

数据库故障恢复连接了 Availability、Usability 和 Security：503 使程序能判断故障，页面保留订单并解释如何恢复，通用错误限制信息泄漏。写请求不自动重放保护交易一致性，同时要求用户先核对订单，不能把友好提示当作幂等保证。

物流策略连接了 Modifiability 和 Testability：收费计算可以用固定时间独立验证，库存与优惠券仍留在统一事务里。SQL 回归继续检查事务结果，使策略替换不会只在纯函数测试中看起来正确。高隔离级别的锁竞争成本仍需要真实负载测量。

来源限流同时影响 Security、Availability 和 Usability：它限制敏感入口消耗，也会影响共享网络用户。可靠代理配置使计数依据正确，429 等待提示说明何时再试；增加并行实例时还需要统一限流状态。

## 答辩练习示例

以下是基于代码的讲解示例，供成员用自己的语言练习，不代表任何成员已经独立实现或掌握。个人贡献应关联实际提交、修改文件和本人验证记录。

| 可问的问题 | 可以展示的代码或结果 | 回答应说明的边界 |
| --- | --- | --- |
| 数据库断开时为什么 live 仍是 200？ | `MapQualityHealthChecks` 的不同 Predicate；恢复报告中的 live=200、ready=503 | live 表示进程已能响应，不保证交易可完成；启动迁移前还没有 HTTP 监听 |
| 5.168 秒说明系统高可用吗？ | 第二次恢复 JSON 的计时起点、`recoveryMilliseconds` 及成功探针 | 它是 ONLINE 后一次本机观察；没有长期可用率、冗余或生产 SLA 证据 |
| 为什么抽物流策略而不拆订单事务？ | `IOrderShippingPolicy.Quote` 与 `OrderService.CreateAsync`；替代运费 6、总额 26、库存 3 | 报价可以替换；库存和保存仍须共同提交。新计费因素可能要求扩展合同 |
| 为什么还要真实 SQL 测试？ | 末件竞争、验证码单次消费及旧迁移升级三组结果 | InMemory 不能证明 SQL 锁、约束和旧结构升级行为 |
| 订单自动刷新怎样尊重键盘用户？ | `hasOpenEditor`、`hasFocusedAction`；组件焦点截图与真实订单失败/恢复图 | 当前延后整页刷新，未持久化所有离焦草稿；恢复提示不是用户满意度数据 |
| 为什么伪造转发头不能换一个限流额度？ | `ForwardedHeadersOptions` 配置及第十一次 429 测试 | 只接受可信代理转发；部署配置错误仍可能把共享代理下的用户合并 |

答辩前，成员应实际找到上述入口并复述“触发条件、执行路径、可观察结果、限制”，再记录自己可以独立解释的部分。未经过个人尝试的知识状态仍为尚未验证。
