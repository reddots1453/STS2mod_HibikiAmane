

## 2026-10-03 娅露丝书库原生轮廓发光（LIBRARY-NATIVE-GLOW-01～05）

前置快照 `827b8173a7b0f9087bd4bc1bef0d43a172455cfb`。用户截图确认细线描边不符合期望，应复用原版可打出卡牌的蓝色边缘光效并改色。DesignDoc相对最后接受提交无漂移，完整复核先古书库相邻规则与最新Plan/追踪：左邻消耗红光、右邻重放绿光，仅手牌相邻持续生效。本批替换上一LIBRARY-EDGE轮廓细线方案，CARD-N先古书库玩法不变，IMPLEMENTED、游戏内待验。

原生依据：v0.111.0 NHandCardHolder.UpdateCard使用CardHighlight.AnimShow/AnimHide和Modulate可打出蓝色/条件红色/金色；NCardHighlight复用TextureRect ShaderMaterial的width（0→0.075、0.5秒Cubic Out）。本批删除StyleBoxFlat所有描边/_Draw，创建原版NCardHighlight节点，直接使用当前卡牌高亮的纹理与同一Shader、独立Duplicate材质，仅改Modulate为红/绿，并调用原生AnimShow。节点不复制活跃Tween，不共享width参数，不新建贴图或shader。

受影响卡牌临时隐藏自身原蓝光leaf的SelfModulate Alpha以避免颜色混合，原版UpdateCard继续正常更新颜色与Tween；失去相邻关系/离手/离树时恢复保存的SelfModulate，其他牌继续原生蓝光。每帧跟随实际原生高亮的局部Position/Size/Scale/Rotation/PivotOffset与Texture；只有效果变化才刷新文本和播放动画。双效果使用同一原生轮廓的左右两份裁剪，左半红/右半绿，同时显示且不叠出黄光；裁剪外围足够宽以保留柔光。NCard池退出清理，避免复用到其他牌时颜色残留。

验收：左邻整体原生红光/右邻绿光、标题与插图保持原色；两书库中间牌红绿双色且仍具有消耗+重放；不可打出的受影响牌也显示对应关系；换位/离手后恢复普通可打出蓝光或原生不可打出状态；悬停/拖动/抽弃牌/卡池复用无残留、材质互不干扰。按用户要求仅编译，不运行静态测试。最新部署范围仅本地游戏，沙箱/ModUploader/安装JSON不动；构建完成后若游戏仍运行则等待退出，不替换占用DLL。资源无改动。


构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误。未运行静态测试/游戏内验收。独立产物聊天outputs/library-native-glow-20261003，包括DLL/PDB和原样复用此前完整PCK。实现提交已完成，安装部署单独记录，不写沙箱/上传器。
