# DS27第93批：多语句攻击链和外围代码删除

变更前快照`9d702987`，先行计划`3cdfcb55`。DesignDoc行级/词级无漂移。上一轮已提交来源/X支持，属于有效进展；本轮继续用户直接抽取调用的方向，不逐牌登记。

## 实现

- 沿唯一局部AttackCommand继续读取配置及Execute；伤害放在执行位置而非构建位置排序，避免先构建攻击、后格挡、再Execute时改变实际顺序。
- 同一if/else中单体/全体目标分支使用原卡实时TargetType，可跟随原生外部目标变更。顺序配置冲突、不同次数/来源、未知重赋值、重复Execute和延迟使用均明确拒绝，不选择最后一个分支冒充正确。
- 新增RemovedCalls统一识别费用设置、明确类型的CardModel关键词/免费设置、List/HashSet维护、纯视觉创建/挂载等其他效果；不将未知Add方法当作集合操作。
- 支持OnlyPlayAnimOnce删除；BeforeDamage只允许已识别纯视觉回调，未知调用或直接状态赋值不能冒充视觉。伤害/格挡回调继续拒绝，而非提前执行。
- 实机脚本增加从生成目录打出完美打击的基础/升级情况，将小刀外部刀扇目标变更的现有测试改为生成目录路径。这些仅编译，未启动游戏执行。

## 验证

```powershell
dotnet run --project tests/HumilityCallExtraction --no-restore -- --self-test
dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false
dotnet run --project tests/HumilityCallExtraction --no-build -- --verify-assembly MaidenSuccubus.dll
python scripts/TestDesignSyncHumilityRuntime20260927.py
```

- 42个通用场景通过，包括本轮执行顺序、真实程序执行、互斥目标、配置冲突、重赋值/重复执行、视觉回调及费用/列表删除。未新增逐牌预期表。
- 14个既有静态接线检查通过；它们不是游戏执行证据。
- Debug零警告零错误，实际DLL嵌入资源读取通过。762个OnPlay记录中694提取、68明确不支持，比前轮减少37条；包含非攻击/技能，不能作为全卡完成百分比。
- 实际源码PerfectedStrike/Shiv/Compact/DaggerSpray/Hyperbeam/SweepingBeam已提取；SovereignBlade仍因未知视觉辅助回调明确未支持，没有借原型相似性猜测。
- 不部署、不执行破坏性游戏测试、不重跑全量；范围diff检查通过。正式谦逊入口、觉醒判定、旧表删除和其余未支持语法仍待完成，全目标未完成。
