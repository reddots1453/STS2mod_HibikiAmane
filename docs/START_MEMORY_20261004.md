

## 2026-10-04 秘密的记忆与开局点击回修（START-003 / START-MEMORY-01～05）

前置快照`57dbf9ed328dd2db1681b2ca3c970932ca1653d5`，设计提交`26e6349fc62018014a47cc7f549bb33ea14b93a6`。完整复核START全部、SYS-SEA封印与SYS-TRF分层完整衣装规则、已同步START-SHION任务及永久解锁边界；相对HEAD和5e068395逐行/词级漂移均保存。按摩事件的用户未提交叙述继续保留，不混入此次实现。用户确认布局正确，对上次START-SHION视觉布局记用户反馈通过；点击不可用未验收，不把所有交互标VERIFIED。

明确证据：本地godot.log 2817起出现StartRoutePortraitArt→NButton._Ready的InvalidOperationException，原生明确要求子类重写_Ready直接ConnectSignals，禁止base._Ready。上轮漏写子类初始化入口，导致hover/MousePressed/MouseReleased信号未连接、画面正常但无法切换。本次override _Ready=>ConnectSignals，不自行重复发鼠标Released，不影响原生手柄/聚焦/音效/拖动检查。

| 验收ID | 任务 | 状态 |
|---|---|---|
| START-MEMORY-01 | 头像按钮按原生子类初始化接通输入，保留斜切_HasPoint与原生_Mouse/GUI_Input，锁定项仍可选择；已准备后禁止切换。确认门禁与解锁数据沿用，不绕过锁定 | READY→IMPLEMENTED后待实测 |
| START-MEMORY-02 | 移除封印信息条和共用说明，只留条件、初始堕落值、可开始状态；保持用户确认的右侧头像/原生按钮布局。显示名称换为秘密的记忆三项，标题完整显示，缩略图名称分两行，实际枚举和保存不改 | READY→IMPLEMENTED后待实测 |
| START-MEMORY-03 | Refresh预览同步InfoPanel/VBoxContainer/DescriptionLabel；圣洁/堕落按用户精确两行中文，中立从原characters本地化读取原文。其他角色不覆盖原生描述；联机只本地角色预览，关闭和重新打开由原生选角/Refresh正确恢复 | READY→IMPLEMENTED后待实测 |
| START-MEMORY-04 | 复用已打包character_armor_3、character_corrupt_armor_3、character_normal三个完整服装图；_Draw调整UV区域维持比例并显示上半身衣装，不能三种状态继续同一头像。不修改源图、不生成新衣装、PCK/JSON原样继承，不改变跑局变身状态 | READY→IMPLEMENTED后待实测 |
| START-MEMORY-05 | Debug build，0警告0错误才范围提交；按用户不运行静态测试。延续本地实测部署上下文，若游戏占用不得强制退出或替换DLL；仅本地，不改上传器/沙箱/JSON和用户存档。Build不等于交互验收 | READY |

原版NButton/NClickableControl初始化、聚焦与GuiInput入口已逐项阅读；控件根Ignore、头像Stop和透明斜角不拦截仍沿用。只改三个UI代码、设计/计划/记录；不增加卡牌或数值修订。

实现完成：子类Ready按原生接通信号，三项名称/角色简介/完整衣装缩略图切换及两处说明删除；用户认可布局保持，Debug构建通过，未进行静态测试或游戏内验收；当前产物待本地部署，状态IMPLEMENTED。
