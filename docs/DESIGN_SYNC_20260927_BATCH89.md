# DS27第89批：直接抽取伤害/格挡调用

前置`c03dad85`，先行计划`8f0bb229`。依据用户最新技术澄清，停止逐牌扩充，冲浪不再模拟抽牌；本批交付自动抽取工具，不声称已替换正式游戏入口。

## 实现

- `tests/HumilityCallExtraction`是可运行的抽取CLI兼通用自测入口；虽置于tests以隔离Mod编译扫描，抽取器本身不是测试替身。使用已安装.NET SDK中的Roslyn，解析C#语法树，不执行原OnPlay，不依赖游戏启动。
- 识别Attack链与GainBlock调用，保持源码顺序，提取目标、数值、显式WithHitCount；去掉外围条件/循环及其他效果。数值支持动态变量、CalculatedVar、纯局部别名、加乘/Min/Max及原生X取值，交给已有生产程序统一翻倍执行。
- 其他效果无直接伤害/格挡时可输出空程序；未知辅助方法、回调内伤害、复杂局部赋值、未知数值表达式/攻击链等整张报告unsupported及行号，绝不把这些失败当成空程序。仍需补全这些语法与辅助方法切片，不能将初版工具标作全卡兼容。
- stdout输出JSON，不写输入源文件。未知格式退出2；语法/数值分析不运行被删命令。生产JSON模式复用现有程序，未引入按卡名匹配。
- 统一验证入口新增`humility_call_extraction`套件，全部登记15套；按套件运行，不重复全卡测试。

## 验证

```powershell
dotnet run --project tests/HumilityCallExtraction --no-restore -- --self-test
python scripts/TestDesignSyncValidationRunner20260927.py
python scripts/ValidateDesignSync20260927.py --suite humility_call_extraction
dotnet build MaidenSuccubus.csproj -c Debug --no-restore
```

- 12个通用场景通过：去条件/抽牌、去外层抽牌循环、X局部计算、多段随机、伤害格挡顺序、CalculatedVar、依赖副作用拒绝、未知辅助方法拒绝、纯其他效果为空、回调拒绝、语法错误与表达式体拒绝；并通过实际生产Execute验证8伤害×4段仍为一次多段命令。
- 抽取实际源码：MvpNeutralCardsBatch3.cs内5个OnPlay、原版IronWave/Barrage提取通过；Whirlwind先识别出未支持的无前缀X调用/音效语句，补通用识别后提取通过。没有为这些卡新增手工注册条目。
- 入口12单元测试通过；定向报告`obj/design-sync-validation/20260928T145959Z-dc9d7c813df2/report.json`为当时工具版本通过，随后仅收紧X方法接收者并补音效排除，再次直接自测通过；不冒称旧报告对应最后源码。
- Mod Debug零警告零错误；未启动游戏、未部署。此工具编译/运行不会将Roslyn打包到Mod。

## 后续

仍需将工具结果接入构建和正式谦逊入口，补必要的局部赋值/辅助方法切片，移除手写卡牌表及重复逐牌测试，并同步卡面/觉醒判定。没有以工具阶段代替全量目标，也没有将旧161档案标为已删除。此轮不再重复索要冲浪抽牌模拟答案。
