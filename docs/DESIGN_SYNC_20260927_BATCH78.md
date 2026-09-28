# DS27 第78批：合池随机遗物

前置/计划：`6f79890a`；计划前`291bef4f`。范围：Q18/Q20、RELIC-CHAR-007/RELIC-EVENT-004。DesignDoc对HEAD行级/词级差异为空，按已确认规则实施，不改其他机制、不部署。

## 实现

- `Core/Rewards/UnifiedRouteCardPool.cs`：中立、圣洁、堕落合池，保留解锁与多人约束；ID去重、稳定排序，每个候选一份，不先选路线、不使用堕落概率。
- `Relics/TonysCharm.cs`：登记商店遗物，拾取沿原生EmptyCage选择2张永久移除，每次移除用玩家Rewards随机流生成升级稀有牌；选择返回重验、上限和去重、保存拾取回执、同卡重入守卫。普通弃牌/战斗消耗没有永久移除奖励。
- `Relics/MvpRelics.cs`的WitheredTreeSoul：战斗本人消耗一次生成一张，结束边界不触发；沿`AddGeneratedCardToCombat`保留生成历史与后续钩子，满手由原生Add进入弃牌堆。
- 枯木图标从用户指定STS1包提取，不调整图像；通过既有`RuntimeTextureAssets.PrepareResource`把松散PNG转为原生遗物getter可读取的资源，避免只填PNG路径却未导入。缺图安全回退，东尼仍使用现有占位美术。

## STS1 对照证据

路径：`F:\steam\steamapps\common\SlayTheSpire\desktop-1.0.jar`。只读包，用随游戏Java及CFR 0.152反编译三个相关类，不复制Java源码入运行时、不修改原安装。反编译工具在忽略的obj目录，不进入提交。

1. `com.megacrit.cardcrawl.relics.DeadBranch.onExhaust`：怪物未全部结束时，提示遗物并将随机牌复制加入手牌。
2. `AbstractDungeon.returnTrulyRandomCardInCombat`：common/uncommon/rare源池平铺；跳过HEALING标签；cardRandomRng均匀索引。STS2没有对应标签，本Mod直接治疗牌HealingArt仅从枯木战斗候选排除，东尼永久奖励仍可获得它；其他`CanBeGeneratedInCombat=false`原有排除保留。
3. `MakeTempCardInHandAction`：手牌上限10，超出转弃牌堆；生成普通复制并保留生成增强的原版机制。这里映射为STS2原生生成命令及对应钩子，而非搬运STS1引擎类。

提取文件/运行时文件与SHA256：

| jar条目 | 运行时目录`MaidenSuccubus/images/relics/sts1/` | SHA256 |
|---|---|---|
| images/relics/deadBranch.png | dead_branch.png | BEBF6F98561129085E5BF97E3AC5BE5E4C06765E77FE94D6B982B61401A20C60 |
| images/relics/outline/deadBranch.png | dead_branch_outline.png | 4CB289AC775C62D00A7B33F6A9E3804071909D43CA7153DFF0867CADC868183E |

## 验证

- `python scripts/TestDesignSyncRandomRelics20260927.py`：6项通过，含平铺候选、原生入口、存档/选择守卫、两图标精确哈希、登记/文本、游戏脚本接线。
- `scripts/ValidateMvpContent.ps1 -ProjectDir .`：通过，33遗物登记与契约一致。
- `dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`：通过，0警告0错误。
- 没有重新运行无关全卡审计、全量12门，也未写入并行审计输出。

游戏入口：在可丢弃的单人天音存档分别于战斗外、活动战斗中执行`ms_test_random_relics confirm`。入口会清理牌组/遗物或重设当前战斗，不能在保留存档执行。非战斗组测0/1/4可移除牌、不可移除牌、每次拾取移除奖励、后续永久移除、独立预测Rewards RNG、保存回执和非战斗不触发；战斗组测0/10手牌、真实消耗生成历史/归属、普通移动不触发、永久牌组不变。共用候选组测三个路线合并、唯一稳定槽位和治疗排除范围。

这些入口**仅编译未运行**。仍待实机确认：两组断言执行、真实商店交互/生成动画、游戏结束边界、多人归属、图标大小与加载。状态IMPLEMENTED，不标VERIFIED。九项已确认答复剩Q14～Q17；其他大范围未完成项见当前状态文档，全目标未完成。
