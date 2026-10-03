

## 2026-10-03 三路线解锁持久化与选角精简（START-003 / START-SAVE-01～04）

前置快照`fa105ffbfd0d378943c5dd4c94ac8b816741aca6`，用户确认UI调整设计提交`0ba630ccb0e597ab2abce0398f74919d484b3c5f`。相对HEAD与上一接受版本逐行/词级差异已保存，最新接受版本无设计漂移；全文复核START-001/003、三开局Plan13.4及20261003实现/验收记录。既有永久解锁要求回修，UI按本轮明确要求同步设计。旧Plan“未解锁显示条件”由本批精简覆盖，其余开局、遗物、大厅同步与存档初始化规则不变。

证据：20:16:51日志在6586行记录corruption=5胜利解锁Succubus，6721行成功以+3开局；随后多份启动日志（含当前godot.log2505行）记录route_start_unlocks.json File not found。本机没有该解锁文件。已反编译构建依赖0.4.64及实际Workshop0.6.5 Shared：ModDataStore.Modify→PersistentDataEntry.Modify仅修改内存与Changed事件，不写磁盘，需显式Save(key)。当前RecordVictory缺此调用，因此运行内可用、重启丢失。保留Profile作用域和稳定ModId，不改成全局/跑局变量。

任务：合格胜利Modify后立即Save(Key)，重复合格胜利也请求保存，避免内存已解锁时提前return使先前未成功写入的标志永远不再保存。已解锁路线不重复修改，另一标志保持。框架原生持久化仍负责路径、备份、档案切换与云镜像；Save接口不返回结果，不把调用完成日志当作磁盘校验。移除RouteUnlockConditions节点/字段/赋值，把108px行收为48px，只保留名称、初始值与原生箭头，未解锁仍不能切换。原OnEnded胜利/本地角色/ShouldSave/非放弃/终局幂等判定保留。

验收：START-SAVE-01天音≥3胜利关闭重启保留+3；02≤-3胜利保留-3且另一路线不被覆盖；03档案独立、失败/放弃/其他角色不解锁，新旧文件字段兼容；04选角下方无条件或状态说明、无原空白占位，箭头仍只轮转已解锁项。IMPLEMENTED待用户实测，不运行静态测试，仅build；暂不部署，不修改游戏/沙箱/ModUploader/安装JSON或用户存档。之前从未落盘的旧标志不能凭空读回；原版历史run只记录胜负不保存最终堕落值，不从遗物或当前跑局猜测通关路线，不做自动补授。

最终构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误；静态测试NOT_RUN，游戏内NOT_RUN。outputs/start-unlock-save-20261003包含新DLL/PDB和原样继承wet-power-restore-20261003的完整PCK/反馈配置；暂不部署。
