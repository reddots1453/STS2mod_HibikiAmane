# DS27第95批：谦逊觉醒正式钩子（2026-09-29）

变更前快照`a5ec582e`，先行计划`1f262f41`。DesignDoc行级/词级无漂移。本轮从上一状态报告确认的实际旧入口继续，未重复全范围审计。

## 实现

- 正式HumilityRouteRelic.AfterCardPlayed不再按句号切分数量判断，符合新判定时调用原生CardPileCmd.Draw(context, 2, Owner)。原Stage<3、卡牌拥有者与谦逊本牌排除保留。
- 生成目录schema2额外保存onlyDamageAndBlock，记录原卡结构证据，而非“删除其他效果后的程序非空”这一错误推断。原始抽牌、状态、条件触发、其他钩子、字段增长都不能因成功抽取被误判为纯伤害。已识别纯视觉不影响该标记；辅助展开记录被删的字段操作证据。
- 新HumilityAwakening运行时入口：改写实例读取实际非空程序；未改写实例读取独立原始证据。空程序不触发，临时挣脱投影不借原卡触发；原关键词和额外负面描述不计为纯，原附魔独立保留。
- 未改写牌的CanonicalKeywords即使与附魔关键词重合也不能免除。改写后已删除的原关键词不因CanonicalKeywords仍有定义而复活。
- schema1旧目录不虚构纯度，schema2缺失字段/空程序冒充纯度拒绝。无本地化文本长短启发式，无新增逐牌表。
- 实际游戏脚本通过RelicCmd.Obtain注册遗物，再走ctx.Play而非手工直接调用遗物回调；覆盖阶段2/3/4、纯攻击/格挡、剑柄打击改写前后、Swift附魔、空效果附魔、消耗牌改写前后。脚本只编译未执行。

## 验证

```powershell
dotnet run --project tests/HumilityCallExtraction --no-restore -- --self-test
dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false
python scripts/TestDesignSyncHumilityRuntime20260927.py
dotnet run --project tests/HumilityCallExtraction --no-build -- --verify-assembly MaidenSuccubus.dll
```

- 59通用场景、15静态接线检查通过；Debug零警告零错误，实际DLL资源读取通过。
- 原始分类实际样本：MaidenStrike/MaidenDefend/StrikeIronclad/IronWave/Whirlwind为纯效果；PommelStrike/Bash/Claw/Surf不是。按源码结构得到，不按卡名登记。
- 抽取总数仍734/28，纯度是另一维度，不能把734全部当作纯效果或完整谦逊覆盖率。
- 范围diff检查通过，无游戏执行，无部署，不重跑无关全套。

## 未完成

这是正式觉醒钩子的回修，不是完整谦逊完成。原始源码无法给出结构证据的牌（含尚未支持/继承入口）当前保守不触发，完整分类覆盖仍需补齐；实际游戏行为未验。谦逊正式选牌仍未切换，剩余调用、旧表迁移及全目标其他范围不缩减。
