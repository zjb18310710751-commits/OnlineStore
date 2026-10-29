# 安全性设计与部署边界

## 场景与实现

未认证请求或登录用户短时间调用账号敏感入口时，每个可信来源IP共用一分钟10次额度，无排队；超过返回429与Retry-After。Login、Register、SendCode、ForgotPassword、Profile、ChangePassword、SendProfileEmailCode的POST统一使用account策略。密码/验证码的持久化失败次数、一次性消费和Identity锁定仍独立生效；IP限流不是这些校验的替代。

`QualityProtectionExtensions`只接受回环代理及显式`ReverseProxy:KnownProxies`中的地址，最多处理一跳。PostConfigure重建白名单，防止此前清空列表的云配置意外变成信任所有来源。限流按转发解析后的RemoteIpAddress进行，不读取未经验证的X-Forwarded-For。

`FailureResponse`根据Accept、JSON内容类型或现有AJAX约定返回JSON；普通页面得到无需数据库/CDN的中英HTML。响应不含异常、服务器名或连接密码，有请求ID供运维关联日志。安全头含nosniff、same-origin referrer、SAMEORIGIN，以及仅约束frame-ancestors/base-uri/object-src的CSP；这不是完整脚本CSP。

## 可重复验证

`dotnet test OnlineStore.Tests/OnlineStore.Tests.csproj -c Release --filter FullyQualifiedName~QualityPipelineTests`

真实本地HTTP验证：未知代理轮换伪造X-Forwarded-For，第11次仍429；此前存在trust-all配置也不能绕过；可信回环代理提供12个不同客户端地址，各自仍有额度。账号POST元数据测试防止遗漏新入口。JSON请求只设置Content-Type、不设置Accept时仍得到结构化故障响应。数据库约束等确定性错误不被标为503可重试。

本轮交叉复核先发现个人资料发送入口遗漏、JSON协商遗漏和异常分类过宽；对应测试先失败后通过。再以“先前配置清空代理名单”场景复现限流绕过，并用PostConfigure收紧，保留回归。

## 部署迁移要求

本轮不部署Azure，线上代理链尚未验收。默认仅回环代理；Azure Linux、容器或非回环负载均衡器需运维根据实际连接来源配置`ReverseProxy__KnownProxies__0`、`ReverseProxy__KnownProxies__1`等，值必须是真实受信IP，不填客户端IP，也不能照抄示例。确认网络只允许这些代理访问后端。当前仅支持一跳；多跳拓扑需要独立设计和验证。

在预发布环境核对HTTPS首页与登录不存在重定向循环、两个客户端不会错误共用代理地址额度、未知来源的转发头被忽略。不要通过重新信任所有代理或单独开启`ASPNETCORE_FORWARDEDHEADERS_ENABLED`来绕过检查。微软的[代理配置说明](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-8.0)解释了可信来源、转发协议与HTTPS重定向之间的关系；本项目选择显式白名单。

可用`DataProtection__KeyDirectory`指定持久密钥目录。验证进程指定E盘；生产目录需访问控制与备份，未配置密钥静态加密时不得把目录当作普通公开文件。已有生产公开默认账号、SMTP/Brevo凭据和云权限须单独检查，不能用本机测试替代。

## 与其他属性的取舍

限速减少数据库和邮件压力，帮助可用性；429/503的双语可恢复反馈帮助易用性；统一策略减少遗漏，帮助可修改性；真实HTTP负向用例提供可测试性证据。代价是同一NAT的多用户共享额度、进程重启重置IP桶、单实例限流不具备分布式一致性。本项目没有宣称防住分布式攻击；可按真实流量另设边缘限流。日志在服务器受控保存，页面只返回请求ID。
