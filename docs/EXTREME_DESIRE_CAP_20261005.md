

## 2026-10-05 +5欲望上限与悬停同步（SYS-COR-004 / SYS-DES-002B，READY）

前置`32636db88ee30e879b49af555f50f2d429d1e5d1`，设计`402ae80b7b42826a5827e87384ff4e5807ce9f50`，基线`99f4acd91c3f77b214a396552688e87160723d6a`。用户截图说明+5仍旧效果，询问后明确确认+5也改为上限+5替代战后+1。复核SYS-COR/DES全部阈值、原动态上限/旧排队/audio阈值保存、UI/持久化章节；与上次-5规则一致，本轮两端共享一份+5不叠加。DesignDoc先前旧文字漂移在本次明确确认后定向同步至动态±5上限，其他用户内容不改。HEAD/接受版本逐行词级差异存档、私有index保留真实index。

任务：DesireRuleModifiers基础加成条件为IsMaxHoly||IsMaxCorrupt；Character.AfterCombatVictory删除旧+5 desire.Modify，保留同名无副作用Task返回边界。既有GetMaximum/惩罚/CG/音频/UI/排队全部自动读新上限，无复制算法或新存档字段。static_hover_tips仅plus5、description、threshold10三键更新；minus5不动，其余用户资源定向封包保留。验收EXCAP-01：±5有效基础15、±4基础10，10～14不满、15原惩罚归3；02：+5战后不再+1、-5不-1，8点规则不变；03：天平+5和欲望条/满值说明正确，读档金额和上限一致、旧排队/音频去重规则继续适用；04：英雄宝珠≤-4保留+沉眠精华以及试炼/眼罩前批修订保留。仅build，静态测试/游戏内NOT_RUN；完成IMPLEMENTED，用户授权仅本地部署、保留所有安装JSON、不写上传器和沙箱。


SYS-COR-004 / SYS-DES-002B IMPLEMENTED（2026-10-05）：前置`32636db88ee30e879b49af555f50f2d429d1e5d1`，设计`402ae80b7b42826a5827e87384ff4e5807ce9f50`，计划`7276433bdd3f6c047ef0444552a0f499fa476b24`。+5由战后欲望+1替换为欲望上限+5，-5同规则保留，两端通常15且不叠加；其他通常10。满值/音频/CG/计量条使用已有动态上限，8点不变；三条悬停说明同步。英雄宝珠本轮修订累计包含。Debug构建`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`0警告0错误；静态测试NOT_RUN，游戏内NOT_RUN。尚未部署此累计包，不写安装/上传器JSON；已授权本地部署将待游戏关闭后替换。
