

## 2026-10-05 祝圣参考原版熵叠加与多选变化（READY）

前置`55d228f8cba76a8bd841c8083faa83e6d65a9d9b`；接受基线`43433e17027d27227fa797b3645048ba244356be`。DesignDoc只读核对、原样快照，按用户要求不修改。

SYS-SCR-001 / CARD-H-祝圣 / CONSECRATION-STACK-001：用户反馈多张祝圣不叠加，明确要求参考STS2原版无色Entropy。只读复核DesignDoc圣言/祝圣说明、现有生成命令和原版EntropyPower/Entropy/CardSelectCmd.FromHand；当前缺陷为Single层数策略且回合开始选择数写死1。

任务：ConsecrationPower改Counter，复用原版FromHand与CardSelectorPrefs(TransformSelectionPrompt, Amount)，一次选择多张不同手牌，再依次通过现有ScriptureCmd变为随机圣言。保持IsTransformable过滤及异步返回后逐张重验拥有者/仍在手牌/可变化/Power仍生效；不足层数的合法手牌按原版全部选中，空手牌自动返回，不递归选择刚生成的圣言。每张祝圣仍施加1层，升级固有、费用、稀有度及六种圣言的生成RNG/原版变化升级与附魔行为不改。

状态栏仍为一个图标，显示累计层数。description/smartDescription用{Amount}显示实际选牌数，源本地化仅两键调整，打包以上一接受PCK定向替换，保留用户其他文本。旧存档可继续使用已有层数，过去Single丢失的重复施加次数无法凭空恢复。

验收待玩家实测：两张/三张祝圣层数2/3、回合开始一次选择对应数量、选中实例各变化一次且新生成圣言不再次进入本次选项；手牌不足、空手牌、异步移走/Power移除安全跳过，升级仍固有，其他圣言效果不变。仅Debug build，不运行静态测试；用户已明确授权完成后仅部署本地目录，全部安装JSON保留，ModUploader/沙箱不更新。


IMPLEMENTED（2026-10-05）：前置`55d228f8cba76a8bd841c8083faa83e6d65a9d9b`，计划`55f540b0c34117da4557246135109ac03ff17687`。祝圣状态Single改Counter，每张仍+1层；按原版熵一次选Amount张当前合法手牌，固定所选实例逐张变化，保留异步目标重验及随机圣言规则。状态栏显示累计层数，双悬停文案显示{Amount}张。费用/升级固有/其他Power及卡牌不变，DesignDoc未改写。
构建`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过0警告0错误；按用户要求静态测试NOT_RUN、游戏内NOT_RUN；尚未部署。DesignDoc未修改，安装/上传器JSON未写入。


## 2026-10-05 用户授权仅本地部署（LOCAL-CONSECRATION-STACK-20261005-163304）

源码`7edbcd3bde2d8ba65f392a6f2da1fb1b7f2e6cfa`，部署前快照`1bc149582f5bdc94de7e0e8e4545449106ac7cf1`。将祝圣叠加修复的DLL、PDB、PCK部署到`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`；祝圣采用原版Entropy的Counter与Amount一次多选手牌、逐张变化路径，继续使用现有随机圣言命令及异步目标重验。状态栏仍一个图标，显示累计层数；双悬停文案显示当前选择张数。沿用前批欲望/眼罩累计修订，其他卡牌/费用/升级固有不变。

复用Debug构建`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`，0警告0错误，不重复构建或静态测试，游戏内NOT_RUN。旧产物及所有已存在JSON已备份到`C:\Users\wilson\Documents\Codex\2026-09-30\amane-recovered-context\backups\local-consecration-stack-20261005-163304`，逐文件SHA256与修复包一致，全部JSON保持。DesignDoc未改写，不更新ModUploader或沙箱，不执行上传、不结束游戏进程。部署明细：outputs/consecration-stack-20261005/local-deployment-20261005-163304.json。
