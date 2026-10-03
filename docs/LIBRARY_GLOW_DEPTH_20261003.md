

## 2026-10-03 书库发光绘制层级回修（LIBRARY-GLOW-DEPTH-01～03）

前置快照`6182fb9a9ecd0c41baaf85f1b05d56c71420f43c`。用户实测截图确认上一原生光效版本仍将整个卡面染红/绿；上一版未通过视觉验收。本批只修绘制层级，七牌数值、相邻消耗/重放与卡图无变化。DesignDoc相对上一已部署版本逐行/词级无漂移，复核书库完整条目及Plan/追踪，仍按原生边缘光效需求回修，IMPLEMENTED，游戏内NOT_RUN。

直接从当前正式游戏SlayTheSpire2.pck提取`scenes/cards/card.tscn`和`shaders/card_ripple.gdshader`核对：CardContainer子节点按Shadow→Highlight→PortraitCanvasGroup→AncientBorder/Frame→文字顺序绘制；Highlight使用SDF纹理、blend_add材质，fragment生成的alpha包含整个内部区域，不是空心轮廓。原版把该层放在卡图与牌框之后，让卡图/牌框遮挡中心，仅外缘发亮。上一版AddChild把复制光效放在CardContainer最后，越过遮挡层，以加色混合染亮整张牌。此为已确认的层级错误，不是颜色/动画或卡图资源错误。

修复：新增光效后立刻MoveChild到原生Highlight紧后一位，在PortraitCanvasGroup及所有牌框/文字之前绘制；保持与原生Highlight相同ZIndex和ZAsRelative。源代码注释明确SDF中心为实心、必须依赖原生卡面遮挡，防止后续再次把它放到顶层。红/绿独立材质、原版纹理/Shader/动画、双效果双色裁剪以及离手/池复用恢复继续使用既有实现。

验收：普通与先古框型受影响牌只显红/绿外缘、卡图/标题/费用/描述保持原色；双书库中间牌红绿双色且中心不染色；悬停放大/拖动/换位/离手及普通蓝光恢复。只读原生资源与源代码检查不宣称视觉运行时通过；按用户要求不运行静态测试，仅编译，完成后仅本地游戏部署，沙箱/ModUploader/安装JSON不动。
