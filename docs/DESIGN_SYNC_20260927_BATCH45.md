# DS27 批次45：升级数值文本绑定检查

前置快照`a3f9c8ce`，Plan同步`e3cd5c63`，完成提交为本文件所在独立范围提交。DS27-02U IMPLEMENTED，仅静态检查；不是全部卡牌VERIFIED。DesignDoc逐行/词级无漂移，未部署。

## 实现

`scripts/AuditUpgradeTextBindings.py`从当前注册表、C#源码和本地化读取，不读取或重写并行`.review/card_localization_audit.json`。复用已有类、继承、OnUpgrade和SmartFormat占位符解析。

- 识别OnUpgrade中的直接DynamicVars属性/索引UpgradeValueBy写入；直接方法或BaseValue/IntValue赋值中不支持的形式列为UNRESOLVED，不自动通过。
- 按继承覆盖和显式base调用收集升级语句。排除注释及字符串中的假调用。
- 计算变量只有在源码声明了对应原生CalculatedDamageVar、CalculatedBlockVar或命名CalculatedVar时才形成依赖；使用原生CalculationBase及相应附加变量。
- 图标StringVar必须在OnUpgrade实际根据源变量更新StringValue；仅声明固定初始图标不能作为证据。
- 缺描述、缺绑定、不支持的已识别操作返回非零；`--json`输出逐卡依据至标准输出，不生成报告文件。
- `ValidateLocalizationStyle.ps1`执行本检查并检查退出码，失败纳入原门禁。

当前225张注册牌：127张包含可识别升级标量，149个绑定，其中6个间接绑定；98张没有直接标量升级。绑定检查0失败。本轮未发现新的生产文本遗漏，未修改数值、规则或素材。

## 反例与测试

`TestUpgradeTextBindings20260927.py`新增17项：直接属性/索引、硬编码失败、注释字符串排除、继承与base调用、表达式方法、三类计算变量及缺失证据、假定义、图标初始值与实际升级刷新、传递依赖和循环终止、不支持写入、缺描述、继承循环、CLI JSON/失败退出码、门禁接线、当前全注册扫描。

真实卡牌反例仅在内存执行：把气旋破裂`{ShatterPower:diff()}`改回字面量1会报告缺绑定；移除镇静药OnUpgrade的图标刷新会报告DesireLoss缺绑定。不修改用户源码来制造错误。

## 验证

| 命令（Mod目录） | 结果 |
|---|---|
| `python scripts/AuditUpgradeTextBindings.py`（亦由门禁执行） | 225注册、149绑定、6间接、0失败 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 364通过，新增17 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，合计383静态 |
| `dotnet build MaidenSuccubus.csproj -c Release -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Debug` | 0警告0错误，最终保留Debug |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540生产纯规则断言通过，本轮无新增 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，非引擎存读档 |
| MvpContent/StructuralContracts/LocalizationStyle/CardEffectTests（各`-ProjectDir .`） | 四门通过 |

既有失败：VisualAssets第317行诱惑度口红/独立数字约束；全卡审计`--no-write`返回GagCurse费用期望1/源码2、4未知元数据、215文本待审、2 designOnly、2 retiredCompat。没有改写并行报告来消除这些结果。

## 明确边界

这是有限源码依赖检查，不是C#编译器或SmartFormat渲染器。多个描述变体合并引用，不能证明每条分支都正确；同名计算定义、继承和运行时重绑定仍需人工核对。不分析任意helper驱动、别名或所有可能的赋值语法，不把无直接标量升级称为已验证。也不核对具体数值、标点、等级、排版、概率、出牌结算或多人行为。

完整卡牌全文复核、尚未实现/待澄清规则、游戏内脚本执行、基础/升级预览和图标截图仍需完成。总目标保持未完成；未部署、未启动游戏、未执行破坏性游戏测试。
