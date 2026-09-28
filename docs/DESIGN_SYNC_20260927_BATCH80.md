# DS27 第80批：慷慨供奉

前置/计划`4118e24a`，计划前`a7e25e36`；Q15/Q16/ACT4-001。当前DesignDoc行级与词级无漂移。范围限慷慨奖励交互、直接相关同步/保存/UI与测试，不部署。

## 已实现

- `Core/Routes/GenerosityOfferingRules.cs`：活动三次试炼可供奉；待领奖、碎片、献祭隐藏；觉醒且有可移除牌可供奉删牌。不是本角色/未选慷慨不启用。
- `Rewards/GenerosityOffering.cs`：原生LinkedRewardSet两个互斥子项；原遗物领取或明确供奉。组内异步占用及完成回执阻止重复选择，普通跳过只写原生未领取历史，不推进。觉醒用原生不可取消的牌组移除选择器，重验归属/牌堆/可移除、去重及最多两张。
- 战斗奖励在原生生成/修改完成之后包装，不重抽遗物，不改变普通奖励排序；事件/商店直接RelicCmd.Obtain不拦截。只包装原生RelicReward，不覆盖其他Mod的派生奖励逻辑。
- 宝箱保留全部投票、争夺和分配；仅替换AnimateRelicAwards分配后最终领取调用。得到遗物的玩家再作选择，其他玩家不竞争供奉。未确认前不播放提前入栏动画，领取分支才放开动画；结束/跳过清理待领取标记。
- `UI/GenerosityOfferingVisibility.cs`：原生关联奖励视觉与链线，显示“供奉”标识及说明；同屏其他供奉使试炼转待领奖后，其余无收益按钮和链线实时隐藏。
- 供奉组序列化为含确定ModelId的原生遗物奖励，恢复生成时按当时资格包装，避免RewardType.None原生无法恢复。没有另造一套游戏中途保存机制，保存时点仍遵循原版。

## 原版对照与网络适配

直接读取用户F盘STS1 jar中的`RewardItem`：蓝钥匙与遗物双向relicLink，选中一项设置另一项完成/忽略，单纯退出不等于取得钥匙。这里保留该二选一规则，不复制原版蓝钥匙美术冒充供奉。

STS2当前安装DLL的`RewardsSetSynchronizer`已直接反编译核对：本地用顶层`Rewards.IndexOf(reward)`，远端只按顶层索引取奖励；直接把LinkedRewardSet子项交给它会得到-1，且父组不会自动标为SuccessfullySelected。因此只对本Mod组使用稳定编码`1000000+顶层位置×2+分支`，保留原RewardSelectedMessage、发送者和房间/奖励栈流程；远端解码后调用原生选择重载，由原生完成奖励栈。已完成组的旧消息拒绝执行，避免误作用于后续奖励组。组完成只标父回执，不虚构第二次AfterRewardTaken。

异步方法改的是实际MoveNext调用点，每个transpiler要求恰好命中一个调用；所有patch业务体Safe.Run防护。新增引擎脚本检查实际IL替换，不能仅以注册了patch就认为命中成功。没有改其他原版/Mod的LinkedRewardSet逻辑。

## 实际执行的验证

- `dotnet run --project tests/GenerosityOfferingContracts/GenerosityOfferingContracts.csproj`：107条生产源规则/索引断言通过（直接编译生产文件，不复制规则实现）。
- `python scripts/TestDesignSyncGenerosityOffering20260927.py`：5项通过。
- `python scripts/TestDesignSyncGenerosity20260927.py`：8项原拾取相关检查通过。
- `dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`：通过，0警告0错误。
- 没有跑无关全卡审计或全量12门，没有改并行审计输出。

## 游戏脚本与未验项目

沿现有`ms_test_generosity confirm`加入`Debugging/GenerosityOfferingContract.cs`。只用于可丢弃的单人天音非战斗存档；会改路线、牌组与遗物。测试九阶段可用性、无可移除牌、其他角色、普通跳过、三试炼推进、双向重复领取、确定遗物序列化、异步删牌中锁定另一项、真实奖励栈、本地/远端入口、完成后旧消息、宝箱分配后相同遗物/归属以及当前原生IL注入。

上述引擎脚本**仅编译，未运行**。远端入口在单机fixture中调用，不等于双人联机已验证。仍需实测：原生精英战利品界面、单/双人宝箱动画与分配后供奉、鼠标/手柄焦点与无收益隐藏、保存退出重进、真实跨网络重复/延迟消息。状态IMPLEMENTED而非VERIFIED。

九项答复中剩Q14谦逊改写；本轮更大的怪物/事件等未完成项保留在当前状态文档，不缩减总目标。
