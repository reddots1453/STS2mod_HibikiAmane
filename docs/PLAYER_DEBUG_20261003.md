# 玩家反馈修复 2026-10-03

前置快照 `9e61c6b61eaa5501a69b9709e93941c900f490c6`。DesignDoc逐行与词级差异已复核：除本轮暴露狂2/1费、淫乐园保留外均为已同步的历史变更。本轮直接用户确认：路线概率改为每张独立；变更已同步DesignDoc/Plan。其他任务工作树与索引不修改。

| 项目 | 证据、修复 | 状态 |
|---|---|---|
| 燃烧不生效 | 最新日志BurningPerHitPatch在AttackCommand.Execute.MoveNext安装失败；旧日志仅记录外层异常。改补丁为原生AttackCommand.BeforeDamage逐段回调，同一段多目标只结算一次，多段逐次燃烧；保留已有回调且不重入其他Mod的伤害前缀，燃烧杀死攻击者时批量伤害入口立即返回空结果，保持玩家旧规则；异常现在记录完整堆栈。独立无引擎诊断无法执行Godot日志/游戏Harmony（系统仅.NET10，Harmony不支持）；不宣告根因已运行复现。 | IMPLEMENTED |
| 淫乐园/暴露狂升级 | 两张OnUpgrade为空；淫乐园+添加保留，暴露狂+能量费用降低1。 | IMPLEMENTED |
| 挣脱文本 | 缩短中文临时投影描述为挣脱X，原牌和拘束详细说明仍在悬停。 | IMPLEMENTED |
| 帝皇蟹夹击 | 原版SurroundedPower在BeforeCardPlayed/BeforePotionUsed按目标背击方向变更Facing，乘伤1.5不受本Mod覆盖；原生已翻转Body，但旧动画Tween可带回旧方向。等待原生FaceDirection完成同步立绘与表情整体朝向，后续动画按原生Facing取符号。 | IMPLEMENTED；伤害/视觉待实测 |
| 心跳 | heartbeat.ogg已在资源包，但PerformanceLoopCue没有入口；新增非瑟瑟心跳循环，战斗本地天音欲望>=8播放、<8渐停，离战斗节点清理。不恢复先前移除的欲望8语音/施法/自慰循环。 | IMPLEMENTED |
| 原版生成/变化卡池 | 原生卡牌、药水、遗物、能力的角色池调用与默认变化，局部替换GetUnlockedCards调用为三路线合并；保留原生类型/费用/可生成/多人/原牌排除等后续过滤。奖励/事件/商店池API未全局改动。 | IMPLEMENTED |
| 路线概率偏低 | 原来整组掷骰，最多1张替换；现在每张独立，+5期望65%堕落，非保证张数。保留稀有度、升级、过滤、去重，候选耗尽不跨稀有度。满屋芝士/脑蛭及所有同CardFactory奖励接口的事件纳入；无色/他色专属奖励保留。 | IMPLEMENTED |
| 读档欲望条0 | 当前存档仅RitsuLib secondary_resources=5，旧桥接值未保存。读取已注册的原生快照用于旧存档迁移，RunLoaded后延迟刷新UI；有桥接值优先，等值刷新不重播满值音效。 | IMPLEMENTED |
| 乳汁污染6 | 原生VitalSparkPower.AfterCardEnteredCombat先附污染3，BeforeCombatStart扫描技能再附3；非重复创建/注册。榨乳器、谦逊、耐心开场生成统一BeforeCombatStartLate，在原生扫描后、第一轮抽牌前生成，原生入堆/生成钩子照常一次。其他本Mod生成牌在打牌/回合钩子，不存在此扫描顺序。 | IMPLEMENTED |

验收：燃烧单/三段攻击及燃烧致死取消该段；两卡升级；挣脱；帝皇蟹左右目标/药水/伤害；欲望7→8→7及读档8心跳；变化/攻击技能药水三路线；+5满屋芝士8/脑蛭5多次候选分布；地图读档欲望；感染棱柱乳汁/谦逊/耐心技能污染应为一次层数。

不运行静态测试；游戏内验证NOT_RUN。构建和部署结果待补充。部署不改安装manifest JSON/ModUploader。

原版入口审阅补充：攻击/技能/能力药水、OrobicAcid及Discovery/Distraction/InfernalBlade/WhiteNoise/Metamorphosis/Stoke/Jackpot等角色池生成共享三路线；Splash的LINQ闭包也纳入局部调用替换。合并方法不提前删除基础/先古类别，由每个原生入口自己的生成/变化过滤决定，避免Fasten这类寻找防御的原生逻辑被空池破坏。事件检索发现五个CreateForReward调用：满屋芝士、脑蛭、无尽传送带、感染自动机两处；其角色候选可进入路线算法，原事件明确指定的他色/无色过滤不扩写。


最终验证：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 成功，0警告0错误（最终耗时9.38秒）。初次构建因两个命名空间遗漏有2错误，已补齐；最终DLL采用原生逐段BeforeDamage回调及含LINQ闭包的生成池版本。未运行静态测试；游戏内NOT_RUN。

独立测试包：聊天`outputs/player-debug-20261003`，DLL/PDB及完整683资源PCK。PCK以已部署上一批资源为基准，仅替换本轮挣脱中文文本；其他资源内容逐项MD5保持原样。未操作ModUploader或安装目录JSON。检测正式游戏SlayTheSpire2进程PID8864仍在运行，部署暂未执行，修复已提交供关闭游戏后部署。
