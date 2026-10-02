# 慷慨路线领取遗物后卡住（2026-10-03）

状态IMPLEMENTED，游戏内待用户实测；不运行静态测试。对应ACT4-001慷慨宝箱/战利品“遗物或供奉”二选一规则，不改变供奉次数、删牌数、同步消息或奖励归属。

日志已只读备份至聊天work/compat-sandbox/generosity-choice-20261003/godot-before.log。第3675行附近已经获得RELIC.CLOAK_CLASP；第3683行NRewardButton.GetReward发出RewardClaimed后报“Expected 0 argument(s), received 1”；第3710行及之后多次NRewardsScreen.OnProceedButtonPressed报“not currently viewing any reward set”。问题是普通宝箱遗物的慷慨二选一UI收尾，不是新增背景、开局沉睡遗物或慷慨阶段拾起删牌。

原生0.107.1和0.111.0的NLinkedRewardSet.Reload都将零参数GetReward绑定到带NRewardButton参数的领取信号；GetReward自身又直接调用RewardCollectedFrom并发出缺少NLinkedRewardSet参数的父信号。后端已完成领取，而父行未移除，导致继续按钮对已弹出的奖励集反复执行跳过。

修复只替换NLinkedRewardSet.Reload中的Callable.From(Action)构造边界：GenerosityOfferingGroup使用Callable.From<NRewardButton>，领取成功后保留幂等未选分支处理，并携带父节点发出一次父领取信号；由原生NRewardsScreen监听处理行移除、非终结奖励关闭和焦点。其他LinkedRewardSet继续使用原回调。不重新领取、不重复调用RewardCollectedFrom、不吞掉SkipLocalRewardsSet异常，也不绕过奖励同步。

同日志第3661行起多次VariationRelicReloadPatch警告“Model was accessed before it was set”。这是上一批视觉刷新对尚未赋值NRelic的读取问题：改为Harmony传入可空_model字段，跟踪与重新读取均先检测；不使用会抛异常的Model getter。此项不是卡住根因。

手测：慷慨路线宝箱领取遗物能关闭二选一并继续；宝箱供奉能关闭并推进试炼；精英战利品领取/供奉后其他奖励保留且地图可继续；阶段奖励拾起删牌仍正常；非慷慨路线和多人分配不受影响。成功日志含[Generosity] Linked choice UI completed。

附带定位但不在本次修复范围：日志第486行BurningPerHitPatch安装失败，以及既有BlindfoldIntentHoverPatch安装失败。没有把这些问题声明为已修复；需另查实际安装异常细节。

前置快照：8bbb5f5777f3074f6ec946bd2b96db6bb2fbf371。本轮只build、保留当前683条资源PCK；正式游戏及兼容沙箱的部署记录另列，上传器JSON不改动。


构建命令：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 成功，0警告0错误。不运行静态测试，游戏内验收NOT_RUN。本轮仅代码和文档修改，复用上一批哈希一致的完整683条资源PCK，保留全部先古牌/变奏图/试炼背景。独立DLL/PDB/PCK见聊天outputs/generosity-choice-debug-20261003，部署另记。

首次构建实际为1警告0错误（日志读取Reward的可空引用）；已修正该日志空值处理，以下最终0警告结论以随后重新构建为准。

可空日志修正后的重新构建实际通过：0警告0错误，未运行静态测试；最终交付采用这次重建DLL/PDB。


## 2026-10-03 慷慨奖励卡死修复部署

接续用户已授权的本轮部署，使用日志确认的慷慨卡死修复Debug构建（0警告0错误），PCK资源未改动、含683条资源；游戏进程关闭后备份安装产物，并部署至正式游戏mods/MaidenSuccubus、兼容沙箱mod-store/local-MaidenSuccubus及沙箱game/mods/local-MaidenSuccubus。三处部署文件逐项SHA-256复核一致，现有JSON哈希不变，未动ModUploader。备份：`C:\Users\wilson\Documents\Codex\2026-09-30\amane-recovered-context\backups\generosity-choice-deployment-20261003-021842`。本轮文档前置快照`4012e8b07e85eadea3be5a0c27363417bffe558d`；部署明细见聊天outputs/generosity-choice-debug-20261003/deployment-20261003-021842.json。不运行静态测试；画面和玩法由用户实测。
