# DS27-02K：风神披风实际耗能与首次触发

日期：2026-09-28。前置快照：`358710b2`。DesignDoc逐行/词级diff为空；依据CARD-N-300～399正式卡条目。上一目标回合已实际提交炎之剑回修，属于进展；本批继续关闭正式差异，不缩减全面目标。

## 核对证据与改动

原版`ResourceInfo`区分EnergySpent和EnergyValue：自动打出实际支付0，但效果计算仍可使用正费用或X值。原实现检查EnergyValue，因此漏掉本应符合“耗能为0”的免费自动出牌。卡面也仍是旧“0费牌”。

- 读取EnergySpent。手动1费不触发，实际减至0和免费自动打出可触发；X按实际支付，不按效果X值判断。
- `BeforeCardPlayed`只在首个序列开始预留本回合资格和Amount快照，`AfterCardPlayed`最后一段执行后生成。旋涡等嵌套代打不能抢走外层已开始的第一次触发。
- 新披风是在OnPlay中施加，因未经历该牌序列的Before预留，不会追溯复制自己；不再使用只忽略第一次回调的_activationCardToIgnore，消除华彩重放激活牌时的漏洞。
- 多层按触发时Amount复制；激活另一张披风新增的层数不追溯加入该次复制。复制继续使用原生CloneCard与AddGeneratedCardToCombat，清除DeckVersion，保留深克隆附魔和满手溢出处理。
- 同一实例重入单独记录序列深度，原生重放迭代不重复预留。生成前清理临时引用，生成中战斗结束或CombatState切换则停止。自身玩家回合开始才重置，敌方和其他玩家额外回合不重置。
- CopiedThisTurn沿用原SavedProperty。深克隆保持该已用标志，但不复制进行中的调用栈/卡引用。未新增持久化模型或修改原版角色卡。
- 卡面与普通Power说明使用当前DesignDoc完整句号文本，smartDescription显示当前层数对应的复制数量。

## 自动验证

| 命令 | 结果 |
| --- | --- |
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告0错误，未部署 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 128项通过，含本批8项 |
| `python scripts/TestCardLocalizationAudit.py` | 19项通过；静态总计147项 |
| `dotnet run --project tests/DesignSyncContracts/DesignSyncContracts.csproj --no-restore` | 305条既有生产规则断言通过 |
| `dotnet run --project tests/LayeredSaveContracts/LayeredSaveContracts.csproj --no-restore` | 33条既有编码断言通过，不是引擎执行 |
| `ValidateMvpContent.ps1` / `ValidateStructuralContracts.ps1` / `ValidateLocalizationStyle.ps1` / `ValidateCardEffectTests.ps1`（均`-ProjectDir .`） | 四门通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有第317行诱惑度UI断言失败，未修改或放宽 |
| `python scripts/AuditCardLocalization.py --no-write` | 225注册、1既有GagCurse费用差异、4未知元数据、215待逐字渲染、2仅设计、2退役兼容 |

静态测试只证明目标代码接线/文本和回归入口存在，不能替代实际战斗；未覆盖或覆写并行审计报告和美术改动。

## 游戏内测试入口与未完成验收

在一次性单人天音测试战斗运行`ms_test_cards confirm WindGodCloak`。沿用破坏性统一框架，基础/升级各一场景，至少25条行为断言要求，覆盖：

- 实际SpendResources与手动OnPlayWrapper；正耗能不触发，减费到0触发；复制再次打出不递归触发。
- 原生AutoPlay正费用牌，X费手动0/3、自动X正效果值零支付。
- 华彩真实重放激活披风，两个层数但不复制激活牌；下张牌复制两份；已有披风加层时只用先前层数。
- 黑色旋涡真实嵌套打出两张牌，外层保留首次复制资格。
- 满手沿用原生弃牌堆溢出且仍消耗资格；附魔独立复制、永久链接清除。
- 敌方/非自身回合不重置、自身回合重置；原生SavedProperties往返已使用标志。
- 同一实例递归以合成Before/After顺序探针验证深度，Power克隆不携带进行中的复制，重复After不能重发。此项明确不是自然递归卡牌实测。

入口**已编译未执行**。仍须实机验证自然回合、各类免费效果、X费/付费动画、复制排版、多人各自回合与临时附魔隔离、保存退出后的状态。SavedProperties字段往返不等同于完整存档恢复，也没有声称支持在任意未完成卡牌选择中断点存档。

全部卡牌描述、其余机制/遗物/事件/第四层及游戏内自动脚本仍有未完成项，目标保持进行中。
