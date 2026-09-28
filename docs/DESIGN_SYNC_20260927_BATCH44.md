# DS27 批次44：破碎及随机攻击

前置快照`eb51da19`，Plan同步`09fa1739`，完成提交为本文件所在独立范围提交。IMPLEMENTED，游戏内待验，未部署。DesignDoc逐行/词级无新增漂移。

## 核对与修正

完整审阅破碎正式定义及气旋破裂、闪电踢击、爆发式冲击、渎神黄昏条目，并核对原生ValueProp、IsPoweredAttack、VulnerablePower、CreatureCmd.Damage。

1. 破碎明确仅增加攻击伤害，且敌我一致。现有ModifyDamageAdditive只看目标和对立伤害来源，遗漏类型检查，导致非攻击伤害也增加。增加`!props.IsPoweredAttack()`排除；保持原有对立来源条件和自身侧回合末减1，不改变数值。
2. 气旋破裂代码基础1/升级2层正确，描述却硬编码1。替换为`{ShatterPower:diff()}`，升级卡面与实际效果一致。
3. 爆发式冲击“对受到伤害的敌人给予”改为正式“给予受到伤害的敌人”，保持标点和分行。
4. 爆发式冲击和渎神黄昏原探针分别用DamageTargetPower/Damage，断言全部随机伤害都在PrimaryEnemy；多敌时必然存在误报。替换为所有敌人生命差总和、逐敌命中数和实际施加状态验证，不改变生产RNG或强制所有伤害落单敌。

## 真实命令脚本

`ms_test_cards confirm ds27-shatter-random`限定四牌的基础/升级变体，沿用现有角色、单人和confirm门禁。**只可在可丢弃测试战斗运行**，会清理卡牌/状态/遗物。本轮仅编译，未执行。

- 四牌精确费用/稀有度/类型/目标；基础/升级永久预览和战斗预览全文，尤其气旋1/2和冲击正式语序。
- 气旋破裂与闪电踢击保留实际出牌伤害及层数断言；每变体至少26条效果断言。
- 同一破碎用例分别作用于玩家和敌人：原生Damage命令覆盖Unpowered、Unpowered|Move、Unblockable|Unpowered均不加成；Move及Move|Unblockable按层加成；同侧来源、无来源不加成。
- 玩家实际承受怪物两段攻击，每击加成；敌人实际承受打击获得加成。对方侧回合末不减、自身侧减1、剩余1层的伤害、归零移除均检查。回合末调用真实Power钩子，但不是完整自然回合调度验收。
- 两随机牌每变体至少14条效果断言：分别在0/2力量下创建额外敌人，确保至少两敌，真正打牌；验证所有敌人的伤害总和、每敌损血对应整数次命中及合计命中次数；冲击只对受击者施加一次2层破碎，不按命中次数重复叠加。敌人生命充足且无格挡/减伤，便于精确反推命中，不假装覆盖未在该场景测试的格挡语义。
- 测试敌人在finally清理；未修改随机种子以挑选有利分布，未改变攻击实现。

## 离线验证

| 命令（Mod目录） | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Release` | 0警告0错误，最终保留Debug构建 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 347通过，新增7 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，合计366静态检查 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540断言通过，本批没有新增纯规则断言 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，非游戏存读档 |
| 四内容门MvpContent/StructuralContracts/LocalizationStyle/CardEffectTests（各`-ProjectDir .`） | 通过；225卡/32遗物/10附魔、223可执行卡效/2设计待定 |

既有失败保留：VisualAssets第317行诱惑度口红/独立数字；全卡审计GagCurse费用1/2差异、4未知元数据、215文本待审、2 designOnly、2 retiredCompat。审计只用`--no-write`，未覆盖并行报告。

待验：游戏脚本执行、自然战斗中的多段/状态伤害和减层、不同随机分布、卡面升级预览截图、完整存读档、多人。离线通过不是游戏验收；总目标仍未完成，不标VERIFIED，不部署。
