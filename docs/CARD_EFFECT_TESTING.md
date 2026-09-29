# 卡牌效果自动测试

## 范围与需求基线

本测试工具以当前 `DesignDoc.md` 为测试预期，并以内容契约校验注册集合；不自动从卡牌实现的 `DynamicVars` 反推出期望值。这样，代码中的错误数值不会同时污染测试预期。

内容契约中的 229 张牌必须逐一登记。当前状态：

- 228 张牌具有可执行测试；通常同时测试基础版和升级版。
- 仅 `HypnosisCurse` 的效果仍待后续设计，记录为 `DESIGN_PENDING`。
- `DESIGN_PENDING` 不会显示为通过；除此以外不允许跳过。

测试断言读取真实结算后的生命、格挡、费用、能量、欲望、堕落值、Power 层数、牌堆位置、升级/关键字/附魔和怪物意图等状态。仅能成功打出、存在 `OnPlay` 方法、存在 DynamicVar 或存在某个接口均不能构成通过条件。

对于持续型能力牌，测试不会止于“已获得某个 Power”：它会继续触发对应的真实回合、抽牌、出牌、消耗、欲望资源、圣言、断罪审判或变身钩子，并核对最终数值。例如“暴风雪”会验证初次结算和后续两个回合各自的全体伤害与冰雾生成；“基础训练”会分别验证打击和防御标签的实际加成；“万劫不复”会执行达到阈值的断罪审判并检查层数仍被保留。

## 运行方法

工具只编译进 Debug 版本。由玩家启动游戏、使用响木天音进入一场可以放弃的单人战斗，然后按 `F10`。测试会在当前 Godot 主线程开始执行全部229张牌的登记项；运行期间再次按 `F10` 不会启动并发测试。`Shift+F10`只运行68张第二轮新增/变更卡牌；`Ctrl+F10`单独运行16项拘束意图套件，不再触发卡牌全测，详见[`CONTROL_INTENT_TESTING.md`](CONTROL_INTENT_TESTING.md)。

控制台入口仍保留用于单牌复测或无法使用热键时的调试。完整测试命令为：

```text
ms_test_cards confirm all
```

只测试一张牌时使用 C# 类型名：

```text
ms_test_cards confirm JudgmentBlade
```

命令中的 `confirm` 是强制确认令牌，不能省略。

## 破坏性与确定性边界

测试会移除当前角色的遗物，清空战斗牌堆和 Power，将参战者生命上限设置为 20000，重置生命、格挡、能量、欲望与堕落值，并为各场景重新生成怪物意图。拾取类效果还会短暂向永久牌组加入测试牌，并在断言后移除。因此只能在专用测试跑局中执行，完成后应放弃该跑局。

每个场景复位时会同步清理由真实卡牌效果生成的 `NCard` 节点，避免模型已经移除但卡牌视觉节点继续堆积在手牌区。测试完成后手牌区应被清空；最终状态仍以带有 `FinishedAt` 的 JSON 报告为准。

`Rest` 的“结束回合”调用在该牌的测试作用域内由 Debug-only 拦截器记录并抑制，避免测试套件中途推进真实战斗；退出变身、下回合抽牌和加费仍使用真实 Power 结算验证。拦截器在其他时间不生效。

随机目标效果使用所有敌人的总状态或逐敌状态验证，避免只因随机目标不同而误报。需要选择牌或选择透支的场景使用游戏自带 `TestCardSelector` 提供确定选择，卡牌本身仍通过真实 `CardCmd`、`PowerCmd`、`DamageCmd` 和牌堆命令结算。

## 输出

控制台和游戏日志会输出每张牌、每个基础/升级场景的 `PASS` 或 `FAIL`。完整 JSON 报告写入已加载模组程序集旁：

```text
card-effect-test-results/card-effects-YYYYMMDD-HHMMSS.json
card-effect-test-results/latest.json
```

每个失败断言同时保存预期值和实际值；运行时异常保存完整堆栈。两张待设计牌使用独立的 `DesignPending` 状态。

## 2026-09-08 首轮实测基线

首轮完整 F10 测试于 14:16:05 开始、14:23:02 正常结束，并成功写出报告，不属于卡死或中断。结果为 150 张通过、51 张失败、3 张按设计状态跳过。失败项经源码和数值报告复核后分为两类：测试夹具未按真实手牌、资源支付、变身、回合抽牌、圣言触发及伤害修正流程建立状态；以及侵犯/事件诅咒的实际效果尚未实现。该报告作为修复前运行时基线，Git 快照为 `maiden-card-test-first-run-20260908`。

## 2026-09-08 第二轮实测基线

第二轮完整 F10 测试于 16:03:55 开始、16:11:33 正常结束，报告带有 `FinishedAt`，结果为 197 张通过、4 张失败、3 张按设计状态跳过。Git 快照为 `maiden-card-test-second-run-20260908`。

四张失败牌的复核结论如下：

- `BattleTechniqueReplay`：夹具错误地尝试打出未激活原卡，没有执行其真实的战斗开始钩子，因此没有安装负责牌面复制的 Power。
- `LureDeep`：测试发生在 `NIBBITS_WEAK`；小啃兽按权威色情意图目录没有拘束意图，本来就是无效目标。复测改为通过原版 `CreatureCmd.Add<Byrdonis>` 临时加入真实且具备拘束目录项的敌人，结算后再通过 `CreatureCmd.Escape` 清理。
- `LoversDagger`、`SwordVerdict`：测试连续改写怪物意图却没有重建状态机，残留的“必须先执行”临时意图会令原版 `CreatureCmd.Stun` 静默拒绝切换。同时，本模组原有强制眩晕状态未使用引擎识别的 `STUNNED` ID，属于真实运行缺陷。

复位现在为每个现存怪物重建原版状态机，隔离前一场景的临时意图。模组眩晕桥接先走原版命令；若原版命令因未执行的临时色情意图拒绝切换，则强制安装可被 `Creature.IsStunned` 识别的 `STUNNED` 状态。所有声明击晕的本模组卡牌统一走该桥接。

审阅还发现 `DesireWhip` 原先只验证了7点伤害，没有验证“目标为拘束或侵犯意图时将其击晕”。该登记已改为建立真实拘束意图、打出卡牌并同时核对7点伤害和眩晕状态；构建门禁止卡牌源码绕过模组眩晕桥接，并要求保留这条条件断言。

## 2026-09-09 第三轮实测基线

第三轮完整 F10 测试于20:04:21开始、20:11:53正常结束，报告带有 `FinishedAt`，结果为200张通过、1张失败、3张按设计状态跳过。Git快照为`maiden-card-test-third-run-20260909`。

唯一失败项为`BattleTechniqueReplay`：复制牌正确生成、继承升级保留并成功打出，但恢复后的原牌不在弃牌堆。当前游戏源码证明`AfterCardPlayed`在牌仍位于`PileType.Play`时触发；此时立即Transform会让恢复牌留在打出区，而原版稍后尝试移动的旧实例已经被Transform移除。修复后，`AfterCardPlayed`只登记待恢复实例，等原版将它从打出区移入实际结算牌堆并触发`AfterCardChangedPiles`后，再原地Transform回战技复读。

对应测试除原有复制类型和保留检查外，新增恢复牌升级状态及打出区清空断言。构建门要求保留“从`PileType.Play`离开后恢复”的生命周期边界。

## 2026-09-09 拘束意图栈溢出回归

第三轮完整测试结束后的人工战斗在敌人执行`MAIDENSUCCUBUS_CONTROL`时直接退出。`godot.log`最后记录为`MagicResonance.OnPlay`的挣脱投影补丁安装；Windows应用程序错误报告记录异常码`0xc00000fd`，确认是栈溢出而非普通托管异常。变更前快照为`maiden-pre-control-stack-overflow-fix-20260909`。

根因路径是拘束Power的`AfterApplied`同步刷新所有战斗牌，并在此时调用Harmony动态改写每个新遇到的具体`OnPlay`方法。修复删除全部运行时卡牌效果补丁安装，改由RitsuLib 0.4.64的`ICardOnPlayHookListener.BeforeCardOnPlay`在统一`CardModel.OnPlayWrapper`内只抑制已登记投影实例的原始效果。费用、打出区、附魔/苦痛投影、`AfterCardPlayed`和结算移牌仍由原版包装流程负责。

`MagicResonance`测试现在额外建立能力牌拘束，验证施加拘束不会中断、投影牌原效果不生效、1费精确减少1点挣脱值且最终进入弃牌堆。结构门禁止恢复`EscapeEffectPatcher`或在投影路径调用`Harmony.Patch`，并要求`ControlPower`保留RitsuLib的实例级出牌监听器。

### 2026-09-10 复测与第二层修复

首次修复部署后的人工复测仍在`MAIDENSUCCUBUS_CONTROL`退出。最新日志已经没有动态`OnPlay`补丁记录，Windows错误报告则由模糊`StackHash`进一步定位为`coreclr.dll`中的`0xc00000fd`。这证明运行时Harmony安装是一个已移除风险，但不是唯一递归入口。

第二层修复把`ControlQuery`的线程重入门从“仅重建投影”扩大到“校验或重建投影”的整个解析事务。投影卡的标题、描述、关键字、附魔、苦痛和目标均通过Harmony getter查询同一投影；完整重入门保证getter间接调用或第三方getter补丁只能看到该次解析的原始值。`EscapeCardVisuals.Refresh`也增加同线程重入门，同时删除一次Power变化中来自`AfterApplied`、`AfterPowerAmountChanged`和命令层的重复全牌堆刷新。

`MagicResonance`能力牌拘束场景现在连续32轮读取投影标题、描述、关键字和目标，再验证原效果抑制、挣脱值及结算牌堆。游戏内复测仍需覆盖成功施加拘束（格挡低于意图阈值）和被格挡的拘束两条分支。

### 2026-09-10 卡牌实例 capability 重构

进一步对照原版女王的`ChainsOfBindingPower`与`Bound`后，挣脱不再使用独立的弱引用投影表。每张符合条件的实际战斗卡附着`EscapeProjectionCapability`，由`ControlPower`在数值及牌堆生命周期中统一协调；标题等展示查询只读取已附着状态，不再从getter内部遍历Power并重建投影。

RitsuLib capability现在负责无目标、弃牌结算、锁链覆盖层、原牌与拘束Power悬停，以及精确卡实例的原`OnPlay`抑制。RitsuLib 0.4.64不能完全替换描述，也不能在替换标题时去掉原牌升级后缀，因此严格展示仍保留标题、描述、关键字、附魔和苦难五个精确getter补丁，但这些补丁不再拥有生命周期或刷新职责。

`MagicResonance`场景同时给被测牌附加保留、`Glam`和`Bound`，验证投影期间三者不可见、序列化仍保存原附魔、原效果不执行、只减少实际支付的挣脱值，并在移除拘束后于同一卡实例上恢复原关键字、附魔与苦难。该静态/游戏内回归不能替代敌人真实执行`MAIDENSUCCUBUS_CONTROL`的人工复测。

## 构建期完整性门

`ValidateMod=true` 会执行 `scripts/ValidateCardEffectTests.ps1`，并检查：

- 测试登记与五个卡池中的 229 张牌完全一致且无重复；
- 只有 `HypnosisCurse` 可标记为 `DESIGN_PENDING`；
- 其余 213 张牌都有非零效果断言以及适用的基础/升级场景；
- 测试目录不存在方法存在性、占位 Marker 或 `expectedToExist` 之类伪断言；
- 控制台命令保留显式破坏性确认门。

构建期验证只证明测试目录完整且结构合规。卡牌实际效果是否符合描述，必须在游戏内运行上述命令后，以 JSON 中的数值断言结果为准。
