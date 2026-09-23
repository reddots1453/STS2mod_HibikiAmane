# 2026-09-23 百科卡图、高清大图、选角界面与阈值角标

## 版本边界

- 修改前提交：`b9e0d3b8f3242b0ba442d9db80d8c898c92c36c3`。
- 修改前快照：`maiden-pre-compendium-hd-character-select-threshold-labels-20260923`。
- 本轮只处理卡图显示管线、角色选择视觉和三类色情意图阈值 Power 的数值显示，不改变卡牌或意图机制语义。

## 运行日志证据

- 百科滚动过程中，旧管线会为首次出现的每个卡牌类型同步生成一个 `user://maiden_succubus_card_art_<Type>.tres`。
- 实测每个生成资源约 `14.9 MB`；连续滚动本角色的一百余张卡时会反复执行 PNG 解码和 ResourceSaver 落盘。
- 打开大图时，已安装的 `ScbsInspectCardPortraitPatch` 调用 `AssetCache.GetCompressedTexture2D(card.PortraitPath)`；旧路径实际保存的是 `ImageTexture`，因此日志明确记录 `InvalidCastException: Godot.ImageTexture -> Godot.CompressedTexture2D`。

## 修改

1. `CardAssetProfile.PortraitPath` 改为始终提供有效的原版压缩纹理回退路径，保证所有要求 `CompressedTexture2D` 的外部补丁安全运行。
2. 正式卡图不再在百科滚动热路径生成或保存 `user://` 资源；改为通过进程内缓存解码一次，并仅对本模组的 `NCard` 实例直接安装纹理。
3. 为 `NInspectCardScreen.UpdateCardDisplay` 增加末序、角色模组限定的高清重应用层，保证第三方大图补丁结束后仍显示本模组 `1000×760` 正式卡图。
4. 根据本轮用户指令，将 `运行时背景候选V1/hibiki_amane_char_select_bg_v01_2561x1201.png` 晋升并复制为运行时选角背景；不使用母图、概念稿或裁切预览。
5. 选角按钮使用正式 `256px` 角色图标；因原版接口强制返回 `CompressedTexture2D`，保持安全预加载路径，并在按钮生命周期结束后仅替换本角色的 `TextureRect`。
6. 欲望攻击、拘束和侵犯三类阈值 Power 改用原版 `Counter` 显示规则，使阈值数值自动出现在状态图标右下角。
7. 视觉验证器新增卡图热路径、高清大图补丁、选角背景/图标和阈值角标结构断言。
8. 当前正式卡图 manifest 已由用户扩充到 120 张；运行时补齐此前缺失的八张正式图：`DreamMist`、`Surf`、`IceBreakingSlash`、`GaleSword`、`ShiningSword`、`FlameSword`、`JudgmentBlade`、`FlashStab`。验证器改为以 manifest 为权威数量，不再固化旧的 112 张基线。

## 验收重点

- 百科中持续滚动响木天音卡牌时不再发生逐卡落盘卡顿。
- 从百科、奖励和牌组打开卡牌大图时显示对应正式高清卡图，日志中不再出现上述纹理类型转换异常。
- 选角界面显示响木天音正式背景和角色图标；锁定图标使用正式描边版本。
- 三类色情意图阈值 Power 的右下角分别显示其怪物配置阈值。

## 构建与部署记录

- 功能提交：`2ae07b2109c14c61e4f5c06c1294f9a96bdd51bb`。
- Debug 编译：`0 warning / 0 error`。
- 内容契约、结构契约、视觉契约、本地化样式、222 张卡牌测试契约和 11 个拘束意图场景契约通过。
- 完整验证仍报告既有的 10 条 DesignDoc 卡牌审计差异；本轮未新增差异。
- 已部署到 `D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`，并同步热加载目录 `D:\game_backup\steam\steamapps\common\Slay the Spire 2\MaidenSuccubus`。
- 部署 DLL SHA-256：`A03E38F5D84DE201E944F1901D574E0C14C631706B13B40F17C71D1E4AC1EB10`。
- 两个部署目录均包含 121 个运行时 PNG（120 张正式卡图 + `default.png`）；抽查 DLL、选角背景、256px 图标和新增卡图均与源码产物 SHA-256 一致。
