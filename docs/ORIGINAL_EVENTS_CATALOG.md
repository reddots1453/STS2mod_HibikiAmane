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
| `ABYSSAL_BATHS` | 深渊浴场 | 投身其中：获得2最大生命并失去3生命；之后可反复逗留，每次获得2最大生命，生命损失依次增加1。敬而远之：恢复10生命。 | 投身其中仅首次选择时`【MS·堕落+1】`；敬而远之`【MS·堕落-1】`。`【MS·新增选项｜堕落≤-3】净身：恢复全部生命。` `【MS·新增选项｜堕落≥+3】沉溺：连续结算前三次浸泡，共获得6最大生命并失去12生命。` |
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

以下内容保留知识库的双语标题、英文叙事描述和已提取的中文初始选项。无初始选项的条目通常使用动态选项或自定义界面，完整结算以上表为准。

### Abyssal Baths / 深渊浴场
- **ID**: `ABYSSAL_BATHS`
- **Description**: You discover a secluded chamber.

Steam rises from bubbling pools of hot liquid that shifts colors with hypnotic rhythm. Barnacled growths hang from the ceiling, dripping viscous fluid that hisses and writhes when it touches the surface. The air feels heavy, laden with [blue]salt[/blue] and something [green]unmistakably organic[/green].

As you approach the edge of the largest pool, the liquid ripples. The waters bubble more intensely as if anticipating your entry.
- **Options**:
  - [0] 投身其中
  - [1] 敬而远之

### Amalgamator / 熔合者
- **ID**: `AMALGAMATOR`
- **Description**: [b][jitter]CLANG! CLANG!!![/jitter][/b]

You hear the echoes of metal upon metal on the otherside of a wall...

As you press your head against the wall to listen in—the wall opens up, revealing a [orange]hulking six-armed figure[/orange] toiling away.
Its “face” is a [gold][sine]swirling vortex of glowing sigils[/sine][/gold] that shift about.

“One with an [aqua]Ascending Spirit[/aqua] has found my workshop? Let's get [jitter]COMBINING[/jitter]!”
- **Options**:
  - [0] 融合打击
  - [1] 融合防御

### Aroma of Chaos / 混沌芳香
- **ID**: `AROMA_OF_CHAOS`
- **Description**: Upon trudging through a thick underbrush and making it into a clearing, you're hit with an unplaceable nostalgia.

A mix of [purple]floral scents[/purple], [green]putrid decay[/green], and [orange]something else entirely[/orange]. The scent grows stronger with each step, and you feel as though the world around you is [sine]warping and twisting[/sine].

Overcome with a sense of [sine][rainbow freq=0.3 sat=0.8 val=1]shifting chaos[/rainbow][/sine], you begin to lose your sense of self.
- **Options**:
  - [0] 放任自流
  - [1] 维持理智

### Battleworn Dummy / 战痕累累的训练假人
- **ID**: `BATTLEWORN_DUMMY`
- **Description**: As you approach, it begins to rumble and fizzle and lights up brilliantly!

“[jitter]BZZZT![/jitter] TIME TO TRAIN!!! YOU HAVE [blue]3 TURNS[/blue] TO DEFEAT ME!
CHOOSE A SETTING OR FACE [red]LETHAL HUMILIATION[/red].
Here are the options:”

After the terrifying message, the dummy carefully reads out detailed instructions.

What do you choose?
- **Options**:
  - [0] 第1档
  - [1] 第2档
  - [2] 第3档

### Brain Leech / 脑蛭
- **ID**: `BRAIN_LEECH`
- **Description**: [jitter]*thunk*[/jitter]

A sharp pain is felt atop your head and a thought stabs into your mind.

[purple][sine]“SHARE KNOWLEDGE???”[/sine][/purple]

You are unsure how to proceed...
- **Options**:
  - [0] 分享知识
  - [1] 把它扯下来

### Bugslayer / 害虫杀手
- **ID**: `BUGSLAYER`
- **Description**: As you're fending off a [jitter][red]swarm of aggressive insects[/red][/jitter] you suddenly notice a [gold]Mighty Rugged Warrior-Man[/gold] fighting beside you this whole time!

The insects, sensing defeat, scatter. You turn to your fellow fighter.
“Would you like any tips on exterminating these pests?”

How polite. You nod and accept his offer.
- **Options**:
  - [0] 学习杀灭的技巧
  - [1] 学习压扁的技巧

### Byrdonis Nest / 多尼斯异鸟巢
- **ID**: `BYRDONIS_NEST`
- **Description**: You spot an enormous [jitter][red]Shambling Beast[/red][/jitter] chase off an injured [green][sine]Green Byrd[/sine][/green].

In the alcove the byrd fled from, a [gold]single unguarded egg[/gold] lies abandoned.

[sine]Your stomach gurgles...[/sine]
- **Options**:
  - [0] 吃掉这颗蛋
  - [1] 带走这颗蛋

### Colorful Philosophers / 色彩哲学家
- **ID**: `COLORFUL_PHILOSOPHERS`
- **Description**: Before you is a rather epic sight.
You see 3 different colored statues towering over a dais, having a [jitter][red]heated debate[/red][/jitter] over the philosophical implications of color.

As you listen in, you get a sense that the most important question at hand is which color truly is [gold]THE BEST[/gold].

You chime in with your thoughts.

### Colossal Flower / 巨大花卉
- **ID**: `COLOSSAL_FLOWER`
- **Description**: There is a [green]colossal flower[/green] growing atop a [red][jitter]mountain of bones[/jitter][/red].

As its [sine][rainbow freq=0.3 sat=0.8 val=1]color-shifting petals[/rainbow][/sine] pulsate, you sense there is a [aqua]Powerful Cluster of Pollen[/aqua] in the center but the undulating petals are [red]razor sharp[/red] and unpredictable.

You could grab some of the [gold]golden nectar[/gold], but reaching the prize in the center is so tempting...
- **Options**:
  - [0] 采集花蜜
  - [1] 深入探索

### Crystal Sphere / 水晶球
- **ID**: `CRYSTAL_SPHERE`
- **Description**: [sine][blue]“I predicted you'd enter...!”[/blue][/sine]

A [jitter]raspy[/jitter] voice calls out as you enter a [purple]mystic hut[/purple].

[sine][blue]“Your destiny has brought you here. We must uncover your future and fortunes so you can SAVE US ALL!!”[/blue][/sine]
“Okay, here are the options for the [gold]Crystal Sphere reading[/gold]. Be sure to sign this waiver as well,” she says while bringing out pen and parchment.
- **Options**:
  - [0] 揭幕未来
  - [1] 分期付款

### Dense Vegetation / 茂密的植被
- **ID**: `DENSE_VEGETATION`
- **Description**: Having taken the wrong path for a good while, you find yourself in a thick jungle of [green]ferns[/green], [green]shrubs[/green], and [green]vines[/green]. Especially [green]vines[/green]. Exhaustion sets in, and a dark thought comes to mind:

[sine][purple]“You are lost, unprepared, and the inevitability of death is approaching.”[/purple][/sine]

What do you do?
- **Options**:
  - [0] 坚持跋涉
  - [1] 休息

### Doll Room / 玩偶室
- **ID**: `DOLL_ROOM`
- **Description**: You enter a hidden room...

It's packed with an array of dolls. Each are unique, their expressions ranging from [green]joy[/green] to [purple]sorrow[/purple], their attire spanning centuries and realms beyond your own.

A [sine][blue]chorus of whispers[/blue][/sine] grows louder and louder until [jitter][red]the dolls are shouting[/red][/jitter] over each other and calling for you...

You must choose one and leave immediately.
- **Options**:
  - [0] 随机拿走一尊
  - [1] 花点时间慢慢来
  - [2] 仔细检查然后挑选最好的那个

### Doors of Light and Dark / 光与暗的门扉
- **ID**: `DOORS_OF_LIGHT_AND_DARK`
- **Description**: A doorway existed where none existed moments before...

You enter to find two shimmering doorways and a [gold]well-dressed doorman[/gold].
“Finally, a visitor! Please please! Choose a door, any door!”

[blue]Are we still in the Spire? How is this place so clean?[/blue]
- **Options**:
  - [0] 光之门
  - [1] 暗之门

### Drowning Beacon / 淹水灯塔
- **ID**: `DROWNING_BEACON`
- **Description**: Crossing a dreamscape of rocks and still waters, you emerge to face a [sine]sinking lighthouse[/sine] emitting [purple]anti-light[/purple].

You can bottle up the [aqua][sine]eerily-glowing water[/sine][/aqua] that has pooled here or climb up the dilapidated structure to retrieve the [gold]lens[/gold].
- **Options**:
  - [0] 装瓶
  - [1] 攀爬

### Endless Conveyor / 无尽传送带
- **ID**: `ENDLESS_CONVEYOR`
- **Description**: You lumber into a shack with a bright but crooked sign that reads:
[aqua]“ENDLESS FEAST - PAY WHAT YOU HUNGER!”[/aqua]

Inside, a [orange]willowy multi-armed chef[/orange] is deftly preparing [green]bites of food[/green] and placing them onto a [sine]winding chitinous belt[/sine].

One of the chef's arms point to a sign:
[blue]35[/blue] [gold]Gold[/gold] each

### Field of Man-Sized Holes / 人形洞穴之地
- **ID**: `FIELD_OF_MAN_SIZED_HOLES`
- **Description**: The sudden sight of a massive field, [jitter]gouged with large outlines of bodies[/jitter], stops you in your tracks.

One of these outlines matches your own.

[sine][orange]It's perfect...[/orange][/sine]
- **Options**:
  - [0] 抵抗诱惑
  - [1] 进入你的洞

### Grave of the Forgotten / 遗忘之墓
- **ID**: `GRAVE_OF_THE_FORGOTTEN`
- **Description**: There is a single grave with a [jitter][aqua]roiling blue flame[/aqua][/jitter]...

A prideful warrior died in battle but its soul is restless. It wants to keep fighting, you feel it [sine]begging[/sine] to journey with you.

But perhaps it's better for it to face the [red]truth[/red]?

### Hungry for Mushrooms / 蘑菇饥渴
- **ID**: `HUNGRY_FOR_MUSHROOMS`
- **Description**: [sine]How long has it been since you ate?
...wait, what's that fantastic smell?[/sine]

Following the scent, you reach a cozy campground with all manner of [green]tasty mushrooms[/green] being cooked! You don't consider the safety of eating these mushrooms because you are so hungry.
(So hungry that you don't notice the dead adventurer)
- **Options**:
  - [0] 大蘑菇
  - [1] 芳香蘑菇

### Infested Automaton / 被寄生的自动机械
- **ID**: `INFESTED_AUTOMATON`
- **Description**: There's a chamber filled with [red]dead robots[/red].

A lone automaton still has a [sine][aqua]faintly glowing core[/aqua][/sine] but it's engulfed by a grotesque, organic growth and doesn't respond.
- **Options**:
  - [0] 学习
  - [1] 触碰核心

### Jungle Maze Adventure / 丛林迷宫奇遇
- **ID**: `JUNGLE_MAZE_ADVENTURE`
- **Description**: In a clearing, you run into a [green]Ragtag Group of Adventurers[/green] looking down and gesturing at a [purple]gigantic maze[/purple].

They offer to join forces with you in an [b]Epic Quest[/b] to loot the riches of this maze. As an experienced adventurer yourself, it's clear this maze is filled with [red][jitter]deadly traps[/jitter][/red] and [orange][sine]guardians[/sine][/orange].

It would be easier if you work together, but then you would have to split the loot.
- **Options**:
  - [0] 独自挑战
  - [1] 结伴同行

### Luminous Choir / 冷光合唱团
- **ID**: `LUMINOUS_CHOIR`
- **Description**: You stumble upon a clearing bathed in an unnatural blue glow. Towering mushrooms pulse with bioluminescence, their caps swollen and glistening. As you approach, the fungi begin to emit [sine][purple]haunting, melodic tones[/purple][/sine] that reverberate throughout your chest.

You notice something gleaming, embedded within the largest mushroom's flesh, [sine]pulsing[/sine] in rhythm with the fungal song.

### Morphic Grove / 变形灵林谷
- **ID**: `MORPHIC_GROVE`
- **Description**: You enter a grove of [green]crystalline trees[/green], and they begin to [jitter]quiver excitedly[/jitter]!
A chorus of [sine]hellos and welcomes[/sine] bombard you as a group of [aqua]Morphics[/aqua] [jitter]burst[/jitter] forth from trees.

One of the [aqua]Morphics[/aqua] is fidgeting in the corner, clearly not as sociable as the others.

Approach the group or the loner?
- **Options**:
  - [0] 大群变形灵
  - [1] 落单变形灵

### Potion Courier / 药水快递员
- **ID**: `POTION_COURIER`
- **Description**: There is a [sine]sour[/sine] smell in the air and shortly after, you find a [gold]Potion Courier[/gold] collapsed on the ground.
Unmoving. Lifeless. [jitter]Dead!?[/jitter]
While their belongings have been ransacked, there is a batch of [green][sine]Foul Smelling Potions[/sine][/green] with a note: "Recipient: Merchant".
- **Options**:
  - [0] 拿走这批药水
  - [1] 洗劫

### Punch Off / 重拳出击
- **ID**: `PUNCH_OFF`
- **Description**: Two [gold]Punch Constructs[/gold] are duking it out and you see some treasure in between them...

Should you try to nab it?
- **Options**:
  - [0] 顺走
  - [1] 我能打两个

### Ranwid the Elder / 长者兰伟德
- **ID**: `RANWID_THE_ELDER`
- **Description**: You are approached by the [purple]oldest person you have ever seen[/purple].
[sine]“We meet once more... it's me, Ranwid!”[/sine]

You do not know this man.

### Reflections snoitcelfeR / 镜中倒影  影倒中镜
- **ID**: `REFLECTIONS`
- **Description**: What is this? Something feels wrong here...
[sine]...ereh gnorw sleef gnihtemoS ?siht si tahW[/sine]
- **Options**:
  - [0] 触碰镜子
  - [1] 打碎

### Relic Trader / 遗物交换商
- **ID**: `RELIC_TRADER`
- **Description**: You turn a corner and suddenly, a shadowy figure is just standing there. He pivots to face you.

“Welcome! What're ya trading?”
The figure inquires as he flares open his cloak to reveal a slew of [sine][purple]suspicious wares[/purple][/sine].

### Room Full of Cheese / 满屋芝士
- **ID**: `ROOM_FULL_OF_CHEESE`
- **Description**: This room is full of [b][jitter]cheese!!![/jitter][/b]

You look around to see if there are any traps and above you is... more [b]cheese[/b]. The candles in this room appear to be made of [b]cheese[/b]. The furniture is also [b]cheese[/b].
Suddenly, the door behind you slams shut.

It is made of [b]cheese[/b].
- **Options**:
  - [0] 大快朵颐
  - [1] 仔细翻找

### Sapphire Seed / 蓝宝石种子
- **ID**: `SAPPHIRE_SEED`
- **Description**: You narrowly avoid stepping on a bright, sharp... seed? Its unmistakable color gives away that it's a [aqua]Sapphire Seed[/aqua]! Weren't these incredible seeds supposed to be extinct!?

Legends say that consuming one can greatly [gold]amplify your endurance[/gold], but what if you plant and nourish it instead?
- **Options**:
  - [0] 吃下
  - [1] 种植培育

### Self-Help Book / 自助指南
- **ID**: `SELF_HELP_BOOK`
- **Description**: On the floor lies a tome,
“Top 3 Ways to Conquer the Spire”

The book is [red]encrusted with blood[/red] so you find this manuscript highly suspicious.

Will you read it?

### Slippery Bridge / 滑脚木桥
- **ID**: `SLIPPERY_BRIDGE`
- **Description**: While crossing a rickety wooden bridge, there is a sudden [blue][jitter]torrent of rain[/jitter][/blue]. [purple][sine]Massive gusts of wind[/sine][/purple] buffet you, threatening to end your journey.

### Spiraling Whirlpool / 螺旋漩涡
- **ID**: `SPIRALING_WHIRLPOOL`
- **Description**: You happen upon a [aqua][sine]Massive Spiraling Whirlpool[/sine][/aqua]. The water swirls with beautiful precision and its surrounding walls are adorned with spiral patterns.

[purple][sine]Around and around... around...
....and around.... what a sight......[/sine][/purple]

What do you do?
- **Options**:
  - [0] 观察
  - [1] 饮用

### Spirit Grafter / 灵魂嫁接者
- **ID**: `SPIRIT_GRAFTER`
- **Description**: Above, a cocoon is [jitter]shaking and wriggling[/jitter], about to burst forth!

...it stops wriggling. [orange][sine]The cocoon ignites[/sine][/orange]! [jitter]What is going on?[/jitter]
In a flash of [gold]light[/gold] and [red]flame[/red], a [orange]Spirit Grafter[/orange] rushes into you, threatening to merge with your being to become complete.
- **Options**:
  - [0] 接纳
  - [1] 拒绝

### Stone of All Time / 永恒之石
- **ID**: `STONE_OF_ALL_TIME`
- **Description**: A massive boulder sits in the middle of an abandoned courtyard.

A plaque reads:
[gold][b]Stone of All Time[/b][/gold].

Its presence, shape, and adornments indicate that this courtyard was built around this [blue]Stone[/blue] and that it has never moved from this spot.

### Sunken Treasury / 淹水金库
- **ID**: `SUNKEN_TREASURY`
- **Description**: A path leads you into a partially submerged vault.
There are [blue]2[/blue] [gold]chests[/gold] but only [blue]1[/blue] [jitter][purple]brittle key[/purple][/jitter].

[gold]The First Chest:[/gold] Shaking it, you get jingly-jangly noises. Bit of gold.
[gold]The Second Chest:[/gold] Enormous, ornate, and clearly [red]cursed[/red]. Lots of gold!
- **Options**:
  - [0] 第一个箱子
  - [1] 第二个箱子

### Symbiote / 共生体
- **ID**: `SYMBIOTE`
- **Description**: Along your travels, you stumble upon a dark, amorphous polyp. You can sense that it is very old, and very evil.

[sine]Approach...[/sine]

### Tablet of Truth / 真理石板
- **ID**: `TABLET_OF_TRUTH`
- **Description**: You defeat a pair of [blue]Guardian Kin[/blue] and make your way into a protected vault.

Inside is a [purple][sine]tablet with familiar inscriptions[/sine][/purple]. Your intuition tells you that this language can be deciphered rather easily... However, once you begin deciphering you'll be locked in.

The tablet gives off a soothing energy and you can smash it to receive its [green]healing energies[/green] instead.
- **Options**:
  - [0] 解读
  - [1] 砸碎

### Tea Master / 茶艺大师
- **ID**: `TEA_MASTER`
- **Description**: You stumble into a dimly lit shack and find yourself immersed in a very peaceful space containing an obsessive variety of tea.

The man inside gestures to a selection of [green]teas[/green], each neatly labeled with a [gold]premium price[/gold].

### The Future of Potions? / 药水的未来？
- **ID**: `THE_FUTURE_OF_POTIONS`
- **Description**: You feel a faint rumbling as you round the corner to discover a gigantic spinning apparatus!
It has several slots which appear to convert liquids into a highly compressed digestible tablet.

The idea of turning these health tonics into a tiny morsel is uncomfortable but perhaps it's good to try new things?

### The Lantern Key / 灯火钥匙
- **ID**: `THE_LANTERN_KEY`
- **Description**: You come upon a faintly glowing key and go to pick it up.

“Been looking all over for that key! Now if you don't mind...”

 This person looks like bad news but maybe it's not good to be judgy.
- **Options**:
  - [0] 交还钥匙
  - [1] 留下钥匙

### The Legends Were True / 传说是真的
- **ID**: `THE_LEGENDS_WERE_TRUE`
- **Description**: Entering into a pitch dark chamber, a sudden light pierces the darkness from above, just as the door slams behind you.

Illuminated before you appears to be a podium with a map on it. You feel compelled to take it.

Suspicious.
- **Options**:
  - [0] 顺走地图
  - [1] 耐心寻找出口

### The Lost Wisp / 迷失鬼火
- **ID**: `LOST_WISP`
- **Description**: In the distance is an odd sight. Mounds and mounds of [sine][red]deceased insects[/red][/sine], surrounding what looks to be a [orange]small glowing mote[/orange].
It seems to be an effective attractant for all manner of bugs but you see it lash out some flames!

You move closer to investigate.
- **Options**:
  - [0] 抓住这团鬼火
  - [1] 搜索附近的区域

### The Merchant??? / 商人？？？
- **ID**: `FAKE_MERCHANT`
- **Description**: Placeholder

### The Round Tea Party / 圆桌茶会
- **ID**: `ROUND_TEA_PARTY`
- **Description**: You find an [orange]invitation[/orange] for a [blue]“Sir Galalot”[/blue] to a tea party and decide to show up in his stead.

Upon entering an unassuming rotunda, you find yourself in the midst of [gold]commanders[/gold], [aqua]generals[/aqua], [red]warlords[/red], and [blue]mercenaries[/blue] having... tea?
The gigantic knight donning a [gold]golden crown[/gold] speaks.

[jitter]“Thou art late for [b]Tea[/b]?!”[/jitter]
- **Options**:
  - [0] 喝杯好茶
  - [1] 挑事斗殴

### The Sunken Statue / 沉没雕像
- **ID**: `SUNKEN_STATUE`
- **Description**: You spot an aged statue half submerged in a pond; its hands gently rest atop a [blue]stone sword[/blue].

Something is also [gold][sine]glittering[/sine][/gold] at the bottom of the pond.
Votive offerings perhaps?

You could really use some money...
- **Options**:
  - [0] 拿起石剑
  - [1] 潜水

### The Trial / 审判
- **ID**: `TRIAL`
- **Description**: You join a line of people entering a massive building.

As you pass under a [gold]golden archway[/gold], horns blare, [jitter]confetti explodes[/jitter], and [sine]streamers glide[/sine] down from the ceiling!

“Entrant [blue]{EntrantNumber}[/blue], you are the [green]DECIDER[/green] for today's [gold]Trial[/gold].”
- **Options**:
  - [0] 接受
  - [1] 拒绝

### This or That? / 这个还是那个？
- **ID**: `THIS_OR_THAT`
- **Description**: Arms suddenly jut out from a nearby hole, clutching a [gold]suspicious bag of riches[/gold] and a clearly [purple]cursed relic[/purple].

[jitter][blue]“This... or That?”[/blue][/jitter]
a scratched-up voice whispers from below.
- **Options**:
  - [0] 这个
  - [1] 那个

### Tinker Time / 打造时间
- **ID**: `TINKER_TIME`
- **Description**: Navigating through an [red][sine]endless sea of corpses[/sine][/red], you find a [orange]mad scientist[/orange] scavenging for various scraps.

“Yes. Hi, hello! You look like a capable fighter... I need a tester for my next [green]atrocity device[/green]! How about it?”
- **Options**:
  - [0] 接受

### Trash Heap / 垃圾堆
- **ID**: `TRASH_HEAP`
- **Description**: You discover a towering pile of [red][jitter]scrapped weapons[/jitter][/red], [orange]discarded trinkets[/orange], and [purple][sine]other oddities[/sine][/purple]. The massive pile shifts and rumbles, as if growing from within...

If you scavenge from the surface, you may find some decent things. But if you really get in there, you may find some [gold]exotic treasures[/gold].
- **Options**:
  - [0] 扎进垃圾堆
  - [1] 随便拿点垃圾

### Unrest Site / 无休之处
- **ID**: `UNREST_SITE`
- **Description**: You find a secluded [gold]Rest Site[/gold] and start a [orange]fire[/orange] to get some rest.
Or so you thought.

Once the fire is started, it begins to swell, reaching not upwards but [sine]sideways[/sine] towards a grove of [purple]oil-seeping trees[/purple].

Should you try resting anyways?
- **Options**:
  - [0] 就这样休息
  - [1] 杀死树木

### War Historian, Repy / 战史学家 付袭
- **ID**: `WAR_HISTORIAN_REPY`
- **Description**: An academic is trapped in a [purple]hanging cage[/purple] next to a [gold]treasure chest[/gold].

The academic is muttering about [sine][orange]“One-Time-Use Keys”[/orange][/sine] while furiously writing something.

Looks like you can unlock the [purple]cage[/purple] or the [gold]chest[/gold].
- **Options**:
  - [0] 打开笼子
  - [1] 打开宝箱

### Waterlogged Scriptorium / 水漫缮写室
- **ID**: `WATERLOGGED_SCRIPTORIUM`
- **Description**: Navigating the murky passages, you stumble upon a [aqua]withered figure[/aqua] working a small shop. The multitude of shelves are [sine]stuffed[/sine] full of damp scrolls and parchment.

Noticing a patron has entered, the [aqua]scribe-shopkeeper[/aqua] snaps to attention and gestures at some [gold]implements[/gold] arranged on a desk.

### Welcome to Wongo's / 欢迎来到旺购百货
- **ID**: `WELCOME_TO_WONGOS`
- **Description**: “Welcome to Wongo's. We have what you want at Wongtastic prices.” says the least enthusiastic clerk you have ever met.

“Peruse our wares and enjoy your time at Wongo's,” they continue, without even looking up.

### Wellspring / 泉水
- **ID**: `WELLSPRING`
- **Description**: You trace a [jitter]low rumble[/jitter] to its source and find yourself captivated by a tranquil wellspring. The water is an [sine][aqua]emerald green[/aqua][/sine] with clusters of [sine][gold]glowing motes[/gold][/sine].

It looks inviting. What can possibly go wrong?
- **Options**:
  - [0] 装瓶
  - [1] 沐浴

### Whispering Hollow / 低语空谷
- **ID**: `WHISPERING_HOLLOW`
- **Description**: Making your way through a [red]hollow of dead trees[/red] you happen upon a single bone-white tree. Clay baubles hang off its branches, which curve inward like protective ribs.

Such a [purple]creepy tree[/purple]. It whispers.

[sine][blue]...make exchange.....[/blue][/sine]
- **Options**:
  - [0] 交换金币
  - [1] 拥抱树木

### Wood Carvings / 木雕
- **ID**: `WOOD_CARVINGS`
- **Description**: You open a dusty box to find a set of 3 elaborate wood carvings:

A bird, snake, and... a torus?

A nearby pedestal has indentations which match the bottom of the carvings. Which carving will you place on the pedestal?

### Zen Weaver / 修禅织网者
- **ID**: `ZEN_WEAVER`
- **Description**: You walk through a spider web and [jitter][red]PANIC[/red][/jitter]!
But then, a [sine][blue]wave of calm[/blue][/sine] washes over you...

[sine]“Calm down. I need you to quit fussing up my web.”[/sine]

How did you calm down so quickly?
An [orange]itsy bitsy[/orange] spider abseils down to meet you. It seems willing to impart some techniques for [gold]gold[/gold].
