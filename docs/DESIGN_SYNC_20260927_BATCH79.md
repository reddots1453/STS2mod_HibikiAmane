# DS27 第79批：免费X费用遵循原版

计划前`63b15207`，计划/实施前快照`e897edb7`。Q17/ACT4-001，含直接关联的本回合免费包装；不重审无关卡牌，不部署。

## 修改与原版依据

- 原生`CardEnergyCost.GetAmountToSpend`对CostsX直接返回当前能量，不读取固定费用0层。
- RitsuLib 0.4.64的`CardModelSetToFreeThisTurnBindingPatch`已绑定原版调用；`SecondaryResourcePaymentFreeMode.FromCardCostScope/AppliesTo`区分固定和X型资源免费。直接读取当前引用DLL反编译确认，不另造一套X免费规则。
- `GeneratedCardCostCmd.SetFreeThisTurn`删除手工SecondaryResourceCost.Free覆盖，只调用原版。旧覆盖会把X费用本身变成固定0，造成语义错误；原版/Ritsu入口负责正常固定次级资源免费。
- UntilPlayed现有三个最终费用处理均已排除X，保留实现，仅去掉“待Q17确认”的旧注释与断言名称。其复制、保存、跨回合及最终重放后清理不变。

## 定向验证

`python scripts/TestDesignSyncFreeUntilPlayed20260927.py`：10项通过。

`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`：0警告0错误。初次新增测试数组混合命名元组导致名称推断丢失，已显式指定类型后复验成功。

游戏入口仍为`ms_test_resource_relics confirm`，仅可丢弃的单人天音战斗，沿既有`DesignSyncFreeUntilPlayedContract.Run`调用新增`RunXCosts`：两种免费持续期×基础/升级×正常、重放、零能量、零副资源共16组，真实SpendResources后OnPlayWrapper，检查实际扣除、伤害和命中历史、重放一次付款及资格清理；另测本回合免费固定能量/副资源首次0、再次恢复正常扣除。UntilPlayed额外经过回合清理后验证资格保留。

游戏脚本仅编译，未执行；不把静态字符串契约等同实际支付通过。实际双X与多玩家/自然跨战斗仍需游戏验收。状态IMPLEMENTED，非VERIFIED。未修改并行文件和审计输出，未运行全量12门。九问剩Q14/Q15/Q16，本轮目标仍未完成。
