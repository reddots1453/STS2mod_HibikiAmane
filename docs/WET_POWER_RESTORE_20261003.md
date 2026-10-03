

## 2026-10-03 高欲望湿了状态恢复回修（WET-RESTORE-01～05）

前置快照`508328fc01dee2b307be6026b2ae838bd33cc18e`。SYS-DES-002A/B明确欲望≥8获得湿了、低于8移除；既有规则回修，不改DesignDoc。相对最新接受版本055fcff2逐行/词级无漂移，复核欲望完整规则、跨战斗资源、拘束8点绕过格挡以及既有M2/读档Plan和追踪。当前ControlCmd本来直接读欲望≥8判断绕过格挡；本批补状态同步，不改变拘束判定或数值。

日志证据：最新godot.log记录WetPower成功注册为MAIDEN_SUCCUBUS_POWER_WET_POWER，未发现湿了/欲望变更监听施加异常。最近22:31:58归档仅到主菜单退出，不含该实测战斗，不能用注册日志宣称已复现阈值施加。正在记录的godot.log含多场继续读档/怪物欲望意图及楼层20战斗；只读当前保存的secondary_resources和desire_amount均为9。读档窗口Temptation.Get Hand异常属于前批已修但尚未部署的代码，不混作本次湿了直接根因。日志没有每次欲望变更/Power施加明细，因此不能逐次确定首次缺失时点。

确定的代码缺口：湿了仅在AfterSecondaryResourceChanged中创建/移除。原生Player.AfterCombatEnd移除全部Power，下一战欲望持久值仍在；RitsuLib.RestoreSnapshot→SetFromPersistence采用emit:false，不调用资源变化Hook。已核对本机实际加载的Workshop RitsuLib.Runtime 0.6.5（compat/0.111.0）恢复路径，亦不发布资源变化。模组RestoreForCombat只发等值UI通知，不补WetPower；因此保留8/9欲望跨战斗或读档进入战斗时会缺少状态，直到发生下次数值变化。注册/图标资源缺失不是已找到的根因。

修复计划：提取await SyncWetPower统一阈值状态同步，用实时欲望值检查；资源变化继续调用；角色BeforeCombatStart在持久值恢复后await同步，保证进入首回合前状态就绪；每次AfterPlayerTurnStartEarly先处理满值惩罚，再幂等检查，兼容继续及特殊战斗入口。存在时不再施加，不叠层、不重复发动画，低于8移除。仅有效战斗中操作，离战斗/结束/死亡不创建Power。通过原生PowerCmd正常施加/移除，保留其负面状态类型及原生免疫/Mod修正行为，不强制绕过其他Mod。施加后再次读取：成功/移除记录DesireStatus，仍缺失记录数值与原因方便定位阻止来源。

验收WET-RESTORE-01保存8/9在下一战或继续进入首回合前可见湿了；02正常7→8获得、8→7移除；03满值恢复/惩罚10→3后无湿了；04连续9点回合/多次恢复不叠层、低欲望新局不创建；05玩家死亡/战斗结束不重加，多人各角色状态独立，若原生或Mod阻止施加日志明确。IMPLEMENTED待手测，不运行静态测试，仅build，继续暂不部署。修复包继承前批欲望UI、瘴雷次数、combat_feedback分组，不改安装目录、沙箱、ModUploader、任何JSON或用户存档。


最终构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误。未运行静态测试，游戏内NOT_RUN；outputs/wet-power-restore-20261003保存DLL/PDB及继承上批的完整PCK/分组配置。暂不部署，安装配置与存档未改。
