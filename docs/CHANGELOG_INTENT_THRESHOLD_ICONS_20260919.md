# 色情意图阈值图标与圣焰可见性调整（2026-09-19）

## 用户确认

- 欲望攻击阈值、拘束意图阈值、侵犯意图阈值直接复用对应意图图标。
- 圣焰不作为可见 Power 状态显示。

## 实现

- `DesireIntentThresholdPower` 复用 `desire_attack` 图标。
- `ControlIntentThresholdPower` 复用 `restraint` 图标。
- `InvasionIntentThresholdPower` 复用 `violation` 图标。
- 三个 Power 的 64×64 状态栏图标与意图共用同一运行时纹理缓存；Power 悬停大图使用同系列 256×256 素材。
- `HolyFlamePower` 设置为隐藏 Power；其层数、燃烧增幅、保存和移除行为不变。

## 版本管理

- 变更前快照：`maiden-pre-intent-threshold-icon-reuse-20260919`
- 本轮只提交上述源码与本文件，不纳入工作树中的其他用户素材或修改。

## 验证

- `dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`：通过。
- 编译结果：0 warnings、0 errors。
- 内容契约：222 张卡牌注册与当前卡池计数通过。
- 结构契约、文本审计、视觉素材、68 张第二轮数值测试和 11 项拘束意图结构测试全部通过。
- 显式部署在本轮提交后执行。
