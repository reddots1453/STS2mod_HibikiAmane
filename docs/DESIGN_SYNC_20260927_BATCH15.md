# DS27-02J：炎之剑五场战斗成长

日期2026-09-28；前置快照`8350c0d8`。DesignDoc两种diff均为空，按CARD-N附魔成长正式条目实施。上一目标回合属于进展：已提交退役获取清理`8350c0d8`；本批继续修复另一项真实设计差异。

## 差异与实现

旧实现和旧测试均以“打出6次后附魔”为准，且剩余次数只在战斗内显示。当前DesignDoc明确要求“完成5场战斗后”，附魔永久，并在完成后隐藏剩余括号。

`FlameSword`改为：

- 9/12伤害、1费、罕见和目标类型不变；OnPlay只执行伤害。
- 永久牌组实例在原生AfterCombatVictory推进`CompletedCombats`；不依赖抽到或打出，战斗克隆/临时生成/已移除实例不向牌组计数。原生跑局钩子枚举永久牌组，因此不另建全局监听器。
- 场数0～5单独保存；旧`TimesPlayed`仅保留反序列化兼容，不将旧出牌次数误算作完成战斗。旧存档已赚取的余烬不删除。
- 弱引用同一CombatRoom去重，避免重复回调，又不长期持有整个旧战斗对象。深克隆清空这一非持久化标记；原生已完成房间恢复不重新派发胜利流程，仍需实机回归。
- 第五场对永久实例使用原版CardCmd.Enchant，获得真实0费和永恒；后续战斗从该牌克隆。只向正确本地玩家显示现有附魔预览，不重复创建战斗克隆演出。
- 普通卡仍受单附魔槽约束：不覆盖已有附魔、不在战斗结束抛异常；场数可以完成并保留，槽位释放后下次胜利再尝试授予。未授予时仍显示剩余0场，不谎报已经附魔。
- 卡面改正式句号与场数，采用现有每句换行规则；战斗内外都有具体剩余整数，成功附魔后隐藏括号。特殊附魔悬停沿用原版。

不改DesignDoc、不动并行素材和共享审计报告、不部署。并列检查发现风神披风文字仍写“0费牌”而非“耗能为0”，已留待后续连同实际支付/重放边界核对，本批没有将其标为完成。

## 验证结果

| 命令 | 结果 |
| --- | --- |
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告0错误 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 120项通过，含本批8项 |
| `python scripts/TestCardLocalizationAudit.py` | 19项通过；静态累计139项 |
| `dotnet run --project tests/DesignSyncContracts/DesignSyncContracts.csproj --no-restore` | 305条既有生产规则断言通过 |
| `dotnet run --project tests/LayeredSaveContracts/LayeredSaveContracts.csproj --no-restore` | 33条既有编码断言通过，不代表游戏引擎执行 |
| `ValidateMvpContent.ps1` / `ValidateStructuralContracts.ps1` / `ValidateLocalizationStyle.ps1` / `ValidateCardEffectTests.ps1`（均`-ProjectDir .`） | 四门通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有第317行诱惑度UI断言失败，未放宽 |
| `python scripts/AuditCardLocalization.py --no-write` | 225注册；1费用差异（GagCurse）、4未知元数据、215待逐字渲染、2仅设计、2退役兼容；不宣告全量通过 |

本地化初次检查因新条件段前缺少分句换行失败；按项目既有格式修正并复验通过，没有降低门禁。

## 游戏内测试与待验收

一次性单人天音战斗中运行`ms_test_cards confirm FlameSword`（破坏性测试，沿用已有统一测试框架）。基础/升级各一场景，至少35条行为断言要求，实际包含：

- 六次真实出牌只伤害、不成长；永久牌是原生钩子监听者。
- 分别调用五个不同房间令牌的真实胜利钩子、同令牌重复调用、战斗副本先收到钩子；只在第五次永久附魔。
- 每阶段真实保存/反序列化；3场存档恢复为新永久牌后继续到第4场，原实例不受影响。
- 永久余烬费用、永恒及后续克隆；旧六次计数字段不被误迁移，旧余烬保留。
- 移除的未完成牌不计数；临时生成不计数；已有锋利不覆盖；释放槽后授予；非法进度边界。
- 原生描述格式化的基本/升级、战斗内外、各剩余场次和隐藏后缀。

这些入口**已编译未执行**。手测仍需自然完成五场战斗（包含未打出/封印场景）、第五场附魔动画/奖励界面、真实保存退出/重进、多人本地演出隔离和长文本大图排版。人工构造房间令牌调用钩子不能证明自然战斗调度或场景切换已通过。

全面同步目标继续保持未完成；本批只是关闭炎之剑的已确认差异，不缩小卡牌/怪物/第四层/事件总范围。
