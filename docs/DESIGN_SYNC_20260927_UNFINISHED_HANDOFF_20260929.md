# DS27 未完成项交接（2026-09-29）

## 1. 接手起点

- 分支：`codex/maiden-controlled-merge-v2`；本交接的实现快照：`aaf29d57`（神清气爽奖励效果）。本轮仅整理文档，不改代码、不部署，也不将原目标标为完成。
- 原目标：同步本轮 DesignDoc 全部变更，包括卡牌数值、规则、稀有度、标点与排版，并提供卡牌、怪物行动、第四层路线、事件的自动测试脚本。
- 用户指定的 9 月 15 日 23:44 精确快照未找到；此前采用 `125d9f1c` 为保守比较基线。不要声称已恢复精确时间点。
- 用户已经要求加速：只核对本轮变更，不再重新审计全项目，不为谦逊继续扩建逐牌手写断言表，不为旧测试清理另开长线工程。
- 原目标目前为 `blocked`。剩余部分受限内容，不能擅自删除这些范围，再宣告全目标完成。
- 玩法仍以 [DesignDoc](../DesignDoc.md) 为准；实施遵守 [协议](DESIGN_CHANGE_PROTOCOL.md)、[Plan](../PLAN_FRAMEWORK.md) 和 [追踪矩阵](DESIGN_TRACEABILITY.md)。本次检查 DesignDoc 相对 HEAD 的逐行、词级 diff 均为空。

## 2. 真正尚未完成的实现

| 范围 | 当前缺口 | 接手处理与验收边界 |
|---|---|---|
| 五个新增事件 | `EVENT-NEW-001` 可疑的商店、`004`～`006` 按摩店三段、`007` 择祸从轻，尚未完成正式事件实现 | 补事件入口、选择结算、持久化及定向测试。 |
| 跨幕预约链 | 固定预约、跨幕恢复、实际问号房间替换尚未接通 | 贪婪已有“预约优先、免费商店顺延”边界和模拟测试，但没有正式预约提供者；不能把模拟预约当作完整事件链。 |
| 事件奖励获取 | `Refreshed`（神清气爽）效果已实现，但事件获取入口未接入；事件遗物“榨乳器”及衍生牌“乳汁”仍缺实现 | 不要重写 Refreshed 本身；相关受限内容一并处理。缺图目前使用原版回退，不擅自制作新素材。 |
| 怪物行动表 | 本轮新表尚未逐怪物、逐行动完整同步，全面回归脚本也未完成 | 已有适配器和旧控制测试不等于新表全部完成。需追踪原行动恢复、阶段转换、自然触发限制、强制改意图等差异；受限内容先解决设计边界。普通状态机可靠性可独立处理。 |
| 卡牌遗留证据 | `Hypnosis` 缺独立正式卡面条目，现有文本覆盖报告未识别其完整契约 | 继续实现。 |
| 多人路线边界 | 试炼归属与完整同步规则仍缺足够明确的验收依据 | 慷慨已接原生分配/网络子项，不代表所有路线的多人流程已验收；未写明的产品规则不要自行补完。 |

注意：用户 Q10 已确认“写完整的七个事件与角色遗物纳入本轮”。DesignDoc 中部分 `DRAFT` 标签仍未统一，是文档成熟度同步欠账，不能解释成用户未批准。替代设计确定后，应同步这些标签、Plan 和追踪状态。

“欲望爆发”位于明确标注未完成的敌人草稿，不与正式事件引用的“乳汁”混为一类；没有完整需求时，不自动生成新正式卡牌。

## 3. 已实现但尚未完成验收的部分

这些条目主要缺游戏运行证据，不应直接回退成“代码没写”。所有脚本的编译通过，均不代表实际执行通过。

| 范围 | 已有实现 | 仍需验证 |
|---|---|---|
| 本轮卡牌与文字 | 已注册 228 个卡牌模型；最近文本报告在 218 个现行分类条目中识别 217 个双实例全文声明、1 个 Hypnosis 未识别 | 基础/升级、战斗外/战斗内动态文本、图标、标点换行、选择目标、重复打出、跨战斗恢复；声明数量不是渲染或语义通过率。 |
| 谦逊 | 正式选牌已接自动提取程序，实例攻击/格挡翻倍，保留次数与 X，移除原触发/额外效果；描述替换与附魔文字保留已接入 | 正式选牌、空效果、动态数值、重复格挡、原卡/改写卡觉醒、附魔、保存恢复。目录可解析不证明所有实际结算正确。 |
| 第四层路线 | 14 路线、42 试炼及阶段状态、碎片、献祭、单人领奖、先古前开局选择、正常结局流程已实现 | 自然跨层、完整获得奖励、商店购买、第二试炼吸收碎片、存读档、多人所有权。正式预约缺口见上表。 |
| 慷慨 | 原生宝箱分配后供奉、互斥奖励、隐藏无收益选项、网络子项、觉醒删牌已实现 | 实际单人/双人奖励界面、保存重进、领取与供奉不重复结算。 |
| 神清气爽 | 三场战斗开场额外抽 2；原生抽牌修正、持久计数/本场收据、抽牌完成后耗尽移除 | 实际三场战斗、无抽牌、重复回调、存读档、其他角色隔离；现有脚本模拟战斗不能替代自然流程。 |
| 两个新增事件 | `DarvAssistance`、`UndeadGathering` 已有正式类和定向脚本 | 正式事件选择、奖励界面、随机结果、精确一次结算及保存恢复。 |
| 总体验收 | 多套离线验证与 Debug/Release 构建已有通过记录 | cards、monsters、fourth_act_routes、events、multiplayer、visual_layout 六组游戏验收仍为 `NOT_RUN`。 |

当前精确登记：中立 49、堕落 63、圣洁 60、专属诅咒 17、共享衍生 39，共 228 模型；遗物 34、附魔 10。它们是登记契约，不是完成百分比。

## 4. 接手入口

以下路径相对于 Mod 根目录。先从对应入口继续，不必顺序翻阅上百份历史批次记录。

- 怪物：`src/Core/Intents/VanillaIntentAdapters.cs`、`IntentAdapterRegistry.cs`、`IntentMoveFactory.cs`；旧测试位于 `src/Debugging/ControlIntents/`。
- 事件：`src/Events/DarvAssistance.cs`、`src/Events/UndeadGathering.cs`；新增事件需求为 `EVENT-NEW-001`～`007`。
- 奖励：`src/Relics/Refreshed.cs`；现有测试 `src/ConsoleCommands/DesignMagicRelicTestConsoleCmd.cs`。
- 路线：`src/Acts/FourthRouteRewardFlow.cs`、`FourthRouteRewardOffer.cs`；`src/UI/FourthRouteRewardScreen.cs`；`src/ConsoleCommands/DesignRouteRewardTestConsoleCmd.cs`。
- 谦逊：`src/Core/Cards/HumilityExtractedCards.cs`、`HumilityRewriteCapability.cs`、`HumilityNativeEffects.cs`、`HumilityRewritePresentation.cs`、`HumilityAwakening.cs`；`src/Patches/HumilityRewritePatches.cs`；提取工具 `tests/HumilityCallExtraction/`。
- 谦逊正式游戏回归：`src/ConsoleCommands/DesignHumilityRuntimeTestConsoleCmd.cs`。正式选择与此回归都已脱离旧手写表。`HumilityCardProfiles.cs` / `HumilityProfileDefinitions.cs` 仍被历史离线测试保留，删除它们不是交付前置条件。
- 汇总验证：`scripts/ValidateDesignSync20260927.py`；文字证据：`scripts/ReportCardTextCoverage20260927.py`；内容清单：`docs/content_contract_20260824.json`。

提取目录当前为 785 条、解析不支持数 0，嵌入资源 `MaidenSuccubus.HumilityExtractedCatalog.json`。它覆盖已提取的原版与本 Mod 源码，不保证任意第三方程序集；不要擅自扩成“兼容所有 Mod”的无限任务。

## 5. 最近验证证据及复验方式

| 提交 | 实际证据 | 限制 |
|---|---|---|
| `aaf29d57` | 625 静态检查、Debug 0 警告 0 错误、34 遗物精确登记；选定三套通过 | 神清气爽相关游戏脚本仅编译；未部署。 |
| `0de8070c` | 117 提取器自测、17 接线检查、Debug 和实际 DLL 目录读取通过 | 修复纯计数格挡觉醒分类；游戏未运行。 |
| `4d45a9dd` | 静态 624、生产规则、存档编码、Release、内容共五套选定验证通过 | 当时为 33 遗物；不是最新全量结果。 |
| `2f1cf086` | 正式谦逊回归迁移与重复格挡修复，提取器/接线/Debug/实际目录验证通过 | 游戏脚本仅编译。 |
| `21ea4ace` | 原生商店碎片购买测试补齐、9 项静态及 Debug 通过 | 真实商店场景未执行。 |

本地报告（`obj/` 产物可能不随 Git 交接）：

- 最新三套：`obj/design-sync-validation/20260928T190459Z-94beab83f92e/report.json`。
- 前次五套：`obj/design-sync-validation/20260928T184858Z-2a994de0c19e/report.json`。

报告为 `passed_selected`，不是全量完成；统一工具对选定验证返回码 2，要结合报告判读。运行时无源码漂移只针对当次运行，不表示报告指纹等于之后的文档提交。

在 Mod 根目录按修改范围选用，不要为重复报数字重跑全套：

```powershell
python scripts/ValidateDesignSync20260927.py --list
python scripts/ValidateDesignSync20260927.py --suite design_static --suite debug_build --suite content
dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false
dotnet run --project tests/HumilityCallExtraction -- --self-test
```

统一入口现在有 15 套离线验证；不要引用旧状态文件的“11/12”或“12/12”作为当前结论。改动完成后，针对实际影响面补测，再决定是否需要一次最终合并验证。

游戏测试须先获用户对可丢弃测试存档的明确授权。这些命令可能清空牌组、遗物或调整生命/金币，不能直接在真实进度中执行：

- `ms_test_humility_runtime confirm`：单人本角色战斗，至少两个敌人。
- `ms_test_route_reward confirm merchant`：实际商店内验证碎片购买。
- `ms_test_magic_relics confirm`：含神清气爽三场开局测试。
- `ms_test_darv confirm`：事件奖励选择与一次性结算。

## 6. 不要重复做、不要重复问

- Q1～Q20 已答复，不要从旧 `OPEN` 表再次发问。旧 [状态汇总](DESIGN_SYNC_20260927_CURRENT_STATUS.md) 的第 61 批正文是历史记录，上方第 110 批等增量覆盖其过时待办。
- 娅露丝的书库是留在手牌时持续影响左右邻牌；旧效果已移到节制奖励。该拆分已实现，不回退。
- 连锁破坏各 Power 独立计数并依次作用后续牌；子守歌按全部生成后的结果计算。这些不是新待确认项。
- 谦逊保留次数/X，删除其他关键词和原触发；不恢复逐卡人工效果表。
- 免费使用原版语义，不能把 X 能量/副资源的计算及支付手工抹零；相关修复已完成。
- 三路线合池等概率按牌抽取；枯木树枝已按用户提供的 `F:\steam\steamapps\common\SlayTheSpire` 源码对照实现。
- 原意图入栈顺延；玩家强制改意图不受自然连续上限/冷却限制；成功后的禁用例外按无例外。不要重新询问这三点。
- 耐久降到 0 保持变身，再次损失才解除；0 层不提供那次减伤或邪瘴增欲望，“全损”为耐久 ≤1。按已答规则追踪，不自行改数值。
- DesignDoc 没有完整定义额外第四幕/Boss 战斗，不能为了“第四层完成”自行造内容。
- 不把全文声明、模型登记、提取器 0 解析失败、构建通过等同于完整实机验收。

## 7. 最短接续顺序与工作树边界

1. 阅读本交接，确认 `aaf29d57` 之后是否有新提交/用户设计修改；仅检查新增漂移，不重开此前完整审计。
2. 优先取得剩余受限事件/怪物内容的非性化替代设计或明确范围决定；不得将未答复视为授权删项。没有新指令时，本交接不恢复原阻塞目标。
3. 对允许继续的具体缺口先同步 Plan/追踪，再实现对应入口与自动测试；已有奖励、谦逊与路线框架直接复用。
4. 定向离线验证后，在用户授权的测试存档执行上述运行场景；游戏六类验收逐项记录，不以编译代替运行。
5. 未经本轮明确授权不部署 DLL。只提交自己修改的文件；更新 CHANGELOG 并保留前置快照。

本交接前已存在并行修改：`CHANGELOG.md` 63 增/17 删、`.review/card_localization_audit.json` 702 增/507 删、`src/Characters/MaidenSuccubusCharacter.cs`，以及仓库其他已暂存资产/项目。不得覆盖、暂存或提交它们；CHANGELOG 只能提交本轮新加的独立段落。禁止仓库级 `git add -A`、清理或回滚。

本轮文档验证：核对关键路径与 Git 快照、Markdown 本地链接和 `git diff --check`；不重跑构建，不修改玩法或 DesignDoc。全部实现/测试目标仍未完成。
