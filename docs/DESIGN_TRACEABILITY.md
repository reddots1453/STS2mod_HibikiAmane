# DesignDoc需求追踪矩阵

## 2026-10-02 先古之民正式对话（ANCIENT-DIALOGUE-20261002，IMPLEMENTED 待实测）

用户确认将审阅稿正式加入游戏；DOC-ANCIENT-DIALOGUE-001 → 既有AncientDialoguePatch登记及localization/zhs/ancients.json → ANCIENT-DIALOGUE-01（9位、27组、57句、30按钮、标签闭合）、02（拜访/重复/建筑师攻击）、03（游戏显示/翻页/富文本）。本轮仅落地叙事，无新奖励、休息或契约机制。保留既有用户DesignDoc修改及共享暂存。安装PCK仅替换先古对话一个条目，其他资源与安装DLL/PDB/清单保持；详见docs/ANCIENT_DIALOGUES_20261002.md。运行时验收NOT_RUN。


## 2026-10-02 HibikiAmane累计修复上传准备（UPLOADER-RELEASE-20261002，文件部署完成、游戏内待验）

用户要求本轮所有bug清单并将文件部署到ModUploader准备上传，授权覆盖先前“暂不部署”的上传器文件限制。本轮不部署游戏安装目录，不执行Steam发布。前置快照`61d03721f38e62e514bf9a8b6f5c365374ed4df9`；DesignDoc逐行/词级差异仍为此前已同步内容，未改玩法设计。完整18项累计修订、尚未修复的继续黑屏及实测边界见docs/RELEASE_FIXES_20261002.md。

Release `dotnet build MaidenSuccubus.csproj -c Release -p:DeployMod=false -p:ValidateMod=false --no-restore`成功，0警告0错误；未运行静态测试。PCK重建648资源并完成路径/大小/哈希核验，资源新增变化为characters本地化。官方content/MaidenSuccubus仅同名DLL/has_pck=true JSON/PCK，复制后哈希一致。workshop标题改为HibikiAmane，changeNote记录修订，保留mod_id、预览图和其他属性；旧三文件及配置备份于聊天work/uploader-release-20261002/uploader-before。产物和deployment.json保存于outputs/hibiki-amane-uploader-release-20261002。UPLOADER-RELEASE-01文件部署完成；各玩法验收仍IMPLEMENTED待实测，继续黑屏仅定位，不标VERIFIED。

## 2026-10-02 变化获得卡牌的拾起附魔（PICKUP-TRANSFORM-20261002，IMPLEMENTED 待实测）

CARD-N附魔输入与输出/娅露斯的记忆拾起规则 → 用户报告变化生成卡牌不触发拾起附魔 → PickupEnchantmentCmd.IsPickupOrDeckTransformation及MagicSword/GaleSword/ShiningSword/YarusMemory → PICKUP-TRANSFORM-01/02/03。原生0.107.1和0.111.0的CardCmd.Transform在替换卡插入永久Deck后，AfterCardChangedPiles传入原牌堆Deck；旧代码仅接受None，确实跳过附魔。统一接受None→Deck普通拾起和Deck→Deck变化回调，目标必须是自己。沿用原回调和同步，不全局改原版Hook、不重放所有模型监听、不另开异步选牌任务。三剑各自充能2/迅捷2/活力3及娅露斯的记忆自身与另选牌附魔不变，原已附魔检查与SavedProperty一次性标记保留，战斗生成/战斗内变化不视为拾起。加速运动拾起复制及旧版本黑暗风暴字段不属于本次附魔修复。

PICKUP-TRANSFORM-01：事件/遗物普通及多张变化到三剑，升级/未升级均附魔一次且数值正确。02：变化到娅露斯的记忆进入原牌组选取，完成灵魂联结。03：普通奖励/商店仍触发，重复通知、旧存档加载、抽弃移动及战斗生成不追加附魔，已有其他附魔保留。前置快照`011af5d0ed75802b2893186e278c085270bb996b`；DesignDoc本轮逐行/词级差异仍为此前已同步内容，不覆盖用户设计。用户要求不运行静态测试、暂不部署。Debug `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 构建成功，0警告0错误；未运行静态测试、未部署。DLL/PDB、更新后的manifest及characters本地化保存于聊天outputs/pickup-transform-debug-20261002。下一次资源部署需包含更新本地化或重建PCK；游戏内待验。

## 2026-10-02 正式模组显示名与设置文案（HIBIKI-NAME-20261002，IMPLEMENTED 待实测）

用户直接确认正式模组名称为HibikiAmane，并要求RitsuLib设置中的“成人”全部改为“瑟瑟”；仅品牌和文案，无玩法语义变更。统一manifest name、试炼设置主标题/模组名、演出设置模组名、上报授权申请显示名、初始化日志和额外开局卡池提示。ModDisplayName为代码唯一名称常量；角色本名仍为响木天音。五处演出设置标题/开关/音量/帮助文字替换为“瑟瑟”。保留ModId、DLL/程序集名、模型ID、资源路径、设置键及存档字段，避免既有存档/开关偏好失效。

HIBIKI-NAME-01：RitsuLib所有本mod设置及授权界面显示HibikiAmane，演出文案显示“瑟瑟”。02：已保存设置和旧跑局仍通过原标识读取，游戏mod列表显示新名称。变更前快照`aab215b5deca36286bb53374c127618a8f16ce16`；用户要求暂不部署、不运行静态测试。Debug `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 构建成功，0警告0错误；未运行静态测试、未部署。DLL/PDB、更新后的manifest及characters本地化保存于聊天outputs/hibiki-amane-debug-20261002。下一次资源部署需包含更新本地化或重建PCK；游戏内待验。

## 2026-10-02 内部运行状态的图标边界（INTERNAL-POWER-UI-20261002，IMPLEMENTED 待实测）

SYS-CTL/INV及MON-ERO-CATALOG状态持久化 → 既有内部Power的IsVisibleInternal=false契约 → MaidenInternalPowerUi / MaidenInternalPowerVisibilityPatch / MaidenInternalPowerContainerPatch / MaidenInternalPowerHoverTipsPatch → INTERNAL-POWER-UI-01/02/03。截图证明血条下内部EroticIntentRuntimePower以兜底红剑和未翻译键公开；以原生Power可见性和容器入栏两层过滤修正，不给内部载体伪造公开描述或功能图标。四个已隐藏载体均受保护，保存字段、实际阈值Power和上方原生/自定义意图均保留。源码与0.107.1/0.111.0布局核对，第三方环境具体绕过途径及运行结果未确认。暂不部署、不运行静态测试；Debug `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 成功，0警告0错误，谦逊目录786项、0项不支持。DLL/PDB另存聊天outputs/internal-power-ui-debug-20261002；未运行静态测试，未部署、未更新上传器，内部状态隐藏与谦逊手牌界面待用户实测。


## 2026-10-02 谦逊原生手牌界面（HUMILITY-HAND-20261002，IMPLEMENTED 待实测）

ACT4-001谦逊 → 用户确认旧网格选牌正常并要求手牌界面 → HumilityLesson.OnPlay原生CardSelectCmd.FromHand / RequireManualConfirmation / PretendCardsCanBePlayed → HUMILITY-HAND-01/02/03。沿用本玩家选择上下文和原生手牌/多人同步，不改目标牌身份、伤害格挡抽取、附魔、升级费用或关键词。保留零候选跳过及选后归属/牌堆/类型复核。新增选取入口/返回日志供悬停再现定位；旧悬停根因未证实，不声称本次已经通过运行时验收。手牌界面待实测；不执行静态测试、暂不部署，Debug `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 成功，0警告0错误，谦逊目录786项、0项不支持。DLL/PDB另存聊天outputs/humility-hand-debug-20261002；未运行静态测试，未部署、未更新上传器，新手牌界面待用户实测。


## 2026-10-02 重启继续游戏黑屏定位（CONTINUE-DIAG-20261002，定位实现完成，黑屏尚未确认修复）

玩家确认 `godot (1).log` 在继续黑屏后导出，等待数分钟仍未进入。日志为游戏0.107.1/RitsuLib0.6.3，启动到主菜单约167秒，最后只有 Common 772资源+92VFX预加载开始，没有完成标记、Continuing run记录或本模组读档异常。约107个mod及跨版本适配同时启用；133个本mod补丁安装，三个版本相关补丁失败不作为黑屏根因。现有证据不能判定具体阻塞资源/存档步骤，不能标修复或VERIFIED。

按已有存读档/ACT4-001稳定生命周期要求补充 `[ContinueDiag]` 本地日志：只对天音继续流程观察反序列化、读档初始化、重载次数保存、跑局/幕资源、地图生成和最后房间加载；保留原Task及异常传播、不给等待超时强制成功、不跳过存档或奖励。Common启动资源预加载另有低频观察，主线程每秒缓存队列计数/前三个路径及当前VFX，背景每30秒最多20次报告停顿阶段与最后处理时间，不跨线程访问Godot或枚举原生队列、不序列化玩家存档、不增加网络上传。

CONTINUE-DIAG-01：正常重启继续完成，日志记录各阶段而且原状态/遗物不变。CONTINUE-DIAG-02：卡住时30秒后记录当前阶段、加载路径和主线程快照时间，可区分初始化/云保存等待/资源队列停顿。CONTINUE-DIAG-03：其他角色不创建继续流程跟踪，失败仍走原版错误处理。两个游戏版本运行时待用户实测；不运行静态测试。Debug `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 成功，0警告0错误。定位DLL/PDB保存在聊天outputs/continue-diagnostics-debug-20261002；游戏进程19840仍在运行，未替换安装DLL，用户随后明确要求暂不部署，保留当前安装版本。本轮是定位版本，不宣称黑屏已修复。


## 2026-10-02 卡牌预览痛击回退（PORTRAIT-PREVIEW-20261002，IMPLEMENTED 待实机复验）

用户已确认完成版卡图及现有 PROFILE-ART/卡图要求 → CardArtAssets现有按类型取图 → MaidenCardModelPortraitPatch.Prefix/Postfix、MaidenCardPortraitRefreshPatch、MaidenCardVisualRefreshPatch、原先Reload/大图补丁 → PORTRAIT-PREVIEW-01/02/03。保留第三方大图需要的压缩路径兜底，原生实际纹理从真实图缓存提供；补齐原生UpdatePortrait及完整UpdateVisuals后的赋图，避免预览或升级刷新把图重置成统一痛击。日志证实PCK加载、RitsuLib0.6.4，未给出纹理解码失败，不宣称已证实单一框架根因。LibraryAuraOverlay过滤canonical模型后才访问Owner，消除同份日志中的预览警告。普通/先古、其他角色隔离、两个RitsuLib版本游戏内待验；未执行静态测试。Debug编译0警告0错误，按本轮追加授权部署至游戏mods/MaidenSuccubus，DLL/PDB/JSON/PCK哈希复核通过，未更新上传器。


## 2026-10-01 试炼奖励兼容与可延后领取（TRIAL-REWARD-20261001，IMPLEMENTED 待实机复验）

`ACT4-001` → `FourthRouteTrialRelicReward`（固定 Reward，内部复用原生 RelicReward 视觉）、`FourthRouteProgressService.ClaimInitialReward`（旧阶段替换、重复目标去重、收据推进）、`FourthRouteRewardScreen.Presentations`（玩家/房间/收据单次提示）、`FourthRouteRewardFlow`/`FourthRouteQuestSelectionPatch`（奖励不再锁地图） → TRIAL-REWARD-01/02/03。证据：玩家日志中 RelicRewardChoices 连续替换/领取同一谦逊遗物，随后本mod仍 reward=True。用户要求修复重复发放与未领取无法走图，覆盖旧“不可关闭领奖”实现；原生奖励界面可关闭但持久收据不消费，下个房间/读档恢复，领奖只获得一份目标阶段遗物并推进一次。旧存档重复遗物可在本次领取时收口，不撤销此前已经作用于牌组等对象的拾取效果。兼容策略不依赖或禁用 RelicRewardChoices，原生 Reward 同步/选择/关闭协议保持适用；仅本mod与同时启用遗物选择mod两个环境均需实测。Debug 无部署编译0警告0错误，源码实现待玩家验证，无静态测试、无部署。


## 2026-10-01 节制递归链（TEMPERANCE-CHAIN-20261001，IMPLEMENTED 待实机复验）

`ACT4-001` 节制 → `TemperancePileCmd.Play` / `PlayChain.Reserved` → `TEMPERANCE-CHAIN-01/02/03`。用户已确认实施，取代上一轮“仅分析、暂不修改”的结论。沿用原版 `CardPileCmd.Add` / `CardCmd.AutoPlay`，整批实例先入 Play 再逐张执行；嵌套调用共享实例预留记录直到顶层退出，阻止同一实例回堆后在同链再次被选中，保留首次打出其他节制牌以及后续独立结算。空候选跳过选择；未开打的暂存牌在能力失效时归还原堆。状态仅为代码实现，游戏内由用户验证，不标 VERIFIED。


## 2026-10-01 玩家测试回修（PLAYER-BUGFIX-20261001，IMPLEMENTED 待实机复验）

- `CARD-C` 燃烧描述 → `BurningPower.BeforeAttack`：攻击者在原生攻击动画/命中前承受1次燃烧，非攻击伤害不触发；多段攻击按单次攻击命令结算。验收：带燃烧怪物攻击前先扣血，致死时攻击取消。
- `SYS-COR-002` 处女跨层 → `CorruptionActLifecycle.AfterActEntered` 运行订阅：2/3/4层且仍有标记时各减1堕落值，按层触发ID防重复；不依赖女神试炼开关。验收：普通/禁用试炼跑局进入下一层时数值与反馈同步，重复进入同一层不再减。
- `ACT4-001` 嫉妒试炼 → `FourthRouteLifecycle.BeforeCardRemoved`：原版永久删牌钩子计进度，普通战斗牌堆移动不计；仁爱入牌仍用 `AfterCardChangedPiles`。验收：商店/事件删牌后1/1并推进奖励，战斗中消耗不计。
- `ACT4-001` 谦逊 → `HumilityLesson.OnPlay` 以 `CardSelectCmd.FromSimpleGrid` 展示手牌攻击/技能，返回后校验仍在手牌再改写；避免此前手牌选择模式不弹出、卡牌悬停。生成目录786项可抽取、0项不支持；实机选择与目标效果待验。
- `ACT4-001` 节制 → `LibraryPileChoice.Title`/`GetTitleFragments` 为三个实例各自绑定抽牌堆、弃牌堆、消耗牌堆名称。戒／环相互自动打出的无限链只记录候选方案，不改玩法，待用户确认后实施。


## 2026-10-01 四张卡与本地授权队列（CARD-TELEM-20261001，IMPLEMENTED 待实机验收）

`CARD-C` 咬 → `BiteInvader.OnUpgrade` 增加 Retain；已有目标选择补丁仅允许 `InvasionIntent`。`CardEffectTestCatalog.BiteInvaderProbe` 与 `DesignSyncStatusTextContract` 覆盖基础/升级，未执行游戏内测试。

`CARD-N` 火焰绽放、魔法师的秘诀、泡澡及 `CARD-H-500～599` 太阳之舞 → Plan `CARD-TELEM-20261001` → 对应四卡 OnPlay/升级、中文本地化、CardEffectTestCatalog 与文本/费用契约。RitsuLib 本地队列 → `MaidenTelemetry.Register`，用户授权 `run_history` 后只捕获 `MAIDEN_SUCCUBUS_CHARACTER` 类别的已结束跑局；无接收端时不上传。验收：四张卡及队列变更的 Debug 全部校验通过；追加“咬”后按用户要求只编译，不再运行静态测试。基础/升级牌效与授权页/队列/撤销行为尚未实机执行。


## 2026-10-01 理外锻成全附魔手牌跳过（FORGE-ALL-ENCHANTED-20261001，IMPLEMENTED 待复验）

CARD-N-400～499／SYS-ENC-001 → BeyondReasonForge.OnPlay → DesignSyncForgeContract：出牌前全附魔手牌零提示、零选择随机消耗并正常消耗卡牌；首段选择后目标全部失效时不启动第二段选牌。光之翼仅在仍存在普通未附魔手牌时保留多重附魔路径。日志中的附魔图标纹理转换异常由 EnchantmentTextureCompatibilityPatch 修复，安装游戏仍是旧版，未实机复验。

## 2026-10-01 运行时缺陷回修（RUNTIME-BUGFIX-20261001，IMPLEMENTED 待复验）

SYS-CTL-001／MON-ERO-CATALOG-001：原生 STUNNED 覆盖未执行的临时色情意图，保留原生击晕回调和恢复状态；新增噬尸蛞蝓回归场景，游戏日志无该战斗异常栈，根因依据 RavenousPower→Creature.StunInternal→SetMoveImmediate 与 MustPerformOnceBeforeTransitioning 的源码控制流确认。拘束格挡后冷却1回合、命中后冷却2回合。CARD-GENERATED-PILE：卡牌、附魔、遗物及敌人生成牌统一预览入抽/弃牌堆。CARD-MAGIC-SWORD：奖励大图的自定义附魔图标避开 ImageTexture 到 CompressedTexture2D 的异常转换。完整 Debug/Release 构建与18场景结构门通过；运行时未复验，状态保持 IMPLEMENTED。


## 2026-10-01 已完成卡图漏映射修复（FORMAL-CARD-ALIGN-20261001，IMPLEMENTED 待复验）

玩家报告变身、防御等卡仍显示默认或旧图。审计发现完成版卡图根目录有19张对应有效卡牌类的PNG未写入manifest；此前171张哈希门仅检查已登记项，因此未发现漏登。把这19张写入清单并按类名复制到运行时，使正式卡图达到190张加默认图；旧版内观与现行武神的呼吸同类，移入superseded保存，不参与运行时映射。视觉资源门现要求完成版目录全部PNG均有清单项，运行时卡图数与类名集合也必须精确一致。190张正式卡图及默认图的源/运行时映射与哈希校验通过；完整编译0警告0错误，二次部署到安装目录及游戏资源镜像后哈希复核通过。游戏内显示待复验。


## 2026-10-01 选角V4素材与完成版卡图核对（SELECT-V4-CARDS-20261001，IMPLEMENTED 待复验）

用户追加要求选角大图和角色图标使用最新素材，并更新完成版卡图。选角界面切到V4原作画风的2561×1201背景及132×195正常/锁定图标；原V2资源保留为兼容别名。完成版卡图以已验收manifest为唯一来源，逐张检查最新171张及默认图的源哈希与运行时副本；无差异时不重复改写美术内容。未来定向部署预检纳入V4三图及全量正式卡图。游戏内选角显示和卡图更新待复验；完整编译与内容/结构/视觉资源门通过（0警告、0错误）；49套遗物图标、9枚附魔图标及定向部署预检通过。已按用户授权部署至游戏目录，安装及资源镜像哈希复核通过；游戏内显示待复验。


## 2026-10-01 遗物与附魔图标接入（RELIC-ENCHANT-ICONS-20261001，IMPLEMENTED 待复验）

前置快照979361fb9484681dba166930f677c6a7f47dc9bd；目标旧文件逐项备份。DesignDoc逐行/词级复核为既有邪瘴文案和娅露丝书库章节移动，本批不改设计工作树。用户确认43枚V3遗物图标及9枚V1附魔图标进入运行时。遗物模型绑定各自小图、大图和轮廓；十四路线遗物及碎片随路线选择对应图标，清/浊项链随实际变奏选择图标。未有V3图的旧存档/占位遗物使用V1图，枯木树枝保留DesignDoc指定的STS1图。附魔中能量过载只兼容旧存档，LayeredEnchantment容器继续复用原生图。资源清单、正式源哈希、定向部署预检和完整视觉资源门同步；原生遗物悬停与附魔卡面显示需游戏内复验。171张正式卡图与默认图源/运行时哈希一致；V4选角三图及V2兼容别名资源门、完整编译与定向部署预检通过。已按用户授权部署至游戏目录，安装及资源镜像哈希复核通过；游戏内显示待复验。


## 2026-10-01 堕落值行为原因即时提示（CORRUPTION-FEEDBACK-20261001，IMPLEMENTED 待复验）

前置快照 d337b1b452e9ed6b52e92c17fb1eec4b46c403e1；本批目标旧文件逐项备份于工作区。用户明确要求在触发后说明导致堕落值变化的具体行为与实际变化量。堕落值事件改由独立于顶栏的即时提示呈现，即使事件界面隐藏顶栏也可见；按实际截断后的变化量显示正负数，值未变化不提示。首次侵犯、首次火堆自慰、保持纯洁进入下一幕补充明确来源；既有事件选项、祝福、跨幕选择及试炼来源逐项映射为玩家可读的行为原因。提示在淡入、短暂停留后渐隐，仅对本机天音跑局出现，调试改值不提示。验收：对应行为实际触发后立刻看到‘行为使堕落值 +1/-1’；事件中顶栏隐藏也可见；同一行为不重复提示，边界截断显示实际变化量。验证：独立来源契约覆盖15个原版事件选项、14个命名来源与3处旧未知来源；Debug --no-restore -p:DeployMod=false -p:ValidateMod=true 完整构建及内容、资源、文本、卡牌和意图门通过，0警告0错误。事件弹窗上的实际位置、文字和动画待游戏内复验，保持IMPLEMENTED；按用户要求暂不部署。


## 2026-10-01 音频触发与RitsuLib设置修订（PERF-AUDIO-20261001，IMPLEMENTED 待复验）

前置快照 `5a1a9aacd86979facd5221705a830a726bc02706`，12项目标文件逐项备份；DesignDoc逐行/词级漂移仅有既有邪瘴文案与书库归属，未覆盖设计工作树。用户明确删除施法音和欲望达到8时音效；移除MagicCast及DesireHigh/Heartbeat触发，欲望满值撤下DesireFull音效，最终按用户补充指令，火堆自慰和欲望满值均只播放高潮音；循环音及原满值音不再触发。源音频清单仍保留原16个素材供追溯，但其中5个不再运行时引用。
CG与音频独立开关显示于RitsuLib主菜单、跑局暂停和战斗暂停设置；模组清单description精确改为`playable character`，后续部署必须同步该清单。音量设置保持原有持久化与默认成人关闭。为恢复全量资源门，按已确认正式卡图清单同步最新FlameBloom运行时图；同步源/运行时SHA-256。未来定向部署脚本包含MaidenSuccubus.json。验证：Debug --no-restore -p:DeployMod=false -p:ValidateMod=true 完整构建与内容、结构、154张正式卡图、13CG/16OGG源资源、本地化及卡牌/意图门全部通过，0警告0错误；RitsuLib三类宿主界面可见设置源码契约、已撤音效不再引用与高潮音双触发校验通过。定向部署脚本默认预检通过，未执行Apply。CG/音频实际可见可闻效果需游戏内复验，不标记VERIFIED。 按用户先前要求暂不部署。


## 2026-10-01 选角简介与正式卡图更新（PROFILE-ART-20261001，IMPLEMENTED 待复验）

前置快照 `59a91e09a65742dcde4bf124392c4a0c2a53ea9e`；目标旧资源与文本逐文件备份。用户明确更新选角简介为两句话并保留换行：与魔导书·娅露丝相遇，获得魔法力量的少女。 / 虽然内心依然纯洁，但身体却因为快感而慢慢觉醒。
正式卡图以用户已验收的完成版卡图manifest为唯一依据，共154张及默认图；补齐/替换18张运行时资源，不变更正式素材或候选图。后续定向部署增加characters.json和完整正式卡图清单，预检逐张验证正式源/清单/运行时哈希。该增量按本轮明确指令受理；DesignDoc仅复核，不覆盖设计编辑。
验证：Debug --no-restore -p:DeployMod=false -p:ValidateMod=true 完整构建通过，0警告0错误；内容、结构、154张正式卡图与默认图及HD读取、13CG与16OGG、文本风格、229卡牌和17意图结构门全部通过。20项UI键及格式、含5份完整本地化表与155份卡图的部署预检通过，未执行Apply。
前批试炼/商店修复提交87a0f9e6亦通过最终完整构建；自然事件结束及原生领奖点击、多人流程仍待实机验收。用户暂不部署的要求持续有效。按用户要求暂不部署；真实安装目录与游戏数据未修改。实际选角换行及新卡图显示待游戏内验收。


## 2026-09-30 事件试炼奖励与商店路线补充（TRIAL-SHOP-20260930，IMPLEMENTED 待复验）

前置 529deafea307ebb330d15c3155a44d75de36afc2，目标文件批次备份；DesignDoc逐行/词级发现SYS-TRF-002已明确缩短邪瘴天衣状态说明，按用户文案同步description/smartDescription，规则结算不变；不提交或覆盖设计工作树正在编辑的DesignDoc。
用户追加确认：女神试炼遗物使用原生战利品界面；事件选项异步任务完成且事件已到结束页后才允许打开试炼奖励。采用RelicReward/RewardsSet/NRewardsScreen原生按钮、提示及入栏动画；试炼收据仍唯一领取、不可跳过，不再自行搭建Modal/宝箱按钮，也不更改事件的结束选项。
用户追加确认：商店5个角色卡槽按与卡牌奖励相同的堕落权重及遗物概率加成抽路线，缺圣洁/堕落时优先替换中立槽补足各至少1张；保底优先于极端堕落时对立路线0概率。Native MerchantCardEntry继续生成2攻击/2技能/1能力，保留稀有度、价格、唯一折扣、无重复、无色卡/遗物/药水/删牌和购买回调；使用Shops确定性随机流。此增量需设计工作树回填商店规则，旧“商店不替换”约束由本次明确指令覆盖。
验收TRIAL-SHOP-01：事件完成试炼后保留结束选项，原生战利品只领取一次；TRIAL-SHOP-02：-5至+5及重复种子商店均满足路线保底，其他角色隔离；TRIAL-SHOP-03：邪瘴天衣两类状态提示精确符合新文案。
用户追加：娅露丝的书库InsatiableGreed转入MSNeutralCardPool并继承MSNeutralCard；保持旧模型/存档ID、古老稀有度、尘封魔典来源与相邻持续效果，内容契约按50中立/62堕落同步。验证：Debug无部署编译0警告0错误；DesignSyncContracts共18207断言；奖励6项、书库8项、设计同步14项、UI结构3项通过；19项UI本地化、内容/结构/13CG与16OGG/文本风格/229卡牌/17意图结构门通过。独立游戏副本原生探针验证132组商店（堕落-5至+5，各12种子）路线双保底、5槽类型、无重复及唯一折扣；自身库存生成步骤不改其他商品；娅露丝的书库仅属中立且古老稀有度不变；42阶段原生RelicReward/RewardsSet预览通过。实际事件结束与领奖点击、多人流程待游戏内复验，不能标记VERIFIED。
追加DesignDoc漂移复核：设计工作树已把书库条目移动至中立章节，与用户指令及实现一致；未提交DesignDoc。用户要求暂不部署。完整资源门此前因正式卡图同步缺口失败，该独立资产任务另批处理。用户要求暂不部署；DeployMod=false，未来定向部署清单补powers.json。


## 2026-09-30 选角初始遗物箭头漂移（RELIC-ARROW-20260930，IMPLEMENTED 待复验）

关联 START-002、RELIC-START-002/003；前置提交 02653018e8d6354f0f54bf0795b3efeb1fe64e1d，目标脏文件另存工作区批次备份。
修复 StarterRelicSelector 将原生进阶箭头挂到遗物区域，由 StarterRelicArrowLayout 使用局部左右锚点和固定点击区域；不再在遗物刷新时用屏幕坐标定位。面板入场、父级缩放、宽度变化和图标局部位置变化自动跟随；原生悬停材质仍独立，避免影响进阶按钮。
验收 RELIC-ARROW-01：同一选角面板交替切换全能/英雄宝珠后，双箭头中心不变；悬停/按下/释放、面板动画与缩放后仍对齐图标，两侧间距一致。新增 --maiden-relic-arrow-probe 独立主菜单引擎几何检查，无大厅/跑局/存档操作。
验证：Debug无部署编译0警告0错误；UI本地化17键及参数、3项结构回归、内容/结构/演出资源/本地化/卡牌与17项意图测试门通过。独立游戏副本与独立用户数据目录下，引擎验证8次遗物切换按钮中心不变、45帧面板动画及原生按钮动画、缩放/尺寸/图标位移；102怪物239行动451条提示均格式化，17格挡组件数值可见，复用节点不残留数值，商店2张/100金币提示正确。完整VisualAssets门仍因既有143卡图清单与运行时正式图数量不符失败，未覆盖并行美术资产。
本批追加：格挡组件采用EroticBlockIntent，按自身/其他敌人/召唤物分别显示数值及主体；自身预览采用原生ModifyBlock修正。已证实安装目录intents缺侵犯诅咒2键、static_hover_tips缺商店清理2键，两份表源文件均已有正确文案。新增UI本地化校验与默认只预检的定向部署脚本，未来部署必须同步三份完整UI表到两个运行时目录并校验哈希。本批按用户要求未部署。用户明确要求累积修复后再部署，本批 DeployMod=false，不替换正在运行的游戏 DLL。


## 2026-09-30 实机 UI 反馈修复（UI-RUNTIME-20260930，IMPLEMENTED 待复验）

前置 HEAD `3e1543491ed0c14d53d0061f6f2480f61bd8fa11`；本轮由用户报告未生效并授权修复。
关联 SYS-DES-001、SYS-DES-INTENT-001、CARD-POOL-001、SYS-COR-001、SYS-INV-001。
实机 godot.log 证据：百科复制控件缺 %TickboxVisuals；商店复制槽缺 %Hitbox；自定义意图空动画名导致 ArgumentNullException；变身音频缺文件。
修复：重建原生复制控件场景 Owner/唯一节点引用；百科路线纵向排列；商店独立 FillSlot/延迟定位；意图使用合法动画键，并在每次 UpdateVisuals 清除自定义帧缓存；边缘 shader 乘入 CanvasItem 透明度；原生 AnimHide 与模态弹窗即时隐藏资源 UI。
部署必须同时核对 DLL 以及运行时 CG/音频、意图图标、侧栏图与本地化资源，不再只以 DLL 哈希宣告资源接入。CG/音频继续遵守独立开关、默认关闭、场景清理及本地角色隔离。
女神试炼开关用户已在设置中看到，但跨幕选择仍未测试；本轮不改玩法或用户设置。
验证：Debug --no-restore -p:DeployMod=false -p:ValidateMod=true 完整项目门通过（0警告0错误）；13 CG/16 OGG清单与运行时源资源精确校验通过。最终验证：修复源码编译0警告0错误；本批结构/13 CG与16 OGG清单门、侧栏21项和路线筛选24组合/6本地化项通过；53项运行时资源双目录逐文件哈希一致。主菜单引擎探针已确认复选框、商店节点/价签、意图刷新/原生恢复、102怪物239行动组件、Godot读取CG与OGG；无真实跑局操作。最终全资源门另因并行卡图验收清单142项与运行时138张正式图不一致失败，该独立缺口未覆盖或修改。需用户实机确认：商店旁清理槽及独立结算、百科筛选、意图变更与状态牌图标、试炼遮挡/口红数值/欲望方框、8→7 渐隐、启用后 CG/音频。不得标记 VERIFIED。


## 2026-09-30 高欲望边缘渐隐（IMPLEMENTED，待游戏复验）

`SYS-DES-001` → Plan“高欲望粉色边缘柔和退场” → `MaidenSuccubusCreatureVisuals.SetPersistentPinkEdge`的重复状态门、脉冲缓出和低于8点的过渡淡出 → Debug构建与静态动画检查；8点以上、8→7、7→8及退出战斗尚待游戏内视觉确认。

## 2026-09-30 百科全书响木天音路线筛选（IMPLEMENTED，待游戏复验）

`CARD-POOL-001` → Plan百科路线筛选 → `CardLibraryRoutePoolPatch`保持原三路线合池角色谓词，并添加原生风格的三项路线复选框；`MaidenRouteFilterRules`处理单/多选与全不选，原生费用/稀有度/类型/搜索仍叠加 → `TestCardLibraryRouteFilters20260930.ps1` 24/24、中文本地化6/6及Debug完整内容门。游戏内需验证布局无重叠、鼠标/手柄焦点、切换角色隐藏与重开重置；不标记`VERIFIED`。

## 2026-09-30 女神试炼可选与跨幕二选一（IMPLEMENTED，待游戏复验）

用户2026-09-30明确变更`ACT4-001/SYS-COR-002`的可选流程 → Plan“女神试炼开关与跨幕堕落值选择” → `GoddessTrialMode`注册RitsuLib主菜单设置、捕获每Run模式及幕索引收据；`FourthRouteOpeningPatch`/`FourthRouteQuestSelectionPatch`/`FourthRouteLifecycle`关闭试炼入口和监听；`ActAlignmentChoiceScreen`在第二/第三幕地图强制二选一，`CorruptionCmd`结算±2 → `TestGoddessTrialMode20260930.ps1`生产规则10/10、`FrameworkSelfTests.AssertActAlignmentChoices`、Debug无部署构建；游戏内主菜单设置、Neow、两次自然跨幕、退出读档、其他角色待验。旧档缺模式字段保留原试炼流程；不对第四幕追加一次选择。该明确用户增量需设计工作树回填DesignDoc稳定ID，代码不宣称第四层完整验收。

## 2026-09-30 商店精液类诅咒清理入口视觉（IMPLEMENTED，待游戏复验）

`SYS-INV-002` → Plan“商店清理诅咒入口原生化” → `MerchantInvasionCursePatch`复制原生`NMerchantCardRemoval`槽位、独立购买/悬停与焦点路径，`InvasionCurseMerchantService`结算保持不变 → `TestMerchantCurseVisual20260930.py`静态契约及Debug完整构建；游戏内需验证与普通删牌图标并排、退款价签、交互反馈、普通删牌仍可用、无诅咒或其他角色不显示，未运行时不标VERIFIED。

## 2026-09-30 顶栏资源生命周期及侧栏文字布局（IMPLEMENTED，待游戏复验）

`SYS-COR-001/SYS-DES-001` → Plan“顶栏资源生命周期与侧栏精简” → `CorruptionMeter`、`MaidenSidebarRail`以原生牌组/地图按钮、Neow初始房间、全屏Modal和顶栏位置为共同显隐门，降低Z层避免遮挡女神试炼；`TemptationMeter`移除标题并居中下移数字，`DesireMeter`裁去贴图自带数值框 → `TestSidebarLayout20260927.py`变异测试及双配置构建；第四层试炼打开/关闭、地图与设置切换、欲望数值视觉待游戏复验。

## 2026-09-30 遗物箭头与战技复读暗边（IMPLEMENTED，待游戏复验）

`START-002/RELIC-START-002` → `StarterRelicSelector`原生按钮运行时副本、独立材质/信号、稳定图标锚点 → 选角切换遗物后箭头位置与进阶高亮互不干扰；`CARD-N-战技复读` → `BattleReplayOriginCapability`/`BattleReplayCardVisuals`显式300×422卡牌画布 → 复制牌边缘可见、打出后还原无残留。`TestUiVisualRegression20260930.py`覆盖结构回归；游戏内视觉仍待复验。

## 2026-09-30 游戏测试失败回修（IMPLEMENTED，待实机复验）

`SYS-SCR-001/CARD-H-圣言` → `DesignSyncScriptureGenerationContract`原生手牌节点与`DesignSyncScriptureContract`动态变量绑定 → 重跑Consecration/Gospel/GuardianScripture；`SYS-ENC-001` → 战斗牌归属夹具、原版合法性与费用断言、`CurseInfectionSerializationPatch` → 重跑FlashStab/GoddessOfIce/MagicIndex/ForgeStrike/BeyondReasonForge/CurseInfection/LightWings；`SYS-CTL-001` → 挣脱文案按实际费用而非拘束层数验收 → 重跑MagicResonance；`CARD-N-娅露丝的书库` → 原生古书候选池检查 → 重跑InsatiableGreed；`CARD-N-节制之环` → `LibraryPileChoice`标题变量绑定 → 重跑LibraryPileChoice；`CARD-N-冲浪` → 实际抽牌/牌堆断言 → 重跑Surf；`EVENT-*` → 原版事件非空中间页夹具 → 重跑`ms_test_vanilla_events confirm`。其余CycloneRupture/LightningKick/Milk/Takemikazuchi由对应夹具、文案和预览回修覆盖。游戏旧报告210/18/1仍为旧DLL证据，新DLL完整结果未取得。

## 2026-09-29 第二轮视觉反馈与测试结果（IMPLEMENTED，待复验）

`START-002/RELIC-START-002` → `StarterRelicSelector`克隆原生`NButton`（不复制进阶信号）→ 选角双箭头尺寸/悬停/按压实机验收；`CARD-N-战技复读` → `BattleReplayOriginCapability`的全卡前景遮罩 → 复制/还原卡面验收；`SYS-ENC-001` → `EnchantmentRevealVisualPatch`在`NCard.UpdateVisuals`后排除揭示过场内重复原生标签 → 战斗和拾取附魔动画验收。游戏测试日志：`ms_test_events` PASS349，`ms_test_route_opening` PASS200，`ms_test_route_reward` PASS511及merchant21；`ms_test_vanilla_events` FAIL“non-bath intermediate page does not complete”；`ms_test_cards confirm all`报告210通过/18失败/1待设计，尚不能标VERIFIED。商店立绘继续由素材Agent负责。

## 2026-09-29 实机回归：选角、资源UI、遗物与测试命令（IMPLEMENTED，待复验）

`START-002/RELIC-START-002/004` → Plan `UI-START/RELIC-CHAR-002/DS27-GAME-REGRESSION` → `StarterRelicSelector`复用进阶箭头贴图并在遗物两侧定位；`StarterRelicCollectionPatch`将可选英雄宝珠和先古永恒宝珠插入原版百科子类，不额外授予开局遗物。`SYS-COR-001/SYS-DES-001` → `MaidenSidebarRail`与`CorruptionMeter`按本地角色、已访问地图坐标、顶栏显隐及设置界面保持一致生命周期。`RELIC-CHAR-002`（DesignDoc内用套套罕见条目）→ `InternalCondom`/`InvasionCmd`/`InvasionCurseMerchantService`/本地化：原生入牌组前拦截、持久计数、堕落≥3最大生命及商店清空/退款；旧存档无此遗物则不影响。`DS27`命令日志显示入口已触发但书库候选零效果门、TestMode与真实UI冲突导致失败；修正测试边界后仅离线编译与结构验证，游戏重跑尚未完成。商店角色立绘过大交素材Agent，非本批实现范围。

## UI-CHAR-SELECT-V2-003：getter类型保护与逐角色诊断（IMPLEMENTED，游戏内待验）

`START-002`、`UI-CHAR-SELECT-V2-001` → Plan `UI-CHAR-SELECT-V2-003` → `MaidenCharacterSelectVisualPatch` 两个实际图标getter前置保护、Debug逐角色按钮日志 → VisualAssets门和启动日志复查。路径修正后异常未消失，故此轮不再凭堆栈推定触发角色；通过日志明确故障角色后再判断后续兼容范围。

本地启动复查：24/24角色按钮（含本角色与随机角色）均完成初始化，贴图转换异常及其连锁空引用未复现；标准模式点击、遗物切换和多人游戏仍待玩家可见界面验收，状态保持IMPLEMENTED。

## UI-CHAR-SELECT-V2-002：选角按钮贴图类型修复（IMPLEMENTED，游戏内待验）

`START-002`、`UI-CHAR-SELECT-V2-001` → Plan `UI-CHAR-SELECT-V2-002` → `MaidenCharacterSelectVisualPatch` 的两个路径getter与按钮V2覆盖层 → VisualAssets门和游戏内选角复测。禁用`Ryoshu`后仍报贴图类型异常，证实上一轮把外部模组列为唯一原因并不充分；本项只保证本角色原版初始化路径为压缩贴图资源，正式V2图标继续在初始化后显示，不改其他角色或遗物规则。

## UI-RUN-RESOURCE-20260929：游戏内资源补装与选角崩溃归因（IMPLEMENTED，游戏内待验）

关联`RELIC-START-003`、`UI-CHAR-SELECT-V2-001`及路线卡视觉：正式源码实现无设计漂移，但DLL单独部署遗漏已提交中文本地化和新版贴图，造成旧遗物文案与百科翼饰消失；定向补装并哈希核验后待实机VERIFIED。选角崩溃的首因是外部`Ryoshu`的图标贴图类型转换失败（日志还标明不支持当前`public-beta`），不归因本Mod的初始遗物切换；禁用该模组复测，若仍复现再查本Mod入口。

## DS27-03E：化石追踪者侵犯后击晕顺延回归（IMPLEMENTED，游戏内待验）

`SYS-INV-001`、`SYS-DES-INTENT-001`、`MON-ERO-CATALOG-001` → DesignDoc 3.2～3.5及怪物详细表 → Plan `DS27-03E` → `ControlIntentTestRunner.FossilInvasionStunOnce`和`ControlIntentTestContext.AddFossilStalker` → DS27-MON/COMPAT/GATES。真实怪物行动将验证一次诅咒、成功后禁用、单次击晕、重复请求幂等与原意图出栈。测试结构15场景及Debug无部署构建已通过；游戏脚本未执行，不能据此断言化石追踪者问题在游戏中消失。未改生产规则或怪物数值，未部署。

## DS27-06H：固定预约事件访问历史（IMPLEMENTED，游戏内待验）

`EVENT-NEW-005/006`固定替换问号 → Plan `DS27-06H` → `MassageAppointmentService.RoomCreated`确认实际事件房间后复用`RunState.AddVisitedEvent` → DS27-EVENT/COMPAT/GATES。原版事件候选会登记已访问事件，固定注入路径先前绕过；只补历史，不改变预约时机或奖励。游戏自然跨幕/存读档待验。

预约定向5项、日期静态635项和Debug无部署构建通过；游戏运行时未验。

## DS27-03D：逐怪物三表候选与前置一致性（IMPLEMENTED，离线已复验）

`MON-ERO-CATALOG-001`分配表及详细行动表 → Plan `DS27-03D` → 101个ID逐列比较次数/阈值/详细行动有无、I需B的运行时目录校验 → DS27-MON/GATES。仅数据完整性约束，不修改表中数值；真实怪物行动未运行。

目录定向5项、日期静态634项及Debug无部署构建通过；游戏内初始化和行动仍待验。

## DS27-07U：乳汁后的审计器自测计数（IMPLEMENTED，离线已复验）

`EVENT-NEW-001`/`RELIC-EVENT-003`衍生牌乳汁 → 当前229注册牌 → Plan `DS27-07U` → 审计器自测注册数和严格审阅输出更新，保留内容缺口拒绝通过 → DS27-CARD-META/GATES。只同步测试期望，不改生产内容；游戏内验收独立保留。

审计器自测29/29、统一离线15/15通过，报告`obj/design-sync-validation/20260929T090146Z-1f0809916701/report.json`；六类游戏运行组均未执行，仍不标VERIFIED。

## DS27-07T：万念俱灰双X结构门（IMPLEMENTED，离线已复验）

`CARD-C`万念俱灰现行6Y伤害、X/X+1次 → Plan `DS27-07T` → 结构门严格匹配当前战斗外基础/升级模板 → DS27-CARD-TEXT/GATES。仅同步旧测试预期；卡牌规则和本地化不变，游戏内双X场景仍待验。

结构门、双X定向3项、日期静态632项与Debug无部署构建通过；游戏内场景仍未执行。

## DS27-06G：按摩店三段预约（IMPLEMENTED，游戏内待验）

`EVENT-NEW-004～006`、`RELIC-EVENT-006`及相关淫纹诅咒 → 用户Q10确认 → DesignDoc成熟度修正 → Plan `DS27-06G` → 待接原生问号房间优先替换、跨幕持久预约、三段事件页面/结算、本地化与定向脚本 → DS27-EVENT/ACT4/COMPAT/GATES。事件规则和文字未修改；当前只有“神清气爽”遗物效果，事件来源及预约链尚缺。贪婪冲突依已确认的“预约优先、免费商店顺延”，不将模拟预约测试误计为正式实现。

现已接`MassageAppointmentService`、两个房间补丁、三段事件及本地化，已有神清气爽效果获得入口；`DesignMassageEventContract`覆盖费用、淫纹替换、拒绝门槛与后续页面，静态测试覆盖保存字段和贪婪补丁优先级。632项静态、内容/本地化和Debug构建通过；结构门仍因既存“万念俱灰”旧断言失败。游戏命令仅编译、未执行，正式跨幕预约及完整存读档仍待游戏内验证。

## DS27-03C：默认侵犯诅咒与强制拘束续接（IMPLEMENTED，游戏内待验）

`SYS-INV-001`、`SYS-DES-INTENT-001`、`MON-ERO-CATALOG-001` → DesignDoc 3.2～3.5及怪物详细表 → Plan `DS27-03C` → `InvasionCmd` 默认“精液”映射、`IntentMoveFactory.TryForceControl` 连续回合记录、`CatalogIntentToRecovery` 原意图真实出栈场景 → DS27-MON/COMPAT/GATES。当前目录承认“精液”但侵犯结算缺分支；强制拘束只计总次数；原有测试停在恢复意图出现，尚未证明原意图顺延。设计无漂移，不修改怪物数值或意图次数。

上述缺口已接入并补 `default_invasion_curse` 游戏场景及目录静态映射检查。14场景结构、628静态和Debug无部署构建通过；游戏内场景仅编译，真实战斗/存读档仍待验，整组怪物行动不能标VERIFIED。

## DS27-03B：Q1/Q6已确认规则统一（READY，代码回修与游戏验证未完成）

`SYS-TRF-001`、`SYS-INV-001`、`SYS-DES-INTENT-001`、`MON-ERO-CATALOG-001` → DesignDoc/怪物表 `7f87016d` → Plan `DS27-03B` → 正数降0保持、0层再损失解除、侵犯耐久≤1、成功后无例外禁用、原意图入栈顺延 → DS27-MON/TRF/COMPAT。此次是已答问题的文档矛盾修正；现有耐久合法性与后继保存代码需回归，自然连续上限及强制改意图冷却过滤仍待代码修复。没有新增怪物数值或游戏验收证据。

局部实现：强制改意图绕过自然冷却但仍消耗总次数；新增持久化连续回合计数并限制第三次自然生成。`forced_intent_ignores_natural_cooldown`、`natural_consecutive_limit_and_saved_state` 两个游戏场景已编译，13场景结构门、627静态与Debug构建通过；尚无游戏执行结果，不将整个DS27-03B或逐怪物状态机标为VERIFIED。

## DS27-03A：怪物目录基数与表间覆盖（IMPLEMENTED，游戏内待验）

`MON-ERO-CATALOG-001` → DesignDoc 3.3～3.5与两份怪物表 → Plan `a6ec931b` 的 `DS27-03A` → `EroticAttackCatalog.Build` 基数/ID解析修复及 `TestDesignSyncMonsterRoster20260927.py` → DS27-MON/GATES。正式表101个ID、17个意志坚定；修正代码原102/18硬编码和不能识别ID后中文名的正则。627项静态、11项控制意图结构与Debug无部署构建通过；未在游戏中验证目录初始化、逐怪物行动或完整战斗状态机。

## DS27-06F：可疑的商店与榨乳器（IMPLEMENTED，游戏内待验）

`EVENT-NEW-001`、`RELIC-EVENT-003` → DesignDoc `4bd0d08d` → Plan `b8003248` 的 `DS27-06F` → `SuspiciousShop`、`Milker`、`Milk`、中文文本与事件/遗物/卡牌定向脚本 → DS27-EVENT/RELIC/CARD/GATES。门槛、RNG区间、三个限定遗物、每战开场伤害与生成两牌进入离线与游戏内验收。229张牌、35件遗物；625项定向静态单测通过，统一选定5组离线门通过。游戏脚本只编译未执行，未标 VERIFIED；未部署。

## DS27-06E：择祸从轻事件（IMPLEMENTED，游戏内待验）

`EVENT-NEW-007` → DesignDoc `2f7d77e6` 成熟度同步 → Plan `DS27-06E` → 事件类、本地化与 `ms_test_events confirm` → DS27-EVENT/COMPAT/GATES。验收包括限定诅咒池、原生随机稀有遗物、欲望+6、放弃无变化、选项只结算一次及其他角色隔离；游戏尚未测试。

实现 `src/Events/LesserEvil.cs`、事件本地化、`src/ConsoleCommands/DesignLesserEvilEventContract.cs` 并接入 `DesignEventTestConsoleCmd`。选定四组离线门及 Debug 构建通过；游戏内三分支、其他角色隔离与存读档仍待验。其余四个新增事件及怪物表不因本项完成而关闭。

> 接续入口：[2026-09-29 未完成项交接](DESIGN_SYNC_20260927_UNFINISHED_HANDOFF_20260929.md)。实现快照 `aaf29d57`；保留原未完成状态，不以文档交接提升为 VERIFIED。

> 本文件记录DesignDoc需求、Plan阶段、实现状态和验收入口之间的映射。
>
> 玩法规则以`DesignDoc.md`为准，技术方案以`PLAN_FRAMEWORK.md`为准。
>
> 更新流程以`DESIGN_CHANGE_PROTOCOL.md`为准。

> 当前汇总：[2026-09-28全范围状态核对](DESIGN_SYNC_20260927_CURRENT_STATUS.md)。旧批次段落是带日期的历史证据；不得用历史未实现项覆盖后续交付，也不得把局部IMPLEMENTED提升为全范围VERIFIED。

## DS27-04L：神清气爽奖励效果（IMPLEMENTED，事件入口和游戏验证未完成）

DesignDoc神清气爽/普通护理奖励→新增事件遗物Refreshed→原生开场抽牌修正/三战计数/消失/保存/拥有者隔离。前置`0de8070c`，Q10已批准完整条目；不改事件产品规则。只补独立普通效果与既有测试，事件获取链仍未完成，不部署。

计划`00be3ba4`→Refreshed/34遗物登记/正式文本/原生ModifyHandDraw与抽牌完成清理→625静态、Debug零警告零错误、精确内容门通过。三战、重复调用、原生NoDraw、序列化及拥有者隔离脚本编译未运行；暂用已有原版回退图标，无专属素材，事件链未完成、未部署。报告`obj/design-sync-validation/20260928T190459Z-94beab83f92e/report.json`仅选定离线结论。

## DS27-05AU：重复格挡与觉醒抽牌（IMPLEMENTED，游戏内待验）

ACT4-001谦逊觉醒→原卡只有格挡描述但有纯次数循环→原始纯效果识别。前置`4d45a9dd`；修复蜻蜓点水误判，不用改写后的程序冒充原牌纯度。补现有实际遗物/阶段抽牌场景及通用反例，编译不等于游戏验收，不部署。

计划`f796999a`→原始纯计数格挡不再误判→117抽取、17接线、Debug及实际DLL目录通过。蜻蜓点水/邪眼原卡、附魔及觉醒阶段真实脚本已编译，游戏未运行；带抽牌/原触发条件反例保留，未部署。

## DS27-07S：遗物登记及Q17验收漂移（IMPLEMENTED，选定离线验证通过）

RELIC-CHAR-007已新增东尼的咒符、ACT4-001已确认免费不改变X支付→旧静态断言同步。前置`2f1cf086`；实际选定验证3/4通过，5个失败均要求历史32数量或Q17未答注释，不通过回滚正确生产实现解决。保留严格登记和原支付检查，复验后仅记离线结果，游戏未执行。

计划`9e853f66`→四处数量33及一处正式X规则断言→选定五组5/5通过，624静态/生产规则/编码/Release/精确登记，无运行中源码漂移。报告`obj/design-sync-validation/20260928T184858Z-2a994de0c19e/report.json`，仅选定离线结论，不是全15组或游戏验收。生产规则未改、未部署。

## DS27-05AT：谦逊回归测试入口迁移（IMPLEMENTED，游戏内待验）

ACT4-001→正式自动提取程序→既有战斗回归不再使用手写实现表。前置`21ea4ace`；保留原生结算与附魔/X/文本边界的实际验证，停止重复逐牌表巡检。迁移发现纯格挡计数循环的次数丢失，修复并复用原敌人数/敏捷测试，禁止把预期改成只格挡一次。只定向测试与Debug，不部署，不能将编译等同游戏执行。

计划`4410d6d1`/`8bb413dc`→实际回归统一生成程序并发现/修复纯格挡计数丢失→114抽取、17接线、Debug零警告零错误、实际DLL目录读取通过。蜻蜓点水和邪眼保留次数及逐次敏捷；原战斗预期未降级，补动态文本/原生消耗记录场景，仅编译未运行。未部署，未进行旧离线表清理；其余全范围缺口不因本组完成而消失。

## DS27-05AS：碎片商店真实入口脚本（IMPLEMENTED，游戏内待验）

ACT4-001碎片仅下一商店出现/购买解锁第二试炼→现有商店Patch/原生PurchaseWrapper→路线奖励Runner的merchant子模式。前置`0b3016a6`；不以直接RelicCmd.Obtain替代购买，金币失败不改变试炼，未购买与已购买后续均不再出现；不部署。

计划`9224d3d3`→真实库存/购买/支付/回调与碎片阶段脚本→9项定向检查、Debug零警告零错误；游戏未执行，生产代码未改。运行器新增明确商店前提与破坏性警告，测试Prepare重置碎片标志防止场景串扰。自然跨层/读档/UI仍待验。

## DS27-05AR：原版直接攻击与动态类型（IMPLEMENTED，游戏内待验）

ACT4-001→原生AttackContext分组/直接伤害、结果扩散、CardType实例身份与次数枚举→通用抽取/保存/描述及正式选牌脚本。前置`8601704a`，不增加逐牌实现表，不把未知外部代码视为空效果，不部署。

计划`51ba0942`→三条原版未解析收口并修正两条直接伤害遗漏→106抽取、940生产契约、17接线、Debug/本地化/实际DLL目录通过。目录785条无解析失败不是全语义通过率；保存1/2兼容，正式两敌人脚本仅编译。外部牌、旧表测试迁移及游戏验证未完成，不部署。

## DS27-05AQ：谦逊战斗记录与结果依赖（IMPLEMENTED，游戏内待验）

ACT4-001→只读失血次数参数、前序原生攻击结果格挡→通用抽取/正式选牌测试。前置`737ac276`，不运行原OnPlay、不增加逐牌实现表，描述和执行同时接入；仅定向验证、不部署，整体范围仍未完成。

计划`a83d596d`→Spite/Fisticuffs共用查询/结果索引/原生预览→95抽取、939生产契约、17接线及Debug/嵌入目录通过，正式游戏脚本仅编译。782提取/3未支持，不是实机通过率；其余兼容和整体目标不提升为完成。

## DS27-02AV：遗漏事件诅咒模型（IMPLEMENTED，游戏内待验）

DesignDoc媚药中毒条目→生成池AphrodisiacPoisoningCurse与真实Gain修正→牌堆移动/叠加/恢复/满值及全文脚本。前置`42ef2fd0`；不复用催情液身份，不进入怪物专用池，不提前声称事件获取已实现，不部署。

计划`7c727c1d`→注册/原生Gain/全文完成→15清单、29审计器、6资源回归和Debug、本地化/登记门通过。游戏场景仅编译；素材用既有缺图回退，正式事件来源仍待接入，非全目标完成。

## DS27-02AU：剩余注册卡牌全文（IMPLEMENTED，游戏内待验）

CARD/STATUS/CURSE当前条目→37个既有注册模型的双实例全文、专用诅咒移除语义及关键词→原有卡牌测试入口和本地化门。前置`52f6bcbe`；催眠正文与未注册条目不由注册覆盖清单自动豁免。正式预约事件未接入，不冒充优先级集成完成；不部署。

计划`3333cbf4`→37全文/移除多余消耗/阈值正文→15清单、3数值回归、Debug、本地化及登记门通过。游戏入口ds27-status-text仅编译；216注册现行模型双实例声明不是通过率。媚药中毒等未注册设计、催眠正文及原范围缺口保留，不部署。

## DS27-05AP：原版视觉回调删除（IMPLEMENTED，游戏内待验）

ACT4-001谦逊→纯视觉局部赋值与节点读取的共用识别→抽取正反例及正式选牌测试。前置`8630f383`；禁止恢复消耗手牌/施加能力等被删效果，未知回调仍拒绝，不部署。

计划`84402418`→FiendFire/Haze/SovereignBlade支持→88抽取执行/17接线、Debug/嵌入目录通过，游戏脚本仅编译。目录780/5不是实机通过率，完整功能仍未完成。

## DS27-05AO：谦逊剩余本Mod公式（IMPLEMENTED，游戏内待验）

ACT4-001谦逊→通用局部算术/只读参数、目标意图需求和增益层数→实际提取程序及正式选牌测试。前置`3e493ad1`；不新建逐牌手写表，不将未知代码当空效果。无部署，未覆盖项保留。

计划`8a57fb40`→五条本Mod公式及两条原版公式支持→82通用用例、17接线、Debug/嵌入目录验证通过。正式选择、伤害/格挡、空效果和描述脚本仅编译；777条提取/8条明确未支持不是实机覆盖率，不标完整谦逊完成。

## DS27-02AT：手牌满值保护（IMPLEMENTED，游戏内待验）

当前事件诅咒ClimaxBanCurse条目→手牌资源判定修饰、离手复查、延迟结算保护→真实命令游戏脚本及定向登记/构建。前置`3684bb9c`，不沿用旧“设计待定”占位，不修改其余事件或资源规则，不部署。

计划`d8ba3ee9`后代码和正文完成；原生牌堆移动、消耗、双卡保护、延迟与未满值脚本已编译。Debug、本地化、227登记（226可执行/仅催眠待设计）及6项相关回归通过，不代表游戏验收或完整目标完成。

## DS27-02AS：剩余卡牌数值/全文（IMPLEMENTED，游戏内待验）

当前DesignDoc卡牌条目→衣装透明手牌增量20、魅魔液句号、深海黏液分号→实际牌堆移动与完整文本契约。前置`1760a080`，仅剩余范围；已有万念俱灰脚本登记不冒充新增实现。事件边界等待非性化方案，未缩减目标。不部署。

计划`ecb52116`→20点手牌修正/即时离手、两个标点差异与重复关键词→12牌全文和手牌叠加脚本、18定向测试/Debug/本地化及登记门通过。第98批清单179双实例声明、38未识别，非游戏通过率。绝顶禁止已有设计却仍占位，继续未完成；五新事件的边界询问等待答复，不擅改产品规则或缩减目标。不部署。

## DS27-05AN：谦逊共用解析缺口（本组IMPLEMENTED，剩余公式READY）

ACT4-001→累计支付/现有Power数值、目标别名及非伤害附加效果边界→通用抽取与正式选牌测试。前置`1be7c547`，只实现源码明确的读取和删除边界，不补写卡名表；完整覆盖仍待完成，不部署。

计划`e1a5126c`→只读层数/支付、目标别名/最低生命/友方和非伤害边界→71通用场景、17接线、直接生产程序、Debug及实际DLL资源通过。第97批目录未解析从27降到15；正式选牌脚本仅编译，游戏未运行、不部署。未支持不标完成。

## DS27-05AM：谦逊正式选牌（入口IMPLEMENTED，解析缺口仍READY）

ACT4-001→HumilityLesson正式选择/实例改写/描述刷新→真实选牌脚本及继承入口定向测试。前置`04adf72c`；不再让已支持改写停留在调试API，不缩小选牌范围。未知卡保留明确未完成诊断，不部署、不宣称完整覆盖。

计划`c0dd223f`→正式选牌接入实例程序/描述、继承入口/虚方法派发→63通用场景、16接线、Debug和嵌入资源验证通过。第96批实际选牌脚本仅编译；785记录中27未解析，不是全卡兼容率，未解析保留开发期提示而非擅改最终规则。不部署，不标VERIFIED。

## DS27-05AL：谦逊觉醒钩子（入口IMPLEMENTED，完整识别仍READY）

ACT4-001谦逊觉醒→效果纯度元数据/实例程序与原生附魔边界→正式AfterCardPlayed抽2张、通用和游戏脚本。前置`a5ec582e`，不以句数或已删除其他效果后的非空程序判断原卡纯度；完整选牌及未解析入口仍待完成，不部署。

计划`1f262f41`→原始纯度与改写结果分离、正式遗物钩子→59通用场景/15静态/Debug和DLL资源通过，第95批。空程序不触发，附魔独立；完整未知/继承原卡判定和实机仍未完成，不把入口改好当作全范围兼容。

## DS27-05AK：其他效果边界与重载（IMPLEMENTED）

用户仅保留伤害/格挡调用与对应描述→非伤害命令边界/参数重载解析→通用测试及生成目录验证。前置`f54cfb4a`；不改变规则、不新增卡名表、不部署，未知调用不能视为空效果，正式入口仍未完成。

计划`95bc08b8`→命令边界/原生参数类型与数量判别/结果配置删除→50通用场景、14静态与Debug、实际DLL资源通过。第94批734提取/28未支持，游戏脚本仅编译；不以描述可改写或目录成功替代正式接入完成。

## DS27-05AJ：多语句链与外围删除（IMPLEMENTED）

用户直接摘取调用→局部构建器接续、执行顺序、非规则代码排除→通用语法和生成目录验证。前置`9d702987`，冲突/未识别调用显式失败，无逐牌登记和部署；整体正式接入尚未完成。

计划`3cdfcb55`→局部构建器/执行顺序与视觉/费用/集合删除→42通用场景、14静态、Debug及实际DLL资源通过。第93批：694提取/68未支持，游戏脚本仅编译。正式入口/觉醒/旧表迁移未完成，不提升整体状态。

## DS27-05AI：调用来源与副资源X（IMPLEMENTED）

用户直接提取调用→来源参数/副资源账本保留→通用抽取、程序存档、原生命令和预览。前置`a444725e`；不新增卡名映射，不恢复召唤/其他效果。未支持来源明确失败，正式接入和游戏验收仍待完成，不部署。

计划`09f6cd68`→schema2/原生来源与预览/副资源X/数值升级分支→31通用场景、直接依赖契约及Debug通过，实际DLL657提取/105未支持。第92批；未执行实机、不部署，不标整体完成。

## DS27-05AH：调用辅助方法展开（IMPLEMENTED）

用户直接抽取调用→源码方法索引/参数替换/视觉链排除→不依赖具体卡名的通用场景。前置`30ae44a5`，拒绝递归/歧义，不将真实攻击来源当成可删除视觉。不部署，完整正式入口仍待接。

计划`a05e474b`→HelperExpansion、视觉/其他效果排除、未知return调用守卫→23通用场景、Debug及PE资源解析通过。第91批；共享攻击实际游戏脚本已编译未执行，119不支持记录保留，不标谦逊整体完成。

## DS27-05AG：自动提取产物的构建/运行时接线（IMPLEMENTED）

用户直接调用抽取方案→构建生成/嵌入程序目录→运行时身份解析和真实卡牌测试。前置`77fcbfff`；不在游戏中读取源码，不按牌名补例外，不支持记录保留明确失败；正式任意选牌范围不缩减，不部署。

计划`5c991753`→生成资源/纯目录加载器/原生类型接线→15通用场景、Debug构建、实际DLL中资源读取通过；冲浪原生运行脚本仅编译未执行。第90批明确178个未支持记录，正式入口仍未切换，不能将资源接线当作整体完成。

## DS27-05AF：谦逊调用抽取（工具IMPLEMENTED，接入READY）

用户最新澄清→停止手写逐牌登记、直接抽取Attack/GainBlock调用及纯数值依赖→语法树工具、通用场景验证。前置`c03dad85`；抽取外围抽牌/循环/条件不执行，调用链内次数/X/目标保留；不再以冲浪抽牌模拟问题阻塞。自动抽取到正式执行及旧表替换仍需完成，不部署。

计划`8f0bb229`→可执行抽取CLI→12通用场景、实际源码抽取及Debug通过，统一定向入口已登记。不是逐牌新增白名单；复杂局部赋值/间接调用等未支持项明确报错。第89批保留正式入口未切换边界。

## DS27-07R：定向自动验证入口（IMPLEMENTED）

全范围自动测试要求及用户缩小日常核对范围→补两套独立纯生产测试登记、按套件执行、报告范围/未运行项→真实定向运行和入口单元测试。前置`43cfda68`，不增加卡牌档案或改变玩法；未选套件不能当作通过，不部署。

计划`1c6635a2`→14套完整登记/可重复套件过滤/schema2范围报告→12单元测试及真实谦逊/慷慨2套通过，运行前后源码无漂移，其他套件及实机未运行。第88批；不提升全目标完成状态。

## DS27-05AE：Q14故障机器人档案（IMPLEMENTED，游戏内待验）

ACT4-001谦逊→20张精确类型档案→弹幕动态段数、堆栈动态格挡、原卡充能/选牌/能力删除测试。前置`e6f04c5c`，不猜延迟效果，不部署；仅本轮直接依赖范围，正式入口与觉醒仍待完成。

计划`d55fd85f`→累计161生产档案→936直接生产断言、14接线与Debug通过。游戏脚本仅编译，未运行；第87批保留验收边界，完整Q14未完成。

## DS27-05AD：Q14原版牌与外部目标变化（IMPLEMENTED，游戏内待验）

前置`002288dc`→32张原版明确档案、CurrentCardTarget动态标记→生产执行/序列化及游戏真实牌脚本。铁波/冲刺顺序、全身撞击当前格挡、小刀外部刀扇目标变化保留；不把未知牌猜成单段。不部署，正式选择器/其余特殊公式/觉醒仍待完成。

计划`53926742`→141档案与`ResolveTarget`执行/显示统一→772生产断言、14接线及Debug通过。游戏原版基础/升级、混合先后、格挡动态伤害及刀扇变化脚本已编译未执行；见第86批，不代表正式入口和全范围验收完成。

## DS27-05AC：Q14混合效果、成长与接口规则（IMPLEMENTED，游戏内待验）

前置`3f8cadf5`→29张显式档案、光箭专属标记失效→生产程序断言与真实游戏脚本。保留原有数值/多段/操作顺序，删除未来触发；不取消已完成的独立规则。即时与延迟增幅使用统一资格检查，保留外部Power及附魔。其余Q14待办不缩减，不部署。

计划`e38f8c01`→109档案＋`MagicAmplificationCardRules`即时/延迟接线→645生产断言、12静态和Debug通过。实机光箭、屏障/镰刀消耗、闪耀之剑多段脚本仅编译未运行；第85批，正式选择器与觉醒仍待完成。

## DS27-05AB：Q14普通圣洁/圣言档案与实例属性限制（IMPLEMENTED，游戏内待验）

前置`1ea97dc2`→36张显式档案及敌人数重复格挡→生产档案断言与真实游戏脚本。只清除谦逊实例自身的使用限制及已删除回合末效果提示，不绕过原生付款/外部钩子；BeforeFlush仍由原卡钩子过滤处理。范围之外的特殊公式、正式选择器及觉醒仍待完成，不部署。

计划`ac4f80cc`→累计80档案＋`HumilityRewritePatches.IntrinsicFlags`→525生产断言、10接线和Debug通过；真实休息/费用/Sloth/BeforeFlush/重复格挡等脚本仅编译未运行。第84批；不把档案注册等同正式选择器已切换。

## DS27-05AA：Q14显式效果档案（首组IMPLEMENTED，其余READY）

前置`f7591b7f`→已审阅卡牌的单段/多段/双格挡/全敌/次数变量/双X和空效果档案→生产档案直接执行、真实模型变量绑定及打牌脚本。未知类型不猜测，正式选择器不先限选；依赖删除操作的次数与条件增伤边界单列确认。此组不改变Q14整体未完成状态，不部署。

计划`1da2d33a`→`HumilityProfileDefinitions/HumilityCardProfiles`44份档案（含13空程序）→369生产断言＋8接线检查、Debug通过；`ms_test_humility_runtime confirm`改用同一生产档案并新增模型变量/空效果附魔/回合末/随机多段脚本，仅编译未执行。第83批；选择器、剩余档案及觉醒判定仍未完成。

## DS27-05Z：Q14谦逊实例运行时（IMPLEMENTED，整体仍READY）

前置`00e4aec7`→05Y程序绑定原卡能力→原生打牌替换/触发隔离/关键词和卡面/附魔保留→真实卡牌游戏脚本。只影响明确附加此能力的实例，不改卡牌规范模型。各牌完整效果档案和觉醒判定仍待实现，不以运行时单层宣称Q14完成；不部署。

计划`22b2a314`→`HumilityRewriteCapability/NativeEffects/RewritePresentation/HumilityRewritePatches`→146生产程序断言＋6接线静态、Debug零警告零错误。`ms_test_humility_runtime confirm`覆盖实际Wrapper/原生费用、多段、化学X、Swift/Glam/Goopy/Steady、抽牌触发与嵌套钩子、克隆/能力JSON及目标预览，仅编译未游戏运行；正式谦逊选择器仍未改，详见第82批。

## DS27-05Y：Q14谦逊效果程序（基础层IMPLEMENTED，整体READY）

ACT4-001谦逊→数值/段数/X/目标分离的可执行伤害格挡程序→直接链接生产源码的离线契约。前置`7152120e`，条件与其他效果不进入程序；多次翻倍不改变段数，资源快照保证重放一致。仅基础层，不对完整谦逊改写、卡面、关键词/触发抑制、附魔边界或觉醒抽牌宣称IMPLEMENTED，完整Q14继续READY；不部署。

计划`29b49420`→`Core/Cards/HumilityEffectProgram.cs`→`tests/HumilityEffectContracts`146断言与Debug0警告0错误。测试直接执行生产解释器而非复制算法；多段伤害作为同一命令传递给后续原生适配器。尚无游戏接线或实机测试，范围见第81批。

## DS27-Q12～Q20：用户九项确认（READY，2026-09-28）

前置`6ffb39e2`→DesignDoc九项规则说明→DS27-02AR/04G/04J/05F/05K/05M2。Q12/Q13/Q19为能力叠加和原生重放顺序；Q14谦逊改写；Q15/Q16供奉所有权与可见性；Q17原版免费X逻辑；Q18/Q20三路线合池逐牌等概率。全部已答，不沿用历史OPEN阻塞；代码完成分项登记、游戏未验不标VERIFIED，不部署。

Q12/Q13/Q19→DS27-02AR已IMPLEMENTED：独立计数/排队复读、分层生成后统一格挡、保留原生净化顺序与历史测试。技术输入`0e679b6f`，12定向静态和Debug构建通过，游戏待验。Q14～18/Q20仍READY待实现，见`DESIGN_SYNC_20260927_BATCH77.md`。

## DS27-Q12～Q20：用户九项确认（READY，2026-09-28）

前置`6ffb39e2`→DesignDoc九项规则说明→DS27-02AR/04G/04J/05F/05K/05M2。Q12/Q13/Q19为能力叠加和原生重放顺序；Q14谦逊改写；Q15/Q16供奉所有权与可见性；Q17原版免费X逻辑；Q18/Q20三路线合池逐牌等概率。全部已答，不沿用历史OPEN阻塞；代码完成分项登记、游戏未验不标VERIFIED，不部署。

## DS27-05X：Q15/Q16慷慨供奉（IMPLEMENTED，游戏内待验）

ACT4-001慷慨→限定试炼/觉醒资格、战斗奖励互斥组、宝箱分配后选择、原生网络子项适配→定向阶段/互斥/删牌/序列化/索引与多人归属脚本。前置`a7e25e36`。STS1实际jar中的RewardItem双向relicLink和选中后忽略另一项已核对；不把OnSkipped当供奉。原版LinkedRewardSet只有UI关联，网络顶层索引和父完成状态需本组专用适配。

计划`4118e24a`落实：`GenerosityOfferingRules/GenerosityOffering/GenerosityOfferingPatches/GenerosityOfferingVisibility`→`GenerosityOfferingContracts`107生产断言、`TestDesignSyncGenerosityOffering20260927.py`5＋原拾取8静态、Debug0警告0错误。`GenerosityOfferingContract`接入原`ms_test_generosity confirm`，覆盖实际选择器等待/互斥/原生奖励栈/本地及远端入口、确定奖励恢复、宝箱分配后入口与当前DLL实际IL替换；仅编译未运行，真实多人未验，不部署。第80批详述。

## DS27-05W：Q17原版免费与X费用（IMPLEMENTED，游戏内待验）

ACT4-001免费至打出及直接关联本回合免费→GeneratedCardCostCmd/原生SetToFreeThisTurn/Ritsu固定费用绑定→真实SpendResources＋OnPlayWrapper验证X/Y支付与重放效果。前置`63b15207`，不新增X特例；修正本回合包装把X副资源抹成0的旧代码，UntilPlayed保留现有原版规则。定向验证，不部署。

计划`e897edb7`完成；`DesignSyncFreeUntilPlayedContract`新增16实际双X付费/伤害/重放场景及固定费用恢复，沿`ms_test_resource_relics confirm`调用。10静态及Debug0警告0错误，脚本未游戏运行，见第79批。

## DS27-04K：两个随机遗物（IMPLEMENTED，游戏内待验）

RELIC-CHAR-007/Q18与RELIC-EVENT-004/Q20→`UnifiedRouteCardPool`/`TonysCharm`/`WitheredTreeSoul`→`ms_test_random_relics confirm`（战斗外与战斗内各一轮）及`TestDesignSyncRandomRelics20260927.py`。计划`6f79890a`，用户F盘jar已对照原版消耗/随机/满手规则并接入两个原图标。三路线合池逐牌均匀，东尼永久移除升级稀有，枯木原生战斗生成含历史/后续钩子；治疗术只在枯木池排除。6静态、登记门、Debug0警告0错误，游戏脚本仅编译，未部署。详见第78批；东尼图标暂沿已有占位，未新增专属美术。

## DS27-02AQ：两处遗留数值（IMPLEMENTED，游戏内待验）

CARD-C更换胖次/欲望鞭挞正式条目→Threshold 5/4、Damage 5/5及原Probe预期→DS27-CARD-META/EFFECT。前置`554718f9`，不新增或修改意图机制；仅本次变量与边界测试，不部署。

计划`0bf69eee`后已修复，3项定向静态及Debug无部署构建通过，游戏内待验。见`DESIGN_SYNC_20260927_BATCH76.md`。

## DS27-02AP：万念俱灰双X同步（IMPLEMENTED，游戏内待验）

CARD-C欲望输出万念俱灰→支付台账Value/预览/战斗外公式→DS27-CARD-TEXT/EFFECT。前置`7ddbe565`；修复当前设计X/Y轴与重放丢失倍率，真实支付测试取代人工AfterSpent。按用户新指示收敛至本轮变动范围的定向验证，不重审无变化内容、不部署；Q17继续保留。

计划`4ff4f20b`后修复与七组引擎场景完成；定向静态3/3，Debug零警告零错误，游戏脚本未运行。详细证据及命令见`DESIGN_SYNC_20260927_BATCH75.md`；本批未重新运行全量检查。

## DS27-02AO：五牌全文与变奏/资源支付（IMPLEMENTED，游戏内待验）

CARD-C/初始黑暗元素、黑暗之源、背水一战、传奇矿工、回想房→五牌独立全文/元数据/变奏边界与回落、背水条件分行、传奇矿工真实支付→DS27-CARD-TEXT/META/EFFECT/COMPAT/GATES。前置`3da18bfe`，无设计漂移；保留既有变奏/负属性/抽牌行为测试，不改规则或扩大成人内容，不部署。

计划`eaa44e09`后的全文/元数据/变奏十次边界、背水三层和负属性全文以及矿工真正支付脚本完成。604静态、13798生产/33编码、双构建五门与统一12/12通过；初轮旧模板预期未同步导致一项静态失败，已保留记录并更新精确预期后全部复验。166双实例全文声明/51未识别，游戏六组未运行，不部署。详见`DESIGN_SYNC_20260927_BATCH74.md`。

## DS27-02AN：瘴雷累计消耗回修（IMPLEMENTED，游戏内待验）

CARD-C输出瘴雷及自身费用计入说明→成功资源支付监听＋战斗/玩家隔离台账、瘴雷命中次数与全文→DS27-CARD-EFFECT/TEXT/META/COMPAT/GATES。前置`24adb48f`，设计无漂移。原Probe以当前余额为预期亦需修复；台账不用跨战斗保存或未清理的依赖sidecar历史。覆盖支付成功/失败、免费/重放、非消费减少、新战斗/读档重开与同战斗延续，不改未定或其他玩法，不部署。

计划`1e7db443`落实，九组付款＋实际逐段伤害、失败/零成本/生命替代/回合/过期回调和全文脚本已编译；战斗域20生产断言已执行。597静态、13798生产/33编码、双构建五门与统一12/12通过无漂移；161双实例全文声明/56未识别。游戏六组仍not_run，不将纯对象隔离测试等同多人或实际读档验收。见`DESIGN_SYNC_20260927_BATCH73.md`。

## DS27-02AM：27张堕落普通机制牌全文及暗焰壁障（IMPLEMENTED，游戏内待验）

CARD-C-100～600/800正式完整条目→27牌独立Run/Hand全文与元数据/原生关键词、暗焰壁障费用/格挡/持续时间/减伤时序、似水年华标点→DS27-CARD-TEXT/EFFECT/META/GATES。前置`52a2e273`，设计无漂移；旧暗焰实现与当前明确要求冲突，按Plan回修。连锁破坏全文不解决Q12，不改任何未定或成人规则；未部署。

技术计划`965f2a63`后的实现完成：54份全文、全局/独立组接线与保留原行为测试；暗焰壁障实际费用/格挡/延长/伤害/敌方回合到期脚本，似水年华标点修复。590静态、13778生产/33编码、双构建五门及统一12/12通过。160双实例全文声明/57未识别，全部游戏待验，不部署。保留初轮本地化失败记录；新增瘴雷累计消耗差异为未完成项。详见`DESIGN_SYNC_20260927_BATCH72.md`。

## DS27-02AL / DS27-05P：书库及节制两处设计回修（IMPLEMENTED，游戏内待验）

设计输入`fd3a6fa5`→先古书库/ACT4节制→InsatiableGreed手牌持续邻接服务、出牌快照和红绿边框；新节制之戒/环及拾取奖励、试炼2/2/3→DS27-CARD-TEXT/EFFECT/META/COMPAT、DS27-ROUTE、DS27-GATES。实施前审查见`DESIGN_SYNC_20260928_LIBRARY_TEMPERANCE.md`；Q21用户已答持续手牌效果，Q22条件不成立。旧书库与节制成果回退READY；耐心和其他路线不变，不部署。

计划`5253596e`落实：LibraryHandAura/LibraryNeighbourRules/LibraryAuraOverlay、TemperancePileCmd及两张先古衍生牌、奖励保存幂等和2/2/3试炼完成；旧200断言迁环，新书库/戒和拾取保存场景单独接线。577静态、13778生产/33编码、双构建五门、统一12/12通过无漂移。227注册模型，133双实例全文声明/84未识别。第70批离线复验已收口；本批游戏六组仍未执行，未部署，见`DESIGN_SYNC_20260927_BATCH71.md`。

## DS27-02AK：初始及辅助五牌全文（IMPLEMENTED，游戏内待验）

CARD-N基础打防/变身/随身与困了→DS27-02AK→真Run/Hand全文、原生关键词排序、变奏边界回落重入及元数据→DS27-CARD-TEXT/META/COMPAT/GATES。前置`7dad6468`；数值和效果不变，测试需要一次性测试局，不部署、不以编译替代实机。

计划`f8ea5341`后代码已写，统一12子项通过但DesignDoc在运行中漂移，整体失败；本批8项和清单11项在新文档下再次通过，仍保留READY。新书库/节制变更不得继续沿用旧IMPLEMENTED证明。详见第70批检查点，未部署。

第71批已同步新设计并重新统一12/12通过、指纹稳定，当前更新IMPLEMENTED；上述失败是第70批历史证据。全部游戏验收仍未运行。

## DS27-02AJ：七牌双实例全文（IMPLEMENTED，游戏内待验）

CARD-N炎之剑/风神披风/理外锻成、CARD-C超再生/黑暗风暴/咒印传染、口球正式费用描述→DS27-02AJ→独立全文Run/Hand契约及炎之剑计数分行→DS27-CARD-TEXT/GATES。前置`118dec5d`；保持既有机制/效果测试，清单只登记声明，不冒充运行证据，不部署。

计划`74b2ab83`；七牌13份适用全文接线完成，计数隐藏时换行同步隐藏。新增7静态累计560、统一12/12，126双实例全文声明/89待核对；游戏六组未运行，整体未完成，未部署。见`DESIGN_SYNC_20260927_BATCH69.md`。

## DS27-07E：全卡文本证据分级（IMPLEMENTED，离线已执行）

DS27-CARD-TEXT/全注册模型→DS27-07E→人工审阅提供者清单＋源代码接线守卫＋逐卡未验状态→DS27-GATES。前置`54779591`；不改变原审计通用pendingText或宣称完整行为覆盖，不将脚本存在等同运行通过；区分真实Run实例、仅战斗和片段检查，保存可重跑的独立报告及负例，未部署。

计划`d67c40a5`；225模型完整清单已输出，215现行分119/5/2/89四级。新增9项结构/篡改/只读保护测试累计553，统一12/12、无源码漂移；游戏六组未运行、整体未完成，未部署。见`DESIGN_SYNC_20260927_BATCH68.md`，清单成功退出不等于目标完成。

## DS27-02AI：四张随身牌全文覆盖（IMPLEMENTED，游戏内待验）

CARD-H-900～929四张随身牌→DS27-02AI→HolyText 48张/96份独立文本、基础升级元数据与Run/Hand描述→DS27-CARD-TEXT/META/GATES。前置`145c7541`，不变更现有效果或意图；保留原测试目录和所有既有44牌预期，运行时仍待验，不部署。

计划`fe4060ba`；新增8份独立全文及4牌元数据，原入口扩为48张，不降低行为断言门槛。4新增静态含备注边界修改/缺失负例，累计544；统一12/12无漂移，双构建五门通过，未运行游戏或部署。见`DESIGN_SYNC_20260927_BATCH67.md`。

## DS27-02AH：心神宁静完整文本与门槛（IMPLEMENTED，游戏内待验）

EVENT-CARD-001→DS27-02AH→补正式描述“则”、基础/升级Run/Hand独立全文和其他手牌5/6/7张门槛→DS27-CARD-TEXT/EFFECT/GATES。前置`4cc86719`，不改变0费/保留/奖励规则；抽牌不足/禁止不伪造抽取收益，测试使用真实牌堆/打牌命令，未运行不标VERIFIED，不部署。

计划`4591b772`；13组真实牌堆/自动打出/收益和完整Run/Hand描述接入原CalmMind入口，最低90效果断言，已编译未执行。6新增静态累计540，统一12/12、无漂移、双构建五门通过；文字漏“则”已修，实际规则未改。详见`DESIGN_SYNC_20260927_BATCH66.md`。

## DS27-07D：继承关键词解析（IMPLEMENTED，离线已验）

四张正式状态牌消耗/虚无/保留→DS27-07D→AuditCardLocalization构造参数/只读字段来源解析→DS27-CARD-META/GATES。前置`ad418cb2`；仅验证工具变化，不改规则或意图。无法证明的集合/赋值保持未知；合成负例防止错误归零，真实四牌独立预期与strict-review不冒充全量验收。

计划`f555136c`；新增10项审计正反例，四牌设计与源码独立一致，534静态通过；全卡225模型明确失败0/未解析0。统一12/12、源码无漂移，双构建与五门通过，215全文标记/2仅设计及全部游戏验收仍未完成；未部署，见`DESIGN_SYNC_20260927_BATCH65.md`。

## DS27-02AG：手牌加费卡自身费用（IMPLEMENTED，游戏内待验）

正式诅咒牌口球1费条目→DS27-02AG→GagCurse构造费用与手牌条件回归→DS27-CARD-META/TEXT/EFFECT/GATES。前置`034db6bb`，纯非露骨数值同步，不改附加费规则/存档身份/意图；新增自身费用和离手恢复断言，不再把明确差异长期作为已知失败保留。统一无部署验证，游戏内仍待验。

计划`50f2f38c`；构造费用1、21项手牌条件/离手/重入最低效果断言和全文检查完成，游戏脚本编译未执行。524静态、13760生产/33编码、双构建和五门通过，统一12/12无源码漂移；审计明确失败归零但未解析和覆盖缺口保留，整体未完成、未部署。见`DESIGN_SYNC_20260927_BATCH64.md`。

## DS27-02AF：魔力爆发支付后预估（IMPLEMENTED，游戏内待验）

CARD-H-500～599魔力爆发/KW-OVERDRAFT-001→DS27-02AF→耐久支付后层数预估、增幅只读可用性、预览/实际伤害与资源断言→DS27-CARD-TEXT/EFFECT/COMPAT/GATES。前置`2daa1d95`；保留实际结算与增幅消耗规则，不部署。

计划`8f5648d5`；11组独立预览/伤害/剩余资源场景与直接预留钩子检查已编译，7新增静态通过；统一11/12、无漂移，游戏未运行。见`DESIGN_SYNC_20260927_BATCH63.md`。

## DS27-02AE：八牌全文与计算后缀（IMPLEMENTED，游戏内待验）

CARD-N-150～299/CARD-H-200～299、500～599、930～999八牌→DS27-02AE→独立基础/升级完整原文与Run/Hand显示、条件换行、复制复原描述→DS27-CARD-TEXT/EFFECT/GATES。前置`fe89d863`；补文本覆盖不代替原行为回归，Q13保持OPEN，不部署。

计划`dbf3905e`；两模板及格式器条件换行修复、八牌独立全文/动态与复原接线完成；新增9静态，实际格式器正反例通过。统一11/12、源码无漂移；原生脚本已编译未运行，见`DESIGN_SYNC_20260927_BATCH62.md`。

## DS27-02AD：有符号状态层数（IMPLEMENTED，游戏内待验）

正式负力量备注及CARD-N/H/C按正负状态层数公式→DS27-02AD→PowerLayerQuery按TypeForCurrentAmount和可见性选取后累加绝对值→DS27-CARD-EFFECT/TEXT/COMPAT/GATES。前置`2fcf8357`；不修改原版正负分类或卡牌基值，纯聚合离线执行与实际Power/命令测试分开报告，未部署。

同步`f74213dd`；负数层数遗漏已修复，13新增纯聚合断言累计13760、503静态/33编码及双构建四门/视觉通过。审判之刃、背水一战、高级治疗、圣咒、魂之冲击原测试追加实际原生状态/命令断言，尚未游戏执行。统一11/12，既有GagCurse费用差异保留，全目标未完成，见`DESIGN_SYNC_20260927_BATCH60.md`。

## DS27-02AC：理外锻成九附魔选择（IMPLEMENTED，游戏内待验）

CARD-N-150～299理外锻成/SYS-ENC-001→DS27-02AC→两阶段选择返回守卫、原九项合法候选和数值、临时附魔/永久隔离→DS27-CARD-EFFECT/META/TEXT/COMPAT/GATES。前置`60b3be08`；不改变附魔规则或随机选择概率，不部署。

同步`cf715fd6`；`ms_test_cards confirm BeyondReasonForge`原基础/升级测试扩充九项独立类型/数值/原文、合法目标集合和RNG及失效返回场景。498静态、13747/33、双构建四门和视觉门通过；统一11/12，既有费用差异不计通过。真实弹窗、自然中断、完整存读档及多人待验，见`DESIGN_SYNC_20260927_BATCH59.md`。

## DS27-02AB：多重再现额外回合时序（IMPLEMENTED，游戏内待验）

2026-10-01 DS27-02AB-R1：CARD-H-950～999最新DesignDoc费用2/1及玩家反馈的叠层故障→`MultipleReproduction.OnUpgrade`、`MultipleReproductionPower`的Counter总数与本/下回合独立保存计数、混合角标/提示→`DesignSyncExtraTurnContract`基本费用、重复延迟、混合时点、连续领取、旧存档迁移与原生Hook广播断言→DS27-CARD-EFFECT/COMPAT/GATES。旧“保留Single”的历史限定由本轮明确需求覆盖；源文件改动、构建及游戏手测结果另记，状态IMPLEMENTED/游戏内待验。DesignDoc另两处现存差异（SYS-TRF-002文案、娅露丝的书库章节移动）已由既有任务追踪；本轮不改玩家尚未提交的DesignDoc。

CARD-H-950～999多重再现→DS27-02AB→纯额外回合查询、拥有者回合开始到期、未到期广播保护、个人回合号与失效守卫→DS27-CARD-EFFECT/COMPAT/GATES。前置`242902d3`；不改Single堆叠/数值/关键词，不部署。

同步`680a9d32`；`ms_test_cards confirm MultipleReproduction`基础/升级契约已编译未运行，覆盖原生Ambergris共存、反复查询、同轮/其他玩家/失效回调及图标切换。491静态、13747/33、双构建四门和视觉门通过；统一11/12、既有GagCurse费用仍失败，源码无漂移。详见`DESIGN_SYNC_20260927_BATCH58.md`，自然回合与存读档仍待验。

## DS27-07C：竖向侧栏结构门（IMPLEMENTED，离线已执行；游戏视觉待验）

用户侧栏竖排/设置隐藏要求→既有MaidenSidebarRail/两资源控件→DS27-07C→正式图标/居中标签/竖排几何/设置隐藏结构校验及负例→DS27-GATES。前置`2b5e0961`；替代过时的横排坐标断言，不变更已确认布局或任何规则；运行时视觉仍待验，不部署。

同步`8a1a4c7f`；ValidateSidebarLayout20260927与17自测接入现有视觉门/统一入口，11/12实际通过，剩余全卡审计GagCurse费用不符。484静态、13747/33、双构建四门通过；结构校验不是引擎渲染验收，不因此关闭OPEN项或全量目标。见`DESIGN_SYNC_20260927_BATCH57.md`。

## DS27-07B：统一离线验证证据（IMPLEMENTED，已执行；全量验收未完成）

DS27-GATES→DS27-07B→固定无部署验证计划/独立日志/JSON/HEAD及源码漂移/严格状态→验证脚本自测和真实全量运行。前置`9fbbad9f`；不执行游戏内破坏性命令、不导入旧报告、不把部分检查或构建成功当全范围通过。

同步`699766b1`；12项实际运行10通过2既有失败，正确退出1，无源码漂移。新增9自测、累计467静态及13747/33通过，构建和四门通过；JSON明确游戏六大验收组not_run及goalCompleted=false。详见`DESIGN_SYNC_20260927_BATCH56.md`，不因此关闭开放问题或全量目标。

## DS27-01B：封印与献祭正式展示（IMPLEMENTED，游戏内待验）

SYS-SEA-001/ACT4-001→DS27-01B→封印方向悬停、实际永久实例/拥有者隔离、原牌升级预览、染色生命周期及献祭原文→DS27-CARD-TEXT/COMPAT/ACT4/GATES。前置`1d3f4daa`，不改封印/移除/试炼实际规则；未部署。RELIC-EVENT-004另列DS27-04J-Q20 OPEN，随机池范围待确认。

同步`46b308a3`；封印分方向说明、献祭原文及永久实例限定完成，节点复用恢复自己的色值、保留外部色值。207新增生产断言累计13747、7新增静态累计458、33编码与双构建四门通过；新增只读`ms_test_seal_view`编译未执行，不据此标视觉/真实战斗VERIFIED。旧失败保留，见`DESIGN_SYNC_20260927_BATCH55.md`。

## DS27-02AA：先古书库完整回归（READY，新设计回修）

`fd3a6fa5`将本节旧效果移往节制之环，当前书库改手牌持续相邻效果。以下为历史证据，不证明新规则实现。以DS27-02AL/05P为当前入口。

先古卡“娅露丝的书库”→DS27-02AA→InsatiableGreed/YarusLibraryPower→三牌堆实际选项、随机十张身份/顺序/上限、抽牌禁止及状态生命周期→DS27-CARD-EFFECT/META/COMPAT/GATES。前置`aa69ce99`，不变更随机流或规则，保留旧序列化ID；补选择返回守卫和独立期望的真实命令测试。未部署。

同步`266420e6`；`DesignSyncLibraryContract`接入原两变体，每个最低200效果断言，真实CardPlayStarted/Drawn历史和随机副本检查；15种牌堆/数量组合、不可打出牌及选择失败/取消/移除编译完成。8新增静态累计451、双构建四门与13540/33通过；自然回合/多人/存读档未验，旧失败未解除。见`DESIGN_SYNC_20260927_BATCH54.md`。

## DS27-02Z：圣言生成变化链（IMPLEMENTED，游戏内待验）

`SYS-SCR-001/CARD-H-700～799`四牌→DS27-02Z→三选一/目标身份/变化结果/升级/永久隔离/魔力解放→DS27-CARD-EFFECT/TEXT/COMPAT/GATES。前置`62c15347`，无漂移，未部署。魂之净化独立Q19 OPEN：每次立即先消耗再抽，还是每次先抽、整组重放后消耗一次；已询问，未改实现。

同步`f5372a69`；神圣惩戒从移入手牌改为真正原版抽牌，祝圣/惩戒/福音加入异步目标重验；四牌8变体实际命令/选择器/RNG/Drawn历史检查已编译。8新增静态累计443、双构建四门和13540/33通过；游戏内未执行、不标VERIFIED，旧失败保留，见`DESIGN_SYNC_20260927_BATCH53.md`。

## DS27-02Y：战术分析仪候选合法性（IMPLEMENTED，游戏内待验）

`CARD-H-800～899`战术分析仪、`SYS-ENC-001`、光之翼例外→DS27-02Y→升级且可稳定附魔的候选/返回重验/抽牌不丢失→DS27-CARD-EFFECT/TEXT/COMPAT/GATES。前置`024a9767`，无设计漂移；对齐原版普通附魔规则，不允许借修复覆盖已有附魔、不把临时效果写入DeckVersion，未部署。

同步`6e9b7416`；实际候选集合/双操作/光之翼共存/无候选/选择中资格变化的基础与升级测试接入原目录，每变体最低25效果断言；全文测试沿用圣洁全文契约。7新增静态、累计435、双构建四门及13540/33通过；脚本未游戏执行，不标VERIFIED，见`DESIGN_SYNC_20260927_BATCH52.md`。

## DS27-06D：亡灵集会选项回归（IMPLEMENTED，游戏内待验）

`EVENT-NEW-003`完整正文及Q9当前生命/具体整数澄清→DS27-06D→已有真实事件测试扩充全文、选择数量/等待、门槛、RNG和结果页断言→DS27-EVENT/COMPAT/GATES。前置`a28fb31b`，无设计漂移，不增补玩法、未部署。

同步`39c1967c`；测试扩充已完成，`ms_test_events confirm`仍只允许专用单人非战斗局，已编译未执行。7静态新增、累计428通过、既有13540/33、双构建四门通过；自然事件抽取/联机/存读档和真实弹窗不由静态或TestMode验证，旧失败保留，见`DESIGN_SYNC_20260927_BATCH51.md`。

## DS27-02X：圣言时点与生命周期（IMPLEMENTED，游戏内待验）

`SYS-SCR-001`六种圣言及圣光共鸣完整说明→DS27-02X→轻灵事件改为回合末/失效回调隔离/六牌全文及逐回合收益契约→WORD-1～4、LAYER-1、DS27-CARD-TEXT/GATES。前置`39dfee48`，无设计漂移；六独立图标现有映射保留，不改数值，不部署。

同步`a1f3dc10`；模板/惩戒文本→`DesignSyncScriptureContract`和`ds27-scriptures`入口、8静态、迁移后的结构门。累计421静态、13540/33既有断言、双构建四门通过。引擎内脚本仅编译，逐回合回调手动驱动；自然回合、多人、存读档仍待验，不标VERIFIED，见`DESIGN_SYNC_20260927_BATCH50.md`。

## DS27-02W：圣洁路线全文（IMPLEMENTED，游戏内待验）

本批44张`CARD-H-*`正式条目→DS27-02W→7处逐字/标点回修、88基础/升级独立全文/资源图标计数及Run/Hand验证→DS27-CARD-TEXT/GATES。前置`2eba001c`，无设计漂移，既有效果场景保留，不改规则/数值，不部署。

同步`3e0a0b9f`；`DesignSyncHolyTextContract`/选择器及7处正式文本完成。7新增静态累计413、13540/33既有断言、双构建四门通过；88变体的Run/Hand原生渲染及原卡效尚未执行，旧审计失败保留，见`DESIGN_SYNC_20260927_BATCH49.md`。

## DS27-04I：遗忘之魂（IMPLEMENTED，游戏内待验）

`RELIC-VANILLA-002`完整条目→DS27-04I→原版ForgottenSoul实例动态伤害/本角色隔离/正反变奏描述→DS27-RELIC/COMPAT/GATES。前置`4538afc9`，没有设计漂移。不将原版CharonsAshes误作遗忘之魂，不替换消耗命令或原生RNG，不部署。

同步`743a97fe`；`ForgottenSoulVariation`、变量与描述getter patch、独立本地化→`ms_test_forgotten_soul confirm`（已编译未运行）和7静态。累计406静态、既有13540/33、双构建四门通过，当前DLL原生回调也已只读核对；真实战斗与多人待验，旧视觉/审计失败保留，见`DESIGN_SYNC_20260927_BATCH48.md`。

## DS27-02V：中立牌全文（IMPLEMENTED，游戏内待验）

`CARD-N-100～399`本批26牌及冰晶碎片/功性魔防壁II～IV这4张衍生（共30牌）→DS27-02V→独立基础/升级Run与战斗全文断言/图标计数/梦色圣洁颜色修复→DS27-CARD-TEXT/GATES。前置`5da574c7`，无设计漂移；原效果测试保留，不据静态通过冒称渲染验收，不部署。

同步`68827522`；`DesignSyncNeutralTextContract`与`ds27-neutral-text`选择器完成，新增8静态、累计399通过，既有13540纯规则/33编码、双构建四门通过。游戏内60变体×Run/Hand全文及原效果场景尚未运行；旧审计问题保留，见`DESIGN_SYNC_20260927_BATCH47.md`。

## DS27-04H：眼罩（IMPLEMENTED，游戏内待验）

`RELIC-EVENT-001`完整条目→DS27-04H→原生只读遭遇队列预览/本地持有者意图及悬停隔离/精确描述→DS27-RELIC/COMPAT/GATES。前置`18c3dade`；不猜问号类型，不执行未来随机编成，不改意图结算，补序列/RNG不变与跨层读取测试，不部署。

同步`cab0314b`；`BlindfoldPresentation`/3个安全UI patch/眼罩拾取移除刷新与正式本地化→`ms_test_blindfold`只读游戏内契约（已编译未执行）及8静态。累计391静态、13540既有纯规则/33编码、双构建及四内容门通过。旧视觉/全卡审计失败保留，多人显示/自然跨层/实际存读档/手柄与鼠标悬停待验；见`DESIGN_SYNC_20260927_BATCH46.md`。

## DS27-02U：升级标量文本绑定检查（IMPLEMENTED，仅静态绑定检查）

全注册`CARD-*`升级数值显示→DS27-02U→只读源码/继承/本地化依赖审计与反例测试→DS27-CARD-TEXT/GATES。前置`a3f9c8ce`；避免局部修复后同类硬编码再现。仅验证可识别标量绑定，不能认定全部文本/机制正确，未知单列，不部署。

同步`e3cd5c63`；`AuditUpgradeTextBindings.py`/`TestUpgradeTextBindings20260927.py`→LocalizationStyle门。225注册、149绑定（6间接）、0失败；新增17静态用例，累计383，既有13540/33断言、双构建及四门通过。旧视觉/全卡审计失败保留，不标VERIFIED、不部署；范围限制和待验见`DESIGN_SYNC_20260927_BATCH45.md`。

## DS27-02T：破碎与随机多目标攻击（IMPLEMENTED，游戏内待验）

`CARD-N-150～299`破碎/两牌与堕落路线闪电踢击/渎神黄昏完整条目→DS27-02T→ShatterPower原生攻击类型过滤/气旋升级显示/冲击正式语序/`DesignSyncShatterRandomContract`→DS27效果/文本/COMPAT/GATES。前置`eb51da19`，同步`09fa1739`；不改设计数值。`ms_test_cards confirm ds27-shatter-random`已编译未运行，指定目标牌每变体26、随机牌14最低效果断言。366静态、既有13540生产规则/33编码、Debug/Release与四门通过。旧视觉/全卡审计仍失败；自然战斗/多人/完整存档/截图待验，未部署，见`DESIGN_SYNC_20260927_BATCH44.md`。

## DS27-02S：加速运动拾取复制（IMPLEMENTED，游戏内待验）

`CARD-N-150～299`完整条目→DS27-02S→`AcceleratedMotion.AfterCardChangedPiles`原生Run克隆/永久加入一次/战斗排除/保存标记→`DesignSyncAcceleratedMotionContract`→DS27效果/文本/COMPAT/GATES。前置`3441ef47`，同步`a543f051`。修复新建牌丢失附魔，保留数值和文本；每基础/升级场景至少41效果断言，入口`ms_test_cards confirm AcceleratedMotion`编译未运行。359静态、既有13540生产规则/33编码、Debug/Release及四门通过。旧视觉/全卡审计失败保留，完整存档/自然拾取/多人待验，未部署；见`DESIGN_SYNC_20260927_BATCH43.md`。

## DS27-02R：雷击、耀斑与瞬闪刺（IMPLEMENTED，游戏内待验）

`CARD-N-150～299`完整三牌条目→DS27-02R→`DesignSyncChainCopyContract`连锁/魔力解放与斩杀合并/仆从排除、战斗减费生命周期、原生复制继承与预览→DS27效果/文本/COMPAT/GATES。前置`5d27cd1d`，同步`dbacae2f`。保留符合设计的生产逻辑与正式卡面。定向入口`ms_test_cards confirm ds27-chain-copy`，每变体最低33/25/17效果断言，编译未运行；352静态、既有13540生产断言/33编码、Debug/Release及四门通过。自然回合/动画/完整存读档/多人未验；旧视觉317与全卡审计保留，未部署，见`DESIGN_SYNC_20260927_BATCH42.md`。

## DS27-02Q：索引与拾取附魔三剑（IMPLEMENTED，游戏内待验）

`CARD-N-150～299`完整四牌条目→DS27-02Q→`MagicIndexPower`有效性/状态文本、`DesignSyncEnchantmentInputContract`精确卡面/原生连续抽牌/真实拾取附魔及首次后续收益→DS27效果/文本/COMPAT/GATES。前置`19229c1e`，同步`c323a201`。保留正确递归抽牌顺序和三剑规则。7新增静态累计345，既有13540生产断言、33编码、Debug/Release及四内容门通过。`ms_test_cards confirm ds27-enchantment-input`编译未游戏执行；三剑每变体至少17效果断言、索引至少25；自然时序/视觉/完整存读档/多人待验。旧视觉和全卡审计失败保留，未部署，见`DESIGN_SYNC_20260927_BATCH41.md`。

## DS27-02P：冰界的女神（IMPLEMENTED，游戏内待验）

`CARD-N-150～299`完整条目→DS27-02P→`GoddessOfIcePower/GoddessOfIce`逐CardPlay资格快照/原生生成命令、升级状态文本/衍生悬停→`DesignSyncIceGoddessContract`及7静态检查→DS27效果/文本/COMPAT/GATES。前置`5bb83c8b`、同步`83b1a202`；不改Single堆叠及1张生成数值。319同步+19审计、13540生产规则、33编码、Debug/Release及四门通过。`ms_test_cards confirm GoddessOfIce`脚本编译未运行；每个基础/升级场景至少20效果断言。自然时序/完整存读档/联网/视觉待验，旧失败保留，未部署；见`DESIGN_SYNC_20260927_BATCH40.md`。

## DS27-04G：东尼的咒符（OPEN）

角色专属商店遗物→DS27-04G→Q18随机稀有牌卡池范围待用户明确。已只读核验原生CardPool仅中立、三路线合并服务和永久删牌钩子；不自行固化卡池规则。未实现、未部署。

## DS27-04F：反咒镜与神界星尘（IMPLEMENTED，游戏内待验）

前置`17284731`，同步`dea193fd`；Q10→`RELIC-CHAR-003/006`→DS27-04F→`ReactiveMagicRelics/ReactiveMagicRelicRules`、安全描述分支、原生负面变化/反应防递归、原生生成/每次实际出牌/计数保存→DS27效果/文本/COMPAT/GATES。数值原文不变；338新增生产断言累计13540、8新增静态累计331、33保存编码、Debug/Release和四内容门通过。`ms_test_reactive_relics confirm`编译未运行，复制品/神器/审判/重放/生成及保存用例待游戏执行。专用美术尚未映射，显式原版占位；自然时序/完整存读档/联网待验。视觉317及全卡旧差异保留，未部署，见`DESIGN_SYNC_20260927_BATCH39.md`。

## DS27-04E：祈祷耳环与结界生成装置（IMPLEMENTED，游戏内待验）

`RELIC-CHAR-001/004`→DS27-04E→普通/稀有两模型、原生变身成功钩子、本人回合跨战斗计数、文本与悬停→DS27文本/效果/COMPAT/GATES。前置`e0cb64f2`，同步`16bf64c9`；数值原文不变。采用AfterSideTurnStartLate避免圣域当回合扣除，当前DLL双阶段顺序已只读核验。317新增生产断言累计13202、8新增静态累计323、33编码、Debug/Release和四内容门通过。`ms_test_magic_relics confirm`已编译未执行；自然回合/完整存读档/联网与图标渲染待验。视觉317和全卡旧审计问题保留，未部署，见`DESIGN_SYNC_20260927_BATCH38.md`。

## DS27-04D：清心／浊心项链（IMPLEMENTED，游戏内待验）

`RELIC-CHAR-005`→实时变奏共用模型、稀有度与单一正常获取候选、旧ID兼容→DS27文本/COMPAT/GATES。Q10授权正式化完整条目；不改设计数值。前置`108cdc45`，需求/Plan同步`8802aebc`。132新增生产断言累计12885、8新增静态累计315、33编码及Debug/Release和四内容门通过；`ms_test_necklace confirm`已编译未执行，实际回合/资源/模型重建脚本不等于实机和完整存档验收。视觉317和全卡旧差异保留；既有存档抓取袋不整表迁移，未部署，见`DESIGN_SYNC_20260927_BATCH37.md`。

## DS27-05O：正式领奖与末战安全时序（单人IMPLEMENTED，游戏内待验）

前置`46e636b1`；ACT4-001→当前PCK原版宝箱holder/奖励快照、战后完整Hook等待、非战斗空闲入口、共用去重/旧档待领奖→DS27-ACT4/COMPAT。保留原生第三层结局，不自动授予或提前进入第四层；不改多人待定规则。新增5736生产断言累计12753、8接线/资源检查累计307、33编码、Debug/Release与四内容门通过。`ms_test_route_reward confirm`已编译未运行；原生资源路径/直接依赖只读核验通过，非渲染验收。视觉317/旧全卡审计不变，未部署；布局、完整保存和自然末战待手测，见`DESIGN_SYNC_20260927_BATCH36.md`。

## DS27-05N：正式试炼开场（单人IMPLEMENTED待手测；多人仍OPEN）

前置`477dd5a5`；ACT4-001及引用叙事→先古奖励前等待、公共/双栏/确认全文UI、已抽选项/不可反悔/恢复状态、沉睡遗物→DS27-ACT4/COMPAT。条件数值以主文档生产规则为准；单人本角色入口，多人既有路径不改。新增2944生产状态断言累计7017、8静态累计299、33编码、Debug/Release及四内容门通过；公共及十四段叙事逐字校验通过。实际模型/IL接线命令`ms_test_route_opening confirm`已编译未运行，自然开场、布局/手柄、整局保存退出恢复待手测。视觉317与全卡旧差异保留，未部署；正式奖励领取UI及最终Boss领奖另项未完，见`DESIGN_SYNC_20260927_BATCH35.md`。

## DS27-05M2：固定费用最终免费优先级（IMPLEMENTED待手测；X/Y仍OPEN）

前置`0fa4902f`；ACT4-001觉醒→实例附着免费能力、最终能量/星星/必需固定次级支付、原生清理及复制/保存隔离→DS27-ACT4/COMPAT。只覆盖UntilPlayed调用，X/Y Q17仍OPEN。新增8静态累计291、4073既有规则/33编码、Debug/Release和四内容门通过；动态凝神一斩、合成费用提供者、真实支付、复制/序列化脚本已编译未运行，修正旧AutoPlay扣费测试。视觉317及全卡旧审计保留，未部署。批次33动态加价READY由此项接续完成代码，非游戏内VERIFIED；见`DESIGN_SYNC_20260927_BATCH34.md`。

## DS27-05M：色欲/贞洁（部分IMPLEMENTED待手测；动态加价READY；X/Y语义OPEN）

前置`94e085ca`；ACT4-001→阶段2/3/3抽牌、首次实际增加、真实Draw返回值、跨回合固定费用免费；贞洁1/2/2阻止次数与本人回合阈值、状态名称/文本/悬停→DS27-ACT4/COMPAT。Q17免费X/Y支付及效果值已询问，未固化。原生和次级资源命令测试覆盖阻挡后不触发、恢复/Set不触发、原生费用生命周期与保存收据，游戏内未验。

8新静态累计283、4073既有生产规则、33编码、Debug/Release及四内容门通过；命令已编译未执行，未部署。动态加价在本地费用后执行的剩余缺口已从当前DLL确认，免费动态牌未完成；X/Y还未明确，不把“所有免费”标为完成。视觉317和全卡旧审计项保留；见`DESIGN_SYNC_20260927_BATCH33.md`。

## DS27-05L：贪婪免费商店（IMPLEMENTED待手测；预约事件集成未完成）

前置`4fa8efe8`；ACT4-001→实际问号选房/创建与资格保存、仅本人指定房间全价格为0、拾起防重和阶段文本→DS27-ACT4/COMPAT。预约事件尚未实现；只预留已接管房间优先的边界，不标Q8事件集成完成。旧档Active缺少位置时不得凭当前任意商店猜测免费资格，保留Pending后续正确执行。

482新增生产断言，累计4073；8新增静态，累计275；33编码、Debug/Release及四内容门通过。实际游戏命令已编译未执行，不据此证明自然地图/网络/完整存读档已通过；视觉317和全卡既有差异未解决，未部署。见`DESIGN_SYNC_20260927_BATCH32.md`。

## DS27-05K：慷慨（拾起IMPLEMENTED待手测；供奉交互OPEN）

前置`156b75f6`；ACT4-001→残缺/完整拾取1/2移除与阶段文本/领取防重→原生选择器及保存回归。供奉互斥UI未实现；Q15多人共享宝箱的供奉时点与Q16非试炼阶段显示已询问。STS1指定安装目录不存在，当前仅完成STS2 LinkedRewardSet及宝箱分配接口审查。正式供奉与相应验证仍待完成，不缩减为普通跳过计数。

新增8静态，合计267通过；3591既有规则、33编码、Debug/Release及四内容门通过；167项真实模型/选择器断言入口已编译未执行。视觉317、全卡旧差异仍在，未部署；见`DESIGN_SYNC_20260927_BATCH31.md`。完成后保存收据防重已覆盖脚本，选择中退出恢复不宣称通过。

## DS27-05J：傲慢/自负（IMPLEMENTED，游戏内待验）

前置`8d47ab15`；ACT4-001傲慢→DS27-05J→保持正确的阶段数值/未格挡判定/可净化类型、修正正式文案和悬停→逐阶段文本检查及真实伤害/格挡/净化/保存测试。未实机验收，不改写已符合设计的扣层机制。

新增8静态（累计259），既有3591生产断言、33编码断言、Debug/Release与四内容门通过。`ms_test_pride confirm`已编译未执行；视觉317及全卡旧审计项仍在，未部署，见`DESIGN_SYNC_20260927_BATCH30.md`。

## DS27-05I：嫉妒（IMPLEMENTED，游戏内待验）

前置`1e191366`；ACT4-001→原生负面变化判定、本人施加与同战斗过滤、每战斗/本人回合首次、精确阶段描述→生产规则/原生命令/保存与文本回归。设计没有敌人限定，移除该错误过滤；当前原生血肉巧技与人造物使用GetTypeForAmount识别降低力量等负面变化。游戏内待验。

170新生产断言（累计3591）、8新静态（累计251）、33编码断言、Debug/Release及四内容门通过。游戏内命令`ms_test_envy confirm`已编译未运行；视觉317与全卡旧审计项仍在，未部署，见`DESIGN_SYNC_20260927_BATCH29.md`。

## DS27-05H：懒惰（IMPLEMENTED，游戏内待验）

前置`1fe44576`；ACT4-001→实际能量支付、本人回合末阈值、下一本人回合奖励、跨战斗清理与整数存档→生产状态机/原生命令/精确描述测试。多次打出不多次计费，其他玩家不能消耗奖励；额外回合不依赖全局RoundNumber。

440新生产断言（累计3421）、8新静态（累计243）、33编码断言、Debug/Release与四内容门通过；视觉317和全卡旧审计项仍在。游戏内`ms_test_sloth confirm`已编译未执行、未部署，见`DESIGN_SYNC_20260927_BATCH28.md`。

## DS27-05G：愤怒（IMPLEMENTED，游戏内待验）

前置`1be1fd4a`；ACT4-001→愤怒生成副本重新可触发首次效果、觉醒仅对愤怒攻击牌固定+6、拾起附魔收据→生产数值过滤与真实打牌/复制/多层保存测试。普通克隆不全局重置其他已消耗附魔状态。

129新生产断言（累计2981）、8新静态（累计235）、33编码断言、Debug/Release及四内容门通过。视觉317及全卡旧差异仍在；游戏内`ms_test_wrath confirm`已编译未执行、未部署。见`DESIGN_SYNC_20260927_BATCH27.md`。

## DS27-05E：暴食（IMPLEMENTED，游戏内待验）

前置`14b7e373`；ACT4-001→DS27-05E→4最大生命/栏位、觉醒填空与本人用药增长→生产阶段/容量规则及实际命令测试。移除旧5点数值，保护药水Owner和重复拾起。

137新生产断言（累计2852）、8新静态（累计227）、33既有编码断言、Debug/Release及四内容门通过。视觉317/全卡旧差异仍在，`ms_test_gluttony confirm`未游戏内执行、未部署。见`DESIGN_SYNC_20260927_BATCH26.md`。

## DS27-05F：谦逊改写（OPEN，Q14）

谦逊数值改写/附魔保留/觉醒抽2为正式需求；复杂条件、多段、X及依赖被删除操作的伤害计算，还有原有关键词/被动触发如何删除，已向用户询问。未自行选择固定快照或保留条件的语义。其余路线不依赖该答案继续推进。

## DS27-05D：节制与耐心（节制READY回修；耐心IMPLEMENTED待验）

`fd3a6fa5`改变节制试炼首目标和全部奖励效果，转DS27-05P；耐心无设计变更。以下旧节制测试需替换，不能继续算当前实现证据。

前置`cc56366c`；ACT4-001→DS27-05D→可选0～2/3抽牌堆牌加消耗、分阶段圣洁生成与减费直到打出→生产规则/真实模型测试/文本对照。无额外附魔或升级。谦逊另列效果改写与可靠纯描述判定回修，尚未实现。

49新生产断言（累计2715）、8新静态（累计219）、33编码断言及Debug/Release/四内容门通过。视觉317、全卡审计旧项未解决；`ms_test_combat_virtues confirm`为未执行的破坏性Debug游戏内入口。未部署，见`DESIGN_SYNC_20260927_BATCH25.md`。

## DS27-05C：仁爱与勤勉效果（IMPLEMENTED，游戏内待验）

前置`77126085`；ACT4-001两美德阶段效果→DS27-05C→仁爱永久牌组随机升级/勤勉2、2、3次奖励及升级附魔→生产规则与模型测试。严格核实伶俐Adroit、灵巧Nimble，不使用错误的英文直译映射；其余路线/正式UI仍待实施。

新增203生产断言（累计2666）、8静态（累计211）、33既有编码断言；Debug/Release零警告零错误，四内容门通过。视觉门317与全卡审计旧差异仍在；模型级`ms_test_virtues confirm`已编译未执行，未部署、未游戏内验收。详见`DESIGN_SYNC_20260927_BATCH24.md`。

## DS27-05B：三段试炼主流程（IMPLEMENTED，游戏内待验）

前置`2d7b409a`；ACT4-001→42条件目录、独立保存状态机、沉睡/碎片/献祭/三次领奖→DS27-ACT4/COMPAT。重置累计、不追溯、状态阈值即时判断、奖励不重复、旧档已得效果不回滚。正式开场及慷慨互斥供奉UI、逐遗物效果仍待实现，不据主流程测试标全路线完成。

新增1432生产规则断言（累计2463）、8静态（累计203）、33既有编码断言、Debug/Release和四内容门通过；视觉门旧317失败、全卡已知差异仍在。实际Run存读档、界面与引擎命令未运行，未部署。慷慨只完成数据/状态机，正式供奉入口未完成；详见`DESIGN_SYNC_20260927_BATCH23.md`。

## DS27-05A：第四层空注册与结局（IMPLEMENTED，游戏内待验）

前置`8e30603f`；ACT4-001→DS27-05A→资格与开放入口分离、第三幕检查点、移除自动Boss觉醒、旧档未来占位Act归一化→DS27-ACT4/COMPAT。保留原版建筑师与胜利流程、当前已进入章节及其他Mod章节。生产规则直接执行与静态接线测试，完整游戏结局另验。Q7/Q8已确认；此批不代表42试炼及全部遗物效果已实现。

验证：新增687生产断言（累计1031）、8静态（累计195）、33编码断言，Debug/Release零警告零错误及四内容门通过。视觉门既有317失败、全卡审计已知差异仍在；原生结局/旧存档/联机未实机执行，未部署。详见`DESIGN_SYNC_20260927_BATCH22.md`。

## DS27-02O：咒印传染全类型与异步测试（IMPLEMENTED，游戏内待验）

前置`e6be28a8`；CARD-C-400～499咒印传染→DS27-02O→非附魔附加效果、全类型转移、动态文本/悬停及原任务异常保护→DS27-CARD-EFFECT/TEXT/COMPAT。8新静态（累计187）、344生产/33编码断言、Debug和四内容门通过，视觉门既有317失败。基础/升级全类型、复制/序列化、满手/空牌堆与异步测试各至少65条已编译未执行。未据猜测改动玩法，不部署，见`DESIGN_SYNC_20260927_BATCH21.md`。

## DS27-02N：引燃延迟目标身份（IMPLEMENTED，游戏内待验）

前置`d03fc8b7`；CARD-C-400～499引燃→DS27-02N→每次施放独立Power与实际牌引用、原生免费代打→DS27-CARD-EFFECT/COMPAT。修复同名误选和多次覆盖；8新静态（累计179）、344生产/33编码断言、Debug和四门通过，视觉门既有317失败。`ms_test_cards confirm Ignite`各版本至少35条测试已编译未运行，存档/自然回合待验。Q12/Q13连锁破坏/子守歌叠层已提问，仍OPEN子项，未固化规则。未部署，见`DESIGN_SYNC_20260927_BATCH20.md`。

## DS27-06C：原版事件追加结算与门槛（IMPLEMENTED，游戏内待验）

前置`fe6a49f0`，EVENT-VANILLA-001/002→DS27-06C→15选项拥有者/完成/重复隔离，10门槛选项、共生体1合法牌、反射事件RNG、正式文本→DS27-EVENT/COMPAT。24新生产断言（累计344）、8新静态（累计171）、33编码断言、Debug和四门通过，视觉门既有317失败。`ms_test_vanilla_events confirm`真实路线选项与受控完成探针已编译未运行；不宣称15个原版叙事流程已实测。跨存档/真实联机和退出交互待验，不改共享资源架构，不部署。见`DESIGN_SYNC_20260927_BATCH19.md`。

## DS27-02M：万咒之噬消耗快照（IMPLEMENTED，游戏内待验）

前置`744d4990`，CARD-C-400～499→DS27-02M→计算型攻击伤害和单次消耗快照、战斗隔离→DS27-CARD-EFFECT/COMPAT。异常/嵌套与全战斗牌堆纳入测试；永久牌组原生累积未证实，不作为已复现问题。8新静态（累计163项）、15新生产断言（规则累计320）及33编码断言通过，Debug和四门通过，视觉门既有317失败。真实命令及保存字段测试已编译未执行；游戏内未验、未部署。见`DESIGN_SYNC_20260927_BATCH18.md`。

## DS27-02L：十二牌文本回归（IMPLEMENTED，游戏内待验）

前置`fc20da7a`；CARD-C燃烧/消耗/力量、CARD-H压制/防御/断罪/附魔、CARD-N桥接完整条目→DS27-02L→精确卡面模板、原生关键词/动态预览与焚刃祭仪独立伤害→DS27-CARD-TEXT/EFFECT。12牌核对，8牌文案/布局回修，焚刃祭仪无可消耗手牌仍造成伤害。8新静态（累计155项）、305规则/33编码断言、Debug和四门通过，视觉门既有317失败；`ms_test_cards confirm ds27-text`基础/升级及动态/空手边界测试编译未执行。全量未完成，未部署，见`DESIGN_SYNC_20260927_BATCH17.md`。

## 1. Git基线

| 项目 | 值 |
|---|---|
| 初始DesignDoc基线 | `86d749d` |
| 基线日期 | 2026-07-26 |
| 基线用途 | 保存加入索引和治理协议前的908行DesignDoc |
| 本次变更前DesignDoc基线 | `d067a56` |
| 当前同步状态 | 2026-09-27：全量设计差异已审阅；Plan §25登记范围与Q1～Q11，等待澄清后实施；本表历史IMPLEMENTED不代表新规则已经实现 |

后续每次同步完成后，应将“当前同步状态”更新为对应提交ID和涉及的需求ID。

## 1.1 2026-08-07语义变更集

| 类型 | 需求ID | 变化 | Plan结论 |
|---|---|---|---|
| ADD | `DOC-MVP-001` | 新增角色MVP与下一轮的强制范围边界 | 新增MVP-0～MVP-3，优先于历史里程碑 |
| CHANGE | `SYS-DES-002A/B` | 将10点满值惩罚与5/8点控制联动拆开 | 10点规则留在MVP；5/8点规则迁移下一轮，MVP提示不显示 |
| CHANGE | `SYS-DES-003A/B` | 将玩家可确认的欲望来源与怪物意图来源拆开 | 前者允许进入MVP内容；后者迁移下一轮 |
| MIGRATE | `SYS-CTL-*`、`SYS-INV-*`、`SYS-DES-INTENT-*` | 控制、挣脱、侵犯及怪物欲望意图整体延期 | 已有框架保留；MVP默认关闭所有运行时影响 |
| MIGRATE | `KW-PORTABLE-001`、`CARD-C-700～799` | 随身与性技延期 | 从MVP卡池、百科统计和验收中排除 |
| MIGRATE | `MON-*`、`ACT4-001`及其他非核心系统 | 新怪物、Boss、第四幕等延期 | MVP复用原版内容和胜利流程 |
| CLARIFY | 已实现延期框架 | “代码存在”不等于“MVP启用” | 不删除代码；建立默认关闭的统一功能门和实验入口 |

影响结论：

- 存档：不删除既有字段或类型；MVP关闭延期模块时仍须安全读取已有字段。
- 战斗生命周期：仅保留堕落、欲望及MVP卡牌所需Hook；怪物意图注入不得运行。
- UI/本地化：MVP欲望提示移除5/8点控制说明；延期关键字不得出现在MVP卡牌上。
- RNG：关闭怪物Adapter后恢复原版意图RNG；路线奖励仍按`SYS-COR-003`消耗RNG。
- 回归：新增其他角色隔离、原版敌人AI、普通商店/Boss奖励/章节流程和存读档验收。
- 本轮只同步文档，不修改代码；实际隔离由`MVP-0`实施。

## 2. 核心机制追踪

| 需求ID | DesignDoc范围 | 设计成熟度 | Plan阶段 | 交付状态 | 验收入口 |
|---|---|---:|---|---|---|
| `DOC-SCOPE-001` | 文档定位与框架范围 | READY | 全阶段 | IMPLEMENTED | 文档审查 |
| `DOC-MVP-001` | 当前角色MVP与下一轮边界 | READY | MVP收口 | IMPLEMENTED | DesignDoc/Plan范围审查 |
| `DOC-ITER2-001` | 第二轮迭代交付范围 | READY | 待生成第二轮Plan | READY | 第二轮范围审查 |
| `DBG-PORT-001`～`DBG-OUT-001` | 类尖塔设计理念 | READY | 内容填充 | READY | 内容评审 |
| `START-001` | 中立0、堕落+3、圣洁-3 | READY | 20261003选角与初始化 | IMPLEMENTED | START-ROUTE-SELECT/LOAD待实机 |
| `START-003` | 天音以最终堕落≥3/≤-3首次通关解锁对应开局，档案持久化 | READY | 20261003通关解锁 | IMPLEMENTED | START-ROUTE-VICTORY/PROFILE待实机 |
| `START-002` | 外观、解锁、初始遗物选择 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-COR-001` | 堕落范围、UI和角色隔离 | READY | M0/M1 | IMPLEMENTED | M0/M1回归 |
| `SYS-COR-002` | 堕落增减与每Run一次行为 | OPEN | M0～内容填充 | IMPLEMENTED（框架） | 控制台/事件回归 |
| `SYS-COR-003` | 路线奖励概率 | READY | M1 | IMPLEMENTED | M1路线奖励 |
| `SYS-SEA-001` | 封印区、跨战斗快照和火堆移除 | READY | M1 | IMPLEMENTED | REST-3/封印回归 |
| `SYS-COR-004` | 事件门槛和遗物阈值 | OPEN | 下一轮内容填充 | DEFERRED | 不进入MVP验收 |
| `ACT4-001` | 第四幕、路线任务与路线Boss | OPEN | MVP回归＋后续内容 | IMPLEMENTED（MVP流程框架；待运行时回归） | 已恢复任务选择、进度、碎片、献祭、阶段替换、入场判定和占位Act；新敌人与Boss内容仍延期 |
| `SYS-DES-001` | 跨战斗欲望资源和UI | READY | M2 | IMPLEMENTED（左侧条战斗内外持续显示；战斗内同时显示RitsuLib计数器） | M2回归 |
| `SYS-DES-002A` | 10点欲望与高潮平复 | READY | MVP | IMPLEMENTED | M2核心回归 |
| `SYS-DES-002B` | 5/8点欲望与控制联动 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-DES-003A` | 卡牌/遗物/事件/火堆等欲望来源 | OPEN | MVP内容填充 | READY（框架） | MVP卡牌与Run回归 |
| `SYS-DES-003B` | 怪物意图增加欲望 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-CTL-001` | 控制格挡、Power与挣脱 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-CTL-002` | 弱怪晕眩/强怪低威胁恢复策略 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-INV-001` | 侵犯意图、诅咒来源和晕眩 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-INV-002` | 商店特殊移除全部精液类诅咒；每张奖励50金币 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-DES-INTENT-001` | 怪物增加欲望意图 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-ENC-001` | 战斗中临时附魔 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `KW-PORTABLE-001` | 随身 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `KW-CONDEMNATION-001` | 断罪与审判 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `KW-PURIFICATION-001` | 净化 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-SCR-001` | 六种圣言及持续Power | READY | 下一轮 | DEFERRED（样本保留） | 不进入MVP验收；来源牌不展开圣言卡图悬停预览 |
| `SYS-BLS-001` | Boss后光明/黑暗恩赐 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |

### 2.1 表现层接入追踪（2026-09-26）

| 交付ID | 关联需求 | 边界 | 交付状态 | 验收入口 |
|---|---|---|---|---|
| `PERF-CG-001` | `SYS-CTL-001`、`SYS-INV-001`、`SYS-DES-003A` | 成功结算后的可跳过CG；跳过不撤销规则 | IMPLEMENTED | `CutscenePlaybackService`、专项资源门、游戏内手测 |
| `PERF-CG-002` | `SYS-CTL-001`、`SYS-INV-001` | 怪物类型映射独立于UI，未知侵犯回退弱怪 | IMPLEMENTED | `MonsterPerformanceProfiles`映射审查 |
| `PERF-AUD-001` | `SYS-TRF-001/002`、`SYS-DES-001/002A` | 普通提示、成人演出分轨；循环单例；场景清理 | IMPLEMENTED | `PerformanceAudioService`、Debug构建 |
| `PERF-AUD-002` | `SYS-DES-003B`、`SYS-CTL-001`、`SYS-INV-001` | 只在效果实际执行/成功后播放，同怪同动作去重 | IMPLEMENTED | 意图入口审查、游戏内手测 |
| `PERF-CFG-001` | 表现设置降级 | RitsuLib主菜单CG/音频独立开关、普通/成人音量，全局保存；默认均关闭成人演出 | IMPLEMENTED；游戏内待验证 | `PerformanceSettings`、关闭态与持久化手测 |
| `PERF-REG-001` | `MP-001`、角色隔离 | 仅正确本地响木天音显示，不重复规则命令 | IMPLEMENTED；待多人手测 | `PerformanceAudience`、手测清单 |

### 2.2 历史记录UI回归追踪（2026-09-26）

| 交付ID | 关联需求 | 边界 | 交付状态 | 验收入口 |
|---|---|---|---|---|
| `UI-RUN-HISTORY-001` | 角色正式UI素材回归 | 只缩放历史记录中RitsuLib贴图工厂生成的响木天音头像；不修改共享素材和其他界面 | IMPLEMENTED；待游戏内验证 | `MaidenRunHistoryCharacterIconPatch`、结构契约、历史记录截图 |

## 3. 内容章节追踪

| ID范围 | 内容分类 | 当前成熟度 | 实现策略 |
|---|---|---:|---|
| `CARD-C-100～199` | 堕落欲望体系 | OPEN | MVP优先；单卡闭合后纳入资格清单 |
| `CARD-C-200～599、800～899` | 堕落原版机制体系 | OPEN | 不依赖延期机制的单卡可进入MVP |
| `CARD-C-600～699` | 临时附魔体系 | OPEN | 下一轮；框架保留 |
| `CARD-C-700～799` | 性技/控制利用 | DRAFT | 下一轮，不进入MVP |
| `CARD-H-100～199` | 低欲望超模与降欲望 | OPEN | 逐卡填充 |
| `CARD-H-200～599、800～899、950～999` | 圣洁原版机制体系 | OPEN | 不依赖延期机制的单卡可进入MVP |
| `CARD-H-600～699` | 断罪体系 | OPEN | 下一轮；框架保留 |
| `CARD-H-700～799` | 圣言体系 | OPEN | 下一轮；六种样本保留 |
| `CARD-H-900～949` | 控制应对与临时附魔 | DRAFT/OPEN | 下一轮，不进入MVP |
| `CARD-H-950～999` | 圣洁体系外卡 | OPEN | 逐卡填充 |
| `CARD-N-100～199` | 天平与过渡数值 | OPEN | 现有样本继续作为回归基线 |
| `CARD-N-200～299` | 打击、防御与随身 | OPEN | 原版机制牌可进MVP；随身/控制牌转下一轮 |
| `CARD-N-300～399` | 路线桥梁卡 | OPEN | 逐卡填充 |
| `STATUS-001～099` | 通用状态牌 | DRAFT/OPEN | 完整条目达到READY后实现 |
| `CURSE-001～099` | 通用诅咒牌 | DRAFT/OPEN | 完整条目达到READY后实现 |
| `CURSE-INV-001～099` | 侵犯注入诅咒 | OPEN | 下一轮；“精液”样本保留 |
| `RELIC-START-001～099` | 初始遗物 | OPEN | `RELIC-START-003`样本已实现 |
| `RELIC-CHAR-001～099` | 角色专属遗物 | DRAFT | 下一轮，不进入MVP |
| `RELIC-EVENT-001～099` | 事件遗物 | OPEN | 下一轮内容填充 |
| `EVENT-001～899` | 正式事件 | OPEN | 下一轮内容填充 |
| `EVENT-EASTER-001` | 炉石传说彩蛋 | DEPRECATED | 不实现 |
| `MON-001～899` | 普通/精英敌人 | DRAFT | 下一轮；MVP使用原版敌人且不注入Adapter |
| `MON-BOSS-C-001` | 堕落路线最终Boss | DRAFT | 下一轮，不进入MVP |
| `MP-001` | 每玩家资源独立 | READY | 下一轮 | DEFERRED |
| `MP-002～099` | 控制目标、协助挣脱等 | OPEN | 下一轮等待设计补充 |

### MVP卡牌内容交付状态

| ID范围 | 当前交付 | 下一验收 |
|---|---|---|
| `CARD-N-100～399` | 已实现条目待MVP资格审计 | 保留原版机制牌；排除随身/控制依赖；核对百科与奖励池 |
| `CARD-C-100～599、800～899` | 已实现条目待MVP资格审计 | 优先保留欲望及原版机制牌；逐张验证欲望支付、描述与升级 |
| `CARD-C-600～799` | 下一轮 | 临时附魔、性技和控制利用不进入MVP |
| `CARD-H-100～599、800～899、950～999` | 已实现条目待MVP资格审计 | 优先保留低欲望及原版机制牌；排除自定义延期机制依赖 |
| `CARD-H-600～799、900～949` | 下一轮 | 断罪、圣言、控制应对和临时附魔不进入MVP |

当前已具备完整示例规则的事件条目为：

- `EVENT-001`：光与暗的门扉，`READY`；
- `EVENT-002`：混沌芳香，`READY`；
- `EVENT-003`：低语空谷，`READY`。

初始遗物条目中：

- `RELIC-START-001`、`RELIC-START-002`仍为`OPEN`；
- `RELIC-START-003`已经由当前初始遗物样本实现，状态为`IMPLEMENTED`；
- `RELIC-START-004`规则完整，状态为`READY`。

## 4. 状态解释

- `IMPLEMENTED（框架）`表示公共扩展点和最小安全样本已经存在，但DesignDoc仍有内容或数值待填充。
- `IMPLEMENTED（样本）`表示机制由测试内容证明可运行，不代表正式内容全部完成。
- 未经游戏内统一手测的实现不得标为`VERIFIED`。
- DesignDoc规则变化后，本表对应项必须回退成熟度/交付状态并生成Plan回修任务。

## 5. 2026-08-22 第一轮迭代同步

| 类型 | 需求ID | 变化 | Plan结论 | 当前状态 |
|---|---|---|---|---|
| ADD | `DOC-ITER1-001` | MVP验收后启动第一轮完整范围 | 新增`ITER1-0～6` | READY（从时间戳MVP基线正向重做） |
| CHANGE | 基础牌章节 | 黑暗元素注册并实现为基础稀有度内容，但不加入普通初始牌组 | 初始牌组保持4打击、4防御和变身 | IMPLEMENTED（待运行时验收） |
| CHANGE | `SYS-DES-002A` | 满欲望按玩家回合立即、敌方回合/战斗外排队，并逐次结算 | `ITER1-1`重做时序和持久化计数 | IMPLEMENTED（待运行时验收） |
| CHANGE | `SYS-TRF-001/002`、`KW-MAGIC-AMP-001`、`KW-OVERDRAFT-001` | 变身、耐久、增幅、透支采用新规则 | `ITER1-1`重做并全卡回归 | IMPLEMENTED（待运行时验收） |
| ADD | `SYS-TRF-004` | 四档分层立绘与五类轻量动画 | `ITER1-3` | READY |
| CHANGE | `SYS-CTL-001/002`、`SYS-INV-001/002` | 闭合多来源挣脱、恢复意图和商店清理 | `ITER1-2`替换延期状态；2026-09-10挣脱改为RitsuLib卡实例capability；2026-09-11增加9场景真实运行时拘束套件 | IMPLEMENTED（静态门通过后仍待游戏内自动套件与人工存读档/视觉验收） |
| ADD | `SYS-DES-INTENT-001`、`MON-ERO-CATALOG-001`、`KW-STEADFAST-001` | 逐怪物色情意图、权重、次数、优先级与意志坚定 | `ITER1-2`；交付范围覆盖DesignDoc成熟度标签；2026-09-12统一“欲望攻击”正式名称并修正并排意图数值布局 | IMPLEMENTED（静态门通过，待游戏内视觉验收） |
| CHANGE | 全部卡牌范围 | 所有类型、费用、效果完整的单卡均纳入第一轮 | `ITER1-4`全量资格审计和实现；2026-09-12“战技复读”复制实例补齐DesignDoc要求的暗色边缘来源遮罩，并由实例capability约束生命周期；内部生命周期监听Power按玩家确认隐藏；2026-09-13修正RitsuLib百科过滤器排序，三路线154张正式牌及两张放行的初始基础牌统一显示，其他衍生牌继续排除；同日补齐守护圣言基础3格挡经原版格挡Hook受到敏捷等状态修正的数值测试与动态Power文本，并将精神统一的延迟非Powered格挡卡面预览与实际结算统一为不受敏捷修正 | IMPLEMENTED（静态门通过，待游戏内数值与显示验收） |
| CLARIFY | `ENCH-INFECTION-001` | 寄生在被附魔牌离手前传播至相邻手牌 | `ITER1-4`按新生命周期回修 | READY |
| ADD | 第四层路线遗物 | 各阶段描述严格分离 | `ITER1-5` | READY（保持MVP流程，只增量修改阶段文案） |

第一轮实现状态统一记为`READY`；完成代码、资源、构建和调试入口后改为`IMPLEMENTED`，不得提前标记`VERIFIED`。

## 6. 2026-09-14 第二轮迭代同步

| 类型 | 需求ID/范围 | 变化 | Plan结论 | 当前状态 |
|---|---|---|---|---|
| ADD | `DOC-ITER2-001` | 建立第二轮交付范围 | 生成第二轮Plan时以本范围为最高优先级 | READY |
| CHANGE | `SYS-DES-INTENT-001`、`MON-ERO-CATALOG-001` | 纳入诱惑度阈值、色情意图替换与逐怪物适配 | 第二轮核心机制 | READY/OPEN，以各正文成熟度为准 |
| ADD | `STATUS-001～099`相关完整条目 | 纳入撕裂衣服及倒刺钩、衣物燃烧、咬衣纸片、溶解液 | 第二轮怪物欲望攻击内容 | READY |
| CHANGE | 已实现卡牌与遗物 | 按当前DesignDoc效果增量回修 | 第二轮内容回归 | READY |
| ADD | 新增卡牌与遗物 | 仅纳入类型、费用和完整效果齐全的条目 | 第二轮内容实现 | READY；逐条资格审计 |
| EXCLUDE | 无名先古牌、待定诅咒、未完成内容 | 本轮暂时跳过 | 不生成实现任务 | DEFERRED |

第二轮不会自动纳入新敌人、第三层替换Boss、第四层最终Boss和多人游戏；这些内容继续按DesignDoc各章节成熟度处理。恶魔法杖维持既有实现，仅同步候选卡池需求说明。

2026-08-24 基线更正：第一轮开始时间为`2026-08-22 02:12:23 +08:00`，最后MVP源码写入时间为`2026-08-21 21:03:17 +08:00`。完整时间戳MVP树由提交`36764029f670c49b9b8298e40da399c3477735ab`保存。新的合并分支必须直接以此提交为父节点，再逐功能正向合入第一轮；禁止把MVP文件反向复制到错误第一轮树。

先前第一轮实现因错误基线作废，只保留为取证来源，不继承其`IMPLEMENTED`结论。新分支每个批次完成代码和MVP回归后单独标记`IMPLEMENTED`；统一运行时验收见`docs/ITERATION1_MANUAL_TEST_CHECKLIST.md`。

## 7. 2026-09-26 UI正式素材交接同步

| 类型 | 需求ID/范围 | 实现映射 | 当前状态 |
|---|---|---|---|
| CHANGE | `UI-ROUTE-V4-001` | `RouteCardVisuals`使用`maiden_route_wing_v4`贡献和两张V4正式翼饰；仅圣洁/堕落路线返回覆盖层，旧V3运行时资源移除 | IMPLEMENTED（待游戏内视觉验收） |
| CHANGE | `UI-CHAR-SELECT-V2-001` | `MaidenSuccubusCharacter`显式绑定V2背景、正常/锁定头像及新缓存名；`MaidenCharacterSelectVisualPatch`仅在选角按钮使用V2头像，顶栏Q版图标不变 | IMPLEMENTED（待游戏内视觉验收） |
| AUDIT | `UI-FORMAL-ASSET-AUDIT-001` | `完成版卡图/manifest.json`的130张正式卡图及默认图、火堆/商店正式素材、CG/音频清单均与运行时逐项哈希一致 | IMPLEMENTED（静态复验通过） |

本节不改变DesignDoc机制含义，也不把素材候选、V1选角背景或V3路线翼饰重新纳入运行时。

## 8. 2026-09-27 全面变更审阅与重新验收

`DS27-02E`（IMPLEMENTED，前置`83adb19b`）：新增梦色的颜料及8个现有模型数值/等级/关键词/显示回修；新增三路线抽牌生命周期、边界和基础/升级独立契约。6项新静态+47项既有检查通过；225注册，9模型/18场景编译未运行，入口`ms_test_cards confirm ds27-batch6`。Debug与四门通过，视觉门既有失败；未部署。魔力共鸣不直接改名；黑暗风暴升级附魔与光之翼多附魔未完成，另列后续完整实施。见`DESIGN_SYNC_20260927_BATCH6.md`。

`DS27-07A`（IMPLEMENTED，前置`01b58da2`）：全量文本/元数据审计基础回修。固定行号改结构定位，保留标点/多行，继承与注册属性纳入解析，未能证明的数值/渲染项目明确待验；输出独立报告，不覆盖并行审计产物。新增19项审计测试+既有28项通过；224模型全量审计仍报8项差异、3项反向覆盖和4模型未知关键词，严格模式不通过。Debug及四门通过，视觉门既有失败。范围仍为全卡及后续全机制，不缩减为此前小批白名单；见`DESIGN_SYNC_20260927_BATCH5.md`。

`DS27-02D`（IMPLEMENTED，前置`a404d031`）：10个圣洁模型核对，修正7牌、回归3牌；`DesignSyncHolyContract`及20基础/升级场景已编译，入口`ms_test_cards confirm ds27-holy`。新增6项静态检查及既有22项通过，Debug和四门通过；视觉门既有UI断言失败、运行时未执行、未部署。见`DESIGN_SYNC_20260927_BATCH4.md`。光之翼完整多重附魔另列待实现，不能宣告圣洁路线全量完成。

`DS27-02C`（IMPLEMENTED，前置`f0680239`）：`CARD-N-150～299`中14个模型的数值/稀有度/附魔/衍生链/文案回修完成；`DesignSyncNeutralContract`及CardEffect场景覆盖，入口`ms_test_cards confirm ds27-neutral`。8项新增静态检查及14项既有静态检查通过；Debug和四个内容/结构门通过，视觉门仍受既有诱惑度UI契约阻断。运行时未执行、未部署，详见`DESIGN_SYNC_20260927_BATCH3.md`；不改无关素材或未写完整设计。

### 澄清后的当前增量

`DS27-02K`（IMPLEMENTED，游戏内待验，前置`358710b2`）：CARD-N风神披风→EnergySpent实际支付、出牌开始预留/结束复制、多层/重放/嵌套及自身回合重置；正式卡面/Power同步，复制无永久牌组链接。8新静态（累计147项）、305规则/33编码断言通过，Debug0警告0错误及四门通过，VisualAssets既有317失败。`ms_test_cards confirm WindGodCloak`基础/升级真实支付、X费、华彩/堆叠、旋涡嵌套、满手和保存标志契约编译未运行；递归同实例的合成探针不当成自然实机结果。未部署，见`DESIGN_SYNC_20260927_BATCH16.md`。

`DS27-02J`（IMPLEMENTED，游戏内待验，前置`8350c0d8`）：CARD-N炎之剑→完成5场战斗成长与永久余烬、旧计数兼容、战斗内外剩余文本；DS27-CARD-TEXT/EFFECT/COMPAT覆盖永久牌组、生成/移除/克隆隔离、重复钩子及存档恢复。8新静态（累计139项）、305规则/33编码断言通过，Debug0警告0错误及四门通过，VisualAssets既有317失败。`ms_test_cards confirm FlameSword`基础/升级原生契约已编译未运行，旧6次出牌测试已删除，未部署。详见`DESIGN_SYNC_20260927_BATCH15.md`。

`DS27-02I`（IMPLEMENTED，游戏内待验，前置`1314b40a`）：全量审阅REMOVE条目→两张旧牌获取/生成/变化/百科过滤、能量过载禁止新附魔，225个原注册与旧序列化身份保留；不新增旧效果。显式兼容清单与审计门、10新静态/变异测试（累计131项）、305生产规则和33编码断言通过，Debug0警告0错误及四门通过，VisualAssets既有317失败。`ms_test_retired confirm`原生候选及基础/升级/附魔读档、克隆和普通卡隔离已编译未运行；全卡审计剩1费用差异及其他待验，不声明全量完成。未部署，见`DESIGN_SYNC_20260927_BATCH14.md`。

`DS27-02H`（IMPLEMENTED，游戏内待验，前置`2431d0ec`）：CARD-C-400～499超再生自身回手及不可选择同名牌、恶魔法杖自身消耗、黑色旋涡代打不扣增幅及中途新获得增幅支付；关联KW-OVERDRAFT-001/KW-MAGIC-AMP-001。实际消耗/返回、候选/支付、嵌套隔离与精确文案纳入测试。12新静态检查、累计121项通过；生产作用域15新断言，规则累计305、编码器33条通过；Debug零警告零错误和四内容门通过，既有VisualAssets317失败。三卡真实测试已编译未运行，游戏内/多人仍待验。未部署，见`DESIGN_SYNC_20260927_BATCH13.md`。

`DS27-02G`（IMPLEMENTED，游戏内待验，前置`34fa3e6b`）：CARD-H-930～949光之翼，稀有9/12与多重附魔；SYS-ENC-001真实子附魔保存/深克隆、直接数值/OnPlay聚合及原版钩子展开、仅该牌放开单槽、选择器/灵魂联结/克隆火堆/显示适配、普通卡隔离。15新静态检查（累计109）、33生产存档编码断言与既有290规则断言通过，Debug0警告0错误、四门通过；既有VisualAssets317失败未放宽。真实基础/升级出牌、子SavedProperty、旧单槽迁移、永久隔离与火堆测试已编译未运行；视觉/联网仍待验。详见`DESIGN_SYNC_20260927_BATCH12.md`。未部署。

`DS27-02F`（IMPLEMENTED，前置`a1f1e3d9`）：CARD-C-500～599黑暗风暴，固定8/2数值及升级华彩，消除新牌拾取附魔；SYS-ENC-001战斗与永久牌隔离、原版单槽不覆盖、真实重放/复制/读档/预览测试。旧持久化附魔不自动删除。8新静态检查，累计94项通过；290条既有生产规则断言通过。Debug及四门通过，视觉门既有失败；`ms_test_cards confirm DarkStorm`两场景已编译未运行。光之翼多重附魔仍未完成，不能用数值或文案同步替代。未部署，见`DESIGN_SYNC_20260927_BATCH11.md`。

`DS27-04C`（IMPLEMENTED，前置`b7e8794b`，设计`3379d42d`）：RELIC-START-002/004及START-002。英雄三分支与永恒保留/升级/原版沉眠精华、先古映射、选角左右按钮、每玩家大厅配置与新局初始化幂等；沿用既有独立第四层监听。临时与关键词保留区分、永久牌组不污染、已有附魔不覆盖、首次附魔减费一次及准备/离开/其他角色隔离纳入契约。28遗物，10新静态检查（累计86项）、66新生产规则断言（累计290条）通过；Debug及四门通过，视觉门既有失败。两个真实命令入口已编译未运行，受控牌投影、实机选角、自然弃牌、存档/多人仍待验；额外开局既有开关未开启，不宣告三开局已交付。未部署，见`DESIGN_SYNC_20260927_BATCH10.md`。

`DS27-04B`（IMPLEMENTED，前置`d983d84f`，设计`8343708b`）：RELIC-START-003全能宝珠回修与RELIC-START-005天穹宝珠，连续选择现有奖励/最后一张终止、恢复与最大生命、先古替换、本地玩家及其他角色隔离。关联第四层仅迁出遗物绑定的既有监听，按本角色玩家单次跑局订阅，不声明ACT4全量新试炼完成。新增8静态检查及132条生产规则断言通过，累计76项/224条；Debug及四门通过，视觉既有门失败。`ms_test_orbs confirm`已编译未运行、未部署。英雄/永恒、选角按钮仍待后续。详见`DESIGN_SYNC_20260927_BATCH9.md`。

`DS27-04A/06B`（IMPLEMENTED，前置`55e1f15a`，设计提交`0c1ba660`）：RELIC-EVENT-005灵魂罗盘和EVENT-NEW-002达弗的援助。落实Q9的概率百分点/中立区限定和Q10正式启用，三分支/稀有三选一/拾取三次普通奖励及避免双发；8新静态检查+此前60项通过、生产概率92断言通过，Debug及四门通过。25遗物；`ms_test_darv confirm`真实命令入口已编译未运行，视觉门既有失败，存档/多人/实际UI待验，未部署。详见`DESIGN_SYNC_20260927_BATCH8.md`。其余新事件、初始遗物切换、第四层尚未完成，不沿用旧实现状态。

`DS27-01A`（IMPLEMENTED，前置`fac99db7`）：SYS-TRF-001/002/004与KW-OVERDRAFT-001的统一耐久生命周期及形态互斥。初始3/上限5、33%减损、正数到0保留/0再损失退出、支付前检查、所有既有直接减层入口和正常形态增长限制；零层立绘与同形态提示同步。新增7静态检查通过，累计60项；8个游戏内真实命令场景已编译未运行，入口`ms_test_cards confirm ds27-transformation`。Debug及四门通过；VisualAssets既有断言失败，未部署。存档/跨战斗/实际UI仍待实机验收，详见`DESIGN_SYNC_20260927_BATCH7.md`；不将编译等同实机验收，不将DS27-01整体标完成。

最新确认（前置`46801cba`）：无名先古牌命名“娅露丝的书库”；黑暗之源采用先古稀有度；亡灵集会代价扣当前生命并显示实际整数。三项OPEN关闭，分别进入`DS27-02B`、`DS27-06A`（IMPLEMENTED，游戏内待验），不再等待设计。旧段落保留审阅历史，不代表仍待确认。

本批追踪：先古卡章节→`InsatiableGreed`/`YarusLibraryPower`/`DarkOrigin`与尘封魔典/古老牙齿注册→CardEffect三个探针；`EVENT-NEW-003`→`UndeadGathering`→`ms_test_events confirm`。累计14项静态测试、Debug无部署构建及四个内容/结构门通过；完整视觉门既有UI断言失败；真实游戏测试尚未执行。详见`DESIGN_SYNC_20260927_BATCH2.md`，不据此将全部DS27标为完成。

用户已确认大部分Q1～Q11；历史审阅表中的OPEN不再阻止独立已明确项。仍OPEN：无名先古牌名称、黑暗之源稀有度、亡灵集会生命代价。性侵犯或露骨性行为机制/演出未实施，不纳入本次实现。

| 需求 | 当前技术任务 | 状态 | 验收 |
|---|---|---|---|
| 瘴气转化、魂弗妮卡、使魔契约、回想房 | DS27-02A：费用/关键词、跳过与永久奖励、升级生成、抽牌来源补足 | IMPLEMENTED；游戏内待验 | 7项静态契约及Debug编译通过；真实命令测试已编译、未运行 |
| SYS-TRF-001/002/004、KW-OVERDRAFT-001 | 统一零耐久规则；0层无减伤，无实际损失奖励 | 设计确认；代码待实施 | 正数到0与0时再次损失分别验证 |
| ACT4-001、RELIC-START、灵魂罗盘 | 献祭条件、可见碎片、选角按钮、概率百分点 | 设计确认；代码待实施 | ACT4/EVENT/COMPAT |

本节状态更新至2026-09-29的`1b7898f1`；初始差异证据见[历史审阅报告](DESIGN_SYNC_20260927_REVIEW.md)和[原始差异](DESIGN_SYNC_20260927_RAW_DIFF.md)。Q1～Q20及后续补充已按对应批次追踪；完整游戏内验收尚未闭环。旧[状态核对](DESIGN_SYNC_20260927_CURRENT_STATUS.md)只作历史记录，不覆盖上方增量和本表当前状态。

| 需求范围 | 变化与结论 | Plan任务 | 当前状态 | 验收组 |
|---|---|---|---|---|
| `DOC-ITER2-001` | 以指定历史交付为保守基线，纳入所有文本和行为差异；保留并行修改 | DS27-00 | 审阅/初轮澄清完成；精确快照与全范围闭环仍有限制 | DS27-DOC |
| `SYS-TRF-001/002/004`、`KW-OVERDRAFT-001` | 上限5、减损33%、零层退出边界、异形态切换；旧完成结论回退 | DS27-01 | 01A IMPLEMENTED；游戏/保存恢复待验 | DS27-CARD-EFFECT/COMPAT |
| `SYS-SEA-001`、`KW-VARIATION-001` | 封印卡视觉/说明；基础牌变奏实际路线 | DS27-01/02 | 01B展示及既有变奏实现；实际快照/视觉/旧档待验 | DS27-CARD-META/TEXT/EFFECT |
| 全部`CARD-*`、`STATUS-*`、`CURSE-*`、`ENCH-*` | 所有卡面标点/排版/等级与行为；新增、移动和删除条目，旧存档兼容 | DS27-02 | 229模型静态审计失败0/元数据未解析0；219项待实际渲染、1项设计暂缓；游戏行为与旧档未全验 | DS27-CARD-META/TEXT/EFFECT |
| `SYS-DES-INTENT-*`、`MON-ERO-CATALOG-001`、`SYS-CTL-*`、`SYS-INV-*` | 逐怪物数值表、冷却与连续上限、阶段保护、意图恢复 | DS27-03 | 102怪物表含幻象怪意志坚定、状态机和定向脚本已接；逐怪物实战、恢复及保存验证未运行 | DS27-MON/COMPAT |
| `RELIC-*`、`START-002` | 新/改遗物、先古入口、事件来源、奖励与选择交互 | DS27-04 | 35遗物登记和事件/初始选择实现已有定向脚本；奖励、商店、存读档与多人待验 | DS27-EVENT/ACT4/COMPAT |
| `ACT4-001` | 14路线×3试炼与4显示形态，单次碎片、每段堕落、献祭只解锁；第四层空注册 | DS27-05/05A～05O | 主状态机、单人开场领奖、谦逊、供奉和预约已有实现/脚本；自然跨幕、X支付、存读档与多人待验 | DS27-ACT4/COMPAT |
| `EVENT-001～003` | 原版事件集成不回归 | DS27-06C | 实现和Debug回归入口完成；游戏内未运行 | DS27-EVENT |
| `EVENT-NEW-001～007` | 七新事件及所有页面、文本、门槛、强制替换 | DS27-06 | 七个正式事件类及定向脚本已接；真实选项、跨幕预约、Greed顺延与保存恢复未运行 | DS27-EVENT/COMPAT |
| 测试基础设施 | 原文/渲染/行为分层，不忽略标点、不依赖固定行号；静态与运行时分开报告 | DS27-07 | 15项离线入口已建立并曾全通过；游戏6组未执行，全范围覆盖待收口 | DS27-GATES |

未完成的新敌人、第三层替换Boss、第四层战斗及多人设计不因本轮审阅自动转为READY。澄清前不在程序中固化候选规则。

`MON-ERO-CATALOG-001`→`DS27-03`→`TestDesignSyncMonsterRoster20260927.test_roster_ids_exist_in_declared_game_version`：101个分配ID与`v0.111.0`怪物类逐一对应，定向6项通过。正常遭遇额外出现未分配的`PARAFRIGHT`，分类待用户确认；未修改玩法、怪物数值或DesignDoc，游戏内覆盖仍未运行。

`MON-ERO-CATALOG-001`→`DS27-03`→`EROTIC_ATTACK_ASSIGNMENTS.md`→`TestDesignSyncMonsterRoster20260927.test_recovery_moves_exist_in_versioned_state_machines`：骇鳗恢复键改用`THRASH_MOVE`而非方法名；26个指定恢复ID与版本化原版状态机对应。静态核验不替代实际挣脱和意图栈验证。

`MON-ERO-CATALOG-001`/`SYS-CTL-001`→`DS27-03`→`ControlIntentTestRunner.terror_eel_recovery_state`：实际怪物、拘束、打牌挣脱、代理恢复动作和原意图顺延共16个控制场景；结构门及Debug编译通过，游戏运行未执行，故不标VERIFIED。

`EVENT-NEW-004～006`→`DS27-06`→`DesignMassageEventContract.CheckAppointmentLifecycle`：隔离跑局依次验证第二幕/第三幕第二个问号预约与一次消费、访问历史、非本角色不触发。源码/编译门通过；自然地图与实际事件页面仍未运行，不当作VERIFIED。

`MON-ERO-CATALOG-001`→`DS27-03`→`TestDesignSyncMonsterRoster20260927.test_every_intent_has_core_values_consumed_by_runtime_parser`：239个允许候选的核心欲望/伤害/次数、格挡/挣脱、侵犯诅咒格式均可被现有解析表达式读取，诅咒名称在运行时名单内；仅为静态正文契约，非全部附加效果的实战证明。

`MON-ERO-CATALOG-001`→`DS27-03`→`EROTIC_ATTACK_ASSIGNMENTS.md`/`EROTIC_ATTACK_INTENTS.md`→`EroticAttackCatalog.Build`→`TestDesignSyncMonsterRoster20260927.test_parafright_is_only_steadfast_without_erotic_intents`：用户确认胧光怪召唤的`PARAFRIGHT`为`S`；目录102个ID、18个意志坚定，幻象怪没有A/B/I、阈值或恢复动作。静态契约通过；召唤后实际Power施加和意图隔离待游戏内验收。


`START-001`/`SYS-TRF-004` → `ASSET-CEL-UI-20261001` → `图片素材/选角界面/V3竖向适配_20261001`、选角兼容资源与`maiden_succubus_merchant.tscn`：原版/Hornet132×195、TheQueen131×194；正常/锁定竖图132×195、原生88×130蒙版，商店有效高度+15%、脚底不变。资源定向检查及完整门见本批记录；IMPLEMENTED，实机待验，未部署。遗物43枚V3单独为美术候选，未进入正式运行时，不标VERIFIED。

本批最终验证：43枚遗物RGBA/512/64/清单哈希、两张选角132×195/源运行时一致性及商店有效脚底保持全部通过；ValidateVisualAssets.ps1完整视觉门通过；Debug --no-restore -p:DeployMod=false -p:ValidateMod=true完整构建通过，0警告0错误。首次构建碰到并行演出代码暂时不一致，相关引用由独立批次同步后，本批重新完整构建通过。仍未部署，游戏内复验NOT_RUN，角色资源标IMPLEMENTED，遗物仍为待审阅候选。


`START-001` / `UI-CHAR-SELECT-V2-001` → `ART-CHAR-SELECT-ORIGINAL-V4-20261001` → `图片素材/选角界面/V4原作画风_20261001`、V2正式同步别名及character_select三张兼容PNG：原作主菜单两种天音身份/画风，大图天平与光暗分隔，图标132×195适配88×130。静态尺寸/哈希和完整资源门见本批记录；IMPLEMENTED，原作风格与UI遮挡待用户游戏内复验，未部署。

本批最终验证：三张资源尺寸、全不透明PNG、manifest哈希、V4权威源/V2兼容源/运行时逐字节一致及旧版本留档全部通过；Debug --no-restore -p:DeployMod=false -p:ValidateMod=true完整构建与资源门通过，0警告0错误。未部署，游戏内复验NOT_RUN，IMPLEMENTED。


`SYS-TRF-004` → `MERCHANT-SIZE-V2-20261001` → `maiden_succubus_merchant.tscn`中性Node2D根/内部Visuals Sprite2D，`图片素材/商店立绘/尺寸修正V2_20261001/comparison.json`记录RitsuLib 0.4.64复制根变换且保留原节点的二次缩放证据。可见高度由源码上次311.9→515.9、安装旧235.8→515.9，对照原版460.8～557.4setup。定向几何检查/完整资源门/构建见验证记录；IMPLEMENTED，实际身高、脚底、多人及按钮遮挡待实机，不部署，不把setup静态范围当动画帧验收。

本批最终验证：PNG哈希保持、版本场景/源码一致、RitsuLib转换后高度515.9/脚底0、原版setup范围及1/2/4人原生布局几何检查全部通过；Debug --no-restore -p:DeployMod=false -p:ValidateMod=true完整构建和资源门通过，0警告0错误。未部署，游戏内复验NOT_RUN，IMPLEMENTED。


## 2026-10-02 第二轮玩家反馈修复（PLAYER-ROUND2-20261002，IMPLEMENTED）

用户直接授权六项：试炼遗物内层能量参数、死亡复活/爆炸等特殊意图优先、恶魔法杖消耗引号、燃烧逐段、事件选牌堕落概率、满欲望语音去重。具体路径、原生两版本检查与实测入口见 docs/PLAYER_ROUND2_20261002.md。事件范围按本轮用户修订覆盖旧设计限定，燃烧仅补敌人多段结算；保留用户 DesignDoc 不改写。仅编译，不运行静态测试、暂不部署到游戏或上传器，既有上传器 JSON 不变。状态 IMPLEMENTED，游戏内待用户验证。

追踪：SYS-COR-003→事件奖励扩展/RouteCardRewardService；SYS-DES-001/002→DesireAmountState/DesireResourceRules音频标记；SYS-CTL/INV→NativeIntentPriority/IntentMoveFactory/SetMoveImmediate补丁；CARD-C-300～399燃烧→BurningPerHitPatch/MvpDebuffPowers；第四路线遗物→TwinSoulChaliceDescriptionPatch内层参数；恶魔法杖→zhs/cards.json关键字文案。

PLAYER-ROUND2-20261002 构建结果：Debug `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 成功，0警告0错误；未运行静态测试、未部署。


## 2026-10-02 第二轮追加反馈（PLAYER-ROUND2-FOLLOWUP-20261002，IMPLEMENTED）

榨乳器与谦逊遗物生成的牌未登记当前CombatState：日志确认原生抽牌/出牌注册断言失败，统一修正两处生成域并保留谦逊手牌选择；龟缩防御动态文本补“点”；变身悬停按用户两句重写；附魔动画 viewport 补本mod新增图标。范围与日志、原生API、实测入口详见 docs/PLAYER_ROUND2_FOLLOWUP_20261002.md。用户授权修复，保留DesignDoc及其他修改；不运行静态测试，暂不部署或更新ModUploader，现有上传器JSON不动。Debug构建成功，0警告0错误，游戏内待用户实测。

PLAYER-ROUND2-FOLLOWUP-20261002 验证命令：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`。最终构建成功，0警告0错误；未运行静态测试。本地PCK构建进程退出0，PCK_COMPLETE=648，包含恶魔法杖、龟缩防御与变身说明最新本地化；DLL/PDB/PCK只保存在聊天outputs/player-round2-debug-20261002。资源构建日志有环境证书/Sentry初始化告警，不影响PCK完成；未进行游戏内验收或部署、未更新上传器JSON。


### 2026-10-03 ACT4-001 首次开局兼容

FourthRouteLifecycle / FourthRouteQuestSelectionPatch 在单人地图入口复用 FourthRouteOpeningScreen，解决首次没有 Neow 时出现旧界面；根据 NeedsOpening 兼容叙事中断恢复。FourthRouteOpeningPatch 记录等待点注入、Neow 入口和补丁 owners。试炼条件、奖励及旧跑局已选路线不变；IMPLEMENTED，待首局无 Neow、正常有 Neow、选定后中断恢复手测。

## 2026-10-03 已审定三张变奏异画接入（IMPLEMENTED，游戏内待验）

`KW-VARIATION-001` 与初始三牌既有阈值逻辑 → 正式异画清单／运行时模型取图／视觉资源门。变身堕落态及黑暗元素、黑暗之源圣洁态使用各自独立图；基础资源不变，形态按现有 RouteKind 实时选择。前置 HEAD `83cf2ca945a899e2044e8cc1a09683e7d2f19cfd`，无玩法数值变更；游戏内画面切换待验。


## 2026-10-03 可选多重附魔兼容（IMPLEMENTED）

SYS-ENC-001 / CARD-N-400～499 → MULTIENCHANT-COMPAT-20261003 → MultiEnchantmentCompatibility / LayeredEnchantments / CombatEnchantmentCmd / BeyondReasonForge。外部API管理附加槽与战斗生命周期，Has<T>识别外部槽，单独启用时仍保留全附魔跳过；合法目标为零时不提示。CARD-N终极耀斑文本 → cards.json / DesignSyncChainCopyContract，条件和数值不变。游戏内NOT_RUN，未运行静态测试，未部署。

## 2026-10-03 遗物变奏异画接入（IMPLEMENTED，游戏内待验）

`KW-VARIATION-001`、`RELIC-START-001/002`、`RELIC-CHAR-002` 与原版遗忘之魂既有变奏规则 → 8 张审定独立图像／小图、大图、轮廓图／运行时阈值切换／视觉资源验证。门槛沿用现有玩法实现，不调整数值或存档。女神试炼界面 4 张背景在聊天工作区待审，暂不计正式资源。DesignDoc 的其他未提交变更未改动；本轮不部署，游戏内待验。


## ART-VARIATION-RUNTIME-20261003（IMPLEMENTED）

KW-VARIATION-001 / START-002 / RELIC-START-002/003 / RELIC-CHAR-001/002/003 / 已有RELIC-VANILLA-002视觉 → 正式卡图和遗物异画manifest / CardArtAssets / 遗物AssetProfile与ForgottenSoulVariationPatch / VariationArtRefreshPatch。五张先古牌只换图；三张卡牌和八张遗物异画按既有效果阈值切换，可见节点随所属RunState堕落值变动更新，大图缓存重置；游戏内验收NOT_RUN。


## TRIAL-BACKGROUND-20261003（IMPLEMENTED）

ACT4-001公共开场叙事/左右双栏/选择后全屏叙事 → 用户确认四张正式背景 → TrialBackgroundArt / FourthRouteOpeningScreen / FourthRouteSelectionScreen / 正式资源manifest。标题与选项分区缩放，确认/恢复时全屏图切换；滚动、按钮、存档和奖励规则保持既有实现。此前四张背景待审状态由本次授权更新为正式接入，游戏内验收NOT_RUN。


## GENEROSITY-CHOICE-20261003（IMPLEMENTED）

ACT4-001慷慨宝箱/战利品二选一 → 本机godot.log 3683/3710领取信号/重复跳过错误 → GenerosityLinkedRewardCompletionPatch → typed child callback / once parent signal / 原生RewardCollectedFrom收尾。KW-VARIATION-001视觉 → VariationArtRefreshPatch可空_model保护。原玩法不变，游戏内验收NOT_RUN；燃烧/眼罩补丁安装错误另外记录，不声明已修复。


## 2026-10-03 玩家反馈：燃烧、升级、卡池、欲望、开场衍生牌

前置快照 `9e61c6b61eaa5501a69b9709e93941c900f490c6`。SYS-COR-003：用户确认战斗与事件每张独立路线判定，DesignDoc本轮同步；SYS-DES-001/UI读档与心跳、SYS-CTL/挣脱短文本、CARD-C升级、燃烧逐段、原版夹击朝向与三路线生成、RELIC-EVENT-003及谦逊/耐心开场生成晚钩子。状态IMPLEMENTED，游戏待验。具体证据与验收见 `docs/PLAYER_DEBUG_20261003.md`；不运行静态测试，构建/部署待补充。


最终验证：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 成功，0警告0错误（最终耗时9.38秒）。初次构建因两个命名空间遗漏有2错误，已补齐；最终DLL采用原生逐段BeforeDamage回调及含LINQ闭包的生成池版本。未运行静态测试；游戏内NOT_RUN。

独立测试包：聊天`outputs/player-debug-20261003`，DLL/PDB及完整683资源PCK。PCK以已部署上一批资源为基准，仅替换本轮挣脱中文文本；其他资源内容逐项MD5保持原样。未操作ModUploader或安装目录JSON。检测正式游戏SlayTheSpire2进程PID8864仍在运行，部署暂未执行，修复已提交供关闭游戏后部署。


## 2026-10-03 三路线开局与通关解锁（START-001/003）

前置快照 `87ce00bb51f5f812aab6c345c4fd5fa096b4cb58`。用户直接要求中立0、堕落+3、圣洁-3开局，天音胜利时最终堕落值≥3/≤-3分别解锁。设计已独立提交，随后同步实现。本批沿用一个注册角色和既有初始遗物选择，不注册额外角色模型，不引入新美术。

技术：原生选角信息VBox中遗物上方增加路线行，沿用原生箭头/字体；未解锁显示条件，箭头仅遍历已解锁路线，已准备、锁定、随机、其他角色和离开屏幕不可操作。per-player大厅选择复用starter_relic_choice向后兼容字段；RitsuLib导入完成后的FinalizeStartingRelics初始化，并保存RouteApplied收据，继续存档不重置。堕落值仍沿用既有共享Run/首位天音规则，多天音不作新的独立堕落设计。解锁使用RitsuLib Profile作用域，OnEnded原生胜利完成后记录，仅本地天音、正常保存、非放弃、首次终局结算；普通战斗胜利、其他角色、失败、放弃与Debug不解锁。旧存档缺字段默认中立与未解锁。

验收ID START-ROUTE-SELECT/VICTORY/PROFILE/LOAD：首次≤-3及≥3通关，各自解锁并跨重启保留；±2不解锁；三种初始值及两种遗物独立切换；读档保留中途数值；档案切换、选其他角色、准备/取消、手柄、不同分辨率需用户实测。代码状态IMPLEMENTED，游戏内NOT_RUN，按用户要求不运行静态测试、不部署、不操作ModUploader及安装JSON。


构建交接：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`成功，0警告0错误。未运行静态测试，未进行游戏内验收；不部署至正式游戏、沙箱或ModUploader。独立产物保存到聊天outputs/start-routes-20261003，复用上一批完整PCK（本批无资源文件变更）。


## 2026-10-03 原版转阶段晕眩优先级（SYS-CTL-001 / MON-ERO-CATALOG-001）

前置快照 `6ec90dcdc3ac64e3788c6a48d3ed516e63033fa3`。用户反馈仪式鹿（原版CeremonialBeast/仪式兽）挣脱后的晕眩导致一阶段滞留。本轮只修复原生重要状态与本模组临时行动交接，不修改怪物数值、通关流程或分配表。设计独立提交后同步本技术任务；状态IMPLEMENTED，实机NOT_RUN。

原版源码证据：PlowPower解除冲撞会调用SetStunned（设置二阶段旗标），随后CreatureCmd.Stun携带StunnedMove动作与BEAST_CRY_MOVE后继；本模组原有优先级在IsPerformingMove时拒绝所有非固定名字的晕眩请求，因此该原版STUNNED可能被CanTransitionAway挡掉。ForceStun又复用了原版STUNNED状态名；普通模组Stun走原版StateLog回退，还可能丢弃正在暂存的下一原生行动。最近本机日志没有仪式兽复现记录，不能宣称已游戏复现；日志中的其他模组旧API错误不属于本次故障证据。

技术任务：①原版STUNNED携带后继ID时作为必须保留的原生中断，执行中的色情行动也允许切入，保留整个MoveState动作/后继，不构造通用替身；②清除被原生中断取代的ForceStun标志，原版已forceTransition的复活/爆炸也清除；③模组晕眩独立命名MAIDENSUCCUBUS_STUNNED，保持一次晕眩和原生续接，不与原版STUNNED争用；④本模组卡牌在色情行动上请求普通击晕时直接走自有续接路径，不从StateLog回到旧阶段；⑤自有晕眩继续受优先级保护，避免次回合被色情判定替换；旧STUNNED仍兼容识别。SetMoveImmediate补丁使用Safe.Run。

同类源码审阅：PlowPower仪式兽BEAST_CRY_MOVE、ShriekPower骇鳗TerrorState、AsleepPower沉睡唤醒SLASH_MOVE、SlumberPower唤醒ROLL_OUT_MOVE、BurrowedPower脱壳BITE_MOVE、FlutterPower落地后继及RavenousPower噬尸蛞蝓带回调的晕眩，均走同一原生STUNNED入口，保留回调/后继；残杀千足虫DEAD/REATTACH、实验体DEAD/RESPAWN和瀑布巨兽ABOUT_TO_BLOW/EXPLODE延续既有高优先级。未扩展其他怪物的普通攻击免疫，不全局禁止原版眩晕。

人工验收PHASE-STUN-01～04：仪式兽色情/恢复行动时打掉冲撞机制，确认原生晕眩后进入兽吼/二阶段；已在原版转阶段晕眩时挣脱不覆盖；普通怪物挣脱/侵犯仍只晕一回合且续接正常，不自循环；同类唤醒/落地/噬尸/复活/爆炸流程与中途存读档。既有手动控制套件仅同步自有晕眩的新ID，不新增或运行静态测试。

按用户最新指令暂不部署，不修改正式游戏、沙箱、ModUploader或安装manifest JSON。


构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误，10.70秒。未运行静态测试与游戏内验收。本轮继承上一批三路线开局及其他待部署修复；不部署，产物保存到聊天outputs/phase-priority-20261003。无资源改动，完整PCK沿用start-routes-20261003。


## 2026-10-03 娅露丝书库相邻牌仅边缘发光（DS27-02AL / CARD-N先古书库）

前置快照 `84f6788ff2e9b228532c40273c0185ba58cd6b4c`。用户截图显示绿色覆盖整张卡，违反DesignDoc现有“左侧红色发光边框、右侧绿色发光边框”要求；相对上一接受提交DesignDoc无新增漂移，完整复核书库相邻持续效果。只回修视觉，不改变相邻规则、消耗/重放、卡牌数值或卡图资源。

实现任务：LibraryAuraOverlay的StyleBoxFlat显式DrawCenter=false、ShadowSize=0、透明背景及零ShadowColor；以缓存的四圈圆角轮廓描边、由外向内透明度0.08/0.16/0.30/0.95模拟边缘柔光，最大外扩6px，中心没有任何几何填充或矩形阴影。左侧红、右侧绿，双效果红外圈/绿内圈间隔9px；继续继承卡牌坐标、旋转与缩放，忽略鼠标，原生可打出高亮保留。仅手牌中的实际相邻实例显示，移动/打出/书库离手后按原有查询刷新与清理。

验收LIBRARY-EDGE-01～04：左右单侧悬停大图与普通手牌均仅边缘显色、图和文字保持原色；两本书库中间牌红绿两条可辨；移动手牌/书库离手/打出后无残留；消耗与重放仍按原有规则生效。IMPLEMENTED，游戏内NOT_RUN；按用户要求不运行静态测试，暂不部署，不修改正式游戏/沙箱/ModUploader或安装JSON。新样式在DLL中构造，无新增资源/PCK变更。


构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误；未运行静态测试与游戏内验收。独立产物outputs/library-edge-20261003继承前两批待部署开局/转阶段修复，PCK原样复用phase-priority-20261003。继续暂不部署。


## 2026-10-03 七牌与慷慨/傲慢平衡修订（BALANCE-20261003 / DS27-02M / ACT4-001）

前置快照 `48bec13447ae62b6dfb1b8b1854a3e6af4b71a27`，已确认设计提交 `a52454d7ecbe027cb49638695aea74fe1df1abd9`。完整读取相对HEAD与上一接受版本DesignDoc逐行/词级差异，保留用户七牌、慷慨、傲慢及懒惰措辞编辑。七牌/两路线用户明确READY→本批IMPLEMENTED，游戏内NOT_RUN；旧段落历史数值仅为历史记录，以本批为准。懒惰同义文字同步本地化，机制无需代码变更。

| 对象 | 实现任务与验收 BALANCE-01～09 |
|---|---|
| 光子伏特 | 10/12伤害，欲望≤2增幅2/3；条件与费用不变 |
| 挥剑压制 | 8/10伤害，贞洁Counter1/2次，文本显示动态次数 |
| 审判之刃 | 7+负面层数×5/7，继续实时计算目标预览 |
| 反射屏障 | 打出/自身消耗分别7格挡+1增幅，升级虚无；两次触发各自独立 |
| 光之翼 | 12/16伤害，原生抽附魔牌和已有多重附魔保持 |
| 恶魔法杖 | 移除自身消耗关键词，不移除其他角色候选的消耗资格/升级过滤/本回合免费 |
| 万咒之噬 | 1×1基础，分别累计消耗攻击的单段伤害与次数，原生WithHitCount；快照同时固定两值，跨监听与重入一致；SavedProperty保留成长，旧存档缺次数默认1次 |
| 慷慨 | 首试炼目标原本1不变，残缺/完整拾取删牌均2，保持一次拾取收据与原生移除选择流程 |
| 傲慢 | 三试炼目标原本2不变；残缺/完整开战1/2，觉醒本人每回合开始2+2且不再额外开战3+3；自负现有每次未格挡失1层/力量已正确，去文字引号 |

万咒次数优先使用现有自动提取的原生攻击表达式（含固定多段/动态次数/谦逊改写），不把群攻敌人数或附魔重放次数当作攻击次数；未提取的外部牌按其Repeat/Hits/CalculatedHits/Repeats动态变量兼容，单次默认为1。伤害保持消耗时单段数值，不乘攻击次数。手测重点：消耗6伤害×2攻击后基础万咒应为7×3；成长牌相互消耗均使用同一事件开始值；各战斗牌堆都成长，永久牌组不成长；旧存档与克隆；反射打出与消耗分别触发；慷慨不足2张可删牌时原生选择规则；傲慢第一回合/多回合/受伤/净化。

遵循用户要求仅构建，不运行静态测试、不宣告运行时VERIFIED，继续暂不部署。不修改正式游戏、沙箱、ModUploader或安装manifest JSON。卡牌与遗物中文本地化会纳入完整待部署PCK。


构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误。未运行静态测试与游戏内验收；独立完整DLL/PDB/PCK保存聊天outputs/balance-20261003，继承此前待部署修复。继续暂不部署，不修改安装JSON。


## 2026-10-03 侵犯成功解除来源拘束（SYS-INV-001 / SYS-CTL-001）

前置快照 `0732a4c42f4b6b64beafb8d63a1821c1bb54df26`。已逐行/词级检查DesignDoc相对HEAD及上一接受提交，并全文复核3.1～3.5对应规则、Plan与追踪矩阵。最新用户明确规则已在DesignDoc 3.2：成功塞入诅咒后，该怪物本场战斗不再使用拘束或侵犯，且由该怪物发起的拘束解除，无Boss例外。因此DesignDoc不另改；修复实现遗漏并更新本轮技术追踪。

技术任务INVASION-RELEASE-01～05（IMPLEMENTED，游戏内NOT_RUN）：
- InvasionCmd.Resolve仅在AddCurse实际成功后立即设置已有SavedProperty载体ControlDisabled，早于CG与额外效果；自然候选与强制改意图原本均过滤该标志，不另设永久或怪物类型全局标志。
- 成功后通过原生ControlCmd.Release/PowerCmd.Remove解除本场战斗全部玩家身上由该怪物实例Applier发起的全部拘束类型，使用Direct原因；其他来源怪物即使同类型/同模型ID也不解除。恢复原牌及状态图标走既有ControlPower.AfterRemoved刷新，不伪造挣脱完成。
- InvasionCmd直接执行与CreateControl预选后执行均复核ControlDisabled，防止已缓存/外部入口再次结算；欲望攻击仍按已有独立规则选择。
- 贞洁防御或其他成功阻止入牌的路径返回false，不禁用敌人、不解除拘束。内置储存遗物此前定义为成功侵犯，延续该成功语义。
- 原有成功侵犯的收尾及原生转阶段优先级保留；解除拘束不额外插入恢复/晕眩，不重复结算。标志随怪物战斗状态保存，新的战斗/新怪物实例不继承。打印成功、来源ID和解除数量日志供复现。

人工验收：一敌多类拘束成功后全部解除且后续无拘束/侵犯；两只同模型敌人只解除实施成功侵犯者的拘束；B借A拘束成功侵犯时保留A的拘束、只禁用B；贞洁/防塞牌失败无禁用与解除；CG开关/跳过、Boss/转阶段、战斗存读档、下一场同怪物及多人来源隔离。按用户要求仅构建，不运行静态测试，继续暂不部署，不改正式游戏、沙箱、ModUploader或安装JSON。无需资源/PCK修改。


构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误。未运行静态测试/游戏内验收，继续暂不部署；聊天outputs/invasion-release-20261003保存本轮DLL/PDB及原样复用上一批完整PCK，包含此前待部署修复与平衡调整。


## 2026-10-03 用户授权仅部署本地游戏（LOCAL-DEPLOY-20261003-164145）

用户明确要求“仅部署到本地游戏目录”，覆盖此前暂不部署指令，仅此安装目录授权。最新源码完成提交`998769a6e07995d72d717085130cfff51ca7c94a`，部署前文档快照`64236d867095cd49acc9a006e4a6898e48ec7acf`；复用本轮已通过的Debug构建，0警告0错误，命令`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`。DLL/PDB/完整PCK累计包含此前待部署玩家反馈修复、三路线开局、原生重要意图保护、书库边缘光效、七牌及慷慨/傲慢平衡、侵犯成功解除来源拘束。

仅替换`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`下MaidenSuccubus.dll/.pdb/.pck，替换前备份`C:\Users\wilson\Documents\Codex\2026-09-30\amane-recovered-context\backups\local-invasion-release-deployment-20261003-164145`，安装后逐项SHA-256一致，原有JSON哈希不变。未改沙箱、ModUploader、上传包或其他游戏/模组目录。未运行静态测试与游戏内验收，用户实测后再标VERIFIED。部署明细：聊天outputs/invasion-release-20261003/local-deployment-20261003-164145.json。


## 2026-10-03 娅露丝书库原生轮廓发光（LIBRARY-NATIVE-GLOW-01～05）

前置快照 `827b8173a7b0f9087bd4bc1bef0d43a172455cfb`。用户截图确认细线描边不符合期望，应复用原版可打出卡牌的蓝色边缘光效并改色。DesignDoc相对最后接受提交无漂移，完整复核先古书库相邻规则与最新Plan/追踪：左邻消耗红光、右邻重放绿光，仅手牌相邻持续生效。本批替换上一LIBRARY-EDGE轮廓细线方案，CARD-N先古书库玩法不变，IMPLEMENTED、游戏内待验。

原生依据：v0.111.0 NHandCardHolder.UpdateCard使用CardHighlight.AnimShow/AnimHide和Modulate可打出蓝色/条件红色/金色；NCardHighlight复用TextureRect ShaderMaterial的width（0→0.075、0.5秒Cubic Out）。本批删除StyleBoxFlat所有描边/_Draw，创建原版NCardHighlight节点，直接使用当前卡牌高亮的纹理与同一Shader、独立Duplicate材质，仅改Modulate为红/绿，并调用原生AnimShow。节点不复制活跃Tween，不共享width参数，不新建贴图或shader。

受影响卡牌临时隐藏自身原蓝光leaf的SelfModulate Alpha以避免颜色混合，原版UpdateCard继续正常更新颜色与Tween；失去相邻关系/离手/离树时恢复保存的SelfModulate，其他牌继续原生蓝光。每帧跟随实际原生高亮的局部Position/Size/Scale/Rotation/PivotOffset与Texture；只有效果变化才刷新文本和播放动画。双效果使用同一原生轮廓的左右两份裁剪，左半红/右半绿，同时显示且不叠出黄光；裁剪外围足够宽以保留柔光。NCard池退出清理，避免复用到其他牌时颜色残留。

验收：左邻整体原生红光/右邻绿光、标题与插图保持原色；两书库中间牌红绿双色且仍具有消耗+重放；不可打出的受影响牌也显示对应关系；换位/离手后恢复普通可打出蓝光或原生不可打出状态；悬停/拖动/抽弃牌/卡池复用无残留、材质互不干扰。按用户要求仅编译，不运行静态测试。最新部署范围仅本地游戏，沙箱/ModUploader/安装JSON不动；构建完成后若游戏仍运行则等待退出，不替换占用DLL。资源无改动。


构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误。未运行静态测试/游戏内验收。独立产物聊天outputs/library-native-glow-20261003，包括DLL/PDB和原样复用此前完整PCK。实现提交已完成，安装部署单独记录，不写沙箱/上传器。
