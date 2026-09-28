# DS27 批次42：唤雷、终极耀斑、瞬闪刺

变更前快照`5d27cd1d`，Plan同步`dbacae2f`，完成提交为本文件所在独立范围提交。IMPLEMENTED，游戏内待验；未部署。

## 设计与源码核对

DesignDoc逐行、词级无新增漂移。三牌完整正式条目及运行时源码、本地化逐一核对：

- 唤雷：普通攻击，1费7/9伤害；斩杀时和魔力解放各产生一次最低生命目标攻击，后续斩杀继续连锁。现有pending计数保留两个来源，初击和后续均遵循原生ShouldOwnerDeathTriggerFatal。
- 终极耀斑：罕见攻击，4费全体40/52伤害；手牌回合末战斗内减费1，升级不改费用。现有AddThisCombat正确；不能错误加上回调时必须仍在Hand的条件，因为原生CombatManager先移至Play再调用。
- 瞬闪刺：罕见攻击，0费5/7伤害，复制入抽牌堆。现有CreateClone及PreviewCardPileAdd沿用原版Anger模式，只将目的堆改为随机抽牌堆。

生产规则、数值与三牌完整描述已符合，本批无需修改；不为制造实现差异而重写正确路径。

参考只读原版源码：CardModel.CreateClone、CardEnergyCost、CombatManager.DoTurnEnd/ResolveTurnEndCardEffects、Anger、MinionPower。复制重设ExhaustOnNextPlay，附魔与费用模型独立克隆；回合末效果先于弃牌。

## 测试脚本

`DesignSyncChainCopyContract`接入原卡效目录和运行器；定向命令：

`ms_test_cards confirm ds27-chain-copy`

命令沿用单人、角色与confirm门禁，**只能在可丢弃测试战斗运行**，会清理战斗牌、状态和遗物。本轮只编译，未实际执行。

共同检查：基础/升级费用、稀有度、类型、目标；永久牌预览和战斗预览完整文本，保持标点、换行。

唤雷每变体最低33条效果断言：七场景分别为普通不触发、只解放、拒绝解放、三敌连续斩杀、连续斩杀且同时解放、初击仆从、后续命中仆从。实际创建敌人、设置明确生命值、执行卡牌，验证指定目标伤害、最低生命幸存者收到的待结算攻击总数、连锁死亡、无关敌人不受伤及耐久付款。临时存活敌人在finally中退场，保留原敌人避免误结束战斗。

耀斑每变体最低25条效果断言：实际永久牌生成战斗副本，循环5次Play→原生OnTurnEndInHandWrapper→Discard并执行费用回合清理，检查3/2/1/0/0费用；永久牌和未收到手牌回调的抽牌堆实例仍4费；实际打出验证全体40/52伤害；出牌不清除战斗减费，从永久实例生成的新战斗副本仍4费并保留升级。**这是卡牌生命周期验证，不是自动结束整场自然玩家回合**。

瞬闪刺每变体最低17条效果断言：锋利2和力量3下实际打出；复制保留升级、伤害、费用、拥有者及战斗归属，附魔与费用不别名共享；原牌一次性消耗标志不继承到复制；复制打出后再复制且记录正确CloneOf，不增加永久牌组。预览动画命令保留静态契约，视觉时序仍待手测。

既有ValidateCardEffectTests曾依赖移出旧探针的四条文字，已改为检查新目录委托、真实出牌命令、伤害/付款断言及两种仆从情形；不删除原斩杀门禁，也不以存在方法名替代效果检查。

## 验证

| 命令（Mod目录） | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Release` | 0警告0错误，最终保留Debug构建 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 333通过，本批新增7 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，合计352静态检查 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540断言通过，本批未增加纯逻辑断言 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，非完整存读档 |
| 四内容门：MvpContent / StructuralContracts / LocalizationStyle / CardEffectTests（`-ProjectDir .`） | 通过；225卡、32遗物、10附魔；223可执行卡效，2设计待定 |

未解决的既有失败：VisualAssets第317行诱惑度口红与独立数字契约；全卡审计GagCurse费用设计1/源码2，4未知元数据、215文本待审、2 designOnly、2 retiredCompat。审计使用`--no-write`，未覆盖并行报告。

未完成的游戏内验收：运行本定向脚本、自然回合末顺序、连续斩杀视觉、随机洗入预览、完整存读档、多人归属。编译和静态通过不代表这些项目通过；本批不标VERIFIED，总目标未完成。
