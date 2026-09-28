# DS27第82批：谦逊实例运行时

变更前快照`00e4aec7`，计划提交`22b2a314`。DesignDoc行级与词级无漂移。本轮仅Q14运行时和直接相关测试，不部署，不改并行素材、角色实现及审计结果。

## 实现

- `Core/Cards/HumilityRewriteCapability.cs`：明确完整效果程序附加到战斗内攻击/技能实例；重复施加翻倍已有程序。不触碰规范模型或DeckVersion；保留相同附魔对象。移除原关键词和本体复读，保留Steady/RoyallyApproved/Goopy/TezcatarasEmber及本Mod层叠容器的附魔关键词，不再次执行ModifyCard。
- `HumilityNativeEffects.cs`：伤害走原生FromCard＋WithHitCount；格挡走GainBlock，均传原CardPlay。能量/星星X调用原生Resolve方法保留化学X等修正，副资源X取本次台账Value；随机目标使用原生RNG。退出战斗后停止后续命令。
- `HumilityRewritePresentation.cs`及`HumilityRewritePatches.cs`：实例Description替换为程序生成文本，原生DamageVar/BlockVar预览处理力量、敏捷、目标易伤及附魔，目标作用域异常时清理。保留原生附魔后缀与相关悬停，去掉旧效果悬停。拦截原回合末包装及原结果牌堆覆盖，同时保留原生消耗/复制牌移除规则。
- 原卡从跑局/战斗钩子列表移除，但能力、附魔、外部遗物和Power仍保留。Ritsu会在RunState的嵌套CombatState流上再次展开能力，故延迟到外层展开后再过滤，防止能力被丢弃。未出现任何谦逊实例前直接跳过该过滤。
- 首次编译发现目标预览枚举是CardModel私有嵌套类型，改为按原版唯一三参数私有方法定位；空战斗预览警告也已修正。最终Debug零警告零错误。

## 验证与边界

在Mod目录执行并通过：

```powershell
python scripts/TestDesignSyncHumilityRuntime20260927.py
dotnet run --project tests/HumilityEffectContracts/HumilityEffectContracts.csproj -c Release
dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore
```

6项是源码接线检查，不冒充运行时验证；146项直接运行生产程序解释器。没有重复全量审计。

新增`ms_test_humility_runtime confirm`，只允许一次性单人本角色战斗，明确破坏当前战斗。脚本已编译但未执行：

- 基础/升级剑柄打击真实支付/Wrapper执行，额外抽牌消失、伤害翻倍、原生AfterAttack一次；
- Swift仅附魔抽牌、Glam重放、Goopy消耗/累加和Steady保留；
- 双重防御两次格挡，敏捷不被重复翻倍；目标力量/易伤预览与实际31点伤害一致；
- KinglyKick抽到减费被隔离，未改写实例仍正常；ShiningStrike不再获得星星或回抽牌堆；
- 零/一/三X与化学X、多敌人、单个多段攻击命令；双X实际付款和附魔重放；
- 原卡监听移除、能力在战斗及嵌套跑局流中各保留一次；实例克隆独立和框架能力JSON恢复；规范模型未改。

## 仍未完成

正式`HumilityLesson`选择器尚未接入：需补齐各卡完整伤害/格挡效果档案及特殊计算，不能用“看到Damage/Block变量就当成单段”的猜测或限选白名单替代正式范围。觉醒纯描述判定与抽2张仍待实现。当前原生适配器对伤害支持选中/自身/全敌/随机敌人，格挡支持自身/选中/全友方；其他组合在绑定前明确拒绝，后续档案发现需要时继续扩展。

通用第三方附魔新增本地关键词尚需适配，不能声称支持未知附魔来源。特殊卡牌自身的非钩子属性/生命周期覆盖应随其完整档案检查。本批实机画面、联机、真实保存退出重进及所有卡牌覆盖均未验；Q14整体仍READY，完整目标未完成。
