# 第77批：九项答复同步及能力叠加

日期2026-09-28；前置`6ffb39e2`，设计/计划`0e679b6f`。九项答复完整写入DesignDoc，全部原OPEN问题转READY。

## 本批完成

- Q12：`ChainDestructionPower.InstanceType=Instanced`，对照本地原版`OrbitPower`实现；独立Power各自显示距下一触发的消耗数。已有待重放Power的Amount作为剩余牌数，每张+1次、每次整组出牌减1，不累加到同一张。
- Q13：子守歌每次施加1层，每层生成1张困了；循环全部生成后读取最终手牌数，再获得手牌数×层数×2格挡。卡面的每牌格挡变量与Power层数分开，动态悬停显示生成张数与每层收益。
- Q19：生产实现已符合答复：每次AfterCardPlayed抽牌，原版OnPlayWrapper整组重放后消耗。不改写原生顺序；新增Glam重放、每次抽牌和单次最终消耗的历史顺序断言。
- 连锁测试错开两次能力施加的时点，实际消耗牌观察两套独立计数，积累3个待重放后连续打4张，验证前三张各2次、第四张1次。子守歌1/2/3层走Hook.BeforeFlush并检查最终手牌及格挡。

## 验证

- `python -m unittest discover -s scripts -p 'TestDesignSyncConfirmedStacks20260927.py'`：4通过。
- `python -m unittest discover -s scripts -p 'TestDesignSyncNeutral20260927.py'`：8通过。
- `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore`：最终0警告0错误。首次新脚本对IEnumerable误用Count属性导致编译失败，修为Count()后通过。
- 引擎脚本仅编译，未运行；未部署，未改其他Agent暂存/素材。按用户要求不重复全量审计。

## 后续

Q14谦逊、Q15/Q16供奉、Q17原版免费、Q18/Q20三路线合池与STS1枯木参考均已明确，继续READY待实现，不再追问。其他原目标仍保留；本批不宣称全范围完成。
