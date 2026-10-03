

## 2026-10-03 侵犯成功解除来源拘束（SYS-INV-001 / SYS-CTL-001）

前置快照 `0732a4c42f4b6b64beafb8d63a1821c1bb54df26`。已逐行/词级检查DesignDoc相对HEAD及上一接受提交，并全文复核3.1～3.5对应规则、Plan与追踪矩阵。最新用户明确规则已在DesignDoc 3.2：成功塞入诅咒后，该怪物本场战斗不再使用拘束或侵犯，且由该怪物发起的拘束解除，无Boss例外。因此DesignDoc不另改；修复实现遗漏并更新本轮技术追踪。

技术任务INVASION-RELEASE-01～05（IMPLEMENTED，游戏内NOT_RUN）：
- InvasionCmd.Resolve仅在AddCurse实际成功后立即设置已有SavedProperty载体ControlDisabled，早于CG与额外效果；自然候选与强制改意图原本均过滤该标志，不另设永久或怪物类型全局标志。
- 成功后通过原生ControlCmd.Release/PowerCmd.Remove解除本场战斗全部玩家身上由该怪物实例Applier发起的全部拘束类型，使用Direct原因；其他来源怪物即使同类型/同模型ID也不解除。恢复原牌及状态图标走既有ControlPower.AfterRemoved刷新，不伪造挣脱完成。
- InvasionCmd直接执行与CreateControl预选后执行均复核ControlDisabled，防止已缓存/外部入口再次结算；欲望攻击仍按已有独立规则选择。
- 贞洁防御或其他成功阻止入牌的路径返回false，不禁用敌人、不解除拘束。内置储存遗物此前定义为成功侵犯，延续该成功语义。
- 原有成功侵犯的收尾及原生转阶段优先级保留；解除拘束不额外插入恢复/晕眩，不重复结算。标志随怪物战斗状态保存，新的战斗/新怪物实例不继承。打印成功、来源ID和解除数量日志供复现。

人工验收：一敌多类拘束成功后全部解除且后续无拘束/侵犯；两只同模型敌人只解除实施成功侵犯者的拘束；B借A拘束成功侵犯时保留A的拘束、只禁用B；贞洁/防塞牌失败无禁用与解除；CG开关/跳过、Boss/转阶段、战斗存读档、下一场同怪物及多人来源隔离。按用户要求仅构建，不运行静态测试，继续暂不部署，不改正式游戏、沙箱、ModUploader或安装JSON。无需资源/PCK修改。
