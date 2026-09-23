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
