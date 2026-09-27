# DS27 批次26：暴食与谦逊规则边界审阅

日期：2026-09-28；前置快照：`14b7e373`；ACT4-001 → DS27-05E（IMPLEMENTED，游戏内待验）及05F（OPEN）。未部署，总目标未完成。

## 审阅与实现

DesignDoc逐行/词级diff无漂移。完整重读谦逊、暴食条目，查询原版知识库与当前DLL。

- 原版AlchemicalCoffer使用`CreateRandomPotionsOutOfCombat`和`CombatPotionGeneration`，逐个通过`PotionCmd.TryToProcure`获取；本批沿用这一原生流程，不以直接写入库存绕过其他遗物规则。
- 原版ReptileTrinket通过`potion.Owner == Owner`确定用药归属，而不是看药水目标。本批暴食按该归属过滤；设计没有限定战斗中，因此不额外添加原版蜥蜴饰物的战斗限制。
- `GluttonyRules`集中阶段/数值和空栏位边界。Stage1/2每次拾起各获得4最大生命、1栏；Stage3旧档和Stage4觉醒拾起补满当前空栏，之后本人用药每次获得4最大生命。沉睡无效果。
- `GluttonyRouteRelic`移除全部旧5点实现，使用原生GainMaxHp/GainMaxPotionCount；不在获得/丢弃药水时增长生命，不覆盖已有药水。满栏不调用药水工厂、不消费生成RNG。
- 保存`PickupEffectGranted`收据，在原生副作用前写入，重复拾起通知不再奖励。每段试炼领取创建新遗物实例，因此残缺与完整各自正常发放一次，不让第一阶段收据阻止第二阶段奖励。
- 中文描述数值、句子顺序和标点按DesignDoc同步，Stage4不再是“延续繁荣”；Stage3兼容觉醒文案。

没有编辑用户存档或扣回旧实现历史已给的5点最大生命；正常读档不重放拾起命令。收据用于防重复，不宣称对原生命令中断提供完整事务回滚。

## 验证

在Mod目录执行：

| 命令 | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告、0错误 |
| 同命令Release，随后重建Debug | 均0警告、0错误 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 新增137生产断言，累计2852通过 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 208项通过，新增8 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，静态合计227 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33既有编码断言通过，不是本轮游戏保存测试 |
| MVP/Structural/LocalizationStyle/CardEffect四个PS1各`-ProjectDir .` | 全部通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有317行诱惑度独立数字栏失败，未放宽 |
| `python scripts/AuditCardLocalization.py --no-write` | 225注册；1费用差异、4未知元数据、215待文本、2设计独有、2退役兼容；共享报告未覆写 |

137条直接执行生产策略：各阶段字面期望、其他Owner不得触发、0～8栏与0～10已占用数、满栏/超占用、非法负值/极值不溢出。静态验证原生命令、收据顺序、归属/目标区分、RNG门及精确描述文本。

## 游戏内测试命令（未执行）

`ms_test_gluttony confirm`仅Debug、单人本角色、非战斗，拒绝重入。**会删除遗物/药水并重置最大生命与栏位，仅用于可丢弃测试局，不能在正式存档中执行；不恢复这些测试数据。** 正常和失败路径均恢复TestMode。

已编译用例：5阶段×空/部分/满药水栏；真实RelicCmd.Obtain后生命/栏位/药水数量；保留原药水对象、满栏RNG不变；重复通知/保存字段恢复不重发；其他玩家药水即使目标为本角色也不触发；丢弃和获取不是使用；原生FruitJuice.OnUseWrapper保留药水本身效果并且觉醒只追加4点；两段独立拾取累计8最大生命和2栏位。

实际拾取动画、生命UI、战斗内用药、完整保存/恢复、异常中断及多人同步仍待游戏内验收。模型测试中外角色隔离直接调用钩子，不等于完整联网多人测试。

## Q14 谦逊待确认

发现当前谦逊仅修改DynamicVars，没有删除其他卡牌效果；觉醒按句号数量猜测纯描述并只抽1。没有找到可直接套用的原版“删除所有其他描述”机制。用户已要求不简化，因此不能以固定伤害快照或粗糙句子过滤代替正式语义。

2026-09-28已提出：

1. 改写多段、X费、条件伤害/格挡时，保留目标/次数/X/条件计算、只翻倍数值，还是展平为无条件固定数值？尤其“消耗全部诅咒后按其数量计算伤害”的牌，删除消耗后怎样计算？
2. 原有消耗/虚无/保留关键词，以及抽到/回合结束触发是否一并移除？附魔已明确保留。

未收到确认前不固化上述规则，不修改谦逊代码。之前Q12连锁破坏、Q13子守歌叠层仍按各自记录等待，不重复改变其设计。暴食及其他不依赖这些答案的工作继续推进；其余路线、事件、怪物、UI和全面游戏内验收仍是总目标组成部分。
