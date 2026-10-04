

## 2026-10-04 紫音开局布局回修（START-003 / START-SHION-01～06）

前置快照`d377f7c44d569017b84830816077c530ce19b83d`，设计提交`5db6b1085d144c3d03bd65fd3e50314eb04365ec`。已完整读取本地AGENTS/DPP、START章节及封印/永久解锁/同角色牌组遗物/多人规则，复核Plan与追踪既有START-PREVIEW及START-SAVE记录；相对HEAD和8dd5e93逐行/词级漂移均保留。只有按摩事件叙述仍为用户未提交变化，机制不变，工作树/快照保留，不混入本次设计或代码提交。本次用户截图确认大方框遮挡原生开始按钮且未落实紫音布局，回退旧布局验收结论，重新实现；上一批修复已按用户本轮部署指令安装本地，报告见outputs/humility-retain-fix-20261004。

实际参考F:/steam/steamapps/workshop/content/646570/3242483596/VUPShionMod.jar；读取SkinManager、SkinCharButton、SkinInfoLabel方法字节码并保存到聊天work/compat-sandbox/shion-ui-reference-20261004。SkinManager以右侧656宽基准滑动，SkinInfoLabel.update信息按x=180+60*i、y=屏高/2+430-80*i错位排列，SkinCharButton.setPos选中scale1、相邻scale0.7、斜向相邻位置、uiLerpSnap过渡及独立selected/locked；锁罩和头像分层，锁定项仍能切换，confirm在当前锁定项禁止。移植该信息/轮转/锁定分层与缓动结构到STS2 Godot原生NButton，不复制紫音角色美术或Java运行时。

| 验收ID | 技术任务 | 状态 |
|---|---|---|
| START-SHION-01 | 去掉420×490圆角大方框和横向文本tabs，右侧上部四条横线信息（获取条件、初始堕落、封印效果、可开始状态），按层错位。下部3个斜切天音头像围绕当前选中项排列；名称与初始值写在头像条内，默认头像使用最新正式选角图 | READY |
| START-SHION-02 | 当前项scale1、上下邻项0.7，选择时位置/尺寸缓动；路线上色轮廓与轻量高亮，锁定项暗化加锁罩。已知没有独立路线选角美术，不假装更换角色立绘，同一天音头像按路线标识区别 | READY |
| START-SHION-03 | 全部装饰与按钮在独立屏幕坐标区域内；根据ConfirmButton实际变换边界预留至少28像素间隔，平移缩放一次，不再混合右下anchors/offsets/scale。监听屏幕/原生确认按钮布局变化，窄窗口同比缩小；根节点Ignore、仅斜切头像点击区域Stop，不截获开始按钮输入 | READY |
| START-SHION-04 | 保留锁定浏览→确认禁用→OnEmbarkPressed入口再次验证，与原生准备/取消准备/锁定其他角色恢复边界；联机只本地桶，退出关闭、在其他角色不显示 | READY |
| START-SHION-05 | 不改牌组、初始遗物/解锁资源、保存字段/封印数值，±3开局与永久解锁沿用已授权实现，原生遗物切换不受影响 | READY |
| START-SHION-06 | 仅构建，不静态测试；需用户原生游戏手测缩放/全部路线/锁定/开始按钮。源码完成标IMPLEMENTED，不凭构建或参考示意标VERIFIED | READY |

只修改3个界面代码文件和范围设计/计划记录，资源PCK与现有JSON原样继承；保留刚部署的谦逊/保留/碎片折扣/福音/堕落诊断。当前批完成后供后续UI实测，前置部署和本批新UI是否安装分别记录，未修改上传器或沙箱。

实施完成：信息条目/斜切头像轮转/锁罩/原生按钮实际边界定位均已实现，START-SHION-01～06代码状态IMPLEMENTED。沿用原永久解锁与确认门禁；首次编译的DrawPolygon数组/span重载歧义已通过显式Color[]修正，最终Debug构建通过。未执行静态测试与游戏内验收，本批新UI尚未部署，不混称前置已安装的修复。
