# DS27 批次31：慷慨拾起效果

日期：2026-09-28；前置快照：`156b75f6`；ACT4-001 → DS27-05K。
拾起IMPLEMENTED待游戏内验收；供奉交互OPEN；未部署，总目标未完成。

## 实现与边界

DesignDoc逐行/词级无漂移，复核慷慨和每阶段独立替换规则。GenerosityRouteRelic原完整阶段错误地只移除1张；现残缺1、完整2、沉睡和新旧觉醒阶段0。使用原生FromDeckForRemoval与RemoveFromDeck，不另造选择器；Cancelable=false。候选不足时遵循原版自动取全部可移除牌，永恒等不可移除牌不进入候选。

保存PickupEffectGranted，异步选择前预占，防重复调用/重入。返回后再次检查本人永久牌组与IsRemovable，去重并最多取阶段数量。每阶段新遗物各自拥有收据，旧永久移除不回退。Stage2正式文本改为2张；Stage3/4同步觉醒设计原句，不表示觉醒供奉已可用。

本批只验证完成后的收据保存，不承诺选择中强制退出能恢复待选项。原生保存通常在房间流程边界，但完整退出/重进、联机与异常恢复仍需实际验收；不要把SavedProperties字段复制测试当成整局存读档通过。

## 供奉入口调查与未决问题

- 普通战利品有LinkedRewardSet互斥奖励组；宝箱则通过TreasureRoomRelicSynchronizer共享分配，不可直接套用普通奖励。
- Q15已询问：多人宝箱是否先原版分配，再由获得者选择领取/供奉；分配前供奉的竞争语义未定。
- Q16已询问：等待碎片/献祭等非试炼计数、尚未觉醒阶段是否隐藏无收益供奉。
- 未将OnSkipped、关闭界面或离开房间冒充主动供奉，未推进放弃遗物计数。
- 指定STS1目录`D:/Games/steam/steamapps/common/SlayTheSpire`不存在，已知备用Steam库无STS1；未完成用户要求的STS1源码参考，也未扫描其他安装目录。当前只查阅已安装STS2 DLL。

## 验证结果

| 命令 | 结果 |
|---|---|
| Debug / Release，`-p:DeployMod=false --no-restore` | 零警告零错误，最后保留Debug产物 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 248通过，新增8项；三试炼旧检查改为明确核对1/2阶段门及早退，不删除沉睡检查 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，静态合计267 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 3591既有生产规则断言通过，本批无新增纯规则函数 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33既有编码断言通过，不代表慷慨运行时保存通过 |
| ValidateMvpContent / StructuralContracts / LocalizationStyle / CardEffectTests | 四门通过 |
| ValidateVisualAssets | 既有317行口红独立标签门失败，未修改门禁 |
| `AuditCardLocalization.py --no-write` | 225项，既有GagCurse费用1/2差异、4元数据未解、215文本待审、2设计独有、2退役兼容；未写共享审计报告 |

## 游戏内自动测试（编译但未运行）

`ms_test_generosity confirm`，日志`[DS27GenerosityTest] PASS/FAIL`。仅Debug、单人响木天音、战斗外；**破坏性：清空遗物和永久牌组，不恢复游戏数据，仅可丢弃测试局使用**。完成或异常恢复TestMode与运行锁。

167项断言走真实RelicCmd、CardPileCmd、TestCardSelector、SavedProperties：

1. 0/1/2/3/4五阶段 × 0/1/2/4张可移除牌，附带AscendersBane永恒牌；检查数量、选中牌、未选中牌、永恒保护、拾起标记。
2. 重复调用AfterObtained不再移除，原生保存后恢复阶段及收据，不重复开选择器。
3. 依次移除旧遗物、获得1/2/4阶段新遗物，共永久移除3张，觉醒不立即移除。
4. 延迟选择期间重复领取不启动第二选择器；目标提前离开牌组时只移除仍有效对象。
5. 测试选择器返回重复和超额对象时去重并限制2张（测试接口不限制min/max，实际UI会限制）。

仍需游戏内运行上述命令、人工查看选择布局、完整保存退出重进、自然阶段领奖和联机验收。供奉UI及其互斥/同步/规则测试仍未完成，其余全量变更待项与Q12～Q16保留。
