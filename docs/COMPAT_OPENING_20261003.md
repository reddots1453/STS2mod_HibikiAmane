# 首次无涅奥开局与合集日志诊断（2026-10-03）

范围：ACT4-001 开场入口兼容；不改变试炼条件、奖励规则、旧跑局已选路线或多人流程。变更前快照 `3b7951a26a5c30261a8517c42db8f44f72cefe59`。

用户确认首次游戏没有涅奥。原新版入口仅在 Neow.SetupLayout 的等待点触发；地图入口仍使用 FourthRouteSelectionScreen，导致回退到旧 UI。现在单人地图入口复用 FourthRouteOpeningScreen 公共叙事、双栏选择和选定后叙事，保留相同收据与沉睡遗物授予流程。已选路线但叙事未确认完成时也可从地图恢复；旧存档已选路线而没有新版开场收据时不重播。多人保留现有选择同步边界。

诊断标记：`await-injection=installed`、`entry=neow`、`entry=map-fallback` 与 `completed`；Neow 入口附加 SetupLayout/MoveNext 的 Harmony owner 列表，共享方法只表示补丁重叠。

本次沙箱日志 full-20261003-000605.log 的战后奖励多次成功领取。行12559执行控制台 win，行12571出现 MayhemPower 与 Transform 的 PlayerChoiceContext 栈不匹配；行12702/12717继续成功领取金币和卡牌，故不能认定为首战奖励卡死原因。此错误仍需自然胜利与自动出牌组合复现。BlindfoldIntentHoverPatch 在 NIntent.OnHovered 上安装失败；Ovelle_White 工坊包缺少 PCK；CVC 提示14个待修补模组先于它加载。BetterCharacterRelics 的 Conflict 信息是共享补丁归属记录，不等于已确认功能冲突。

按用户要求只 build，不运行静态测试。Debug build 使用 DeployMod=false/ValidateMod=false；游戏内首次开局、正常 Neow 开局、叙事中断恢复待用户实测。仅准备沙箱测试构建，不部署正式游戏或 ModUploader，不修改其 JSON。


构建结果：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 成功，0警告0错误。未运行静态测试，未进行游戏内验收。独立Debug DLL/PDB保存于聊天 outputs/opening-fallback-debug-20261003；尚未替换任何运行中的游戏或沙箱模组。沙箱启动脚本另行修正为保留各配置已保存的加载顺序与单项启用状态，兼容原生序列化的来源名称与配置中的数值来源。
