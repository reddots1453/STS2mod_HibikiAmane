

## 2026-10-03 娅露丝书库相邻牌仅边缘发光（DS27-02AL / CARD-N先古书库）

前置快照 `84f6788ff2e9b228532c40273c0185ba58cd6b4c`。用户截图显示绿色覆盖整张卡，违反DesignDoc现有“左侧红色发光边框、右侧绿色发光边框”要求；相对上一接受提交DesignDoc无新增漂移，完整复核书库相邻持续效果。只回修视觉，不改变相邻规则、消耗/重放、卡牌数值或卡图资源。

实现任务：LibraryAuraOverlay的StyleBoxFlat显式DrawCenter=false、ShadowSize=0、透明背景及零ShadowColor；以缓存的四圈圆角轮廓描边、由外向内透明度0.08/0.16/0.30/0.95模拟边缘柔光，最大外扩6px，中心没有任何几何填充或矩形阴影。左侧红、右侧绿，双效果红外圈/绿内圈间隔9px；继续继承卡牌坐标、旋转与缩放，忽略鼠标，原生可打出高亮保留。仅手牌中的实际相邻实例显示，移动/打出/书库离手后按原有查询刷新与清理。

验收LIBRARY-EDGE-01～04：左右单侧悬停大图与普通手牌均仅边缘显色、图和文字保持原色；两本书库中间牌红绿两条可辨；移动手牌/书库离手/打出后无残留；消耗与重放仍按原有规则生效。IMPLEMENTED，游戏内NOT_RUN；按用户要求不运行静态测试，暂不部署，不修改正式游戏/沙箱/ModUploader或安装JSON。新样式在DLL中构造，无新增资源/PCK变更。


构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误；未运行静态测试与游戏内验收。独立产物outputs/library-edge-20261003继承前两批待部署开局/转阶段修复，PCK原样复用phase-priority-20261003。继续暂不部署。
