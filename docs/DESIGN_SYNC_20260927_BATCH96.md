# DS27第96批：谦逊正式选择接入（2026-09-29）

变更前`04adf72c`，先行计划`c0dd223f`。DesignDoc行级与词级无漂移。上一个目标回合仅报告进度，没有代码进展；本轮直接补正式入口，不再扩张手写卡表。

## 交付

- `HumilityLesson.OnPlay`原生手牌选择后接入`HumilityExtractedCards.Apply`，实例程序翻倍伤害/格挡、保留次数/X、删除其他原规则，描述由已有实例展示层即时替换；不再改底层DynamicVars或调用假升级刷新。
- 保留所有其他攻击/技能可选，不做支持名单过滤。尚无完整提取程序时记录原因、显示“原效果未改变”提示，避免未处理异常中断战斗。该牌未改写是明确待修缺口，不是新产品规则；当前谦逊仍会按原流程消耗，不声称此路径已完成。
- 已改写/复制/恢复的实例再接受谦逊时直接使用自身程序，不依赖旧卡表或重置此前倍率。
- 构建期按源码基类链找到继承OnPlay，并把虚/抽象辅助调用派发到实际子类；显式base调用保留词法基类。区分泛型元数，不将抽象模板作为卡记录；原卡纯度同时检查继承的触发钩子。
- 六种圣言均形成具体类型的空效果程序，谦逊后不再施加原Power。
- 通用卡牌效果测试不再错误断言Damage变量本身翻倍，而是走选择再打出、检查实际伤害。
- `ms_test_humility_runtime confirm`新增实际打出谦逊的基础/升级场景：攻击与无关实例隔离、即时伤害描述、谦逊自身消耗、再次改写四倍、删除原抽牌并保留Swift、格挡且不生成小刀、继承圣言空描述/无Power。只编译未游戏执行。

## 验证

```powershell
dotnet run --project tests/HumilityCallExtraction --no-restore -- --self-test
python scripts/TestDesignSyncHumilityRuntime20260927.py
dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false
dotnet run --project tests/HumilityCallExtraction --no-build -- --verify-assembly MaidenSuccubus.dll
```

63通用场景、16静态接线通过；Debug最终0警告0错误，修正了新增脚本误用原生Enchant签名的首次编译错误。真实DLL资源读取成功：758提取/27未解析，共785条记录；包含不同卡类型，不能当作全卡兼容率。范围diff检查通过，无部署、无游戏运行。

## 剩余边界

未解析公式/目标/特殊回调和未知外部Mod仍待支持，目录诊断没有掩盖为空效果。旧手写档案只保留在历史调试回归中，正式路径不回退它；这些测试后续需迁移。完整游戏手测、其他怪物/事件/路线变更缺口仍未完成，不标整体完成。
