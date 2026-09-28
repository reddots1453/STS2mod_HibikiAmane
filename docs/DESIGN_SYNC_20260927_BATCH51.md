# DS27 批次51：亡灵集会选项与异步选择测试

前置`a28fb31b`，Plan同步`39c1967c`，完成提交为本文件所在独立范围提交。DS27-06D IMPLEMENTED，游戏内待验，不标VERIFIED。DesignDoc逐行/词级无漂移，完整复核EVENT-NEW-003及当前生命/具体整数说明。本批不修改事件玩法、本地化或素材，不部署。

## 实现入口

扩充`src/ConsoleCommands/DesignEventTestConsoleCmd.cs`，保留已有三分支测试；新增`DesignUndeadEventContract.Run`。入口仍为`ms_test_events confirm`，仅本角色、单人、非战斗且显式confirm允许执行；异步任务重入拒绝。运行会移除遗物、改写牌组/生命/堕落，**只能在可丢弃专用局中使用，未在本轮执行**。

命令范围内启用原生TestMode并在finally恢复旧值；CardSelectCmd.UseSelector作用域退出恢复选择器。异步测试使用原生TestCardSelector的待答复TaskCompletionSource，在finally完成选择并等待原操作，不留下故意悬置的选择任务。

## 覆盖内容

- 原始初始叙事逐字比较，保留段落空行、blue和sine标签；三条结果页逐字比较，不去掉标点或换行。
- 生命上限1/9/10/19/59/60/97/100/199对应整数费用0/0/1/1/5/6/9/10/19。直接检查生产PrayerHpLoss及真实选项格式化显示；原三组实际支付测试保留。
- 堕落-5～+5全部门槛、锁定标题/说明及调用锁定选项不改变状态；空牌组、全部已有死灵附魔的牌组显示无候选锁定。
- 0/1/2候选时不注入测试选择结果，走原版FromDeckForTransformation不足数量时自动采用全部合法牌的路径；4张时只选第二和第四，保留其余实例。
- 4张场景中故意等待选择答复；对同一个EventOption重复Chosen不得抢先变化或重复支付，答复后检查只支付一次。
- 使用独立RNG副本预测每次变化牌ID，比较实际新增顺序和完整事件随机状态；同时检查Niche和普通奖励RNG未变化。候选要求已解锁、正式圣洁常见/罕见/稀有牌，不接受基础/先古/衍生牌。
- 祈祷97最大生命扣当前生命9、最大生命不变；堕落-4→-5；牌数不变，变化数量为min(2,合法候选数)，只替换选中牌。
- 对话只加入两张各自独立的原版Soul，不损生命或改堕落。死灵选项只附魔指定合法牌，保留已有附魔和未选牌，只加一张Normality，堕落5处不超上限。
- 三分支完成后检查IsFinished、清空初始选项、精确结果页；重复同选项不会再次收费、随机或发奖。

7项Python静态测试逐段比对DesignDoc与生产本地化、独立C#叙事期望，检查守卫/真实命令/作用域/RNG接线。文本适配仅保留先前已确认的统一“附魔：死灵”格式、资源公式变量以及颜色标签；不忽略句号/逗号。

## 已执行验证

| 命令（Mod目录） | 结果 |
|---|---|
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 409通过，本批新增7 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，累计428静态 |
| `dotnet build MaidenSuccubus.csproj -c Release -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Debug` | 0警告0错误，最终保留Debug |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540生产纯规则断言通过 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，非引擎执行 |
| MvpContent/StructuralContracts/LocalizationStyle/CardEffectTests（各`-ProjectDir .`） | 四门通过；升级绑定225注册/149直接/6间接/0失败 |

既有失败仍在：VisualAssets第317行诱惑度图标/独立数字；全卡审计`--no-write`的GagCurse费用1/2、4未知元数据、215文本待审、2 designOnly及2 retiredCompat。没有改写并行审计报告。

**未验证范围：**上述游戏内脚本仅编译；自然事件抽取和实际出现在第二/三层、真实选择弹窗/视觉、自然保存退出和读档、死亡导致的整局退出、网络同步、其他角色/混合队伍的自然事件池隔离。作用域代码的静态检查不冒充这些实机结果。测试不模拟取消合法的必选步骤，也不擅自给规则添加“跳过祈祷”行为。未启动游戏、未部署DLL、未修改并行文件；总目标未完成。
