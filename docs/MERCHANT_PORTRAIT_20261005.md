

## 2026-10-05 商店立绘与假商人尺寸回修（FAKE-MERCHANT-SHOP-01 / SYS-ASSET-MERCHANT-001，READY）

前置快照`0fd6e054bc7b026def3f567f154b0105fef12ea0`，设计提交`3d8ecfe5bd37b8c10836f04c97a96475d331513a`，接受基线`5962c8780461301defca9d8c4b5bbad5ef3d4e6e`。核对分支/HEAD/状态/最近提交，按独立index提交本批范围，未触碰真实index、旧删除与其他工作。DesignDoc相对HEAD和接受提交的逐行/词级差异留档，完整复核SYS-TRF-004与既有FAKE-MERCHANT-SHOP-01；其余用户设计/文案保留。上批原版变化三路线候选与眼罩修复均包含。

证据：对比本地已打包combat-text-pause、control-net-id、texture-memory三个发布候选，商店PNG哈希相同（1024×1536），场景局部scale均0.35；主要差异为user://外部纹理.tres改.res。godot(6).log曾缺失merchant_texture.res，本地12.18.21.log有.res复用缓存且正常进入商店；这支持缓存/版本配对嫌疑，不证明当前玩家报告唯一原因。未读取本次玩家新日志，未在本机复现消失。RitsuLib0.4.64与本机运行0.6.5 Runtime源码确认FakeMerchant.AfterRoomIsLoaded由库Prefix接管，创建NMerchantCharacter且绕过StartCharacterAnimation原补丁。原版0.111.0事件CharacterContainer.scale=1.75，普通商店=1，截图约1.75倍与此一致。

技术：补丁公开ModWorldSceneVisualNodeFactory.TryInstantiateMerchantCharacter，只对MaidenSuccubusCharacter构造自有静态NMerchantCharacter，RuntimeTextureAssets按需读PCK内PNG，不重写缓存器/不做图像缩小；共用Sprite参数0.35、position(0,13.65)、底部居中offset。框架负责原生布局/注册，派生Ready与仅本类型PlayAnimation不启动Spine。新增FakeMerchant.AfterRoomIsLoaded Postfix最后执行，对已创建自有商店根抵消事件根以下全部父变换的缩放，参考原生商店场景父scale，赋绝对值幂等。旧NCreatureVisuals创建路径前缀复用同一素材与缩放函数作为fallback，移除对用户纹理.tscn实例的读取。所有异常Safe.Run，无新依赖/后台线程/队列/RNG变化；本次不改PCK素材和安装JSON。

验收MP-01：无旧用户纹理的新安装商店可见；MP-02：旧缓存/升级素材加载一致，缺PCK PNG日志明确并保留fallback；MP-03：fake_merchant与普通商店同高、头部不被异常放大裁掉，脚底位置不移动；MP-04：Ritsu0.4.64/0.6.5流程、多人其他角色不受影响，重复布局不重复缩放；MP-05：原版变化与眼罩上批待验内容保留。按用户要求不做静态测试，只build；游戏内NOT_RUN，完成仅标IMPLEMENTED，未授权部署，本地/沙箱/ModUploader/JSON均不写。


FAKE-MERCHANT-SHOP-01 / SYS-ASSET-MERCHANT-001 IMPLEMENTED（2026-10-05）：前置`0fd6e054bc7b026def3f567f154b0105fef12ea0`，设计`3d8ecfe5bd37b8c10836f04c97a96475d331513a`，计划`eb66eab679ff17a45f1cd083147879cf357077b5`。普通商店改由公开RitsuLib工厂按需从包内PNG构造静态天音商店节点，共用Sprite尺寸/脚底锚点；假商人布局完成后抵消父级额外缩放，兼容库NMerchantCharacter与旧NCreatureVisuals双路径，重复调用幂等。仅本模组商店视觉不启动Spine，其他角色/普通商店尺寸/战斗/火堆不变。玩家消失原因尚未运行时确认，版本对比资源未删除而用户缓存路径发生变动，旧玩家日志缺该资源，现消除此加载依赖。Debug构建命令`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过0警告0错误；静态测试NOT_RUN、游戏内NOT_RUN，未部署，不写安装JSON。
