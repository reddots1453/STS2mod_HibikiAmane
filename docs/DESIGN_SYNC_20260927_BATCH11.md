# DS27-02F：黑暗风暴升级附魔

实施日期2026-09-28。前置快照`a1f1e3d9`，DesignDoc逐行/词级无未同步修改；重读力量输出牌、临时附魔和光之翼条目。代码IMPLEMENTED，游戏内未验收，未部署。

## 当前需求与实现

黑暗风暴为罕见攻击、2费、全体敌人8伤害并给予2易伤；基础与升级数值相同，升级收益是附魔原版华彩。旧实现为拾取附魔，基础1易伤，升级10伤害/2易伤，卡面也写拾取。现已同步。

- `src/Cards/MvpStrengthCards.cs`：取消该牌AfterCardChangedPiles附魔钩子。OnUpgrade仅对当前无附魔实例调用原版Glam/EnchantInternal/ModifyCard；不用CardCmd.Enchant写历史，不访问Owner，不触碰DeckVersion，也不在预览或读档中启动动画。
- 普通单附魔槽已被占用时不覆盖；已有华彩不重新创建、不重置当前战斗使用状态。这并未给黑暗风暴增加多重附魔能力。光之翼是另一个明确允许多重附魔的需求，不能据此放开所有牌。
- 保留`EnchantedOnPickup`旧SavedProperty以兼容已有存档，但不再读它触发拾取。旧存档原有附魔不擅自清除；新基础牌没有拾取赠送。无拥有者的升级预览、生成升级牌、原版FromSerializable先还原附魔再升级均使用同一实现。
- 普通升级命令继续负责原版升级展示，避免OnUpgrade在预览/加载时误播附魔VFX；实际动画与附魔图标仍需手测。
- `localization/zhs/cards.json`：把拾取句改为“升级时，为这张牌附魔：华彩。”，沿用富文本颜色及句间换行。数值动态变量固定8和2；原版华彩悬停不改写。

## 自动测试范围

`src/Debugging/CardEffects/DesignSyncDarkStormContract.cs`接入现有CardEffect目录的基础/升级两场景，替换旧数值断言。入口：`ms_test_cards confirm DarkStorm`，只在一次性单人本角色战斗中使用；会重置战斗、生命、卡牌、遗物和资源，不用于正式存档。入口已编译，本轮未在游戏中执行。

- 每场景至少26条行为断言，以及9条元数据/格式化文本断言；多个敌人时增加实际AoE断言。
- 真正AutoPlay原版命令。升级首次为8伤害→2易伤→12伤害→再2易伤，共20伤害/4易伤；后续不再重放，单次12伤害/加2易伤。未升级首次8/2、后续12/再2。
- 真实Glam重放次数及首次消耗，战斗克隆保持使用状态但附魔实例不共享，原版序列化和加载不重复加华彩。
- 原版无Owner升级预览不改canonical；真实牌组拾取不赠附魔，战斗升级不改DeckVersion，重复Upgrade命令无重复附魔，牌组升级及读取确实保存华彩。
- 现有Sharp不被覆盖；旧EnchantedOnPickup与未升级Glam保存后仍保留，随后升级不重复叠加。

静态`TestDesignSyncDarkStorm20260927.py`有8项，分别验证固定元数据、升级入口、移除拾取、预览安全边界、与DesignDoc的精确文案（句间显示换行单独处理）、目录改用新场景、真实重放和持久化测试接线。静态断言不等同于已经执行了游戏效果。

## 已执行结果

Mod目录内：

- `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore`：0警告0错误。初次测试代码缺两个ToMutable显式转换，补齐后复跑成功。
- `python -m unittest discover -s scripts -p 'Test*20260927.py'`：75项通过。
- `python scripts/TestCardLocalizationAudit.py`：19项通过，合计94项。
- `dotnet run --project tests/DesignSyncContracts/DesignSyncContracts.csproj --no-restore`：既有290条生产规则断言通过；本批不冒称增加了可离线执行的游戏引擎测试。
- `ValidateCardEffectTests.ps1`、`ValidateMvpContent.ps1`、`ValidateLocalizationStyle.ps1`、`ValidateStructuralContracts.ps1`，参数`-ProjectDir .`：通过。结构门最初仍断言DarkStorm拾取附魔，已替换为升级入口、禁止拾取和固定数值三类断言，不保留过期规则。
- `ValidateVisualAssets.ps1 -ProjectDir .`：既有第317行诱惑度图标断言仍失败，未放宽。
- `python scripts/AuditCardLocalization.py --no-write`：225模型，仍4个明确差异（GagCurse费用、LightWings稀有度、两退役模型映射）、4个元数据待解析、217个逐字渲染待验、2个仅设计条目。不写共享审计报告，不把本批通过当全卡验收。

## 未完成与下一步

游戏内自然火堆/战斗升级、预览动画/附魔图标、真实多人以及存档往返均未手测。普通受控牌投影与OnUpgrade抑制边界也需回归。

光之翼仍未实现多重附魔：当前原版CardModel仅有一个Enchantment槽，原版CanEnchant/CardCmd也限制单槽；没有检出RitsuLib提供直接多重附魔API。后续必须覆盖同名叠层与异名共存、所有附魔回调/数值、复制/存档、选择过滤与可读显示；不能仅修改9/12和稀有度，或仅增加一个数据列表却不执行每个附魔，就称其完成。原版Glam的次数来自Times而非Amount，也不能简单增加Amount来假称它叠层生效。

剩余全卡、遗物、事件、第四层和其他同步任务仍保留原goal范围，本批无部署授权。并行素材和其他Agent修改未纳入提交。
