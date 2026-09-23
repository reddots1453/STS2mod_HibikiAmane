# 2026-09-23 独立卡图显示修复

## 版本边界

- 修改前提交：`9e4275dcda5c436a60501645118c3762ac17f23a`。
- 修改前快照：`maiden-pre-card-art-standalone-fix-20260923`。
- 本轮只修复禁用第三方 `Succubus mod (scbsmod)` 后响木天音卡图退回占位图的问题，不改变卡牌效果、描述或卡池。

## 运行时证据与根因

- 禁用 `scbsmod` 后的最新日志确认响木天音运行时目录仍包含完整卡图，且没有图片读取、解码或 Harmony 安装异常。
- 上一轮为规避 `scbsmod` 将 `ImageTexture` 强制转换为 `CompressedTexture2D` 的异常，把模型公开的 `PortraitPath` 改成原版压缩纹理回退路径，再由本模组补丁恢复高清图。
- 恢复层通过 `SetDeferred` 延迟写入画像节点。百科中的池化卡牌会在延迟赋值真正执行前继续刷新或复用，因此禁用第三方刷新补丁后，统一回退图成为最终显示结果。

## 修改

1. 保留安全的原版 `CompressedTexture2D` 模型回退路径，继续兼容会读取 `CardModel.PortraitPath` 的第三方大图补丁。
2. `NCard.Reload` 完成后，直接读取原版 `_portrait` 与 `_ancientPortrait` 节点并同步赋值正式卡图，不再依赖延迟消息队列。
3. 大图界面继续在 `NInspectCardScreen.UpdateCardDisplay` 后复用同一赋值入口。
4. 视觉结构门禁止卡图恢复层重新使用 `SetDeferred`，并固定原版画像节点与同步纹理赋值要求。

## 验收重点

- 禁用 `Succubus mod (scbsmod)` 时，百科、牌组、奖励及战斗卡面仍显示响木天音正式卡图。
- 禁用状态下可以打开卡牌大图，且大图显示对应正式图片。
- 重新启用 `scbsmod` 后不再出现 `ImageTexture -> CompressedTexture2D` 转换异常，卡面和大图仍正确。

## 构建记录

- `dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true` 已完成编译、内容契约、结构契约与视觉契约；视觉门确认 120 张正式卡图、默认图和高清大图管线通过。
- 完整验证最终被当前工作区 DesignDoc 尚未同步实现的 12 条卡牌审计差异阻断，本轮未越界修改这些卡牌语义。
- `dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 通过，`0 warning / 0 error`。
