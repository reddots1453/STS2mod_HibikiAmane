# DS27第91批：共享调用展开与非规则视觉删除

前置`30ae44a5`，先行计划`a05e474b`。DesignDoc行/词级无漂移，继续用户指定的自动提取方向，不登记新的手写卡牌档案。

## 实现

- 新增HelperExpansion：为输入源码建立方法索引，在可确定同类/静态调用处展开方法体，替换实参，保留默认/命名参数；辅助方法局部变量自动改名，避免多次调用及调用者变量冲突。递归、过深展开、重载歧义、参数修饰符等明确拒绝。只做语法树变换，不执行辅助方法。
- 源码中的IterationCardEffects.Attack由通用索引解析，没有为它或具体卡牌登记特例。多个调用仍按源顺序提取，纯局部X公式可沿实参进入WithHitCount。
- 允许删除原版已识别纯视觉攻击链与VfxCmd；新附魔命令、ForgeCmd.Forge、OstyCmd.Summon按其他效果删除。FromOsty是真实攻击来源而非视觉标记，仍明确报告未支持，不擅自改为玩家攻击。
- 修复return未知辅助调用可能漏检成空程序的问题。加入基础/升级燃烧手环从生成目录打出的引擎测试脚本，只编译，未运行。

## 验证

```powershell
dotnet run --project tests/HumilityCallExtraction --no-restore -- --self-test
dotnet build MaidenSuccubus.csproj -c Debug --no-restore
dotnet run --project tests/HumilityCallExtraction --no-build -- --verify-assembly MaidenSuccubus.dll
```

- 23个通用场景通过。本批新增8个：共享方法默认/命名实参和X/局部隔离、递归拒绝、重载歧义、表达式体格挡、视觉链删除、不同攻击来源拒绝、非伤害其他命令删除、return未知调用拒绝。未新增逐牌数值断言表。
- Debug零警告零错误，实际DLL资源读取通过。目录762个OnPlay记录中643成功提取、119不支持，较上批减少59个未支持记录；包含非攻击/技能，不能作为谦逊全量实现百分比。
- 实际源码通用展开确认HolyCurse/WindRumor/BurningBracelet成功；Uppercut视觉链成功。ForgeStrike最初因新附魔命令被拦，随后通过通用其他效果排除处理，不追加卡名条件。
- 范围diff检查通过，无游戏运行、无部署、不重跑全套。

## 剩余范围

FromOsty等攻击来源、复杂局部数值/目标、其他辅助调用等仍需通用处理；正式选牌入口/觉醒尚未切换，旧表尚未删除。全目标未完成，不以当前抽取成功记录缩小任意攻击/技能范围。
