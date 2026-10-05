

## 2026-10-05 英雄宝珠圣洁形态（RELIC-START-002，READY）

前置`5eafcf69244c481d1d3d114d71607ebe23e64a71`，设计`deb3ee1ae605d7cc31275854a7452397c2421cec`，基线`e90c17a29c7b99a9f7bbc1e201921e46a35b01d6`。复核初始遗物/永恒宝珠/变奏显示规则、RetentionOrbRule、BeforeFlushLate、CombatEnchantmentCmd原版白名单以及原生SlumberingEssence.BeforeFlush；保存HEAD与接受版本逐行/词级DesignDoc差异，不覆盖其他用户编辑。旧正常index保持，只私有index明确范围提交。上批试炼/眼罩及累积修订已本地部署，本轮不改上传器。

技术任务：RetentionOrbRule.At对非eternal且corruption≤-4将Enchant由false改true，PermanentRetain/Upgrade规则不变。复用现有手牌选择/实例归属复核、本战斗keyword/附魔、首次补BeforeFlush和已有附魔不覆盖路径，无新hook/存档字段/RNG。HeroOrb仅激活圣洁形态时附加原版沉眠精华悬停，永恒宝珠不变；定向替换relics descriptionHoly一键，动态getter和异画沿用现有阈值。已有DEBUG命令仅校正原断言文案和enchant条件，不新增、不执行测试。

验收HERO-HOLY-01：0/+3/-3仍单回合保留，+4/+5保留并升级；02：-4/-5所选手牌保留关键字+沉眠精华、当次回合末减1费、后续每次回合末减1直到打出，不升级、不改DeckVersion；03：已有附魔不覆盖/不重复新附魔tick，空手牌安全，其他玩家不生效；04：变奏描述、异画、圣洁附魔悬停说明一致，永恒宝珠不变。仅Debug build，静态测试NOT_RUN，游戏内NOT_RUN，完成IMPLEMENTED；用户本轮已授权本地部署，构建完成后只本地DLL/PDB/PCK，所有安装JSON保留，不写ModUploader或沙箱。


RELIC-START-002 IMPLEMENTED（2026-10-05）：前置`5eafcf69244c481d1d3d114d71607ebe23e64a71`，设计`deb3ee1ae605d7cc31275854a7452397c2421cec`，计划`87d673d90de6266cc4b06b156dc1e8eb9d775c8d`。英雄宝珠≤-4选择1张手牌添加保留+原版沉眠精华，首次回合末即时减1费；已附魔不覆盖，不重复补tick，不改永久牌组；中立/+4和永恒宝珠不变。圣洁描述与附魔悬停同步，动态异画沿用原实现。Debug构建`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`0警告0错误；静态测试NOT_RUN，游戏内NOT_RUN，当前打包完成尚待本地部署，安装和上传器JSON不修改。累计包含试炼奖励地图修复、眼罩节点提示与此前全部未部署修订。
