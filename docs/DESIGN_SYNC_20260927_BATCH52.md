# DS27 批次52：战术分析仪升级附魔候选

前置`024a9767`，Plan同步`6e9b7416`，完成提交为本文件所在独立范围提交。DS27-02Y IMPLEMENTED，游戏内待验，不标VERIFIED，不部署。DesignDoc逐行/词级无漂移；完整复核战术分析仪、SYS-ENC-001和光之翼多重附魔例外。

## 原因和修复

`TacticalAnalyzer.OnPlay`原候选只检查IsUpgradable，普通已有附魔牌仍可被选中；先升级，再调用CombatEnchantmentCmd.ApplyVanilla<Steady>时，普通牌附魔槽冲突会抛异常。现有测试只用无附魔打击，没有覆盖这条路径。

统一`CanUpgradeAndEnchant`检查当前拥有者、当前手牌、可升级及原版Steady.CanEnchant。此规则与DesignDoc“临时附魔逻辑完全与普通附魔一致”相符，亦与同Mod其他选择后附魔卡牌的候选约束保持一致。

- 抽1/2牌仍最先执行。
- 普通已有附魔的牌不再被提供；不会覆盖旧附魔。
- 使用原版CanEnchant，而非自行写死“附魔为空”，保留已实现的光之翼多重附魔适配。
- 选择返回再次检查位置/归属/升级/附魔资格；等待期间失效的目标不再被改写。
- 没有合法目标时沿用原版FromHand空集合返回，完成抽牌并正常结算，不创建额外规则或弹窗。
- 不改费用、稀有度、抽牌数、升级收益、描述或永久牌组。

## 自动测试入口

`ms_test_cards confirm TacticalAnalyzer`沿用原有单卡入口与破坏性战斗夹具。`DesignSyncTacticalAnalyzerContract.Run`替换原3断言测试，基础/升级每变体最低25效果断言；原圣洁全文契约仍在Runner中执行。

测试不是直接调用OnPlay：经CardCmd.AutoPlay和原生CardSelectCmd，使用ICardSelector观察**真正提供的候选集合**。观察选择器只返回牌，不替生产代码实施升级/附魔。

覆盖：

1. 抽1/2牌后选中刚抽到的牌；实际候选恰为无附魔可升级牌，排除已升级、已有Sharp、已有Steady牌；只选中牌升级并获得Steady/保留。
2. 永久牌组卡及其实际Combat.CloneCard+DeckVersion：战斗副本升级附魔，永久原件两者均不改变；finally移除测试牌组条目。
3. 抽到的牌都已升级且其他手牌已附魔：不调用选择器，抽牌不丢失，原牌和来源牌结算正常。
4. 已有Sharp的光之翼仍可选择，升级后恰有原Sharp和新Steady两层，保留生效。
5. 选择器开放后异步使目标新增附魔、升级或移出手牌，再返回该实例：重验拒绝本次升级/稳定，保留外部改动，来源正常进入弃牌堆。

新增7项Python检查覆盖生产双重资格守卫、原文与富文本不变、独立真实候选/收益断言接线、DeckVersion隔离、多重附魔例外、异步变化及原目录注册。属于静态检查，不是引擎执行。

## 已执行验证

| 命令（Mod目录） | 结果 |
|---|---|
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 416通过，新增7 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，累计435静态 |
| `dotnet build MaidenSuccubus.csproj -c Release -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Debug` | 0警告0错误，最终保留Debug |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540纯规则断言通过 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，非引擎执行 |
| MvpContent/StructuralContracts/LocalizationStyle/CardEffectTests（各`-ProjectDir .`） | 四门通过；升级绑定225注册/149直接/6间接/0失败 |

既有失败未消除：VisualAssets第317行诱惑度图标/独立数字；全卡审计`--no-write`的GagCurse费用1/2差异、4未知元数据、215文本待审、2 designOnly与2 retiredCompat。未改写并行报告。

未完成实机验收：上述两变体脚本仅编译，尚未执行；自然手动选择界面、动画、多人同步和真实存读档仍待验。未启动游戏、未部署DLL、未改其他卡牌规则或并行素材。总目标仍未完成。
