# DS27 批次40：冰界的女神

前置快照`5bb83c8b`；需求/Plan同步`83b1a202`；完成提交为本文件所在独立范围提交。状态IMPLEMENTED，未游戏验收，未部署。DesignDoc逐行与词级无新增漂移；只修正完整条目的遗漏，不改变设计数值。

## 发现与修正

DesignDoc：1费、稀有能力，每当打出附魔牌生成1张冰晶碎片，升级生成冰晶碎片+。现有Power以IsLastInSeries过滤，导致原生Glam等重放一整组只产生1张；升级后的Power文本也错误显示普通碎片。

- 每次CardPlay按实际Player及同战斗归属记录资格，完成后消费一次；没有完成回调不生成。对附魔的检测发生在出牌前，附魔在OnPlay中耗尽不会取消当前次资格。
- 多个附魔层仍是一张牌，重放各次分别计数；同一CardPlay重复回调不重复生成。同一牌实例之后再次打出重新记录资格。
- 记录使用实例独立弱引用表，Power移除、深克隆清空记录，已移除状态不继续反应。
- 女神自身若有附魔，新Power在BeforeCardPlayed阶段还不存在，由自身OnPlay在实际成功施加后补登记当前次；已有状态用GetValue保留原记录，避免覆盖/重复计数。
- 保留Single堆叠和Amount>=2表示升级版本的既有实现，不另增层数机制。继续用原生AddGeneratedCardToCombat，满手走原生溢出弃牌，不静默丢牌。
- 普通/升级Power的Description、SmartDescription、碎片预览统一；碎片名称着色。卡牌正式描述本已符合，未改写。

## 回归脚本

`ms_test_cards confirm GoddessOfIce`使用现有单人、角色、confirm门禁，**仅可在可丢弃测试战斗运行**：会清除当前战斗卡牌、状态和遗物等。原目录条目改接`DesignSyncIceGoddessContract.Run`，基础/升级两个场景，每场至少20条效果断言；不是新增平行的未接入脚本。

包含：元数据与基础/升级完整卡面、Run实例和战斗实例、Power和衍生悬停；普通牌不触发；真正附魔牌打出；Glam实际重放生成2张，之后同实例再次打出1张；光之翼双层附魔+重放只生成2张；附魔耗尽后仍生成、重复完成回调只一次；非实际拥有者、未见开始的完成回调排除；Power移除及克隆记录隔离；自身附魔女神生成1张；满手10张时新碎片进入弃牌堆，升级等级保留。

**脚本已编译未执行**。不得据此声称实际重放/动画/联网或存档通过。

## 验证结果

所有命令在Mod目录执行。

| 命令 | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Release` | 0警告0错误 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 319通过，新增7 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，合计338 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540生产断言通过，本批未宣称新增纯逻辑断言 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过 |
| `ValidateMvpContent.ps1 / ValidateStructuralContracts.ps1 / ValidateLocalizationStyle.ps1 / ValidateCardEffectTests.ps1`（分别`-ProjectDir .`） | 全部通过 |

既有失败仍在：VisualAssets第317行诱惑度口红/独立数字契约；全卡审计GagCurse费用1/2不符、4未知元数据、215文本待审、2 designOnly、2 retiredCompat。审计仅`--no-write`，保留其他Agent报告修改。

仍待：上述游戏脚本实际执行、自然玩家手动出牌/重放时序、多人归属、完整存读档、Power悬停截图。未部署，不标VERIFIED，总目标继续。

## 咒符范围待定

本轮只读发现“东尼的咒符”随机稀有牌未说明三路线卡池范围。角色原生CardPool仅中立，而项目另有三路线合并服务；已向用户提出Q18（三路线等概率／仅中立／按堕落路线概率）。对应DS27-04G保留OPEN，不先实现任一种产品选择。永久删牌选择与原生钩子已查证，未修改该遗物。
