# DS27第73批：瘴雷累计消耗回修

2026-09-28；前置/技术计划`1e7db443`，计划前`24adb48f`，分支`codex/maiden-controlled-merge-v2`。DesignDoc逐行/词级无漂移；关闭第72批新发现的真实旧规则差异。IMPLEMENTED，未部署、未启动/操作游戏，不改DesignDoc或其他玩法。

## 依据与实现

- DesignDoc瘴雷明确为1费1欲望、3/4伤害、每累计消耗1欲望重复一次，并明确自身付款计入。旧效果和旧Probe使用“当前欲望＋1”，余额相同但历史不同无法区分，故连同旧预期一起替换，不保留错误测试语义。
- 依赖NuGet STS2.RitsuLib 0.4.64反编译：SecondaryResourceCmd.SpendCore成功减少资源后，先写支付历史、再AfterSpent；失败、非正数和普通Set/Lose不调用AfterSpent。现有全局DesireResourceRules增加该监听，按实际Amount计入，不在卡牌OnPlay自行补缴。
- 没有直接总和SecondaryResourceHistory：该库的附属历史绑定CombatManager长期复用的History，源码没有清空接口；原生0.111.0的Reset/结束调用History.Clear，不能证明会清除附属集合。改用生产`CombatSpendLedger<TCombat,TPlayer>`，双层弱引用按对象身份隔离，读取不建新状态，非正数无操作，关闭战斗后拒绝迟到记录；整数容量为初始一段留出空间。
- `DesireCombatSpending`只接受欲望资源、有效玩家战斗状态和匹配的当前CombatState。通过原有DesirePersistenceCoordinator战斗结束回调封闭；不增加跨层存档字段、不从持有量反推消费、不在回合开始或UI刷新清零。DEBUG夹具显式清零仅用于重复使用同一个一次性战斗实例，不编入Release入口。
- 原生CombatRoom.FromSerializable恢复遭遇并建立新战斗，并非恢复中途动作计数；新CombatState自然从0开始，不继承上次未完成战斗的消费。该代码依据不是实际保存重载/多人回放测试结果；这些仍待实机。
- `CorruptCardsExpanded.BurningDesire.OnPlay`使用1＋累计值，付款已在OnPlay前完成，自动重放没有再支付就不增加计数。本地化完整同步为“本场战斗中每消耗〔欲望图标〕，重复1次。”及标点/分行。伤害、费用、稀有度、目标不变。

## 测试

- `DesignSyncBurningDesireContract`基础/升级真实Run与Hand全文、图标和元数据；九组独立字面量场景：无历史但高余额、正常自身付款、历史2、历史2＋3、余额为0、0费用、改为3费用、Y费用等。实际SpendResources及资源/能量变化、真实伤害总量、DamageReceivedEntry逐段数均检查；再免费自动执行一次确认没有重复记账。
- 补真实Lose、Set/同值刷新、消费不足/0/负数、回合/阵营切换、错误资源/旧CombatState回调、生命替代真实扣HP但不算欲望消费，以及战斗封闭后迟到回调。回合/阵营字段探针finally恢复；这些不是自然回合/多人手测。每版最低100效果断言防止场景未执行，当前仅编译。
- 离线项目直接链接生产台账，20条独立断言覆盖同值不同对象玩家、两场战斗、累计/初始段、只读/无效量、关闭/迟到/重复清理及整数边界。不使用复制实现作为测试对象。
- 新6静态契约与1文本清单断线负例；旧“2当前欲望→3段”的单断言已删除并被上述正确行为测试取代。

## 验证证据

- 最终统一命令`python scripts/ValidateDesignSync20260927.py`，报告`obj/design-sync-validation/20260928T095155Z-525de31ca9ff/report.json`，12/12离线通过且源码无漂移。前后源码SHA256均`09f8dfdd8030eaa1997b72d274be5440d692a29763fd907666de41228b6c0164`，基点HEAD1e7db443加本批实现。本记录与Plan/追踪的结果文字在运行后写入，不属于该指纹。
- 568日期静态＋29审计自测＝597通过；13798生产纯规则及33保存编码通过。Debug/Release均DeployMod=false、零警告零错误；内容/结构/本地化/卡牌登记/视觉五门通过。
- 初次定向编译发现新台账测试变量与旧事件测试同名、CombatSide命名空间错误，修正后定向及两次完整统一验证通过。第一份统一报告`20260928T094955Z-7f30eb9a4d39`也12/12；随后将每版最低断言提高到100并完成上述最终复验。
- 全卡审计227、失败0、未解析0、通用pendingText217、designOnly2、retiredCompat2。清单`obj/card-text-evidence-b73.json`完整性0：217现行中161双实例全文声明、56未识别；该文件在提高最低断言前生成，最终全门再次验证相应接线。全部运行时状态not_run，不把脚本数量当作实机通过。

## 未完成与边界

仍需授权的一次性测试局运行本卡基础/升级、手动支付到伤害的真实流程、自然跨回合/战斗、保存重开和多人隔离；六组游戏验证均not_run。完整目标仍未完成，其他56张全文/其他卡牌行为、既有OPEN、怪物/路线/事件缺口保持。统一逻辑退出2不是离线门失败，而是保留全目标未完成与未实机验收。

本批提交仅含上述源代码、独立测试、相应文档及CHANGELOG首段；不包含并行角色源码、素材或共享审计。未部署、未改安装目录。
