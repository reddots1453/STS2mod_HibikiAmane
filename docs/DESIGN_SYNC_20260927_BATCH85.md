# DS27 第85批：混合效果、成长与接口规则

前置实现`3f8cadf5`，先行计划`e38f8c01`（DS27-05AC）；DesignDoc行级/词级无漂移。仅本轮范围定向验证，不部署。

## 交付范围

- 新增29张明确档案，总计109张（101本Mod、8原版），含48张空效果。单次攻击、全体攻击、闪耀之剑的Repeat多段、光箭攻击后格挡分别显式保留；反射屏障删除消耗触发，不错误地合并成两次即时格挡。
- 千咒之镰使用已成长的Damage.BaseValue；谦逊翻倍不回写CurrentDamage，后续消耗触发被隔离。其他卡牌生成、选牌、施加Power等规则删除；不会倒追回滚先前已独立结算的状态/奖励。
- 光箭原生“魔力增幅效果翻倍”由`IDoubleMagicAmplification`标记而非卡牌钩子实现，新增`MagicAmplificationCardRules.HasIntrinsicDouble`统一资格检查，即时Power与延迟值计算两处均使用。只对谦逊实例移除该专属规则，普通增幅、战术核心和附魔保持原生流程。
- 实机脚本新增6种光箭基础/升级×普通/谦逊/战术核心+附魔场景，检查真实伤害和格挡、增幅消耗及在原生BeforeAttack时采样延迟计算；新增反射屏障打出/消耗、镰刀成长值/消耗、闪耀之剑多段场景。自动模型绑定循环扩展为109张基础/升级。

## 验证结果

- `dotnet run --project tests/HumilityEffectContracts/HumilityEffectContracts.csproj --no-restore`：645条直接链接生产档案/执行器的断言通过，较上批新增120条；含伤害后格挡的独立有序期望、成长已有值、多段升级和15张新增空效果。
- `python scripts/TestDesignSyncHumilityRuntime20260927.py`：12项静态接线检查通过，含即时/延迟统一资格路径及保留战术核心。
- `dotnet build MaidenSuccubus.csproj -c Debug --no-restore`：0警告0错误。
- `ms_test_humility_runtime confirm`仅编译未运行；不宣称游戏内、多人或UI已验收。未部署。

## 仍待完成

正式谦逊选牌入口仍未切换；剩余特殊公式、原版完整覆盖、外部依赖触发以及觉醒纯效果判定/抽2继续待办。此批109张档案不是完整Q14，更不代表全DesignDoc同步完成。冲浪删除抽牌后的次数与条件倍伤的具体澄清尚未收到答复，其他明确项继续推进。
