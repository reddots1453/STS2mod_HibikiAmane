# 天音战斗提示扩充文案交接

2026-10-04，用户授权直接编写源目录combat_feedback.json。本轮完成文案填充与候选交互文案准备，不修改程序代码、技术Plan、游戏安装配置或战斗规则。

## 内容与来源

既有十交互及三类拘束专属组共171槽，保留原有3条默认伤害句并补168槽；另为十二个尚未通知的交互准备108槽。合计22个交互键、279个非空槽，方法统计为保留3槽、原作短句拼接11槽、编写265槽。源配置保留schema_version=2与enabled开关。所有句子为单行纯文本；原作首尾回车已清理，不携带RPG Maker或BBCode指令。

原作参考为用户提供的V56.5 BattleMessage.json，SHA256为6fc0d8b7aa206bbda6c4a153707a1a6a3b618a1ffa82156eed5e6a9e258fb88e。逐槽来源、全部审阅稿、修改前后配置与检查结果保存在聊天outputs/combat-feedback-texts-20261004。只有实际使用原句的条目标记原作ID和字段，编写项不伪标为原作。

## 已有接口与待接入键

已有十交互继续按原通知语义工作：意图开始不提前宣告结果；一般欲望上升、魔装受损不猜敌人；挣脱后剩余量是全部拘束合计；最后解除包含主动挣脱、直接解除、来源死亡。专属类型选择和轮换继续由现有解析器负责。

待接入键：control_applied, control_blocked, control_partially_released, invasion_applied, invasion_blocked, invasion_diverted, armor_depleted, transformation_broken, desire_penalty_resolved, desire_recovered_below_8, critical_damage_received, all_damage_blocked。当前解析器可以读取其三路线数组，但尚无Notify调用，填写不会自动产生这些反馈。待接入项只使用静态结果标签与人物短句，不预设新占位符合同。

## 编程侧交接

需求UI-COMBAT-FEEDBACK-003为READY内容填充；UI-COMBAT-FEEDBACK-004为OPEN后续触发设计。编程侧接收本轮设计提交后同步PLAN_FRAMEWORK.md与docs/DESIGN_TRACEABILITY.md。本轮遵守设计工作树职责，没有改动两份技术文档。

后续应分别核对拘束防住/生效/部分解除、侵犯后果取消/转移、耐久耗尽/真正退出、满值到达/惩罚结算、欲望降低原因、全格挡/危急生命。危急阈值、行动关联ID、反应限频与行动标签仍未定值；不因文案存在而视为实现已完成。不要让源文案覆盖游戏安装目录中用户自行编辑的配置。

## 验证与版本边界

JSON解析、22键/279槽覆盖、三路线/每组3槽、按既有事件可用占位符核对、原作控制码检查、已有非空槽保留均通过。最长模板47字符，包含尚未展开的占位符；没有做游戏内视觉验收，不声明显示宽度已通过。纯配置文案任务没有构建或运行游戏。

变更前HEAD：83cf2ca945a899e2044e8cc1a09683e7d2f19cfd。可恢复快照：5586c2037eaf0c37a6dc067d3176e4d39ab32e9b，标签codex/before-combat-feedback-texts-20261004-185754。共享工作树已有其他任务的设计与实现修改；本轮使用独立Git索引与辅助分支提交，不切换主分支，不修改其HEAD或暂存区。完成提交见聊天交付记录。
