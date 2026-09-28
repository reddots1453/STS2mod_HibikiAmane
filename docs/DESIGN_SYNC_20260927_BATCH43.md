# DS27 批次43：加速运动永久复制

前置快照`3441ef47`，Plan同步`a543f051`，完成提交为本文件所在独立范围提交。IMPLEMENTED，游戏内待验；未部署。DesignDoc逐行/词级无新增漂移。

## 偏差与修正

完整正式条目：稀有技能，0费，抽2/3张牌，消耗；拾起时向永久牌组加入1复制，不在战斗内生成或打出时触发。

原实现用`CreateCard<AcceleratedMotion>`新建规范牌，只手动继承升级，不保留原牌已有附魔等状态。改为`Owner.RunState.CloneCard(this)`；原生ICardScope明确永久复制用Run作用域，战斗复制才用CreateClone。原生CardModel.DeepCloneFields独立复制动态变量、费用、局部关键字、附魔等，AfterCloned重设运行时引用；不自己拼装复制状态。

保留原有`AddedPickupCopies`保存字段；原牌在克隆前设为true，复制也在加入Deck之前设为true。原牌/复制都不会因再次通知递归增殖；None→Deck与card==this门禁不变。没有修改费用、稀有度、抽牌量、升级收益、复制数量或正式文本，不追溯补偿旧存档已经生成的缺失附魔复制。

## 自动测试脚本

`ms_test_cards confirm AcceleratedMotion`接入原卡效目录，用`DesignSyncAcceleratedMotionContract`替换原来的单项抽牌探针。基础/升级每场至少41条效果断言。**只可在可丢弃测试战斗运行**；沿用单人、角色、confirm门禁，会清理战斗牌/状态/遗物。本轮已编译，未在游戏执行。

- 精确费用、稀有度、技能/自身目标、消耗关键字；普通/升级完整永久预览与战斗预览，包含换行与原生附加的消耗说明。
- 实际战斗生成与打出：永久牌组不增加，未写拾取标记，抽2/3，出牌后进入消耗堆。
- 原生永久入牌：普通、迅捷4并附加保留的两条路径，原牌加1复制，没有复制链。
- 升级与抽牌变量保留；附魔类型/数量/状态保留且独立绑定复制；改变复制的关键字不影响原牌。
- 原牌与复制重放拾取通知均不再增牌。
- 两实例各经过ToSerializable→RunState.LoadCard，验证保存标记、升级、附魔；再加入Deck只加该加载实例，不再发复制。
- 永久复制克隆进战斗不再增永久牌组；finally仅清理当前测试新增的加速运动牌。

模型保存加载不是完整保存退出/重启游戏测试；不把脚本编译或静态检查当作真实引擎验收。

## 离线验证

| 命令（Mod目录） | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Release` | 0警告0错误，最终保留Debug构建 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 340通过，新增7 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，合计359静态检查 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540通过，本批未增加纯规则断言 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，非游戏流程 |
| 四内容门MvpContent/StructuralContracts/LocalizationStyle/CardEffectTests（各`-ProjectDir .`） | 通过；225卡、32遗物、10附魔，223可执行卡效与2设计待定 |

仍失败：视觉门第317行诱惑度口红及独立数字；全卡审计GagCurse费用1/2差异、4未知元数据、215文本待审、2 designOnly、2 retiredCompat。审计只用`--no-write`，未修改并行报告。

待游戏内验收：定向脚本实际运行、自然奖励/商店拾取、附魔复制预览、保存退出再读档、多人。总目标尚未完成，状态不标VERIFIED，不部署。
