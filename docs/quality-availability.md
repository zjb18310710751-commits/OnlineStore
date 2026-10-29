# 数据库故障与恢复验证

本地工具用全新隔离 SQL Server 数据库和独立 Web 进程演示短暂数据库故障。它不停止 SQL Server 服务、不接触日常站点进程，也不接受已有数据库名。

## 运行命令

前提是 Windows、运行中的本机 SQL Server、一个由 SQL Server 服务账号可写的 E 盘数据目录，以及已发布到 E 盘的当前 Web 应用。连接必须显式指向本机 `master`，登录需有新建数据库及切换自己创建数据库状态的权限。

在仓库根目录运行，按机器实际配置替换实例与目录；输出目录必须不存在或为空：

```powershell
$env:ONLINESTORE_SQL_TEST_CONNECTION = 'Server=localhost\SQLEXPRESS;Database=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;'
$env:ONLINESTORE_SQL_TEST_DATA_DIRECTORY = 'E:\OnlineStoreVerification\sql-data'

dotnet publish OnlineStore/OnlineStore.csproj -c Release -o 'E:\OnlineStoreVerification\publish'
dotnet build tools/verification/SqlChecks/SqlChecks.csproj -c Release
dotnet run --project tools/verification/SqlChecks/SqlChecks.csproj -c Release --no-build --no-restore -- --recovery 'E:\OnlineStoreVerification\recovery-run-01' 'E:\OnlineStoreVerification\publish\OnlineStore.dll'
```

环境变量只用于当前终端进程；不要写入机器级配置，不要把 SQL 凭据提交到仓库。示例 `TrustServerCertificate=True` 仅用于本地开发实例的自签名证书。SDK、包缓存和临时文件也应使用已配置的 E 盘工作环境；原有 SQL 回归命令及其九组测试保持不变。

工具给自己启动的子进程单独设置 Development 环境、临时 loopback HTTP 端口、新库连接（连接超时 3 秒，连接重试数 0），以及 E 盘 Data Protection 密钥、TEMP/TMP 和 DOTNET_CLI_HOME 目录。这些设置不会改变机器环境或日常 Web 进程。发布应用需包含 `DataProtection:KeyDirectory` 配置支持。

## 观察与判定

1. 生成 `OnlineStore_Verification_<日期>_<时间>_<GUID>` 名称。先确认 `DB_ID` 不存在，且目标 MDF/LDF 文件不存在，再在指定 E 目录创建数据库。
2. 启动独立发布应用，最多等待 60 秒使 `/health/ready` 返回 200；随后记录 `/health/live` 和 `/Products` 的 200。
3. 保存稳定数据快照：商品、订单与订单明细数量，订单总额、明细数量总和，以及逐商品、逐 SKU 的库存。
4. 核对本次创建成功标志、生成名称、数据库 ID 与两个物理文件路径后，只将这个库设为 `OFFLINE WITH ROLLBACK IMMEDIATE`。
5. 离线期间必须观察到 `/health/live` 为 200、`/health/ready` 为 503、`/Products` 为 503；商品页需带 `Retry-After` 且不暴露 SQL 异常类型或堆栈。
6. 在 `finally` 中执行同样所有权校验并将库恢复 `ONLINE`。之后轮询就绪和商品页，要求在本机目标 20 秒内重新返回 200。计时从 `ALTER DATABASE ... ONLINE` 返回后开始，切换上线的耗时单独记录。
7. 比较恢复前后稳定数据快照，最后停止且仅停止工具启动的 Web 进程。数据库及数据文件保留，不执行 DROP。

只有全部必需步骤通过才返回退出码 0。初始化、HTTP、数据库恢复或数据一致性验证失败都应视为未通过。Ctrl+C 会进入恢复和进程清理流程；强制结束工具、操作系统关机或 SQL 服务自身故障可能使恢复无法完成，不能把 `finally` 视为跨进程崩溃保证。

## 证据文件

输出目录保留：

- `recovery-verification.json`：整体通过标志、每步结果/耗时、各阶段 HTTP 状态/耗时、生成库名与文件路径、是否已恢复 ONLINE、前后数据快照，以及限制说明。
- `recovery-verification.log`：工具步骤记录，不输出连接字符串。
- `web-stdout.log`、`web-stderr.log`：独立 Web 进程的诊断输出；已知连接字符串和密码字段会遮蔽，仍应按本地诊断材料保存。
- `keys/`、`temp/`、`dotnet-home/`：仅用于该子进程的 E 盘运行文件。

失败时先查看 `database.offlineAttempted` 和 `database.onlineRestored`。如果已离线但没有成功上线，应在 SSMS 核实 JSON 中**精确的本次生成库名、ID、MDF/LDF 路径**，只恢复该库；不要将日常应用库作为替代目标。留存测试库不再需要时，由使用者核对后另行清理。

## 范围与限制

20 秒是这次本机实验的验收目标，不是生产 SLA、平均恢复时间或并发负载下的保证。连接恢复没有自动重试订单提交：工具只轮询 GET，现有 HTTP 单元测试另外验证发生写入异常时处理程序仅调用一次。

就绪探针的 3 秒是传给数据库提供程序的合作式取消预算，不能声明每个网络状态下绝对在 3 秒返回。HTTP 客户端每次取样另有 8 秒取消预算。

应用仍在开始监听前等待数据库迁移和种子初始化，因此**冷启动初始化期间 `/health/live` 也不可访问**。部署时需区分允许初始化耗时的启动探针与运行期间的存活探针；本工具在启动成功后再注入故障。

后台确认服务可以合法推进种子订单状态和签收时间，所以这些字段不进入一致性快照。计数、总额和库存比较能检查本次实验关注的不变量，但不能证明所有字段未变，也不能取代事务回滚、并发写入或灾难恢复测试。

本工具一次只创建一个随机新库；端口先通过 loopback 临时绑定分配，再交给子进程监听。若在这段间隙被其它进程占用，本次运行会失败并清理自己启动的进程，不会结束占用端口的其它进程。

## 2026-10-29 实测结果

[本轮恢复记录](evidence/recovery-run-2/recovery-verification.json)十个必需步骤均通过。数据库离线期间，live=200、ready=503、商品页=503，商品响应附带 Retry-After: 5。ONLINE 完成后 5168 ms 观测到 ready 与商品页都恢复 200；稳定字段快照相等，测试库已恢复在线、工具子进程已关闭。首轮工具读取 SQL float 时错误地使用 decimal，验证在故障注入前失败；按真实 EF 存储类型修复后使用全新输出目录重跑，未覆盖失败记录。
