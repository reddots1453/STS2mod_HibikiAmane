# DS27第92批：调用内来源与副资源X

变更前快照`a444725e`，先行计划`09f6cd68`。DesignDoc行/词级无漂移，继续按用户要求直接提取伤害/格挡调用，不增加逐牌登记。

## 实现

- 调用程序保留Card/Osty攻击来源；FromOsty按源码识别Owner.Osty，不把未知宠物或来源静默替换。运行时使用原生FromOsty；没有存活奥斯提就不执行该攻击，不恢复删除的召唤效果，不改成玩家攻击。预览使用原生OstyDamageVar，保留宠物自身修正来源。
- 程序schema2保存来源，读取旧schema1时保持原先Card语义；新版缺失/未知来源拒绝，格挡不得错误携带攻击来源。
- 识别play/cardPlay.SecondaryResources().Value(DesireResource.Id)，数值仍从打牌账本读取，不读支付后余额。未知资源或别的对象不猜测。
- 实际万念俱灰提取遇到数值内的升级分支，补通用IsUpgraded数值选择表达式；随实例升级即时解析，保留X次数，不恢复外围触发条件。

## 验证

```powershell
dotnet run --project tests/HumilityCallExtraction --no-restore -- --self-test
dotnet run --project tests/HumilityEffectContracts --no-restore
dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false
dotnet run --project tests/HumilityCallExtraction --no-build -- --verify-assembly MaidenSuccubus.dll
python scripts/TestDesignSyncHumilityRuntime20260927.py
```

- 31个通用场景通过，覆盖来源提取/倍增/序列化/执行、旧程序兼容、非法来源、双资源X及升级次数等。没有新增逐牌数值清单。
- 直接依赖的旧程序契约937次Check执行通过（不是937个独立场景）；只增加新版必需字段检查，没有继续扩张161张旧档案表。
- Debug零警告零错误，实际DLL资源解析通过。762个OnPlay记录中657提取、105明确未支持，含非攻击/技能，不能当作全卡完成率。实际Flatten来源为Osty，AllHopeLost双X提取成功。
- 14项已有接线检查通过；首次旧检查依赖FromCard与WithHitCount相邻的字符串失败，已改为检查共用段数、两种来源分支和宠物缺失守卫。这是静态检查，不是游戏执行证据。
- 未执行游戏测试、未部署、未重跑全量验证。其余未支持表达式/调用、正式选择器及觉醒、旧表移除尚未完成；全目标未完成。
