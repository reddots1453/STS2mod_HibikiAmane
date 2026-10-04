

## 2026-10-04 瘴雷平衡与天平悬停（CARD-C-BURNING-DESIRE-COUNT-001 / SYS-COR-001/003）

前置快照`9ffeaaafb1a2839ac8945e1998aedad29415385e`，设计提交`f61bccc396da721788ddf268b66f15e87597528c`。完整复核瘴雷体系输出、自身费用、欲望资源/满值及路线奖励概率章节；相对HEAD与ca4a397c的逐行/词级差异留档。同步本次已有瘴雷与悬停文案编辑，独立按摩事件和战斗提示扩充的其他编辑完整保留，不纳入本批设计提交。

技术任务READY：瘴雷1费1欲望、罕见攻击不变；CalculationBase(3/4)、ExtraDamage(1)、原生CalculatedDamageVar按本战斗实际获得欲望累计计算单段伤害；独立Hits计算保留1+实际成功支付台账，两项基础变量不混用。RitsuLib AfterSecondaryResourceChanged仅Reason.Gain且NewAmount>OldAmount记录实际增加值，先记账后发布显示/处理满值；Set/Reset/读档/非战斗/已结束会话不计，按玩家与CombatState对象隔离，战斗结束关闭。伤害执行直接传原生CalculatedDamage变量，预览与实际共用公式并由原生Hook处理力量、易伤、附魔，不持久修改基础伤害、不重复增伤。升级仅基础3→4；括号战斗内显示动态伤害与次数，战斗外仍基础说明。

天平通用悬停严格使用用户新文案、[sine]一些行为[/sine]和两项百分数。同RouteCardRewardService读取RouteRewardProbabilityModifiers与RouteRewardProbabilities.Calculate；乘100显示，默认+5为65%/0%、0为10%/10%、-5为0%/65%，遗物加成与归一化不另写概率表。保持±3/±5现有阈值提示、原卡池算法和每张候选判定。

验收BD-HOVER-01基础/升级、累计获得后降低仍保留增伤、多段次数与自身支付、阻止获得、满值回落、免费重放、下一场清空；02预览目标力量易伤附魔与实际一致；03天平0/±3/±5及概率修正显示，文字仅“一些行为”浮动。本批只构建，不运行静态测试；游戏内由用户手测后方可VERIFIED。只重新封装完整PCK的cards/static_hover_tips两项，原资源和combat_feedback配置继承上一包。累积此前未部署试炼UI与商人尺寸修订，暂不部署，不改游戏/沙箱/上传器JSON。
