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

## 提交与部署

- 功能提交：`8830cf2`（`fix(maiden): restore standalone card art rendering`）。
- 游戏进程未运行时执行 `dotnet build -c Debug -p:DeployMod=true -p:ValidateMod=false --no-restore`，部署构建 `0 warning / 0 error`。
- 源码产物与安装目录 DLL SHA-256 均为 `1052D1723E282A575F6308C4F4800F5DDEF42C2966FA479B2547FB437D51B793`；PDB SHA-256 均为 `760464CA7A4BFE6685F4BF2D73AC2D85876DC2A5BE1651CC9AB44A66FAC7AC51`。
- 源码运行时目录、安装目录和热加载目录均包含 121 张 PNG（120 张正式卡图及 `default.png`）。

## 第二次运行时修正

- 用户在禁用 `Succubus mod (scbsmod)` 后复测，确认上述节点级恢复仍未显示正式卡图；因此上一轮关于 `SetDeferred` 是唯一根因的判断不充分。
- 新日志确认卡图文件、响木天音 DLL 与 RitsuLib 均已加载，且没有图片读取或解码错误；实际缺口是正式纹理没有进入原版统一读取的 `CardModel.Portrait` 管线。
- 新增仅作用于 `MAIDEN_SUCCUBUS_CARD_*` 的 `CardModel.Portrait` getter 后置补丁，把缓存的正式 `ImageTexture` 作为模型纹理返回。百科、奖励、牌组和战斗卡面因此不再依赖某个 `NCard` 节点刷新时机。
- 保留原版压缩纹理 `PortraitPath`，用于兼容旧版第三方大图补丁；同时保留节点级与大图界面的同步恢复层，防止其他 UI 扩展在模型取图后再次覆盖纹理。
- 首次成功替换会在游戏日志写入 `Card portrait pipeline active`，便于下一轮实测直接确认正式取图链已经执行。
- 修改前快照：`maiden-pre-card-model-portrait-pipeline-fix-20260923`（目标提交 `f3f1cd92342d72a93b386a149a2f4f5b97ee3502`）。
- 规定验证构建已通过编译、内容契约、结构契约和视觉契约，随后仍被同一组 12 条既存 DesignDoc/源码卡牌审计差异阻断；独立无验证构建为 `0 warning / 0 error`。
- 功能提交：`258ac3b`（`fix(maiden): route standalone art through card model`）。
- 已在游戏进程关闭时部署，部署构建为 `0 warning / 0 error`；源码产物与安装目录 DLL SHA-256 均为 `DE22EBF134B09B2D507539D5D1CEBD9B3B3398368F9855C36FCC8E0BBCC0C493`，PDB SHA-256 均为 `8CD80098DF51A18CEFCB8FDCFE3FB8FD15B716A61EFD1C87F441ED1F08245E35`。
- 源码运行时目录、安装目录及热加载目录再次核对，均为 121 张卡图 PNG。
