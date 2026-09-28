# DS27第97批：谦逊共用数值与目标（2026-09-29）

变更前`1be7c547`，先行计划`e1a5126c`。DesignDoc行级/词级无漂移；上轮正式选牌已提交，是实际进展。本轮按共同语法补缺，不增加手写卡档案或重复全范围审计。

## 实现

- 源码中只读`DesireCombatSpending.Get(Owner)`映射实际战斗支付记录；`GetPower<T>()?.Amount ?? 0`读取当前已有层数，原新施加Power命令仍被删除。支付次数不因谦逊倍率翻倍。
- 牌堆Cards快照Count可作为次数；手牌预览排除即将离手的本牌。没有执行原消耗、抽牌或资源支付来模拟数值。
- 所选玩家别名的Creature保留为所选目标；源码中按CurrentHp排序取First的敌人保留为实时最低生命目标。追加谓词/不明目标仍拒绝，不猜目标。
- 友方查询明确筛选存活玩家时保留该筛选，不扩大到宠物；执行使用原生GainBlock。目标类型支持保存恢复，描述与原生预览使用相同目标类型。
- 已确认的额外战斗奖励、UI卡牌配置、卡牌关键词操作、抽牌历史/钩子等作为被删除效果，不再阻断仅保留攻击/格挡。未知方法不一概删除。
- 正式谦逊脚本新增基础/升级：顽强抵抗读取既有5层但不增加层数；实际支付后保留次数；唤雷删除外围触发/循环、保留选定攻击及一次最低生命攻击。仅编译，未实际运行。

## 验证

```powershell
dotnet run --project tests/HumilityCallExtraction --no-restore -- --self-test
python scripts/TestDesignSyncHumilityRuntime20260927.py
dotnet run --project tests/HumilityEffectContracts --no-restore
dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false
dotnet run --project tests/HumilityCallExtraction --no-build -- --verify-assembly MaidenSuccubus.dll
```

71通用场景、17静态接线、939直接生产程序/旧档案断言通过；后者不是游戏执行。Debug零警告零错误，真实DLL嵌入资源读取成功：785条记录中770提取、15明确未支持（并非全卡覆盖率）。范围diff检查通过。无游戏操作、无部署。

## 剩余

条件数值/赋值、依赖攻击结果的公式、直接CreatureCmd.Damage和特殊回调仍有未解析记录；外部Mod源码不在当前目录中。旧手写档案的历史测试迁移、真实引擎测试和其他怪物/事件/路线缺口未完成，不标全目标完成。
