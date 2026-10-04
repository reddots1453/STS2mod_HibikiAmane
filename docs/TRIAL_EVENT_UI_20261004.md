

## 2026-10-04 女神试炼事件式开场（ACT4-001 / TRIAL-EVENT-01～05）

前置快照`654f81b8ea7c3be35b0d2cadcf260e4a708742ac`，设计提交`fd663f81f160504f821d4fcb0ba35e8d32530c5e`。复核ACT4-001完整三阶段流程、开场叙事、当前阶段预览与存档恢复、现有背景任务及多人地图旧选择器；相对HEAD及64555c2b逐行/词级差异保留在批次。用户未提交的按摩事件叙述保留工作树和快照，不混入范围提交。独立文档任务刚交接UI-COMBAT-FEEDBACK-003/004，已阅读全文和docs/COMBAT_FEEDBACK_TEXTS_20261004.md：003纯文案填充源配置已完成，技术结论无需新增代码，文案IMPLEMENTED／安装部署NOT_RUN；004触发语义仍OPEN，只保留当前解析公共接口，危急阈值/关联/限频不固化，不为新键添加通知。此次UI实现不修改该任务的源JSON或设计段落，不覆盖用户安装配置；两份技术文档同步该交接边界，后续另批处理。旧双栏规则由用户本轮事件风格要求替代，叙事稿同步，不改变玩法。

原版参考来自实际SlayTheSpire2.pck中的default_event_layout.tscn和event_option_button.tscn及NEventLayout/NEventOptionButton/NButton源码：800宽、金色36号标题、奶油26号正文、kreon字体，纵向选项使用event_button底图、NinePatch描边、HSV亮度、1.01悬停与0.99按压比例。仅复用原生视觉子节点；不让原生EventModel/EventSynchronizer/NEventRoom接管自定义试炼，从而不干扰当前Neow或其他事件。

| 验收ID | 任务 | 状态 |
|---|---|---|
| TRIAL-EVENT-01 | 复用原生事件标题／正文字体与选项视觉模板，整屏已审定背景＋右侧叙事＋纵向选择条；取消双栏圆角面板。按1920×1080设计画布等比适配窗口，正文滚动、按钮固定可见 | READY |
| TRIAL-EVENT-02 | NButton子类Ready仅ConnectSignals，原生鼠标／手柄输入和音效；选项hover/press动画与左侧原生遗物悬停预览，退出清理tween/hover；键盘方向循环聚焦，忙碌时按钮禁用 | READY |
| TRIAL-EVENT-03 | 确认锁定/单次沉睡遗物/故事恢复/踏入尖塔及异常退出继续原有服务，不新增随机抽取、不更改收据；原版故事全文和富文本不改字，长篇滚动阅读，故事继续也是事件条 | READY |
| TRIAL-EVENT-04 | 多人／旧地图二选一复用同一事件页面与输入。原生战利品奖励页不改，第一奖励动态描述/能量富文本仍取生产数据；只预览第一阶段，不提前泄露后续试炼 | READY |
| TRIAL-EVENT-05 | Debug构建0警告0错误才范围提交，按用户要求不静态测试；输出独立DLL/PDB与原PCK，不改任何JSON、不改上传器/沙箱。本轮为UI优化，未单独要求部署时不自动部署 | READY |

手测入口：正常Neow前、首次无Neow地图、两项鼠标／手柄聚焦与点击、悬停奖励、长故事滚动及继续、中断故事读档、多人与不同窗口比例。完成后仅IMPLEMENTED，用户实测后才VERIFIED。

TRIAL-EVENT-01～05实现完成，状态IMPLEMENTED：原生视觉模板、右侧滚动正文与纵向选项、原生输入/悬停、两套开场入口保持服务与存档行为。Debug构建0警告0错误，未运行静态测试或游戏内验收，未部署；旧资料中的双栏背景位置为历史，当前使用事件式整屏背景与预览。
