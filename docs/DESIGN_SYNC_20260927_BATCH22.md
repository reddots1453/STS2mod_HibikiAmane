# DS27 批次22：第四层入口、检查点与原版结局

日期：2026-09-28。前置快照：`8e30603f`。需求：`ACT4-001` → `DS27-05A`。
状态：IMPLEMENTED，尚未游戏内验收，未部署。

## 复核与范围

DesignDoc逐行/词级相对HEAD均无变化。重读第四层全部路线与流程、Plan、追踪矩阵，先登记05A再实现。
本批只处理确定的“空注册、第三幕后正常结束”边界，不声称已完成三段式试炼整体。

旧实现存在两个越界：

- `AfterCombatVictory`在第三幕Boss胜利后直接把第三阶段路线遗物升级，并追加第四层。
- `EnsurePresent`即使Enabled=false，也允许已觉醒且满足堕落阈值的跑局绕过关闭入口。

当前安装DLL的`RunManager.EnterNextAct`在最后一幕结束时进入建筑师；胜利房间再次调用它反而抛异常，必须走独立`WinRun`。仓库旧反编译版在此处不同，不能沿用旧结束流程。

## 实现

- `FourthActEntryRules`是实际生产纯规则：七罪/七德映射、觉醒资格与大于-3/小于3边界、仅第三幕后一次检查点、未来占位章节筛选。普通入口保持关闭。
- `FourthRouteProgressService.HasFourthActQualification`独立于`CanEnterFourthAct`；已觉醒且满足阈值也不会启用未完成章节。无路线/未知枚举/其他角色不合格。
- 删除战斗胜利自动觉醒分支；调试阶段推进也不再伪造“已击败第三幕Boss”。其他战斗试炼计数保留，后续按42试炼独立重构。
- 新局、读档与切幕前调用`NormalizePendingActs`，只从当前章节之后移除本Mod占位Act，保留其他Mod章节的身份和相对顺序。不删当前/已进入章节，不修改当前地图与房间，不覆写用户存档文件。
- 切幕前不登记结束；包装原Task并完整await，成功后仍是同RunState且房间确为建筑师才写一次检查点。原Task失败/取消向上传播，不吞异常、不登记成功、不调用替代房间/胜利命令。
- `FourthRouteEndingChecked`、`FourthRouteEndingEligible`存入既有M5Progress状态；缺字段旧档默认为false。历史`ThirdBossDefeated`只在真实结束检查点设定，不冒充已迁移的资格快照。
- `ms_route state`显示endingChecked、endingEligible、qualifiesNow、canEnter；`set`调试重置清除新字段。
- `ms_act4`仍是显式调试命令，但只在单人Debug构建允许；不再临时打开进程级全局开关，Release返回不可用。占位Act模型和已进入的调试旧档兼容仍保留。

## 已执行验证

从Mod目录执行：

| 命令 | 结果与证据边界 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告、0错误，无部署 |
| 同上`-c Release` | 0警告、0错误，确认Release关闭调试分支；随后重建Debug |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 176项通过，其中本批8项接线/文本检查；不是游戏执行 |
| `python scripts/TestCardLocalizationAudit.py` | 19项通过，静态合计195 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 1031条断言通过；新增687条直接执行生产规则，独立字面边界表，未复制实现 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33条既有附魔编码/DTO断言通过，不证明完整Run存读档 |
| `ValidateMvpContent.ps1`、`ValidateStructuralContracts.ps1`、`ValidateLocalizationStyle.ps1`、`ValidateCardEffectTests.ps1`各`-ProjectDir .` | 全部通过 |
| `ValidateVisualAssets.ps1 -ProjectDir .` | 既有第317行诱惑度口红独立数值栏断言失败；不改宽、不宣称全门通过 |
| `python scripts/AuditCardLocalization.py --no-write` | 225注册；1费用差异GagCurse、4未解元数据、215待文本、2设计独有、2退役兼容；未覆写共享审计报告 |

新增生产规则测试涵盖14路线×-5到5全部堕落值、有/无觉醒、其他角色、未知路线/方向；检查点只允许第三幕后首次；原版三幕列表无变化；待进入占位Act及重复项删除；其他Mod章节保留；当前/已进入调试第四层保留；身份/顺序/输入不变；归一化幂等；空列表与非法索引不处理。

## 仍须游戏内验收

1. 普通与进阶10第三幕：完成全部必要Boss后才发生检查点；第二Boss之前不提前结束或觉醒。
2. 已觉醒、未觉醒、堕落阈值满足/不满足：均正常建筑师→胜利记录；遗物形态不由Boss自动改变。
3. 第三幕结束前`ms_route state`无新检查点；到建筑师后只记录一次，canEnter仍false。真实切幕异常/取消/返回主菜单不得误记。
4. 旧档含尚未进入的第四层：加载并通关正常结束；保存再加载、历史章节/地图保持正确。已进入第四层调试旧档保持当前位置。
5. 全部原版角色不增加路线状态、不改章节列表。混合多人房间完整同步与重连尚未运行，不能据纯规则测试宣称联机验证。
6. 其他Mod额外章节的相对顺序不变；是否继续该Mod流程由其自身决定，不强行终止其他Mod内容。
7. 单人Debug可显式`ms_act4`，然后返回菜单开新局不残留开关；Release与多人不允许此调试跳转。

## 未完成范围

42项三段试炼、选择后沉睡遗物、每段奖励、100金币可见碎片、献祭仅解锁、各阶段拾起/常驻效果、慷慨互斥供奉UI、免费商店与预约优先等仍待实现。Q7/Q8已确认，不是新的澄清阻塞。没有将部分入口完成当作全部路线完成。
保留并行Changelog和全卡审计工作区修改，仅提交本批明确文件及Changelog首个新增块。
