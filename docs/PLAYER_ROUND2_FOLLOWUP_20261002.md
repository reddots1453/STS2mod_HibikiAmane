# 第二轮追加反馈修复（2026-10-02）

状态：IMPLEMENTED，待用户实测；不运行静态测试，暂不部署游戏或 ModUploader；所有上传器 JSON 保持原样。

## 实际修改

1. 榨乳器阻塞抽牌：最新本机 `godot.log` 第3182行明确显示回合循环异常终止，`MAIDEN_SUCCUBUS_CARD_MILK must be added to a CombatState before adding it to this pile`。堆栈在 DrawInternal 移入手牌时失败。Milker 原先 RunState.CreateCard 只登记跑局，首次放抽牌堆时卡尚未 IsInCombat，未被检查，抽牌时检查 CombatState.ContainsCard 才报错。改为当前 CombatState.CreateCard，继续使用 AddGeneratedCardToCombat、随机抽牌堆位置和原生预览/飞牌动画；损失3生命、生成2张、每场一次不变。
2. 谦逊无效果/悬停：用户 Downloads/godot (2).log 第19556、19644行明确显示 AddDuringManualCardPlay 的 `HUMILITY_LESSON must be added to a CombatState before playing it`，异常发生于 OnPlay/选择前。HumilityRouteRelic 使用与乳汁相同的错误生成域。改用 CombatState.CreateCard，再由 AddGeneratedCardToCombat 放手牌，原生历史、同步和UI处理一致。保留现有 FromHand、手动确认、费用不限制、零候选跳过与选后复核，不再通过切换为网格掩盖生成问题。最新本机日志只证实 Milk 开局阻塞，无独立本轮谦逊选择异常，需用户在新生成的谦逊牌上实测；不宣称已确认所有第三方环境的选牌根因。
3. 龟缩防御文案：原有 BlockVar=12、升级15实现正确，描述漏“点”，改为动态数值后加“点”；基础显示获得12点格挡，保留升级、敏捷等预览修正与原效果，不硬编码数值。
4. 变身关键字悬停：按用户提供两句重写，保留关键字金色突出及换行。不调整变身、耐久数值或消耗规则。
5. 附魔动画图标：现有兼容层给原生 CompressedTexture2D 的 Icon getter 返回占位纹理，仅给卡牌标签/悬停补上 Texture2D；原生 NCardEnchantVfx._Ready 单独读取 Icon 至 `%EnchantmentInViewport/Icon`，因此遗漏。Ready 后对本模组 loose icon 覆盖这个 Texture2D 插槽，shader reveal 通过同一 viewport 使用真实图标，保留原生时序、粒子、音效、数值和飞回牌堆动画；原版附魔和无自定义图标的 layered 容器仍走原生路径。0.107.1/0.111.0 的节点入口相同。

只读检查相同生成模式：FourthRouteRelics 中节制奖励 RunState.CreateCard 的目标是永久 Deck，因此不属于战斗登记错误，保持不改。其余遗物战斗生成走 CombatState。日志中的 ComicChess/TheQueen 包装栈不能单凭存在就归咎第三方；本mod生成域错误由原生注册规则确认。

## 追踪与实测

- RELIC-EVENT-003 榨乳器：新战斗两张乳汁进入抽牌堆并能抽入手牌、打出、消耗；日志含 Milker active combat scope。
- 第四路线谦逊遗物：新战斗生成谦逊/谦逊+，能进入手牌选择和执行改写；日志含 Humility active combat scope 与 Native hand selection entered/returned。
- CARD-H-300～399 龟缩防御：基础12点、升级15点及实战预览修正正确。
- SYS-TRF-001 变身悬停：显示用户指定两句，不影响实际耐久结算。
- SYS-ENC/ENCH 附魔动画：充能、迅捷等已新增图标在附魔揭示动画与静态卡面/悬停一致。

追加批次前置快照 `fcffd66f117009a1a3ac29d0cc63d6790a4d3465`，接续六项完成提交 `11485c5d222857affcf080bb3d7df65566e4e011`。最近日志备份只存聊天 work/player-round2-followup-20261002/logs，不修改游戏日志。Debug构建成功，0警告0错误。两处本地化改动需随本轮资源 PCK 交付；不修改安装或上传器中的 JSON。

PLAYER-ROUND2-FOLLOWUP-20261002 验证命令：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`。最终构建成功，0警告0错误；未运行静态测试。本地PCK构建进程退出0，PCK_COMPLETE=648，包含恶魔法杖、龟缩防御与变身说明最新本地化；DLL/PDB/PCK只保存在聊天outputs/player-round2-debug-20261002。资源构建日志有环境证书/Sentry初始化告警，不影响PCK完成；未进行游戏内验收或部署、未更新上传器JSON。
