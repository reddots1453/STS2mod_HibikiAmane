

## 2026-10-04 玩家日志谦逊数值与本机淫纹完全保留（HUMILITY-RETAIN-20261004）

前置快照`b281816f935d0a51d306fc7601a1df8240b8617d`；基线f37e2b9d。复核DesignDoc相对HEAD及最后接受版逐行与词级差异，完整复核ACT4谦逊翻倍/移除描述/保留附魔规则、CURSE淫纹三级全部及已有Plan/追踪；未有本批玩法变更。不相关按摩事件文案继续保留工作树与快照，未混入本批实现；不新增卡牌数值。本批覆盖上轮SOURCE_CHECKED的淫纹保留结论：用户本机实测失败，必须修补特殊回合末路径后重新验收。

| 验收ID | 证据、任务与结论 | 状态 |
|---|---|---|
| HR-01 | v0.111 CombatManager.DoTurnEndCards将HasTurnEndInHandEffect牌移入Play；ResolveTurnEndCardEffects调用效果后非虚无无条件Discard，无Retain分支。故淫纹完全的Retain虽然存在，仍被特殊管线弃置。改为BeforeFlush钩子：仅Owner且仍在Hand、活战斗时生成2张发情；取消HasTurnEndInHandEffect入口，让原版正常Flush保留自身。原生生成/牌堆动画/手牌计数不变，轻微和扩散牌原流程不改。BeforeFlush签名在0.107和0.111相同 | READY→IMPLEMENTED，用户实测待验 |
| HR-02 | 用户godot (4).log运行v0.107.1、Ritsu0.6.5、CrossVersionCompat、MultiEnchantment及EcoLib/Watcher。用户补充72→含格挡37发生于LAGAVULIN_MATRIARCH。日志19702～20176可定位该场多次谦逊/打击，未含每段输入/预览/结果，不能单凭日志归责某个模组或证明哪一次乘法漏算。原版族母睡眠/覆甲/攻击无固定半伤规则。预览临时DamageVar.UpdateCardPreview与实际攻击Hook.ModifyDamage入口不同，日志4944～4946列出前者额外EcoLib/Watcher补丁，4917～4919列出后者另一组补丁。战斗内谦逊伤害预览直接走实际Hook.ModifyDamage入口，保留原牌/附魔/能力/目标/Power修正；不把已修正预览伤害再当基础值输入攻击。计算型变量仍实时求值，翻倍只作用数值不改变次数/X | 入口统一IMPLEMENTED后待验；72→37完整成因待新逐段日志 |
| HR-03 | 增添每个改写实例的最近指向目标预览缓存；改写记录multiplier/program，出牌记录当前变量BaseValue、倍率与最近预览，每段结果记录敌人、BlockedDamage/UnblockedDamage/OverkillDamage。只读日志，无额外Hook计算/玩法RNG，不修改资源；日志可分辨基础值变化、倍率丢失、预览补丁差异或伤害后减免 | READY→IMPLEMENTED后待验 |
| HR-04 | 日志5931旧版无GetResultLocationForCardPlay、5938旧版无CombatTurnState参数重载。新位置补丁加Prepare空目标跳过，另给旧GetResultPileTypeForCardPlay型安全补丁；PortableRetainPatch优先新带state重载，否则旧无参重载。两版本只接一个随身入口，不重复触发；不把这两处位置/随身报错当作72伤害不一致的直接证明 | 确认接口兼容修补READY→IMPLEMENTED后待验 |

仅build，不静态测试；不部署、不动ModUploader/沙箱/JSON/用户存档。保留上轮碎片折扣、福音预览、堕落值诊断的累计源码。未在0.107沙箱或玩家环境实际运行，日志中的其他图标与本地化旧包告警不自动当本批新问题修补。
