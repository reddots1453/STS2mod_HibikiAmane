

## 2026-10-05 祝圣参考原版熵叠加与多选变化（READY）

前置`55d228f8cba76a8bd841c8083faa83e6d65a9d9b`；接受基线`43433e17027d27227fa797b3645048ba244356be`。DesignDoc只读核对、原样快照，按用户要求不修改。

SYS-SCR-001 / CARD-H-祝圣 / CONSECRATION-STACK-001：用户反馈多张祝圣不叠加，明确要求参考STS2原版无色Entropy。只读复核DesignDoc圣言/祝圣说明、现有生成命令和原版EntropyPower/Entropy/CardSelectCmd.FromHand；当前缺陷为Single层数策略且回合开始选择数写死1。

任务：ConsecrationPower改Counter，复用原版FromHand与CardSelectorPrefs(TransformSelectionPrompt, Amount)，一次选择多张不同手牌，再依次通过现有ScriptureCmd变为随机圣言。保持IsTransformable过滤及异步返回后逐张重验拥有者/仍在手牌/可变化/Power仍生效；不足层数的合法手牌按原版全部选中，空手牌自动返回，不递归选择刚生成的圣言。每张祝圣仍施加1层，升级固有、费用、稀有度及六种圣言的生成RNG/原版变化升级与附魔行为不改。

状态栏仍为一个图标，显示累计层数。description/smartDescription用{Amount}显示实际选牌数，源本地化仅两键调整，打包以上一接受PCK定向替换，保留用户其他文本。旧存档可继续使用已有层数，过去Single丢失的重复施加次数无法凭空恢复。

验收待玩家实测：两张/三张祝圣层数2/3、回合开始一次选择对应数量、选中实例各变化一次且新生成圣言不再次进入本次选项；手牌不足、空手牌、异步移走/Power移除安全跳过，升级仍固有，其他圣言效果不变。仅Debug build，不运行静态测试；用户已明确授权完成后仅部署本地目录，全部安装JSON保留，ModUploader/沙箱不更新。
