

## 2026-10-05 +5拘束阈值与欲望标记、-5战后减欲望、悬停精简及眼罩提示（READY）

前置`8c481fe71d760f31210caaffeb95f0a2b7eb1152`；基线`e724d581723bab480b59e8e09b3d2a6098852600`。本轮用户明确要求今后未经指示不修改DesignDoc；已只读核对现有修改并保存原样快照，不改写、不追加DesignDoc。按用户修订的2.1三项悬停原文实现；其1.3/末尾旧记录仍写-5上限+5，但本轮用户明确要求-5恢复战斗结束减1，直接指示优先，不自行改文档。

SYS-COR-004 / SYS-DES-002B / SYS-CTL-001：+5基础上限15、拘束格挡绕过和Wet门槛为>=10；其他堕落值门槛>=8。-5基础上限10，胜利原生AfterCombatVictory恢复减1欲望，0值不负数；+5不恢复旧战后+1。心跳/粉色屏幕边缘等8点表现规则不改。统一GetControlBypassThreshold供ControlCmd、Wet和UI读取；欲望变更、开战、回合开始沿用原生合法异步入口，拘束结算前和首次侵犯增堕落后复核Wet，不从UI事件启动异步模型变更。

欲望UI：原128×416素材固定粉线中心y=124，真正量程为y=72.5至330；局部CanvasItem shader只替换原粉线像素并在实际threshold/maximum位置绘制粉色标记，不改PNG、其余立绘/泡泡/计量状态。同步标记悬停区、阈值文案，缓存纳入threshold，+5/-5/其他路线切换即时刷新。通用和满值悬停严格使用DesignDoc新原文及已有富文本色标，不追加通常/15点等解释。

RELIC-EVENT-001：原生悬停标题改下一场遭遇战：/下一场精英战：，正文仅真实遭遇名；不再在Boss地图节点创建眼罩悬停。读取只读RoomSet，不改RNG或遭遇顺序，持有者范围、原版布局保留。

验收待用户实测：+5欲望9可用格挡/10不可；其他路线7可/8不可；-5战后欲望减1且上限10；+5上限15；粉线/悬停随阈值和上限对应；三项说明逐字匹配用户原文；普通/精英标题分行、Boss无眼罩提示。按用户要求只build、不执行静态测试。完成后已获授权本地及ModUploader部署，全部安装JSON保持。


IMPLEMENTED（2026-10-05）：前置`8c481fe71d760f31210caaffeb95f0a2b7eb1152`，计划`8a3410b3918090d3846569f074a1d635ee55f835`。+5拘束格挡绕过/Wet阈值10；其他8。粉线按真实量程及当前threshold/maximum绘制，旧素材固定粉线局部替换，悬停区域与文案同步，路线变化纳入UI缓存。-5恢复胜利减1欲望且取消容量+5；+5容量+5保留，原8点屏幕/心跳表现不改。通用欲望及满值说明采用用户修订DesignDoc原文；不改写DesignDoc。眼罩普通/精英提示采用用户指定标题与独立遭遇名，Boss不创建提示。静态测试NOT_RUN、游戏内NOT_RUN。构建`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`0警告0错误。用户已授权本地游戏及ModUploader部署；本提交尚未部署，后续独立部署记录，全部安装JSON保持。


## 2026-10-05 本地游戏与ModUploader部署（20261005-160300）

源码`79839a5b3d4afa8c905222776e29ddaaac2cd3c7`；部署前快照`3d0db03320bdd04ef136fac8577dc95cfa394334`。按用户本轮明确要求，将累计欲望/拘束/眼罩修订的DLL、PCK部署到本地`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`和上传器`D:\game_backup\steam\steamapps\common\Slay the Spire 2\ModUploader-win-x64\MaidenSuccubus\content\MaidenSuccubus`，本地另更新PDB，上传器不添加调试PDB。+5阈值10及对应粉线、-5恢复战后减1、欲望说明按用户原文、眼罩普通/精英新标题及Boss提示移除均包含。

旧产物和两处所有JSON已备份到`C:\Users\wilson\Documents\Codex\2026-09-30\amane-recovered-context\backups\desire-control-blindfold-both-20261005-160300`；替换前后SHA256与构建包一致，两处JSON逐文件哈希完全保持。DesignDoc未改写；原仓库暂存区保持。复用构建`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`（0警告0错误），不重复构建/静态测试，游戏内NOT_RUN。未修改沙箱、未执行上传、不终止游戏。部署记录：outputs/desire-control-blindfold-20261005/deployment-20261005-160300.json。
