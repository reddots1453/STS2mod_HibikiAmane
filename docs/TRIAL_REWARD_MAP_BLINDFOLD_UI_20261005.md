

## 2026-10-05 试炼奖励地图阻塞与眼罩地图提示（ACT4-001-REWARD-MAP-001 / RELIC-EVENT-001，READY）

前置快照`c0b7c8b399b1812980b476a534a5c747004277ea`，设计`8b9c573a68e5e48b46a11b5f61e27756235683b1`，接受基线`1947fe74c2f8516ff036a033e5a54dd95dc58752`。核对正常分支/HEAD/最近提交/目录状态，真实index只读，私有index独立提交；保存DesignDoc相对HEAD及接受版本的逐行/词级diff。复核ACT4三阶段奖励/可跳过收据、奖励watcher/胜利/map入口、原版NRewardsScreen/NOverlayStack/NMapScreen/RewardsSetSynchronizer及眼罩辉眼源码。其他未提交用户编辑/其他mod删除不入本批。

证据：本地日志outputs/trial-reward-diagnosis-20261005/godot.log第3103～3104行显示普通奖励卡选取后地图reward=True但没有领取，下一场胜利第3188～3190行才领取Benevolence FirstReward。原版终结奖励Proceed只Open地图而不Remove奖励页，StackIsCovered会把新Push叠层隐藏。Ready把ScreenCount>0统视真实操作，误挡残留终结页；仅放宽Ready仍会把试炼页藏在地图下。本次只对mapOpen且栈中唯一、同run/room的原版终结NRewardsScreen放行，其他叠层阻塞保持。Show临时Close(false)，不Remove/Skip原战利品；await原生Offer后等待一帧UI回调退栈，再在同scene/run/room、存活、无transition/真实叠层情况下恢复地图；恢复travel捕获值，跳过的收据不变化，既有同房间一次提示与领取门禁保留。新增实际展示/返回日志，事件选项异步稳定等待不变。

眼罩去除NRelicInventoryHolder两个hover补丁与额外三类预览API；地图普通/精英/首领未走节点保留HasEffect本地天音持有者门禁、只读下一场队列和可能敌人。辉眼MapPointPredictionPatch同样OnFocus/Unfocus、760宽深色圆角面板、18/21字号、▌标题/分隔线与分类颜色，面板左侧优先越界回退，鼠标穿透，不加新DLL依赖；仅敌人字段，不导入辉眼奖励/事件/未来幕预测。遗物悬停仅原生正式说明，相关7个文本键定向打包。已有DEBUG命令只同步移除的预览API与文案，不运行、不新增测试。

验收TRM-01：普通奖励结束/跳过后第一次开地图能显示待领试炼；TRM-02：原战利品剩余可继续领，不误发/重复，试炼跳过后地图可走；TRM-03：选牌/教程/事件异步/其他叠层等待，场景变化不复开旧图；BLM-01：眼罩遗物无附加预测，地图三类分别显示下一场及可能敌人，辉眼样式；BLM-02：无眼罩/已移除/远端/其他角色不显示，重复悬停与读档不改变队列/RNG。仅build，不运行静态测试，游戏内NOT_RUN；不自动部署，不写安装目录/沙箱/上传器JSON。


漂移同步：相对1947fe74完整逐行/词级差异仅为上批动态上限文案回退，未收到对应玩法撤销；用户原正文保留，设计补充`ca469bc5c14fd8bde50d3cca40b609be86a71cd1`明确上批已确认-5规则继续生效，本轮不据此回滚代码。其余真实DesignDoc内容以现有用户正文为准。本批READY范围仍限ACT4-001奖励地图桥接和RELIC-EVENT-001地图提示。


ACT4-001-REWARD-MAP-001 / RELIC-EVENT-001 IMPLEMENTED（2026-10-05）：前置`c0b7c8b399b1812980b476a534a5c747004277ea`，设计`ca469bc5c14fd8bde50d3cca40b609be86a71cd1`，计划`dc6825f34172fd4935626e476affb751db024f88`。试炼奖励仅允许地图覆盖同房间原版终结战利品页时进入，收起地图显示原生试炼奖励、结束同上下文返回；原战利品保留，其他真实叠层/教程/动作/未结束事件仍等待，跳过/收据/同房间一次提示/唯一发放保持。新增展示与返回日志。眼罩移除遗物额外预览，改为辉眼样式地图战斗节点提示（分类标题、下一场、可能出现的敌人），760宽暗色圆角、字体/颜色/分隔线/节点左侧定位沿用参考；只读下一场且本地持有者生效，无新依赖。累计包含前批原版变化/商人立绘/-5欲望上限变更。Debug构建`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`0警告0错误；静态测试NOT_RUN，游戏内NOT_RUN，未部署，安装和上传器JSON不修改。


## 2026-10-05 用户授权仅本地部署（LOCAL-TRIAL-MAP-BLINDFOLD-20261005-152152）

源码`123fea88c652c8fd60e290748a2c37159256a18e`，部署前快照`958b1e7b71b27c21c2df613458f81d016e545ffc`。用户明确要求部署到本地mod目录，已将最新試炼地图奖励/眼罩节点提示包的DLL、PDB和PCK写入`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`，同时累计此前未部署的原版三路线变化、商店立绘及-5欲望上限15修订。复用Debug构建，命令`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`，0警告0错误；不重复构建或静态测试，游戏内NOT_RUN。旧产物与全部已存在JSON已备份到`C:\Users\wilson\Documents\Codex\2026-09-30\amane-recovered-context\backups\local-trial-map-blindfold-20261005-152152`，逐文件SHA-256与包一致，全部JSON哈希保持。未改ModUploader或沙箱，未执行上传，不结束游戏进程。试炼领奖后返回地图，眼罩仅在地图战斗节点显示预测，原战利品保留；游戏效果待用户实测。部署明细：聊天outputs/trial-reward-map-blindfold-20261005/local-deployment-20261005-152152.json。
