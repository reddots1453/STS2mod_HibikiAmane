# DS27-04A / 06B：灵魂罗盘、达弗的援助

日期2026-09-27。前置`55e1f15a`；开始时DesignDoc逐行/词级无漂移。设计确认独立提交`0c1ba660`仅登记Q9/Q10已确认的成熟度与ID。实现状态IMPLEMENTED，不是VERIFIED；未部署。

## 对照范围

| 需求 | 实现证据 |
|---|---|
| RELIC-EVENT-005，仅-3＜堕落值＜3各加20个百分点 | `SoulCompass`通过现有IRouteRewardProbabilityModifier注入；生产纯规则`RouteRewardProbabilityBonus.ForSoulCompass` |
| 中立降低40个百分点 | 现有`RouteRewardProbabilities.Calculate`保持总和1；独立11行期望表验证真实C#生产代码 |
| 拾取3次普通奖励，不是奖励所有3张牌 | `CreatePickupRewards`创建3个CardReward，每个原版三选一；选后加入牌组，可跳过 |
| 普通奖励参与路线概率；指定稀有路线不得被换池 | 普通奖励使用ForRoom(Monster)；指定奖励使用Other/Uniform、Rare过滤、NoCardPoolModifications和事件RNG |
| 避免事件与遗物重复发奖 | `BalancedInsight`只Obtain；`AfterObtained`唯一发奖入口，SavedProperty标记在await前写入 |
| EVENT-NEW-002仅第三层/本角色 | `DarvAssistance.IsAllowed`限定CurrentActIndex=2和全部参与玩家为本角色 |
| 三分支、锁定、叙事与结果页 | 17项本地化与完整DesignDoc事件段逐项比较；引号、标点、段落和色标均保留 |
| 非本角色不生效、旧存档模型身份保留 | 罗盘检查Owner/Character；不替换BalancedLens，后者的退役/开局迁移仍待后续 |

在普通区间，(圣洁,堕落,中立)依次为：-2=(40%,28%,32%)，-1=(35%,30%,35%)，0=(30%,30%,40%)，+1=(30%,35%,35%)，+2=(28%,40%,32%)。±3及更外无加成。不是乘1.2。

## 自动测试及结果

Mod目录下执行：

- `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore`：0警告0错误。
- `dotnet run --project tests/DesignSyncContracts/DesignSyncContracts.csproj`：92断言通过。此独立控制台使用本机.NET10 SDK直接链接并编译两份生产纯规则源码；不复制算法，不加载游戏/Godot，不下载包（局部NuGet.Config清空源）。Mod本体仍为net9.0。最初以net9测试宿主恢复时缺少本地引用包且网络受限，改用已安装的net10测试宿主后离线通过；并非放宽业务断言。
- `python -m unittest discover -s scripts -p 'Test*20260927.py'`：49项通过。
- `python scripts/TestCardLocalizationAudit.py`：19项通过。合计68项静态检查；不是68张卡的游戏内效果验收。
- `ValidateCardEffectTests.ps1`、`ValidateMvpContent.ps1`、`ValidateLocalizationStyle.ps1`、`ValidateStructuralContracts.ps1`，均以`-ProjectDir .`执行：通过。注册为225卡、25遗物、9附魔。
- `ValidateVisualAssets.ps1 -ProjectDir .`：既有第317行“Temptation meter must use the formal lipstick with an independent value label.”失败；未修改该UI或断言，不声明全门通过。

## 游戏内测试入口（已编译，尚未执行）

`ms_test_darv confirm`：仅在本角色、单人、非战斗的一次性测试存档执行。会移除遗物、改变堕落值、增加卡牌及遗物，不用于正式存档。测试限定范围启用原版TestMode和TestCardSelector，finally恢复TestMode，避免挂起在真实奖励选择UI。

覆盖真实BeginEvent/选项Chosen、11档选项锁定/拥有遗物修正、移除后无加成、两路线三张不同稀有卡、同种子确定性、事件不消耗普通奖励RNG、重复Populate不重抽、实际入牌、双击无双发、普通奖励恰好三次、重复AfterObtained不双发、稀有奖励跳过及三次普通奖励全部跳过。该入口未实际在游戏运行，不能宣告通过。

仍待手测：第三层自然事件抽取、其他角色排除、奖励叠层关闭与结果页、17项文本实际排版、保存/读档标记往返、多人的选择同步。概率/资格的静态检查不等于上述运行时验收。

## 资源与剩余范围

- 尚无本轮专属图标/背景交接：罗盘使用原版circlet占位，事件使用原版whispering_hollow背景占位；不依赖Succubus或其他Mod，不擅自生成图片。
- 初始遗物按钮、全能宝珠多选、其他遗物、剩余事件、第四层42试炼、附魔系统、全量卡牌/怪物测试仍未全量完成；本批不缩小原goal。
- 提交保留并行素材、共享审计和其他Agent日志，不部署DLL。
