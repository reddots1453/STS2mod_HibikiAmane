

## 2026-10-03 读档欲望侧栏回修（DESIRE-LOAD-UI-01～05）

前置快照`e07b2ad459d7291fb84741e384b82fceeee6a383`。SYS-DES-001已有跨回合/跨战斗/存读档及左侧UI要求，用户实测上一修复仍显示0；本轮是既有需求回修，不改DesignDoc。已保存相对HEAD及最新接受版本e8f58833的逐行/词级差异；相对最新接受版本无漂移，并复核欲望完整章节及既有Plan/追踪。

只读当前存档证据：desire_amount玩家桥接Amount=6/HasValue=true，RitsuLib secondary_resources对应玩家欲望也为6；磁盘值未丢失。上一实现以战斗对象引用存在判定实时资源已准备好，而RitsuLib Get在资源尚未挂接/该资源尚未初始化时返回默认0。原生SetUpCombat先ResetCombatState，资源持久值在稍后的BeforeCombatStart恢复。RunLoaded在InitializeSavedRun之后、NRun/UI创建之前发布；旧UI订阅不回放，只刷新一次，仍存在错过绑定/恢复通知的窗口。上述为源码顺序证据，用户场景尚未由本轮运行时日志复现，不宣称所有路径已实测。

实现计划：侧栏首次绑定和重新显示时从持续更新的玩家桥接读取，旧存档继续读取原生已注册快照；实际资源读取必须确认资源已存储，不能把未初始化的默认0当成真实0。RunLoaded通知采用等值刷新，UI回放已发生的加载事件并在可见性恢复时刷新，未绑定时收到资源变化可重新绑定；避免重播欲望增长、满值语音/CG。战斗恢复兼容无桥接旧存档，并显式初始化实际0值。增加单次加载/战斗恢复数值诊断，便于后续实测核对。

关联错误：最新godot.log在Continue加载窗口两次记录Temptation.Get读取Hand抛出“Tried to get Hand pile while out of combat”；Creature.CombatState已存在但PlayerCombatState未建立。改为仅从已有PlayerCombatState.Hand统计，不触发原生GetPile异常。此为同一侧栏初始化边界修复，不改变诱惑度公式。

验收：DESIRE-LOAD-UI-01地图读档6立即显示6；02事件/火堆读档保持数值并可正常增减；03保存值真实0不可回退旧非零值；04继续进入战斗前后数值一致、战斗内支付/涨跌正确、读档8/满值不重复播演出；05首次游戏0、换存档/角色/多人仅本地数据，侧栏显隐仍遵从顶栏，加载窗口无Hand异常。状态IMPLEMENTED待用户手测。按用户要求只build，不运行静态测试，不部署正式游戏、沙箱或ModUploader，不修改安装JSON/用户存档。


最终构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误。未运行静态测试，游戏内NOT_RUN；outputs/desire-load-ui-20261003保存独立DLL/PDB及未改完整资源PCK。用户最新指令暂不部署，安装文件/存档/ModUploader均未改。
