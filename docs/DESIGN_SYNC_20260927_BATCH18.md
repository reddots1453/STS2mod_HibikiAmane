# DS27-02M：万咒之噬的计算伤害与单次消耗快照

日期2026-09-28；前置快照`744d4990`。上轮完成范围提交，属于实际进展；本轮继续全卡行为核对。DesignDoc逐行与词级diff均为空，重读消耗体系相关正式条目、Plan和矩阵，先记录任务再编码。未修改DesignDoc、素材和并行审计。

## 证据与修正

当前DesignDoc要求3/2费、初始1伤害，在本场战斗中任何战斗牌堆均累计被消耗攻击牌的伤害数值，只增加伤害。旧代码用`CalculatedDamage.IntValue`作为备选值，而DynamicVar.IntValue只取初始BaseValue。因此已增长的万咒之噬等计算型攻击被消耗时，无法贡献完整计算值。

- `ExhaustDamageSnapshot.ReadCardDamage`先读取普通Damage，没有该变量时读取`CalculatedDamage.Calculate(null)`；无伤害变量或非攻击贡献0，负数钳制0。只取该牌的伤害数值，不把多段次数、附加效果或全局力量/易伤乘区再次算入吸收量；没有目标时按原生无目标计算。
- 必须在原生消耗钩子枚举监听者前固定值：否则被消耗的万咒之噬本身也是监听者，自我增长可能改变后续副本读取的值。Prefix捕获，原AfterCardExhausted保持负责实际累加；不另行重复派发游戏结算。
- Postfix只包装Task，`finally`等待完整任务完成或失败后释放；同步异常由Finalizer补清理。所有Harmony入口用Safe.Run，不吞掉原Task失败。
- `WeakInstanceValueScope`使用弱身份键、每实例值栈；嵌套事件独立捕获，结束恢复外层，同值不同实例不共享。重复释放和非LIFO父作用域释放安全，不持有卡牌强引用。
- 接收端显式限制战斗牌堆、同拥有者、同CombatState，保留原SavedProperty与动态ExtraDamage同步，不改变永久牌组或初始值。

纠正调查中的初步推测：当前安装DLL的`Hook.AfterCardExhausted`使用`IterateCombatHookListeners`，后者只调用CombatState监听者枚举；永久牌组不在正常消耗回调列表中。因此本批**没有证实自然流程会污染永久牌组**。相关保护是防御性边界与回归，不以假设当作已复现Bug。

## 自动验证

| 命令 | 结果 |
| --- | --- |
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告0错误，未部署 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 144项通过，新增8项 |
| `python scripts/TestCardLocalizationAudit.py` | 19项通过，静态总计163项 |
| `dotnet run --project tests/DesignSyncContracts/DesignSyncContracts.csproj --no-restore` | 新增15条真实生产作用域断言，累计320条通过 |
| `dotnet run --project tests/LayeredSaveContracts/LayeredSaveContracts.csproj --no-restore` | 33既有编码断言通过，不是引擎执行 |
| MVP / Structural / LocalizationStyle / CardEffectTests PowerShell门（均`-ProjectDir .`） | 通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有317行诱惑度图标/独立标签失败，未放宽 |
| `python scripts/AuditCardLocalization.py --no-write` | 225注册、1既有GagCurse费用差异、4未知元数据、215待渲染、2仅设计、2退役兼容 |

纯作用域测试覆盖相同值不同身份、跨await、内外层快照恢复、重复释放、非LIFO释放、异常清理、独立实例，不是源码字符串模拟。静态门只证明接线、文档与测试入口存在，不当成战斗效果已通过。

## 游戏内契约与未验项

在一次性单人天音测试战斗使用`ms_test_cards confirm AllCurseBite`。该框架会破坏当前战斗夹具，勿用于要保留进度的存档。基础/升级各场景，至少28条行为断言：

- 依次放入抽牌/弃牌/手牌/消耗/结算区，每次真实消耗6伤害打击，累计30；技能不增加，升级打击增加9，实际打出造成40伤害。
- 同时存在永久实例，验证其始终0；新的永久克隆初始0，而战斗内复制保留已成长39；原生序列化往返保留39并同步ExtraDamage。
- 真正消耗该成长副本时，其他副本与其自身都吸收消耗开始时的40，不能只取1，也不能让后处理的牌读取更新后的80；结束后查询恢复实时值。
- 直接调用永久实例的钩子应无效，此项明确是防御性合成探针，不伪称原生钩子会派发永久牌组。
- 快照期间修改数值、嵌套另一次快照后恢复，以及故障Task的finally清理。这些是实模型加合成生命周期测试，不是自然多卡递归的实机证明。

契约**已编译未执行**。自然战斗、UI动态预览、多玩家相互隔离、完整中途存档恢复、其他计算型原版牌及其他Mod钩子并存仍需游戏内验收。存档字段往返不等于引擎恢复测试。

子守歌与连锁破坏多层语义、其他牌、机制/事件/怪物/第四层仍未全部关闭。本批没有擅自确定这些尚未核对清楚的叠加规则，完整目标保持进行中。未部署DLL，未启动或关闭游戏。
