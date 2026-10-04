

## 2026-10-04 本地部署后四项检查（FOLLOWUP-20261004 / ACT4-001、CARD-H圣言、CURSE事件诅咒、SYS-COR-001/START-003）

前置快照`b3d2fcae8de5bc7fffd9a11ba1557beffd48b1ca`；仅基础价折扣语义澄清进入设计提交`87ca61ec86e0f62f4ec8989e7cd09e20104c2bae`。用户明确要求正常会员卡等折扣，100金币是基础价，不是最终价覆盖。DesignDoc相对HEAD及上一接受提交逐行/词级diff留存并复核；完整复核堕落/开局持久化章节、圣言/福音章节、事件诅咒保留及试炼三段流程，关联Plan/追踪既有任务。按摩事件叙述待单独同步，保留工作树及恢复快照，不混入本批设计/代码；不修改事件行为。

本地部署已先完成：源码c857db66 → 安装mods/MaidenSuccubus，报告见outputs/start-route-preview-20261004，用户可测右侧开局界面。以下是部署后的后续构建，不混称已安装。

| 验收ID | 任务/证据与计划 | 状态 |
|---|---|---|
| FOLLOWUP-01 | 淫纹·完全CanonicalKeywords已有Unplayable+Retain；原版CardModel.ShouldRetainThisTurn检测该关键词，CombatManager.FlushPlayerHand将其归入cardsToRetain，不进普通弃牌。回合结束生成2张发情不移除自身。本地日志仅注册/旧进度告警，无该牌战斗移动证据；无需冗余自定义保留修补，待游戏内按回合确认 | SOURCE_CHECKED / GAMEPLAY_NOT_RUN |
| FOLLOWUP-02 | FourthRouteLifecycle.ModifyMerchantPrice把中间价格重置100，覆盖先执行的会员卡乘法。移除该Hook覆盖，在MerchantRelicEntry.CalcCost的Postfix仅为碎片设置基础_cost=100，保留原计算和RNG消耗，其余价格修正走原生Cost→Hook。无折扣100、会员卡50、微笑面具/叠加/免费商店遵循原版；非碎片不改 | READY→IMPLEMENTED后待验 |
| FOLLOWUP-03 | 本地5份日志，10-04 15:56会话存在多次完成的ContinueDiag，未发现明确堕落反序列化异常或SL期间新开局Applied；现有日志无每次读档数值/天平渲染值，不能认定无重置。开局仅FinalizeStartingRelics，续局不调用；天平每帧读取保存句柄。新增只读日志：RunLoaded的corruption.Value/一次性标记数、CorruptionCmd实际old→new/source、可见天平读取值/渲染值/纹理是否加载（仅变化或新Run记录）。不修改存档值、不生成补偿资源、不重播奖励 | INSPECTED / 原因未证实；诊断待验 |
| FOLLOWUP-04 | 福音增加守护圣言/惩戒圣言的原生FromCard悬停预览，传IsUpgraded使福音+预览衍生牌+，与实际变化结果一致 | READY→IMPLEMENTED后待验 |

用户要求不运行静态测试，只执行DeployMod=false/ValidateMod=false Debug build；不写用户存档/解锁、不改ModUploader/沙箱/JSON。本批构建供后续部署，不打断当前UI实测；谦逊72→37仍等待外部玩家日志，不纳入此批修补。只有用户统一实测后标VERIFIED。
