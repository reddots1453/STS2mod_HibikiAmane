# DS27第38批：祈祷耳环与结界生成装置

## 版本与范围

- 批次前置`e0cb64f2`，DesignDoc行级/词级diff无漂移；依据用户Q10，单独正式化完整的`RELIC-CHAR-001/004`，没有改变规则原文。文档/Plan/追踪同步提交`16bf64c9`为实施前置。
- 当前IMPLEMENTED，游戏内未验；未部署、未运行/停止游戏。并行资产、角色代码、审计报告及旧日志差异保留，不混入提交。
- 两个新增模型为普通祈祷耳环和稀有结界生成装置。其他专属遗物不因本批标为完成。

## 实现入口

- `src/Relics/MagicSupportRelics.cs`：PrayerEarrings、BarrierGenerator注册到MSRelicPool，角色/持有者/存活/移除检查；原版circlet临时图标，不依赖其他mod。
- `src/Core/Relics/MagicSupportRelicRules.cs`：耳环≤-2阈值、新变身层数变化判断和装置五回合周期；测试直接编译该生产代码。
- 耳环BeforeCombatStart加1层魔力增幅；AfterPowerAmountChanged仅识别本人三种形态实际从0到正数。永恒初始9层只触发一次，已有形态叠层、同形态Enter短路、耐久增减、退出及0施加均不产生额外奖励。使用原生await链，不订阅UI事件或丢弃异步任务。
- 耳环描述在既有Safe描述补丁中按当前Run阈值切换；所有标点、数值和比较符与DesignDoc一致。变奏后原文没有返回阈值句，因此未擅自添加。
- 装置保存TurnsSeen，显示0～4角标，第4步高亮；战斗结束只清理高亮，不清空进度，遵循原版HappyFlower同类计回合遗物的跨战斗语境。
- 装置在AfterSideTurnStartLate且本人确实参与该回合时递进，敌方/其他玩家/其他战斗不计数；每第5回合施加1圣域。不要改成AfterPlayerTurnStart：当前CombatManager会先启动SetupPlayerTurn，随后才分发AfterSideTurnStart。当前安装sts2.dll的Hook已通过ILSpy只读核验：普通回调全部完成后才遍历Late回调，保证旧圣域扣层在本次授予之前。
- 30个遗物精确注册契约同步，既有两处总数断言28→30，不移除旧模型；状态悬停使用原生HoverTipFactory。独立风味文本缺失时复用正式文本占位，不新编机制或背景故事。

## 已执行验证

从Mod目录运行，构建始终`DeployMod=false`：

| 验证 | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同上Release，随后Debug | 0警告0错误 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 304项通过，新增8项 |
| `python scripts/TestCardLocalizationAudit.py` | 19项通过，静态合计323 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 13202断言通过，新增317 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33生产编码断言通过，非游戏执行 |
| `ValidateMvpContent.ps1 -ProjectDir .` | 225卡、30遗物、10附魔注册契约通过 |
| `ValidateStructuralContracts.ps1 -ProjectDir .` | 通过 |
| `ValidateLocalizationStyle.ps1 -ProjectDir .` | 通过 |
| `ValidateCardEffectTests.ps1 -ProjectDir .` | 223可执行卡、2设计待定，通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有317行诱惑度口红/独立数值标签契约失败；本批未改UI |
| `python scripts/AuditCardLocalization.py --no-write` | 原有GagCurse费用1项、元数据4项、文本待核215项、设计独有2项、退役兼容2项；未写共享报告 |

新增纯规则断言覆盖11档堕落、两个所有者匹配状态、形态新施加1/9与重复叠层/零/负变化、五种已保存计数分别连续30步及非法计数无溢出。它们不能替代实际Godot游戏中的Hook、UI或保存验证。

## 游戏内脚本：编译通过，未执行

`ms_test_magic_relics confirm`仅Debug可用，要求一次性单人响木天音战斗及明确confirm。会删除遗物、清理战斗牌/状态、改变生命、资源和变身，不恢复原局；finally只恢复TestMode与执行标志，严禁在需要保留的局运行。

- 11档堕落：战斗开始实际Power授予、三种TransformationCmd切换、同形态重复、护甲变化、已有形态再叠层、退出。
- 格式化后的两分支文本与状态悬停存在性。
- 零值施加、其他目标形态、其他角色持有及移除后的隔离。
- 装置从5种初始计数分别通过实际Hook.AfterSideTurnStart执行10轮；检查每第5回合新圣域保留、本人的下一回合到期，以及可见角标。
- 战斗结束/新战斗不重置进度；原生RelicModel.ToSerializable/FromSerializable重建后不调用拾起回调即可延续效果。
- 使用原生Power命令，不用手写假效果替代游戏结算；模型级序列化不等于完整保存退出/恢复已验收。

## 待手测

自然战斗首回合及第5回合（含额外回合）、同侧多人轮次、形态转换动画与实际悬停、随机奖励/商店/百科、跨战斗与完整存读档。专属图标仍未接入。其他遗物与总目标剩余项继续追踪，不因本批静态通过标VERIFIED。
