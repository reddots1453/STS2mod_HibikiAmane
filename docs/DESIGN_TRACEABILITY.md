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
| 本次变更前DesignDoc基线 | `d067a56` |
| 当前同步状态 | 2026-08-16：已同步DesignDoc `f921bf1`；当前DesignDoc相对该Git基线无未同步修改，MVP代码已完成静态交付审计 |

后续每次同步完成后，应将“当前同步状态”更新为对应提交ID和涉及的需求ID。

## 1.1 2026-08-07语义变更集

| 类型 | 需求ID | 变化 | Plan结论 |
|---|---|---|---|
| ADD | `DOC-MVP-001` | 新增角色MVP与下一轮的强制范围边界 | 新增MVP-0～MVP-3，优先于历史里程碑 |
| CHANGE | `SYS-DES-002A/B` | 将10点满值惩罚与5/8点控制联动拆开 | 10点规则留在MVP；5/8点规则迁移下一轮，MVP提示不显示 |
| CHANGE | `SYS-DES-003A/B` | 将玩家可确认的欲望来源与怪物意图来源拆开 | 前者允许进入MVP内容；后者迁移下一轮 |
| MIGRATE | `SYS-CTL-*`、`SYS-INV-*`、`SYS-DES-INTENT-*` | 控制、挣脱、侵犯及怪物欲望意图整体延期 | 已有框架保留；MVP默认关闭所有运行时影响 |
| MIGRATE | `KW-PORTABLE-001`、`CARD-C-700～799` | 随身与性技延期 | 从MVP卡池、百科统计和验收中排除 |
| MIGRATE | `MON-*`、`ACT4-001`及其他非核心系统 | 新怪物、Boss、第四幕等延期 | MVP复用原版内容和胜利流程 |
| CLARIFY | 已实现延期框架 | “代码存在”不等于“MVP启用” | 不删除代码；建立默认关闭的统一功能门和实验入口 |

影响结论：

- 存档：不删除既有字段或类型；MVP关闭延期模块时仍须安全读取已有字段。
- 战斗生命周期：仅保留堕落、欲望及MVP卡牌所需Hook；怪物意图注入不得运行。
- UI/本地化：MVP欲望提示移除5/8点控制说明；延期关键字不得出现在MVP卡牌上。
- RNG：关闭怪物Adapter后恢复原版意图RNG；路线奖励仍按`SYS-COR-003`消耗RNG。
- 回归：新增其他角色隔离、原版敌人AI、普通商店/Boss奖励/章节流程和存读档验收。
- 本轮只同步文档，不修改代码；实际隔离由`MVP-0`实施。

## 2. 核心机制追踪

| 需求ID | DesignDoc范围 | 设计成熟度 | Plan阶段 | 交付状态 | 验收入口 |
|---|---|---:|---|---|---|
| `DOC-SCOPE-001` | 文档定位与框架范围 | READY | 全阶段 | IMPLEMENTED | 文档审查 |
| `DOC-MVP-001` | 当前角色MVP与下一轮边界 | READY | MVP-0～MVP-5 | IMPLEMENTED；运行时待验收 | DesignDoc/Plan范围审查与MVP统一清单 |
| `DBG-PORT-001`～`DBG-OUT-001` | 类尖塔设计理念 | READY | 内容填充 | READY | 内容评审 |
| `START-001` | 响木天音60HP；普通开局0/0；4打击4防御1变身 | READY | MVP-1 | IMPLEMENTED | 新Run初始状态/牌组/遗物 |
| `START-002` | 外观、解锁、初始遗物选择 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-COR-001` | 堕落范围、UI和角色隔离 | READY | M0/M1 | IMPLEMENTED | M0/M1回归 |
| `SYS-COR-002` | 堕落增减与每Run一次行为 | OPEN | M0～内容填充 | IMPLEMENTED（框架） | 控制台/事件回归 |
| `SYS-COR-003` | 路线奖励概率 | READY | M1 | IMPLEMENTED | M1路线奖励 |
| `SYS-SEA-001` | 封印区、跨战斗快照和火堆移除 | READY | M1 | IMPLEMENTED | REST-3/封印回归 |
| `SYS-COR-004` | 事件门槛和遗物阈值 | OPEN | MVP-3/内容填充 | IMPLEMENTED（5个MVP事件门槛） | 5个原版事件门槛；其余阈值内容继续填充 |
| `ACT4-001` | 第四层任务、路线遗物成长与章节流程 | OPEN（无争议框架进入MVP） | MVP-4 | IMPLEMENTED（流程框架） | 路线状态/商店/火堆/Boss占位/条件进入/存读档 |
| `SYS-DES-001` | 跨战斗欲望资源和UI | READY | M2 | IMPLEMENTED | M2回归 |
| `SYS-DES-002A` | 10点欲望与高潮平复 | READY | MVP | IMPLEMENTED | M2核心回归 |
| `SYS-DES-002B` | 5/8点欲望与控制联动 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-DES-003A` | 卡牌/遗物/事件/火堆等欲望来源 | OPEN | MVP内容填充 | IMPLEMENTED（MVP来源） | MVP卡牌与Run回归 |
| `SYS-DES-003B` | 怪物意图增加欲望 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-TRF-001`～`SYS-TRF-002` | 变身、魔装与魔装保护 | READY | MVP-1 | IMPLEMENTED | 战斗初始化、变身、逐段伤害、层数归零、状态图标、存读档 |
| `SYS-TRF-003` | 色情攻击与魔装联动 | OPEN | 下一轮 | DEFERRED | 不进入MVP验收 |
| `KW-OVERDRAFT-001` | 透支的条件确认与魔装支付 | READY | MVP-1/MVP-2 | IMPLEMENTED | 足额/不足/拒绝/支付至0/保存载入 |
| `SYS-CTL-001` | 控制格挡、Power与挣脱 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-CTL-002` | 弱怪晕眩/强怪低威胁恢复策略 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-INV-001` | 侵犯意图、诅咒来源和晕眩 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-INV-002` | 商店特殊移除和返金 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-DES-INTENT-001` | 怪物增加欲望意图 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-ENC-001` | 战斗中临时附魔 | READY | MVP-2 | IMPLEMENTED | 永久附魔拒绝、战斗副本隔离、正式来源、战斗结束回收 |
| `KW-PORTABLE-001` | 随身 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `KW-CONDEMNATION-001` | 断罪与审判 | READY | MVP-2 | IMPLEMENTED | 层数/审判/清除/保留规则 |
| `KW-PURIFICATION-001` | 净化 | READY | MVP-2 | IMPLEMENTED | 获得/消耗/卡牌联动 |
| `SYS-SCR-001` | 六种圣言及持续Power | READY | MVP-2 | IMPLEMENTED | 六牌、选择生成、触发与升级 |
| `SYS-BLS-001` | Boss后光明/黑暗恩赐 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |

## 3. 内容章节追踪

| ID范围 | 内容分类 | 当前成熟度 | 实现策略 |
|---|---|---:|---|
| `CARD-C-100～199` | 堕落欲望体系 | OPEN/READY（逐卡） | MVP-2；除明确未完成项外全部实现 |
| `CARD-C-200～599、800～899` | 堕落完整机制体系 | OPEN/READY（逐卡） | 除明确未完成项外全部进入MVP-2 |
| `CARD-C-600～699` | 临时附魔体系 | OPEN/READY（逐卡） | 效果完整卡牌进入MVP-2 |
| `CARD-C-700～799` | 性技/控制利用 | DRAFT | 下一轮，不进入MVP |
| `CARD-H-100～199` | 低欲望超模与降欲望 | OPEN | 逐卡填充 |
| `CARD-H-200～599、800～899、950～999` | 圣洁完整机制体系 | OPEN/READY（逐卡） | MVP-2；断罪/净化/圣言/附魔牌进入MVP |
| `CARD-H-600～699` | 断罪体系 | READY（逐卡） | 全部进入MVP-2 |
| `CARD-H-700～799` | 圣言体系 | READY（逐卡） | 六圣言及生成/触发牌全部进入MVP-2 |
| `CARD-H-900～949` | 控制应对与临时附魔 | DRAFT/OPEN | 下一轮，不进入MVP |
| `CARD-H-950～999` | 圣洁体系外卡 | OPEN | 逐卡填充 |
| `CARD-N-100～199` | 天平与过渡数值 | OPEN | 现有样本继续作为回归基线 |
| `CARD-N-200～299` | 打击、防御与随身 | OPEN | 原版机制牌可进MVP；随身/控制牌转下一轮 |
| `CARD-N-300～399` | 路线桥梁卡 | OPEN | 逐卡填充 |
| `ENCH-INFECTION/NECROMANCY/ENERGY-OVERLOAD-001` | 新增附魔 | OPEN/READY | 全部注册；仅规则完整且有MVP来源者启用 |
| `STATUS-001～099` | 通用状态牌 | OPEN/READY | 只注册效果完整条目并实现效果 |
| `CURSE-001～099` | 通用诅咒牌 | OPEN/READY | 全部注册；未完成项无效果/来源 |
| `CURSE-INV-001～099` | 侵犯注入诅咒 | OPEN | 全部注册；MVP无生成来源和未决运行时效果 |
| `RELIC-START-001～099` | 初始遗物 | OPEN | `RELIC-START-003`为MVP默认，重新回归效果 |
| `RELIC-CHAR-001～099` | 角色专属遗物 | DRAFT | 下一轮，不进入MVP |
| `RELIC-EVENT-001～099` | 事件遗物 | OPEN/READY | `EVENT-VANILLA-002`直接依赖项进入MVP，其余延期 |
| `EVENT-VANILLA-001` | 15个原版事件选项堕落变化 | READY | MVP-3；必须在原选项完整成功后结算；IMPLEMENTED |
| `EVENT-VANILLA-002` | 5个原版事件门槛选项 | READY | MVP-3；门槛、奖励、移除/复制/附魔已IMPLEMENTED |
| `EVENT-CARD-001` | 心神宁静事件卡 | READY | MVP-3；IMPLEMENTED；事件专属，不进入普通奖励池 |
| `EVENT-EASTER-001` | 炉石传说彩蛋 | DEPRECATED | 不实现 |
| `MON-001～899` | 普通/精英敌人 | DRAFT | 下一轮；MVP使用原版敌人且不注入Adapter |
| `MON-BOSS-C-001` | 堕落路线最终Boss | DRAFT | 下一轮，不进入MVP |
| `MP-001` | 每玩家资源独立 | READY | 下一轮 | DEFERRED |
| `MP-002～099` | 控制目标、协助挣脱等 | OPEN | 下一轮等待设计补充 |

### MVP卡牌内容交付状态

| ID范围 | 当前交付 | 下一验收 |
|---|---|---|
| `CARD-N-100～399` | IMPLEMENTED：36张MVP合格牌进入中立奖励池 | 百科、奖励、升级、保存读取运行时验收 |
| `CARD-C-100～599、800～899` | IMPLEMENTED：52张MVP合格牌进入堕落奖励池 | 欲望、状态、燃烧、消耗、力量/破碎及附魔逐组验收 |
| `CARD-C-600～799` | IMPLEMENTED/DEFERRED混合：C600完整牌已实现；C700性技/控制依赖延期 | 确认延期牌不出现在百科奖励池 |
| `CARD-H-100～599、800～899、950～999` | IMPLEMENTED：43张MVP合格牌进入圣洁奖励池 | 低欲望、防御、增减益、回合操作逐组验收 |
| `CARD-H-600～799、900～949` | IMPLEMENTED/DEFERRED混合：断罪、六圣言及H930完整附魔牌已实现；H900拘束应对延期 | 确认六圣言生成与延期牌隔离 |

当前已具备完整示例规则的事件条目为：

- `EVENT-001`：光与暗的门扉，`READY`；
- `EVENT-002`：混沌芳香，`READY`；
- `EVENT-003`：低语空谷，`READY`。

初始遗物条目中：

- `RELIC-START-001`、`RELIC-START-002`仍为`OPEN`；
- `RELIC-START-003`已经由当前初始遗物样本实现，状态为`IMPLEMENTED`；
- `RELIC-START-004`双向棱镜已实现，状态为`IMPLEMENTED`，待奖励概率与重掷运行时验收。

## 4. 状态解释

- `IMPLEMENTED（框架）`表示公共扩展点和最小安全样本已经存在，但DesignDoc仍有内容或数值待填充。
- `IMPLEMENTED（样本）`表示机制由测试内容证明可运行，不代表正式内容全部完成。
- 未经游戏内统一手测的实现不得标为`VERIFIED`。
- DesignDoc规则变化后，本表对应项必须回退成熟度/交付状态并生成Plan回修任务。
