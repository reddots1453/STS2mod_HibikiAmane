# DS27第65批：继承关键词审计

2026-09-28；前置`ad418cb2`，计划`f555136c`。DesignDoc逐行/词级无漂移；只改审计及自测，不改游戏代码、资源、本地化、数值或意图。IMPLEMENTED，离线已执行；游戏验收仍未完成，无部署。

## 证据与修改

原4项未知来自同一结构：子类base传关键词集合，ClothingHazardStatus构造函数复制到私有readonly字段，CanonicalKeywords返回字段。不是卡牌漏加关键词。

| 模型 | 独立设计及源码预期 |
|---|---|
| BarbedHookStatus／倒刺钩 | 消耗 |
| ClothingBurnStatus／衣物燃烧 | 虚无、消耗 |
| BitingPaperStatus／咬衣纸片 | 消耗 |
| DissolvingFluidStatus／溶解液 | 消耗、保留 |

审计新增顶层参数分隔，避免集合内逗号被误认为构造参数；传递构造参数绑定，识别最接近子类的关键词声明。仅支持完整已知字面量，或来源确定的私有readonly集合ToArray复制；字段额外引用/修改、带副作用的转发构造、重载歧义、条件/调用/扩展元素保持未知。不再把动态集合中能识别的一部分关键词当作完整结果。

全限定的原版CardKeyword和本Mod Portable/Sinking名称显式识别，不接受任意命名空间后缀；实际万劫不复的全限定沉底声明不被新解析误报。字符串/注释不能伪造关键词。此工具是有界源码读取器，不宣称等价C#编译器或证明任意运行时行为。

新增10项自测：集合单项/多项/空值、嵌套逗号与括号不匹配、动态/字符串/扩展拒绝、直接getter动态拒绝、全限定名、条件/可变/字段逃逸、构造副作用/重载、子类覆盖、多层转发及注释、真实四牌独立设计/源码/元数据。原strict-review拒绝未完成文本审查和no-write保护也继续通过。

## 验证

- `python scripts/ValidateDesignSync20260927.py`：12/12离线通过，源文件无漂移。日期静态505、审计自身29，共534；13760生产纯规则、33编码断言；Release/Debug零警告零错误（DeployMod=false），MVP/结构/本地化/卡牌登记/视觉门均通过。
- `python scripts/AuditCardLocalization.py --no-write`：225模型、failures=0、unresolvedMetadata=0、pendingText=215、designOnly=2、retiredCompat=2。不覆盖并行.review报告，不删严格模式或降低覆盖要求。
- 报告`obj/design-sync-validation/20260928T065343Z-0a2f9169c8b7/report.json`，测试HEAD`f555136c`加当时未提交的本批脚本；前后SHA256均`fcb5406f95059c9b97f026713422ae8ab36be5d3e087fa255958f4bfb26f919f`。后补交接文档不在当次指纹内。
- JSON为offlineStatus=passed、exitCode=2、goalCompleted=false，游戏六组全部not_run；终端宿主把非零退出呈现为1，不据此误判12套件失败，也不据离线通过冒称全目标完成。
- 没有游戏操作、部署或共享审计写入。其他Agent的工作区/暂存修改保留。

未解析字段归零只说明当前源码元数据可解析，不替代215项文本逐字/排版与效果覆盖、仅设计条目处理、怪物/路线/事件剩余实现和完整引擎验收。当前剩余范围沿用[全范围状态](DESIGN_SYNC_20260927_CURRENT_STATUS.md)。
