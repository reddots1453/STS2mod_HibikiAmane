# DS27第72批：27牌全文与暗焰壁障

2026-09-28；变更前快照/技术计划`965f2a63`，计划前`52a2e273`，分支`codex/maiden-controlled-merge-v2`。DesignDoc逐行/词级无漂移。本批IMPLEMENTED，不是VERIFIED；未部署、未启动或操作游戏，不变更素材、其他路线或OPEN规则。

## 实现

- `DesignSyncCorruptTextContract`登记27张非露骨普通机制牌的54份基础/升级独立全文，来自完整DesignDoc条目；真实Run牌组实例和Combat手牌分别生成原生描述，包括关键词位置、费用图标、标点和换行。不是从待测本地化反推期望。全局Runner和独立`ds27-corrupt-text`组接线，原行为场景保留；连锁破坏Q12仍待定。
- `Iteration1CorruptCards.DarkFlameBarrier`修复旧规则：补1欲望费用，基础Turns从2改1，6/9格挡无条件获得，魔力解放改额外1回合，不再在解放时才给格挡。
- `Iteration1CorruptPowers.DarkFlameBarrierPower`参考原生Colossus：仅对有来源、异侧、燃烧的攻击者所造成的攻击伤害减半；按敌方回合结束扣层，而非玩家个人回合开始。拥有者死亡、Power不在列表或层数归零后引用惰性失效。通用说明不含未绑定变量；实际状态smartDescription显示剩余回合。
- 似水年华的抽牌和获得能量之间由逗号改为句号换行；保留能量图标。其余26牌不因此改变玩法。
- `DesignSyncDarkBarrierContract`七组无资源、零耐久、接受/拒绝、增幅优先等场景，每组先实际SpendResources再执行卡牌，核对能量/欲望、格挡、耐久/增幅、消耗、持续时间；用真正伤害命令检查普通/燃烧/非攻击来源，直接检查其他目标/无来源/同侧与已移除引用，测试敌方到期及重复施放叠时间不叠减伤。每个升级版本最低100效果断言；仅编译，未在引擎中执行。
- 两个新增静态套件及文本清单接线负例共13项；清单不把存在源码误报为运行通过。

## 验证证据

- 首轮`obj/design-sync-validation/20260928T091950Z-d27073fa8e61/report.json`为11/12：本地化门拒绝通用Power description的未绑定`{Amount}`。保留失败证据，不放宽检查器；拆分通用说明/动态悬停并更新独立断言后重跑全部门。
- 最终`python scripts/ValidateDesignSync20260927.py`：`obj/design-sync-validation/20260928T092615Z-27433f7e3ab5/report.json`，12/12离线通过，源码未漂移。源码SHA256 `ab4526b8dcd70defdbc89eb473d8e319f8c382fd8c19faa5d3462330568e609b`；基点HEAD965f2a63加本批实现。本记录及Plan/追踪状态在该次运行后更新，不属于该次指纹。
- 561日期静态＋29审计自测＝590通过；13778生产纯规则及33存档编码断言通过。Debug/Release均`DeployMod=false`、零警告零错误；内容、结构、本地化、卡牌登记、视觉五门通过。
- 全卡227注册，明确失败0、未解析0，通用pendingText217、designOnly2、retiredCompat2保留。独立清单`obj/card-text-evidence-b72.json`：217现行中160双实例全文声明、57未识别，完整性错误0；这是脚本覆盖声明，不是160张实机通过。
- 卡牌/怪物/第四层/事件/多人/视觉六组均not_run；统一逻辑退出2明确表示离线通过但全目标未完成。宿主返回码不替代JSON语义。

## 未完成事项

本次发现`CorruptCardsExpanded.BurningDesire`（瘴雷）仍使用`Data.Desire.Get(Owner)+1`计算次数，本地化也仍写当前拥有量；DesignDoc明确要求本场战斗累计消耗，且自身付款计入。旧Probe亦用当前值构造预期，须独立计划回修累计支付来源、跨战斗清零/读档恢复/重复打出以及文本，不把旧测试通过当作已正确。该卡不在本批27张内。

仍需一次性测试局验证27牌全部实际渲染/布局及暗焰壁障正常手动出牌、自然回合/增幅/重复施放/多人隔离。全范围审计、未覆盖57张、其他已记录OPEN及事件/怪物/路线缺口不因本批完成而缩减。独立提交只包含本批文件与CHANGELOG首段，保留素材、角色源码、共享审计及其他Agent的改动。
