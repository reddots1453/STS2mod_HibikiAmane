# DS27 第84批：普通圣洁/圣言档案与自身限制清除

前置实现`1ea97dc2`，先行计划`ac4f80cc`（DS27-05AB）。DesignDoc行级/词级无漂移；只验证本轮直接变动，不部署。

## 实际实现

- 新增36张显式档案，总计80张（72本Mod、8原版），其中33张删除原有其他描述后为空程序。读取的源码范围：HolyCards、HolyCardsExpanded、MvpHolyCardsBatch2/3、Iteration1/2HolyCards及Scriptures模板/派生类；未依据DamageVar是否存在猜测效果。
- 蜻蜓点水保留“按当前敌人数，逐次获得格挡”，不把它合并成一笔格挡，因原生敏捷等修正须每次生效。普通攻击/格挡删除其余抽牌、选牌、附加Power、自动打出等规则。六张圣言描述是获得Power层数，不伪造成立即伤害/格挡。
- 补`IntrinsicFlags`：谦逊实例清除原`IsPlayable`限制与`HasTurnEndInHandEffect`标记；未改写实例与外部临时投影不变。不替换`CanPlay`，原生费用、外部ShouldPlay、目标合法性等仍在原调用路径。
- `ms_test_humility_runtime confirm`新增休息变身限制消失、普通休息仍受限、能量不足、原版SlothPower限制仍生效、回合末提示清除、双实例BeforeFlush隔离、敌人数重复格挡、锻成·伶俐不再打开附魔选择。现有全档案基础/升级变量绑定循环自动扩展到80张。

## 验证证据

- `dotnet run --project tests/HumilityEffectContracts/HumilityEffectContracts.csproj --no-restore`：525条生产执行器/档案断言通过（较上批新增156）；敌人数0/1/2/5、基础/升级格挡与20张新增空程序有独立期望。
- `python scripts/TestDesignSyncHumilityRuntime20260927.py`：10项直接接线检查通过，不代表已运行游戏测试。
- `dotnet build MaidenSuccubus.csproj -c Debug --no-restore`：通过，0警告0错误。新增测试初次把返回Task的辅助方法当作返回Power，已改为从实际角色状态获取并重新编译。
- 游戏命令仅编译，尚未实际运行；不得据此宣称Harmony运行时、真实UI或多人已验收。未部署。

## 未完成边界

正式谦逊选牌入口仍是旧实现，不能将80张档案登记等同玩家已得到完整效果。剩余特殊伤害/格挡公式、原版全卡范围、卡牌外部依赖触发、正式选牌替换、觉醒纯效果判定与抽2继续待实现；不以白名单限制选择替代原设计。冲浪次数及条件倍伤公式的具体问题仍待用户答复，其他清晰范围继续推进。整个目标保持未完成。
