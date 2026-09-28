# DS27 批次39：反咒镜与神界星尘

状态：IMPLEMENTED，未游戏内验收，未部署。需求同步前`17284731`，独立文档同步及实施前置`dea193fd`；完成提交由本文件所在范围提交记录。

## 范围与证据

Q10批准完整专属遗物正式纳入；`RELIC-CHAR-003/006`原文数值和标点不变。两模型此前不存在。

- 反咒镜：稀有，实际给予同战斗敌人负面状态时额外1燃烧；堕落≤-3实时改为1断罪。使用原生`GetTypeForAmount`判定变化而非状态总类型，减力量也成立。阻挡/零变化/净化/他人施加/友方/死亡/移除实例排除。按拥有者共享`WeakInstanceScope<Creature>`覆盖await的反应链，同拥有者多个复制品不互相触发，异常自动释放。
- 神界星尘：罕见，原生生成事件按creator归属，实际打出按`CardPlay.Player`归属；共7次获得2增幅。原版Shuriken及CardModel的每次重放循环作为参考，不使用IsLastInSeries。生成后打出分别计数，多层附魔仍是一张牌；普通移堆/仅给已有牌附魔不算生成。
- 出牌前记录附魔资格，完成后消费一次凭据；中途附魔耗尽不漏算。每次CardPlay独立，弱引用不持有整场卡牌；遗物深克隆清空快照，战斗开始/结束清理暂态。保存Progress 0～6并显示角标，不在战斗边界重置进度。
- 正式文本逐字符对照DesignDoc（去掉颜色标签后）；增加状态悬停。遗物契约30→32。无已映射专用图标，显式使用原版circlet占位；无正式风味文本，风味字段暂复用正式效果说明。

## 自动验证

在Mod目录执行，全部构建使用`-p:DeployMod=false`：

| 命令 | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug/Release -p:DeployMod=false --no-restore`（分别执行） | 0警告0错误 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 新增338，累计13540条生产规则断言 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 312通过，新增8 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过；静态合计331 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33通过，仅原生DTO编码，不是实机存读档 |
| `ValidateMvpContent.ps1 -ProjectDir .` | 225卡/32遗物/10附魔通过 |
| `ValidateStructuralContracts.ps1 -ProjectDir .` | 通过 |
| `ValidateLocalizationStyle.ps1 -ProjectDir .` | 通过 |
| `ValidateCardEffectTests.ps1 -ProjectDir .` | 通过 |

仍未通过的既有门：`ValidateVisualAssets.ps1`第317行要求诱惑度正式口红和独立数值；`AuditCardLocalization.py --no-write`报告GagCurse期望1费而源码2费、4项元数据未解析、215文本待审、2 designOnly、2 retiredCompat。本批不改写并行审计报告，不将这些失败包装为全门通过。

## 游戏内自动脚本（仅编译，未执行）

`ms_test_reactive_relics confirm`：Debug、单人本角色、有效战斗、非结束中、明确confirm门禁。**破坏性临时测试**，会删除当前遗物/战斗牌与状态并改生命等，只能在可丢弃测试局使用。

覆盖实际PowerCmd：虚弱首次/叠层、易伤、神器阻挡、负力量、净化/零/他人/友方；全部堕落值文本分支；燃烧根效果追加一次、镜子复制品不互相递归、断罪达到7后原生审判。原生生成事件、实际附魔牌打出、Glam重放、光之翼两附魔层、生成与打出混合计数；0～6全部保存进度、战斗边界保留；耗尽附魔、重复回调和复制品快照隔离；移堆和给已有牌附魔不误计；非本角色及已移除遗物排除。

未完成：上述脚本实际执行、自然战斗/奖励/商店获取、完整退出重进存档、多人归属和同步、图标视觉验收。尚无实机结果，不标VERIFIED。未更改卡牌/怪物/第四层机制，不宣告总目标完成。
