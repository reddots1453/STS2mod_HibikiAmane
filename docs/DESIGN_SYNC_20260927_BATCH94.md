# DS27第94批：其他效果边界与辅助重载（2026-09-29）

变更前快照`f54cfb4a`，先行计划`95bc08b8`。DesignDoc行级/词级无漂移。上一用户问答通过原生描述入口确认战斗实例可动态替换；本批继续落实所需抽取链路，不将可行性确认视为已经正式接入。

## 实现

- RemovedCalls识别独立状态、资源、变身、选牌等非伤害API边界。辅助展开在这些边界停止，避免把已经删除的施加Power/选牌等操作内部效果重新提取回来。不修改这些命令本身或玩法规则。
- 识别PowerCmd.Apply返回值及其配置、CardSelectCmd返回的选中牌、牌堆返回类型，包括可空CardModel；删除相应配置及费用/牌堆维护。未知接收者的同名Schedule/Add等方法仍不能视为已知其他效果。
- 辅助重载按参数数量/名称/默认值绑定，并用可明确的原生Player/ICombatState/IntValue参数排除不兼容项；数值隐式转换仍有歧义时拒绝。支持返回等待集合后的FirstOrDefault这一非效果包装。
- 同类实例辅助方法若源码证明仅字段/变量变更、不包含调用、构造或等待，则直接删除，不把另一张牌的实例当作当前伤害来源。
- 补斗篷与匕首通过生成目录改写的基础/升级游戏脚本：描述只含翻倍格挡，打出仅加格挡不生成小刀。它只完成编译，未在真实游戏中执行。

## 验证

```powershell
dotnet run --project tests/HumilityCallExtraction --no-restore -- --self-test
dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false
dotnet run --project tests/HumilityCallExtraction --no-build -- --verify-assembly MaidenSuccubus.dll
python scripts/TestDesignSyncHumilityRuntime20260927.py
```

- 50通用场景通过，本轮新增8个涵盖独立效果边界、可空选中牌、Power返回配置、被删除命令内部伤害不复活、同参数数量的类型判别、重载转发返回集合、纯字段辅助及未知接收者拒绝；不扩充手写逐牌表。
- 新可空选中牌用例首次失败，定位为类型字符串保留问号，修正统一类型识别后通过，不修改测试预期回避错误。
- 14已有静态接线通过；Debug零警告零错误，实际DLL资源读取通过。762个OnPlay记录中734提取、28未支持，相比上批减少40条；包含非攻击/技能，不能当作全卡完成率。
- 实际BladeDance为零保留效果；CloakAndDagger只留格挡，LeadingStrike只留攻击；Claw/Maul不保留增长操作，TheBomb不保留Power。按通用源码规则得到，不使用卡名条件。
- 未部署，未运行游戏，未重跑无关套件；范围diff检查通过。

## 未完成

余下公式、特殊目标及调用仍明确未支持；继承入口、正式谦逊选择器、觉醒纯描述判定、旧表替换和游戏内验收尚未完成。不能用本批抽取通过或测试脚本编译代替全目标完成。
