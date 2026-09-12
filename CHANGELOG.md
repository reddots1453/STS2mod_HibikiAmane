# Changelog

## 2026-09-12 — 校服裤袜脚部连续性修复与永恒天衣三档立绘

- 变更前快照：`maiden-pre-school-tights-eternal-robe-assets-20260912` → `8680191`；未修改或提交玩家正在编辑的`DesignDoc.md`。
- 修正普通校服形态的小腿—脚部色泽断层：以原始裸腿/短袜图层作为分类遮罩，将鞋口上方脚部统一到现有白色连裤袜的实际暖白调色板，并只在旧袜口内部区域进行羽化，保留鞋面、外轮廓和腿部阴影。
- 更新`BuildNormalWhiteTightsAsset.py`，使裤袜色泽连续处理可重复生成；同步刷新运行时`character_normal.png`与`图片素材/变身形态/校服形态/普通校服.png`归档副本。
- 新增`BuildEternalTransformationAssets.py`，使用原作光之力解放差分`01_0046b/01_0024b/01_0025b`分别生成永恒天衣魔装耐久3/2/1立绘；下肢复用对应耐久档的已验收白蓝金魔装腿甲，补全原作立绘在STS2画布中缺失的脚部。
- 新增运行时就绪文件`character_eternal_armor_3/2/1.png`及其本地原作服装层副本，并在`图片素材/变身形态/永恒天衣/`集中归档。所有输出均为`922×1250 RGBA`透明立绘，画布、人物锚点与脚底位置一致。
- 部署：否；本轮完成可供后续程序接线的角色素材，不修改形态切换逻辑或部署Mod。

## 2026-09-12 — “终极耀斑”定稿与完成版卡图集中归档

- 变更前快照：`maiden-pre-final-card-art-consolidation-20260912` → `2ba7816`；未修改或提交玩家正在编辑的`DesignDoc.md`。
- 玩家确认`014_UltimateFlare_abstract_overhead_v03.png`作为“终极耀斑”正式卡图；将选择清单来源切换至该版本并设为`accepted`，同步更新`selected/014_UltimateFlare.png`。
- 补记玩家此前对燃烧手环`t03`的正式确认，将其状态由`recommended`改为`accepted`；光子伏特仍为推荐候选、精液变换仍为暂定候选，均未混入完成版。
- 新建`图片素材/完成版卡图/`，按`三位编号_英文类名_中文卡名.png`统一归档燃烧手环、心眼、终极耀斑、黑暗元素和反射屏障五张完成版；新增README与包含来源、状态和SHA-256的清单。
- 验证：五张图片均为`1000×760` PNG，完成版副本与各自已确认源文件SHA-256一致，清单JSON可解析。
- 部署：否；本轮只确认并整理卡图资产，不修改或部署Mod运行时代码。

## 2026-09-12 — 校服形态归档与原作永恒天衣素材核查

- 变更前快照：`maiden-pre-school-form-archive-eternal-audit-20260912` → `2763828`；未修改或提交玩家正在编辑的`DesignDoc.md`。
- 将当前运行时使用的白色连裤袜版`character_normal.png`复制到`图片素材/变身形态/校服形态/普通校服.png`，作为普通校服形态的集中归档副本；保持`922×1250 RGBA`、人物锚点与脚底位置不变。
- 核查确认原游戏素材确实包含“永恒天衣”：装备ID 65使用`PID:46`和`actor01change_0005`行走图，`cloth/`目录内存在九种站立姿势的`0046`基础立绘层与`0046b`光之力解放差分。
- 同时确认装备ID 315/316分别定义“永恒天衣（破损）”与“永恒天衣（严重破损）”，对应`PID:24/25`；两套均包含九种姿势的基础版与`b`强化差分。`CallStand.js`会在光之力解放/唯心解放状态下自动选用`b`差分。
- 部署：否；本轮只归档美术素材并记录原作素材核查结果，尚未合成或接入永恒天衣STS2战斗立绘。

## 2026-09-12 — “终极耀斑”马尾束环与具体背景移除

- 变更前快照：`maiden-pre-ultimate-flare-ring-background-fix-20260912` → `f2417eb`；未修改或提交玩家正在编辑的`DesignDoc.md`。
- 根据玩家反馈淘汰`014_UltimateFlare_red_overhead_v02.png`：该版侧马尾存在三枚不需要的粗金环，环形石地、断柱和瓦砾也构成了过于具体的废墟场景。
- 新候选`014_UltimateFlare_abstract_overhead_v03.png`移除全部马尾金属环，重建连续的浅金至浅桃粉发丝并保留蓝色根部发饰；背景替换为无可辨识地点的深海军蓝至黑色抽象魔法空间，仅保留克制的红色粒子和漫射辉光。
- 近乎垂直的高位俯视、向下收束的全身透视、双臂展开姿势、白蓝金魔法少女服装与头顶红白耀斑均保持不变；新图规范化为`1000×760 RGB`，保存高分辨率源图、完整生成规格和独立可重复规范化脚本，等待玩家确认。
- 部署：否；本轮只修正卡图候选，不修改正式选择清单或Mod运行时代码。

## 2026-09-12 — “终极耀斑”发型与高位俯视透视修正

- 变更前快照：`maiden-pre-ultimate-flare-overhead-hair-fix-20260912` → `c9f4ca7`；未修改或提交玩家正在编辑的`DesignDoc.md`。
- 根据玩家反馈淘汰`014_UltimateFlare_red_overhead_v01.png`：该版错误地将浅金长发改成偏粉棕色，同时镜头仍接近正面，未形成高位俯拍透视。
- 新候选`014_UltimateFlare_red_overhead_v02.png`将镜头置于角色正上方、以约75～85度向地面俯拍；通过可见头顶和肩部上表面、向远处缩短的躯干与腿部、无地平线的环形地面建立明确鸟瞰关系。
- 发型重新锁定旧参考图：浅金色长发、下段浅桃粉渐变、画面右侧高位侧马尾、三枚粗金环和根部蓝色饰件；继续保留青蓝眼睛与白蓝金魔法少女服装。
- 头顶保留单一红白高亮耀斑及猩红放射线；新图规范化为`1000×760 RGB`，保存高分辨率源图、完整生成规格和独立可重复规范化脚本，等待玩家确认。
- 部署：否；本轮只修正卡图候选，不修改正式选择清单或Mod运行时代码。

## 2026-09-12 — Bug修复自动部署规则与第四层修复交付

- 变更前快照：`maiden-pre-auto-deploy-policy-and-fourth-route-deploy-20260912` → `c9f4ca7`；未修改或提交玩家正在编辑的`DesignDoc.md`。
- 将玩家的长期交付要求写入`docs/DESIGN_CHANGE_PROTOCOL.md`：此后每个Bug修复在无部署构建/验证、范围提交完成后立即部署，并核对安装产物哈希，无需再次等待单独部署口令。
- 部署前诊断：源码产物`MaidenSuccubus.dll`为`C20653A9…AAB51D`，安装目录仍为2026-09-12 12:09的旧DLL `4B233E26…E6A4D4`；因此本轮测试没有加载提交`9109302`的第四层模态/地图旅行恢复修复，也没有加载提交`dc0d8a6`的统一献祭行动修复。
- 无部署构建：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过，`0 warning / 0 error`；204张卡内容与本地化契约、结构契约、201张可执行卡效果测试、3张`DESIGN_PENDING`跳过项及9个拘束意图场景契约全部通过。首次在受限环境中的NuGet恢复被网络权限拒绝，获准恢复依赖后成功，未把环境错误误判为代码错误。
- 已立即执行`dotnet build -c Debug -p:DeployMod=true -p:ValidateMod=true`并通过，`0 warning / 0 error`。源码与安装目录DLL SHA-256均为`F175BAC231D3751F51381A22C4FDD8A909731BE9E03007920C62F3CA433392B5`，PDB均为`41E1DE01DDFA01D29ADC17F4ADCC1456DE5DFAE83E8086A117742BE3850A7630`；42个资源文件在安装目录和热加载目录中的84份副本全部与源码哈希一致，manifest哈希一致。

## 2026-09-12 — “心眼”与“反射屏障”定稿、“终极耀斑”红光俯视候选

- 变更前快照：`maiden-pre-card-art-acceptance-ultimate-flare-red-20260912` → `b0ab504`；未修改或提交玩家正在编辑的`DesignDoc.md`。
- 玩家确认单眼特写`008_MindsEye_eye_focus_v01.png`作为“心眼”正式卡图；`selection_manifest.json`改为`accepted`并更新`selected/008_MindsEye.png`标准化副本。
- 玩家确认第一批`065_ReflectiveBarrier_反射屏障_v01.png`作为“反射屏障”正式卡图；将其加入V3选择清单并新增`selected/065_ReflectiveBarrier.png`标准化副本。
- 以旧候选`014_UltimateFlare_u01.png`为姿势和服装参考重构“终极耀斑”：镜头改为高位俯视，角色继续悬空并展开双臂，头顶出现单一亮红色高强度耀斑；移除蓝色巨环、机械边框和蛛网状构件，背景改为下方的黑蓝色环形废墟。
- 新候选`014_UltimateFlare_red_overhead_v01.png`已规范化为`1000×760 RGB`，保存原始高分辨率图、完整生成规格与可重复规范化脚本；等待玩家确认，未擅自替换正式`selected/014_UltimateFlare.png`。
- 部署：否；本轮只更新卡图选择记录与候选资产，不修改或部署Mod运行时代码。

## 2026-09-12 — 变身立绘归档与“心眼”单眼构图候选

- 变更前快照：`maiden-pre-transform-archive-mindseye-eye-20260912` → `c69471f`；未修改或提交玩家正在编辑的`DesignDoc.md`。
- 在`图片素材/变身形态/`中分别归档无垢天衣与邪瘴天衣的魔装耐久3/2/1三档透明立绘，并以README记录运行时源文件映射；采用复制而非移动，现有运行时资源路径不变。
- “心眼”放弃完整人物、躲闪和反击构图，改为仅聚焦响木天音的一只青蓝色眼睛；以虹膜内同心折射、单一聚焦光点和深靛蓝感知光线表达预判与洞察，同时保留粉棕刘海、青蓝虹膜和原作细线条作为身份与画风锚点。
- 新候选`008_MindsEye_eye_focus_v01.png`已规范化为`1000×760 RGB`，视觉焦点位于卡框安全区；严格排除第二只眼、完整人物、手臂、武器、文字、UI和卡牌状物。候选等待玩家确认，未擅自更新`selected/`或正式选择清单。
- 保存原始高分辨率生成图、完整生成规格和可重复执行的Lanczos规范化脚本；本轮使用内置高保真图像生成，因为该卡图不涉及色情内容。
- 部署：否；本轮只归档美术素材和新增卡图候选，不修改或部署Mod运行时代码。

## 2026-09-12 — 普通形态脚部去白边与并行卡图试制

- 变更前快照：`maiden-pre-foot-defringe-parallel-card-art-20260912` → `ac496b8`；未修改或提交玩家正在编辑的`DesignDoc.md`。
- 修正普通校服形态棕色便鞋外沿的白色底色污染：只在脚部局部范围识别紧邻透明区的浅色中性边缘像素，并用最近的深色鞋面边缘颜色替换其RGB，保留原透明度、鞋型、脚底坐标和整张立绘锚点。
- 脚部浅色边缘像素由旧素材的`1069`降至`144`；输出继续保持`922×1250 RGBA`、像素包围盒`(408,152)～(632,1163)`，生成脚本重复执行前后SHA-256一致。
- 并行试制“心眼”6张和“极限闪光”6张`1000×760`候选，保存逐图提示词、生成配置、可重复运行脚本、尺寸/哈希验证清单和联系表。人工语义闸门判定两批均未达到正式采用线，因此不更新`selected/`与选择清单；这些图片仅作为下一轮构图和负面提示词样本。
- 为“精液变换”建立单器皿、珠白液体转化为猩红生命精华的6构图配置；本轮因IP-Adapter/CLIP Vision模型载入导致本地ComfyUI接口持续阻塞并最终停止服务而停止排队，未把未完成任务误记为候选产出。
- 部署：否；本轮只修正角色素材并保存卡图试制资料，不修改或部署Mod运行时代码。

## 2026-09-12 — 普通校服形态改用原作白丝连裤袜

- 变更前快照：`maiden-pre-normal-white-tights-20260912` → `a071063`；未修改或提交玩家正在编辑的`DesignDoc.md`。
- 原作装备数据库中“白丝连裤袜”（Armor 307）的`PID:12`对应`actor01_pose01_option_0012.png`；将该原始透明图层保存为`white_tights_option_0012.png`并用于普通校服形态，未误用实际呈深色的`actor01_pose01_stockings01.png`。
- 保留现有`922×1250`校服立绘的脸、身体比例、姿势、棕色便鞋和战斗锚点；原作裤袜层在膝下渐隐，并将同一白色半透明材质延伸到现有全身腿部，消除原作`922×922`画布底边的水平截断。
- 保存替换前的`character_normal_bare_legs.png`并新增可重复执行的`BuildNormalWhiteTightsAsset.py`；已验证脚本重复执行哈希不变、备份与变更前Git对象一致、原作图层哈希一致，输出为`922×1250 RGBA`且脚底坐标不变。像素差异严格限制在`(432,642)～(610,1079)`腿部区域，实际战斗缩放预览无可见拼接线。
- 部署：否；本轮只替换普通形态美术素材，不修改角色形态逻辑或部署Mod。

## 2026-09-12 — 邪瘴天衣三档可用战斗立绘

- 变更前快照：`maiden-pre-corrupt-form-assets-20260912` → `ec320d4`；未修改玩家正在编辑的`DesignDoc.md`。
- 按现有普通形态与无垢天衣立绘的同一规格，复用原作身体、脸部和`邪瘴天衣/破损/严重破损`服装层，生成魔装耐久3/2/1对应的三张`922×1250`透明合成立绘。
- 原作邪瘴天衣服装层仅延伸到膝部；为保持现有战斗落地点和切换时角色位置不跳变，从已验收圣洁形态提取相同腿型与脚底位置，只将材质与配色转换为黑色长袜、紫色装甲和金色折线，并保留独立透明下肢层供后续调整。
- 新增可重复执行的`BuildCorruptTransformationAssets.py`，并将原作`0027/0028/0029`服装层一并保存到运行时素材目录。验证计划：检查尺寸、RGBA透明通道、像素边界、三档差分及与现有角色脚底Y坐标的一致性。
- 部署：否；本轮只准备可直接接线的角色美术素材，不修改变身逻辑或部署Mod。

## 2026-09-12 — 黑暗元素卡图正式确认

- 变更前快照：`maiden-pre-card-art-dark-element-finalize-20260912` → `019147e`；未修改玩家正在编辑的`DesignDoc.md`。
- 玩家确认`049_DarkElement_orb_identity_03.png`作为“黑暗元素”正式卡图；`selection_manifest.json`来源改指该图并将状态设为`accepted`，标准化副本更新为`selected/049_DarkElement.png`。
- 正式图使用原作黑暗元素动画的紫色球体语言表现攻击，以独立月牙屏障表现魔力解放格挡；局部双手通过原作堕落形态护手保留响木天音身份特征，不依赖完整人物展示。
- 验证：正式源文件与`selected/`副本SHA-256一致，均为可读取的`1000×760` PNG；选择清单JSON可解析。
- 部署：否；仅确认第一批卡图资产，不修改或部署Mod运行时代码。

## 2026-09-12 — 黑暗元素手部角色特征修正

- 变更前快照：`maiden-pre-card-art-dark-element-hand-identity-fix-20260912` → `405a94d`；未修改玩家正在编辑的`DesignDoc.md`。
- 根据玩家反馈淘汰仅有通用裸手的`049_DarkElement_orb_original_style_02.png`；该图虽已匹配原作渲染与黑暗元素特效，但无法从局部辨认响木天音。
- 从原作`actor01_pose01_cloth_0026.png`提取不含角色躯干的护手参考：紫红主体、金色折线边、黑色束带、粉色心形饰件与粉色荷叶边；将这些身份锚点加入双手，并保留两侧不对称结构。
- 新候选`049_DarkElement_orb_identity_03.png`继续以黑暗球体为最大主体，不加入脸或全身；原作护手只占画面下部，并保持攻击射流、防护弧面和中央偏上卡框安全区。输出已规范化为严格`1000×760`。
- 部署：否；仅更新单张卡图候选、原作局部参考与审阅记录，不替换正式选择清单或修改Mod运行时代码。

## 2026-09-12 — 黑暗元素原作画风校准候选

- 变更前快照：`maiden-pre-card-art-dark-element-style-fix-20260912` → `dad4e59`；未修改玩家正在编辑的`DesignDoc.md`。
- 根据玩家反馈淘汰`049_DarkElement_orb_hifi_01.png`：虽然卡牌语义正确，但厚重的商业奇幻渲染、哥特护腕和教堂背景与《魔法少女天穹法妮雅》的原作视觉语言不符。
- 从原作`Skills.json`确认“黑暗元素”使用动画176，并从`30FPS_ACQ026_Dark.png`提取原作帧37作为权威特效参考；球体修正为深紫外缘、洋红发光核心、环状空洞颗粒和像素状散射光。
- 以原作响木天音普通立绘约束细线条、肤色和二至三阶赛璐璐上色，以旧候选只约束双手夹持球体的构图；移除复杂环境、写实材质、哥特服装和人物展示，输出`049_DarkElement_orb_original_style_02.png`并规范化为严格`1000×760`。
- 部署：否；仅更新卡图候选与审阅记录，不修改正式选择清单或Mod运行时代码。

## 2026-09-12 — 黑暗元素效果主体重构候选

- 变更前快照：`maiden-pre-card-art-dark-element-orb-redesign-20260912` → `dc0d8a6df08b3a31b2a03c654939b7ff2cbb1d6e`；未修改玩家正在编辑的`DesignDoc.md`。
- 放弃“展示完整角色及原作服装”的旧构图，把卡牌效果设为唯一主体：双手之间悬浮单一黑紫色球体，分别以向外爆发的暗能量和独立的紫色弧形屏障表达基础伤害与“魔力解放”格挡。
- 本地ComfyUI/WAI v14先生成4张纯效果构图诊断图；因模型将球体误解为身体局部、星形法阵或带画框水晶球，全部标为淘汰，不进入正式候选。
- 该牌不含色情内容，按玩家许可改用高保真图像生成取得候选`049_DarkElement_orb_hifi_01.png`，再居中裁切缩放为严格`1000×760`；黑暗球体、两只手、攻击射流与防护弧面均可独立辨识，等待玩家视觉验收。
- 部署：否；仅新增可复现的本地ComfyUI诊断生成器、提示配置与卡图候选，不修改Mod运行时代码或正式选择清单。

## 2026-09-12 — 黑暗元素原作服装重建候选

- 变更前快照：`maiden-pre-card-art-dark-element-outfit-fix-20260912` → `8915732`；用户未提交的`DesignDoc.md`未修改，SHA-256保持`2B3D19DF003DBE0106E19E386780D6A72C739C6861C6E063C409519C719A879E`。
- 核对原作`actor01_pose01_cloth_0026`后确认旧候选使用了错误的完整包覆式紫色连体衣；原作实际为紫红金边颈肩饰、裸露胸部、白色V形腰侧布、黑色心形吊带、白袜粉边、紫红护臂/靴套及巨型紫黑刃。
- 从原作身体、服装和表情透明分层重新合成无白底专用参考；使用本地ComfyUI分别测试IP-Adapter、低重绘img2img、原作人物与纯背景合成、低强度混合整合，共10张扩散候选和3张原作合成候选。
- 当前推荐人工审阅`049_DarkElement_o09.png`：以原作像素级合成图为底，使用0.18 denoise和低LoRA强度统一暗元素法阵光影；原作人物完全不重绘的`049_DarkElement_c01.png`作为服装绝对准确的保底对照。玩家确认前不替换正式选择清单。
- 新增可复现的原作分层合成器、暗元素纯背景ComfyUI生成器、原作人物/背景合成器、专用生成配置及逐候选审阅记录。成人向人物与服装图片全程仅由本地ComfyUI处理，未调用内置图像编辑。
- 部署：否；本轮只生成并审阅单张卡图候选，不修改或部署Mod运行时代码。

## 2026-09-12 — 合并第四层献祭与封印区火堆行动

- 变更前快照：`maiden-pre-unified-sacrifice-rest-site-action-20260912` → `9109302b0e2f7817df7929983f8168863d701943`；早先同一提交上的诊断标签`maiden-pre-fourth-route-sacrifice-dedup-fix-20260912`保留。用户未提交的`DesignDoc.md`、`.review`研究资料和卡图资产未纳入修改。
- 玩家澄清设计：第四层路线“献祭”就是牌组中因路线进入封印区的卡牌所使用的火堆清除行动，不应另有一个“选1张对立路线牌”的并列行动。旧实现分别由`RestSiteSealPatch`和`TwinSoulChalice.TryModifyRestSiteOptions`注入两个不同`OptionId`，因此界面同时显示“解除封印”和“献祭”；这不是同一监听器的重复调用。
- 删除拆分的`RemoveSealedCardsRestSiteOption`与`FourthRouteSacrificeOption`，改成唯一`SacrificeRestSiteOption`；仅在存在封印牌时生成，确认后一次移除全部封印牌，取消则不产生任何变化，并继续使用一次性保留策略确保不占正常火堆行动。
- 路线遗物不再自行添加火堆选项。统一献祭在确认的封印牌中包含当前任务所要求的对立路线牌、且路线遗物正处于生长阶段时调用既有阶段推进服务，使遗物成长为繁荣；不满足该条件时只执行普通封印区献祭。
- 中文火堆标题统一为“献祭”，删除“解除封印”和“选择1张对立路线牌”的旧文案；结构验证与两份手测清单增加“只存在一个行动、批量移除、条件推进”的回归门。
- 验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过，0 warning、0 error；204张卡本地化、内容契约、结构契约、文本格式、卡牌效果测试契约和9场景拘束测试契约全部通过。首次受限构建仅因NuGet签名源网络权限失败，获准访问后恢复并完成。
- 部署：否；等待用户明确要求后再部署。

## 2026-09-12 — 第四层路线地图触发与奖励后旅行修复

- 变更前快照：`maiden-pre-fourth-route-map-modal-fix-20260912` → `230f32cc80a1aaeaa2da4be7cfa3e7507d40bc7e`；用户未提交的`DesignDoc.md`和卡图配置修改保持原样。
- 运行日志在本轮报告时没有包含新的第四层复现记录；根据稳定复现症状和原版UI源码定位到同一根因：`NOverlayStack`在`NMapScreen.Opened`时主动隐藏全部overlay，旧路线界面却在地图打开后压入该栈，因此必须关闭地图才可见；奖励结算时地图已经关闭，又使原有`SetTravelEnabled(true)`恢复分支被跳过。
- 路线任务选择和始源奖励界面改用原版`NMapSelectFtue`在地图上方采用的`NModalContainer`路径；界面直接实现`IScreenContext`，进入/退出树时登记和移除阻断输入，关闭时只清理自己占用的modal，并在外部清理时完成等待任务，避免悬挂异步流程。
- 保留地图稳定门：等待开幕动画、原版地图教程及已有modal结束后再显示路线界面；显示期间暂停地图旅行，奖励/任务状态结算后重新调用原版`SetTravelEnabled(true)`刷新可达节点，并增加低频日志记录modal打开与旅行恢复结果。
- 结构验证新增第四层地图modal、输入清理、禁止重新使用地图隐藏overlay及旅行开关成对出现的回归门；MVP与第一轮手测清单新增“地图不关闭即显示、领奖后节点立即可选”的精确验收项。
- 验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过，0 warning、0 error；204张卡本地化、内容契约、结构契约、文本格式、卡牌效果测试契约和9场景拘束测试契约全部通过。
- 部署：否；等待用户明确要求后再部署并进行第四层地图实测。

## 2026-09-12 — 燃烧手环右臂与肤色修复

- 变更前快照：`maiden-pre-card-art-burning-bracelet-fix-20260912` → `230f32cc80a1aaeaa2da4be7cfa3e7507d40bc7e`；用户未提交的`DesignDoc.md`未修改，SHA-256保持`2B3D19DF003DBE0106E19E386780D6A72C739C6861C6E063C409519C719A879E`。
- 针对玩家指出的“手臂变形、变色”，先验证WAI局部遮罩重绘；两张结果均产生肉质纹理块，明确淘汰，不进入候选集。
- 改用高保真精确对象编辑，仅重建前伸右臂、腕部、手环、手掌及接缝：右臂恢复连续正常肤色，肘腕比例合理，五指完整，火焰从手环外侧产生且不再侵入皮肤。
- 输出以居中Lanczos裁切规范化为`1000×760`，保存为`targeted/002_BurningBracelet_t03.png`；`selection_manifest.json`和本地`selected/`候选已指向修复版。完整输入、方法和提示词记录在`precision_edit_manifest.json`。
- 部署：否；本轮只调整单张卡图，不修改或部署Mod运行时代码。

## 2026-09-12 — V3final逐卡精修与局部语义修正

- 变更前快照：`maiden-pre-card-art-v3-refinement-20260912` → `d87fca2ce8b573727afd3be6af8b2b57aa4028b9`；用户未提交的`DesignDoc.md`未修改，SHA-256保持`2B3D19DF003DBE0106E19E386780D6A72C739C6861C6E063C409519C719A879E`。
- 从每张代表卡的V3final人工选择一张基础图，以真实img2img链路生成两档精修，共6张卡、12张候选；统一`1000×760`，使用WAI v14、Celesphonia LoRA、逐卡提示与FaceDetailer，不再使用会复制平面立绘内容的IP-Adapter。
- 建立三列精修联系表与机器可读提示词目录。燃烧手环`r01`、黑暗元素`r01`稳定提升了材质、轮廓和语义；心眼`r01`闭眼感知更明确；终极耀斑和精液变换只取得局部质量提升。
- 新增局部遮罩重绘工作流并完成4张牌的诊断试验。光子伏特`t01`成功补出前臂发射器、短光刃和命中星爆；高重绘幅度会生成第二人物，而心眼/终极耀斑/精液变换的大面积遮罩会生成巨型物体、重复人物或重复器皿，因此失败结果不进入候选集。
- 建立`selection_manifest.json`与本地`selected/`候选目录：3张标记推荐（燃烧手环、光子伏特、黑暗元素），3张标记暂定（心眼、终极耀斑、精液变换），等待玩家逐张视觉验收。生成物与遮罩均由`.gitignore`排除。
- 新增可复现脚本：`RefineCardArtV3Final.ps1`、`InpaintCardArtV3Targets.ps1`、精修联系表、遮罩和候选清单生成器。局部遮罩结论是普通WAI底模只适合小面积构件增补；大面积无痕擦除应换专用SDXL inpaint工作流或重做初始构图。
- 部署：否；本轮仅调整美术试制、选择清单与生成工作流，不修改或部署Mod运行时代码。

## 2026-09-12 — 第一批卡图V3视觉闸门试制与V2淘汰

- 变更前快照：`maiden-pre-card-art-v3-pilot-20260912` → `31b1a550e083d67b838ad43f1c75adbcdfd8cfeb`；用户未提交的`DesignDoc.md`未修改，写入前SHA-256保持`2B3D19DF003DBE0106E19E386780D6A72C739C6861C6E063C409519C719A879E`。
- 人工复盘已中断的V2输出：实际生成图链仅13个有效节点，姿态参考字段未接线，所有路线反复使用平面立绘IP-Adapter与通用十模板；正式生成到251项后停止。V2标记为淘汰并仅保留失败样本，不再作为批量基线。
- 新建V3视觉闸门：选择燃烧手环、心眼、终极耀斑、光子伏特、黑暗元素、精液变换6张代表卡，每张A/B/C三个候选；统一`1000×760`，先文生图，再按已确认“反射屏障”的30步、CFG 5.5、denoise 0.3低强度精修，最后运行Advanced_V31已有的FaceDetailer。
- 通过Civitai模型版本元数据核对`celesphonia-1.8`的真实训练标签，分别建立普通形态与圣洁形态提示词；堕落形态暂无专用训练标签，保留独立文本与原作差分试验。卡牌核心动作使用逐卡加权提示，不再复用通用镜头模板。
- 生成并人工审阅两轮共36张试制图。最新18张全部可读且严格为`1000×760`；节点执行0失败。A候选总体显著优于平面立绘IP-Adapter候选；燃烧手环v01/v02、黑暗元素v01和精液变换v01形成可继续精修的方向。
- 视觉闸门未通过：光子伏特三候选均缺乏明确光子刃攻击；心眼与终极耀斑仍有语义偏移；B/C出现说明页、多人、飞机和静态背影。禁止据此恢复780张批量，后续须改为语义优先构图，并只将逐卡筛选的完整原作CG用于场景/情绪参考。
- 新增可复现脚本：`GenerateCardArtV3Pilot.ps1`、V3联系表生成器、原作3315张图片审计器和52组事件CG联系表生成器；生成图片及审计大文件保留在本地并由试制目录`.gitignore`排除。
- 部署：否；本轮只处理美术试制、诊断与资产工作流，不修改或部署Mod运行时代码。

## 2026-09-12 — 拘束意图九场景游戏内验收通过

- 记录前快照：`maiden-pre-control-intent-runtime-result-20260912` → `56e6fb44e395e580f108100ff18a073ce061ec7b`；本轮只审阅玩家完成的运行时报告和日志，不修改游戏实现、不重新部署。
- `control-intent-test-results/latest.json`记录测试于12:15:25开始、12:15:33完整结束，9个场景全部通过、0失败，合计89项精确数值/状态断言全部通过，场景累计执行时间7507ms；报告`Success=true`，不是卡死或中断。
- 通过范围包括：意图稳定ID及显示值、精确格挡、8欲望绕过、60张牌压力投影、攻击/技能/能力矩阵、随身豁免、同一原卡实例与升级/附魔/苦难状态保留、0/1/2/3费真实支付和挣脱值、多拘束源优先级与无溢出、来源死亡后的重绑定/解除，以及权威目录意图到恢复意图的完整执行链。
- 对应`godot.log`测试区间共151行，0条错误、异常、栈溢出或致命记录；9条警告均来自CommunityStats测试夹具缺少统计上下文（7条）及原版Byrdonis测试资源未预缓存（2条），未影响断言和测试完成。
- 边界：本套件验证了投影期间原卡序列化字段，但没有执行“保存退出并重新载入战斗”的真实存读档流程；启动阶段BaseLib仍报告`EroticIntentRuntimePower`等类型不支持其`SavedProperty`，该存读档风险须另行实测，不能由本次9/9结果视为通过。

## 2026-09-12 — 拘束意图测试套件部署

- 部署前快照：`maiden-pre-control-intent-test-deploy-20260912` → `a8a74c646096fe6088cdfc42d5a58ab4ad2e1582`；该HEAD包含拘束意图九场景测试提交`fcba973b954d5ef95fe30956aca8f82854580659`及其后的首批卡图V2生成流程提交，用户未提交的`DesignDoc.md`、`.review`资料和素材保持原样。
- 显式执行`dotnet build -c Debug -p:DeployMod=true -p:ValidateMod=true`；内容契约、结构契约、204张卡本地化、文本格式、204张卡效果测试契约（201张可执行、3张`DESIGN_PENDING`）及9场景拘束意图测试契约全部通过，0 warning、0 error。
- 已部署至`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`，并同步33个运行资源至安装目录与`D:\game_backup\steam\steamapps\common\Slay the Spire 2\MaidenSuccubus`热加载目录；两处资源均为0缺失、0哈希不一致。
- 源码产物与安装目录SHA-256一致：DLL `4B233E26ECFC5B0CD9A3F94490325DD7A9E89260C51F4110B615E1BBBAE6A4D4`，PDB `8E482014C34AF1BBB2A14CB4AFAEDCC9E5C2A747E9F75448B2CAA8293EA17D3D`，manifest `B30F2B6A2999AA5DBD7ACD3649A1B51666AA19B38221C42A38A4A06EB84542A3`。
- 游戏内验证：尚待玩家在一次性单人战斗中按`Shift+F10`（或控制台执行`ms_test_control confirm`）运行破坏性的9场景测试；结果写入已加载模组程序集旁的`control-intent-test-results/latest.json`，测试后应放弃该局。

## 2026-09-11 — 第一批卡图V2多参考图与十变体批处理

- 变更前快照：分支`codex/maiden-controlled-merge-v2`，提交`59c68bbe32a03b432eafb4a6ddca8c9a20d8c071`；用户未提交的`DesignDoc.md`保持原样，写入前SHA-256为`2B3D19DF003DBE0106E19E386780D6A72C739C6861C6E063C409519C719A879E`。
- 根据第一批78张原作命名卡牌建立V2生成流程：每张10个不同镜头、动作、表情、光照和原作参考图变体，共780项，统一输出PNG `1000×760`。
- 从原作身体、服装和表情图层合成中立、圣洁、堕落三组路线参考图；堕落路线固定参考原作紫黑／品红魅魔装，并使用单参考图IP-Adapter约束，避免双参考图引发重复人物。
- 清理正向提示词中所有可能生成实体卡牌的`card/deck/hand of cards`语义，将对应画面改写为符文、术式光印、记忆碎片和旋涡；负向提示词同步禁止卡牌状纸片、边框、文字和UI。
- 统一提升精细线稿、人物解剖、手部、服装材质、分层赛璐璐、电影光照、环境景深和前后景完成度；镜头覆盖低机位、高机位、俯视、越肩、后背三分之四、侧向跟拍、荷兰角、虫视、横向广角和斜上后视。
- 验证：生成目录含78张卡、每张10个变体、780个唯一提示词；正向提示词禁词0项，缺失参考图0项，宽高配置严格为`1000×760`。圣洁“光子伏特”和堕落“黑暗元素”路线样张已核对通过，完整批次以断点续跑模式开始生成。
- 部署：否；本轮只生成设计美术资产和工作流，不改动游戏实现或已安装Mod。

## 2026-09-11 — 拘束意图九场景运行时测试套件

- 变更前快照：`maiden-pre-control-intent-test-harness-20260911` → `59c68bbe32a03b432eafb4a6ddca8c9a20d8c071`。
- 需求边界：只测试并提高`SYS-CTL-001/002`与`SYS-DES-002B`拘束链的验证效率；用户提出的初始卡路线“变奏（原升变）”留待下一版DesignDoc，本轮没有修改卡牌语义。
- 新增Debug专用`Shift+F10`与`ms_test_control confirm`入口；沿用玩家启动游戏并手动进入一次性单人战斗的边界，不自动导航或启动战斗。
- 九个场景通过真实怪物`PerformMove`、真实Power/牌堆命令、卡牌`SpendResources`和完整出牌包装器核对精确格挡、8欲望绕过、三种卡牌类型、随身豁免、原实例及升级/关键字/附魔/苦难保留、0/1/2费挣脱、多来源优先级与无溢出转移、来源死亡、权威目录意图及恢复意图。
- 压力场景在三个战斗牌堆生成60张以上夹具并反复读取投影属性；真实行动、投影读取、出牌和死亡链前后记录检查点。每场景有30秒超时，完整预期/实际值和异常写入`control-intent-test-results/latest.json`。
- 新增`ValidateControlIntentTests.ps1`并纳入`ValidateMod=true`，固定九场景、真实执行/支付/死亡/目录/报告路径及破坏性确认门。
- 验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过，0 warning、0 error；204张卡文本审计、内容契约、结构契约、本地化格式、卡牌效果测试契约及新增9场景拘束测试契约全部通过。游戏内数值结果仍须在部署后运行`Shift+F10`取得，静态门不冒充运行时通过。
- 部署：否；需用户明确要求后才部署，游戏内9场景结果尚待运行。

## 2026-09-11 — 第一批原作命名卡图与1000×760工作流

- 变更前快照：分支`codex/maiden-controlled-merge-v2`，提交`b762924a36baadd8e533a177ab44d7ce495d5a21`；用户未提交的`DesignDoc.md`保持原样，写入前SHA-256为`2B3D19DF003DBE0106E19E386780D6A72C739C6861C6E063C409519C719A879E`。
- 通过《魔法少女天穹法妮雅》本地`Skills/Items/Weapons/Armors/States`数据库精确匹配当前DesignDoc正式卡名，建立78张第一批清单：中立21、圣洁27、堕落30；其余卡牌明确留待第二批。
- ComfyUI用户工作流`Advanced_V31.json`的实际宽高控制节点已改为`1000×760`，潜空间节点同步为`1000×760×1`；修改前副本保存为`Advanced_V31_before_1000x760.json`。
- 使用`waiNSFWIllustrious_v140.safetensors`与`celesphonia-1.8.safetensors`生成78张`v01`卡图，按`序号_实现类_中文名_版本`一一命名并存入`图片素材/第一批卡图/`；视觉焦点按卡框遮挡规则约束在中央偏上安全区。
- 新增机器可读`first_batch_manifest.json`、人工审阅清单、三张路线总览和可重复执行的`GenerateFirstBatchCardArt.ps1`；`065 反射屏障`采用此前已确认定稿候选的1000×760裁切版，纯文生图初稿另行保留。
- 验证：78/78清单文件存在，0缺失、0多余、0重复索引、0重复实现类、0不可读图片，全部尺寸严格为`1000×760`；生成脚本PowerShell语法0错误。视觉抽查已在清单中标记9张优先复查候选。
- 部署：否；本轮只生成设计美术资产、清单和工作流，不改动游戏实现或已安装Mod。

## 2026-09-10 — 挣脱迁移为卡牌实例 capability

- 变更前快照：`maiden-pre-escape-capability-refactor-20260910` → `30cc2a4`。
- 参考原版女王的`ChainsOfBindingPower + Bound`实现，将“拘束状态附着到具体卡牌实例、由Power统一协调生命周期”作为本轮结构基准；没有复用原版`Bound`的“一回合只能打出一张”语义。
- 删除`ControlQuery`的`ConditionalWeakTable`投影旁表。每张受拘束的实际战斗卡现在附着已注册、可克隆及可存档的`EscapeProjectionCapability`；Power数值或牌堆变化只负责协调 capability 的附着、换源和移除，getter 查询不再重建投影。
- 目标改为无目标、打出后进入弃牌堆、锁链覆盖层、原牌预览/拘束Power悬停和原效果抑制均由RitsuLib capability接口承担；`ControlPower`不再充当全局`ICardOnPlayHookListener`。
- 删除全局`NCard.UpdateVisuals`挣脱补丁，覆盖层改走RitsuLib原生 capability overlay 容器。因RitsuLib 0.4.64不能整段替换描述，也无法在替换标题时隐藏原升级后缀，标题、描述、关键字及原附魔/苦难隐藏仍保留精确getter补丁；这些补丁只查询已附着 capability，不搜索Power或刷新卡牌。
- 为序列化、附魔/苦难变更和降级建立短生命周期“读取原状态”作用域，确保投影期间的保存及状态命令仍看到原关键字、附魔和苦难，解除拘束后恢复同一张卡的原状态。
- `MagicResonance`运行时回归新增：连续getter读取、原效果抑制、精确挣脱值、弃牌结算、序列化保留附魔，以及解除后关键字/附魔/苦难恢复。
- 完整无部署构建与全部验证门通过（0 warning、0 error）；尚未部署，等待本轮提交后的游戏内拘束意图复测。

## 2026-09-10 — 拘束投影重入与重复刷新修复

- 变更前快照：`maiden-pre-control-projection-reentrancy-fix-20260910` → `b110101`。
- 复测仍在花园幽灵鳗执行`MAIDENSUCCUBUS_CONTROL`时退出；新一轮`godot.log`不再出现运行时`OnPlay`补丁安装，但Windows错误报告明确记录`coreclr.dll`异常码`0xc00000fd`，说明旧动态Harmony路径已删除后仍存在独立的托管栈溢出。
- 投影查询的重入门现在覆盖“校验既有投影”和“重建投影”的完整过程；任何卡面getter或其他模组补丁在校验期间再次查询同一投影时都会读取未投影结果，不再递归进入`IsStillValid`。
- 卡面批量刷新增加同线程重入门；拘束施加、挣脱和直接解除不再从命令层重复刷新，初次施加也不再同时走`AfterApplied`与全局`AfterPowerAmountChanged`两条刷新路径。刷新权威入口收敛为Power数值变化和移除生命周期。
- 拘束Power数值变化时分别记录投影刷新开始、投影表完成和卡面刷新完成三个低频诊断点；若仍发生不可捕获的栈溢出，最新日志可直接区分模型投影与Godot卡面刷新阶段。
- `MagicResonance`回归场景新增连续32轮读取投影标题、描述、关键字和目标的检查；结构门固定完整投影重入保护、卡面刷新重入保护及单一刷新所有权。
- 无部署构建和显式部署构建均通过全部验证门（0 warning、0 error）；已部署至`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`。源码与安装目录DLL SHA-256均为`81B7D796A36A291B43AD64BC342EA05F210E48390D8BEFDD09A9E70E14170954`，PDB均为`BE4DC78E1E6BB8B0C9D2AC38759A030CB645BAEE47DEEB08A78F4263454ABE73`；修复后运行时结果待人工复测。

## 2026-09-09 — 拘束意图栈溢出修复

- 变更前快照：`maiden-pre-control-stack-overflow-fix-20260909` → `60e4794`。
- 人工测试在花园幽灵鳗执行`MAIDENSUCCUBUS_CONTROL`时闪退；`godot.log`最后一条模组记录是为`MagicResonance.OnPlay`安装挣脱投影补丁，Windows错误报告于20:31:26记录`0xc00000fd`/`StackHash_2264`，确认是栈溢出。
- 根因是结构重构后的按需效果补丁仍在`ControlPower.AfterApplied`同步刷新过程中调用`Harmony.Patch`，即在战斗运行中动态重写首次遇到的具体卡牌`OnPlay`方法。
- 删除`EscapeEffectPatcher`及所有运行时具体卡牌方法补丁；`ControlPower`改为实现RitsuLib 0.4.64的`ICardOnPlayHookListener`，在统一`CardModel.OnPlayWrapper`内仅对精确登记的投影实例抑制原始`OnPlay`，保留费用、原卡实例、打出与结算生命周期。
- `MagicResonance`数值测试新增能力牌拘束场景，核对投影成功、原效果不生效、1费减少1点挣脱值并进入弃牌堆；结构验证禁止重新引入动态`Harmony.Patch`，手测清单增加三类拘束执行时不得卡死或闪退。
- 无部署构建和显式部署构建均通过全部验证门（0 warning、0 error）；已部署至`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`。源码与安装目录DLL SHA-256均为`8CA155E2724270AE711FCC64342E3BE8E3812847C9F591234C06D93611BD0B84`，PDB均为`313E576D8817B2EFB7C45EA316E7335998335EAD77FEA1EE0A4C71E676E260F1`；修复后运行时复测待完成。

## 2026-09-09 — 204张卡牌第三轮实测修复

- 变更前运行时快照：`maiden-card-test-third-run-20260909` → `bb95c33`；第三轮 F10 报告于20:04:21开始、20:11:53正常结束，结果为200张通过、1张失败、3张`DESIGN_PENDING`，不属于卡死或中断。
- 唯一失败项战技复读已完成生命周期定位：当前游戏在复制牌仍位于`PileType.Play`时触发`AfterCardPlayed`；旧实现立即Transform后，恢复牌滞留打出区，而原版结算器随后只尝试移动已经被移除的旧实例。
- 战技复读Power改为在`AfterCardPlayed`登记待恢复实例，并在原版把复制牌从打出区移入弃牌堆或消耗牌堆、触发`AfterCardChangedPiles`后原地恢复；升级状态和永久牌组来源继续保留。
- 测试新增恢复牌类型、升级状态及打出区清空断言；构建门新增战技复读必须在离开`PileType.Play`后恢复的生命周期检查。
- 无部署构建及全部验证门通过（0 warning、0 error）；部署因测试后的游戏进程仍锁定已安装DLL而未写入，第四轮游戏内结果待完成。

## 2026-09-08 — 204张卡牌第二轮实测修复

- 变更前运行时快照：`maiden-card-test-second-run-20260908` → `05c1f9c`；第二轮 F10 报告于16:03:55开始、16:11:33正常结束，结果由首轮150张通过提升至197张通过、4张失败、3张`DESIGN_PENDING`，不属于卡死或中断。
- 按最新游戏日志和当前v0.111.0游戏DLL复核：原版`CreatureCmd.Stun`仅在当前意图允许转移时调用`SetMoveImmediate`，遇到尚未执行的本模组临时色情意图会静默不生效；本模组原强制眩晕又使用非原版识别的状态ID，导致`Creature.IsStunned`为假。
- 新增统一眩晕桥接：正常情况保留原版眩晕流程；若临时色情意图阻止切换，则强制替换；强制状态使用引擎识别的稳定ID `STUNNED`。剑之裁决、妨碍射击、欲望鞭挞和咬统一改走该路径。
- 测试场景复位会重建每个怪物的状态机，确保前一张牌留下的临时意图不污染后一张牌；战技复读改为执行真实`BeforeCombatStart`并验证Power、牌面变形、保留和打出后恢复；诱敌深入临时加入并清理具备拘束目录项的原版多尼斯异鸟，不再错误选择无拘束意图的小啃兽。
- 补足此前遗漏的欲望鞭挞条件测试：建立真实拘束意图后同时核对7点伤害和击晕，而非只验证伤害。构建门新增`STUNNED`状态ID、临时意图回退、卡牌统一眩晕入口及欲望鞭挞条件断言检查。
- 静态构建验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过，0 warning、0 error；内容契约、结构契约、204张卡文本审计、本地化格式契约和卡牌测试结构门全部通过。
- 游戏内结果：等待部署后的第三轮完整 F10 数值测试；当前不把静态构建通过记作201张实际效果全部通过。
- 部署：`dotnet build -c Debug -p:DeployMod=true -p:ValidateMod=true`通过，0 warning、0 error；已部署至`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`。源码与安装目录DLL SHA-256均为`F146D0E722D76115C1D391BAFDBF74A976E8EC8D3CF984946DE7C263968C74BC`，PDB均为`699C287A52C373FC924DD4F91087B0C425AF164812CC71824D23327A2ECAF80C`。

## 2026-09-08 — 204张卡牌首轮实测修复

- 变更前运行时快照：`maiden-card-test-first-run-20260908` → `409eeeb`；首轮 F10 报告正常完成，结果为150张通过、51张失败、3张`DESIGN_PENDING`，手牌区大量卡牌是测试生成节点残留而非卡死。
- 补齐17张侵犯诅咒的已确认效果：打出后从战斗和对应永久牌组实例移除，并分别结算欲望、负面状态、生命、魔装耐久、下回合能量、随机状态及衍生牌生成。
- 补齐6张已确认事件诅咒的效果：三档淫纹回合末生成发情，痴情限制攻击牌，口球增加技能耗能，衣装透明通过现有精确关键词getter投影使其他手牌临时获得虚无；`HypnosisCurse`和`ClimaxBanCurse`仍按设计待定跳过。
- 修复真实实现偏差：忏悔斩读取正确的`FrailPower`变量；情人匕首按目标实际眩晕状态加倍；沉溺快感升级不再擅自增加3点格挡；献祭狂热升级基础伤害由错误的18修正为DesignDoc基线16。
- 修正测试误判：动态费用/出牌限制先把被测牌放入手牌；变形场景创建真实`NCard`；圣言共鸣触发回合末圣言结算；研究计划走原版起手抽牌Hook；魔装恢复效果先建立天衣形态；精液转化执行真实资源支付；万咒镰刀建立并核对永久牌组实例；诱入深渊核对怪物下一行动的拘束意图而非立即施加Power；伤害测试使用不改变伤害倍率的同层Power夹具。
- 测试复位现在对存在视觉节点的牌走原版可视移除路径，再无视觉移除其他夹具，防止完整测试后手牌区残留大量卡牌。
- 构建验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过，0 warning、0 error；内容契约、结构契约、204张卡文本审计、本地化格式契约和卡牌测试结构门全部通过。
- 游戏内结果：等待部署后的第二轮完整 F10 数值测试；尚不将静态构建通过视为201张实际效果全部通过。

## 2026-09-08 — 卡牌效果测试人工进战斗/F10触发整改

- 变更前快照：`maiden-pre-card-effect-runtime-test-20260908` → `f1ef1c3152c5019a663b42688b44623a874af5fa`。
- 按工作区 `sts2_contrib_tests` 的成熟运行边界调整：不再要求自动化负责启动游戏或导航进战斗；玩家使用响木天音进入可放弃的单人战斗后按 `F10`，即在 Godot 主线程启动全部204张牌的精确效果测试。
- `F10`入口检查战斗状态、单人模式和角色身份，并阻止重复并发启动；原`ms_test_cards confirm [all|CardTypeName]`入口继续用于单牌复测。
- 修复当前游戏v0.111.0中Debug专用`PlayerCmd.EndTurn`拦截器的Harmony签名：该方法返回`void`，前缀不再声明错误的`Task __result`，避免测试补丁安装失败。
- 结构验证新增人工进战斗/F10触发门，确保热键入口及其战斗/角色保护不会被意外移除。
- 构建验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过，0 warning、0 error；内容契约、结构契约、204张卡文本审计、本地化格式和卡牌测试结构门全部通过。受限沙箱首次还原因NuGet网络访问失败，按用户约束允许`dotnet build`访问包源后成功还原并完成验证。
- 部署：`dotnet build -c Debug -p:DeployMod=true -p:ValidateMod=true`通过，0 warning、0 error；已部署至`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`。源码与安装目录的DLL SHA-256均为`23B99E680E2F2FC4A8D43B2E3B9F396835ACAD60BC235B152AADCE0C6B1533C6`，PDB均为`211DD55A7125E2C29AAC5A05D6AF21968D4318C5B6FD790C2FF2F09693ECCE09`。
- 游戏内结果：等待玩家启动游戏、进入响木天音单人战斗并按`F10`后生成实际数值报告。

## 2026-09-08 — 204张卡牌游戏内数值效果测试框架

- 变更前快照：`maiden-pre-card-effect-tests-20260907` → `34c864d4ed413a0fb377b8dbec263a50f071b43e`。
- 新增Debug专用游戏内命令`ms_test_cards confirm [all|CardTypeName]`；命令要求响木天音单人战斗和显式破坏性确认令牌。
- 精确登记内容契约中的204张牌；201张具有真实结算断言，`ClimaxBanCurse`、`HypnosisCurse`以及用户明确要求暂时跳过的`DreamMist`记为`DESIGN_PENDING`，不会计为通过。
- 测试预期固定为2026-08-24需求基线，不从实现侧`DynamicVars`反推；基础版、升级版及关键条件分支分别执行。
- 断言覆盖伤害、格挡、抽牌、能量、欲望、堕落值、Power层数、牌堆迁移、升级、关键字、附魔、怪物意图，以及能力牌的后续抽牌、回合开始/结束、消耗、圣言触发、断罪审判、变身和欲望资源钩子。
- `Rest`仅在该测试场景的作用域内拦截并记录`PlayerCmd.EndTurn`，避免自动测试推进当前战斗；其他效果仍由真实游戏命令结算。
- JSON报告写入已加载程序集旁的`card-effect-test-results/`，保留每条断言的预期值、实际值和运行时异常。
- 新增`ValidateCardEffectTests.ps1`并纳入`ValidateMod=true`：校验204张精确登记、201张可执行、3张明确待设计、基础/升级覆盖、非零效果断言和禁止方法存在性占位测试。
- 验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过；0 error；内容契约、结构契约、204张卡文本审计、本地化格式契约和卡牌测试结构契约均通过。由于受限环境无法访问NuGet漏洞元数据端点，产生2个`NU1900`警告；依赖均从本地缓存成功还原，未影响编译或验证。
- 部署：否；游戏内204张完整运行结果需由用户在专用测试跑局执行命令后查看JSON报告。

## 2026-09-07 — 中文描述与悬停格式版本部署

- 部署前快照：`53b5f190eb1e3cf86c4dec1aeafa7118266132e0`（`refactor(maiden): standardize localization hover formatting`）。
- 执行：`dotnet build -c Debug -p:DeployMod=true -p:ValidateMod=true`。
- 验证：构建0 warning、0 error；204张卡文本审计0 failure；内容契约、结构契约和本地化格式契约全部通过。
- 产物校验：DLL、PDB和manifest与源码构建产物SHA-256一致；33个资源文件在安装目录和Godot热加载目录均为0缺失、0哈希差异。
- 安装目录：`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`。
- 热加载目录：`D:\game_backup\steam\steamapps\common\Slay the Spire 2\MaidenSuccubus`。
- 部署：是；等待用户进行游戏内手动测试。

## 2026-09-07 — 中文描述与悬停格式整改

- 变更前快照：`maiden-pre-localization-format-20260907` → `0bf43eb7c42e9d0c37f61ab6ee6f0bc961b07e47`。
- 需求边界：仅统一卡牌、关键字、Power、附魔与静态悬停的格式和悬停数据，不同步2026-09-02尚未确认的卡牌语义变化。
- 以原版卡面组装、悬停聚合、Power静态/动态描述机制为主基准，以HornetMod 1.2.15的RitsuLib实现为补充，新增`docs/LOCALIZATION_STYLE_GUIDE.md`。
- 统一中文机械术语的金色标注、独立效果换行和数字/量词间距；保留SmartFormat变量、升级分支和既有效果顺序。
- 修复Power canonical悬停中的裸`{Amount}`，补齐遗留Power标题，并让“异常适应”的剩余触发次数通过DynamicVar进入实例悬停。
- 为“拘束”描述显式注入当前剩余挣脱值，避免状态悬停显示裸模板。
- 新增本地化格式验证门，并纳入`ValidateMod=true`构建流程。
- 将“每轮写操作必须具备变更前快照、changelog、范围化Git提交和验证结果”写入本目录`AGENTS.md`。
- 验证：`dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=true`通过；0 warning、0 error；204张卡文本审计0 failure；内容契约、结构契约和本地化格式契约全部通过。另以变更前提交逐键剥离富文本与换行对比，卡牌正文、附魔正文及既有Power动态描述均为0处语义差异。
- 部署：否。
