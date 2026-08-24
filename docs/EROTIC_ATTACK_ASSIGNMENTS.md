# 原版怪物色情攻击分配表

> 需求ID：`MON-ERO-CATALOG-001`
>
> 成熟度：`OPEN`
>
> 规则权威：色情攻击、拘束、侵犯和魔装概率以[`../DesignDoc.md`](../DesignDoc.md)为准；本文只负责原版怪物的适配分配。
>
> 逐怪物意图名称与完整效果：[`EROTIC_ATTACK_INTENTS.md`](EROTIC_ATTACK_INTENTS.md)

## 1. 文档用途

本表为原版怪物分配允许使用的色情攻击类型，使怪物保留原有外形、动作和战斗风格，而不是统一套用同一种色情意图。

分配代码：

- `A`（欲望攻击）：独立意图；可以只增加1点或更多欲望，也可以造成较低的攻击伤害并增加欲望；
- `B`（拘束）：可以主动施加拘束；
- `I`（侵犯）：玩家已被拘束时，可以使用侵犯意图；
- `S`（意志坚定）：没有色情攻击，跳过色情攻击判定，并免疫将其意图改为色情攻击的效果；
- `—`：不允许该类型。

“允许类型”定义候选集合，“首选”用于确定候选权重；具体算法见2.1.1节。`I`在玩家未被拘束时始终不是合法候选。每个怪物ID必须至少具有`A/B/I`之一，或者单独具有`S`；`S`不能与`A/B/I`并存。

## 2. 匹配原则

1. 具有法术、孢子、毒液、凝视、诱导、气味或其他匹配表现的怪物可以获得`A`。欲望攻击是独立意图，不得直接继承原意图的全部效果；其数值应设计为纯增加欲望，或较低伤害加欲望。
2. 具有藤蔓、丝线、黏液、触肢、巨口、夹钳、长尾、绳索、机械抓取或压制动作的怪物可以获得`B`。
3. 具有生物性、类人生物性、寄生性、吞噬性或明确侵入表现的怪物可以获得`I`；纯粹武器、炮台、雕像和一般构装体默认不获得`I`。
4. 不适合色情表现的纯机械、静物、门、蛋、肢体分段或附属物使用`S`。意志坚定显示在怪物状态栏中，并使该对象不参与色情攻击判定。
5. Boss可以拥有更完整的候选集合，但具体数值、诅咒和重复次数仍需单独设计，不能因Boss身份自动提高概率。

## 2.1 每场战斗自然使用次数上限（初稿）

次数按“每个敌人实例的每种色情意图”分别计算。`A/B/I`依次表示欲望攻击、拘束、侵犯的自然使用次数上限；`—`表示该怪物没有这种意图。由玩家卡牌或其他玩家效果强制改变出的色情意图不消耗次数。多个ID写在同一行时，每个敌人实例分别使用该行上限，不共享计数。意志坚定对象没有色情意图，因此不进入本表。

| 幕 | 遭遇/对象 | 怪物ID | A | B | I |
|---|---|---|---:|---:|---:|
| 密林 | 红宝石劫掠者 | `ASSASSIN_RUBY_RAIDER`、`AXE_RUBY_RAIDER`、`BRUTE_RUBY_RAIDER`、`CROSSBOW_RUBY_RAIDER`、`TRACKER_RUBY_RAIDER` | 2 | — | 1 |
| 密林 | 多尼斯异鸟 | `BYRDONIS` | 3 | 2 | — |
| 密林 | 仪式兽 | `CEREMONIAL_BEAST` | 4 | 3 | 2 |
| 密林 | 密林真菌 | `FLYCONID` | 2 | — | — |
| 密林 | 雾菇主体 | `FOGMOG` | 2 | 1 | — |
| 密林 | 毛绒伏地虫 | `FUZZY_WURM_CRAWLER` | 2 | 2 | 1 |
| 密林 | 墨宝 | `INKLET` | 2 | 1 | — |
| 密林 | 蛮兽 | `MAWLER` | 2 | 1 | 1 |
| 密林 | 小啃兽 | `NIBBIT` | 1 | — | 1 |
| 密林 | 缩小甲虫 | `SHRINKER_BEETLE` | 2 | 1 | — |
| 密林 | 异蛙寄生虫 | `PHROG_PARASITE` | 3 | 2 | 2 |
| 密林 | 寄生虫附属体 | `WRIGGLER` | 2 | — | 2 |
| 密林 | 密林史莱姆 | `LEAF_SLIME_M`、`LEAF_SLIME_S`、`TWIG_SLIME_M`、`TWIG_SLIME_S` | 1 | 1 | 1 |
| 密林 | 巨口果 | `SNAPPING_JAXFRUIT` | 2 | 2 | 1 |
| 密林 | 扼杀者 | `SLITHERING_STRANGLER` | 2 | 2 | 1 |
| 密林 | 藤蔓蹒跚者 | `VINE_SHAMBLER` | 2 | 2 | 1 |
| 密林 | 同族小队 | `KIN_FOLLOWER`、`KIN_PRIEST` | 4 | 3 | 2 |
| 密林 | 墨影幻灵 | `VANTOM` | 4 | 3 | 2 |
| 巢穴 | 盛碗虫成体 | `BOWLBUG_NECTAR`、`BOWLBUG_ROCK`、`BOWLBUG_SILK` | 2 | 2 | 1 |
| 巢穴 | 自动机械咬合体 | `CHOMPER` | 2 | 2 | — |
| 巢穴 | 帝皇蟹双组件 | `CRUSHER`、`ROCKET` | 4 | 3 | 2 |
| 巢穴 | 残杀千足虫前段 | `DECIMILLIPEDE_SEGMENT_FRONT` | 3 | 2 | 2 |
| 巢穴 | 蜂群术士 | `ENTOMANCER` | 3 | 2 | 2 |
| 巢穴 | 外骨骼虫 | `EXOSKELETON` | 2 | — | 1 |
| 巢穴 | 猎人杀手 | `HUNTER_KILLER` | 2 | 2 | 1 |
| 巢穴 | 感染棱柱 | `INFESTED_PRISM` | 3 | 2 | — |
| 巢穴 | 知识恶魔 | `KNOWLEDGE_DEMON` | 4 | 3 | 2 |
| 巢穴 | 虱虫之祖 | `LOUSE_PROGENITOR` | 2 | 1 | 2 |
| 巢穴 | 异螨群 | `MYTE` | 1 | — | 1 |
| 巢穴 | 直飞产卵虫 | `OVICOPTER` | 2 | 2 | 3 |
| 巢穴 | 棘刺蟾蜍 | `SPINY_TOAD` | 2 | — | 1 |
| 巢穴 | 熟睡甲虫 | `SLUMBERING_BEETLE` | 2 | 1 | 1 |
| 巢穴 | 胧光怪 | `THE_OBSCURA` | 2 | 1 | 1 |
| 巢穴 | 偷窃草蜢 | `THIEVING_HOPPER` | 1 | — | 1 |
| 巢穴 | 地道虫 | `TUNNELER` | 2 | 2 | 1 |
| 巢穴 | 无厌沙虫 | `THE_INSATIABLE` | 4 | 3 | 3 |
| 荣耀城 | 虔诚雕刻师 | `DEVOTED_SCULPTOR` | 2 | — | 1 |
| 荣耀城 | 门扉缔造者 | `DOORMAKER` | 4 | 3 | 2 |
| 荣耀城 | 骑士团伙 | `FLAIL_KNIGHT`、`MAGI_KNIGHT`、`SPECTRAL_KNIGHT` | 3 | 2 | 2 |
| 荣耀城 | 青蛙骑士 | `FROG_KNIGHT` | 2 | 2 | 1 |
| 荣耀城 | 活体盾牌 | `LIVING_SHIELD` | 2 | 2 | — |
| 荣耀城 | 猫头鹰法官 | `OWL_MAGISTRATE` | 2 | — | 1 |
| 荣耀城 | 咬人卷轴 | `SCROLL_OF_BITING` | 2 | 2 | — |
| 荣耀城 | 史莱姆狂战士 | `SLIMED_BERSERKER` | 2 | 2 | 1 |
| 荣耀城 | 灵魂枢纽 | `SOUL_NEXUS` | 3 | 2 | — |
| 荣耀城 | 女王与火炬头聚合体 | `QUEEN`、`TORCH_HEAD_AMALGAM` | 4 | 3 | 2 |
| 荣耀城 | 实验体 | `TEST_SUBJECT` | 4 | 3 | 3 |
| 荣耀城 | 失落与遗忘之物 | `THE_LOST`、`THE_FORGOTTEN` | 2 | 1 | 1 |
| 荣耀城 | 高塔炮手 | `TURRET_OPERATOR` | 2 | — | 1 |
| 暗港 | 邪教徒 | `CALCIFIED_CULTIST`、`DAMP_CULTIST` | 2 | 1 | 1 |
| 暗港 | 噬尸蛞蝓 | `CORPSE_SLUG` | 2 | 2 | 2 |
| 暗港 | 化石追踪者 | `FOSSIL_STALKER` | 2 | 1 | 1 |
| 暗港 | 邪恶气体主体 | `LIVING_FOG` | 3 | 2 | 2 |
| 暗港 | 地精佣兵 | `GREMLIN_MERC`、`SNEAKY_GREMLIN`、`FAT_GREMLIN` | 1 | 1 | 1 |
| 暗港 | 幽灵船 | `HAUNTED_SHIP` | 2 | 2 | — |
| 暗港 | 乐加维林族母 | `LAGAVULIN_MATRIARCH` | 4 | 3 | 2 |
| 暗港 | 花园幽灵鳗 | `PHANTASMAL_GARDENER` | 3 | 2 | 2 |
| 暗港 | 海洋混混 | `SEAPUNK` | 1 | — | 1 |
| 暗港 | 下水道蚌 | `SEWER_CLAM` | 2 | 2 | 1 |
| 暗港 | 鬼祟珊瑚群 | `SKULKING_COLONY` | 3 | 2 | 2 |
| 暗港 | 淤泥旋螺 | `SLUDGE_SPINNER` | 2 | 2 | 1 |
| 暗港 | 灵魂异鱼 | `SOUL_FYSH` | 4 | 3 | 3 |
| 暗港 | 骇鳗 | `TERROR_EEL` | 3 | 2 | 2 |
| 暗港 | 蟾蜍蝌蚪 | `TOADPOLE` | 1 | — | 1 |
| 暗港 | 双尾鼠 | `TWO_TAILED_RAT` | 2 | 2 | 1 |
| 暗港 | 瀑布巨人 | `WATERFALL_GIANT` | 4 | 3 | 2 |

分配采用以下初始梯度，供后续运行时测试微调：普通弱小敌人通常为`A1/B1/I1`，普通强敌通常为`A2/B1～2/I1`，精英通常为`A3/B2/I1～2`，Boss通常为`A4/B3/I2～3`。产卵、寄生、吞噬等以侵犯为核心风味的敌人可以提高`I`，但不得超过4。

### 2.1.1 色情意图候选权重

怪物通过DesignDoc规定的色情攻击概率判定后，才执行本节的类型选择。候选类型必须同时满足：已列入该怪物的“允许类型”、当前状态下合法、对应自然使用次数尚未耗尽，以及未被其他规则禁止。

- 逐怪物表中的“首选”类型权重为`3`，其他允许类型权重各为`1`；
- 两种类型均合法时，若其中一种是首选，概率为`75%/25%`；
- `A/B/I`三种类型均合法时，首选概率为`60%`，另外两种各为`20%`；
- 先删除不合法候选，再按剩余权重重新归一化。首选不合法时，其余候选均为非首选权重`1`，因此等概率选择；
- 只剩一种合法类型时直接选择，不额外消耗类型选择RNG；没有合法类型时保持原意图，不重新进行色情攻击概率判定；
- 存在至少两种合法类型时，只消耗一次类型选择RNG，并按固定顺序`A→B→I`划分累计权重区间；
- 玩家卡牌或其他效果需要从多个当前合法色情意图中随机选择时沿用本节权重。强制改变意图不消耗自然使用次数的规则保持不变。

示例：某怪物允许`A/B/I`且首选`B`。玩家未被拘束时，`I`不合法，因此在`A/B`中按`25%/75%`选择；玩家被拘束且三种类型均有剩余次数时，按`A20%/B60%/I20%`选择；若`B`次数耗尽，则在`A/I`中各按`50%`选择。

## 2.2 挣脱后的恢复意图 `[SYS-CTL-002]`

本节只分类已被分配拘束意图`B`的原版怪物。没有拘束意图的怪物和意志坚定对象不会成为拘束来源，因此不属于本节的弱怪或强大怪物。分类是固定设计数据，不在运行时按生命值、当前阶段或遭遇房间重新推断。

玩家将某个来源怪物施加的拘束降至0时：

- 弱怪立即将当前意图替换为晕眩意图；
- 强大怪物立即将当前意图替换为下表指定的唯一原版恢复意图；
- 替换只影响下一次行动。该行动结束后重新进入原版状态机，不补做此前被色情意图或恢复意图覆盖的原版行动；
- 恢复意图属于原版意图，不消耗该怪物任何色情攻击次数。

### 弱怪：挣脱后进入晕眩

| 幕 | 怪物ID |
|---|---|
| 密林 | `FOGMOG`、`FUZZY_WURM_CRAWLER`、`INKLET`、`MAWLER`、`SHRINKER_BEETLE`、`LEAF_SLIME_M`、`LEAF_SLIME_S`、`TWIG_SLIME_M`、`TWIG_SLIME_S`、`SNAPPING_JAXFRUIT`、`SLITHERING_STRANGLER`、`VINE_SHAMBLER`、`KIN_FOLLOWER` |
| 巢穴 | `BOWLBUG_NECTAR`、`BOWLBUG_ROCK`、`BOWLBUG_SILK`、`CHOMPER`、`HUNTER_KILLER`、`LOUSE_PROGENITOR`、`OVICOPTER`、`SLUMBERING_BEETLE`、`THE_OBSCURA`、`TUNNELER` |
| 荣耀城 | `FROG_KNIGHT`、`LIVING_SHIELD`、`SCROLL_OF_BITING`、`SLIMED_BERSERKER`、`THE_LOST`、`THE_FORGOTTEN` |
| 暗港 | `CALCIFIED_CULTIST`、`DAMP_CULTIST`、`CORPSE_SLUG`、`FOSSIL_STALKER`、`LIVING_FOG`、`GREMLIN_MERC`、`SNEAKY_GREMLIN`、`FAT_GREMLIN`、`HAUNTED_SHIP`、`SEWER_CLAM`、`SLUDGE_SPINNER`、`TWO_TAILED_RAT` |

`KIN_FOLLOWER`虽然出现在Boss战中，但本身是低生命值随从，明确按弱怪处理。

### 强大怪物：挣脱后进入指定原版意图

| 幕 | 怪物ID | 指定恢复意图ID | 原版意图效果摘要 |
|---|---|---|---|
| 密林 | `BYRDONIS` | `PECK_MOVE` | 造成3点伤害3次。 |
| 密林 | `CEREMONIAL_BEAST` | `STOMP_MOVE` | 造成一次原版“践踏”伤害。 |
| 密林 | `PHROG_PARASITE` | `LASH_MOVE` | 造成4点伤害4次。 |
| 密林 | `KIN_PRIEST` | `BEAM_MOVE` | 造成3点伤害3次。 |
| 密林 | `VANTOM` | `INK_BLOT_MOVE` | 造成7点伤害。 |
| 巢穴 | `CRUSHER` | `ENLARGING_STRIKE_MOVE` | 造成4点伤害。 |
| 巢穴 | `ROCKET` | `TARGETING_RETICLE_MOVE` | 造成3点伤害。 |
| 巢穴 | `DECIMILLIPEDE_SEGMENT_FRONT` | `BULK_MOVE` | 造成一次较低伤害并结算该意图原有增益。 |
| 巢穴 | `ENTOMANCER` | `SPEAR_MOVE` | 造成一次原版“长矛”伤害。 |
| 巢穴 | `INFESTED_PRISM` | `RADIATE_MOVE` | 造成一次较低伤害并获得格挡。 |
| 巢穴 | `KNOWLEDGE_DEMON` | `SLAP_MOVE` | 造成一次原版“拍击”伤害。 |
| 巢穴 | `THE_INSATIABLE` | `THRASH_MOVE_1` | 造成8点伤害2次。 |
| 荣耀城 | `DOORMAKER` | `WHAT_IS_IT_MOVE` | 进入其原版晕眩行动。 |
| 荣耀城 | `FLAIL_KNIGHT` | `RAM_MOVE` | 造成15点伤害。 |
| 荣耀城 | `MAGI_KNIGHT` | `RAM_MOVE` | 造成10点伤害。 |
| 荣耀城 | `SPECTRAL_KNIGHT` | `SOUL_FLAME` | 造成3点伤害3次。 |
| 荣耀城 | `SOUL_NEXUS` | `DRAIN_LIFE_MOVE` | 造成18点伤害并结算该意图原有减益。 |
| 荣耀城 | `QUEEN` | `OFF_WITH_YOUR_HEAD_MOVE` | 造成原版低伤害多段攻击。 |
| 荣耀城 | `TORCH_HEAD_AMALGAM` | `TACKLE_3_MOVE` | 造成14点伤害。 |
| 荣耀城 | `TEST_SUBJECT` | `SKULL_BASH_MOVE` | 造成一次较低伤害并结算该意图原有减益。 |
| 暗港 | `LAGAVULIN_MATRIARCH` | `SLASH2_MOVE` | 造成12点伤害并获得格挡。 |
| 暗港 | `PHANTASMAL_GARDENER` | `FLAIL_MOVE` | 造成1点伤害3次。 |
| 暗港 | `SKULKING_COLONY` | `INERTIA_MOVE` | 获得格挡并结算该意图原有增益，不造成伤害。 |
| 暗港 | `SOUL_FYSH` | `GAZE_MOVE` | 造成7点伤害并放入1张原版状态牌。 |
| 暗港 | `TERROR_EEL` | `ThrashMove` | 造成3点伤害3次并结算该意图原有增益。 |
| 暗港 | `WATERFALL_GIANT` | `RAM_MOVE` | 造成一次较低伤害并结算该意图原有增益。 |

表中的伤害仅用于说明所选意图为何属于该怪物的低威胁行动；实际数值、进阶难度变化及意图原有效果全部沿用原版对应Move，不在本Mod中重写。

## 3. 密林（OVERGROWTH）

| 遭遇/对象 | 怪物ID | 允许类型 | 首选 | 匹配依据 |
|---|---|---|---|---|
| 红宝石劫掠者 | `ASSASSIN_RUBY_RAIDER`、`AXE_RUBY_RAIDER`、`BRUTE_RUBY_RAIDER`、`CROSSBOW_RUBY_RAIDER`、`TRACKER_RUBY_RAIDER` | A、I | A | 类人敌人适合设计低伤害欲望攻击；受拘束后可参与侵犯，但不凭空增加统一拘束动作。 |
| 旧日雕像 | `BYGONE_EFFIGY` | S | S | 静止雕像不适合色情攻击，获得意志坚定。 |
| 多尼斯异鸟 | `BYRDONIS` | A、B | B | 俯冲和爪部压制可表现为拘束，不分配侵犯。 |
| 仪式兽 | `CEREMONIAL_BEAST` | A、B、I | B | 仪式、压制和野兽体型均匹配完整流程。 |
| 方柱构装体 | `CUBEX_CONSTRUCT` | S | S | 纯几何构装体不适合色情攻击，获得意志坚定。 |
| 密林真菌 | `FLYCONID` | A | A | 孢子和气味可以构成纯欲望攻击，不新增肢体动作。 |
| 雾菇主体 | `FOGMOG` | A、B | A | 幻觉和雾气可形成纯欲望攻击或包裹拘束。 |
| 雾菇附属眼 | `EYE_WITH_TEETH` | S | S | 附属眼不适合独立色情攻击，获得意志坚定。 |
| 毛绒伏地虫 | `FUZZY_WURM_CRAWLER` | A、B、I | B | 爬行、缠绕和生物性均匹配。 |
| 墨宝 | `INKLET` | A、B | A | 墨汁适合低伤害欲望攻击或短暂拘束，不分配侵犯。 |
| 蛮兽 | `MAWLER` | A、B、I | A | 野兽撕咬、扑倒与侵犯均匹配。 |
| 小啃兽 | `NIBBIT` | A、I | A | 可设计低伤害啃咬欲望攻击；已有拘束时可侵犯，但缺乏独立拘束表现。 |
| 缩小甲虫 | `SHRINKER_BEETLE` | A、B | A | 缩小效果可以构成纯欲望攻击，甲虫肢体可压制，不分配侵犯。 |
| 异蛙寄生虫 | `PHROG_PARASITE` | A、B、I | I | 寄生是其主要风味，优先侵犯，也可实施拘束。 |
| 寄生虫附属体 | `WRIGGLER` | A、I | I | 作为寄生体可以使用低伤害欲望攻击，并在玩家受拘束后侵犯。 |
| 密林史莱姆 | `LEAF_SLIME_M`、`LEAF_SLIME_S`、`TWIG_SLIME_M`、`TWIG_SLIME_S` | A、B、I | B | 黏液天然适合欲望攻击、包裹拘束和受拘束后的侵犯。 |
| 巨口果 | `SNAPPING_JAXFRUIT` | A、B、I | B | 巨口与植物肢体适合咬合拘束和吞入式侵犯。 |
| 扼杀者 | `SLITHERING_STRANGLER` | A、B、I | B | 缠绕是原有主题，优先拘束。 |
| 藤蔓蹒跚者 | `VINE_SHAMBLER` | A、B、I | B | 藤蔓直接对应拘束，并可衔接侵犯。 |
| 同族小队 | `KIN_FOLLOWER`、`KIN_PRIEST` | A、B、I | A | 随从可使用低伤害欲望攻击，祭司可使用纯欲望法术或法术拘束；玩家受拘束后两者均可侵犯。 |
| 墨影幻灵 | `VANTOM` | A、B、I | B | 影、墨与变化形体适合包裹拘束和侵犯。 |

## 4. 巢穴（HIVE）

| 遭遇/对象 | 怪物ID | 允许类型 | 首选 | 匹配依据 |
|---|---|---|---|---|
| 盛碗虫成体 | `BOWLBUG_NECTAR`、`BOWLBUG_ROCK`、`BOWLBUG_SILK` | A、B、I | B | 丝型优先拘束，蜜液型优先纯欲望攻击，生物成体可在拘束后侵犯。 |
| 盛碗虫卵 | `BOWLBUG_EGG` | S | S | 卵不适合色情攻击，获得意志坚定；孵化后的成体按自身分配判定。 |
| 自动机械咬合体 | `CHOMPER` | A、B | B | 咬合与机械夹持适合拘束，不分配侵犯。 |
| 帝皇蟹双组件 | `CRUSHER`、`ROCKET` | A、B、I | B | 夹钳主体负责拘束，火箭组件偏低伤害欲望攻击；Boss整体可衔接侵犯。 |
| 残杀千足虫前段 | `DECIMILLIPEDE_SEGMENT_FRONT` | A、B、I | B | 前段视为Boss行动主体，虫体和巨颚适合拘束并衔接侵犯。 |
| 残杀千足虫其余分段 | `DECIMILLIPEDE_SEGMENT_MIDDLE`、`DECIMILLIPEDE_SEGMENT_BACK` | S | S | 非行动主体分段获得意志坚定，避免同一Boss重复判定。 |
| 蜂群术士 | `ENTOMANCER` | A、B、I | B | 虫群可形成欲望攻击、包围拘束并衔接侵犯。 |
| 外骨骼虫 | `EXOSKELETON` | A、I | A | 适合低伤害欲望攻击；已有拘束时可侵犯，但缺少鲜明拘束动作。 |
| 猎人杀手 | `HUNTER_KILLER` | A、B、I | B | 捕猎、压制和大型生物结构均匹配。 |
| 感染棱柱 | `INFESTED_PRISM` | A、B | A | 感染能量可以形成纯欲望攻击，几何结构可以禁锢；不分配侵犯。 |
| 知识恶魔 | `KNOWLEDGE_DEMON` | A、B、I | A | 精神诱导、契约束缚和恶魔风味均匹配完整流程。 |
| 虱虫之祖 | `LOUSE_PROGENITOR` | A、B、I | I | 繁殖与寄生风味优先侵犯。 |
| 异螨群 | `MYTE` | A、I | A | 群体啃咬适合低伤害欲望攻击，玩家已受拘束时可侵犯。 |
| 直飞产卵虫 | `OVICOPTER` | A、B、I | I | 产卵与抓取动作明确适配侵犯和拘束。 |
| 产卵虫附属蛋 | `TOUGH_EGG` | S | S | 附属蛋不适合色情攻击，获得意志坚定。 |
| 棘刺蟾蜍 | `SPINY_TOAD` | A、I | A | 舌击、毒液或棘刺适合低伤害欲望攻击；已有拘束时可侵犯。 |
| 熟睡甲虫 | `SLUMBERING_BEETLE` | A、B、I | B | 甲虫肢体和苏醒后的压制动作适合拘束，并可衔接侵犯。 |
| 胧光怪 | `THE_OBSCURA` | A、B、I | A | 光与形体干扰可以形成纯欲望攻击，异常形体可拘束、侵犯。 |
| 偷窃草蜢 | `THIEVING_HOPPER` | A、I | A | 突袭和偷取适合低伤害欲望攻击，已有拘束时可侵犯。 |
| 地道虫 | `TUNNELER` | A、B、I | B | 钻地包围、虫体缠绕与吞入表现均匹配。 |
| 无厌沙虫 | `THE_INSATIABLE` | A、B、I | I | 吞噬与巨口是主要表现，优先侵犯。 |

## 5. 荣耀城（GLORY）

| 遭遇/对象 | 怪物ID | 允许类型 | 首选 | 匹配依据 |
|---|---|---|---|---|
| 斧械 | `AXEBOT` | S | S | 纯武器机械不适合色情攻击，获得意志坚定。 |
| 虔诚雕刻师 | `DEVOTED_SCULPTOR` | A、I | A | 类人施法适合纯欲望攻击；玩家受拘束时可侵犯。 |
| 门扉缔造者 | `DOORMAKER` | A、B、I | B | 类人Boss可通过门扉和构造实施拘束，并衔接侵犯。 |
| 门扉组件 | `DOOR` | S | S | Boss机制组件不适合色情攻击，获得意志坚定。 |
| 组装师 | `FABRICATOR` | S | S | 纯机械制造单位不适合色情攻击，获得意志坚定。 |
| 组装师机械单位 | `GUARDBOT`、`NOISEBOT`、`STABBOT`、`ZAPBOT` | S | S | 功能型机械附属单位获得意志坚定。 |
| 骑士团伙 | `FLAIL_KNIGHT`、`MAGI_KNIGHT`、`SPECTRAL_KNIGHT` | A、B、I | A | 武器与魔法适合低伤害或纯欲望攻击，链枷或法术可以拘束，类人单位可在拘束后侵犯。 |
| 青蛙骑士 | `FROG_KNIGHT` | A、B、I | B | 舌部和骑士压制动作适合拘束，并可衔接侵犯。 |
| 机甲骑士 | `MECHA_KNIGHT` | S | S | 纯机甲单位不适合色情攻击，获得意志坚定。 |
| 电球头 | `GLOBE_HEAD` | S | S | 功能型能量机械不适合色情攻击，获得意志坚定。 |
| 活体盾牌 | `LIVING_SHIELD` | A、B | B | 身体阻挡和压制适合拘束，不分配侵犯。 |
| 猫头鹰法官 | `OWL_MAGISTRATE` | A、I | A | 审判与凝视适合纯欲望攻击；受拘束后可侵犯。 |
| 拳击构装体 | `PUNCH_CONSTRUCT` | S | S | 纯构装体不适合色情攻击，获得意志坚定。 |
| 咬人卷轴 | `SCROLL_OF_BITING` | A、B | B | 纸带可以缠绕拘束，咬击可以设计为低伤害欲望攻击，不侵犯。 |
| 史莱姆狂战士 | `SLIMED_BERSERKER` | A、B、I | B | 类人攻击与黏液同时支持完整流程。 |
| 灵魂枢纽 | `SOUL_NEXUS` | A、B | A | 灵魂能量可以形成纯欲望攻击或禁锢，不分配实体侵犯。 |
| 女王与火炬头聚合体 | `QUEEN`、`TORCH_HEAD_AMALGAM` | A、B、I | B | Boss主体与聚合体共同承担完整流程，但每个实际行动单位分别判定。 |
| 实验体 | `TEST_SUBJECT` | A、B、I | I | 实验、变异与拘禁风味适合完整流程。 |
| 失落与遗忘之物 | `THE_LOST`、`THE_FORGOTTEN` | A、B、I | A | 异常形体和幽灵性适合纯欲望攻击、包裹和侵犯。 |
| 高塔炮手 | `TURRET_OPERATOR` | A、I | A | 类人操作员适合低伤害欲望攻击；已有拘束时可侵犯。 |

## 6. 暗港（UNDERDOCKS）

| 遭遇/对象 | 怪物ID | 允许类型 | 首选 | 匹配依据 |
|---|---|---|---|---|
| 邪教徒 | `CALCIFIED_CULTIST`、`DAMP_CULTIST` | A、B、I | A | 仪式与类人动作可覆盖完整流程。 |
| 噬尸蛞蝓 | `CORPSE_SLUG` | A、B、I | I | 黏液、包裹和吞噬特征优先侵犯。 |
| 化石追踪者 | `FOSSIL_STALKER` | A、B、I | B | 捕猎与压制动作适合拘束并衔接侵犯。 |
| 邪恶气体主体 | `LIVING_FOG` | A、B、I | A | 气体适合纯欲望攻击、包裹拘束和侵入。 |
| 气体炸弹 | `GAS_BOMB` | S | S | 战斗组件不适合色情攻击，获得意志坚定。 |
| 地精佣兵 | `GREMLIN_MERC`、`SNEAKY_GREMLIN`、`FAT_GREMLIN` | A、B、I | A | 类人敌人可使用低伤害欲望攻击；大衣、偷袭或合力可拘束并衔接侵犯。 |
| 幽灵船 | `HAUNTED_SHIP` | A、B | B | 船索和幽灵力量适合拘束，船体本身不侵犯。 |
| 乐加维林族母 | `LAGAVULIN_MATRIARCH` | A、B、I | B | 巨型生物、甲壳肢体和族母风味支持完整流程。 |
| 花园幽灵鳗 | `PHANTASMAL_GARDENER` | A、B、I | B | 长体、幽灵触肢和园艺束缚支持完整流程。 |
| 海洋混混 | `SEAPUNK` | A、I | A | 类人敌人可使用低伤害欲望攻击；玩家受拘束时可侵犯。 |
| 下水道蚌 | `SEWER_CLAM` | A、B、I | B | 壳体夹合可拘束，软体结构可衔接侵犯。 |
| 鬼祟珊瑚群 | `SKULKING_COLONY` | A、B、I | B | 群体包围和珊瑚肢体适合拘束、侵犯。 |
| 淤泥旋螺 | `SLUDGE_SPINNER` | A、B、I | B | 淤泥与旋转包裹支持完整流程。 |
| 灵魂异鱼 | `SOUL_FYSH` | A、B、I | I | 吞食灵魂与巨口结构优先侵犯。 |
| 骇鳗 | `TERROR_EEL` | A、B、I | B | 长体缠绕、电击和侵入均匹配。 |
| 蟾蜍蝌蚪 | `TOADPOLE` | A、I | A | 生物攻击可以设计为低伤害欲望攻击；已有拘束时可侵犯。 |
| 双尾鼠 | `TWO_TAILED_RAT` | A、B、I | B | 双尾提供明确拘束动作，并可衔接侵犯。 |
| 瀑布巨人 | `WATERFALL_GIANT` | A、B、I | B | 巨型水流和体型压制适合欲望攻击、拘束并衔接侵犯。 |

## 7. 逐怪物详细意图

每个怪物的色情攻击名称、数值、附加效果、拘束对象与侵犯诅咒，统一记录在[`EROTIC_ATTACK_INTENTS.md`](EROTIC_ATTACK_INTENTS.md)。本分配表不再保存通用意图模板，避免模板与逐怪物设计产生双重权威。
