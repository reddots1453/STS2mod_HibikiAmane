# 女神试炼背景接入（2026-10-03）

用户直接确认指定候选目录中的四张图加入实现。状态IMPLEMENTED，游戏内待验；对应ACT4-001既有公共叙事、双栏选择与选定后全屏叙事规则，不调整试炼流程、条件、奖励或存档。

- title.png：选择前上方标题与公共叙事区，1840×430。
- sin.png：七宗罪选项，774×504。
- virtue.png：七美德选项，774×504。
- narrative.png：确认后与中断恢复后的整屏叙事，1840×960。

TrialBackgroundArt复用RuntimeTextureAssets加载原始PNG，采用IgnoreSize与KeepAspectCovered等比覆盖。背景子层不接受鼠标或焦点，不抬高布局最小尺寸；文字保持原滚动容器。轻度暗化、正文描边保证阅读。新界面背景分区跟随选择前/确认后切换；多人旧路线选择器复用两张选项图，奖励UI不改动。素材缺失时仍保留原底色，确认/继续按钮与生命周期不受影响。

正式素材见图片素材/女神试炼背景/20261003，运行时见MaidenSuccubus/images/ui/trial。目录内四图预览不纳入游戏资源。

前置快照：7f983631d217708ac1a5d3b3d3482ac467235ca8。独立测试DLL/PDB/PCK保存在聊天outputs/trial-background-debug-20261003。不运行静态测试，不部署正式游戏、沙箱或ModUploader，上传器JSON不修改。

游戏内验收入口：正常Neow前开场、首次无Neow的地图开场、选择后中断再继续、两条路线长文本滚动与按钮点击、不同窗口宽高比例、多人兼容选择界面。


最终构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 成功，0警告0错误。不运行静态测试；游戏内验收NOT_RUN。独立DLL/PDB/完整PCK已保存于聊天outputs/trial-background-debug-20261003，资源条目683个，包含本轮四张试炼背景及此前接入的当前正式美术。未部署正式游戏、沙箱或ModUploader，上传器JSON未修改。
