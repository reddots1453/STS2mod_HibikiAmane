# DesignDoc需求追踪矩阵

> 本文件记录DesignDoc需求、Plan阶段、实现状态和验收入口之间的映射。
>
> 玩法规则以`DesignDoc.md`为准，技术方案以`PLAN_FRAMEWORK.md`为准。
>
> 更新流程以`DESIGN_CHANGE_PROTOCOL.md`为准。

## 1. Git基线

| 项目 | 值 |
|---|---|
| 初始DesignDoc基线 | `86d749d` |
| 基线日期 | 2026-07-26 |
| 基线用途 | 保存加入索引和治理协议前的908行DesignDoc |
| 当前DesignDoc同步提交 | 工作区待提交（基线`d067a56`） |
| 当前同步状态 | 2026-08-16重大变更已审阅；M9单人MVP已同步并实现 |

后续每次同步完成后，应将“当前同步状态”更新为对应提交ID和涉及的需求ID。

## 2. 核心机制追踪

| 需求ID | DesignDoc范围 | 设计成熟度 | Plan阶段 | 交付状态 | 验收入口 |
|---|---|---:|---|---|---|
| `DOC-SCOPE-001` | 文档定位与框架范围 | READY | 全阶段 | IMPLEMENTED | 文档审查 |
| `DBG-PORT-001`～`DBG-OUT-001` | 类尖塔设计理念 | READY | 内容填充 | READY | 内容评审 |
| `START-001` | 普通/圣女/魅魔初始堕落 | READY | M5 | IMPLEMENTED | `M5_MANUAL_TEST` B |
| `START-002` | 外观、解锁、初始遗物选择 | OPEN | M5/内容填充 | IMPLEMENTED（框架） | `M5_MANUAL_TEST` A/B |
| `SYS-COR-001` | 堕落范围、UI和角色隔离 | READY | M0/M1 | IMPLEMENTED | M0/M1回归 |
| `SYS-COR-002` | 堕落增减与每Run一次行为 | OPEN | M0～内容填充 | IMPLEMENTED（框架） | 控制台/事件回归 |
| `SYS-COR-003` | 路线奖励概率 | READY | M1 | IMPLEMENTED | M1路线奖励 |
| `SYS-SEA-001` | 封印区、跨战斗快照和火堆移除 | READY | M1 | IMPLEMENTED | REST-3/封印回归 |
| `SYS-COR-004` | 事件门槛和遗物阈值 | OPEN | M9/内容填充 | IMPLEMENTED（MVP内容） | `MVP_MANUAL_TEST_CHECKLIST` D/E |
| `ACT4-001` | 第四幕与路线Boss | OPEN | M6 | IMPLEMENTED（框架） | `M6_MANUAL_TEST_CHECKLIST` A～C |
| `SYS-DES-001` | 跨战斗欲望资源和UI | READY | M2 | IMPLEMENTED | M2回归 |
| `SYS-DES-002` | 5/8/10阈值与高潮平复 | READY | M2/M4 | IMPLEMENTED | M2/M4回归 |
| `SYS-DES-003` | 欲望来源与事件门槛 | OPEN | 内容填充 | READY（框架） | 待内容验收 |
| `SYS-CTL-001` | 控制格挡、Power与挣脱 | READY | M4 | IMPLEMENTED | `M4_MANUAL_TEST` B～E |
| `SYS-CTL-002` | 弱怪晕眩/强怪低威胁恢复策略 | OPEN | M4/敌人填充 | IMPLEMENTED（框架） | `M4_MANUAL_TEST` E/G |
| `SYS-INV-001` | 侵犯意图、诅咒来源和晕眩 | READY | M4 | IMPLEMENTED | `M4_MANUAL_TEST` F |
| `SYS-INV-002` | 商店特殊移除和返金 | OPEN | M5 | IMPLEMENTED（框架） | `M5_MANUAL_TEST` C |
| `SYS-DES-INTENT-001` | 怪物增加欲望意图 | OPEN | M4/敌人填充 | IMPLEMENTED（框架） | `M4_MANUAL_TEST` G |
| `SYS-ENC-001` | 战斗中临时附魔 | READY | M3 | IMPLEMENTED | M3临时附魔回归 |
| `KW-PORTABLE-001` | 随身 | OPEN | M3/M4 | IMPLEMENTED（框架） | M3/M4回归 |
| `KW-CONDEMNATION-001` | 断罪与审判 | READY | M3 | IMPLEMENTED | M3断罪回归 |
| `KW-PURIFICATION-001` | 净化 | READY | M3 | IMPLEMENTED | M3净化回归 |
| `SYS-SCR-001` | 六种圣言及持续Power | READY | M3 | IMPLEMENTED（样本） | M3圣言回归 |
| `SYS-BLS-001` | Boss后光明/黑暗恩赐 | OPEN | M5 | IMPLEMENTED（框架） | `M5_MANUAL_TEST` D/E |

## 3. 内容章节追踪

| ID范围 | 内容分类 | 当前成熟度 | 实现策略 |
|---|---|---:|---|
| `CARD-C-100～199` | 堕落欲望体系 | OPEN | 单卡达到READY后逐张分配ID |
| `CARD-C-200～299` | 状态/诅咒利用 | OPEN | 依赖状态与诅咒内容 |
| `CARD-C-300～399` | 生命与最大生命支付 | OPEN | 公共支付框架已实现 |
| `CARD-C-400～499` | 消耗体系 | OPEN | 逐卡填充 |
| `CARD-C-500～599` | 力量体系 | OPEN | 逐卡填充 |
| `CARD-C-600～699` | 临时附魔体系 | OPEN | 公共临时附魔框架已实现 |
| `CARD-C-700～799` | 性技/控制利用 | DRAFT | 不进入正式实现 |
| `CARD-C-800～899` | 堕落体系外卡 | OPEN | 逐卡填充 |
| `CARD-H-100～199` | 低欲望超模与降欲望 | OPEN | 逐卡填充 |
| `CARD-H-200～299` | 防御体系 | OPEN | 逐卡填充 |
| `CARD-H-300～399` | 自身负面状态利用 | OPEN | 逐卡填充 |
| `CARD-H-400～499` | 敌方负面状态利用 | OPEN | 逐卡填充 |
| `CARD-H-500～599` | 自身增益利用 | OPEN | 逐卡填充 |
| `CARD-H-600～699` | 断罪体系 | OPEN | 公共机制已实现 |
| `CARD-H-700～799` | 圣言体系 | OPEN | 六种样本机制已实现 |
| `CARD-H-800～899` | 大卡组补强 | OPEN | 逐卡填充 |
| `CARD-H-900～949` | 控制应对与临时附魔 | DRAFT/OPEN | 不完整条目暂不实现 |
| `CARD-H-950～999` | 圣洁体系外卡 | OPEN | 逐卡填充 |
| `CARD-N-100～199` | 天平与过渡数值 | OPEN | 现有样本继续作为回归基线 |
| `CARD-N-200～299` | 打击、防御与随身 | OPEN | 现有样本继续作为回归基线 |
| `CARD-N-300～399` | 路线桥梁卡 | OPEN | 逐卡填充 |
| `STATUS-001～099` | 通用状态牌 | DRAFT/OPEN | 完整条目达到READY后实现 |
| `CURSE-001～099` | 通用诅咒牌 | DRAFT/OPEN | 完整条目达到READY后实现 |
| `CURSE-INV-001～099` | 侵犯注入诅咒 | OPEN | “精液”样本已实现 |
| `RELIC-START-001～099` | 初始遗物 | OPEN | `RELIC-START-003/004`已实现；001/002待设计 |
| `RELIC-CHAR-001～099` | 角色专属遗物 | DRAFT | 不进入正式实现 |
| `RELIC-EVENT-001～099` | 事件遗物 | OPEN | `RELIC-EVENT-001/002`已实现，待运行时验收 |
| `EVENT-001～899` | 正式事件 | OPEN | `EVENT-001/002/003`已通过原版事件扩展实现 |
| `EVENT-EASTER-001` | 炉石传说彩蛋 | DEPRECATED | 不实现 |
| `MON-001～899` | 普通/精英敌人 | DRAFT | 先使用M4敌人Adapter框架 |
| `MON-BOSS-C-001` | 堕落路线最终Boss | DRAFT | 不进入正式实现 |
| `MP-001` | 每玩家资源独立 | READY | M7 | READY |
| `MP-002～099` | 控制目标、协助挣脱等 | OPEN | 等待设计补充 |

### M8卡牌内容交付状态

| ID范围 | 当前交付 | 下一验收 |
|---|---|---|
| `CARD-N-100～399` | M8.1已实现全部已命名闭合条目；名称缺失的“拾起时复制”卡待M8.2技术ID | 百科、奖励池、基本功标签与融汇免费化 |
| `CARD-C-100～699、800～899` | M8.1已实现首批已命名闭合条目；力量的代价已同步新版数值；其余复杂条目待M8.2/M8.3 | 欲望支付、消耗监听、力量恢复、复制入堆 |
| `CARD-C-700～799` | 未实现 | 维持`DRAFT`，等待DesignDoc转为可实施状态 |
| `CARD-H-100～899、950～999` | M8.1已实现首批已命名闭合条目及六张圣言；新版两张防御牌与终末审判已实现；其余复杂条目待M8.2/M8.3 | `MVP_MANUAL_TEST_CHECKLIST` C及圣言回归 |
| `CARD-H-900～929` | 未实现 | 描述未完成，维持`DRAFT` |

当前已具备完整示例规则的事件条目为：

- `EVENT-001`：光与暗的门扉，`READY`；
- `EVENT-002`：混沌芳香，`READY`；
- `EVENT-003`：低语空谷，`READY`。

初始遗物条目中：

- `RELIC-START-001`、`RELIC-START-002`仍为`OPEN`；
- `RELIC-START-003`已经由当前初始遗物样本实现，状态为`IMPLEMENTED`；
- `RELIC-START-004`规则完整，状态为`READY`。

## 4. 状态解释

- `IMPLEMENTED（框架）`表示公共扩展点和最小安全样本已经存在，但DesignDoc仍有内容或数值待填充。
- `IMPLEMENTED（样本）`表示机制由测试内容证明可运行，不代表正式内容全部完成。
- 未经游戏内统一手测的实现不得标为`VERIFIED`。
- DesignDoc规则变化后，本表对应项必须回退成熟度/交付状态并生成Plan回修任务。
