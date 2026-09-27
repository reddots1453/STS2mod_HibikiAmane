# DS27-02I：退役内容新获取关闭及兼容测试

日期：2026-09-28。前置快照：`1314b40a`。DesignDoc两种diff均无漂移。范围依据全面审阅中的REMOVE清单，非新增设计。

## 实现边界

- `MagicResonance`、`SemenAppetite`保留原类、注册池、模型ID、旧效果、升级与本地化。没有删除用户已有卡，也没有改名或强制转换为梦色的颜料。
- `RetiredCardCatalog`仅描述获取策略。Neutral/Corrupt在原生`FilterThroughEpochs`之后过滤，`AllCards`与`AllCardIds`不动；奖励、商店和默认变化沿用`GetUnlockedCards`。自定义跨路线原型候选也过滤。
- 两牌`CanBeGeneratedInCombat`、`CanBeGeneratedByModifiers`为false，百科通过原生构造标记隐藏。普通卡默认true；没有全局Harmony过滤，也不拦截旧卡的加载和克隆。
- `EnergyOverloadEnchantment.CanEnchant`拒绝新附魔；原生读档使用`EnchantInternal`而非新获取资格判断，旧类型与载荷保留。当前文档目录中的旧OPEN索引不作为重新启用该已删除正文效果的授权。
- 保留225个注册身份。内容清单新增`retiredCompatibility`，不把“新内容可获取数量”和“兼容注册数量”混为一谈。
- 审计只有在清单、原生池过滤、直接候选、生成标记、百科参数链、注册身份和设计删除一致时才分类`RETIRED-COMPAT`；不将任意缺设计卡自动豁免。文字保留仍待旧档引擎验收，不是已完成全卡渲染。
- 本批不扩展旧牌效果，不实现成人演出或其他未完成机制。不修改其他Agent的素材、场景、角色文件和共享审计报告。

## 验证

| 命令 | 结果 |
| --- | --- |
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告、0错误；未部署 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 112项通过，其中本批10项 |
| `python scripts/TestCardLocalizationAudit.py` | 19项通过，静态合计131 |
| `dotnet run --project tests/DesignSyncContracts/DesignSyncContracts.csproj --no-restore` | 305条既有生产规则断言通过 |
| `dotnet run --project tests/LayeredSaveContracts/LayeredSaveContracts.csproj --no-restore` | 33条既有生产编码断言通过，不是引擎运行 |
| `ValidateMvpContent.ps1` / `ValidateStructuralContracts.ps1` / `ValidateLocalizationStyle.ps1` / `ValidateCardEffectTests.ps1`（均`-ProjectDir .`） | 四门通过，225模型/28遗物/10附魔身份保留 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有第317行诱惑度lipstick独立数值标签断言失败；未修改或放宽 |
| `python scripts/AuditCardLocalization.py --no-write` | 225注册，1项既有GagCurse费用差异，4未知元数据、215待逐字渲染、2仅设计、2退役兼容；不覆盖并行报告 |

新增静态测试包含移除任一获取门、注释伪造门、移除注册、重新加入设计、运行时策略/清单不一致、百科参数未向基类传递等故障注入；防止报告通过只是隐藏缺口。

## 游戏内入口与尚未验收

Debug控制台在一次性单人天音跑局运行`ms_test_retired confirm`。不执行旧牌效果，不改牌组或推进RNG，但仍限定测试跑局；其他角色和多人拒绝运行。

入口直接调用ModelDb查找、各单人/多人约束的GetUnlockedCards、CardFactory.FilterForCombat及GetDefaultTransformationOptions、真实CardModel序列化/反序列化和CreateClone。检查基础/升级/旧能量过载附魔载荷、子附魔归属、原型不被修改，普通三路线及原版卡不被过滤。该入口已编译，**尚未实际执行**。

人工验收还包括：旧版本存档实际保存退出与加载，旧牌组/历史界面仍可显示，百科不显示退役牌，奖励/商店/事件自然候选无退役牌，正常卡仍可生成/变化。第三方Mod显式强行生成旧ID或控制台生成不在“正常新获取”拦截范围内；禁止新获取不意味着破坏复制玩家已有旧牌。

全面目标仍未完成，普通卡全量文本渲染、剩余遗物/事件/第四层和运行时测试继续追踪，不将本批称为全量通过。
