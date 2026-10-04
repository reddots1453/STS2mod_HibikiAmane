

## 2026-10-04 商人？？？与普通商店立绘一致（SYS-TRF-004 / FAKE-MERCHANT-SHOP-01）

前置快照`2553862bcac9e060bf572b7d4bbe48040431b409`，设计提交`4bdf71f898331b5d38eca666991fd6f2c8e59e72`。用户修正了上一轮固定半尺寸需求，旧FAKE-MERCHANT-SCALE-01被本项覆盖，不能继续固定乘0.5。DesignDoc相对HEAD及ab3999bf逐行/词级漂移已记录，复核SYS-TRF-004与前批任务，其他用户文案/按摩设计保留不改。新检测到瘴雷条目的独立设计编辑：每获得欲望额外伤害、动态总伤害与次数，已完整读取相邻体系输出条目和计数边界；其玩法变更回退READY，技术结论为待独立同步实施，不在本次事件立绘修改中编码或声明完成。保留工作树和快照，范围设计提交不混入该编辑。

普通商店NMerchantRoom取MerchantAnimPath，天音商店Sprite2D位置(0,13.65)、比例0.35、1024×1536纹理，CharacterContainer原生比例1；FakeMerchant取战斗CreateVisuals，其CharacterContainer额外比例1.75。两套图和坐标不同，不能只抵消1.75或比较纹理尺寸。技术任务READY：仅天音的FakeMerchant角色实例复用实际普通商店Visuals Sprite2D全部配置，隐藏该事件实例的战斗Art，不改战斗资源；从普通商店SceneState读取CharacterContainer比例，与实际事件父容器比例相除，乘商店模板根Scale及原实例Scale。保存首次比例且不重复添加图像，重复调用幂等。特殊事件没有Spine的商店静态图不调用原生relaxed_loop，其它角色仍执行原生动画。

获取只读场景模板后立即Free，不打开新商店或触发原生房间脚本；目标比例有效和纹理加载成功后才隐藏旧图。事件中原有Position/脚底根锚点保留，后续戦斗重新CreateVisuals显示原图和尺寸。确认构建0警告0错误后IMPLEMENTED，视觉由用户手测。不运行静态测试；前批试炼UI仍未部署，本批累积产物不自动安装、不改JSON/沙箱/上传器。
