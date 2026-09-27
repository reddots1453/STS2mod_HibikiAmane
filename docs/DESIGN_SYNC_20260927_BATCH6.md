# DS27第六批：三路线抽牌与剩余数值

前置：`83adb19b`。DesignDoc逐行及词级差异为空。`DS27-02E`为IMPLEMENTED，所有运行时场景尚未实机执行；不标VERIFIED，未部署。

## 逐牌实现

| 模型／名称 | 当前设计落实 |
|---|---|
| DreamPigment／梦色的颜料 | 新增独立中立罕见技能，1/0费；从抽牌堆按堕落、圣洁、中立各抽首个匹配牌；沿用当前实际路线（含变奏），不是注册池。缺某路线不补其他牌、不洗弃牌堆；禁抽/满手停止；写抽牌历史，触发抽牌Hook及Drawn通知 |
| DarkThrust／黑暗突刺 | 普通，伤害9/12，固定抽2；原1能量+1欲望费用保持 |
| MiasmaAbsorption／瘴气吸收 | 普通；0能量+2欲望、获得2/3能量保持 |
| PleasureDrowning／沉溺快感 | 普通，格挡7/10；升级改格挡而非抽牌，固定抽2并加入2张既有状态牌 |
| LastStand／背水一战 | 普通；基础3/4+每负面层3/4。卡面先显示基础伤害，再在战斗中追加实时合计，避免把已计算总伤害写成基础后再描述加成；战斗外不显示合计行 |
| ReflectiveBarrier／反射屏障 | 固定8格挡；消耗时8格挡+1增幅；升级获得虚无，不再错误获得消耗；正常打出进入弃牌堆 |
| ThousandCurseScythe／千咒之大镰 | 基础8伤害，每次消耗永久成长4/6；保留本局牌组版本同步和保存字段 |
| MiasmaFlame／瘴炎 | 伤害7/10、固定3燃烧；描述的旧硬编码2改为BurningPower变量 |
| FinalSlash／终焉的斩击 | 伤害9/11；选择抽牌堆1/2张放弃牌堆保持 |

新增牌实现入口`src/Cards/DreamPigment.cs`；其他在既有Mvp卡牌文件中修改。新牌没有同名正式素材，沿用CardArtAssets的默认卡图降级，没有借用/改写魔力共鸣图片或旧模型身份。魔力共鸣及其他退役卡的安全退出获取池仍是独立待做任务，不能把保留当前注册当成退役已完成。

## 自动测试

- `DesignSyncCardBatchSixContract.cs`：独立9牌基础/升级名称、类型、等级、目标、费用、卡池、动态变量及消耗/虚无期望，不从被测数值反推期望。
- `CardEffectTestCatalog`：18基础/升级场景；梦色的颜料每种升级状态测试正常三抽、缺圣洁、手牌9张、书库禁抽、空抽牌堆、堕落±3变奏归属；每轮验证单路线只抽1、非本Mod/衍生池不冒充中立、不洗弃牌堆及Drawn通知。背水一战验证局外模板、零/三层负面实时预览和实际伤害；反射屏障检查真实打出/弃牌/消耗收益；大镰重复消耗同步战斗版/牌组版与复制数值。
- `ms_test_cards confirm ds27-batch6`只选择这9牌；仍是破坏性、一次性测试战斗入口，需明确confirm。测试会清理当前战斗牌堆/遗物、改生命/状态，不用于保留正常进度的对局。
- `TestDesignSyncCardBatchSix20260927.py`：6项静态测试，包含9条当前DesignDoc原文（只去空白，不去标点）、9份精确本地化模板、源码独立元数据、升级轴和测试入口。
- 注册契约及全量审计测试更新225模型，未放松原有精确身份集合检查。

## 实际执行结果

命令在Mod目录执行：

| 命令 | 结果 |
|---|---|
| `dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false -p:ValidateMod=false` | 0警告0错误 |
| `python scripts/TestDesignSyncCardBatchSix20260927.py` | 6项通过 |
| `python scripts/TestCardLocalizationAudit.py` | 19项通过 |
| `python scripts/TestDesignSync20260927.py` | 14项通过 |
| `python scripts/TestDesignSyncNeutral20260927.py` | 8项通过 |
| `python scripts/TestDesignSyncHoly20260927.py` | 6项通过 |
| `./scripts/ValidateCardEffectTests.ps1 -ProjectDir .` | 225精确注册、223可执行/2设计待定；不是运行时结果 |
| `./scripts/ValidateMvpContent.ps1 -ProjectDir .` | 中立49、堕落63、圣洁60、衍生36、专用诅咒17；24遗物/9附魔，通过 |
| `./scripts/ValidateLocalizationStyle.ps1 -ProjectDir .` | 通过；使用项目formatter规范化新增模板并审阅仅本批差异 |
| `./scripts/ValidateStructuralContracts.ps1 -ProjectDir .` | 通过 |
| `./scripts/ValidateVisualAssets.ps1 -ProjectDir .` | 既有失败：317行诱惑度口红/独立数字断言；未修改该UI |
| `python scripts/AuditCardLocalization.py --output .review/design_sync_20260927_batch6_audit.json` | 返回1，225模型，4差异/4未知/217文本待独立认证/2设计未匹配 |
| 范围内`git diff --check` | 通过 |

53项静态检查通过不等于全部卡牌效果通过。未启动游戏、未部署、未执行上述18场景；所有显示/动画、真实抽牌钩子、多人与存读档仍需运行时验证。

## 剩余范围

全量审计仍报GagCurse费用、LightWings等级及完整多重附魔、MagicResonance/SemenAppetite退役映射；反向覆盖剩余乳汁及明确未完成的欲望爆发草稿，不能自动把草稿转为实现。黑暗风暴必须完整处理“升级时附魔”而非仅调数值；黑色旋涡、暗焰壁障、超再生、恶魔法杖等剩余机制继续按总审阅清单推进。耐久、遗物、第四层路线、事件和其测试也没有因本批完成而缩减。
