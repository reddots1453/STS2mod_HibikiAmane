# Changelog

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
