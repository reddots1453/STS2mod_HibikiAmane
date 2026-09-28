# DS27批次36：正式领奖与末战领取时序

2026-09-28；前置`46e636b1`；ACT4-001→DS27-05O→DS27-ACT4/COMPAT。
单人IMPLEMENTED，游戏内未验，未部署，总目标未完成。

## 原因与证据

原入口只有打开地图后的自定义确认框。第三层最后一战后不一定再次打开地图，已完成第三试炼可能无处领取。当前DLL `CombatManager.EndCombatInternal(CombatTurnState)`依次await全部`Hook.AfterCombatVictory`，再保存、发CombatWon并推进原版流程。本批包装Hook返回的完整Task，在原任务完成后处理领奖；不重复运行原函数，不用异步Postfix开始时机冒充完成时机。

只读核对安装版PCK：旧版`shared_relic_picking_screen`路径不存在，现有独立资源为`res://scenes/ui/treasure_relic_holder.tscn`。其136×136原版holder、2倍内部遗物图标和四个命名节点/直接依赖均经二进制索引读取核验。未复制或改写游戏资源，未启动引擎；该核验不证明实际渲染通过。

## 实现

- `FourthRouteRewardScreen`：全屏独立模态，标题“试炼的奖赏”，显示本次完成条件、准确的残缺/完整/觉醒遗物名与无Owner预览效果。原生holder负责鼠标/手柄焦点、点击缩放和悬停。无Owner预览不会附加碎片、献祭或后续试炼提示。
- 不初始化原版共享宝箱collection，不修改其分配器，不把预览遗物交给宝箱Obtain。单击关闭并隐藏本模态，再由`FourthRouteRewardFlow`走既有`ClaimInitialReward`，只发放一次真正的阶段遗物；成功后调用原版遗物栏飞入动画。附魔/删牌/卡牌奖励等拾起选择不嵌在阻挡它们的奖励模态下。
- `FourthRouteRewardOffer`：不可变路线＋待领阶段快照，只有FirstReward/SecondReward/ThirdReward合法，阶段映射1/2/4。确认前再核对当前路线和阶段，旧页面不能领取下一项或另一条路线。
- `FourthRouteRewardFlow`：每局共用展示锁，复用原阶段领取锁及收据；确认前死亡、换房、换局和页面销毁均取消展示，不消费奖励。地图原来可旅行才暂禁用并按原状态恢复。异常保留已达到的保存状态，不尝试回滚已发生的原生拾起副作用。
- `FourthRouteVictoryRewardPatch`：先await原胜利Task；原任务异常仍向原调用方传播，不领奖。只对单人本地响木天音、非测试/回放、活着且仍在原战斗房间处理。设置、卡牌/遗物查看、既有选择界面等暂时占用时等待空闲，战后此时只绕过动作执行器忙/暂停检查，不绕过其他安全条件。自身UI错误记录后不阻止原版保存和结局。
- `FourthRouteRewardWatcher`：附在NRun下，0.15秒检查一次非战斗空闲状态，处理金币、移牌、升级、休息、锻造、碎片解锁即达标等来源，不在正在执行的动作或选择器中嵌套领奖。同一房间同一奖励展示失败不无限重试；地图入口仍可重试，换房可恢复。涅奥开场未完成时不抢在叙事前弹奖励，但开场降级遗留字段不会阻断以后战斗结束。
- 地图单人入口改共用新流程；多人保留原既有流程，本批不解决尚未明确的多人试炼所有权/供奉规则。第四层仍空注册，不启用未完成的战斗或改写建筑师结局。

## 自动测试

- 生产纯规则新增5736断言：14路线×9阶段的合法性/形态/陈旧快照拒绝、JSON值往返，以及4096种显示门禁组合。胜利边界只允许绕过执行器忙，其他隔离条件仍必须满足。累计12753通过。
- `TestDesignSyncReward20260927.py`新增8项：原版PCK中的实际场景及直接依赖、正确原生holder、无共享宝箱命令、只显示本阶段、先关闭再拾起、完整原Task等待、空闲入口、退出/地图恢复和显式调试注册。设计同步288＋审计19，累计307通过。
- `ms_test_route_reward confirm`仅Debug、非战斗、可丢弃单人本角色。清空遗物和牌组、重置路线、增加基础测试牌，改变金币/堕落/药水/生命上限等；**不恢复这些游戏数据**，只finally恢复TestMode和执行锁。
- 该脚本覆盖14×3真实领取服务与原生Obtain/Remove：旧形态移除、第二奖实际碎片移除、每段仅一次±1变化、仅第三奖取得资格且不进入未完成第四层、其他角色/陈旧页面拒绝。用TestCardSelector驱动拾起选择；额外挂起慷慨原生删牌选择后重入领取，断言不会多开选择器或重复改变堕落。检查真实Harmony登记、原Task等待和失败传播。
- **游戏内脚本已编译未运行**。源代码/静态检查、纯规则断言和PCK读取得到的证据不等于42次完整游戏流程或原生界面已实测。

## 验证命令与结果

在Mod目录执行：

| 命令 | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Release -p:DeployMod=false --no-restore`，随后Debug | 均0警告0错误，最终保留Debug |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 12753生产规则断言通过 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 288通过，包含8新增 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33既有编码断言通过 |
| MVP/StructuralContracts/LocalizationStyle/CardEffectTests四PS1，`-ProjectDir .` | 通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有317诱惑度独立数值标签失败，未修改门禁 |
| `python scripts/AuditCardLocalization.py --no-write` | 225张；既有GagCurse费用差异、4未知、215待文本、2仅设计、2退役兼容；未写共享报告 |

## 游戏内仍待验证

1. 三段奖励界面在各分辨率/UI缩放下的原版图标、滚动文字、鼠标及手柄焦点、悬停和飞入遗物栏动画。
2. 原生战后时序，特别第三层最后一战达标：领奖/拾起交互完成后原版奖励及建筑师结局继续，无卡死或重复结算。
3. 金币/删牌/休息/锻造/碎片购买达标后，在既有选择完成后自然显示，设置/查看界面关闭后继续。
4. 慷慨、愤怒、勤勉等实际拾起选择界面没有模态遮挡；领取期间退出、重新载入和旧档待领奖恢复。
5. 其他角色、回放和多人不启用新流程；多人正式设计与慷慨供奉仍是未完成项。

未启动或关闭游戏，未部署；并行素材、共享审计和CHANGELOG其他批次改动不纳入本提交。
