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
| 当前同步状态 | 2026-09-27：全量设计差异已审阅；Plan §25登记范围与Q1～Q11，等待澄清后实施；本表历史IMPLEMENTED不代表新规则已经实现 |

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
| `DOC-MVP-001` | 当前角色MVP与下一轮边界 | READY | MVP收口 | IMPLEMENTED | DesignDoc/Plan范围审查 |
| `DOC-ITER2-001` | 第二轮迭代交付范围 | READY | 待生成第二轮Plan | READY | 第二轮范围审查 |
| `DBG-PORT-001`～`DBG-OUT-001` | 类尖塔设计理念 | READY | 内容填充 | READY | 内容评审 |
| `START-001` | 普通开局为0；圣女/魅魔开局为±3 | READY | 普通开局MVP；额外开局下一轮 | IMPLEMENTED（框架） | MVP只验普通开局 |
| `START-002` | 外观、解锁、初始遗物选择 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-COR-001` | 堕落范围、UI和角色隔离 | READY | M0/M1 | IMPLEMENTED | M0/M1回归 |
| `SYS-COR-002` | 堕落增减与每Run一次行为 | OPEN | M0～内容填充 | IMPLEMENTED（框架） | 控制台/事件回归 |
| `SYS-COR-003` | 路线奖励概率 | READY | M1 | IMPLEMENTED | M1路线奖励 |
| `SYS-SEA-001` | 封印区、跨战斗快照和火堆移除 | READY | M1 | IMPLEMENTED | REST-3/封印回归 |
| `SYS-COR-004` | 事件门槛和遗物阈值 | OPEN | 下一轮内容填充 | DEFERRED | 不进入MVP验收 |
| `ACT4-001` | 第四幕、路线任务与路线Boss | OPEN | MVP回归＋后续内容 | IMPLEMENTED（MVP流程框架；待运行时回归） | 已恢复任务选择、进度、碎片、献祭、阶段替换、入场判定和占位Act；新敌人与Boss内容仍延期 |
| `SYS-DES-001` | 跨战斗欲望资源和UI | READY | M2 | IMPLEMENTED（左侧条战斗内外持续显示；战斗内同时显示RitsuLib计数器） | M2回归 |
| `SYS-DES-002A` | 10点欲望与高潮平复 | READY | MVP | IMPLEMENTED | M2核心回归 |
| `SYS-DES-002B` | 5/8点欲望与控制联动 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-DES-003A` | 卡牌/遗物/事件/火堆等欲望来源 | OPEN | MVP内容填充 | READY（框架） | MVP卡牌与Run回归 |
| `SYS-DES-003B` | 怪物意图增加欲望 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-CTL-001` | 控制格挡、Power与挣脱 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-CTL-002` | 弱怪晕眩/强怪低威胁恢复策略 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-INV-001` | 侵犯意图、诅咒来源和晕眩 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-INV-002` | 商店特殊移除全部精液类诅咒；每张奖励50金币 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-DES-INTENT-001` | 怪物增加欲望意图 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-ENC-001` | 战斗中临时附魔 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `KW-PORTABLE-001` | 随身 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `KW-CONDEMNATION-001` | 断罪与审判 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `KW-PURIFICATION-001` | 净化 | READY | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |
| `SYS-SCR-001` | 六种圣言及持续Power | READY | 下一轮 | DEFERRED（样本保留） | 不进入MVP验收；来源牌不展开圣言卡图悬停预览 |
| `SYS-BLS-001` | Boss后光明/黑暗恩赐 | OPEN | 下一轮 | DEFERRED（框架保留） | 不进入MVP验收 |

### 2.1 表现层接入追踪（2026-09-26）

| 交付ID | 关联需求 | 边界 | 交付状态 | 验收入口 |
|---|---|---|---|---|
| `PERF-CG-001` | `SYS-CTL-001`、`SYS-INV-001`、`SYS-DES-003A` | 成功结算后的可跳过CG；跳过不撤销规则 | IMPLEMENTED | `CutscenePlaybackService`、专项资源门、游戏内手测 |
| `PERF-CG-002` | `SYS-CTL-001`、`SYS-INV-001` | 怪物类型映射独立于UI，未知侵犯回退弱怪 | IMPLEMENTED | `MonsterPerformanceProfiles`映射审查 |
| `PERF-AUD-001` | `SYS-TRF-001/002`、`SYS-DES-001/002A` | 普通提示、成人演出分轨；循环单例；场景清理 | IMPLEMENTED | `PerformanceAudioService`、Debug构建 |
| `PERF-AUD-002` | `SYS-DES-003B`、`SYS-CTL-001`、`SYS-INV-001` | 只在效果实际执行/成功后播放，同怪同动作去重 | IMPLEMENTED | 意图入口审查、游戏内手测 |
| `PERF-CFG-001` | 表现设置降级 | 成人CG/音频独立接口；未确认产品默认前均关闭 | IMPLEMENTED（设置UI OPEN） | `PerformanceSettings`、关闭态手测 |
| `PERF-REG-001` | `MP-001`、角色隔离 | 仅正确本地响木天音显示，不重复规则命令 | IMPLEMENTED；待多人手测 | `PerformanceAudience`、手测清单 |

### 2.2 历史记录UI回归追踪（2026-09-26）

| 交付ID | 关联需求 | 边界 | 交付状态 | 验收入口 |
|---|---|---|---|---|
| `UI-RUN-HISTORY-001` | 角色正式UI素材回归 | 只缩放历史记录中RitsuLib贴图工厂生成的响木天音头像；不修改共享素材和其他界面 | IMPLEMENTED；待游戏内验证 | `MaidenRunHistoryCharacterIconPatch`、结构契约、历史记录截图 |

## 3. 内容章节追踪

| ID范围 | 内容分类 | 当前成熟度 | 实现策略 |
|---|---|---:|---|
| `CARD-C-100～199` | 堕落欲望体系 | OPEN | MVP优先；单卡闭合后纳入资格清单 |
| `CARD-C-200～599、800～899` | 堕落原版机制体系 | OPEN | 不依赖延期机制的单卡可进入MVP |
| `CARD-C-600～699` | 临时附魔体系 | OPEN | 下一轮；框架保留 |
| `CARD-C-700～799` | 性技/控制利用 | DRAFT | 下一轮，不进入MVP |
| `CARD-H-100～199` | 低欲望超模与降欲望 | OPEN | 逐卡填充 |
| `CARD-H-200～599、800～899、950～999` | 圣洁原版机制体系 | OPEN | 不依赖延期机制的单卡可进入MVP |
| `CARD-H-600～699` | 断罪体系 | OPEN | 下一轮；框架保留 |
| `CARD-H-700～799` | 圣言体系 | OPEN | 下一轮；六种样本保留 |
| `CARD-H-900～949` | 控制应对与临时附魔 | DRAFT/OPEN | 下一轮，不进入MVP |
| `CARD-H-950～999` | 圣洁体系外卡 | OPEN | 逐卡填充 |
| `CARD-N-100～199` | 天平与过渡数值 | OPEN | 现有样本继续作为回归基线 |
| `CARD-N-200～299` | 打击、防御与随身 | OPEN | 原版机制牌可进MVP；随身/控制牌转下一轮 |
| `CARD-N-300～399` | 路线桥梁卡 | OPEN | 逐卡填充 |
| `STATUS-001～099` | 通用状态牌 | DRAFT/OPEN | 完整条目达到READY后实现 |
| `CURSE-001～099` | 通用诅咒牌 | DRAFT/OPEN | 完整条目达到READY后实现 |
| `CURSE-INV-001～099` | 侵犯注入诅咒 | OPEN | 下一轮；“精液”样本保留 |
| `RELIC-START-001～099` | 初始遗物 | OPEN | `RELIC-START-003`样本已实现 |
| `RELIC-CHAR-001～099` | 角色专属遗物 | DRAFT | 下一轮，不进入MVP |
| `RELIC-EVENT-001～099` | 事件遗物 | OPEN | 下一轮内容填充 |
| `EVENT-001～899` | 正式事件 | OPEN | 下一轮内容填充 |
| `EVENT-EASTER-001` | 炉石传说彩蛋 | DEPRECATED | 不实现 |
| `MON-001～899` | 普通/精英敌人 | DRAFT | 下一轮；MVP使用原版敌人且不注入Adapter |
| `MON-BOSS-C-001` | 堕落路线最终Boss | DRAFT | 下一轮，不进入MVP |
| `MP-001` | 每玩家资源独立 | READY | 下一轮 | DEFERRED |
| `MP-002～099` | 控制目标、协助挣脱等 | OPEN | 下一轮等待设计补充 |

### MVP卡牌内容交付状态

| ID范围 | 当前交付 | 下一验收 |
|---|---|---|
| `CARD-N-100～399` | 已实现条目待MVP资格审计 | 保留原版机制牌；排除随身/控制依赖；核对百科与奖励池 |
| `CARD-C-100～599、800～899` | 已实现条目待MVP资格审计 | 优先保留欲望及原版机制牌；逐张验证欲望支付、描述与升级 |
| `CARD-C-600～799` | 下一轮 | 临时附魔、性技和控制利用不进入MVP |
| `CARD-H-100～599、800～899、950～999` | 已实现条目待MVP资格审计 | 优先保留低欲望及原版机制牌；排除自定义延期机制依赖 |
| `CARD-H-600～799、900～949` | 下一轮 | 断罪、圣言、控制应对和临时附魔不进入MVP |

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

## 5. 2026-08-22 第一轮迭代同步

| 类型 | 需求ID | 变化 | Plan结论 | 当前状态 |
|---|---|---|---|---|
| ADD | `DOC-ITER1-001` | MVP验收后启动第一轮完整范围 | 新增`ITER1-0～6` | READY（从时间戳MVP基线正向重做） |
| CHANGE | 基础牌章节 | 黑暗元素注册并实现为基础稀有度内容，但不加入普通初始牌组 | 初始牌组保持4打击、4防御和变身 | IMPLEMENTED（待运行时验收） |
| CHANGE | `SYS-DES-002A` | 满欲望按玩家回合立即、敌方回合/战斗外排队，并逐次结算 | `ITER1-1`重做时序和持久化计数 | IMPLEMENTED（待运行时验收） |
| CHANGE | `SYS-TRF-001/002`、`KW-MAGIC-AMP-001`、`KW-OVERDRAFT-001` | 变身、耐久、增幅、透支采用新规则 | `ITER1-1`重做并全卡回归 | IMPLEMENTED（待运行时验收） |
| ADD | `SYS-TRF-004` | 四档分层立绘与五类轻量动画 | `ITER1-3` | READY |
| CHANGE | `SYS-CTL-001/002`、`SYS-INV-001/002` | 闭合多来源挣脱、恢复意图和商店清理 | `ITER1-2`替换延期状态；2026-09-10挣脱改为RitsuLib卡实例capability；2026-09-11增加9场景真实运行时拘束套件 | IMPLEMENTED（静态门通过后仍待游戏内自动套件与人工存读档/视觉验收） |
| ADD | `SYS-DES-INTENT-001`、`MON-ERO-CATALOG-001`、`KW-STEADFAST-001` | 逐怪物色情意图、权重、次数、优先级与意志坚定 | `ITER1-2`；交付范围覆盖DesignDoc成熟度标签；2026-09-12统一“欲望攻击”正式名称并修正并排意图数值布局 | IMPLEMENTED（静态门通过，待游戏内视觉验收） |
| CHANGE | 全部卡牌范围 | 所有类型、费用、效果完整的单卡均纳入第一轮 | `ITER1-4`全量资格审计和实现；2026-09-12“战技复读”复制实例补齐DesignDoc要求的暗色边缘来源遮罩，并由实例capability约束生命周期；内部生命周期监听Power按玩家确认隐藏；2026-09-13修正RitsuLib百科过滤器排序，三路线154张正式牌及两张放行的初始基础牌统一显示，其他衍生牌继续排除；同日补齐守护圣言基础3格挡经原版格挡Hook受到敏捷等状态修正的数值测试与动态Power文本，并将精神统一的延迟非Powered格挡卡面预览与实际结算统一为不受敏捷修正 | IMPLEMENTED（静态门通过，待游戏内数值与显示验收） |
| CLARIFY | `ENCH-INFECTION-001` | 寄生在被附魔牌离手前传播至相邻手牌 | `ITER1-4`按新生命周期回修 | READY |
| ADD | 第四层路线遗物 | 各阶段描述严格分离 | `ITER1-5` | READY（保持MVP流程，只增量修改阶段文案） |

第一轮实现状态统一记为`READY`；完成代码、资源、构建和调试入口后改为`IMPLEMENTED`，不得提前标记`VERIFIED`。

## 6. 2026-09-14 第二轮迭代同步

| 类型 | 需求ID/范围 | 变化 | Plan结论 | 当前状态 |
|---|---|---|---|---|
| ADD | `DOC-ITER2-001` | 建立第二轮交付范围 | 生成第二轮Plan时以本范围为最高优先级 | READY |
| CHANGE | `SYS-DES-INTENT-001`、`MON-ERO-CATALOG-001` | 纳入诱惑度阈值、色情意图替换与逐怪物适配 | 第二轮核心机制 | READY/OPEN，以各正文成熟度为准 |
| ADD | `STATUS-001～099`相关完整条目 | 纳入撕裂衣服及倒刺钩、衣物燃烧、咬衣纸片、溶解液 | 第二轮怪物欲望攻击内容 | READY |
| CHANGE | 已实现卡牌与遗物 | 按当前DesignDoc效果增量回修 | 第二轮内容回归 | READY |
| ADD | 新增卡牌与遗物 | 仅纳入类型、费用和完整效果齐全的条目 | 第二轮内容实现 | READY；逐条资格审计 |
| EXCLUDE | 无名先古牌、待定诅咒、未完成内容 | 本轮暂时跳过 | 不生成实现任务 | DEFERRED |

第二轮不会自动纳入新敌人、第三层替换Boss、第四层最终Boss和多人游戏；这些内容继续按DesignDoc各章节成熟度处理。恶魔法杖维持既有实现，仅同步候选卡池需求说明。

2026-08-24 基线更正：第一轮开始时间为`2026-08-22 02:12:23 +08:00`，最后MVP源码写入时间为`2026-08-21 21:03:17 +08:00`。完整时间戳MVP树由提交`36764029f670c49b9b8298e40da399c3477735ab`保存。新的合并分支必须直接以此提交为父节点，再逐功能正向合入第一轮；禁止把MVP文件反向复制到错误第一轮树。

先前第一轮实现因错误基线作废，只保留为取证来源，不继承其`IMPLEMENTED`结论。新分支每个批次完成代码和MVP回归后单独标记`IMPLEMENTED`；统一运行时验收见`docs/ITERATION1_MANUAL_TEST_CHECKLIST.md`。

## 7. 2026-09-26 UI正式素材交接同步

| 类型 | 需求ID/范围 | 实现映射 | 当前状态 |
|---|---|---|---|
| CHANGE | `UI-ROUTE-V4-001` | `RouteCardVisuals`使用`maiden_route_wing_v4`贡献和两张V4正式翼饰；仅圣洁/堕落路线返回覆盖层，旧V3运行时资源移除 | IMPLEMENTED（待游戏内视觉验收） |
| CHANGE | `UI-CHAR-SELECT-V2-001` | `MaidenSuccubusCharacter`显式绑定V2背景、正常/锁定头像及新缓存名；`MaidenCharacterSelectVisualPatch`仅在选角按钮使用V2头像，顶栏Q版图标不变 | IMPLEMENTED（待游戏内视觉验收） |
| AUDIT | `UI-FORMAL-ASSET-AUDIT-001` | `完成版卡图/manifest.json`的130张正式卡图及默认图、火堆/商店正式素材、CG/音频清单均与运行时逐项哈希一致 | IMPLEMENTED（静态复验通过） |

本节不改变DesignDoc机制含义，也不把素材候选、V1选角背景或V3路线翼饰重新纳入运行时。

## 8. 2026-09-27 全面变更审阅与重新验收

本节覆盖之前与新规则冲突的交付状态。完整证据见[审阅报告](DESIGN_SYNC_20260927_REVIEW.md)和[原始差异](DESIGN_SYNC_20260927_RAW_DIFF.md)。当前仅完成审阅，不标记任何本轮实现为IMPLEMENTED或VERIFIED。

| 需求范围 | 变化与结论 | Plan任务 | 当前状态 | 验收组 |
|---|---|---|---|---|
| `DOC-ITER2-001` | 以指定历史交付为保守基线，纳入所有文本和行为差异；保留并行修改 | DS27-00 | 审阅完成；语义确认OPEN | DS27-DOC |
| `SYS-TRF-001/002/004`、`KW-OVERDRAFT-001` | 上限5、减损33%、零层退出边界、异形态切换；旧完成结论回退 | DS27-01 | OPEN：Q1 | DS27-CARD-EFFECT/COMPAT |
| `SYS-SEA-001`、`KW-VARIATION-001` | 封印卡视觉/说明；基础牌变奏实际路线 | DS27-01/02 | READY，需重验 | DS27-CARD-META/TEXT/EFFECT |
| 全部`CARD-*`、`STATUS-*`、`CURSE-*`、`ENCH-*` | 所有卡面标点/排版/等级与行为；新增、移动和删除条目，旧存档兼容 | DS27-02 | 已明确项READY；Q2～Q5项OPEN；逐卡不继承旧通过 | DS27-CARD-META/TEXT/EFFECT |
| `SYS-DES-INTENT-*`、`MON-ERO-CATALOG-001`、`SYS-CTL-*`、`SYS-INV-*` | 逐怪物数值表、冷却与连续上限、阶段保护、意图恢复 | DS27-03 | OPEN：Q1、Q6；明确表项READY | DS27-MON/COMPAT |
| `RELIC-*`、`START-002` | 新/改遗物、先古入口、事件来源、奖励与选择交互 | DS27-04 | OPEN：Q9～Q11；DRAFT待正式确认 | DS27-EVENT/ACT4/COMPAT |
| `ACT4-001` | 14路线×3试炼与4显示形态，单次碎片、每段堕落、献祭只解锁；第四层空注册 | DS27-05 | 流程/完整试炼READY；Q7/Q8边界OPEN；旧四阶段IMPLEMENTED撤回 | DS27-ACT4/COMPAT |
| `EVENT-001～003` | 原版事件集成不回归 | DS27-06 | 待回归 | DS27-EVENT |
| `EVENT-NEW-001～007` | 七新事件及所有页面、文本、门槛、强制替换 | DS27-06 | DRAFT待Q10确认，其他歧义Q8/Q9 | DS27-EVENT/COMPAT |
| 测试基础设施 | 原文/渲染/行为分层，不忽略标点、不依赖固定行号；静态与运行时分开报告 | DS27-07 | 方案完成，脚本未实现 | DS27-GATES |

未完成的新敌人、第三层替换Boss、第四层战斗及多人设计不因本轮审阅自动转为READY。澄清前不在程序中固化候选规则。
