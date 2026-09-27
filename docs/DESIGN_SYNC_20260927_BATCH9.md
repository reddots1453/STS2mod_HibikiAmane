# DS27-04B：全能、天穹宝珠与试炼监听解耦

实施日期2026-09-27～28。前置快照`d983d84f`；开始时DesignDoc逐行/词级均无漂移，完整复核初始遗物段。设计成熟度/天穹ID独立提交`8343708b`。本批IMPLEMENTED，未游戏内验收，未部署。

## 实现与兼容

- RELIC-START-003：保留`TwinSoulChalice`及存档ID；显示名称改为全能宝珠，三分支逐字同步DesignDoc。正区>=4战后增加5最大生命，其余恢复5；圣区<=-4允许连续选择同一组卡牌，不再另造额外奖励。最大生命沿用原版GainMaxHp命令（同时恢复对应生命）。
- RELIC-START-005：新增`SkyOrb`，Ancient稀有度；战后5最大生命、所有堕落值下多选。`RegisterTouchOfOrobasRefinement(typeof(SkyOrb))`使用RitsuLib正式映射，原版欧洛巴斯之触负责实际替换，不自行重写先古事件。
- 两种宝珠均限定拥有者与本Mod角色；奖励同时核验`cardReward.Player`。多选使用`ShouldAllowSelectingMoreCardRewards`原版循环，保留加入牌组、同步选择、动画、历史、跳过与可用的替代选项处理。
- 运行DLL确认该钩子在所选牌被移除之前调用，因此`offeredCount > 1`才继续。最后一张返回false，避免空选界面；并非通过增加选项数量实现多选。
- 变奏描述继续由已有Description补丁按当前堕落值选分支；正式“变奏”替代旧“升变”，去除额外奖励的错误文字。宝珠暂无专属图标，使用原版neows_lament占位；风味文字仅为不含效果的短句，不写入规则。

## 第四层迁移边界

`FourthRouteLifecycle`从遗物提取既有流程，用ConditionalWeakTable按Player缓存临时重入锁，存档状态仍为M5Progress。Mod初始化仅通过`SubscribeForRunStateHooks`注册一次，且仅返回活跃的本角色玩家实例。原版run+combat分发已包括run订阅，不能再注册combat流。

监听模型使用RegisterSingleton和公共无参构造供ModelDb初始化，实际每个玩家持有其MutableClone；不直接new已注册模型，避免启动构造失败或DuplicateModelException。此边界亦纳入静态检查。

迁移原有战前/进房阈值检查、胜利进度、药水、火堆恢复/锻造、牌组变化、碎片价格；药水事件补齐拥有者限定。地图选择查找本地本角色，不再查找TwinSoulChalice；保留地图稳定等待、互斥与finally恢复旅行。其他角色不进入该监听或地图选择。

没有把这些逻辑直接塞入现有CharacterModel：现有订阅仅为combat流，火堆和房间回调会遗漏。没有给整个CharacterModel增加run订阅，以免现有战斗回调执行两遍。

这只是依赖解耦；旧试炼目标和42项新规则整体同步尚未完成，不因本批迁移标ACT4完成。多人共享M5进度的最终规则也仍需后续验证。

## 已执行的验证

以下命令均在Mod目录执行：

- `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore`：0警告0错误。
- `dotnet run --project tests/DesignSyncContracts/DesignSyncContracts.csproj --no-restore`：宝珠132条新增断言、概率92条，共224条通过；直接链接生产OrbRules，不复制实现。独立布尔表覆盖11档堕落值，两宝珠、0/1/2/3/5剩余选项。
- `python -m unittest discover -s scripts -p 'Test*20260927.py'`：57项通过（本批新增8项）。
- `python scripts/TestCardLocalizationAudit.py`：19项通过；总静态检查76项。
- `ValidateCardEffectTests.ps1`、`ValidateMvpContent.ps1`、`ValidateLocalizationStyle.ps1`、`ValidateStructuralContracts.ps1`，参数`-ProjectDir .`：通过；225卡、26遗物、9附魔。
- `ValidateVisualAssets.ps1 -ProjectDir .`：既有第317行诱惑度UI断言失败，未改UI或放宽断言，不能声称全验证门通过。

过程中的构建缺失using/抽象属性已修复后复跑。新增遗物令上一批固定25计数断言失败，按明确新增SkyOrb更新为26后通过；先古映射属性放在RegisterRelic前，兼容既有严格注册扫描，未删注册断言。新增风味文本填齐内容门必需的非空键。

## 游戏内自动入口（已编译，未执行）

`ms_test_orbs confirm`：仅非战斗、单人、本角色的一次性存档。会移除遗物、重设生命/堕落值、增加大量牌、改写试炼进度；不得用于正式存档。限定范围启用TestMode和原版TestCardSelector，finally恢复TestMode。

- 11档×2宝珠：真实DynamicDescription格式化精确比较；直接调用遗物胜利钩子及原版生命命令，验证恢复、上限、最大生命增长。这里没有伪称已模拟整场胜利分发。
- 11档×2宝珠×0/1/2/3选牌，共88组真实CardReward/OfferCustom；断言奖励对象不增加、剩余候选缩减、无重复实例、实际牌组入牌数、跳过与最后一张关闭。
- 无任何遗物时检查实际run监听恰好一个；通过`Hook.AfterRestSiteSmith`验证进度只加一次。
- 原版`TouchOfOrobas.SetupForPlayer`检查映射，真正Obtain执行替换；再次通过原版火堆钩子验证试炼仍仅计一次。

仍需手测：自然战斗奖励选择/跳过/替代选项、末张关闭动画、事件奖励和原版其他多选遗物组合、真实整场胜利结算、地图弹窗稳定、存档往返、旧ID加载、不同开局及其他角色、多人同步与战斗/跑局监听唯一性。以上尚未运行，不标VERIFIED。

## 下一批

英雄/永恒宝珠、保留/升级/沉眠精华、选角遗物切换仍待实现；42试炼、剩余卡牌/附魔/事件与全面回归继续保留在原goal，不缩小范围。并行素材、共享审计报告和其他Agent日志未纳入本批提交。
