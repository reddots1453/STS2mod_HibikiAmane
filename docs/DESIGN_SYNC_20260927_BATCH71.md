# DS27第71批：新书库与节制奖励迁移

2026-09-28；变更前快照/技术计划`5253596e`，设计输入`fd3a6fa5`，分支`codex/maiden-controlled-merge-v2`。用户Q21确认“留在手牌时持续生效”；Q22不再适用。本批无DesignDoc漂移，未部署、未操作游戏，不改素材或其他路线规则。

## 实现入口

- `Iteration1CorruptCards.InsatiableGreed`保持尘封魔典和旧存档身份，改2/1费先古技能、虚无；本身打出不安装旧Power。文本/关键词/重放悬停同步。
- `LibraryHandAura`查询本人当前Hand实例顺序及直接邻牌，无环绕、不影响其他角色。`LibraryNeighbourRules`提供可离线执行的邻接判定。Global关键词贡献复制原集合，Local查询/克隆/存档不写入临时消耗；原生重放计数额外加1，不替换已有附魔。
- `LibraryHandAuraPatches`在OnPlayWrapper入口捕获离手前贡献，任务完成/失败/取消均finally清理，嵌套打出独立作用域。当前牌在Play才读快照，其他位置读取实时状态。
- `LibraryAuraOverlay`挂在原生CardContainer，以300×422中心坐标绘制红/绿发光轮廓；透明、忽略输入，不覆盖原生高亮；邻接变化才刷新文本。离树保留可复用资源，不在卡节点池重新入树时引用已释放StyleBox。
- 新`TemperanceSignet`（节制之戒）1费先古技能，本回合原生NoDrawPower，选三牌堆随机打出3/4张并消耗。新`TemperanceCirclet`（节制之环）2费先古能力，旧书库持续禁抽/每回合随机10张，升级保留。
- `TemperancePileCmd`共享选择和出牌流程：按原CombatCardGeneration稳定洗牌、一次快照取N张；选择返回后核对战斗/存活/原选项，逐张核对拥有者/牌堆，不重复采样回收牌。`LibraryPileChoice`描述显示调用者3/4/10数量，不写死10。
- `YarusLibraryPower`保留序列化ID，展示改节制之环；旧存档不会因类型被删除而失效。两张新牌仅在MSGeneratedCardPool注册、禁止普通生成，不替代尘封魔典的书库卡。
- `TemperanceRouteRelic`删除旧开战选牌加消耗；残缺/完整各永久加入一张戒，觉醒加入环（Stage4及旧兼容3）。SavedProperty防重复，插入提交前失败可重试、提交后视觉失败不重发；读档本身不补发旧奖励。试炼改2/2/3。

## 测试与验证

- 旧DesignSyncLibraryContract的三堆、0/1/9/10/12、真实伤害、随机身份/顺序/流、禁止抽牌、状态移除、选择异常等场景迁到环；保留每个基础/升级场景最低200效果断言。
- 新DesignSyncLibraryAuraContract：基础/升级真实Run/Hand全文、DustyTome、左右边界/重排/离手/返回/两书库、Local/Global分离、克隆/存档不泄漏、Glam叠加、原生消耗不被撤销、实际3次格挡并消耗、嵌套/失败/取消清理。最低20效果断言，尚未引擎执行。
- 新DesignSyncTemperanceSignetContract：三堆0/1/3/4/6张，独立Fisher–Yates期望、真实格挡/3或4张身份、无重复、随机流隔离、实际消耗、禁抽/回合结束恢复。最低150效果断言，尚未引擎执行。
- `ms_test_combat_virtues confirm`节制场景改永久牌组奖励/全阶段身份、重复拾取/开战不发放、真实序列化往返；耐心场景保持。该入口会改变一次性测试局，不自动运行。
- 调试Catalog/Runner、注册/内容/审计计数225→227；源审计及登记不通过跳过新卡来放行。新增8静态、18生产邻接断言。
- 最终执行`python scripts/ValidateDesignSync20260927.py`：548日期静态+29审计自测=577通过；13778生产纯规则、33保存编码通过；Debug/Release均`DeployMod=false`、零警告零错误；MVP/结构/本地化/卡牌登记/视觉五门通过。
- 最终报告`obj/design-sync-validation/20260928T085545Z-f8e70d676f37/report.json`：12/12，前后源码SHA256均`9d050a4012390210f0232fe713e149ff09d8b78c6122f694745ade60e5093ca4`，HEAD5253596e加本批实现。此前首轮报告`20260928T085337Z-663d3753b310`亦通过，随后将关键词接入完善为Global来源查询，再执行最终全门。本记录/Plan/追踪状态在最终报告后写入，不属于该次指纹。
- 全卡227、明确失败0、未解析0、通用pendingText217、designOnly2、retiredCompat2。独立文本清单`obj/card-text-evidence-b71.json`完整性错误0：217现行中133双实例全文声明、84未识别；不是133张实机通过。该清单在最终关键词接入前生成，只证明当时的提供者接线，最新注册计数由最终统一门再次核验。

## 未执行/剩余边界

游戏内卡牌、怪物、第四层、事件、多人、视觉六组均`not_run`。待一次性测试局验证红绿框缩放/重排、真实手动出牌与节点复用、三堆选择、自然回合禁抽恢复、阶段领奖和保存退出恢复；编译通过不能证明Harmony运行及布局正确。原生附魔Glam首次重放使用后失效，测试按此原生规则预期，不错误保留其次数。

本批收口第70批因DesignDoc漂移未完成的离线复验，但不删除其失败记录。全范围目标及Q12～Q20、其余卡牌全文/事件/路线/怪物缺口保持；不将这一批实现标成全目标完成。统一逻辑退出2表示离线通过但游戏未验/全目标未完成。独立提交仅包含本批文件和CHANGELOG首段，保留并行素材、角色文件及共享审计的改动。
