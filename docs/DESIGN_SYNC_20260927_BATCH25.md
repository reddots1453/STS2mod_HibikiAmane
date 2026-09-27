# DS27 批次25：节制与耐心战斗效果

日期：2026-09-28；前置快照：`cc56366c`；ACT4-001 → DS27-05D。
状态：IMPLEMENTED，游戏内待验；未部署，总DesignDoc任务未完成。

## 审阅证据

DesignDoc逐行和词级diff无漂移，完整核对相关七美德条目及原版语义约定。当前DLL确认：

- `CardSelectorPrefs(prompt, minCount, maxCount)`允许0～上限并要求手动确认，不用固定数量构造器。
- `FromCombatPile`按战斗牌堆选择，不自带立即消耗语义。
- `CardEnergyCost.AddUntilPlayed(-1)`为相对能量修正，期限WhenPlayed，非EndOfTurn；原生X费和不可打出费用按自己的规则处理。`InvokeEnergyCostChanged`刷新显示。
- 当前CombatManager在BeforeCombatStart前已经填充玩家战斗牌堆；可在此从抽牌堆选择。原生`AddGeneratedCardToCombat`处理生成记录及满手转弃牌堆。

## 修改

1. `VirtueCombatRules`集中阶段策略：节制上限0/2/3/3/3；耐心Stage1/2战斗开始触发，Stage3旧档与Stage4觉醒本人回合开始触发；减费仅Stage2/3/4。
2. `TemperanceRouteRelic`允许不选，残缺至多2张、完整/觉醒至多3张。专用中文提示明确附加消耗。只给实际选中、仍在同一战斗本人抽牌堆中的实例添加关键词，不移动牌，不附加迅捷，不修改永久牌。
3. `PatienceRouteRelic`从合法、已解锁、可战斗生成的普通/罕见/稀有圣洁池使用战斗生成RNG取1张，原生战斗创建并入手。空池/无战斗安全返回。移除自动升级，完整/觉醒在入手前附加减费，保留跨回合直到该实例打出；减能量不是免费打出，不改变其他资源成本。觉醒无战斗开始的额外生成。
4. 两遗物Stage1/2/4描述逐字对照设计，保留标点并添加关键词颜色；Stage3旧档描述与觉醒相同。未修改DesignDoc或其他路线规则。

## 测试与结果

在Mod目录执行：

| 命令 | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告、0错误 |
| 同命令Release，随后重建Debug | 均0警告、0错误 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 新增49条，累计2715生产断言通过 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 200通过，新增8 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，静态合计219 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33既有编码断言通过，不是本轮完整存读档 |
| MVP/Structural/LocalizationStyle/CardEffect四个PS1各`-ProjectDir .` | 均通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有317行诱惑度独立数值栏失败，未放宽 |
| `python scripts/AuditCardLocalization.py --no-write` | 225注册；1费用差异、4未知元数据、215待文本、2设计独有、2退役兼容，报告未覆写 |

49条执行生产阶段规则，以独立字面表验证所有阶段、非法阶段、本人/其他人回合、首次两个钩子只生成1张、连续4回合计数。静态检查覆盖原生相对减费、选牌上下限接线、非立即消耗/附魔/升级、同Owner同牌堆保护和描述标点。旧三试炼检查从内联Stage判断更新为同时验证策略调用与策略正文，未移除沉睡保护。

## 游戏内入口与待验

`ms_test_combat_virtues confirm`仅Debug、单人本角色的进行中战斗，禁止重入。**会清空遗物与战斗牌堆，仅用于可丢弃测试局；未执行，不会自动运行，不恢复测试局数据。** 结束或失败均恢复原TestMode。

已编译测试包括：

- 真正`BeforeCombatStart`+`TestCardSelector`：各阶段0/1/上限选择；只给选中实例加消耗，不即时移动牌；手牌/弃牌堆不受影响，空抽牌堆无弹窗。
- 延迟选择过程中移动牌，确认后不错误修改；已有锋利附魔与消耗共存、不覆盖附魔。
- 真正耐心战斗开始/两次本人回合钩子：正确生成次数，合法圣洁牌、不升级。
- 原生费用计算/回合清理/打出清理：0/1/2/4能量、X费、不可打出牌、叠加其他相对减费；战斗克隆继承减费但清理相互独立。
- 满手生成进入弃牌堆并保留减费，永久牌组关键词未改变。

日志`[DS27CombatVirtueTest] PASS/FAIL`。脚本调用费用打出清理接口，不等同于手动实际打出全卡池；真实UI跳过/少选、卡面费用刷新、生成动画、存读档、多人及跨战斗重置仍须游戏内验收。

## 谦逊审计与后续

本轮检查确认两项实质旧实现：`HumilityLesson`仅翻倍Damage/Block而没有移除其他描述和效果；`HumilityRouteRelic`按句号数量猜测纯伤害/格挡牌并只抽1张。需要整体卡牌效果改写及可判定的描述语义，保留附魔，移除其余卡牌规则，并抽2张。不以单改数字或增补另一条句号正则冒充修复。本轮未修改谦逊，后续任务明确保留。

其他路线阶段效果、慷慨互斥供奉、正式开场和最终Boss后领奖，以及此前卡牌/怪物/事件/视觉待项仍未全部完成。
