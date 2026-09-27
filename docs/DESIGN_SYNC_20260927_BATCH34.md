# DS27批次34：免费直到打出的最终费用优先级

2026-09-28；前置快照`0fa4902f`；ACT4-001→DS27-05M2→DS27-ACT4/COMPAT。
代码IMPLEMENTED，游戏内未验；Q17 X/Y仍OPEN，未部署，总目标未完成。

## 证据与范围

DesignDoc逐行/词级无漂移，复核觉醒抽牌、阶段替换和“免费包括次级资源”条目。
当前游戏DLL的CardEnergyCost先Local后Global；RitsuLib 0.4.64的ICardEnergyCostContributor同样在Global之前，不能用另一个本地减费解决全局加价覆盖0的问题。

- GeneratedCardCostCmd.SetFreeUntilPlayed附加FreeUntilPlayedCapability（去重、不清除其他能力），保留原生固定能量/星星/次级资源UntilPlayed层。既有SetFreeThisTurn调用不动。
- 注册能力是具体牌实例的资格，由框架能力克隆及存档文档处理，非全局卡牌ID字典。卡牌复制各自持有资格，消耗一个不影响另一个；原生序列化条目支持恢复资格。没有改变游戏的可保存时机或宣称新增战斗中存档功能。
- 固定能量GetWithModifiers后置、固定星星GetStarCostWithModifiers后置应用0；能量查询须包含Local，不影响None/Global查询，不改变负数不可打出或X费用。
- 次级资源在ResolveLine完成后、支付计划扣减可用资源之前，仅必需固定费用行归零，清除不足/替代生命支付的待提交结果。修改ModifyCost本身会连同OptionalSpend/ExtraSpend被改，故没有采用。X/Y及可选额外支付保持原行为。
- 原生AfterCardPlayedCleanup移除能力并通知费用变化，避免第一次重放的AfterCardPlayed就提前失效；普通回合末/弃牌重抽保留。BeforeCombatStart/AfterCombatEnd移除旧资格，永久牌组不应用最终战斗优惠。原生费用层依原生生命周期，不清除其他来源修正。

## 测试修正与新增入口

`ms_test_resource_relics confirm`，仅Debug、可丢弃单人响木天音战斗，清空遗物/牌堆/状态并改资源生命，**不恢复游戏数据**。

旧脚本通过ctx.Play调用CardCmd.AutoPlay；该入口原生不支付能量，不能用来断言第二次打牌支付。改为SpendResources后调用OnPlayWrapper(isAutoPlay:false)，没有改共用ctx.Play或其他批次脚本。

新增DesignSyncFreeUntilPlayedContract：

- 真实觉醒抽到升级凝神一斩，欲望1/4/6时始终0；未标记同名牌仍按动态费用收费，Global-only查询保持原值。
- 原生回合末、弃牌重入手、CloneCard、ToSerializable/FromSerializable及加入战斗scope；复制/恢复实例清理不消耗原实例，已消费资格不因保存复活。
- 临时安装仅针对测试卡的全局星星+4与次级费用+3提供者，finally按自己的Harmony ID卸载；实际支付和出牌检验最终0、不会扣生命或消费生命替代Power。再次正常出牌支付原次级费用和动态能量。
- X/Y、负数费用不变；可选/额外支付的记录探针验证不被修改。可选支付探针是直接调用补丁，不冒称真实可选卡牌整链路验证。
- 战斗首尾清理直接调用能力回调，属于回调探针；自然战斗首尾、多人复制/传递、完整保存退出恢复和跨Mod补丁顺序仍需实机。

所有游戏内脚本**已编译，未运行**。框架代码审阅和静态字符串门不等于上述行为已实测通过。

## 已执行验证

| 检查 | 结果 |
|---|---|
| Debug / Release，DeployMod=false，no-restore | 零警告零错误，最后保留Debug |
| unittest discover Test*20260927.py | 272通过（新增8） |
| TestCardLocalizationAudit.py | 19通过；静态累计291 |
| tests/DesignSyncContracts | 4073既有生产规则断言通过；本批无新增离线游戏规则断言 |
| tests/LayeredSaveContracts | 33既有编码断言通过，不代表新能力整局存读档通过 |
| ValidateMvpContent / StructuralContracts / LocalizationStyle / CardEffectTests | 四门通过 |
| ValidateVisualAssets | 既有317诱惑度独立数值标签失败，未改门禁 |
| AuditCardLocalization.py --no-write | 全卡旧差异继续保留，不写共享审计报告 |

并行CHANGELOG剩余63增17删、共享审计702增507删保持不纳入本提交。未部署DLL、未运行游戏测试。
