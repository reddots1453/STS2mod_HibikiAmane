# Changelog

## 2026-09-10 — 挣脱迁移为卡牌实例 capability

- 变更前快照：`maiden-pre-escape-capability-refactor-20260910` → `30cc2a4`。
- 参考原版女王的`ChainsOfBindingPower + Bound`实现，将“拘束状态附着到具体卡牌实例、由Power统一协调生命周期”作为本轮结构基准；没有复用原版`Bound`的“一回合只能打出一张”语义。
- 删除`ControlQuery`的`ConditionalWeakTable`投影旁表。每张受拘束的实际战斗卡现在附着已注册、可克隆及可存档的`EscapeProjectionCapability`；Power数值或牌堆变化只负责协调 capability 的附着、换源和移除，getter 查询不再重建投影。
- 目标改为无目标、打出后进入弃牌堆、锁链覆盖层、原牌预览/拘束Power悬停和原效果抑制均由RitsuLib capability接口承担；`ControlPower`不再充当全局`ICardOnPlayHookListener`。
- 删除全局`NCard.UpdateVisuals`挣脱补丁，覆盖层改走RitsuLib原生 capability overlay 容器。因RitsuLib 0.4.64不能整段替换描述，也无法在替换标题时隐藏原升级后缀，标题、描述、关键字及原附魔/苦难隐藏仍保留精确getter补丁；这些补丁只查询已附着 capability，不搜索Power或刷新卡牌。
- 为序列化、附魔/苦难变更和降级建立短生命周期“读取原状态”作用域，确保投影期间的保存及状态命令仍看到原关键字、附魔和苦难，解除拘束后恢复同一张卡的原状态。
- `MagicResonance`运行时回归新增：连续getter读取、原效果抑制、精确挣脱值、弃牌结算、序列化保留附魔，以及解除后关键字/附魔/苦难恢复。
- 完整无部署构建与全部验证门通过（0 warning、0 error）；尚未部署，等待本轮提交后的游戏内拘束意图复测。

## 2026-09-10 — 拘束投影重入与重复刷新修复

- 变更前快照：`maiden-pre-control-projection-reentrancy-fix-20260910` → `b110101`。
- 复测仍在花园幽灵鳗执行`MAIDENSUCCUBUS_CONTROL`时退出；新一轮`godot.log`不再出现运行时`OnPlay`补丁安装，但Windows错误报告明确记录`coreclr.dll`异常码`0xc00000fd`，说明旧动态Harmony路径已删除后仍存在独立的托管栈溢出。
- 投影查询的重入门现在覆盖“校验既有投影”和“重建投影”的完整过程；任何卡面getter或其他模组补丁在校验期间再次查询同一投影时都会读取未投影结果，不再递归进入`IsStillValid`。
- 卡面批量刷新增加同线程重入门；拘束施加、挣脱和直接解除不再从命令层重复刷新，初次施加也不再同时走`AfterApplied`与全局`AfterPowerAmountChanged`两条刷新路径。刷新权威入口收敛为Power数值变化和移除生命周期。
- 拘束Power数值变化时分别记录投影刷新开始、投影表完成和卡面刷新完成三个低频诊断点；若仍发生不可捕获的栈溢出，最新日志可直接区分模型投影与Godot卡面刷新阶段。
- `MagicResonance`回归场景新增连续32轮读取投影标题、描述、关键字和目标的检查；结构门固定完整投影重入保护、卡面刷新重入保护及单一刷新所有权。
- 无部署构建和显式部署构建均通过全部验证门（0 warning、0 error）；已部署至`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`。源码与安装目录DLL SHA-256均为`81B7D796A36A291B43AD64BC342EA05F210E48390D8BEFDD09A9E70E14170954`，PDB均为`BE4DC78E1E6BB8B0C9D2AC38759A030CB645BAEE47DEEB08A78F4263454ABE73`；修复后运行时结果待人工复测。

## 2026-09-09 — 拘束意图栈溢出修复

- 变更前快照：`maiden-pre-control-stack-overflow-fix-20260909` → `60e4794`。
- 人工测试在花园幽灵鳗执行`MAIDENSUCCUBUS_CONTROL`时闪退；`godot.log`最后一条模组记录是为`MagicResonance.OnPlay`安装挣脱投影补丁，Windows错误报告于20:31:26记录`0xc00000fd`/`StackHash_2264`，确认是栈溢出。
- 根因是结构重构后的按需效果补丁仍在`ControlPower.AfterApplied`同步刷新过程中调用`Harmony.Patch`，即在战斗运行中动态重写首次遇到的具体卡牌`OnPlay`方法。
- 删除`EscapeEffectPatcher`及所有运行时具体卡牌方法补丁；`ControlPower`改为实现RitsuLib 0.4.64的`ICardOnPlayHookListener`，在统一`CardModel.OnPlayWrapper`内仅对精确登记的投影实例抑制原始`OnPlay`，保留费用、原卡实例、打出与结算生命周期。
- `MagicResonance`数值测试新增能力牌拘束场景，核对投影成功、原效果不生效、1费减少1点挣脱值并进入弃牌堆；结构验证禁止重新引入动态`Harmony.Patch`，手测清单增加三类拘束执行时不得卡死或闪退。
- 无部署构建和显式部署构建均通过全部验证门（0 warning、0 error）；已部署至`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`。源码与安装目录DLL SHA-256均为`8CA155E2724270AE711FCC64342E3BE8E3812847C9F591234C06D93611BD0B84`，PDB均为`313E576D8817B2EFB7C45EA316E7335998335EAD77FEA1EE0A4C71E676E260F1`；修复后运行时复测待完成。

## 2026-09-09 — 204张卡牌第三轮实测修复

- 变更前运行时快照：`maiden-card-test-third-run-20260909` → `bb95c33`；第三轮 F10 报告于20:04:21开始、20:11:53正常结束，结果为200张通过、1张失败、3张`DESIGN_PENDING`，不属于卡死或中断。
- 唯一失败项战技复读已完成生命周期定位：当前游戏在复制牌仍位于`PileType.Play`时触发`AfterCardPlayed`；旧实现立即Transform后，恢复牌滞留打出区，而原版结算器随后只尝试移动已经被移除的旧实例。
- 战技复读Power改为在`AfterCardPlayed`登记待恢复实例，并在原版把复制牌从打出区移入弃牌堆或消耗牌堆、触发`AfterCardChangedPiles`后原地恢复；升级状态和永久牌组来源继续保留。
- 测试新增恢复牌类型、升级状态及打出区清空断言；构建门新增战技复读必须在离开`PileType.Play`后恢复的生命周期检查。
- 无部署构建及全部验证门通过（0 warning、0 error）；部署因测试后的游戏进程仍锁定已安装DLL而未写入，第四轮游戏内结果待完成。

## 2026-09-08 — 204张卡牌第二轮实测修复

- 变更前运行时快照：`maiden-card-test-second-run-20260908` → `05c1f9c`；第二轮 F10 报告于16:03:55开始、16:11:33正常结束，结果由首轮150张通过提升至197张通过、4张失败、3张`DESIGN_PENDING`，不属于卡死或中断。
- 按最新游戏日志和当前v0.111.0游戏DLL复核：原版`CreatureCmd.Stun`仅在当前意图允许转移时调用`SetMoveImmediate`，遇到尚未执行的本模组临时色情意图会静默不生效；本模组原强制眩晕又使用非原版识别的状态ID，导致`Creature.IsStunned`为假。
- 新增统一眩晕桥接：正常情况保留原版眩晕流程；若临时色情意图阻止切换，则强制替换；强制状态使用引擎识别的稳定ID `STUNNED`。剑之裁决、妨碍射击、欲望鞭挞和咬统一改走该路径。
- 测试场景复位会重建每个怪物的状态机，确保前一张牌留下的临时意图不污染后一张牌；战技复读改为执行真实`BeforeCombatStart`并验证Power、牌面变形、保留和打出后恢复；诱敌深入临时加入并清理具备拘束目录项的原版多尼斯异鸟，不再错误选择无拘束意图的小啃兽。
- 补足此前遗漏的欲望鞭挞条件测试：建立真实拘束意图后同时核对7点伤害和击晕，而非只验证伤害。构建门新增`STUNNED`状态ID、临时意图回退、卡牌统一眩晕入口及欲望鞭挞条件断言检查。
- 静态构建验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过，0 warning、0 error；内容契约、结构契约、204张卡文本审计、本地化格式契约和卡牌测试结构门全部通过。
- 游戏内结果：等待部署后的第三轮完整 F10 数值测试；当前不把静态构建通过记作201张实际效果全部通过。
- 部署：`dotnet build -c Debug -p:DeployMod=true -p:ValidateMod=true`通过，0 warning、0 error；已部署至`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`。源码与安装目录DLL SHA-256均为`F146D0E722D76115C1D391BAFDBF74A976E8EC8D3CF984946DE7C263968C74BC`，PDB均为`699C287A52C373FC924DD4F91087B0C425AF164812CC71824D23327A2ECAF80C`。

## 2026-09-08 — 204张卡牌首轮实测修复

- 变更前运行时快照：`maiden-card-test-first-run-20260908` → `409eeeb`；首轮 F10 报告正常完成，结果为150张通过、51张失败、3张`DESIGN_PENDING`，手牌区大量卡牌是测试生成节点残留而非卡死。
- 补齐17张侵犯诅咒的已确认效果：打出后从战斗和对应永久牌组实例移除，并分别结算欲望、负面状态、生命、魔装耐久、下回合能量、随机状态及衍生牌生成。
- 补齐6张已确认事件诅咒的效果：三档淫纹回合末生成发情，痴情限制攻击牌，口球增加技能耗能，衣装透明通过现有精确关键词getter投影使其他手牌临时获得虚无；`HypnosisCurse`和`ClimaxBanCurse`仍按设计待定跳过。
- 修复真实实现偏差：忏悔斩读取正确的`FrailPower`变量；情人匕首按目标实际眩晕状态加倍；沉溺快感升级不再擅自增加3点格挡；献祭狂热升级基础伤害由错误的18修正为DesignDoc基线16。
- 修正测试误判：动态费用/出牌限制先把被测牌放入手牌；变形场景创建真实`NCard`；圣言共鸣触发回合末圣言结算；研究计划走原版起手抽牌Hook；魔装恢复效果先建立天衣形态；精液转化执行真实资源支付；万咒镰刀建立并核对永久牌组实例；诱入深渊核对怪物下一行动的拘束意图而非立即施加Power；伤害测试使用不改变伤害倍率的同层Power夹具。
- 测试复位现在对存在视觉节点的牌走原版可视移除路径，再无视觉移除其他夹具，防止完整测试后手牌区残留大量卡牌。
- 构建验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过，0 warning、0 error；内容契约、结构契约、204张卡文本审计、本地化格式契约和卡牌测试结构门全部通过。
- 游戏内结果：等待部署后的第二轮完整 F10 数值测试；尚不将静态构建通过视为201张实际效果全部通过。

## 2026-09-08 — 卡牌效果测试人工进战斗/F10触发整改

- 变更前快照：`maiden-pre-card-effect-runtime-test-20260908` → `f1ef1c3152c5019a663b42688b44623a874af5fa`。
- 按工作区 `sts2_contrib_tests` 的成熟运行边界调整：不再要求自动化负责启动游戏或导航进战斗；玩家使用响木天音进入可放弃的单人战斗后按 `F10`，即在 Godot 主线程启动全部204张牌的精确效果测试。
- `F10`入口检查战斗状态、单人模式和角色身份，并阻止重复并发启动；原`ms_test_cards confirm [all|CardTypeName]`入口继续用于单牌复测。
- 修复当前游戏v0.111.0中Debug专用`PlayerCmd.EndTurn`拦截器的Harmony签名：该方法返回`void`，前缀不再声明错误的`Task __result`，避免测试补丁安装失败。
- 结构验证新增人工进战斗/F10触发门，确保热键入口及其战斗/角色保护不会被意外移除。
- 构建验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过，0 warning、0 error；内容契约、结构契约、204张卡文本审计、本地化格式和卡牌测试结构门全部通过。受限沙箱首次还原因NuGet网络访问失败，按用户约束允许`dotnet build`访问包源后成功还原并完成验证。
- 部署：`dotnet build -c Debug -p:DeployMod=true -p:ValidateMod=true`通过，0 warning、0 error；已部署至`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`。源码与安装目录的DLL SHA-256均为`23B99E680E2F2FC4A8D43B2E3B9F396835ACAD60BC235B152AADCE0C6B1533C6`，PDB均为`211DD55A7125E2C29AAC5A05D6AF21968D4318C5B6FD790C2FF2F09693ECCE09`。
- 游戏内结果：等待玩家启动游戏、进入响木天音单人战斗并按`F10`后生成实际数值报告。

## 2026-09-08 — 204张卡牌游戏内数值效果测试框架

- 变更前快照：`maiden-pre-card-effect-tests-20260907` → `34c864d4ed413a0fb377b8dbec263a50f071b43e`。
- 新增Debug专用游戏内命令`ms_test_cards confirm [all|CardTypeName]`；命令要求响木天音单人战斗和显式破坏性确认令牌。
- 精确登记内容契约中的204张牌；201张具有真实结算断言，`ClimaxBanCurse`、`HypnosisCurse`以及用户明确要求暂时跳过的`DreamMist`记为`DESIGN_PENDING`，不会计为通过。
- 测试预期固定为2026-08-24需求基线，不从实现侧`DynamicVars`反推；基础版、升级版及关键条件分支分别执行。
- 断言覆盖伤害、格挡、抽牌、能量、欲望、堕落值、Power层数、牌堆迁移、升级、关键字、附魔、怪物意图，以及能力牌的后续抽牌、回合开始/结束、消耗、圣言触发、断罪审判、变身和欲望资源钩子。
- `Rest`仅在该测试场景的作用域内拦截并记录`PlayerCmd.EndTurn`，避免自动测试推进当前战斗；其他效果仍由真实游戏命令结算。
- JSON报告写入已加载程序集旁的`card-effect-test-results/`，保留每条断言的预期值、实际值和运行时异常。
- 新增`ValidateCardEffectTests.ps1`并纳入`ValidateMod=true`：校验204张精确登记、201张可执行、3张明确待设计、基础/升级覆盖、非零效果断言和禁止方法存在性占位测试。
- 验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过；0 error；内容契约、结构契约、204张卡文本审计、本地化格式契约和卡牌测试结构契约均通过。由于受限环境无法访问NuGet漏洞元数据端点，产生2个`NU1900`警告；依赖均从本地缓存成功还原，未影响编译或验证。
- 部署：否；游戏内204张完整运行结果需由用户在专用测试跑局执行命令后查看JSON报告。

## 2026-09-07 — 中文描述与悬停格式版本部署

- 部署前快照：`53b5f190eb1e3cf86c4dec1aeafa7118266132e0`（`refactor(maiden): standardize localization hover formatting`）。
- 执行：`dotnet build -c Debug -p:DeployMod=true -p:ValidateMod=true`。
- 验证：构建0 warning、0 error；204张卡文本审计0 failure；内容契约、结构契约和本地化格式契约全部通过。
- 产物校验：DLL、PDB和manifest与源码构建产物SHA-256一致；33个资源文件在安装目录和Godot热加载目录均为0缺失、0哈希差异。
- 安装目录：`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`。
- 热加载目录：`D:\game_backup\steam\steamapps\common\Slay the Spire 2\MaidenSuccubus`。
- 部署：是；等待用户进行游戏内手动测试。

## 2026-09-07 — 中文描述与悬停格式整改

- 变更前快照：`maiden-pre-localization-format-20260907` → `0bf43eb7c42e9d0c37f61ab6ee6f0bc961b07e47`。
- 需求边界：仅统一卡牌、关键字、Power、附魔与静态悬停的格式和悬停数据，不同步2026-09-02尚未确认的卡牌语义变化。
- 以原版卡面组装、悬停聚合、Power静态/动态描述机制为主基准，以HornetMod 1.2.15的RitsuLib实现为补充，新增`docs/LOCALIZATION_STYLE_GUIDE.md`。
- 统一中文机械术语的金色标注、独立效果换行和数字/量词间距；保留SmartFormat变量、升级分支和既有效果顺序。
- 修复Power canonical悬停中的裸`{Amount}`，补齐遗留Power标题，并让“异常适应”的剩余触发次数通过DynamicVar进入实例悬停。
- 为“拘束”描述显式注入当前剩余挣脱值，避免状态悬停显示裸模板。
- 新增本地化格式验证门，并纳入`ValidateMod=true`构建流程。
- 将“每轮写操作必须具备变更前快照、changelog、范围化Git提交和验证结果”写入本目录`AGENTS.md`。
- 验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过；0 warning、0 error；204张卡文本审计0 failure；内容契约、结构契约和本地化格式契约全部通过。另以变更前提交逐键剥离富文本与换行对比，卡牌正文、附魔正文及既有Power动态描述均为0处语义差异。
- 部署：否。
