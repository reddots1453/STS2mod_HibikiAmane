# DS27 批次33：首次资源增加抽牌与贞洁

日期2026-09-28，前置快照`94e085ca`，ACT4-001→DS27-05M。
本批部分实现待手测；动态加价仍READY，X/Y费用Q17 OPEN。未部署，总目标未完成。

## 审阅与实现

DesignDoc逐行/词级无漂移，复核色欲/贞洁、阶段替换、挥剑压制和创作章节的“所有免费包括次级资源”说明。

- 色欲原数量1/2/2与“本回合免费”为旧版本，改为残缺2、完整3、觉醒3。仅实际SecondaryResourceChangeReason.Gain、正Delta、本人且同一进行中战斗触发；Set、Reset、负变化、无变化、其他玩家、战斗结束不触发。保存UsedThisCombat，在异步抽牌前占用，战斗首尾重置。
- 原来逐次抽1并比较手牌差集，会混淆抽牌连锁改变的手牌；现在一次原生Draw请求2/3并只对返回的实际抽牌实例处理。尊重禁抽、满手和空牌堆，第一次实际资源增加即消耗触发机会，不延后等到能够抽牌。
- 新增GeneratedCardCostCmd.SetFreeUntilPlayed，原生能量/星星使用UntilPlayed，次级资源对应持续层也只到打出。保留原SetFreeThisTurn调用不动。仅处理基础固定费用；没有擅自设定X/Y行为。
- 贞洁原代码已有1/2/2层及觉醒≤2回合奖励，保留。修正显示名称“禁欲”→“贞洁”，普通描述说明每次扣1层，智能描述显示实际剩余次数；不再错误说每次移除整个Power。耗尽层数不再阻止增加，回合奖励检查本人及进行中战斗。遗物补相应Power悬停，Stage4替换旧空占位文本。

## 未完成的费用边界

1. Q17已询问：免费X/Y是沿用原版支付、免费但保留当前资源作为效果值，还是免费且效果值0。当前只保留既有原版X类费用，不宣称实现完成。
2. 当前DLL的CardEnergyCost.GetWithModifiers先Local后Global。凝神一斩等TryModifyEnergyCostInCombat动态加价仍可能把本地0改为正数。这不是已完成项，下一批需实现免费标记的后置优先级，连同复制/出牌/战斗结束/保存生命周期。当前测试只覆盖基础固定费用，不能证明所有动态牌免费。
3. 原贞洁ShouldGain同步钩子仍用既有PowerCmd异步扣层/移除。正常顺序Gain命令已加入测试，但异步其他Mod的BeforePowerAmountChanged延迟与并发Gain未实测，不据脚本编译宣称该跨Mod调度已验证。

## 已执行验证

| 命令 | 结果 |
|---|---|
| Debug / Release，`-p:DeployMod=false --no-restore` | 零警告零错误，最后保留Debug |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 264通过，新增8项文本、接线和测试覆盖检查 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，静态累计283 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 4073既有生产规则断言通过，本批没有新增纯规则测试 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33既有编码断言通过，不代表本批整局存读档通过 |
| ValidateMvpContent / StructuralContracts / LocalizationStyle / CardEffectTests | 四门通过 |
| ValidateVisualAssets | 既有317口红独立数值标签失败，未修改门禁 |
| `AuditCardLocalization.py --no-write` | 225项；既有GagCurse费用1/2差异、4元数据未解、215文本待审、2设计独有、2退役兼容；未写共享报告 |

## 游戏内测试（已编译，未运行）

`ms_test_resource_relics confirm`，日志`[DS27ResourceRelicTest]`；仅Debug、单人响木天音、进行中且有存活敌人的战斗。

**破坏性，仅可丢弃测试局使用：清空遗物、战斗牌堆与状态，改变生命、能量和资源，不恢复游戏数据；正常结束或异常恢复TestMode及运行锁。**

真实RelicCmd/PowerCmd/资源Gain和Set/打牌命令，覆盖：

- 五阶段首次实际资源增加，2/3/3抽牌；正Set、Reset、负变化、相同值、别人不触发；第二次增加不重复；原生SavedProperties恢复已使用收据不重发。
- 选用原生实际抽到的MaidenStrike实例并附固定2点次级费用、2点临时星星，比较觉醒前后能量/次级/星星价格，保持升级；调用CardModel.EndOfTurnCleanup及弃牌/重新入手仍保持UntilPlayed，未抽到的牌不被修改。
- 觉醒真实首次打牌不扣能量和次级资源，打出后临时费用层清理，第二次打出重新支付1能量和2次级资源。星星测试使用临时固定费用，打出后随原生清理回到0。
- 满手、NoDraw、空牌堆尊重原生抽牌规则，不将首次触发留到后续。
- 贞洁五阶段授予0/1/2/2/2层；0增加不扣层，每层完整阻止一次+4，只减1层；被阻止时色欲不抽牌，耗尽后下一次实际增加才抽2。
- 觉醒0/1/2/3/7资源阈值、本人与其他玩家回合能量对照。

以上是可运行模型测试，不是已执行游戏验证。自然回合/跨战斗、完整保存退出恢复、实际智能悬停、多人和费用动态边界仍需完成。全部其他待办与Q12～Q17继续保留。
