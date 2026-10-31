# 本轮验证产物

这些文件是 2026-10-29 在隔离环境实际运行后保存的结果，不是测试预期值。完整解释见 [质量证据索引](../quality-evidence.md)。

- `test-results/push-final.trx`：提交前重新构建并执行的 261 项 .NET 测试。
- `frontend-final.txt`：42 项 Node 回归输出；提交前再次运行结果同为 42/42。
- `sql-repository-run-2/sql-verification.json`：九组真实 SQL 验证。
- `safety-checks.txt`：17 个不连接数据库的工具输入检查。
- `recovery-run-2/recovery-verification.json`：十步骤数据库故障恢复实验，含状态、计时和稳定数据快照。
- `browser/`：隔离测试库的真实页面截图，仅包含演示数据。
- `source-manifest.json`：对应验证源文件的 SHA-256，可核对后续变更；排除 bin、obj、第三方 lib 和本机配置。

源码基线为 `4bfdc48` 加本次提交。测试日志不等于完整覆盖率、生产部署验收或学生理解程度。本机连接配置、认证密钥、数据库文件和 Web 原始诊断日志没有纳入这些公开审阅产物。恢复 JSON 的随机验证库名和 E 盘路径用于复现与核对，不是日常业务库。
