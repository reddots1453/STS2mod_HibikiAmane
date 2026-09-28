# 第76批：变更范围内两处遗留数值

日期2026-09-28；前置`554718f9`，计划`0bf69eee`。DesignDoc逐行/词级无漂移。

- 更换胖次：旧Threshold 6、升级5；当前DesignDoc要求5/4。生产变量改5并保留升级-1，两个版本均检查低于/等于/高于门槛。2费及两个状态效果不变。
- 欲望鞭挞：旧伤害7、当前要求5；生产变量改5，升级仍只1费→0费。已有实际伤害测试预期同步为5，增加变量/费用断言；其余效果不变。
- 两牌描述读取Threshold/Damage，生产变量修正同时同步卡面，没有新增硬编码描述。
- 两个原Probe都仍用旧期望；因此此前静态/编译通过不构成与当前数值一致的证明。本批直接用DesignDoc作为独立依据。

验证：`python -m unittest discover -s scripts -p 'TestDesignSyncRemainingNumbers20260927.py'`，3/3通过；`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore`成功、0警告0错误。游戏场景仅编译，未运行。

遵守用户最新指示，仅检查变更范围与直接依赖，不重跑全卡/全量门。不部署、不改并行素材或暂存内容。其余任务及待答设计问题仍保留。
