

## 2026-10-04 商人？？？立绘尺寸修复（SYS-TRF-004 / FAKE-MERCHANT-SCALE-01）

前置快照`0a405113b6101a11776ba6b8afc5f7210b74a6d7`，设计提交`114c91b0e27892a775411a16e59fb3ecbff1db5b`。复核完整SYS-TRF-004外观、四档差分与反馈规则；保存DesignDoc相对HEAD及e42638b3逐行/词级漂移。独立按摩叙事及UI-COMBAT-FEEDBACK-003/004文案设计仍保留，不改该内容，文案已完成／新通知OPEN边界沿用上批技术同步。

原版NFakeMerchant.AfterRoomIsLoaded逐实例Character.CreateVisuals后添加到CharacterContainer，再StartCharacterAnimation；未做角色显示缩放，之后仅设置Position和多人背排亮度。v0.107.1与v0.111.0均存在同一StartCharacterAnimation入口。天音自身内部Visuals按0.36与待机/衣装刷新恢复；因此不能改全局RestScale或内部反馈层。

任务READY：只在NFakeMerchant.StartCharacterAnimation后缩放MaidenSuccubusCreatureVisuals根节点到原Scale×0.5，保留脚底根锚点与原版Position、内部衣装比例与反馈。以实例元数据保存原始比例，重复调用仍原Scale×0.5，不再次减半；新事件视觉实例独立记录。其他角色和商店不触发，不改资源/PCK/JSON，不添加事件玩法补丁。完成Debug构建后IMPLEMENTED，用户确认视觉后VERIFIED。不运行静态测试。

手测入口：商人？？？事件中天音缩小至一半、关闭/打开商店与地图或读档仍正确；离开并开始战斗后原尺寸。沿用上一批尚未部署的女神试炼UI，累积产物包含其改版，本轮仅build、不自动把待验UI部署到安装目录、沙箱或上传器。

FAKE-MERCHANT-SCALE-01 IMPLEMENTED：事件专属根节点缩放0.5并以实例原Scale幂等。Debug构建0警告0错误，未运行静态测试与游戏内验收，未部署；包含上一批待验女神试炼UI。
