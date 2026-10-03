

## 2026-10-03 原版转阶段晕眩优先级（SYS-CTL-001 / MON-ERO-CATALOG-001）

前置快照 `6ec90dcdc3ac64e3788c6a48d3ed516e63033fa3`。用户反馈仪式鹿（原版CeremonialBeast/仪式兽）挣脱后的晕眩导致一阶段滞留。本轮只修复原生重要状态与本模组临时行动交接，不修改怪物数值、通关流程或分配表。设计独立提交后同步本技术任务；状态IMPLEMENTED，实机NOT_RUN。

原版源码证据：PlowPower解除冲撞会调用SetStunned（设置二阶段旗标），随后CreatureCmd.Stun携带StunnedMove动作与BEAST_CRY_MOVE后继；本模组原有优先级在IsPerformingMove时拒绝所有非固定名字的晕眩请求，因此该原版STUNNED可能被CanTransitionAway挡掉。ForceStun又复用了原版STUNNED状态名；普通模组Stun走原版StateLog回退，还可能丢弃正在暂存的下一原生行动。最近本机日志没有仪式兽复现记录，不能宣称已游戏复现；日志中的其他模组旧API错误不属于本次故障证据。

技术任务：①原版STUNNED携带后继ID时作为必须保留的原生中断，执行中的色情行动也允许切入，保留整个MoveState动作/后继，不构造通用替身；②清除被原生中断取代的ForceStun标志，原版已forceTransition的复活/爆炸也清除；③模组晕眩独立命名MAIDENSUCCUBUS_STUNNED，保持一次晕眩和原生续接，不与原版STUNNED争用；④本模组卡牌在色情行动上请求普通击晕时直接走自有续接路径，不从StateLog回到旧阶段；⑤自有晕眩继续受优先级保护，避免次回合被色情判定替换；旧STUNNED仍兼容识别。SetMoveImmediate补丁使用Safe.Run。

同类源码审阅：PlowPower仪式兽BEAST_CRY_MOVE、ShriekPower骇鳗TerrorState、AsleepPower沉睡唤醒SLASH_MOVE、SlumberPower唤醒ROLL_OUT_MOVE、BurrowedPower脱壳BITE_MOVE、FlutterPower落地后继及RavenousPower噬尸蛞蝓带回调的晕眩，均走同一原生STUNNED入口，保留回调/后继；残杀千足虫DEAD/REATTACH、实验体DEAD/RESPAWN和瀑布巨兽ABOUT_TO_BLOW/EXPLODE延续既有高优先级。未扩展其他怪物的普通攻击免疫，不全局禁止原版眩晕。

人工验收PHASE-STUN-01～04：仪式兽色情/恢复行动时打掉冲撞机制，确认原生晕眩后进入兽吼/二阶段；已在原版转阶段晕眩时挣脱不覆盖；普通怪物挣脱/侵犯仍只晕一回合且续接正常，不自循环；同类唤醒/落地/噬尸/复活/爆炸流程与中途存读档。既有手动控制套件仅同步自有晕眩的新ID，不新增或运行静态测试。

按用户最新指令暂不部署，不修改正式游戏、沙箱、ModUploader或安装manifest JSON。


构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误，10.70秒。未运行静态测试与游戏内验收。本轮继承上一批三路线开局及其他待部署修复；不部署，产物保存到聊天outputs/phase-priority-20261003。无资源改动，完整PCK沿用start-routes-20261003。
