

## 2026-10-04 光之审判波及伤害回修（KW-CONDEMNATION-001 / CARD-H-600～699 / FINAL-JUDGMENT-01～05）

前置快照`54475d7eab69c889937abaaa870ae6a22ffa540f`。相对HEAD与上一接受版本逐行/词级差异留存；相对ba5031d2的漂移只有START开局段回到旧文字和按摩初始/一般结果叙述扩写，断罪/审判相关规则无变动。保留用户全文编辑于工作树与恢复快照，不混入本次范围提交；事件叙述扩写不影响数值/流程，仅待单独文本同步；START段退回旧“下方显示条件”与用户上轮明确精简指令冲突，本批保持已授权精简实现，不把文档回退擅自固化成恢复旧UI。完整复核圣洁路线断罪定义、输入与提前结算、万劫不复、魔力解放/增幅规则，DS27-02D Plan和追踪/旧七层场景。已实现条目既有“其他敌人受到断罪审判等量伤害”回修，不修改数值或设计，不实现体系OPEN待定项。

证据：当前godot.log仅记录FinalJudgment注册，没有足够逐段数值/出牌记录或该牌异常，不能宣称日志复现。代码确定的差异：主目标CondemnationCmd.Judge使用ValueProp.Move|Unpowered，cardSource/cardPlay为null；FinalJudgment波及却传this/cardPlay。MagicAmplificationPower.ModifyDamageMultiplicative只查dealer/ShouldAmplify(cardSource)，不排除Unpowered，因此通过魔力增幅支付解放时波及可能额外1.5倍；有附魔+战术核心时2倍，主目标无此加成。原始预估层数=旧层数+1还忽略Artifact/施加修正，可能审判未发生也溅射7，或结算层数与预估不一致；旧探针使用魔装耐久支付且无施加阻挡，未覆盖此差异。

任务：CondemnationCmd用既有WeakInstanceValueScope按目标实例建立短期审判结果捕获（名义伤害/真实applier），自动达到7层和主动提前审判共同上报；FinalJudgment在施加及强制审判期间捕获，只有尚无自动审判时强制Judge，然后释放scope再进行魔力解放选择，避免等待期间混入其他审判。审判与波及共享DealJudgmentDamage，源卡/出牌均null且同一props/dealer；不额外吃魔力增幅或卡牌附魔。不把主目标实际扣血或剩余生命当波及基数：各目标照常分别计算格挡/减伤，同为N层×7的审判基础伤害。主目标致死后保留已捕获数值；断罪未生效且无已有层数时不波及；已有层数被Artifact挡掉新增1层时仍强制审判真实已有层数。保持万劫不复保留层数，自动/强制只一次，升级减费与支付行为不变。捕获仅当次调用、按实例隔离、不入存档、using异常清理。

验收：FINAL-JUDGMENT-01普通/升级、零层/6→7/万劫不复主目标与其他无防护敌人7/49一致；02魔力增幅及战术核心不再次放大波及；03Artifact零已有层不波及、已有3层只21；04主目标致死/格挡/自身减伤不将扣血或过杀值当基础，其他敌人独立防护；05多人各实例和重放独立，异常后scope清理。状态IMPLEMENTED、游戏内NOT_RUN；按用户要求不运行静态测试，仅build，暂不部署。保持PCK/JSON原样，无安装目录、沙箱、上传器或用户存档写入。本批不处理日志中其他Mod注册失败和前批未部署的欲望/界面问题。

最终构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误；静态测试NOT_RUN，游戏内NOT_RUN。outputs/final-judgment-splash-20261004保留新DLL/PDB与原样继承start-unlock-save-20261003的完整PCK/反馈配置；暂不部署。

构建复核发现初次0警告0错误但谦逊提取目录785/1：卡牌OnPlay新增Logger.Info被未知辅助函数保护拒绝。已把日志移入独立审判捕获Dispose，保留卡牌纯结算接口和既有Condemnation效果边界，重新构建检查目录恢复，无静态测试。初次完成提交cbdebf59仅为中间构建，最终以回修后的范围提交为准。

最终构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误；静态测试NOT_RUN，游戏内NOT_RUN。outputs/final-judgment-splash-20261004保留新DLL/PDB与原样继承start-unlock-save-20261003的完整PCK/反馈配置；暂不部署。
