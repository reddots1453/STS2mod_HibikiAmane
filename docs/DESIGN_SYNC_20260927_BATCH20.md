# DS27-02N：引燃实际目标与多次施放

日期2026-09-28；前置快照`d03fc8b7`。上一轮范围提交为实际进展；本轮DesignDoc逐行与词级diff均空，复核消耗体系完整条目、Plan与矩阵，先记录任务再编码。未部署，未改DesignDoc、并行素材和审计。

## 证据与修正

设计要求引燃1/0费，消耗一张手牌，下回合开始时将“其”打出。旧实现保存CardId/WasUpgraded后在消耗堆FirstOrDefault，存在同名同升级态错选；同一个Counter Power多次施加还覆盖前一目标。已有测试只用一张目标牌，无法发现两种问题。

- `Ignite.OnPlay`在真实消耗后将选中的CardModel交给Power，不克隆、不以同名模板代替。
- `IgnitePower`采用当前游戏DLL中NightmarePower的`PowerInstanceType.Instanced`，各次施放各自保留目标。仓库较旧反编译使用IsInstanced，首次构建因此失败；已读取安装DLL核实并改为当前接口，最终构建成功。
- 内部数据保存选中牌的身份引用和一次性Resolved状态。同拥有者的回合开始时消费调度、清空引用、移除该Power，之后原生AutoPlay；嵌套/重复回调不会重复执行旧目标。
- 目标在抽牌/手牌/弃牌/消耗等战斗牌堆之间移动或升级不丢失身份；真正移除、离开该战斗或战斗结束则不臆选替代品。保留原生自动目标、无法打出处理和免费出牌，不自行实现扣费逻辑。
- 保留旧CardId/WasUpgraded字段便于兼容已有数据，但不把缺失引用的旧信息当作可靠目标。内部引用采用原版运行中数据范式，本批不添加自定义战斗存档序列化，不能宣称旧的中途Power对象必能重建目标。
- 卡牌费用、类型、稀有度和文本已与正式条目一致，本批未无故改动，新增精确回归。

## 自动验证

| 命令 | 结果 |
| --- | --- |
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 最终0警告0错误，未部署 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 160项通过，新增8项 |
| `python scripts/TestCardLocalizationAudit.py` | 19项通过，静态累计179 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 344既有生产逻辑断言通过；不是引燃引擎执行 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33既有编码断言通过；不是引燃存档测试 |
| MVP / Structural / LocalizationStyle / CardEffectTests（`-ProjectDir .`） | 四门通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有317行诱惑度图标/独立标签失败，未放宽 |
| `python scripts/AuditCardLocalization.py --no-write` | 225注册、1既有GagCurse费用差异、4未知元数据、215待渲染、2仅设计、2退役兼容；未写报告 |

## 游戏内入口与未完成验收

一次性单人天音测试战斗运行`ms_test_cards confirm Ignite`。现有框架会重置战斗、牌组夹具和资源，勿用于需要保留的存档。已替换只有单一目标的旧测试，基础/升级各至少35条行为断言：

- 同名同升级态诱饵先在消耗堆，选中另一张改为17伤害，核对实际伤害、原卡去向、诱饵仍在消耗堆。
- 连续两次引燃，保存11和23伤害的两张同名牌，通过真实`Hook.AfterPlayerTurnStart`派发，检查34总伤害和两个效果都清理。
- 零能量免费执行、其他玩家回调不触发、不立即打出、重复回调不重放。
- 选中后升级并分别移动至抽牌/手牌/弃牌堆，仍打出同一张升级后的9伤害牌。
- 真正移除目标不替换同名牌；只有旧ID元数据时不猜测；原生无法打出的状态牌处理不挂起；空手牌不产生调度。

这些测试已编译，**未在游戏内执行**。原生完整自然回合、额外回合、跨回合保存退出恢复、多个同时显示Power、其他Mod在回合开始阶段生成引燃的组合仍需游戏内验收。不能把直接钩子调用当作自然回合和保存恢复通过。

## 另外两项待确认

Q12：连锁破坏重复施放的层数收益，以及同一张牌打出前累计多次“四张消耗”触发，是增加同一张牌的打出次数，还是依次作用于后续牌？当前多层Amount没有收益，待确认后修复，不能把旧实现认作设计。

Q13：子守歌多层是否每层都生成1张困了，并在全部生成后按最终手牌数计算各层格挡？当前只增加格挡倍率、始终生成一张困了。2026-09-28已向用户询问，未自行确定新规则。

千咒之大镰已有4/6成长和永久同步，本轮未发现足以证明新故障的证据；咒印传染仍需补充全类型/克隆等边界测试，不把查看代码算成已验收。全量卡牌、怪物、事件和第四层目标继续保留，未宣告完成。
