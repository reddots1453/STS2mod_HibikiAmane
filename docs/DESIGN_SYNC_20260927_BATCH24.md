# DS27 批次24：仁爱与勤勉阶段效果

日期：2026-09-28；前置快照：`77126085`；ACT4-001 → DS27-05C。
状态：IMPLEMENTED，待游戏内验收；不代表全部DesignDoc同步完成。未部署。

## 范围与实现

DesignDoc逐行/词级无漂移，重读两条路线全部阶段和勤勉附魔数值。使用当前游戏DLL核实奖励选择、RNG序列化及克隆接口，不修改DesignDoc规则。

- `VirtuePickupRules`：可直接执行的阶段策略、牌组添加过滤、原版附魔数值闭区间。Stage0无效果；Stage1/2/4对应残缺/完整/觉醒；旧Stage3兼容觉醒常驻效果，不追发旧档历史拾起奖励。
- `BenevolenceRouteRelic`：残缺、完整拾起各随机升级2张可升级永久牌；觉醒仅在同Owner卡牌从非Deck进入Deck后随机升级2张。零张/不足2张安全处理，不提供卡牌奖励，不在胜利后触发；随机性使用Run的Niche序列。
- `DiligenceRouteRelic`：按2普通、2升级、3升级附魔构造原生CardReward，采用角色普通怪物战斗奖励选项及原生奖励钩子。生成/重掷得到的新候选先升级，再判定可附魔性；不是只升级选择后的牌。
- 附魔候选为游戏程序集的正式原版附魔、可用于实际卡牌，排除Deprecated和Mock；不混入本Mod附魔，不强行附到非法牌。Sharp/Nimble 1～5，Adroit 2～4，Momentum 3～8，Sown/Swift 1～2，Vigorous 6～12，其余固定1。知识库木札/伶俐对应Adroit，灵巧对应Nimble。
- 每个遗物保存拾起执行标记，重复通知不再发奖。奖励候选按实例弱表去重，不重复升级/附魔或消耗RNG；克隆时重建弱表。仅实际选择并加入永久牌组的牌触发附魔预览，无Owner或其他Owner不处理。
- `relics.json`两路线所有阶段文本同步，标点由静态脚本逐字比较设计（只剥除颜色标签），旧Stage3描述与觉醒一致。

持久收据在开始副作用前写入，用于防重复；不宣称原生奖励选择、异常中断的全部副作用具备事务回滚能力。未改用户存档，未补发旧版本已经领取的历史奖励。

## 自动验证

在Mod目录执行：

| 命令 | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告、0错误 |
| 同命令Release，随后重建Debug | 均0警告、0错误，未部署 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 新增203生产规则断言，累计2666通过 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 192通过，新增8 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，静态合计211 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33既有编码断言通过，非本轮引擎保存测试 |
| MVP/Structural/LocalizationStyle/CardEffect四个PS1各`-ProjectDir .` | 全通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有317行诱惑度独立数字栏失败，未放宽 |
| `python scripts/AuditCardLocalization.py --no-write` | 225注册，1费用差异、4未知元数据、215待文本、2设计独有、2退役兼容；报告不覆写 |

203条直接执行生产策略，使用独立字面阶段表、全部Owner/牌堆组合、7数值范围的每个可能结果和固定数值附魔，验证上下界及RNG调用次数。静态检查不冒充运行时结果。

## 引擎级测试入口（尚未运行）

`ms_test_virtues confirm`仅Debug、单人本角色、非战斗，且拒绝重入。**会删除当前遗物及牌组并重建测试数据，只能在可丢弃的新测试局使用，不能在正式存档执行。** 自动测试不开游戏、不调用此命令。

命令测试真实RelicCmd.Obtain、CardPileCmd.Add、奖励生成与选择器、永久牌组升级、重复拾起/保存字段恢复、合法附魔候选、实际升级附魔及重复处理RNG不变。日志标记`[DS27VirtueTest] PASS/FAIL`，异常和正常结束均恢复TestMode；它并不恢复被明确用于测试的牌组/遗物。

待手测：上述命令、三次试炼领取与UI交互、跳过/选择/重掷奖励、附魔图标/动画、退出/存读档中断、仁爱连续多张永久加入、临时战斗牌与其他角色不触发、多人。已有收据的保存字段模型测试不等于完整Run保存恢复验收。

## 未完成范围

其余12路线阶段效果，慷慨专属互斥供奉入口，先古奖励前正式开场、原版宝箱式奖励UI以及最终Boss后不依赖地图的领奖；其他批次记录的卡牌/事件/怪物与视觉待项继续保留。不会据本轮两遗物测试把总目标标记完成。
