# DS27 批次41：魔力索引与拾取附魔三剑

前置快照`19229c1e`；需求/Plan同步`c323a201`；完成提交为本文件所在独立范围提交。状态IMPLEMENTED，游戏内待验，未部署。DesignDoc逐行与词级无新增漂移，本批不修改设计。

## 范围与发现

- 魔力索引：罕见能力，费用1/0，每抽到一张附魔牌抽1张；叠层按Power数量增加抽牌。
- 魔法之剑：普通攻击，2费，12/18伤害，拾取充能2。
- 疾风之剑：普通攻击，1费，11/14伤害，拾取迅捷2。
- 闪耀之剑：普通攻击，1费，4/6伤害2次，拾取活力3。

原生`CardPileCmd.DrawInternal`先将牌移入手牌，再通知`Hook.AfterCardDrawn`（Early先于普通钩子），随后重新核对手牌容量。索引现有递归方式不会重复抽仍在抽牌堆顶的同一张牌；本批保留顺序，不引入队列、去重集合或异步脱离任务。新增当前Power、存活拥有者、同战斗归属校验，排除失效回调。普通Power说明明确1张，智能说明显示实际Amount。

三剑现有永久拾取路径及卡面完整文本均符合设计，无需改写规则或描述。原浅层探针直接给战斗牌附魔，未覆盖真正拾取，现替换为原生命令回归。

## 可执行游戏回归入口

`ms_test_cards confirm ds27-enchantment-input`接入现有卡效目录，限定上述4牌的基础/升级变体。沿用单人、角色和confirm门禁，**只应在可丢弃测试战斗运行**，会清理战斗卡牌、状态和遗物等。当前仅编译，未在游戏执行。

`DesignSyncEnchantmentInputContract`覆盖：

- 四牌精确费用、稀有度、类型、永久牌预览和战斗预览全文，包含标点与换行。
- 索引实际打出及叠层；连续2张附魔牌的递归链；普通抽牌、直接生成入手不触发；同实例再次抽到重新触发；多层附魔只算一张；9/10手牌边界；禁止抽牌及允许回合初抽牌但禁止索引奖励抽牌；弃牌堆洗牌后为空；移除Power、他人牌、非战斗牌回调排除。每变体最低25条效果断言门禁。
- 三剑真正加入永久牌组取得附魔，重复拾取通知不叠加；克隆战斗牌并关联DeckVersion；首次与第二次实际出牌，分别核对伤害、充能、迅捷、附魔耗尽及永久版本保持完整。每变体最低17条效果断言门禁；临时永久牌用finally清理。

以上为脚本覆盖范围，不是已通过游戏验收的结果。

## 离线验证

全部命令在Mod目录运行。

| 命令 | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Release` | 0警告0错误，最终保留Debug构建 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 326通过，本批新增7 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，合计345静态检查 |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540生产规则断言通过，本批未增加此类断言 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，不代表游戏完整存读档 |
| `ValidateMvpContent.ps1 / ValidateStructuralContracts.ps1 / ValidateLocalizationStyle.ps1 / ValidateCardEffectTests.ps1`（分别`-ProjectDir .`） | 四项通过；225卡、32遗物、10附魔；223可执行卡效场景、2设计待定 |

既有失败仍在：`ValidateVisualAssets.ps1`第317行诱惑度口红及独立数字契约；`AuditCardLocalization.py --no-write`报告GagCurse费用预期1而源码2、4未知元数据、215文本待审、2 designOnly、2 retiredCompat。未重写其他Agent的审计JSON。

仍待游戏脚本实际执行、自然抽牌与附魔动画、完整存读档、多人场景和悬停截图。不部署，不标VERIFIED；本批不表示全部DesignDoc同步目标已完成，Q18等待定设计继续保持OPEN。
