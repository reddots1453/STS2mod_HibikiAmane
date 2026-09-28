# DS27第37批：清心／浊心项链

## 范围与版本

- 需求：`RELIC-CHAR-005`、用户Q10对完整角色遗物正式化的确认。
- 批次前置：`108cdc45`；需求/Plan/追踪先独立提交`8802aebc`，作为实现前置。
- DesignDoc写前行级/词级diff均为空；本批只增加项链稳定ID与READY成熟度，不改规则、数值或标点；其他遗物草案保持原状。
- 当前状态：IMPLEMENTED，游戏内待验；未部署、未启动/终止游戏。其他Agent暂存/未暂存资产、角色代码、旧日志和审计报告不纳入提交。

## 实现

- `src/Relics/Iteration2Relics.cs`：共享HeartNecklace，稀有度Rare；两个历史ID仍注册。正常候选只有Clear，Murky的IsAllowed/商店准入为false，仅保存兼容。
- `src/Core/Relics/HeartNecklaceRules.cs`：堕落≥3为浊心；清心在欲望≥5减1，浊心在欲望≤5加1，包含等于5。
- 行为和标题直接读取当前Run，无需AfterObtained订阅；删除未等待的RelicCmd.Replace，避免路线上下切换改动实例、获取楼层及重复获取回调。保留原生蜡制名称前缀。
- `TwinSoulChaliceDescriptionPatch`既有Safe边界增加项链分支，与标题使用同一ActiveEntry；两条文本逐字匹配DesignDoc，保留比较符号和标点，关键词颜色通过样式门。
- 真实变化走`Data.Desire.Modify`，保留普通资源获取阻挡与监听；非本人回合、其他角色、死亡或已移除实例不生效。不新增全队唯一持有规则。

## 已执行验证

从Mod目录运行：

| 命令 | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同上`-c Release`，随后再次Debug | 0警告0错误 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 296项通过，新增8项 |
| `python scripts/TestCardLocalizationAudit.py` | 19项通过；合计315 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 12885断言通过，新增132；编译生产规则而非复制实现 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33生产编码断言通过，不代表整局保存验收 |
| `ValidateMvpContent.ps1 -ProjectDir .` | 225卡/28遗物/10附魔契约通过 |
| `ValidateStructuralContracts.ps1 -ProjectDir .` | 通过 |
| `ValidateLocalizationStyle.ps1 -ProjectDir .` | 通过 |
| `ValidateCardEffectTests.ps1 -ProjectDir .` | 225注册、223可执行、2设计待定，通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有第317行诱惑度口红/独立数值标签断言失败，本批未改UI |
| `python scripts/AuditCardLocalization.py --no-write` | 既有GagCurse费用差异1项、元数据未解析4项、文本待核215项、仅设计2项、退役兼容2项；未写共享报告 |

纯规则使用独立字面表覆盖-5～5的所有堕落档位和0～10的欲望。静态检查覆盖正式文本/富文本、稀有度、两个ID的注册与获取边界、实时分支、无异步替换/订阅、原生资源及测试入口。

## 已编译、未运行的实机脚本

`ms_test_necklace confirm`：仅Debug、一次性单人响木天音战斗。脚本会删除遗物/清理战斗牌与状态、设置双方生命与资源，不恢复原局；不得在需要保留的存档运行。仅TestMode和并发标志在finally恢复。

- 两个ID分别实际Obtain/Remove、元数据与原生准入接口。
- 堕落每档及欲望0～9实际资源命令、等于5边界、其他玩家和其他角色隔离。
- 标题/描述真实格式化、变奏前后实例/槽位/ID/获取楼层不变。
- 原生ToSerializable/FromSerializable重建，**不调用AfterObtained**即验证当前分支及回合效果。
- 真正Hook.AfterPlayerTurnStart分发一次；正常资源增加阻挡消耗；移除后不触发。
- 实机脚本不设置10再断言项链结果：设置10本身会进入另一套满值结算，10的项链分支由生产纯规则测试覆盖。

## 待验与边界

- 自然回合、界面打开时路线变化后的悬停刷新、蜡制前缀实际显示。
- 整局保存退出/重进、两个历史ID存档、历史抓取袋与商店稀有度来源；本批未整表迁移已保存的抓取袋。仅模型重建脚本不能证明整局迁移已通过。
- 多人各自资源与原生奖励分配；没有新增跨玩家持有互斥，静态检查不等于联网验收。
- 没有专属新图，继续用既有原版circlet安全占位。
- 其他角色遗物、待澄清项、视觉门与全卡审计尚有剩余；不宣告总目标完成。
