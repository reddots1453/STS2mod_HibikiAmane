# DS27-02G：光之翼多重附魔

2026-09-28；前置快照`34fa3e6b`。DesignDoc两种diff无未同步修改，重读CARD-H-930～949及SYS-ENC-001；沿用全量审阅后已确认的具体卡牌要求，不固化该章节其他OPEN项。代码IMPLEMENTED，游戏内未验收，未部署。

## 实现与边界

光之翼改为稀有、1费、9/12伤害，抽1张附魔牌；正式文案补“可以被多重附魔”，按句换行。

- `LayeredEnchantment`是原版单槽的技术适配器，内部保留真实原版/Mod子附魔，不另写一套简化效果。原版可叠层类型合并Amount；不可叠层的同名附魔保留独立实例，因原版华彩增加Amount不会增加重放次数。异名效果并存。
- 伤害/格挡加法、乘法、重放次数和OnPlay按各子附魔执行。CombatState与RunState的IterateHookListeners仅展开容器，已展开的子项不会二次展开，不增加永久订阅。子实例仍遵守原版拥有者与移出战斗检查；清除容器时清除子Card绑定。
- 原版CanEnchant仅在同步资格检查期间隐藏光之翼已有槽位，Finalizer恢复作用域；无await跨ThreadStatic。卡型、费用、关键词限制原样保留，技能专用附魔不能给光之翼，零费时不能再施加本能。普通卡、其他角色普通牌不放开单槽。
- 原版CardCmd.Enchant返回实际子附魔，维持泛型返回类型及跑局附魔历史。CombatEnchantmentCmd单独处理临时附魔，不写永久牌或附魔历史；仍要求真实战斗实例。新增层只执行自己的ModifyCard，不重复既有费用/关键词修改。
- 旧版单槽光之翼首次追加时迁移原实例；不重置旧华彩使用标志、不重新运行本能减费。DeepClone克隆所有子实例及首触发状态，懒绑定到新卡；反序列化才按原版流程重新执行所有ModifyCard。
- SavedProperty字符串承载原版SerializableEnchantment数组，含子SavedProperties字段。独立生产编码器`LayeredEnchantmentSerialization`使用IncludeFields，避免子字段静默丢失；拒绝无ID/空对象等坏数据，容器拒绝嵌套自己。字符串同时可由原版存档/网络SavedProperties承载；完整网络往返仍未实测。
- 本Mod附魔选择器、相关遗物/共生体事件资格、灵魂联结检索和感染覆盖层识别子附魔。明确“没有附魔才施加”的永恒宝珠与感染传播阻断保持不变，不因多槽擅自取消条件。
- 原版火堆克隆动作直接判断`Enchantment is Clone`，因此另在原动作成功后补充容器内Clone层，每层生成一次复制；先快照原卡，不遍历刚生成复制。普通单槽克隆完全沿用原流程。
- 附魔徽标使用原版华彩图标加实际层数，悬停汇总名称/同名数量并提供各子附魔原版说明；额外卡面文本由各子原文合并。没有生成或改动素材；长文本、图标动画和手柄悬停尚待实机检查。

## 已执行自动检查

以下命令在Mod目录执行，均不部署。

| 命令 | 实际结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 最终0警告0错误 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 90项通过，新增15项光之翼结构/接线检查 |
| `python scripts/TestCardLocalizationAudit.py` | 19项通过，静态合计109项 |
| `dotnet run --project tests/DesignSyncContracts/DesignSyncContracts.csproj --no-restore` | 既有290条生产规则断言通过 |
| `dotnet run --project tests/LayeredSaveContracts/LayeredSaveContracts.csproj` | 新增33条生产编码器断言通过；本地游戏DLL的真实DTO，无游戏引擎执行 |
| `ValidateMvpContent.ps1 -ProjectDir .` | 225卡、28遗物、10附魔（含技术容器）通过 |
| `ValidateStructuralContracts.ps1 -ProjectDir .` | 通过 |
| `ValidateLocalizationStyle.ps1 -ProjectDir .` | 通过；修正本批新增句子未换行后复跑 |
| `ValidateCardEffectTests.ps1 -ProjectDir .` | 225登记、223可执行、2设计待定，通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有317行诱惑度图标断言失败，未修改/放宽 |
| `python scripts/AuditCardLocalization.py --no-write` | 3明确差异、4未知元数据、217待渲染、2仅设计项；不写共享报告 |

33条编码断言直接链接生产源码，使用本地sts2.dll/GodotSharp.dll的ModelId/SerializableEnchantment/SavedProperties；验证顺序、重复同名、数值、所有字段类型（含数组和嵌套卡）、Unicode/转义、反序列化对象独立、稳定重编码及坏数据拒绝。进程提示Sentry扩展未加载是非游戏进程提示，并非引擎启动。此测试不等于ModelDb还原、联机或实际出牌已验收。

初次构建修正了与当前游戏DLL不同的格挡修正签名、历史类型命名空间和火堆Owner访问；补充测试后出现的一条可空警告已改为明确失败断言，最终零警告。

## 游戏内自动入口与待验收

`ms_test_cards confirm LightWings`：基础/升级两场景。已编译，**本轮未运行**。仅用于一次性单人测试存档，会重置战斗与资源，并增加测试永久牌，不用于正式跑局。

场景覆盖：

- 真实CardCmd/CombatEnchantmentCmd附魔、AutoPlay；锋利2+3、伶俐3、充能2、迅捷1、华彩两层共7实例；首次真实伤害42/51、格挡9、充能一次、迅捷一次，后续14/17且华彩耗尽。
- 原版跑局与战斗钩子中的每个子实例仅出现一次；沉眠精华两层实际回合末5→3→1费、打出恢复5；腐化两层乘算造成20/27并两次付生命。
- 深克隆不共享子实例、保持已消耗首触发状态；原版LoadCard还原7层/升级/数值；子Wrath的SavedProperty经无Owner加载保留；清除层解除Card绑定。
- 本能追加其他附魔不重复减费，旧版单槽实例追加后身份保留、费用不重复；技能/防御专用附魔、零费本能拒绝，普通牌仍单槽。
- 灵魂联结查找容器内子项，锻成·伶俐可选已有附魔光之翼；战斗修改不传永久DeckVersion；原版Clone火堆动作补充两层Clone生成两份，非容器仍走原版。

仍待：真实游戏入口运行，自然抽牌/回合末/火堆/事件流程，升级/预览和附魔动画布局，存档退出重进，联网同步与断线恢复；不同Mod附魔覆写不调用原版CanEnchant等非标准行为不在本轮保证范围。

全量goal仍有其他卡牌逐字/排版与机制、事件/遗物、第四层试炼及其他测试工作。本批只关闭光之翼实现子任务，不以构建/结构检查代替全卡或游戏内验收。并行素材、共享审计报告与其他Agent日志修改不纳入本批提交。
