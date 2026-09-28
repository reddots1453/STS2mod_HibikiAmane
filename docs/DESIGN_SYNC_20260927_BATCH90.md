# DS27第90批：自动抽取产物接入构建与运行时

前置`77fcbfff`，计划`5c991753`。DesignDoc行/词级无漂移，按用户直接提取调用的方向继续，不增加手写卡牌表。

## 实现

- 抽取CLI新增多个源目录与输出路径选项，确定顺序读取项目内卡牌源码，输出程序、明确失败原因及源文本SHA256。输出前用生产加载器验证目录；输入读取/解析过程不执行游戏代码。
- Mod构建在PrepareResources前生成`obj/<配置>/humility-extracted.json`，嵌入`MaidenSuccubus.HumilityExtractedCatalog.json`。生成命令失败会阻止构建，不继续使用旧目录。工具使用SDK自带Roslyn，运行时不读源码、不加载Roslyn。
- `HumilityExtractedCatalog`严格区分成功、确实空程序、不支持记录，拒绝重复身份、混合状态和无效程序。
- `HumilityExtractedCards.Get/Apply`按实际完整类型及程序集身份读取，不会接受其他程序集同名类型，不回退手写表，不把不支持情况改成空效果。正式选牌入口尚未调用它，剩余通用语法问题需继续处理。
- 游戏调试脚本新增基础/升级冲浪走生成目录：一次全体8伤害、不抽牌、不模拟外层循环；仅编译，未游戏运行。旧手写表回归暂保留，待正式迁移时移除，不能把这个过渡状态描述为已完成替换。

## 验证

```powershell
dotnet run --project tests/HumilityCallExtraction --no-restore -- --self-test
dotnet build MaidenSuccubus.csproj -c Debug --no-restore
dotnet run --project tests/HumilityCallExtraction --no-build -- --verify-assembly MaidenSuccubus.dll
```

- 15个通用场景通过：前批12个调用规则场景，加目录状态区分、重复身份拒绝、错误状态拒绝。没有增加逐牌重复断言。
- Debug零警告零错误。生成扫描762个OnPlay记录，584已提取、178明确不支持；包含非攻击/技能等记录，不等于584张谦逊完整适配或178张需要逐牌处理。
- PEReader直接读取实际DLL资源，生产加载器解析762记录并确认178保留为不支持；没有加载游戏程序集执行任何效果。这证明资源确实打包，不证明游戏行为已验收。
- 未支持主要按通用写法分类：其他攻击来源FromOsty、额外VFX链、锻造/召唤/附魔/创建手牌辅助命令、复杂数值等。后续按语法/命令规则处理，不按牌名注册补丁。
- 范围diff检查通过；未部署、未启动游戏、未重跑全套。

## 尚未完成

正式谦逊入口、觉醒判定、旧手写表移除以及余下抽取规则仍需完成。本批仅使生成结果能被真实运行时读取/应用，不缩小任意攻击/技能选择范围，不标完整目标完成。
