

## 2026-10-04 右侧开局预览与锁定开局门禁（START-003 / START-PREVIEW-01～06）

前置快照`d7dd5ad99db3d37d29def0f3c43d3cdd8204515d`，设计提交`6d7fb93ca34c8083034b02e1f86d6c8de05f3ee4`。逐行与词级diff均留存；完整复核DesignDoc三开局、堕落±3封印、同角色身份/牌组/初始遗物、解锁与多人共享堕落规则，以及Plan三开局与START-SAVE追踪。此次用户直接确认右侧切换、明确条件与效果、允许锁定预览而不可开局，覆盖前一轮精简UI要求。原START段回退旧“不可选择”由本次明确规则更新；按摩事件叙述扩写保持工作树与恢复快照，未混入本次范围提交，不改变事件机制。

本地参考：F:/steam/steamapps/workshop/content/646570/3242483596/VUPShionMod.jar，SkinManager/SkinCharButton/SkinInfoLabel中浏览选中、锁定覆盖、confirmButton和信息显示独立；参考用户截图的右侧面板与锁定浏览交互，使用STS2原生NButton输入与现有选角字体，不拷贝STS1的角色美术或运行时。代码证据：当前StartRouteSelector位于左侧遗物VBox，Refresh将锁定路线重置为Normal，Next只遍历已解锁路线；无法预览锁定状态。

计划：屏幕拥有的右侧独立面板、三路线常驻按钮、选择高亮/锁定状态、当前解锁条件和初始堕落/封印效果；三路线同牌组/遗物说明，不新增奖励或美术。锁定预览保留在当前大厅本地玩家Route字段，RouteUnlockedAtSelection=false；禁用原生ConfirmButton，并在OnEmbarkPressed同步Prefix再次按当前档案校验，拒绝锁定状态（含快捷键/手柄入口）。不悄悄退回中立开局；解除准备才能改选，其他角色/关闭界面恢复本控件造成的按钮禁用；原有StartProfilePatch仍保留新跑局兜底，载入已有跑局不改变资源。保留上一轮显式store.Save解锁修复，不写玩家解锁数据。

验收：01三路线包括锁定可点击预览、条件准确；02中立/已解锁能开局，锁定Confirm禁用且入口拦截；03右侧面板、窗口比例缩放、联机远端列表与底部原生控件不重叠；04其他角色/退出/重进/取消准备无残留；05原生初始遗物切换独立且不改变开局路线；06档案解锁跨重启、多人本地桶与新跑局初始化沿用原规则。READY→实现后IMPLEMENTED，游戏内NOT_RUN。用户要求不再静态测试，仅build；当前暂不部署，不改ModUploader/JSON或用户存档。谦逊72→37等待该玩家日志，本批不修改其算法。

实现完成：右侧三按钮、独立信息面板、锁定预览与原生开局入口同步拦截；尺寸变化按选角屏幕缩放。仅恢复本面板禁用过的Confirm，其他角色和原生准备/退出流程保持独立。Debug构建0警告0错误，提取目录786/0；无静态测试/游戏内验收/部署。前批修复累积在本次DLL；PCK与反馈配置原样继承，不改上传器JSON。IMPLEMENTED，待用户实测。

收尾代码复核补充：原生SelectCharacter先禁用锁定角色Confirm，再通知本控件。切换至原生未解锁角色时，先放弃本控件的恢复标记，避免把原生锁定按钮重新启用；开局门禁仅作用于当前可用天音选角，不干涉其他角色或已关闭面板。焦点外观使用原生公开Focused/Unfocused信号；初次编译的受保护属性访问已修正。

实现完成：右侧三按钮、独立信息面板、锁定预览与原生开局入口同步拦截；尺寸变化按选角屏幕缩放。仅恢复本面板禁用过的Confirm，其他角色和原生准备/退出流程保持独立。Debug构建0警告0错误，提取目录786/0；无静态测试/游戏内验收/部署。前批修复累积在本次DLL；PCK与反馈配置原样继承，不改上传器JSON。IMPLEMENTED，待用户实测。


## 2026-10-04 用户授权仅部署本地游戏（LOCAL-DEPLOY-20261004-165357）

用户明确要求“仅部署到本地游戏目录”，覆盖此前暂不部署指令，仅此安装目录授权。最新源码完成提交`c857db668f15b0577dea7711da46ade8fec1dc46`，部署前文档快照`00eeb5e4f7fffbb094705c663148be966432d57d`；复用本轮已通过的Debug构建，0警告0错误，命令`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`。本次部署右侧三路线开局预览及锁定开局门禁，包含前批未部署的欲望条、湿了、解锁持久化、瘴雷计数、反馈分组及光之审判修复。PCK沿用上一接受版本。只在首次安装时增加combat_feedback.json文案模板，后续保留用户填写内容。

仅替换`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`下MaidenSuccubus.dll/.pdb/.pck及首次文案模板，替换前备份`C:\Users\wilson\Documents\Codex\2026-09-30\amane-recovered-context\backups\local-start-route-preview-deployment-20261004-165357`，安装后逐项SHA-256一致，原有JSON哈希不变。未改沙箱、ModUploader、上传包或其他游戏/模组目录。未运行静态测试与游戏内验收，用户实测后再标VERIFIED。部署明细：聊天outputs/start-route-preview-20261004/local-deployment-20261004-165357.json。
