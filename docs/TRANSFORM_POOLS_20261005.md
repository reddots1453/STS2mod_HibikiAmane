

## 2026-10-05 默认随机变化四池候选（CARD-POOL-TRANSFORM-001）

前置快照`b99bd6b181ebd33df7f0c5c445c0668dbc07a2c0`，设计`f8e30dd306377516d49c1d6dadcd7ee7dddd66f6`；当前分支、HEAD、状态、DesignDoc对HEAD/89254411的逐行/词级差异已留档，其他修改保留。复核CARD-POOL-001、SYS-SCR-001、已有AllMaidenSuccubusCards与NativeGenerationPoolPatch、原生0.107.1/0.111 CardFactory默认变化/过滤/两种创建重载、CardTransformation、CardCmd.Transform与衍生池/侵犯池注册。已有补丁对普通生成来源合并三路线；遗漏MSGenerated，且衍生原牌/事件先古Token原牌走原版Colorless fallback时未识别原池。

任务READY：单独添加TransformationPools为三路线+MSGenerated；原三路线Pools及GetCanonicalCards/GetUnlockedCards接口不改，避免改变奖励、枯木树枝、普通药水等无关生成。NativeGenerationPoolPatch仅CardFactory.GetDefaultTransformationOptions调用GetUnlockedCards时插入original参数，调用GetTransformationCards；根据原牌Pool识别四池（含原生先选Colorless的衍生Token等情况），查询四池解锁候选、排除Retired、按ID去重稳定排序。其他原生卡牌/药水/遗物/Power调用仍GetGenerationCards三路线，其他角色池及侵犯诅咒原牌仍调用原GetUnlockedCards。保留原GetFilteredTransformationOptions、创建CardScope/战斗实例、RNG、变换动画、Hook与附魔拾取路径；指定牌/给定options重载不动。只在已有Transpiler扩展当前调用，不Prefix替换全方法，保留其他兼容补丁。

验收TF-01：战斗与事件普通打击/中立牌可变化为圣洁、堕落及满足过滤的衍生牌如IceMist；02天音三路线原牌/衍生原牌均默认四池，先古/Event/Token原牌仍遵守原稀有度过滤；03普通输入不产生Basic/Ancient/Event/Token/Status/Curse，战斗禁生牌及废弃牌不进入；04圣言限定转换/原版Begone等明确replacement/自定义options不扩大；05其他角色和Colorless/侵犯池保持原版，单人多人/解锁/随机数与附魔变化仍原生。0.107/0.111入口相同，用户要求仅build、静态测试NOT_RUN；编译后IMPLEMENTED，游戏内NOT_RUN，暂不部署本地/沙箱/ModUploader，不修改安装JSON。本轮四池含衍生为新明确授权，替代此前三池默认变化来源。


CARD-POOL-TRANSFORM-001 IMPLEMENTED（2026-10-05）：前置快照`b99bd6b181ebd33df7f0c5c445c0668dbc07a2c0`，设计`f8e30dd306377516d49c1d6dadcd7ee7dddd66f6`，计划`147bf7f76ea7c4632ddffacd42919dd4a857bc5d`。默认随机变化在CardFactory.GetDefaultTransformationOptions取解锁候选时识别原牌四池，合并中立/堕落/圣洁/MSGenerated，并按ID稳定去重、排除废弃牌；原生后续过滤与RNG/创建/动画/附魔不替换。该共用入口同时覆盖战斗内外；指定replacement/options和其他角色、奖励、商店及三路线普通生成不改。衍生池符合过滤的普通稀有度牌可进入候选，包括IceMist和六圣言（CanBeGeneratedByModifiers=false仍保证奖励/Modifier生成禁入；原版默认变化不使用该属性），这是本次用户四池变化规则的明确授权。反射/IL修改只在原调用前增加original实参，原call标签转移到新ldarg_0，维持完整原生筛选。构建命令`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误，17.02秒；按用户要求未跑静态测试，游戏内NOT_RUN，未部署。资源包继承已部署texture-memory包，不改卡图/本地化或安装JSON。
