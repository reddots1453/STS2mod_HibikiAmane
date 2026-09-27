# DS27-02A：首批确认项

前置提交：`9935958b`。完整范围仍以`DESIGN_SYNC_20260927_REVIEW.md`为准；本文件不是全面变更完成报告。

## 已实现

| 条目 | 行为 | 自动运行时断言 |
|---|---|---|
| 瘴气转化 | 费用1/0；消耗手牌攻击/诅咒，但自身不消耗 | 费用、目标牌堆、增幅层数、自身进入弃牌堆 |
| 魂弗妮卡 | 战斗中可跳过；仅选择成功才登记所选牌，战后自动入牌组 | 选择牌身份/升级、强制奖励、重复结束幂等、跳过无手牌或牌组增加 |
| 使魔契约 | 升级版生成升级后的圣洁牌，再附魔使魔 | X=2数量、基础/升级状态、附魔及自动打出 |
| 回想房 | 额外抽1；先取消耗牌，不足正常补足；升级固有 | 消耗堆0/1/2/6/7张、余量、Drawn通知、回合内抽牌不受影响、满手牌保留剩余消耗牌 |

回想房分开使用`ModifyHandDraw`与`ModifyHandDrawLate`，避免1张消耗牌恰好抵消额外抽牌时，原版Hook漏登记监听器。回收牌记录抽牌历史并调用原版抽牌后钩子/Drawn事件；尊重抽牌许可与10张手牌上限。

## 验证记录

- `dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false -p:ValidateMod=false`：通过，0警告0错误。
- `python scripts/TestDesignSync20260927.py`：7项通过。这是源码/元数据/逐字文案静态契约，不是行为执行结果。
- `ValidateCardEffectTests.ps1`：通过；现有222注册/220可执行/2设计待定不代表本轮全卡变化已实现。
- `ValidateStructuralContracts.ps1`、`ValidateLocalizationStyle.ps1`、`ValidateMvpContent.ps1`：通过。结构门中魂弗妮卡旧文案断言已按DesignDoc“那张牌”更新。
- `ValidateVisualAssets.ps1`：失败于既有诱惑度UI契约（formal lipstick with an independent value label）。本批不修改相关UI；不宣称全部验证门通过。
- 游戏内行为、选择界面、保存载入、多人、视觉：未执行；未部署DLL。

四张牌可在**一次性测试存档**的战斗中逐张运行：`ms_test_cards confirm MiasmaConversion`、`SoulFuenika`、`FamiliarContract`、`RecollectionRoom`（后三者同样加命令前缀）。测试会清空当前战斗资源、移除遗物及重设敌人状态，不得用于正常游玩存档。

## 文档同步与剩余项

本批统一耐久归零/再次损失、0层无减伤、对应外观与失去资源收益的说明；明确回想房补足、魂弗妮卡跳过、选角遗物切换按钮、灵魂罗盘百分点及生效区间、路线碎片可见和献祭解锁条件。耐久、遗物、路线和选角代码尚未修改。

仍需用户确认：无名先古牌名称、黑暗之源基础/先古稀有度、亡灵集会10%最大生命值代价含义。

其余卡牌、路线、遗物、事件及全量测试尚未完成；相关问题的设计确认不等于已实现。性侵犯或露骨性行为机制及演出不在本次实现范围内，不声称原总目标全部达成。
