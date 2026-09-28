# DS27 批次49：圣洁路线卡面全文

前置`2eba001c`，Plan同步`3e0a0b9f`，完成提交为本文件所在独立范围提交。DS27-02W IMPLEMENTED，游戏内待验，不标VERIFIED、不部署。DesignDoc逐行/词级无漂移，完整阅读圣洁路线正文。

## 7处生产文本修正

| 卡牌 | 修正 |
|---|---|
| 多重再现 | “在下个回合获得”改为“在下个回合结束后，获得”，保留原魔力解放条款 |
| 神圣惩戒 | “魔力解放：抽取它”补为“魔力解放：并抽取它” |
| 镇静药 | 失去资源和抽牌之间逗号改句号，并分行；升级2/3爱心图标绑定不变 |
| 不可以瑟瑟！ | 获得能量与抽牌之间逗号改句号，并分行；能量/欲望图标绑定不变 |
| 龟缩防御 | 删除DesignDoc中没有的格挡前量词“点” |
| 纯净宝珠 | 删除DesignDoc中没有的诱惑度前量词“点” |
| 娅露斯的记忆 | “这3张牌”改为“这三张牌”；保留用户之前明确要求的统一“附魔：灵魂联结”格式，不恢复引号 |

不修改卡牌数值、效果和Power时序。多重再现仍沿用原有延后一次ShouldTakeExtraTurn判定的实现；本批补文本，不据此宣称其整个回合系统已验收。

## 全文契约

`src/Debugging/CardEffects/DesignSyncHolyTextContract.cs`列明44模型、88基础/升级独立全文。测试期望不由DynamicVars或运行时本地化拼回。

`ms_test_cards confirm ds27-holy-text`使用既有225卡目录选出全部44模型，保留原卡效执行和最低效果断言。每个变体先验证真正RunState牌组实例与战斗Hand实例的GetDescriptionForPile输出；原生保留/固有/虚无/消耗/沉底/随身的位置、标点、换行和数量均需精确相等。**该命令沿用破坏性战斗夹具，需要专用测试局；本轮仅编译，没有执行。**

资源图标只将正确路径的欲望图标与能量图标分别替换为不同测试符号。未知路径仍留在字符串中，不以删除图片标签绕过失败；1～3枚重复图标、4及以上数字+图标、镇静药升级后图标数量分别检查。剥除颜色标签的全文检查不代替颜色/美术尺寸验收。

7项新增Python检查覆盖全部88文本与DesignDoc对照、7处修正、两种图标、原生关键字升级、真实实例/选择器接线、标点和数字反例。静态设计适配只允许：

- 设计的基础/升级二元写法及明确升级说明；
- 原生前置关键字移动到卡面前部；
- 已明确的资源图标和统一附魔冒号；
- 万劫不复条目后的独立“（关键字 沉底：……）”解释不作为卡面句子。

不统一句号/逗号，不去掉数字，不将相似度当精确通过。静态比较去除期望中的布局换行以对应DesignDoc段落，游戏内比较仍保留每个换行。不是SmartFormat解释器。

## 验证结果

| 命令（Mod目录） | 结果 |
|---|---|
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 394通过，新增7 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，累计413静态 |
| `dotnet build MaidenSuccubus.csproj -c Release -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Debug` | 0警告0错误，最终保留Debug |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540生产纯规则断言通过，本批无新增 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，非引擎执行 |
| MvpContent/StructuralContracts/LocalizationStyle/CardEffectTests（各`-ProjectDir .`） | 四门通过 |

既有失败：VisualAssets第317行诱惑度图标/独立数字；全卡审计`--no-write`仍为GagCurse费用1/2差异、4未知元数据、215文本待审、2 designOnly、2 retiredCompat。未覆盖并行报告，不把全文契约“已编译”自动写成215条文本已验收。

待验：游戏内运行88变体的Run/Hand全文及既有效果场景，原生关键字和自定义关键字组合顺序，实际图标尺寸、悬停与大图排版。其他卡牌和总目标仍未全部完成，未部署DLL、未启动游戏、未改并行素材。
