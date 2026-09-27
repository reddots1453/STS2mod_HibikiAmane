# DS27 批次29：嫉妒负面效果与首次触发

日期：2026-09-28；前置快照：`1e191366`；ACT4-001 → DS27-05I。
IMPLEMENTED，游戏内待验；未部署，总目标未完成。

## 审阅与实现

DesignDoc逐行/词级无漂移。完整复核嫉妒及阶段替换/显示规范，残缺每战斗第一次给予负面效果得1能量，完整再抽1牌，觉醒改为每回合首次。未修改设计机制或数值。

当前DLL的StrengthPower固定Type是Buff，但负向变化经GetTypeForAmount判为Debuff；人造物、血肉巧技使用同一原生分类。PowerCmd的AfterPowerAmountChanged发生在实际结算后，被完全阻挡的0变化不触发通知。

- 使用变化量的原生类型，不能只看power.Type/正数。降低力量即使仍剩正力量，也算负面变化；给力量不算；减少虚弱是清除，不算；0变化不占用首次。
- 保留applier==Owner.Creature并验证目标与Owner处于同场战斗，其他施加者不会触发本人遗物。删除设计中不存在的敌方阵营过滤；本人对自己/同侧合法目标实际施加负面效果也符合正式文本。目标合法性由原生命令决定，不新增可选目标。
- 残缺/完整在战斗首尾重置，觉醒在本人回合开始重置。敌方回合、其他玩家额外回合及本人回合结束均不额外刷新，覆盖整个本人回合至下一本人回合的窗口。
- UsedThisWindow沿用同名bool存档字段；首次标记在发能量与抽牌之前设置，抽牌引发嵌套状态变化不会重复领取。状态为值类型，克隆不会共享可变计数。
- Stage4占位文案替换为觉醒正式效果，Stage3保持旧觉醒兼容；精确文本测试保留标点，只把能量图标模板还原为设计中的“1费”。

## 验证结果

| 检查 | 结果 |
|---|---|
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 3591断言通过，新增170条生产规则/状态测试 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 232通过，新增8；旧沉睡门跟踪提取后的实际守卫 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，静态累计251 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33既有编码断言通过，不是本批游戏内存档验收 |
| Debug与Release，`-p:DeployMod=false --no-restore` | 零警告零错误 |
| ValidateMvpContent / StructuralContracts / LocalizationStyle / CardEffectTests | 四门通过 |
| `AuditCardLocalization.py --no-write` | 225项；原有1项GagCurse费用差异、4元数据未解、215待文本、2设计独有、2退役兼容仍在 |
| ValidateVisualAssets | 既有317行诱惑度口红独立数字标签门失败，未修改/放宽 |

生产测试覆盖合法/非法阶段、本人/外部施加者、正负变化判定输入、已用标记、无效事件不占位、多段/多目标仅首次、每战斗与每本人回合的区别、值克隆和战斗重置。原生Power类型语义另由下述游戏内脚本验证，不用纯布尔规则测试冒充游戏行为测试。

## 游戏内脚本（已编译、未执行）

`ms_test_envy confirm`，日志`[DS27EnvyTest] PASS/FAIL`。

**仅Debug、单人响木天音、有存活敌人的进行中战斗；必须可丢弃测试局。会清除遗物、战斗牌堆和状态，改生命/资源，不恢复测试局；结束/异常恢复TestMode。**

5阶段分别执行实际RelicCmd.Obtain、PowerCmd.Apply/ModifyAmount、能量/抽牌结算及SavedProperties：

1. 虚弱施加/叠层与另一个负面效果，只首次获得能量/抽牌；沉睡无效果。
2. 保存已用字段并还原到独立实例，后续通知不重发奖励。
3. 敌方/外部玩家回合开始、本人的回合结束不刷新；本人下一回合仅觉醒刷新。
4. 人造物完全阻挡不触发，随后成功施加仍是首次。
5. 外部施加者、0变化、清除虚弱、增加力量不触发；从2力量减到1力量仍触发。
6. 对自身实际施加虚弱同样触发，不因阵营被错误排除。
7. 战斗开始/结束清除残留标记。

回合边界和外部玩家部分为直接钩子模拟，不是实际网络联机或完整回合管理器推进。尚需真实手测：完整自然回合/额外回合、双人目标与施加者、完整存读档、抽牌嵌套和实际提示动画。未进入游戏执行这些测试，不能据构建成功标VERIFIED。

## 剩余范围

其余路线、慷慨互斥供奉、正式开场/奖励UI及全量卡牌/怪物/事件/视觉待项仍未完成。Q12/Q13/Q14继续等待答案；未用本批通过缩减总目标。
