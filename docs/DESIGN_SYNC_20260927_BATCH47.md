# DS27 批次47：中立全文渲染契约

前置快照`5da574c7`，Plan同步`68827522`，完成提交为本文件所在独立范围提交。DS27-02V IMPLEMENTED，游戏内待验，不标VERIFIED、不部署。DesignDoc逐行/词级无漂移。

## 范围与修正

完整复核中立正文及相关衍生条目，本批26张中立牌与冰晶碎片、功性魔防壁II/III/IV共30张；模型明细及60条独立基础/升级期望在`src/Debugging/CardEffects/DesignSyncNeutralTextContract.cs`。不从生产DynamicVars或本地化反推期望。

修正梦色的颜料把圣洁牌标紫的问题，按正式颜色语义采用现有金色标签，堕落牌仍紫色。回归发现批次6旧测试也固化了错误颜色，同步其期望；未放宽断言。生产改动仅这一处本地化，不改卡牌机制或数值。

## 自动测试

`ms_test_cards confirm ds27-neutral-text`选择全部30张牌的既有场景，先验证基础/升级全文，再执行原有效果场景。**该入口会重置测试战斗等状态，仅应在专用测试局、用户确认后执行。本轮未执行。**

- 为每张牌创建真实RunState牌组实例，并与当前战斗Hand实例分别比较原生GetDescriptionForPile输出。
- 保留原生保留/消耗/沉底的位置，标点和换行精确相等。不Trim，不把全文压缩成相似度。
- 只把准确的能量资源标签转为`〈能量〉`以进行断言；错误资源路径仍留在结果中。1～3枚重复图标与4枚以上紧凑数字形式独立检查。
- 梦色的颜料另检查原始富文本颜色，避免剥离标签的全文比较掩盖错误。
- 8项新增Python静态检查将全部60条期望与当前DesignDoc逐句核对数值、标点，并检查接线、关键词、能量数量、颜色和反例。静态设计比对仅忽略排版换行，实际换行由游戏内全文断言负责。
- Python检查不是SmartFormat解释器，也不验证Godot渲染或视觉尺寸。

## 本轮执行结果

| 命令（Mod目录） | 结果 |
|---|---|
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 380通过（新增8）；首次发现旧颜色期望，修正后全通过 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，累计399静态 |
| `dotnet build MaidenSuccubus.csproj -c Release -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Debug` | 0警告0错误，最终保留Debug |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540生产纯规则断言通过 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，非引擎存读档 |
| MvpContent/StructuralContracts/LocalizationStyle/CardEffectTests（各`-ProjectDir .`） | 四门通过 |

旧失败未隐藏：VisualAssets第317行诱惑度图标/独立数字检查；`AuditCardLocalization.py --no-write`报告GagCurse费用期望1而源码2、4未知元数据、215文本待审、2 designOnly、2 retiredCompat，未写入并行报告。

待游戏验收：运行本批60变体的Run/Hand全文断言及既有效果场景，检查原生关键词顺序、能量标签实际输出、卡面与大图排版。子守歌等未确认语义不在本批擅自决定；本批不代表全卡描述或总目标完成。未启动游戏、未部署DLL，未改并行资源。
