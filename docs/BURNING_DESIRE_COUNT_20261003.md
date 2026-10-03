

## 2026-10-03 瘴雷动态次数显示（CARD-C-BURNING-DESIRE-COUNT-001）

前置快照`4ba910bc34a00d5b98dec981b89bec8f7c6596bd`，用户直接要求瘴雷显示“（造成？次伤害）”，同建御雷神。相对HEAD的历史设计差异已保留；相对最新接受版本65d3f389逐行/词级无漂移。复核瘴雷完整效果、自身费用计数、既有DS27-02AN支付台账及建御雷神动态预览实现；本轮明确需求已补入DesignDoc并由设计提交`b04f3fe8254eaaf55bd30ac6732c0827017d179d`同步。

技术实现：瘴雷增加CalculationBaseVar(1)、CalculationExtraVar(1)、CalculatedVar("Hits")，乘数取DesireCombatSpending.Get(Owner)。战斗中文案添加与建御雷神一致的InCombat条件括号和Hits:diff()，战斗外无多余空行/括号。OnPlay用同一个CalculatedVar.Calculate读取攻击次数，确保文字/实际攻击共用计算。SpendResources先于OnPlay，因此自身成功支付自然进入台账；不预支未发生的费用，不修改免费/重放/生命替代/失败支付/非支付减欲望的既有规则。3/4单段伤害、1能量1欲望、稀有度及升级不变。

验收BD-COUNT-01基础/升级战斗卡随累计支付更新括号数值；02自身正常支付后实际命中次数包含自身1点欲望；03免费/重放/生命替代不凭空累计，其他玩家/战斗隔离；04百科/牌组战斗外不显示括号计数，升级显示仍正确。状态IMPLEMENTED待用户手测。按持续指令不运行静态测试，只build；继续暂不部署，不改安装JSON/ModUploader/用户存档。待部署完整PCK仅替换zhs/cards.json，继承上一批欲望读档修复及浮动提示。


构建通过：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`，0警告0错误。静态测试NOT_RUN，游戏内NOT_RUN；独立包outputs/burning-desire-count-20261003包含DLL/PDB/完整资源PCK及保留空文案配置，暂不部署。
