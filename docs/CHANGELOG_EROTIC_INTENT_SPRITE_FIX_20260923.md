# 2026-09-23 — 色情攻击意图头顶图标修复

## 问题

- 自定义图标只出现在色情攻击阈值 Power 与意图悬停栏中。
- 怪物头顶的欲望、拘束、侵犯和撕裂衣服意图仍显示原版攻击、减益或状态牌动画帧。

## 根因

- 意图悬停栏使用 `AbstractIntent.GetTexture()`，因此此前已能读取自定义素材。
- 怪物头顶的 `NIntent` 则根据 `GetAnimation()` 返回的动画名，在 `_Process()` 中持续写入原版图集帧。
- 旧补丁挂在私有 `UpdateVisuals()` 上；该短方法可被 JIT 内联，运行时没有稳定截断逐帧覆盖。

## 修复

- 图标补丁改挂公开且稳定的 `NIntent.UpdateIntent()`，在意图模型绑定到节点后写入自定义纹理。
- `ControlIntent`、`InvasionIntent`、`DesireGainIntent` 与 `TearClothingIntent` 不再请求原版动画；`NIntent._Process()` 因动画名为空，不会覆盖自定义纹理。
- 普通攻击、格挡、状态牌等原版意图，以及衣物危险状态牌补充意图，继续使用原版动画。
- 结构验证新增目标方法与四类自定义意图禁用原版动画的回归契约。

## 版本管理

- 修复前快照：`maiden-pre-intent-sprite-render-fix-20260923`
- 基线提交：`feefbba00f0ae8835a107d9c7e6245c48222de58`
- 功能提交：`56afca7`（`fix(maiden): render custom erotic intent sprites`）。
- 完整验证通过编译、内容、结构与视觉契约，随后仍被同一组 12 项既存 DesignDoc/源码卡牌差异阻断；独立本地化、卡牌效果、拘束意图测试契约均通过，部署构建为 0 警告、0 错误。
- 已在游戏进程关闭时部署。源码与安装目录 DLL SHA-256 均为 `16D76DFC64D0C3FEF94EF38396C407E202302F7B94055F39A7FA797946148C55`，PDB SHA-256 均为 `BEDACCE08B701F29BA5D8164B31EF4B60376DC0723C4982822B765CA2ECD6073`。
- 欲望与撕裂衣服意图素材在源码、安装目录和热加载目录中的 SHA-256 分别一致为 `0EF61A19F9178CC8E9035D0C2B67702B01A9B86A4D5066409E68FF7327F15764`、`CFAE3B7837C1AF78D6F1A6E8BF1CC2707AF0BE750AC0C08E0236984C28A3264C`。
