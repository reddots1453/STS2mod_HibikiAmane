# DS27 第60批：有符号状态层数

日期2026-09-28。同步前快照`2fcf8357`，计划提交`f74213dd`；DesignDoc逐行/词级无漂移。复核恐惧光环、狂战士假面的“负力量也算负面状态”正式备注，审判之刃、高级治疗及圣咒/魂之冲击等公式。不改设计、数值或原版分类，不部署。

## 证据与修复

知识库明确力量/敏捷可为负；实际DLL通过ILSpy核对PowerModel.GetTypeForAmount：允许负值的Counter在负数时返回Debuff。原共享查询先要求Amount>0，实际完全漏掉这种减益，与明确设计及原版分类不符。

PowerLayerQuery保留IsVisible与TypeForCurrentAmount过滤，改为调用生产PowerLayerMath累计绝对层数。不会把负力量计作Buff，不以正负相加抵消层数，不将一个多层Power误计为一层，也不会把隐藏收尾状态加入公式。零仍贡献0，正数逻辑不变；未修改Power本身、伤害加成顺序或任何卡牌基值。

共享调用包括审判之刃/背水一战的计算伤害、高级治疗、魔力爆发、太阳之舞、圣咒、魂之冲击及其他已有按正负层数收益的牌和调试查询。它们继续调用同一函数，不各写一份分类。不宣称本批为每个调用者新增了独立运行时测试。

## 测试

- 纯测试主机直接链接生产PowerLayerMath，13个独立字面例子实际执行：空、零、正负单层、混合符号、多实例、最大正整数。仅验证筛选后的聚合，不冒称已执行Godot或原生Power分类。
- DesignSyncSignedLayerContract接入原五牌基础/升级测试：HealingArt、JudgmentBlade、LastStand、HolyCurse、SoulImpact。使用真实负Strength/Dexterity，检查原版TypeForCurrentAmount、跨零类型变化及隐藏Ambergris不计入；实际预览/伤害/恢复/抽牌/回能一并断言。背水一战额外检查负力量仍正常降低攻击伤害，永久牌组描述仍不展示战斗总值。
- 5项源码契约检查共享筛选、纯函数链接及五牌测试接入。没有替代原有正数场景，原断言全部保留。
- 游戏入口仍为`ms_test_cards confirm <卡牌类名>`；仅编译未执行，完整战斗、多人、状态同步与视觉仍待验。

## 实際离线验证

`python scripts/ValidateDesignSync20260927.py`执行12项，11通过、退出1。日期静态484＋审计自身19＝503；生产规则13760、存档编码33；无部署Debug/Release零警告零错误；四内容门与视觉门通过。唯一失败仍为GagCurse费用期望1/源码2，不标全卡审计通过。

报告`obj/design-sync-validation/20260928T054901Z-9f538bc21574/report.json`，测试HEAD`f74213dd`包含当时未提交的本批实现；前后源码SHA256均`aa27303c92f80afbab15540c7ed09a41ee3c7dba4c68e7ea8c70e1b662b80f76`，无漂移。本验收记录/状态随后更新，不包含在该次指纹内。未覆盖并行审计报告/素材，未启动或部署游戏。

全量目标及OPEN问题保持未完成；原指定9月15日23:44精确快照不可得的证据限制与保守基线125d9f1c不变。
