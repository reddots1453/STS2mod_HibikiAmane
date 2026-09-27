# DS27-02B / DS27-06A：三项补充确认

前置提交：`46801cba`。本批仅覆盖三项最新澄清及其完整获取/事件入口，不是全面DesignDoc变更完成报告。状态IMPLEMENTED，游戏内待验；未部署。

## 实现入口

| 条目 | 实现 | 自动运行时探针 |
|---|---|---|
| 娅露丝的书库 | 沿用`InsatiableGreed`存档ID，改为2费先古能力、升级保留；`RegisterDustyTomeCard`注册；`YarusLibraryPower`禁止抽牌，回合开始三选一牌堆，使用局内种子随机自动打出 | 注册、升级、禁抽、三个牌堆各0/1/12张、实际伤害、动态选项标题 |
| 黑暗之源 | `DarkOrigin`先古0费8/12，抽出`DarkElementBase`共享既有变奏/魔力解放实现；黑暗元素注册古老牙齿变化 | 先古元数据、变化名单、-3/-2/0路线及伤害/格挡 |
| 亡灵集会 | `UndeadGathering`注册第2/3幕事件，仅天音队伍；聆听加入2灵魂，祈祷扣当前生命/变化2圣洁牌/堕落-1，学习锁定条件/死灵附魔动画/凡庸/堕落+1 | 显示整数与实际扣血一致、生命上限不变、两牌变化、重复点击不再次扣血、聆听及学习分支 |

祈祷数值为非负最大生命的10%截断整数，描述与结算共用`HpLoss`：60→6、97→9、5→0。没有调用降低最大生命的命令。

书库从选择时的牌堆快照随机取最多10张不同牌；空堆直接完成、不足10张打出现有牌，不因牌打完回到同一牌堆而重复选中。自动打出使用原版命令和局内随机源。三个临时选择牌使用RitsuLib标题贡献接口绑定各自动态变量，不修改共享标题对象。

书库保留旧模型ID，避免删除旧卡存档身份；旧`UnboundedDesirePower`仅留兼容，不再由此牌施加。新Power临时复用现有回想房图标；事件暂用原版`whispering_hollow.png`插画，未生成或修改素材。

## 本轮已运行

在Mod根目录执行：

- `dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false -p:ValidateMod=false`：0警告0错误。
- `python scripts/TestDesignSync20260927.py`：累计14项通过；这是静态契约，不是游戏规则执行证明。
- `./scripts/ValidateCardEffectTests.ps1 -ProjectDir .`：224注册、222可执行、2设计待定，68张二轮数值套件登记通过。
- `./scripts/ValidateMvpContent.ps1 -ProjectDir .`：通过。
- `./scripts/ValidateLocalizationStyle.ps1 -ProjectDir .`：通过。
- `./scripts/ValidateStructuralContracts.ps1 -ProjectDir .`：通过。
- `./scripts/ValidateVisualAssets.ps1 -ProjectDir .`：既有诱惑度UI旧契约失败（第317行，formal lipstick with an independent value label）。未改该UI；不声明全部门禁通过。

## 待游戏内执行

所有命令仅用于一次性测试存档，会更改资源、牌组、生命、遗物，不能用于正常游玩存档。

1. 战斗内分别运行`ms_test_cards confirm InsatiableGreed`、`ms_test_cards confirm DarkOrigin`、`ms_test_cards confirm LibraryPileChoice`，并回归`DarkElement`。确认三选一文本、三牌堆实际自动打出、禁抽与升级。
2. 单人天音、战斗外运行`ms_test_events confirm`；查看日志`[DS27EventTest] PASS`或`FAIL`。该命令移除全部遗物并永久修改测试牌组，不自动恢复。
3. 实际达弗尘封魔典获取、欧洛巴斯古老牙齿替换并保持升级/附魔；百科与奖励不误入普通卡池。
4. 正常遭遇亡灵集会：第2/3幕准入、其他角色不出现、低堕落/无可附魔牌锁定、当前生命不足支付时死亡流程、选择界面/附魔动画。
5. 保存载入、联机选项同步/自动打出、旧ID卡牌兼容、其他角色回归及原版占位图实际加载。

以上游戏内项目未执行，不能由静态检查和编译通过推定成功。其余卡牌、遗物、路线、事件、全面测试仍按DS27审阅清单继续推进。
