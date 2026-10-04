

## 2026-10-04 暂停战斗RPG文本（UI-COMBAT-FEEDBACK-005）

快照`10fffc9d581027df0c465b2f5c8c3f9f820d0c8c`，设计提交`919742e0291c32827b5a1e5a2ac01fc378f97af9`。已完整读取CombatTextFeedback/FeedbackTemplates和UI-COMBAT-FEEDBACK-001～004设计及现有触发边界；对HEAD及最后接受版本的DesignDoc逐行/词级漂移留档，其他事件叙事和反馈新通知OPEN设计保留，不混入当前范围。技术任务READY：在统一入口增加只读PresentationEnabled=false，Initialize在订阅事件前返回，Notify在读取配置/轮换/创建浮动层前返回。所有现有Notify接口、清理方法、文案JSON和解析器保持；已有enabled=true不能绕过暂停。独立CorruptionChangeFeedback和原版浮动伤害数字不受影响，不改战斗Hook及机制。验收入口为所有受伤/欲望/拘束等事件均无RPG浮动文案，原生数字及堕落变化仍正常；仅构建，不运行静态测试、不部署，源/安装JSON均不改。

UI-COMBAT-FEEDBACK-005 IMPLEMENTED：所有战斗RPG浮动提示已通过统一呈现门禁停用，文案和接口保留；构建0警告0错误，静态测试NOT_RUN、游戏内NOT_RUN、未部署。


## 2026-10-04 用户授权部署本地与ModUploader（DUAL-DEPLOY-20261004-211607）

用户明确要求“部署到本地mod目录和moduploader”。源码提交`ee25fefab34e959f18db6a5fa9a51e54fd3f1dda`，部署前快照`3205903e644b88cb6fe123b6e4e1359345b282af`，复用已通过构建的Debug DLL/PDB和完整PCK，构建命令`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`，0警告0错误。累计包括左侧试炼路线叠图移除、十四项黑暗/光明女神试炼名称统一、战斗RPG文本暂时停用及此前已接受修订。本轮只有文件部署，无新玩法、无再次静态测试，游戏内验收NOT_RUN。

本地`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`替换DLL/PDB/PCK；上传器`D:\game_backup\steam\steamapps\common\Slay the Spire 2\ModUploader-win-x64\MaidenSuccubus\content\MaidenSuccubus`仅替换DLL/PCK，保持官方content/MaidenSuccubus目录结构，不新增嵌套目录。替换前所有旧产物与JSON备份于`C:\Users\wilson\Documents\Codex\2026-09-30\amane-recovered-context\backups\local-uploader-combat-text-pause-20261004-211607`，部署后逐文件SHA-256及两处DLL/PCK一致性核验通过。全部已存在JSON（包含本地manifest、combat_feedback.json、上传器manifest及workshop.json）哈希原样；上传预览图、mod_id.txt及README也保持。未改沙箱，不执行Steam上传或发布，不结束游戏进程。明细：聊天outputs/combat-text-pause-20261004/local-uploader-deployment-20261004-211607.json。
