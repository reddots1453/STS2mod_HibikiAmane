# DS27-01A：耐久与形态同步

前置快照 `fac99db7`；2026-09-27，DesignDoc逐行/词级检查无漂移。只实现主文档§5.0～5.2.1及已确认Q1，不涉及怪物新行动或新数值。状态 **IMPLEMENTED，未部署、未实机验收**。

## 实现

- `TransformationCmd`分开InitialArmor=3与MaxArmor=5；所有既有损失命令统一钳制，正数到0保留，零层再损失退出。普通形态不获得耐久；魔力解放选择后再检查支付资源，拒绝/0层不扣费不解除形态。
- `MagicArmorLifetimePatch`只拦截本Mod处于变身的0层MagicArmorPower自动移除；原版力量等状态零层照常移除。Explicit Remove仍可用，退出/清理没有被拦截。
- `MagicArmorPower`使用0.67乘算，原版最后取整；预览无副作用。实际有耐久伤害在AfterModifying回调扣层；0层不改变数值，必须在AfterDamageReceived检查真实未格挡伤害才能退出。每玩家回合重置、敌方开始不重置、切换形态保留已使用标志；排除完全格挡/无属性/非敌方伤害。
- 所有现存直接`Decrement(armor)`生产入口（魔力解放、回合结束状态、既有诅咒）迁移到统一命令，其余已用LoseArmor的调用方不扩展效果。
- 三形态互斥，永恒不再依赖叠加无垢来发放9增幅；保留旧叠加状态时无垢不额外发放第10层。不同形态可切换，同形态不可刷新耐久；鼠标/手柄共用原版不能出牌提示入口显示“我已经变身了！”。
- 0和1层均显示严重破损，只有实际退出才显示普通形态；缺损计算依然相对3而非上限5。
- Power与悬停文案同步33%、攻击限定、归零边界、换行、初始值/上限及零层无实际损失奖励；严格文本契约一起更新。

## 自动验证

工作目录Mod根目录；未通过脚本不伪称通过。

| 命令 | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告0错误 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 41项通过，其中本批7项 |
| `python scripts/TestCardLocalizationAudit.py` | 19项通过；累计60项 |
| `scripts/ValidateCardEffectTests.ps1 -ProjectDir .` | 225注册、223可执行/2待定，结构门通过 |
| `scripts/ValidateMvpContent.ps1 -ProjectDir .` | 通过 |
| `scripts/ValidateLocalizationStyle.ps1 -ProjectDir .` | 通过；不是绕过旧契约，而是按新DesignDoc更新对应两项 |
| `scripts/ValidateStructuralContracts.ps1 -ProjectDir .` | 通过 |
| `scripts/ValidateVisualAssets.ps1 -ProjectDir .` | 既有第317行诱惑度口红/数值布局契约失败；未改该UI或降低门禁 |

`ms_test_cards confirm ds27-transformation`仅可在响木天音单人一次性测试战斗使用：会清空/规范化当前测试战斗，勿用于正式进度。2个模型原基础/升级4场景，加4个专门场景共8个；实际执行尚未发生。

独立命令断言覆盖：上限/超量/恰好归零/再损失退出/零层修复、原版力量零层移除、实际正数损失奖励与零层无奖励、首次/多次/回合边界、全部/部分格挡、无属性/自身伤害、纯预览、不同形态切换与互斥、永恒9层/返回1层、最后一层支付/零层不能支付/拒绝/增幅自动支付。

修正8个既有魔力解放测试夹具：不再直接给普通形态加1耐久，而是实际进入形态后调整。魔力爆发的正确预期包括保留下来的1层形态增益，因此为13/16，而不是旧的11/13。

## 待游戏内验证与剩余范围

- 执行上述8场景及全卡回归，查看独立报告中的失败断言。
- 零层Power数值、立绘、悬停排版；相同形态鼠标/手柄提示与异形态可出牌。
- 保存退出/重新进入、跨房间/战斗结束是否恢复普通形态，无残留UI；本批无中途战斗存档往返实测，不标VERIFIED。
- 完整DS27仍有多重附魔、退役迁移、其他卡牌、遗物、事件、第四层、封印视觉及相应测试待完成；不以本批通过取代全目标。
- 不覆盖并行素材、共享审计报告或并行CHANGELOG内容；提交只包含本批文件与本批日志块。
