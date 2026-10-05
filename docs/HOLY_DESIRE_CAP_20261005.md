

## 2026-10-05 -5堕落值欲望上限修订（SYS-COR-004 / SYS-DES-002B，READY）

前置快照`2cc3d5ca54cf8b81baead5a7938a64e8cfe904fa`，设计提交`57a2c787331939514545431200cfc998940180f7`，基线`9b406dfa29398c8deafd5ac2bcff41bd528c41d1`。用户标题+5与正文-5不一致，本轮按正文的明确悬停位置和规则实现-5，已告知；+5战斗后欲望+1不改。复核SYS-COR-001～004、SYS-DES-001/002A/002B、阈值UI/满值惩罚/排队/一次堕落加成/Unbounded/手牌保护/持久化，以及原RitsuLib GetMaxAmount战斗外不会运行hook的边界与Gain/Set仅HardMax钳位规则。保存真实文件快照及相对HEAD/接受提交的两类DesignDoc差异，不覆盖独立用户文案。沿用商店/眼罩/原版变化上一累计构建。

任务：DesireRuleModifiers.ModifyCap在其他modifier之前，仅天音且当前IsMaxHoly增加5；Data.Desire.GetMaximum战斗内用框架GetMax，战斗外用相同modifier规则。RecheckMaximum/音频标记/CG/暂停的RPG接口改读当前上限，8和5不改。删除角色战斗结束-5欲望-1，+5不改；早期玩家回合合法入口在原排队结算后复查当前上限，恢复Wet顺序不变。DesireState增加PendingClimaxThreshold默认0为旧10，排队记录当前阈值、不重复覆盖已排队记录；首次回合如果上限已提高且欲望未满则丢弃旧低阈值排队，其他已发生排队不改变；clear同时清零阈值。DesireMeter按value/max缩放到现有11张贴图，未到上限不显示满；缓存同时含max，Corruption变化立即刷新，8点悬停区域同步相对位置且阈值文本仍8，上端悬停仍兼容旧key但文本改当前上限。只替换静态悬停JSON三个键打包，其余资源/用户文案保持上一包；不新增图片，不改资源hardMax/费用/存档金额。未做静态测试，只Debug build，运行时NOT_RUN、部署NOT_RUN。

验收HDC-01：-5基础上限15，10～14不触发满值，15触发原惩罚并归3；HDC-02：8点Wet/拘束/心跳/边缘不变；HDC-03：-4/0/+5基础上限10，-5战后不-1，+5仍+1；HDC-04：读档15上限与实际金额一致、不凭加载重复CG音频，旧10排队不误罚新圣洁上限；HDC-05：上限变化欲望条即时刷新，15填满/10未满，8点和上端说明正确；HDC-06：无限欲望/手牌保护优先与现有排队语义保持，非天音不受影响。完成IMPLEMENTED，用户游戏实测后再VERIFIED。


SYS-COR-004 / SYS-DES-002B 同义文本补齐：设计补充提交`53a2342dbaaba9bfcf13f3385faecd75203fa9da`统一2.2即时/敌人回合/战斗外结算入口与2.3上限范围，不再保留无条件10点的旧文字；9+3例子明确为普通上限10。未新增玩法需求。音频去重保存MaximumAudioThreshold（旧0视为10），提高上限后真正达到新上限能播放一次，降低上限或在同一满值继续获得不重播；低于当前上限时清除标记。原有10点高欲望表情分档继续作为表现分档，不等于满值惩罚或CG入口。


SYS-COR-004 / SYS-DES-002B IMPLEMENTED（2026-10-05）：前置`2cc3d5ca54cf8b81baead5a7938a64e8cfe904fa`，设计`53a2342dbaaba9bfcf13f3385faecd75203fa9da`，计划`5c7b6173445fcf558c71aa0b45700e3669a596b3`。-5时欲望上限+5（通常15），替代旧战后-1；+5仍战后+1。统一读取当前有效上限，用于满值惩罚/音频去重/CG/暂停RPG接口及条填充，5/8阈值保持。无限欲望/手牌保护规则保留。新增排队触发阈值保存并兼容旧10，提升上限时不执行尚未满的新低阈值排队；原已触发有效排队结算顺序保留。上限降低和无回调恢复在玩家回合合法入口复查，未因加载发布假欲望获得。悬停三个键更新并定向写入PCK，其余资源和用户文本继承上一包。Debug构建`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过0警告0错误；静态测试NOT_RUN、游戏内NOT_RUN，未部署，不修改安装JSON。
