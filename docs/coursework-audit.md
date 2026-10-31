# OnlineStore 课程代码审查

日期：2026-10-29（Europe/London）。审查对象为 `E:/Daily/team work`，基线提交 `37c7ad8`。本次修改在本地完成；本文记录课程依据、实际缺陷、清理范围和验证边界，不预测最终成绩。

## 课程依据

资料来自用户指定的 `C:/Users/Administrator/Desktop/courseware/`。共36份文件：21份DOCX、15份PPTX，合计430张幻灯片。已读取正文与表格，并核对206个嵌入媒体：188个实际查看、18个以SHA256确认与已查看图像重复，14个EMF通过转换后的PNG检查。没有逐张渲染430张幻灯片整页，没有核验Word实际分页，也未观看外链视频；因此以下使用幻灯片编号或DOCX标题/表格/段落定位。复习课件含2025旧时间表，不能作为2026本期截止日期。

| 来源 | 实际要求及本次用途 |
| --- | --- |
| `CEM coursework brief - Copy (1) - Copy.docx`，主表的学习成果、Tasks、Main Functions、Report部分（提取block3） | ASP.NET/C#/MVC/n-tier；商品CRUD、目录筛选分页、购物车；五项质量属性；源代码、视频、报告与AI声明 |
| `Rubric- for students.docx`，System表和Report表（block2、7） | 演示：功能20、界面5、质量属性15、答辩10；报告：架构10、质量属性20、反思10、贡献10 |
| `Week1/1_1_Introduction to ASP.Net MVC Framework - Copy (1).docx`，Model职责（block9） | 业务规则应位于独立项目；用于判断只分文件夹不足以严格对齐课堂实践 |
| `Week4/4_2_N-Tier Architecture.docx`，项目分离步骤（block19–32），全部8张截图 | DataAccess/Models类库、项目引用、移动Data/Models/Migrations；Utility为可选，不强制照搬实验项目名称 |
| `Week2/2_1_Chapter 4_Availability - Copy - Copy.pptx`，第15–20张 | 检测故障、异常处理、回滚、重试及预防；用于检查健康检查、错误页面及事务 |
| `Week3/3_1_Chapter 8_Modifiability - Copy - Copy.pptx`，第13–21、30–33张 | 职责分离、接口封装、限制依赖、配置绑定、MVC及其权衡 |
| `Week3/3_2_Chapter 11_Testability - Copy.pptx`，第12–22张 | 可控状态、sandbox、可执行断言、降低复杂度、构造注入；用于隔离测试及回归设计 |
| `Week3/3_3_Chapter 12_Usability - Copy.pptx`，第3–4、12–22张；`3_3_Validation in Model and View.docx`正文及3张截图 | 输入验证、错误反馈、用户控制与MVC分离；不把教学中的Undo例子当成每个作业必须增加的功能 |
| `Week4/4_1_Chapter 13_Security - Copy.pptx`，第16–19张 | 认证、授权、输入验证、限制失败尝试、避免默认凭据 |
| `Week5/5_2_Revision - 1 (2).pptx`第32张、`5_3_Revision - 2 - Copy (1).pptx`第17张 | 报告约3000词（不含附录），每人提交；现场系统演示无需额外PPT，每人提交源码和视频 |

课件中的85%路径覆盖率、30分钟测试、3小时修改界面是质量场景示例，不是评分表规定的统一门槛。课件截图出现的不同.NET版本和未完成代码步骤也没有被当成最终实现要求。

## 对照评分要求

| 评分内容 | 代码证据与处理结果 |
| --- | --- |
| 商品完整CRUD | 管理员ProductsController、ProductService、产品表单与详情；保持角色授权、服务端验证及商品关系处理 |
| 浏览、筛选、分页 | 前台ProductsController、ProductService.GetPagedAsync、ProductQuery、商品目录；保留分类、关键词、价格、评分与分页 |
| 购物车加入、查看、移除 | CartController、CartService、ICartStore/SessionCartStore；保留数量更新、库存复核、清空与SKU |
| 一致界面 | 保留现有首页、目录、后台、购物车及订单设计；错误页面改用不依赖数据库的布局 |
| MVC与n-tier | Web、Business、DataAccess、Models四个应用项目，加独立Tests项目；业务类库不引用Web，控制器/组件不直接查询DbContext |
| 五项质量属性 | 下述缺陷修复、接口与项目边界、独立测试库、授权与防伪、错误反馈；README说明实际机制及限制 |
| 答辩、架构报告、个人反思/贡献 | 无法通过源码替代本人证明。需解释所用模式/策略及权衡，按真实经历完成个人内容 |

当前报告与视频不在代码验证结果中。此前Word架构报告由上游删除，本地备份保留；旧图如果仍用于提交，必须更新为本次多项目结构，不能继续描述旧单项目结构。本次资料文件夹没有AI声明表原件，因此没有声称已检查或填写该表。

## 发现并修复的问题

1. **业务规则没有独立编译边界。** 将原模型、表单/DTO、验证及资源移入`OnlineStore.Models`，数据上下文/初始化/迁移移入`OnlineStore.DataAccess`，服务移入`OnlineStore.Business`。Web保留页面、控制器与静态资源。原命名空间与迁移编号保持，显式指定迁移程序集并验证中英文资源加载；Docker构建也复制新增项目引用。这里是同一部署进程内的分层，不冒称为多个独立部署服务。
2. **两处展示层直接查询EF。** 分类导航查询移到`ICategoryService.GetNavigationAsync`，优惠券选项查询移到`ICouponService.GetFormOptionsAsync`。排序、数量上限、过滤、空分类、编辑选中状态和取消令牌保留。
3. **验证码可以无限猜测。** 新增持久化尝试次数及并发校验，最多5次；正确码一次性消费。保留10分钟有效期和60秒重发间隔，新增增量迁移，未清空用户或订单。
4. **签收状态竞争可能抛500。** 确认收货在事务内重新核对订单状态、归属及活动售后，并处理并发冲突；不会覆盖另一请求设置的签收异常，也不会在活动售后期间强行完成订单。
5. **合法多商品物流文字可能写不进SQL。** 聚合后的6个中英文快照按订单字段200/100/100字符上限保留前缀和省略号，截断不拆分Unicode代理对；实际SQL持久化已列入验证。
6. **生产环境创建公开默认账号，启动时重置演示买家密码。** 默认管理员和虚构买家/订单仅在Development创建；生产首次管理员需完整Bootstrap配置。已有账号不重置密码、不自动提权；新开发库演示买家使用保留域名，已有账号和订单不删除。
7. **数据库故障时错误页面也失败。** 独立`_ErrorLayout.cshtml`不访问分类、用户、订单或优惠券查询，不使用远程样式/脚本；回归请求验证500响应有可读HTML且不泄露内部异常。
8. **README存在过时说明。** 对齐当前启动项目、部署工作流、账号初始化、迁移命令、测试类型和质量属性限制，删除旧测试数量和“数据库故障不影响网站”等无证据保证。
9. **依赖包存在已知安全通告。** EF Core及对应Identity/HealthChecks/InMemory包统一从8.0.8更新至8.0.11，并对Pkcs使用支持net8的10.0.13版本约束，修复NuGet报告的六种依赖包、八项通告涉及的版本路径。保留.NET 8及EF Core 8主版本，未关闭NuGetAudit或忽略通告。五项目连同传递依赖再次检查，未报告已知漏洞；这不等于证明所有依赖未来或所有部署环境都安全。

依赖版本选择依据：[EF.SqlServer 8.0.11](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/8.0.11)、[SqlClient 5.1.6](https://www.nuget.org/packages/Microsoft.Data.SqlClient/5.1.6)、[Pkcs 10.0.13](https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/10.0.13)。完整依赖路径、适用边界与通告链接保存于审查目录的`dependency-remediation-review.md`。

## 删除依据

删除前搜索了应用、Razor视图和测试的调用，保留框架约定发现的类、数据库迁移、测试、可配置资源和运行上传文件。

| 已删除旧接口/依赖 | 依据与替代 |
| --- | --- |
| IReviewService/ReviewService的GetByProductIdAsync、GetAllAsync、CountAsync | 只有声明/实现；实际页面使用分页和摘要 |
| IProductService/ProductService的CountAsync、CountActiveAsync | 旧计数无消费者；保留仪表盘调用的CountLowStockAsync |
| IShopService/ShopService的CountAsync | 无应用或测试调用 |
| ICategoryService/CategoryService的CountAsync | 无调用，分类列表与导航使用当前查询接口 |
| IOrderService/OrderService的GetAllAsync、GetAfterSalesAllAsync | 已由分页列表替代；唯一旧测试改为分页等价验证 |
| 前台CouponsController的未使用本地化字段/构造依赖 | 该控制器不使用本地化器，删除冗余注入 |

共删除9个旧服务方法及相应接口声明。所有自有CSS/JS均存在引用，没有按文件名猜测删除整文件。`everyday-motion.png`虽无静态引用，但图片路径可保存在数据库中，未证实无运行时用途，故保留。类库拆分中的源文件移动不是删除业务功能。

## 验证记录

以下检查在全部源码及依赖修改完成后执行，日期为2026-10-29；证据根目录为`E:/DMU-Learning/.tmp/coursework-audit-20261029/`。

| 检查 | 实际结果 | 证据 |
| --- | --- | --- |
| 五项目构建 | 成功，0警告、0错误 | `dependencies-build.log`；最终Release测试也重新构建成功 |
| .NET完整回归 | 243/243通过，0失败、0跳过 | `test-results/final-suite.trx` |
| Node前端回归 | 29/29通过 | `OnlineStore.Tests/Frontend/*.test.cjs`实际执行结果，汇总于`final-verification.json` |
| 真实SQL Server | 8组全部通过 | `sql-checks/sql-verification.json` |
| NuGet安全复扫 | 五项目及传递依赖未报告已知漏洞 | `dependencies-before.json`、`dependencies-after.json` |
| Release发布与启动 | publish成功；发布目录启动后`/health`返回200 | `publish/`、`qa-server.json`、启动日志 |
| 实际浏览器 | 中英文首页、分类筛选及第二页、购物车加入/移除、管理员登录与后台商品/订单/轮播/优惠券页面、优惠券分类选项、买家订单/物流弹窗、商品详情/评价、买家访问后台被拒绝、独立404页面均核对 | `browser/verification.json`及6张截图 |
| 手机宽度 | 390px下买家订单与独立错误页无页面横向溢出 | `browser/buyer-orders-mobile.png`、`browser/error-mobile.png` |
| 页面及服务日志 | 本轮浏览器未捕获warning/error，发布预览stderr为空且未记录fail/Unhandled exception | `browser/verification.json`、`qa.stdout.log`、`qa.stderr.log` |
| 文件移动完整性 | 129份文件移动前后SHA256一致；功能修复另有差异记录 | `layer-move-manifest.json` |
| 原数据库与本机配置 | 日常OnlineStore库各表行数及数据摘要前后完全一致；Development配置与备份SHA256一致 | `sql-checks/daily-database-preservation.json`、`final-verification.json` |

SQL验证使用独立数据库`OnlineStore_CourseAudit_20261029_34dcbedf`，包括18份迁移、开发种子、12并发错误验证码限制为5次、8并发正确验证码仅消费1次、商品CRUD/筛选/分页、取消与退款仅恢复库存一次、最后1件库存并发下单不超卖、多商品中英文物流快照写入SQL。SQL写入和浏览器登录均未使用日常数据库。

新验证码迁移已在隔离库应用验证，尚未对日常数据库执行；正常启动Web项目时沿用现有`MigrateAsync()`应用未执行的迁移。临时预览使用5095端口，检查结束后停止；没有占用Visual Studio的5080/5081端口。源码仍位于本地Ethan-Yang工作区，本轮没有提交或推送GitHub。

## 验证边界

代码及回归测试无法证明所有输入、所有并发时序、所有浏览器都绝对没有错误。未执行真实付款扣款、邮件送达、生产Azure部署、生产账号撤销、压力测试或学生答辩测评。当前支付及物流仍为课程演示流程，邮件服务需外部配置；商品图片和常规页面的CDN资源仍依赖网络。生产旧默认凭据需部署管理员检查，更新本地源码不会自动撤销远程账号。

审查前完整源码备份在`E:/DMU-Learning/.tmp/onlinestore-coursework-audit-20261029-143323/`；提取资料、图像阅读记录、回归测试和隔离数据库验证在`E:/DMU-Learning/.tmp/coursework-audit-20261029/`。原始courseware文件未改动，本机未提交的SQL连接配置保留。
