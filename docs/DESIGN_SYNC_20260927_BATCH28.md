# DS27 批次28：懒惰支付与回合奖励

日期：2026-09-28；前置快照：`1fe44576`；ACT4-001 → DS27-05H。
IMPLEMENTED，游戏内待验；未部署，总目标未完成。

## 证据与实现

DesignDoc逐行/词级无漂移，完整审阅懒惰条目及第四层阶段替换规则。当前游戏DLL中CardModel.SpendResources仅在实际支付时调用AfterEnergySpent；免费AutoPlay及一次付款后的重放不应再次计费。当前BeforeSideTurnStart签名为IReadOnlyList/ICombatState，已按DLL修正并通过构建。

- 删除AfterCardPlayed累计EnergyValue，改用AfterEnergySpent，按卡牌Owner累计正整数支付。原生减费后的实际支付、X支付正常计入；免费代打、重放、星星等其他资源不额外计入。只加不扣，获得能量不会抵消已使用的能量。
- 本人回合结束判定≤2，阶段1/2/3或4的下回合能量为1/2/3，觉醒当时获得12格挡。回合末收据在异步格挡前写入，待发能量在异步发奖前消费，重复通知不能重复奖励。
- 开始/结束回合检查参与者；其他玩家的额外回合不清空本人计数、其他玩家能量重置不清掉本人的待发奖励。移除全局RoundNumber>1条件，第一轮中的额外本人回合也可领取上一回合奖励。
- BeforeCombatStart/AfterCombatEnd清空本场状态，异常离开后即使未触发战斗结束，新战斗开始仍防止旧奖励泄漏。状态用值类型，克隆不共享可变账本。
- 现有decimal SavedProperty在当前原生SavedProperties.FromInternal会抛出不支持类型异常；能量原本就是整数，改int保存，同名字段保留，追加bool末回合收据。没有虚构旧decimal存档转换；旧缺失字段采用默认值，完整真实存档恢复仍需验收。
- Stage4占位“延续繁荣”替换为正式觉醒效果，兼容Stage3旧觉醒。精确文本测试只归一化能量图标和富文本标签，不忽略标点；格挡加颜色及觉醒阶段原生悬停，沉睡不提前展示。

## 验证

| 命令/检查 | 结果 |
|---|---|
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 3421断言通过，新增440条实际生产状态机测试 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 224通过，新增8；既有沉睡静态门改为检查提取后的实际状态机守卫 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过；合计静态243 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33既有生产编码断言通过，不是懒惰引擎测试 |
| Debug / Release，`-p:DeployMod=false --no-restore` | 零警告、零错误 |
| ValidateMvpContent / StructuralContracts / LocalizationStyle / CardEffectTests | 四门通过 |
| `AuditCardLocalization.py --no-write` | 225项；既有GagCurse费用1/2差异、4元数据未解、215待文本审查、2设计独有、2退役兼容，未覆盖共享报告 |
| ValidateVisualAssets | 既有317行诱惑度口红独立数字标签门失败，未放宽门禁 |

生产测试覆盖5阶段×0/1/2/3/20支付、阈值、支付累加、其他玩家、下一回合、回合末与发奖重入、战斗重置、值克隆、整数溢出和无效阶段。

## 游戏内测试入口（未执行）

`ms_test_sloth confirm`：仅Debug、单人响木天音、有存活敌人的进行中战斗。**破坏性测试，仅可丢弃测试局；清除遗物、牌堆、状态，改变生命/能量，不恢复测试局。**结束或异常恢复TestMode。

测试使用实际SpendResources/OnPlayWrapper、CardCmd免费代打、华彩重放、减费、X牌、RelicCmd.Obtain和SavedProperties。5阶段×0/1/2/3支付检查结算，并验证其他玩家/敌方回合、同一全局轮数下额外回合、重复通知、保存收据、战斗边界。回合边界及外部玩家为直接钩子模拟，不是实际多人或完整TurnManager自动推进。日志`[DS27SlothTest] PASS/FAIL`。

仍需手测：完整自然回合及额外回合、两名真实玩家不同回合序列、打牌预览/费用特效、觉醒格挡/悬停、存档退出和重启后的状态、跨房间与战斗异常中止。

## 剩余

总目标仍未完成。其余路线、慷慨互斥供奉和正式流程UI、卡牌/怪物/事件/视觉待项继续保留；Q12/Q13/Q14未答部分不固化规则。
