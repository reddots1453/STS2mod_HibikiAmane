# DS27 第54批：娅露丝的书库

- 前置：`aa69ce99`；Plan同步：`266420e6`。
- 设计依据：先古书库正式条目，用户确认命名、先古来源与等级；本批逐行/词级无设计漂移。
- 状态：IMPLEMENTED，游戏内待验；未部署。并行角色源码、资源、本地化审计报告和历史Changelog不纳入。

## 生产修正

`YarusLibraryPower`只对仍持有、正层数、存活拥有者有效。选择牌堆返回后重新检查状态与战斗身份、所选实例是否来自本次三选项；逐张自动打出时检查实际拥有者/牌堆及战斗未结束。移除后的引用不再禁抽。保持原战斗生成随机流、原版StableShuffle、快照、十张上限、原生AutoPlay及不可打出牌的原生处理，不新增费用/资源/卡牌规则。

## 自动脚本

入口：Debug专用可丢弃单人战斗中的 `ms_test_cards confirm InsatiableGreed`。该命令会清空测试局战斗资源并改变生命等状态，不在正常游戏自动执行，本批未运行。

`DesignSyncLibraryContract`替代仅总伤害的旧探针，基础/升级每变体最低200效果断言：

| 范围 | 具体证据 |
|---|---|
| 获取与文本 | 尘封魔典实际SetupForPlayer结果、Ancient/费用2/升级保留；跑局与战斗实例的完整描述/名称 |
| 15种组合 | 抽牌/弃牌/消耗堆 × 0/1/9/10/12张；三选项顺序及身份 |
| 随机及出牌 | 克隆生成RNG，独立Fisher-Yates预测；实际CardPlayStarted逐张引用与顺序，升级/基础混合伤害 |
| 上限/不重放 | 快照最多十个不同实例；弃牌堆返回不重复，未选中牌保持原处 |
| 隔离 | 其他两牌堆原实例/相对顺序不变、手牌不变、永久牌组数量不变、洗牌RNG不变 |
| 原生语义 | 零能量自动出牌、伤口不可打出按原生移至弃牌、技能从消耗堆正常打出；真实Draw被阻止且无CardDrawn历史 |
| 生命周期 | 外部角色回调不执行、不禁其抽牌；异步失败/取消原异常传播且无结算；选择中移除不再出牌/消耗RNG，移除后真实Draw恢复 |

回合开始使用实际Power回调，不冒充完整引擎自然回合。随机预测使用原生相同List.Sort排序处理相同比较键，洗牌交换独立编写而非调用生产StableShuffle。

## 已执行验证

- `python -m unittest discover -s scripts -p 'Test*20260927.py'`：432通过（本批新增8）。原禁抽源码契约同步为有效实例条件，未删除验证。
- `python scripts/TestCardLocalizationAudit.py`：19通过；合计451静态测试。
- `dotnet run --project tests/DesignSyncContracts --no-restore`：13540生产纯规则断言通过。
- `dotnet run --project tests/LayeredSaveContracts --no-restore`：33生产编码断言通过，非引擎执行。
- Debug和Release，`-p:DeployMod=false --no-restore`：均零警告零错误，最后构建Debug。
- ValidateMvpContent、ValidateStructuralContracts、ValidateLocalizationStyle、ValidateCardEffectTests：全部通过。
- 升级绑定审计：225模型、149直接/6间接绑定、0失败。
- ValidateVisualAssets：仍在317行既有诱惑度图标/标签契约失败。
- AuditCardLocalization `--no-write`：仍为GagCurse费用1/2差异，4未知元数据、215待渲染、2仅设计、2退役兼容；report=None，未覆盖并行报告。

## 尚未完成的验收

本批脚本仅编译，未游戏内执行；自然回合开始、实际选择界面/动画、战斗结束或房间切换期间的选择、多人所有权与随机同步、保存/加载仍需游戏验收。状态守卫的静态检查不替代这些真实流程。总目标及先前OPEN问题不因此关闭，也未宣称全部卡牌/怪物/路线/事件完成。
