

## 2026-10-05 native map-node hover for next Blindfold encounter（READY）

前置`a8586a96312e5c23294a5abef36a49ab5fbe51c7`，设计`7168d1ce2969b70272cc94146895543247546843`，基线`9c5a819ca6dec3807f835a8efedde65c51115a4e`。

RELIC-EVENT-001：用户最新要求原版悬停框且仅第一行。完整核对眼罩章节/前批追踪，原版NMapPoint.OnFocus只为已走节点生成历史tip，本补丁只处理未走战斗节点，不覆盖历史。使用NHoverTipSet.CreateAndShow(owner,tip,HoverTip.GetHoverTipAlignment(owner))，原版场景/字体/边框/尺寸/鼠标与TreeExiting清理；延后一帧原生对齐，OnUnfocus原方法删除，补丁只清自身meta。删除全部PanelContainer/StyleBox/RichTextLabel代码及AllPossibleMonsters查询，title沿用分类名称，description仅“下一场：{Encounter}”。本地天音/持眼罩/未移除/非已走/三类战斗门禁不变；队列/双boss读取不变。更新description三键及relic自身说明一键，其他用户本地化/PCK资源保留，已有DEBUG命令只同步文案、不运行。保存DesignDoc相对HEAD与接受版本逐行/词级diff及快照，真实index保持，独立范围提交；其他文案漂移保留，前批确认规则继续有效。
验收BNH-01：地图普通/精英/首领未走节点显示原版样式，仅遭遇行，移开清理；02：遗物无列表，已走节点原历史tooltip保留，无眼罩/其他角色/远端不显示；03：重复悬停不动RNG/visited，双首领下一场正确。仅build，不静态测试，游戏内NOT_RUN，未部署，完成IMPLEMENTED；用户授权仅本地部署，游戏仍运行需待退出后替换，不结束进程、不改JSON/上传器/沙箱。


COMPAT-GAME-001文档诊断同步：设计`3ba7a5a25133c086def7a2605722353739bcd156`。玩家godot(7).log第132～137行明确旧游戏0.107.1不存在CardLocation，原版ModManager.GetTypes在本mod初始化前失败；第129行基础库正常完成，第309行基础库镜像再次记录同一缺失类型。源码CardLocation用于SemenCurse/CurseOfferPlayPower钩子和HumilityRewritePatches的新结果位置补丁，后者Safe.Run闭包会生成包含该类型字段的类；日志只证明缺失类型，不声称唯一加载失败类已通过运行时定位。用户明确仅支持0.111.0，故不做旧版兼容，不把跳过失败类型当正常加载。告知玩家更新至0.111.0。开发构建引用本地0.111.0 sts2.dll；不改安装JSON，不修改库，不新建运行时版本判断（加载阶段早于Initializer）。该项为诊断/支持范围文档IMPLEMENTED，旧版移植NOT_APPLICABLE。
