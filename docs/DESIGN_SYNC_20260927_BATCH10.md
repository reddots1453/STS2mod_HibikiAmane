# DS27-04C：英雄、永恒宝珠与初始遗物切换

实施日期2026-09-28，前置快照`b7e8794b`，设计确认提交`3379d42d`。本批IMPLEMENTED，未游戏内验收，未部署。设计逐行/词级检查无未同步漂移；完整重读初始遗物章节。只推进当前初始遗物批次，不声明整个DesignDoc同步目标完成。

## 实现入口

- `src/Relics/RetentionOrbs.cs`：英雄/永恒模型及`BeforeFlushLate`原版阶段，选择真正的手牌战斗实例。英雄在-3～3单回合保留，>=4单回合保留并升级，<=-4添加保留关键词；永恒添加保留、升级并仅在无附魔时附加原版`SlumberingEssence`。已升级、已有保留、已有其他附魔均可选；不覆盖附魔、不修改DeckVersion。
- `src/Core/Relics/RetentionOrbRules.cs`：生产规则；离线测试直接链接同一源文件，使用独立11档结果表作为断言，不复制算法作期望值。
- `CombatEnchantmentCmd.ApplyVanilla`审计白名单增加原版沉眠精华，复用已有附魔通知/VFX。核对当前游戏DLL，原版`BeforeFlush`通过`AddUntilPlayed(-1)`减费，直到打出时恢复。本遗物在该阶段之后选择，新附魔错过当轮监听快照，因此调用新附魔的真实同步回调一次；已有附魔不补调。没有复制减费算法。ThreadStatic投影抑制范围内不await。
- `TwinSoulChaliceDescriptionPatch`扩展HeroOrb变奏描述，所有文案逐字比对DesignDoc，包括标点；永恒使用固定描述。Hero注册原版欧洛巴斯之触→Eternal映射，稀有度分别Starter/Ancient。原版保留与沉眠精华悬停提示复用。
- 新遗物图标目前与其他宝珠一致，使用原版占位资源，未生成或变更素材。未依赖其他Mod图标。

## 选角、同步与旧存档

- `src/UI/StarterRelicSelector.cs`：选角原生遗物栏后插入左右按钮，中间显示初始遗物名称；原生名称/描述/图标同步更新。默认全能，另选英雄，不另开进局弹窗。控件由选角屏幕拥有，CWT缓存，没有每帧轮询、进程级共享选择或全局UI事件订阅。
- `StarterRelicSelectorUiPatch`分别挂接SelectCharacter、准备/取消准备、PlayerChanged、BeginRun和关闭；每个方法单一Harmony目标，统一Safe.Run。其他角色、锁定/随机选择不显示；已准备禁止修改，启程或退出隐藏。支持按钮键盘焦点与左右邻接；实际手柄导航仍需手测。
- `src/Data/StarterRelicChoice.cs`和Mod注册：`starter_relic_choice`按玩家保存Kind/Applied，`SyncLobbyOnChange=true`，写入仅本地玩家bucket。未知枚举回退全能，不影响别的玩家。
- `StarterRelicSelectionPatch`：同步Prefix位于`RunManager.FinalizeStartingRelics`。核对RitsuLib的InitializeNewRun前置导入流程，确保大厅权威快照已进入run。替换默认初始实例时保留顺序和获得层数，不自己调用Obtain/Replace，交由原版finalizer统一AfterObtained；Applied避免二次替换。原始遗物已不在时不凭空补发，加载存档不走该补丁。
- 旧TwinSoulChalice、BalancedLens模型身份未重命名。第四层监听继续沿用上一批的独立角色生命周期，不依赖是否选择英雄。
- 既有`ExtraStartProfiles=false`未改变。选择器兼容派生角色模型，但圣女/魅魔额外开局UI尚未开放，不把本批表述为三开局功能全部完成。

## 已执行验证

均在Mod目录执行：

- `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore`：0警告0错误。
- `dotnet run --project tests/DesignSyncContracts/DesignSyncContracts.csproj --no-restore`：新增66条保留规则断言，累计290条通过。
- `python -m unittest discover -s scripts -p 'Test*20260927.py'`：67项通过，本批增加10项。
- `python scripts/TestCardLocalizationAudit.py`：19项通过，合计86项离线检查。
- `ValidateCardEffectTests.ps1`、`ValidateMvpContent.ps1`、`ValidateLocalizationStyle.ps1`、`ValidateStructuralContracts.ps1`，参数`-ProjectDir .`：通过。225注册卡、28遗物、9自定义附魔；沉眠精华直接复用原版，不计入自定义附魔数量。
- `ValidateVisualAssets.ps1 -ProjectDir .`：既有第317行“Temptation meter must use the formal lipstick with an independent value label”失败。未修改相关UI、未放宽检查，不能称全部验证门通过。

上述Python主要是源码/本地化/结构契约，不替代运行时交互验证。C#离线宿主真实执行生产分支函数，但也不等同于游戏回合、UI和联网流程验收。

## 游戏内自动入口（已编译，未执行）

两入口都会破坏当前测试局，必须用一次性存档；单人、本Mod角色、显式`confirm`，finally恢复TestMode。

`ms_test_retention_orbs confirm`仅战斗中使用：

- 两宝珠×11堕落值，真正`Hook.BeforeFlush`、原版CardSelect/TestCardSelector与命令链；实际格式化描述逐字断言。
- 只改被选牌、单回合与关键词保留的清理差异、升级条件、DeckVersion不受影响、原版沉眠精华及首次减费一次。
- 第二回合不替换/重叠附魔，实际打出后恢复until-played费用；空手不弹选择、已升级/保留/其他附魔、外部玩家回调无影响。
- 原版TouchOfOrobas真正Setup/Obtain，将英雄替换为永恒。

扩展`ms_test_orbs confirm`，仅战斗外使用：

- 实际Harmony.GetPatchInfo检查六个选角补丁及同步初始化Prefix已安装，不只检查源代码存在。
- 全能、英雄与非法枚举，初始化顺序/获得层数/数量保持；Applied防重入；默认遗物已替换时不覆盖；其他角色不受影响。
- 保留上一批真实奖励选择、恢复/最大生命、先古替换与独立试炼监听测试。

## 尚未完成的游戏内验收

- 自然回合末选择、单张/空手、保留/升级/附魔动画、退出战斗取消选择、与原版其他保留遗物/虚无等交互。
- 临时转为挣脱的牌在投影抑制下保留/升级/附魔，解除后恢复；本批保留抑制边界，但未声称该组合已经实测。
- 两种遗物选择的屏幕比例/字体/鼠标/键盘/手柄，锁定/随机/其他角色切换，准备/取消/退回/重新进入。
- 多人客户端各选不同遗物、准备竞态、重连与主机权威快照，确认不串玩家；并非联网实测完成。
- 保存退出重进、旧档不被替换、先古替换后描述/费用/第四层监听仍正常。

下一批仍需剩余普通卡牌/附魔、角色遗物/事件、第四层新试炼及全量文案回归。无关并行素材、共享审计报告与其他Agent日志不纳入本批提交。
