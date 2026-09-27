# DS27 批次30：傲慢与自负

日期：2026-09-28；前置快照：`8d47ab15`；ACT4-001 → DS27-05J。
IMPLEMENTED，游戏内待验；未部署，总目标未完成。

## 审计结论与变更

DesignDoc逐行/词级无漂移，完整复核傲慢、阶段独立效果与沉睡不揭示后续规则。原代码残缺/完整/觉醒给1/2/3力量和自负，Stage0无效果，Stage3兼容旧觉醒；自负是Debuff/Counter，AfterDamageReceived只针对Owner的正UnblockedDamage，每次失去1层再失去1力量。净化按Counter和TypeForCurrentAmount选取，因此可以减自负层数，且单独净化不会执行伤害钩子扣力量。上述行为无需重写，本批保留。

实际显示差异：

- 自负原普通/智能描述为“每次失去生命值时，失去1层自负和1点力量。”，同步为DesignDoc原句“每次受到未被格挡的伤害时，失去1层自负，并失去1点力量。”；保留逗号与顺序，格挡/力量按既有规范加色。
- 傲慢Stage1～4统一力量颜色；遗物附加力量与自负悬停，沉睡不附加。自负状态附加原生力量和格挡悬停。
- 没有新增猜测性数值函数或改写已有正确扣层行为；以原生命令测试该实现。

## 验证

| 命令/门 | 结果 |
|---|---|
| Debug / Release，`-p:DeployMod=false --no-restore` | 零警告零错误 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 240通过，新增8项傲慢/自负精确文本、接线和测试覆盖检查 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，静态总计259 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 3591既有生产规则断言通过，本批未增加纯规则逻辑 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33既有编码断言通过，不是自负状态游戏内存档验收 |
| ValidateMvpContent / StructuralContracts / LocalizationStyle / CardEffectTests | 四门通过；发现新增“格挡”未加色后已修正并复验 |
| `AuditCardLocalization.py --no-write` | 225项；既有GagCurse费用1/2差异、4元数据未解、215文本待审、2设计独有、2退役兼容仍在，未写共享报告 |
| ValidateVisualAssets | 既有317行诱惑度口红独立标签门失败，未削弱门禁 |

## 游戏内测试（已编译，未运行）

`ms_test_pride confirm`，日志`[DS27PrideTest] PASS/FAIL`。仅Debug、单人响木天音、进行中且有存活敌人的战斗。

**破坏性测试，仅可丢弃测试局。清除遗物/牌堆/状态，改生命和资源；不恢复游戏数据，结束或异常恢复TestMode。**

5阶段调用真实RelicCmd、PowerCmd、CreatureCmd和PurificationPower，检查：

1. 0/1/2/3/4阶段分别给0/1/2/3/3层，负面效果及计数类型正确。
2. 完全格挡和0伤害不减层；伤害他人不减自身层；部分格挡只按一次伤害扣一层。
3. 多次实际伤害逐次扣1，自负耗尽后移除，后续伤害不再继续扣力量。
4. 净化候选包含自负；净化减层不扣力量；之后的伤害只扣尚未净化的层。
5. 原生SavedProperties保存/恢复遗物阶段，下一场重建正确层数，之前的伤害/净化不带入。
6. 直接设置当前生命不是伤害通知；实际不可格挡非攻击伤害会触发，力量可由0降至-1。

此脚本直接模拟战斗开始与净化回合钩子，不是完整自然回合/联机验收；保存测试只覆盖遗物阶段，不代表战斗中途完整状态恢复。仍需运行脚本、检查实际悬停布局、完整退出读档、自然回合净化和真实多段攻击动画。未运行前不标VERIFIED。

其余全量卡牌/怪物/事件/路线/视觉待项及Q12/Q13/Q14仍保留。
