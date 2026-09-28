# DS27 批次46：眼罩遭遇预览与本地显示

前置快照`18c3dade`，Plan同步`cab0314b`，完成提交为本文件所在独立范围提交。`RELIC-EVENT-001`→DS27-04H IMPLEMENTED，游戏内待验；不标VERIFIED、不部署。DesignDoc逐行/词级无漂移，完整核对眼罩条目及原生ActModel/RoomSet/Creature/NCreature/NIntent/RelicCmd实现。

## 实际遗漏与修正

1. 眼罩代码只有隐藏意图，正式要求的下一次遭遇没有实现；本地化也缺少整句。补完整文本，独立句分行。
2. 原patch按任意队友拥有眼罩判断，所有客户端都会隐藏意图。改为当前本地响木天音、有效且未熔化的眼罩实际在遗物栏内。远端持有者、其他角色、玩家与友方宠物不受影响。
3. 原实现仅改透明度，透明Control仍可响应鼠标；原版聚焦怪物本体也会列出所有意图。改为隐藏整个Control，分别拦截NIntent.OnHovered与Creature.HoverTips的意图部分，保留全部Power提示及原生去重。
4. 拾取及移除等待原生RefreshIntents；移除后仅恢复本功能记录的原始Visible值，不把其他功能隐藏的节点擅自打开。记录使用ConditionalWeakTable，不持有退出战斗的节点。异常通过Safe或异步catch隔离，不改变怪物行为或结算。

## 遭遇呈现

`src/Core/Relics/BlindfoldPresentation.cs`由眼罩AdditionalHoverTips调用，每次读取拥有者当前RunState.Act。无静态未来缓存，因此跨层和读档不复用旧幕结果。

原版v0.111.0的`ActModel.PullNextEncounter`虽名为Pull，实际只读取`RoomSet.NextNormalEncounter/NextEliteEncounter/NextBossEncounter`；推进计数发生在独立`MarkRoomVisited`。本服务只调用前者，不生成房间、不克隆或生成未来怪物、不调用随机流。

尚未选择路线，无法把普通/精英/Boss三条序列合称确定的下一房间，因此三类分别展示标题。显示遭遇名称与AllPossibleMonsters的去重本地化名称，并明确“可能出现的敌人”；实际随机编成与未来到达楼层有关，不声称所有候选会同时出现。普通战条目标明不预测问号类型；Boss条目读取下一Boss，兼容原生双Boss进度。空队列/未初始化Boss安全降级，canonical Act不读取。

百科原型、未取得遗物的事件展示、远端遗物栏、移除遗物不泄露未来内容。预览不依赖Foresight或其他Mod。

## 自动测试入口

`ms_test_blindfold`：Debug、非网络命令、无参数，要求本地响木天音正在持有眼罩。**只读当前局**，不执行卡牌/遗物/生命命令，不改测试模式或本地NetId。可在战斗外验证预览，战斗内额外检查实际Creature.HoverTips经patch后仅包含Power提示。

- 比对正式全文、本地持有与无持有、远端同角色/本地其他角色、已移除与百科原型；隔离玩家使用新建但不加入当前局的实例。
- 三类提示区别明确，反复读取10次稳定。
- 逐幕复制原生Act存档，在独立副本上读取并推进普通/精英序列至环绕，以及双Boss；结果与原生下一遭遇对象一致。
- 读取前后副本保存内容不变；Act.FromSave重建保留下一遭遇。空普通/精英/Boss及不支持分类返回空，不抛异常。
- 只读取可能敌人不改变HaveMonstersBeenGenerated。
- 最后检查当前局所有Act保存内容、RunRngSet及PlayerRngSet均与测试前一致。

该命令已编译，**本轮未游戏执行**。独立副本重建不代替完整游戏存档加载，也不代替自然跨层测试。8个Python静态测试检查正式文本、生产接线、隔离条件、原生只读入口、隐藏/恢复/悬停边界和脚本作用范围；不声称它们执行了Godot交互。

## 本轮执行验证

| 命令（Mod目录） | 结果 |
|---|---|
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 372通过，新增8 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，合计391静态 |
| `dotnet build MaidenSuccubus.csproj -c Release -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Debug` | 0警告0错误，最终保留Debug |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540生产纯规则断言通过，本批无新增 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，非引擎存读档 |
| MvpContent/StructuralContracts/LocalizationStyle/CardEffectTests（各`-ProjectDir .`） | 四门通过 |

既有失败：VisualAssets第317行诱惑度口红/独立数字；全卡审计`--no-write`的GagCurse费用1/2差异、4未知元数据、215文本待审、2 designOnly、2 retiredCompat。没有改变这些检查或覆盖并行报告。

游戏待验：执行只读命令、鼠标和手柄两类悬停、实际提示框排版、战斗内拾取/移除及时隐藏恢复、保持怪物Power提示、队友持有/自己持有两客户端、自然换幕与完整保存加载、双Boss场景。总目标仍未完成；本批不修改素材或事件规则，不部署、不启动游戏。
