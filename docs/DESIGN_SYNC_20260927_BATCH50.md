# DS27 批次50：圣言生命周期与共鸣

前置`39dfee48`，Plan同步`a1f3dc10`，完成提交为本文件所在独立范围提交。DS27-02X IMPLEMENTED，不标VERIFIED、不部署。DesignDoc逐行/词级无漂移，完整复核六种圣言、其生成体系和圣光共鸣说明。本批只实现已明确部分，不固化该章节仍OPEN的其他设计。

## 生产修复

- `ScripturePowerTemplate.AfterApplied`：轻灵圣言立即获得2敏捷，但不发布圣言触发事件。
- `AfterSideTurnEnd`：轻灵在所属方回合末发布事件，然后持续回合减1；最后一次圣光共鸣结算时临时敏捷仍在，随后到期移除自身给予的2敏捷。不会每回合重复施加敏捷。
- 六种圣言均检查剩余回合、存活、仍持有该实例以及正确玩家/方/参与者。被移除但仍有正数Amount的引用、已到期实例不再触发或推进计时。事件监听造成实例移除后不再修改已脱离状态栏的实例。
- 惩戒圣言描述“随机1名敌人”对齐DesignDoc“随机敌人”。其他五种收益、数值和独立图标映射不改。

## 测试入口与边界

`ms_test_cards confirm ds27-scriptures`选择六牌、基础/升级12变体，仍经过既有225卡目录和原生出牌命令。替换旧聚合测试，最低效果断言提高到每变体20。

`DesignSyncScriptureContract`独立写出期望全文，验证真正RunState牌组实例及战斗手牌实例；场上先有1敏捷和2层圣光共鸣，逐回合验证实际资源差额、事件唯一性、事件所属实例及减回合前的Amount。

| 项目 | 明确期望 |
|---|---|
| 守护 | 每次3+1格挡，加共鸣2+1，共7；Power动态描述显示4格挡 |
| 轻灵 | 施加即敏捷+2且不触发共鸣；每次回合末共鸣2+3，共5；到期敏捷恢复原有1 |
| 惩戒 | 每次随机敌人共获得1断罪；共鸣3格挡 |
| 睿智 | 每次抽1牌；共鸣3格挡 |
| 活力 | 每次获得1能量；共鸣3格挡 |
| 极乐 | 从2欲望开始，每次先减1；第二次起因归零获得1能量并抽1牌；共鸣3格挡 |

每次检查错玩家、错方、错参与者、错阶段无变化；最后一次效果先于到期；剩余持续回合参与Buff层数。额外检查2/3回合轻灵并存、独立到期、提前移除只清理自身2敏捷、已移除正Amount引用不触发。事件观察订阅在finally释放。

**游戏内命令未执行。**它使用真实卡牌/命令，但生命周期回调由测试手动调用，不是自然引擎完整回合；不能据此声称已验证自然回合、网络同步或真实存读档。命令会使用破坏性战斗夹具，须在专用测试局明确确认后运行。

8项Python静态测试覆盖设计时点、生命周期守卫、六牌原始模板/独立全文、触发配置、临时敏捷清理、动态格挡、六独立图标、实际脚本和入口接线。结构门最初仍引用已替换的旧守护测试，已迁移到新脚本的实际格挡收益和动态预览断言；没有删除测试要求或以方法存在代替效果验证。

## 已执行验证

| 命令（Mod目录） | 结果 |
|---|---|
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 402通过，新增8 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，累计421静态 |
| `python scripts/AuditUpgradeTextBindings.py` | 225注册、149直接绑定、6间接绑定、0失败 |
| `dotnet build MaidenSuccubus.csproj -c Release -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Debug` | 0警告0错误，最终保留Debug |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540纯规则断言通过 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，非引擎执行 |
| MvpContent/StructuralContracts/LocalizationStyle/CardEffectTests（各`-ProjectDir .`） | 四门通过 |

既有失败未消除：VisualAssets第317行诱惑度图标与独立数字检查；全卡审计`--no-write`仍有GagCurse费用1/2差异、4未知元数据、215文本待审、2 designOnly、2 retiredCompat。没有覆盖并行审计报告。

待游戏内执行12变体的全文/效果脚本，以及自然回合事件顺序、多份圣言并存、Power独立图标/悬停、多人回合隔离和存读档恢复。未启动游戏、未部署DLL、未修改并行素材。总目标仍未完成。
