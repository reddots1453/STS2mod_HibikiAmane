# Events / 事件

> 基线：本地运行时知识库`KnowledgeBase/events.json`，共57个事件。
>
> 原版效果已逐项对照`_decompiled/sts2/MegaCrit.Sts2.Core.Models.Events/`中的事件类；下方叙事文本和初始选项来自知识库。
>
> 本文是设计资料，不是技术实现文档。动态数值按未受进阶、多人或其他修正时的基础值记录。

## 标记说明

- `【MS·堕落+1】`、`【MS·堕落-1】`：在原版选项效果结算后追加1点堕落值变化；单个选项绝不超过1点。
- `【MS·新增选项｜条件】`：仅本Mod角色可见，并在满足堕落值门槛时解锁的新选项。
- 没有标记的事件保持原版规则，不因本Mod角色发生变化。
- 堕落值描述信仰与行为倾向。只标记语义明确的放纵、接纳腐化、克制、拒绝或净化行为；普通的贪婪、冒险和求知不自动影响堕落值。

## 原版效果与Mod改造索引

| ID | 事件 | 原版选项与效果摘要 | Maiden & Succubus改造 |
|---|---|---|---|
| `ABYSSAL_BATHS` | 深渊浴场 | 投身其中：获得2最大生命并失去3生命；之后可反复逗留，每次获得2最大生命，生命损失依次增加1。敬而远之：恢复10生命。 | 投身其中仅首次选择时`【MS·堕落+1】`；敬而远之`【MS·堕落-1】`。 |
| `AMALGAMATOR` | 熔合者 | 移除2张基础打击并获得终极打击，或移除2张基础防御并获得终极防御。 | — |
| `AROMA_OF_CHAOS` | 混沌芳香 | 放任自流：变化1张牌。维持理智：升级1张牌。 | 放任自流`【MS·堕落+1】`；维持理智`【MS·堕落-1】`。 |
| `BATTLEWORN_DUMMY` | 战痕累累的训练假人 | 选择三档之一并在3回合内击败假人；依难度分别获得随机药水、随机升级2张牌、随机遗物。超时无奖励。 | — |
| `BRAIN_LEECH` | 脑蛭 | 分享知识：从5张本角色牌中选1张。把它扯下来：失去5生命，获得一次无色牌奖励。 | — |
| `BUGSLAYER` | 害虫杀手 | 二选一获得事件牌“杀灭”或“压扁”。 | — |
| `BYRDONIS_NEST` | 多尼斯异鸟巢 | 吃蛋：获得7最大生命。带走：获得事件牌“多尼斯异鸟蛋”。 | — |
| `COLORFUL_PHILOSOPHERS` | 色彩哲学家 | 从至多3种已解锁且非当前角色的卡池中选一种，依次获得该卡池普通、罕见、稀有牌奖励，每次展示3张。 | — |
| `COLOSSAL_FLOWER` | 巨大花卉 | 可立即获得35金币；或依次失去5、6生命深入并将金币提高到75、135；最深处可再失去7生命获得“花粉核心”。 | — |
| `CRYSTAL_SPHERE` | 水晶球 | 支付随机51～99金币进行3次揭示，或将“债务”加入牌组进行6次揭示；小游戏可能揭示金币、药水、遗物、卡牌奖励或诅咒。 | — |
| `DENSE_VEGETATION` | 茂密的植被 | 坚持跋涉：移除1张牌并失去11生命。休息：按火堆休息量恢复生命，随后进入事件战斗。 | — |
| `DOLL_ROOM` | 玩偶室 | 随机拿走1件玩偶遗物；或失去5生命后在随机2件中选1件；或失去15生命后在全部3件中选1件。 | — |
| `DOORS_OF_LIGHT_AND_DARK` | 光与暗的门扉 | 光之门：随机升级2张牌。暗之门：移除1张牌。 | 光之门`【MS·堕落-1】`；暗之门`【MS·堕落+1】`。 |
| `DROWNING_BEACON` | 淹水灯塔 | 装瓶：获得“辉光水药水”。攀爬：失去13最大生命，获得“菲涅尔透镜”。 | — |
| `ENDLESS_CONVEYOR` | 无尽传送带 | 通常花费35金币购买当前随机菜品：恢复10生命、获得4最大生命、获得药水、变化1张牌、获得无色牌或升级1张牌等；每第5次固定获得“狂宴”，金鱼菜品反而获得75金币。也可观察厨师并随机升级1张牌。 | — |
| `FAKE_MERCHANT` | 商人？？？ | 进入出售仿制遗物的特殊商店；用“恶臭药水”攻击会进入战斗，胜利奖励包含地毯及尚未售出的仿制遗物。 | — |
| `FIELD_OF_MAN_SIZED_HOLES` | 人形洞穴之地 | 抵抗诱惑：移除2张牌并获得“凡庸”。进入你的洞：为1张牌附魔“完美契合”。 | 抵抗诱惑`【MS·堕落-1】`；进入你的洞`【MS·堕落+1】`。 |
| `GRAVE_OF_THE_FORGOTTEN` | 遗忘之墓 | 对抗：获得“腐朽”，为1张牌附魔“灵魂之力”。接受：获得“遗忘之魂”。 | — |
| `HUNGRY_FOR_MUSHROOMS` | 蘑菇饥渴 | 获得“大蘑菇”；或失去15生命并获得“芳香蘑菇”。 | — |
| `INFESTED_AUTOMATON` | 被寄生的自动机械 | 学习：获得1张随机能力牌。触碰核心：获得1张随机0费牌。 | — |
| `JUNGLE_MAZE_ADVENTURE` | 丛林迷宫奇遇 | 独自挑战：失去18生命，获得约135～165金币。结伴同行：获得约35～65金币。 | — |
| `LOST_WISP` | 迷失鬼火 | 抓住：获得“迷失鬼火”遗物和“腐朽”。搜索：获得约45～75金币。 | — |
| `LUMINOUS_CHOIR` | 冷光合唱团 | 探入血肉：移除2张牌并获得“孢子心智”。献上贡品：支付约100～149金币，获得随机遗物。 | `【MS·新增选项｜堕落≤-3】安抚合唱：移除至多2张牌，不获得孢子心智。` `【MS·新增选项｜堕落≥+3】攫取辉光：不支付金币，获得随机遗物。` |
| `MORPHIC_GROVE` | 变形灵林谷 | 大群变形灵：被夺走100金币，变化2张牌。落单变形灵：获得5最大生命。 | — |
| `POTION_COURIER` | 药水快递员 | 拿走：获得3瓶“恶臭药水”。洗劫：获得1瓶随机罕见药水。 | — |
| `PUNCH_OFF` | 重拳出击 | 顺走：获得“受伤”和随机遗物。挑战两只怪物：胜利后获得随机遗物和随机药水。 | — |
| `RANWID_THE_ELDER` | 长者兰伟德 | 交出随机药水换随机遗物；支付100金币换随机遗物；或交出随机可交易遗物换2件随机遗物。 | — |
| `REFLECTIONS` | 镜中倒影 | 触碰：随机降级至多2张已升级牌，再随机升级至多4张牌。打碎：复制整个牌组并获得“厄运”。 | `【MS·新增选项｜堕落≤-4】接纳真我：随机升级至多4张牌，不降级卡牌。` `【MS·新增选项｜堕落≥+4】夺取倒影：选择1张牌，将其复制加入牌组，不获得厄运。` |
| `RELIC_TRADER` | 遗物交换商 | 展示至多3组“已有可交易遗物→新随机遗物”的交易，选择其中1组交换。 | — |
| `ROOM_FULL_OF_CHEESE` | 满屋芝士 | 大快朵颐：从8张随机普通本角色牌中选择2张。仔细翻找：失去14生命并获得“精选芝士”。 | — |
| `ROUND_TEA_PARTY` | 圆桌茶会 | 喝茶：获得“皇家毒药”并恢复全部生命。挑事：失去11生命并获得随机遗物。 | — |
| `SAPPHIRE_SEED` | 蓝宝石种子 | 吃下：恢复9生命并升级1张牌。种植：为1张牌附魔“播种”。 | — |
| `SELF_HELP_BOOK` | 自助指南 | 根据牌组可选：为1张攻击牌附魔2层锋利、技能牌附魔2层伶俐或能力牌附魔2层迅捷。 | — |
| `SLIPPERY_BRIDGE` | 滑脚木桥 | 放弃当前显示的随机可移除牌并结束事件；或承受3、4、5……点递增生命损失，重新随机要放弃的牌并继续选择。 | — |
| `SPIRALING_WHIRLPOOL` | 螺旋漩涡 | 观察：为1张牌附魔“螺旋”。饮用：恢复最大生命的33%。 | — |
| `SPIRIT_GRAFTER` | 灵魂嫁接者 | 接纳：恢复25生命并获得“蜕变”。拒绝：移除1张牌并失去9生命。 | 接纳`【MS·堕落+1】`；拒绝`【MS·堕落-1】`。 |
| `STONE_OF_ALL_TIME` | 永恒之石 | 举起：丢弃随机1瓶药水，获得10最大生命。推动：失去6生命，为1张牌附魔8层“活力”。 | — |
| `SUNKEN_STATUE` | 沉没雕像 | 拿剑：获得“石中剑”。潜水：失去7生命，获得约101～121金币。 | — |
| `SUNKEN_TREASURY` | 淹水金库 | 第一个箱子：获得约52～67金币。第二个箱子：获得约303～363金币并获得“贪婪”。 | `【MS·新增选项｜堕落≤-4】只取所需：获得150金币，不获得诅咒。` `【MS·新增选项｜堕落≥+4】吞下贪欲：获得第二个箱子的金币，但不获得贪婪。` |
| `SYMBIOTE` | 共生体 | 靠近：为1张牌附魔“腐化”。用火烧死：变化1张牌。 | 靠近`【MS·堕落+1】`；用火烧死`【MS·堕落-1】`。`【MS·新增选项｜堕落≤-3】净化：移除1张牌。` `【MS·新增选项｜堕落≥+3】完全共生：为至多2张牌附魔腐化。` |
| `TABLET_OF_TRUTH` | 真理石板 | 砸碎：恢复20生命。解读可连续5次，依次失去3、6、12、24以及除1点外的全部最大生命；前4次各升级1张随机牌，第5次升级全部可升级牌。 | — |
| `TEA_MASTER` | 茶艺大师 | 支付50金币获得“骨茶”；支付150金币获得“余烬茶”；或免费获得“失礼之茶”。 | — |
| `THE_FUTURE_OF_POTIONS` | 药水的未来？ | 交出1瓶药水，从3张与药水稀有度相同、随机指定类型且均已升级的本角色牌中选1张。 | — |
| `THE_LANTERN_KEY` | 灯火钥匙 | 交还：获得100金币。留下：进入事件战斗，胜利获得任务牌“灯火钥匙”。 | — |
| `THE_LEGENDS_WERE_TRUE` | 传说是真的 | 拿走地图：获得“战利品地图”。寻找出口：失去8生命并获得随机药水。 | — |
| `THIS_OR_THAT` | 这个还是那个？ | 这个：失去6生命，获得41～68金币。那个：获得随机遗物和“笨拙”。 | — |
| `TINKER_TIME` | 打造时间 | 从随机展示的2种牌类型中选1种，再从该类型随机展示的2种附加效果中选1种，获得相应定制的“疯狂科学”。 | — |
| `TRASH_HEAP` | 垃圾堆 | 扎进去：失去8生命，获得预设池中的随机遗物。随便拿：获得100金币及预设池中的随机牌。 | — |
| `TRIAL` | 审判 | 接受后随机审理商人、贵族或无名者；不同有罪/无罪判决会给予诅咒并配套2件遗物、升级2张牌、恢复10生命、300金币、两次卡牌奖励或变化2张牌。拒绝后可反悔，或再次拒绝并放弃Run。 | — |
| `UNREST_SITE` | 无休之处 | 休息：恢复全部生命并获得“睡眠不佳”。杀死树木：失去8最大生命并获得随机遗物。 | — |
| `WAR_HISTORIAN_REPY` | 战史学家付袭 | 当前随机事件池中禁用。若通过灯火钥匙链触发：打开笼子获得“历史课程”；打开宝箱获得2瓶药水和2件遗物；两者都会移除灯火钥匙。 | — |
| `WATERLOGGED_SCRIPTORIUM` | 水漫缮写室 | 血墨：获得6最大生命。支付65金币为1张牌附魔稳固；支付155金币为2张牌附魔稳固。 | 触手羽毛笔（65金币选项）`【MS·堕落+1】`。 |
| `WELCOME_TO_WONGOS` | 欢迎来到旺购百货 | 100金币买随机普通遗物；200金币买展示的稀有遗物；300金币买神秘票券；直接离开会随机降级1张已升级牌。购买还累计旺购积分。 | — |
| `WELLSPRING` | 泉水 | 装瓶：获得随机药水。沐浴：移除1张牌并获得“罪责”。 | 沐浴`【MS·堕落-1】`。 |
| `WHISPERING_HOLLOW` | 低语空谷 | 交换：支付50金币，获得2瓶随机药水。拥抱：变化1张牌并失去9生命。 | 拥抱树木`【MS·堕落+1】`。`【MS·新增选项｜堕落≤-2】净化树木：获得300金币，并将1张腐朽加入牌组。` `【MS·新增选项｜堕落≥+4】吸收灵魂：获得枯树灵魂。` |
| `WOOD_CARVINGS` | 木雕 | 将1张基础牌变化为“啄击”；为1张牌附魔“蜿蜒”；或将1张基础牌变化为“环形韧性”。 | — |
| `ZEN_WEAVER` | 修禅织网者 | 支付50金币获得2张“启迪”；支付125金币移除1张牌；支付250金币移除2张牌。 | — |

## 原版事件池与必要触发条件

下表记录事件正常进入候选池所需的全部已知条件，依据各幕`AllEvents`、`ModelDb.AllSharedEvents`、时间线解锁过滤以及事件自身的`IsAllowed`。其中：

- “无额外状态条件”仍要求进入该事件所属幕的未知房间，并且事件已解锁。
- “共享事件池”表示可加入所有幕的事件池；表中若有限定幕数，则以限定为准。
- 多人游戏中，除非特别注明，涉及金币、生命、牌组、药水或遗物的条件必须由**所有玩家**同时满足。
- 正常情况下，已经访问过的事件不会再次出现；当本幕所有独特事件均已耗尽时，原版会允许重复事件作为兜底。
- 金币等随机结算数值可能在进入事件后变化。触发门槛记录`IsAllowed`实际使用的基准值，而非选项最终显示的随机价格。

| ID | 原版事件池 | 必要触发条件 |
|---|---|---|
| `ABYSSAL_BATHS` | `UNDERDOCKS`专属 | 无额外状态条件。 |
| `AMALGAMATOR` | `HIVE`专属 | 牌组中至少有2张可移除的基础“打击”标签牌，且至少有2张可移除的基础“防御”标签牌。 |
| `AROMA_OF_CHAOS` | `OVERGROWTH`专属 | 无额外状态条件。 |
| `BATTLEWORN_DUMMY` | `GLORY`专属 | 无额外状态条件。 |
| `BRAIN_LEECH` | 共享事件池 | 仅第1或第2幕。 |
| `BUGSLAYER` | `HIVE`专属 | 无额外状态条件。 |
| `BYRDONIS_NEST` | `OVERGROWTH`专属 | 当前没有事件宠物。 |
| `COLORFUL_PHILOSOPHERS` | `HIVE`专属 | 已在时间线中揭示`EVENT3_EPOCH`。 |
| `COLOSSAL_FLOWER` | `HIVE`专属 | 当前生命至少为19。 |
| `CRYSTAL_SPHERE` | 共享事件池 | 第2幕或更晚，且至少拥有100金币。 |
| `DENSE_VEGETATION` | `OVERGROWTH`专属 | 无额外状态条件。 |
| `DOLL_ROOM` | 共享事件池 | 仅第2幕。 |
| `DOORS_OF_LIGHT_AND_DARK` | `UNDERDOCKS`专属 | 无额外状态条件。 |
| `DROWNING_BEACON` | `UNDERDOCKS`专属 | 无额外状态条件。 |
| `ENDLESS_CONVEYOR` | `UNDERDOCKS`专属 | 至少拥有105金币。 |
| `FAKE_MERCHANT` | 共享事件池 | 第2幕或更晚；仅单人游戏；并且拥有至少100金币或至少1瓶“恶臭药水”。 |
| `FIELD_OF_MAN_SIZED_HOLES` | `HIVE`专属 | 牌组中至少有1张可以被“完美契合”附魔的牌。 |
| `GRAVE_OF_THE_FORGOTTEN` | `GLORY`专属 | 无额外状态条件。 |
| `HUNGRY_FOR_MUSHROOMS` | `GLORY`专属 | 无额外状态条件。 |
| `INFESTED_AUTOMATON` | `HIVE`专属 | 无额外状态条件。 |
| `JUNGLE_MAZE_ADVENTURE` | `OVERGROWTH`专属 | 无额外状态条件。 |
| `LOST_WISP` | `HIVE`专属 | 无额外状态条件。 |
| `LUMINOUS_CHOIR` | `OVERGROWTH`专属 | 至少拥有149金币，且遗物奖池中仍有可获得的遗物。进入事件后贡品价格才会随机为100～149金币。 |
| `MORPHIC_GROVE` | `OVERGROWTH`专属 | 至少拥有100金币。 |
| `POTION_COURIER` | 共享事件池 | 第2幕或更晚。 |
| `PUNCH_OFF` | `UNDERDOCKS`专属 | 总楼层数至少为6。 |
| `RANWID_THE_ELDER` | 共享事件池 | 第2幕或更晚；至少拥有100金币、1瓶药水和1件可交易遗物。 |
| `REFLECTIONS` | `GLORY`专属 | 已在时间线中揭示`EVENT2_EPOCH`。 |
| `RELIC_TRADER` | 共享事件池 | 第2幕或更晚，且至少拥有5件可交易遗物。 |
| `ROOM_FULL_OF_CHEESE` | 共享事件池 | 仅第1或第2幕。 |
| `ROUND_TEA_PARTY` | `GLORY`专属 | 无额外状态条件。 |
| `SAPPHIRE_SEED` | `OVERGROWTH`专属 | 无额外状态条件。 |
| `SELF_HELP_BOOK` | 共享事件池 | 无额外状态条件。具体附魔选项仍取决于牌组中是否有对应类型的可附魔牌。 |
| `SLIPPERY_BRIDGE` | 共享事件池 | 总楼层数大于6，且牌组中至少有1张可移除的牌。 |
| `SPIRALING_WHIRLPOOL` | `UNDERDOCKS`专属 | 牌组中至少有1张可以被“螺旋”附魔的牌。 |
| `SPIRIT_GRAFTER` | `HIVE`专属 | 无额外状态条件。 |
| `STONE_OF_ALL_TIME` | 共享事件池 | 仅第2幕，且至少拥有1瓶药水。 |
| `SUNKEN_STATUE` | `OVERGROWTH`、`UNDERDOCKS`共有 | 无额外状态条件。 |
| `SUNKEN_TREASURY` | `UNDERDOCKS`专属 | 无额外状态条件。 |
| `SYMBIOTE` | 共享事件池 | 第2幕或更晚。靠近选项能否使用仍取决于是否有可被“腐化”附魔的牌。 |
| `TABLET_OF_TRUTH` | `OVERGROWTH`专属 | 无额外状态条件。 |
| `TEA_MASTER` | 共享事件池 | 仅第1或第2幕，且至少拥有150金币。 |
| `THE_FUTURE_OF_POTIONS` | 共享事件池 | 至少拥有2瓶药水。 |
| `THE_LANTERN_KEY` | `HIVE`专属 | 无额外状态条件。 |
| `THE_LEGENDS_WERE_TRUE` | 共享事件池 | 仅第1幕；牌组非空；当前生命至少为10。 |
| `THIS_OR_THAT` | 共享事件池 | 无额外状态条件。 |
| `TINKER_TIME` | `GLORY`专属 | 无额外状态条件。 |
| `TRASH_HEAP` | `UNDERDOCKS`专属 | 已在时间线中揭示`EVENT1_EPOCH`；当前生命大于5。 |
| `TRIAL` | `GLORY`专属 | 无额外状态条件。 |
| `UNREST_SITE` | `OVERGROWTH`专属 | 当前生命不高于最大生命的70%。 |
| `WAR_HISTORIAN_REPY` | 不进入普通事件池 | 自身`IsAllowed`恒为否。必须持有任务牌“灯火钥匙”，并在第3幕进入未知房间；该牌会把房间改为事件并将下一事件替换为本事件。 |
| `WATERLOGGED_SCRIPTORIUM` | `UNDERDOCKS`专属 | 至少拥有65金币。 |
| `WELCOME_TO_WONGOS` | 共享事件池 | 仅第2幕，且至少拥有100金币。 |
| `WELLSPRING` | `OVERGROWTH`专属 | 无额外状态条件。 |
| `WHISPERING_HOLLOW` | `OVERGROWTH`专属 | 至少拥有50金币。 |
| `WOOD_CARVINGS` | `OVERGROWTH`专属 | 牌组中至少有1张可移除的基础牌。 |
| `ZEN_WEAVER` | `HIVE`专属 | 至少拥有125金币。 |

## 原版标题、叙事描述与知识库初始选项

以下内容逐项列出知识库的双语标题、英文原文、中文描述和中文初始选项。标记为“知识库未提取固定初始选项”的条目通常使用动态选项或自定义界面，完整选项与结算以上方效果摘要为准。

### Abyssal Baths / 深渊浴场
- **ID**: `ABYSSAL_BATHS`
- **原文描述（英文）**: You discover a secluded chamber.

Steam rises from bubbling pools of hot liquid that shifts colors with hypnotic rhythm. Barnacled growths hang from the ceiling, dripping viscous fluid that hisses and writhes when it touches the surface. The air feels heavy, laden with [blue]salt[/blue] and something [green]unmistakably organic[/green].

As you approach the edge of the largest pool, the liquid ripples. The waters bubble more intensely as if anticipating your entry.

- **中文描述**: 你发现了一间僻静的密室。

氤氲的雾气从沸腾的池水中升腾而起，池水的色泽正以一种催眠般的节奏变幻。天花板上垂挂着覆满藤壶的异形生长物，滴落的黏稠液体在触及水面的瞬间，一边发出嘶嘶声，一边如活物般扭动挣扎。空气粘滞沉重，充斥着[blue]盐[/blue]的味道和某种[green]不言而喻的活物的气息[/green]。

当你靠近那口最大的水池时，液面泛起了涟漪。水流翻腾得愈发狂乱，仿佛正期待着你的到来。

- **中文初始选项**:
  - [0] 投身其中
  - [1] 敬而远之

### Amalgamator / 熔合者
- **ID**: `AMALGAMATOR`
- **原文描述（英文）**: [b][jitter]CLANG! CLANG!!![/jitter][/b]

You hear the echoes of metal upon metal on the otherside of a wall...

As you press your head against the wall to listen in—the wall opens up, revealing a [orange]hulking six-armed figure[/orange] toiling away.
Its “face” is a [gold][sine]swirling vortex of glowing sigils[/sine][/gold] that shift about.

“One with an [aqua]Ascending Spirit[/aqua] has found my workshop? Let's get [jitter]COMBINING[/jitter]!”

- **中文描述**: [b][jitter]铿！铿！！锵！！！[/jitter][/b]

墙壁的另一侧传来了阵阵金属交击声的回响…

当你把头贴在墙上准备探听一番时——墙壁轰然开启，露出了一个正在埋头苦干的[orange]魁梧六臂身影[/orange]。
它的“脸部”是一团由[gold][sine]发光符文构成的流转旋涡[/sine][/gold]，正在不断变幻交织。

“一个拥有[aqua]进阶之魂[/aqua]的人竟然找到了我的工坊？那就让我们开始[jitter]融合[/jitter]吧！”

- **中文初始选项**:
  - [0] 融合打击
  - [1] 融合防御

### Aroma of Chaos / 混沌芳香
- **ID**: `AROMA_OF_CHAOS`
- **原文描述（英文）**: Upon trudging through a thick underbrush and making it into a clearing, you're hit with an unplaceable nostalgia.

A mix of [purple]floral scents[/purple], [green]putrid decay[/green], and [orange]something else entirely[/orange]. The scent grows stronger with each step, and you feel as though the world around you is [sine]warping and twisting[/sine].

Overcome with a sense of [sine][rainbow freq=0.3 sat=0.8 val=1]shifting chaos[/rainbow][/sine], you begin to lose your sense of self.

- **中文描述**: 费力地穿过一片厚密的灌木丛并来到空地后，一种莫名的怀旧感突然向你袭来。

那是[purple]花朵的芬芳[/purple]、[green]腐烂的恶臭[/green]以及[orange]某种无法言说的气息[/orange]的混合体。每前进一步，这股气味都变得愈发浓烈。与此同时，你感受到周围的世界正开始[sine]扭曲，旋转[/sine]。

一股[sine][rainbow]变幻莫测的混沌[/rainbow][/sine]将你淹没，你开始慢慢地丧失了对自我的感知。

- **中文初始选项**:
  - [0] 放任自流
  - [1] 维持理智

### Battleworn Dummy / 战痕累累的训练假人
- **ID**: `BATTLEWORN_DUMMY`
- **原文描述（英文）**: As you approach, it begins to rumble and fizzle and lights up brilliantly!

“[jitter]BZZZT![/jitter] TIME TO TRAIN!!! YOU HAVE [blue]3 TURNS[/blue] TO DEFEAT ME!
CHOOSE A SETTING OR FACE [red]LETHAL HUMILIATION[/red].
Here are the options:”

After the terrifying message, the dummy carefully reads out detailed instructions.

What do you choose?

- **中文描述**: 当你靠近时，它突然开始轰鸣，滋滋作响，随后爆发出耀眼的光芒！

[b]“[jitter]滋滋！[/jitter] 训练时间到！！！你有[blue]3个回合[/blue]的时间来击败我！
选择一个档位，否则就准备好面对[red]致命的羞辱[/red]吧。[/b]
以下是可选方案：”

在发布完这段恐怖的宣言后，假人开始认真地读出详细的指令。

要选哪一个呢？

- **中文初始选项**:
  - [0] 第1档
  - [1] 第2档
  - [2] 第3档

### Brain Leech / 脑蛭
- **ID**: `BRAIN_LEECH`
- **原文描述（英文）**: [jitter]*thunk*[/jitter]

A sharp pain is felt atop your head and a thought stabs into your mind.

[purple][sine]“SHARE KNOWLEDGE???”[/sine][/purple]

You are unsure how to proceed...

- **中文描述**: [jitter]咚！[/jitter]

你的头顶传来一阵刺痛，一个念头刺入了你的脑海。

[purple][sine][b]“分享知识吗？？？”[/b][/sine][/purple]

你不知如何是好…

- **中文初始选项**:
  - [0] 分享知识
  - [1] 把它扯下来

### Bugslayer / 害虫杀手
- **ID**: `BUGSLAYER`
- **原文描述（英文）**: As you're fending off a [jitter][red]swarm of aggressive insects[/red][/jitter] you suddenly notice a [gold]Mighty Rugged Warrior-Man[/gold] fighting beside you this whole time!

The insects, sensing defeat, scatter. You turn to your fellow fighter.
“Would you like any tips on exterminating these pests?”

How polite. You nod and accept his offer.

- **中文描述**: 当你正奋力抵御一群[jitter][red]穷凶极恶的虫群[/red][/jitter]时，你突然注意到，一位[gold]威猛剽悍的战士[/gold]竟然一直就在你身边并肩作战！

虫群们，察觉败局，四散而逃。你转向这位战友。
“你想要一些消灭这些害虫的建议吗？”

还挺有礼貌。你点了点头，接受了他的提议。

- **中文初始选项**:
  - [0] 学习杀灭的技巧
  - [1] 学习压扁的技巧

### Byrdonis Nest / 多尼斯异鸟巢
- **ID**: `BYRDONIS_NEST`
- **原文描述（英文）**: You spot an enormous [jitter][red]Shambling Beast[/red][/jitter] chase off an injured [green][sine]Green Byrd[/sine][/green].

In the alcove the byrd fled from, a [gold]single unguarded egg[/gold] lies abandoned.

[sine]Your stomach gurgles...[/sine]

- **中文描述**: 你看见一只巨大的[jitter][red]蹒跚巨兽[/red][/jitter]正在驱赶一只受伤的[green][sine]绿色异鸟[/sine][/green]。

在那只异鸟逃离的隐蔽角落里，一颗[gold]无人看守的蛋[/gold]被遗弃在了那里。

[sine]你的肚子咕咕作响…[/sine]

- **中文初始选项**:
  - [0] 吃掉这颗蛋
  - [1] 带走这颗蛋

### Colorful Philosophers / 色彩哲学家
- **ID**: `COLORFUL_PHILOSOPHERS`
- **原文描述（英文）**: Before you is a rather epic sight.
You see 3 different colored statues towering over a dais, having a [jitter][red]heated debate[/red][/jitter] over the philosophical implications of color.

As you listen in, you get a sense that the most important question at hand is which color truly is [gold]THE BEST[/gold].

You chime in with your thoughts.

- **中文描述**: 在你面前的是一幕颇为宏伟的景象。
你看到三座不同颜色的石像耸立在高台之上，正就色彩的哲学内涵展开[jitter][red]激烈的争论[/red][/jitter]。

随着你的倾听，你感觉到眼下最核心的议题是，究竟哪种颜色才是[gold][b]最棒的[/b][/gold]。

你加入其中，表达了你的见解。

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Colossal Flower / 巨大花卉
- **ID**: `COLOSSAL_FLOWER`
- **原文描述（英文）**: There is a [green]colossal flower[/green] growing atop a [red][jitter]mountain of bones[/jitter][/red].

As its [sine][rainbow freq=0.3 sat=0.8 val=1]color-shifting petals[/rainbow][/sine] pulsate, you sense there is a [aqua]Powerful Cluster of Pollen[/aqua] in the center but the undulating petals are [red]razor sharp[/red] and unpredictable.

You could grab some of the [gold]golden nectar[/gold], but reaching the prize in the center is so tempting...

- **中文描述**: 一朵[green]巨型花卉[/green]正生长在一座[red][jitter]骸骨堆积而成的山[/jitter][/red]上。

随着它那[sine][rainbow freq=0.3 sat=0.8 val=1]变换色彩的花瓣[/rainbow][/sine]不断搏动，你感觉到中心处一定有[aqua]一团强力花粉[/aqua]，但那起伏的花瓣[red]如刀锋般锋利[/red]且捉摸不定。

你本可以只采集一些[gold]金色花蜜[/gold]，但中心处的那个奖赏实在是太诱人了…

- **中文初始选项**:
  - [0] 采集花蜜
  - [1] 深入探索

### Crystal Sphere / 水晶球
- **ID**: `CRYSTAL_SPHERE`
- **原文描述（英文）**: [sine][blue]“I predicted you'd enter...!”[/blue][/sine]

A [jitter]raspy[/jitter] voice calls out as you enter a [purple]mystic hut[/purple].

[sine][blue]“Your destiny has brought you here. We must uncover your future and fortunes so you can SAVE US ALL!!”[/blue][/sine]
“Okay, here are the options for the [gold]Crystal Sphere reading[/gold]. Be sure to sign this waiver as well,” she says while bringing out pen and parchment.

- **中文描述**: [sine][blue]“我预见到了你的到来…！”[/blue][/sine]

当你走进一间[purple]神秘小屋[/purple]时，一个[jitter]沙哑[/jitter]的声音传了过来。

[sine][blue]“你的命运指引你来到此处。我们必须揭开你的未来与运势，这样你才能拯救我们所有人！！”[/blue][/sine]
“好啦，这是进行[gold]水晶球占卜[/gold]的几个选项。另外，记得把这份免责协议也签了，”她一边说着，一边拿出了笔和羊皮纸。

- **中文初始选项**:
  - [0] 揭幕未来
  - [1] 分期付款

### Dense Vegetation / 茂密的植被
- **ID**: `DENSE_VEGETATION`
- **原文描述（英文）**: Having taken the wrong path for a good while, you find yourself in a thick jungle of [green]ferns[/green], [green]shrubs[/green], and [green]vines[/green]. Especially [green]vines[/green]. Exhaustion sets in, and a dark thought comes to mind:

[sine][purple]“You are lost, unprepared, and the inevitability of death is approaching.”[/purple][/sine]

What do you do?

- **中文描述**: 你在错路上走了一段时间之后，你发现自己置身于一片由[green]蕨类[/green]、[green]灌木[/green]和[green]藤蔓[/green]构成的茂密丛林中。尤其是[green]藤蔓[/green]。精疲力竭的感觉油然而生，一个可怕的念头随之浮现在脑海里：

[sine][purple]“你迷路了，毫无防备，而死亡的阴影正悄然逼近。”[/purple][/sine]

你要怎么办呢？

- **中文初始选项**:
  - [0] 坚持跋涉
  - [1] 休息

### Doll Room / 玩偶室
- **ID**: `DOLL_ROOM`
- **原文描述（英文）**: You enter a hidden room...

It's packed with an array of dolls. Each are unique, their expressions ranging from [green]joy[/green] to [purple]sorrow[/purple], their attire spanning centuries and realms beyond your own.

A [sine][blue]chorus of whispers[/blue][/sine] grows louder and louder until [jitter][red]the dolls are shouting[/red][/jitter] over each other and calling for you...

You must choose one and leave immediately.

- **中文描述**: 你走进了一间隐藏的密室……

这里堆满了琳琅满目的玩偶。每一只都独一无二，它们的表情亦从[green]喜悦[/green]到[purple]悲哀[/purple]各不相同，其服饰跨越了数个世纪，甚至来自你认知之外的领域。

一阵[sine][blue]低语的合唱[/blue][/sine]变得越来越响，直到[jitter][red]那些玩偶互相争吵着大叫起来[/red][/jitter]，放声呼唤着你……

你必须选择一个，并立刻离开这个地方。

- **中文初始选项**:
  - [0] 随机拿走一尊
  - [1] 花点时间慢慢来
  - [2] 仔细检查然后挑选最好的那个

### Doors of Light and Dark / 光与暗的门扉
- **ID**: `DOORS_OF_LIGHT_AND_DARK`
- **原文描述（英文）**: A doorway existed where none existed moments before...

You enter to find two shimmering doorways and a [gold]well-dressed doorman[/gold].
“Finally, a visitor! Please please! Choose a door, any door!”

[blue]Are we still in the Spire? How is this place so clean?[/blue]

- **中文描述**: 一道门扉突然凭空出现了…

你步入其中，见到了两道散发着微光的门，以及一位[gold]衣着得体的看门人[/gold]。
“终于有访客了！请，请！选一扇门，任何一扇都可以！”

[blue]我们还在尖塔里吗？这地方为何竟如此洁净？[/blue]

- **中文初始选项**:
  - [0] 光之门
  - [1] 暗之门

### Drowning Beacon / 淹水灯塔
- **ID**: `DROWNING_BEACON`
- **原文描述（英文）**: Crossing a dreamscape of rocks and still waters, you emerge to face a [sine]sinking lighthouse[/sine] emitting [purple]anti-light[/purple].

You can bottle up the [aqua][sine]eerily-glowing water[/sine][/aqua] that has pooled here or climb up the dilapidated structure to retrieve the [gold]lens[/gold].

- **中文描述**: 穿过一片由岩石与静水构成的梦幻之境，你来到了一座散发着[purple]逆流光[/purple]的[sine]沉没灯塔[/sine]前。

你可以把积聚在此的[aqua][sine]诡异发光水[/sine][/aqua]装进瓶子里，或者爬上这座破败的建筑，取下其中的[gold]透镜[/gold]。

- **中文初始选项**:
  - [0] 装瓶
  - [1] 攀爬

### Endless Conveyor / 无尽传送带
- **ID**: `ENDLESS_CONVEYOR`
- **原文描述（英文）**: You lumber into a shack with a bright but crooked sign that reads:
[aqua]“ENDLESS FEAST - PAY WHAT YOU HUNGER!”[/aqua]

Inside, a [orange]willowy multi-armed chef[/orange] is deftly preparing [green]bites of food[/green] and placing them onto a [sine]winding chitinous belt[/sine].

One of the chef's arms point to a sign:
[blue]35[/blue] [gold]Gold[/gold] each

- **中文描述**: 你走进一间挂着歪斜招牌的小木屋，招牌亮得发晃，上面写着：
[aqua][b]“无尽盛宴—按饿付费！”[/b][/aqua]

屋里，一位[orange]身形修长的多臂大厨[/orange]正熟练地准备着[green]一份份小食[/green]，并将它们摆在一条[sine]蜿蜒的几丁质传送带[/sine]上。

大厨其中的一只手臂指向一块牌子：
每份[blue]35[/blue][gold]金币[/gold]

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Field of Man-Sized Holes / 人形洞穴之地
- **ID**: `FIELD_OF_MAN_SIZED_HOLES`
- **原文描述（英文）**: The sudden sight of a massive field, [jitter]gouged with large outlines of bodies[/jitter], stops you in your tracks.

One of these outlines matches your own.

[sine][orange]It's perfect...[/orange][/sine]

- **中文描述**: 眼前突然出现一大片土地，上面[jitter]凿刻着密密麻麻的人形轮廓[/jitter]，让你不由得停下了脚步。

其中有一个洞的轮廓，竟然和你的体型一模一样。

[sine][orange]完美契合…[/orange][/sine]

- **中文初始选项**:
  - [0] 抵抗诱惑
  - [1] 进入你的洞

### Grave of the Forgotten / 遗忘之墓
- **ID**: `GRAVE_OF_THE_FORGOTTEN`
- **原文描述（英文）**: There is a single grave with a [jitter][aqua]roiling blue flame[/aqua][/jitter]...

A prideful warrior died in battle but its soul is restless. It wants to keep fighting, you feel it [sine]begging[/sine] to journey with you.

But perhaps it's better for it to face the [red]truth[/red]?

- **中文描述**: 一座孤冢上[jitter][aqua]翻腾着汹涌的蓝焰[/aqua][/jitter]…

一名高傲的战士战死于此，灵魂却躁动不安。但它渴望继续战斗，你甚至能感觉到它在[sine]哀求[/sine]你带着它一起踏上征途。

但也许，让它直面[red]真相[/red]才是更好的选择？

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Hungry for Mushrooms / 蘑菇饥渴
- **ID**: `HUNGRY_FOR_MUSHROOMS`
- **原文描述（英文）**: [sine]How long has it been since you ate?
...wait, what's that fantastic smell?[/sine]

Following the scent, you reach a cozy campground with all manner of [green]tasty mushrooms[/green] being cooked! You don't consider the safety of eating these mushrooms because you are so hungry.
(So hungry that you don't notice the dead adventurer)

- **中文描述**: [sine]上一次吃饭是什么时候来着？
…等等，这股奇妙的香味是怎么回事？[/sine]

循着气味，你来到了一处温馨的营地，各种[green]美味的蘑菇[/green]正在火上烤着！由于实在是太饿了，你甚至完全没想过这些玩意儿安不安全。
（你饿得甚至连旁边有个死掉的冒险者这事都没注意到）

- **中文初始选项**:
  - [0] 大蘑菇
  - [1] 芳香蘑菇

### Infested Automaton / 被寄生的自动机械
- **ID**: `INFESTED_AUTOMATON`
- **原文描述（英文）**: There's a chamber filled with [red]dead robots[/red].

A lone automaton still has a [sine][aqua]faintly glowing core[/aqua][/sine] but it's engulfed by a grotesque, organic growth and doesn't respond.

- **中文描述**: 这间密室里堆满了[red]报废的机器人[/red]。

其中一台孤零零的自动机械，其[sine][aqua]核心仍在幽幽发光[/aqua][/sine]，但它已被一种诡异的有机增生组织吞没，毫无响应。

- **中文初始选项**:
  - [0] 学习
  - [1] 触碰核心

### Jungle Maze Adventure / 丛林迷宫奇遇
- **ID**: `JUNGLE_MAZE_ADVENTURE`
- **原文描述（英文）**: In a clearing, you run into a [green]Ragtag Group of Adventurers[/green] looking down and gesturing at a [purple]gigantic maze[/purple].

They offer to join forces with you in an [b]Epic Quest[/b] to loot the riches of this maze. As an experienced adventurer yourself, it's clear this maze is filled with [red][jitter]deadly traps[/jitter][/red] and [orange][sine]guardians[/sine][/orange].

It would be easier if you work together, but then you would have to split the loot.

- **中文描述**: 在一片林间空地上，你撞见了一群[green]寒碜的冒险者小队[/green]，他们正俯瞰着一座[purple]巨型迷宫[/purple]并指手画脚。

他们邀请你入伙，共同开启一场搜刮迷宫财富的[b]史诗级任务[/b]。身为老手，你心知肚明这迷宫里塞满了[red][jitter]致命陷阱[/jitter][/red]和[orange][sine]守卫[/sine][/orange]。

搭伙干活确实容易些，但那样你就得跟人平分战利品了。

- **中文初始选项**:
  - [0] 独自挑战
  - [1] 结伴同行

### Luminous Choir / 冷光合唱团
- **ID**: `LUMINOUS_CHOIR`
- **原文描述（英文）**: You stumble upon a clearing bathed in an unnatural blue glow. Towering mushrooms pulse with bioluminescence, their caps swollen and glistening. As you approach, the fungi begin to emit [sine][purple]haunting, melodic tones[/purple][/sine] that reverberate throughout your chest.

You notice something gleaming, embedded within the largest mushroom's flesh, [sine]pulsing[/sine] in rhythm with the fungal song.

- **中文描述**: 你撞入了一片沐浴在诡异蓝光下的林间空地。高耸的蘑菇随着生物荧光起伏脉动，菌盖肿胀而泛着光泽。当你靠近时，真菌们竟开始发出[sine][purple]凄美而婉转的旋律[/purple][/sine]，这旋律在你的胸腔内激荡。

你注意到，在那个体型最大的蘑菇肉质中嵌入了某种闪光的东西，正随着真菌之歌有节奏地[sine]跃动[/sine]。

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Morphic Grove / 变形灵林谷
- **ID**: `MORPHIC_GROVE`
- **原文描述（英文）**: You enter a grove of [green]crystalline trees[/green], and they begin to [jitter]quiver excitedly[/jitter]!
A chorus of [sine]hellos and welcomes[/sine] bombard you as a group of [aqua]Morphics[/aqua] [jitter]burst[/jitter] forth from trees.

One of the [aqua]Morphics[/aqua] is fidgeting in the corner, clearly not as sociable as the others.

Approach the group or the loner?

- **中文描述**: 你进入了一片[green]结晶树[/green]的林谷，树木开始[jitter]激动地颤抖起来[/jitter]！
一阵阵杂乱的[sine]问候与欢迎声[/sine]在你耳边不断响起，原来是一群[aqua]变形灵[/aqua]从树上[jitter]绽放[/jitter]了出来。

有一只[aqua]变形灵[/aqua]在角落里坐立不安的样子，显然不像其他同类那样善于社交。

你是去找一群变形灵，还是去找落单的那个？

- **中文初始选项**:
  - [0] 大群变形灵
  - [1] 落单变形灵

### Potion Courier / 药水快递员
- **ID**: `POTION_COURIER`
- **原文描述（英文）**: There is a [sine]sour[/sine] smell in the air and shortly after, you find a [gold]Potion Courier[/gold] collapsed on the ground.
Unmoving. Lifeless. [jitter]Dead!?[/jitter]
While their belongings have been ransacked, there is a batch of [green][sine]Foul Smelling Potions[/sine][/green] with a note: "Recipient: Merchant".

- **中文描述**: 空气中传来一阵[sine]浓浓的酸味[/sine]，不一会儿你在附近找到了一个倒在地上的[gold]药水快递员[/gold]。
一动不动，已经没了气。[jitter]死了！？[/jitter]
快递员带着的物品已经被洗劫得差不多了，只有一批[green][sine]很难闻的药水[/sine][/green]好好地放在那里，上面有一张字条：“收件人：商人”。

- **中文初始选项**:
  - [0] 拿走这批药水
  - [1] 洗劫

### Punch Off / 重拳出击
- **ID**: `PUNCH_OFF`
- **原文描述（英文）**: Two [gold]Punch Constructs[/gold] are duking it out and you see some treasure in between them...

Should you try to nab it?

- **中文描述**: 两个[gold]拳击构装体[/gold]正在挥拳互殴，而你看到在它们两个之间有一些财宝……

要不要把财宝顺走？

- **中文初始选项**:
  - [0] 顺走
  - [1] 我能打两个

### Ranwid the Elder / 长者兰伟德
- **ID**: `RANWID_THE_ELDER`
- **原文描述（英文）**: You are approached by the [purple]oldest person you have ever seen[/purple].
[sine]“We meet once more... it's me, Ranwid!”[/sine]

You do not know this man.

- **中文描述**: 一个[purple]你至今为止见过最年迈的人[/purple]走到了你的身前。
[sine]“我们又见面了....是我，兰伟德！”[/sine]

你不认识这个男人。

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Reflections snoitcelfeR / 镜中倒影  影倒中镜
- **ID**: `REFLECTIONS`
- **原文描述（英文）**: What is this? Something feels wrong here...
[sine]...ereh gnorw sleef gnihtemoS ?siht si tahW[/sine]

- **中文描述**: 这是什么？这里感觉不太对劲….
[sine]….劲对太不觉感里这？么什是这[/sine]

- **中文初始选项**:
  - [0] 触碰镜子
  - [1] 打碎

### Relic Trader / 遗物交换商
- **ID**: `RELIC_TRADER`
- **原文描述（英文）**: You turn a corner and suddenly, a shadowy figure is just standing there. He pivots to face you.

“Welcome! What're ya trading?”
The figure inquires as he flares open his cloak to reveal a slew of [sine][purple]suspicious wares[/purple][/sine].

- **中文描述**: 你走过一个拐角，一个黑影突然矗立在前方。他转过身来，正对着你。

“欢迎！你想换点什么吶？”
他开口询问，同时猛地掀开斗篷，露出一大堆[sine][purple]可疑货物[/purple][/sine]。

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Room Full of Cheese / 满屋芝士
- **ID**: `ROOM_FULL_OF_CHEESE`
- **原文描述（英文）**: This room is full of [b][jitter]cheese!!![/jitter][/b]

You look around to see if there are any traps and above you is... more [b]cheese[/b]. The candles in this room appear to be made of [b]cheese[/b]. The furniture is also [b]cheese[/b].
Suddenly, the door behind you slams shut.

It is made of [b]cheese[/b].

- **中文描述**: 这房间里全是[b][jitter]芝士！！！[/jitter][/b]

你环顾四周，想看看有没有陷阱，结果发现头顶上是…更多的[b]芝士[/b]。屋里的蜡烛似乎也是用[b]芝士[/b]做的。家具同样是[b]芝士[/b]。
突然，你身后的门砰地一声关上了。

也是[b]芝士[/b]做的。

- **中文初始选项**:
  - [0] 大快朵颐
  - [1] 仔细翻找

### Sapphire Seed / 蓝宝石种子
- **ID**: `SAPPHIRE_SEED`
- **原文描述（英文）**: You narrowly avoid stepping on a bright, sharp... seed? Its unmistakable color gives away that it's a [aqua]Sapphire Seed[/aqua]! Weren't these incredible seeds supposed to be extinct!?

Legends say that consuming one can greatly [gold]amplify your endurance[/gold], but what if you plant and nourish it instead?

- **中文描述**: 你走路时差点踩上了一块发光的尖锐……种子？它的颜色非常明确地表明了这是一枚[aqua]蓝宝石种子[/aqua]！这些稀有种子不是应该已经灭绝了吗！？

传说里，只要吃下一粒这个种子，就能大幅[gold]强化你的体能[/gold]，但如果你把它种下然后好好养育它，又会怎么样呢？

- **中文初始选项**:
  - [0] 吃下
  - [1] 种植培育

### Self-Help Book / 自助指南
- **ID**: `SELF_HELP_BOOK`
- **原文描述（英文）**: On the floor lies a tome,
“Top 3 Ways to Conquer the Spire”

The book is [red]encrusted with blood[/red] so you find this manuscript highly suspicious.

Will you read it?

- **中文描述**: 地上有一本书：
《征服高塔的3种最佳方法》

这本书上[red]血迹斑斑[/red]，所以你觉得它非常可疑。

你要读它吗？

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Slippery Bridge / 滑脚木桥
- **ID**: `SLIPPERY_BRIDGE`
- **原文描述（英文）**: While crossing a rickety wooden bridge, there is a sudden [blue][jitter]torrent of rain[/jitter][/blue]. [purple][sine]Massive gusts of wind[/sine][/purple] buffet you, threatening to end your journey.

- **中文描述**: 当你穿过一座摇摇欲坠的木桥时，忽然间[blue][jitter]暴雨倾盆[/jitter][/blue]。[purple][sine]阵阵狂风[/sine][/purple]不断向你袭来，似乎要将你的旅程终结与此。

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Spiraling Whirlpool / 螺旋漩涡
- **ID**: `SPIRALING_WHIRLPOOL`
- **原文描述（英文）**: You happen upon a [aqua][sine]Massive Spiraling Whirlpool[/sine][/aqua]. The water swirls with beautiful precision and its surrounding walls are adorned with spiral patterns.

[purple][sine]Around and around... around...
....and around.... what a sight......[/sine][/purple]

What do you do?

- **中文描述**: 你偶遇了一个[aqua][sine]巨大的螺旋漩涡[/sine][/aqua]。水流以精准而优雅的韵律回旋着，四周的墙壁也布满了螺旋纹路。

[purple][sine]转啊，转啊…转…
…转个不停…何等奇景…[/sine][/purple]

你要做什么呢？？

- **中文初始选项**:
  - [0] 观察
  - [1] 饮用

### Spirit Grafter / 灵魂嫁接者
- **ID**: `SPIRIT_GRAFTER`
- **原文描述（英文）**: Above, a cocoon is [jitter]shaking and wriggling[/jitter], about to burst forth!

...it stops wriggling. [orange][sine]The cocoon ignites[/sine][/orange]! [jitter]What is going on?[/jitter]
In a flash of [gold]light[/gold] and [red]flame[/red], a [orange]Spirit Grafter[/orange] rushes into you, threatening to merge with your being to become complete.

- **中文描述**: 在你上方，一个茧正在[jitter]颤动扭曲[/jitter]，似乎就要破茧而出了！

……它停止了扭动。[orange][sine]茧燃烧了起来[/sine][/orange]！[jitter]发生什么事了？[/jitter]
一道[gold]光芒[/gold]与[red]烈焰[/red]闪过，一个[orange]灵魂嫁接者[/orange]向你冲来，试图要与你融为一体，以求完整。

- **中文初始选项**:
  - [0] 接纳
  - [1] 拒绝

### Stone of All Time / 永恒之石
- **ID**: `STONE_OF_ALL_TIME`
- **原文描述（英文）**: A massive boulder sits in the middle of an abandoned courtyard.

A plaque reads:
[gold][b]Stone of All Time[/b][/gold].

Its presence, shape, and adornments indicate that this courtyard was built around this [blue]Stone[/blue] and that it has never moved from this spot.

- **中文描述**: 在一个废弃庭院的当中，有一块巨大的石头。

石头边的一块牌子上写着：
[gold][b]永恒之石[/b][/gold]。

从它的样子、形状和装饰来看，整个庭院都是围绕着这块[blue]石头[/blue]建造出来的，它从来没有离开过那个地点哪怕一次。

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Sunken Treasury / 淹水金库
- **ID**: `SUNKEN_TREASURY`
- **原文描述（英文）**: A path leads you into a partially submerged vault.
There are [blue]2[/blue] [gold]chests[/gold] but only [blue]1[/blue] [jitter][purple]brittle key[/purple][/jitter].

[gold]The First Chest:[/gold] Shaking it, you get jingly-jangly noises. Bit of gold.
[gold]The Second Chest:[/gold] Enormous, ornate, and clearly [red]cursed[/red]. Lots of gold!

- **中文描述**: 一条小路让你找到了一个部分淹水的金库。
你看到里面有[blue]2[/blue]个[gold]箱子[/gold]，但却只有[blue]1[/blue]把[jitter][purple]脆弱的钥匙[/purple][/jitter]。

[gold]第一个箱子：[/gold]你摇晃了一下，能听到叮铃哐啷的轻响，应该是少量金币。
[gold]第二个箱子：[/gold]巨大、精致、而且显然[red]被诅咒了[/red]。里面有很多金币！

- **中文初始选项**:
  - [0] 第一个箱子
  - [1] 第二个箱子

### Symbiote / 共生体
- **ID**: `SYMBIOTE`
- **原文描述（英文）**: Along your travels, you stumble upon a dark, amorphous polyp. You can sense that it is very old, and very evil.

[sine]Approach...[/sine]

- **中文描述**: 在你的旅程中，你遇到了一个黑色无定形的肉块状生物，你能感觉到它非常古老、并且非常邪恶。

[sine]过来吧…[/sine]

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Tablet of Truth / 真理石板
- **ID**: `TABLET_OF_TRUTH`
- **原文描述（英文）**: You defeat a pair of [blue]Guardian Kin[/blue] and make your way into a protected vault.

Inside is a [purple][sine]tablet with familiar inscriptions[/sine][/purple]. Your intuition tells you that this language can be deciphered rather easily... However, once you begin deciphering you'll be locked in.

The tablet gives off a soothing energy and you can smash it to receive its [green]healing energies[/green] instead.

- **中文描述**: 你打败了两只[blue]同族守护者[/blue]，进入了它们守护的藏宝库。

里面有一块[purple][sine]刻着熟悉铭文的石板[/sine][/purple]。你的直觉告诉你，上面的语言可以轻易解读……可是一旦开始解读，你就必须全神贯注。

石板上散发出一种令人安心的能量，你也可以就这样砸碎它，来获取其中的[green]治愈能量[/green]。

- **中文初始选项**:
  - [0] 解读
  - [1] 砸碎

### Tea Master / 茶艺大师
- **ID**: `TEA_MASTER`
- **原文描述（英文）**: You stumble into a dimly lit shack and find yourself immersed in a very peaceful space containing an obsessive variety of tea.

The man inside gestures to a selection of [green]teas[/green], each neatly labeled with a [gold]premium price[/gold].

- **中文描述**: 你不知怎么晃进了一个灯光昏暗的小棚屋，发现里面的空间感觉非常和平，四周摆满了种类繁多的茶叶。

屋子里面的男人做出手势，让你去看一批[green]茶叶[/green]，每一种都有雅致的标签告知你茶叶的[gold]高昂价格[/gold]。

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### The Future of Potions? / 药水的未来？
- **ID**: `THE_FUTURE_OF_POTIONS`
- **原文描述（英文）**: You feel a faint rumbling as you round the corner to discover a gigantic spinning apparatus!
It has several slots which appear to convert liquids into a highly compressed digestible tablet.

The idea of turning these health tonics into a tiny morsel is uncomfortable but perhaps it's good to try new things?

- **中文描述**: 转过拐角，你感受到一阵轻微的轰鸣，随后发现了一台巨大的旋转装置！
它带有几个槽位，似乎能将液体转化为高度压缩的易消化药片。

将这些药水炼成一小片的想法令人有些不适，但或许尝试新事物也不错？

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### The Lantern Key / 灯火钥匙
- **ID**: `THE_LANTERN_KEY`
- **原文描述（英文）**: You come upon a faintly glowing key and go to pick it up.

“Been looking all over for that key! Now if you don't mind...”

 This person looks like bad news but maybe it's not good to be judgy.

- **中文描述**: 你发现了一把发出微光的钥匙，于是将它捡了起来。

“我找这把钥匙可把这里翻了个遍！你要是不介意的话……”

这家伙看起来不是什么好人，但或许你也不该以貌取人。

- **中文初始选项**:
  - [0] 交还钥匙
  - [1] 留下钥匙

### The Legends Were True / 传说是真的
- **ID**: `THE_LEGENDS_WERE_TRUE`
- **原文描述（英文）**: Entering into a pitch dark chamber, a sudden light pierces the darkness from above, just as the door slams behind you.

Illuminated before you appears to be a podium with a map on it. You feel compelled to take it.

Suspicious.

- **中文描述**: 踏入漆黑的密室，一道亮光从上方刺破黑暗，与此同时，身后的门砰然关上。

借着光亮，你看到前方的石台上放着一张地图。你有一种强烈的想要拿走它的冲动。

事出反常。

- **中文初始选项**:
  - [0] 顺走地图
  - [1] 耐心寻找出口

### The Lost Wisp / 迷失鬼火
- **ID**: `LOST_WISP`
- **原文描述（英文）**: In the distance is an odd sight. Mounds and mounds of [sine][red]deceased insects[/red][/sine], surrounding what looks to be a [orange]small glowing mote[/orange].
It seems to be an effective attractant for all manner of bugs but you see it lash out some flames!

You move closer to investigate.

- **中文描述**: 远处有一幕古怪的景象。成堆的[sine][red]昆虫残骸[/red][/sine]层层堆叠，围绕着一个像是[orange]微小的光团[/orange]的东西。
它似乎对各种虫子都有着不俗的吸引力，但你却目睹了它迸发出火焰！

你走近前去一探究竟。

- **中文初始选项**:
  - [0] 抓住这团鬼火
  - [1] 搜索附近的区域

### The Merchant??? / 商人？？？
- **ID**: `FAKE_MERCHANT`
- **原文描述（英文）**: Placeholder

- **中文描述**: 占位符

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### The Round Tea Party / 圆桌茶会
- **ID**: `ROUND_TEA_PARTY`
- **原文描述（英文）**: You find an [orange]invitation[/orange] for a [blue]“Sir Galalot”[/blue] to a tea party and decide to show up in his stead.

Upon entering an unassuming rotunda, you find yourself in the midst of [gold]commanders[/gold], [aqua]generals[/aqua], [red]warlords[/red], and [blue]mercenaries[/blue] having... tea?
The gigantic knight donning a [gold]golden crown[/gold] speaks.

[jitter]“Thou art late for [b]Tea[/b]?!”[/jitter]

- **中文描述**: 你找到了一份送给[blue]“加拉洛特爵士”的[/blue][orange]请柬[/orange]，邀请他参加一次茶会，于是你决定代他前去。

走进其貌不扬的圆形大厅时，你发现这里有一大群[gold]指挥官[/gold]、[aqua]将军[/aqua]、[red]军阀[/red]和[blue]商人[/blue]们，正在……喝着茶？
一个头戴[gold]金冠[/gold]的巨人骑士开口说道：

[jitter]“汝来[b]茶会[/b]竟敢迟到？！”[/jitter]

- **中文初始选项**:
  - [0] 喝杯好茶
  - [1] 挑事斗殴

### The Sunken Statue / 沉没雕像
- **ID**: `SUNKEN_STATUE`
- **原文描述（英文）**: You spot an aged statue half submerged in a pond; its hands gently rest atop a [blue]stone sword[/blue].

Something is also [gold][sine]glittering[/sine][/gold] at the bottom of the pond.
Votive offerings perhaps?

You could really use some money...

- **中文描述**: 你看见一尊古老的雕像，一半沉没在池塘的水中。雕像的手放在一把[blue]石剑[/blue]之上。

同时你还看见，池塘的水底似乎有一些东西在[gold][sine]闪烁[/sine][/gold]着光芒。
或许是人们祈愿投下的金币？

钱可是个好东西……

- **中文初始选项**:
  - [0] 拿起石剑
  - [1] 潜水

### The Trial / 审判
- **ID**: `TRIAL`
- **原文描述（英文）**: You join a line of people entering a massive building.

As you pass under a [gold]golden archway[/gold], horns blare, [jitter]confetti explodes[/jitter], and [sine]streamers glide[/sine] down from the ceiling!

“Entrant [blue]{EntrantNumber}[/blue], you are the [green]DECIDER[/green] for today's [gold]Trial[/gold].”

- **中文描述**: 你排起队，和众人一起进入一栋巨大的建筑物。

当你通过一道[gold]金色的拱门[/gold]时，突然号角响起、[jitter]彩纸爆裂飞散[/jitter]、天花板上还飞出了众多[sine]彩条[/sine]！

“[blue]{EntrantNumber}[/blue]号进入者，你将成为今天[gold]审判[/gold]的[green]判决者[/green]。”

- **中文初始选项**:
  - [0] 接受
  - [1] 拒绝

### This or That? / 这个还是那个？
- **ID**: `THIS_OR_THAT`
- **原文描述（英文）**: Arms suddenly jut out from a nearby hole, clutching a [gold]suspicious bag of riches[/gold] and a clearly [purple]cursed relic[/purple].

[jitter][blue]“This... or That?”[/blue][/jitter]
a scratched-up voice whispers from below.

- **中文描述**: 附近的一个坑洞里突然蹿出一双手臂，一只手紧握着一袋[gold]可疑的财宝[/gold]，另一只则拿着一件明显[purple]被诅咒的遗物[/purple]。

[jitter][blue]“这个……还是那个？”[/blue][/jitter]
一个嘶哑的声音从地下传来，向你低语。

- **中文初始选项**:
  - [0] 这个
  - [1] 那个

### Tinker Time / 打造时间
- **ID**: `TINKER_TIME`
- **原文描述（英文）**: Navigating through an [red][sine]endless sea of corpses[/sine][/red], you find a [orange]mad scientist[/orange] scavenging for various scraps.

“Yes. Hi, hello! You look like a capable fighter... I need a tester for my next [green]atrocity device[/green]! How about it?”

- **中文描述**: 你在翻越[red][sine]数不尽的尸山血海[/sine][/red]之时，遇到了一位正在搜寻破烂的[orange]疯狂科学家[/orange]。

“是的，嗨，你好！你看起来是个有实力的战士……我的下一件[green]暴行装备[/green]正需要一个测试员！你觉得怎么样？”

- **中文初始选项**:
  - [0] 接受

### Trash Heap / 垃圾堆
- **ID**: `TRASH_HEAP`
- **原文描述（英文）**: You discover a towering pile of [red][jitter]scrapped weapons[/jitter][/red], [orange]discarded trinkets[/orange], and [purple][sine]other oddities[/sine][/purple]. The massive pile shifts and rumbles, as if growing from within...

If you scavenge from the surface, you may find some decent things. But if you really get in there, you may find some [gold]exotic treasures[/gold].

- **中文描述**: 你发现了一座如山般堆积的垃圾，里面有着[red][jitter]废旧武器[/jitter][/red]、[orange]遗弃饰品[/orange]和众多[purple][sine]其他古怪玩意儿[/sine][/purple]。垃圾堆摇摆晃动着，仿佛正在从内部不断生长变大……

如果你从表面搜索，应该能找到一些好东西。但如果你努努力扎进垃圾堆里面，那就能找到一些[gold]奇珍异宝[/gold]。

- **中文初始选项**:
  - [0] 扎进垃圾堆
  - [1] 随便拿点垃圾

### Unrest Site / 无休之处
- **ID**: `UNREST_SITE`
- **原文描述（英文）**: You find a secluded [gold]Rest Site[/gold] and start a [orange]fire[/orange] to get some rest.
Or so you thought.

Once the fire is started, it begins to swell, reaching not upwards but [sine]sideways[/sine] towards a grove of [purple]oil-seeping trees[/purple].

Should you try resting anyways?

- **中文描述**: 你找到了一个隔绝的[gold]休息处[/gold]，于是点起[orange]篝火[/orange]打算休息。
结果只是打算而已。

篝火点起后，火光立即胀大起来，不是向上而是向着[sine]侧面[/sine]延伸向一片满是[purple]冒油树木[/purple]的林地。

要不要不管这个情况，就这么继续休息？

- **中文初始选项**:
  - [0] 就这样休息
  - [1] 杀死树木

### War Historian, Repy / 战史学家 付袭
- **ID**: `WAR_HISTORIAN_REPY`
- **原文描述（英文）**: An academic is trapped in a [purple]hanging cage[/purple] next to a [gold]treasure chest[/gold].

The academic is muttering about [sine][orange]“One-Time-Use Keys”[/orange][/sine] while furiously writing something.

Looks like you can unlock the [purple]cage[/purple] or the [gold]chest[/gold].

- **中文描述**: 你看到一个学者被关在一个[purple]吊在空中的笼子[/purple]里，而旁边有一个[gold]大宝箱[/gold]。

那个学者一边咕哝着什么[sine][orange]“一次性的钥匙”[/orange][/sine]，一边拼命地在写着些什么。

看起来你可以二选一，要么打开[purple]笼子[/purple]，要么打开[gold]宝箱[/gold]。

- **中文初始选项**:
  - [0] 打开笼子
  - [1] 打开宝箱

### Waterlogged Scriptorium / 水漫缮写室
- **ID**: `WATERLOGGED_SCRIPTORIUM`
- **原文描述（英文）**: Navigating the murky passages, you stumble upon a [aqua]withered figure[/aqua] working a small shop. The multitude of shelves are [sine]stuffed[/sine] full of damp scrolls and parchment.

Noticing a patron has entered, the [aqua]scribe-shopkeeper[/aqua] snaps to attention and gestures at some [gold]implements[/gold] arranged on a desk.

- **中文描述**: 在昏暗的通道中前行时，你找到了一个小店，里面有一个[aqua]憔悴的员工[/aqua]。四周的书架上[sine]塞满了[/sine]无数潮湿的卷轴与羊皮纸。

[aqua]抄写员兼店主[/aqua]注意到有客人进店，猛地回过神来，为你指了指书桌上摆放着的一些[gold]器具[/gold]。

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Welcome to Wongo's / 欢迎来到旺购百货
- **ID**: `WELCOME_TO_WONGOS`
- **原文描述（英文）**: “Welcome to Wongo's. We have what you want at Wongtastic prices.” says the least enthusiastic clerk you have ever met.

“Peruse our wares and enjoy your time at Wongo's,” they continue, without even looking up.

- **中文描述**: “欢迎来到旺购百货，这里有超旺商品，一定让您越购越旺。”店员说着欢迎词，看起来没有丝毫热情。

“请随意查看我们的商品，祝您在旺购过得愉快。”店员头也不抬地继续说着。

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Wellspring / 泉水
- **ID**: `WELLSPRING`
- **原文描述（英文）**: You trace a [jitter]low rumble[/jitter] to its source and find yourself captivated by a tranquil wellspring. The water is an [sine][aqua]emerald green[/aqua][/sine] with clusters of [sine][gold]glowing motes[/gold][/sine].

It looks inviting. What can possibly go wrong?

- **中文描述**: 你跟着一阵[jitter]低沉的轰鸣声[/jitter]找到了源头：居然是一处平静的泉水，让你心生向往。泉水[sine][aqua]绿如碧玉[/aqua][/sine]，你还能从中看见一些[sine][gold]发光的尘埃[/gold][/sine]。

看起来十分诱人，这不可能出什么差错的吧？

- **中文初始选项**:
  - [0] 装瓶
  - [1] 沐浴

### Whispering Hollow / 低语空谷
- **ID**: `WHISPERING_HOLLOW`
- **原文描述（英文）**: Making your way through a [red]hollow of dead trees[/red] you happen upon a single bone-white tree. Clay baubles hang off its branches, which curve inward like protective ribs.

Such a [purple]creepy tree[/purple]. It whispers.

[sine][blue]...make exchange.....[/blue][/sine]

- **中文描述**: 在通过一个[red]枯木林谷[/red]时，你遇见了一株孤单的白骨般的树。它的枝丫如同肋骨般向内弯曲，枝条上则挂着黏土般的球状物体。

真是棵[purple]瘆人的树[/purple]。它还在发出低语：

[sine][blue]…来做交易…..[/blue][/sine]

- **中文初始选项**:
  - [0] 交换金币
  - [1] 拥抱树木

### Wood Carvings / 木雕
- **ID**: `WOOD_CARVINGS`
- **原文描述（英文）**: You open a dusty box to find a set of 3 elaborate wood carvings:

A bird, snake, and... a torus?

A nearby pedestal has indentations which match the bottom of the carvings. Which carving will you place on the pedestal?

- **中文描述**: 你打开一个尘封的盒子，发现里面有一组3个相当精细的木雕：

一只鸟、一条蛇、还有……一个圆环？

附近有一个基座，上面的凹陷痕迹正与这些木雕底部的形状吻合。你要把哪个木雕放在基座上呢？

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。

### Zen Weaver / 修禅织网者
- **ID**: `ZEN_WEAVER`
- **原文描述（英文）**: You walk through a spider web and [jitter][red]PANIC[/red][/jitter]!
But then, a [sine][blue]wave of calm[/blue][/sine] washes over you...

[sine]“Calm down. I need you to quit fussing up my web.”[/sine]

How did you calm down so quickly?
An [orange]itsy bitsy[/orange] spider abseils down to meet you. It seems willing to impart some techniques for [gold]gold[/gold].

- **中文描述**: 你在行走时撞到了一张蜘蛛网，立即[jitter][red]慌了神[/red][/jitter]！
可是很快，一阵[sine][blue]平静的感觉[/blue][/sine]席卷了你……

[sine]“冷静下来，我需要你不再弄坏我的网。”[/sine]

你怎么会这么快就冷静下来的呢？
一只[orange]小之又小[/orange]的蜘蛛沿着丝线降落下来与你问好，它看起来愿意收取一些[gold]金币[/gold]来分享它的部分技术。

- **中文初始选项**:
  - 知识库未提取固定初始选项；该事件使用动态选项或自定义界面，完整选项与效果见上方摘要。
