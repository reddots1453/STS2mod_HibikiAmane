# DS27第五批：全量审计可信度

前置提交：`01b58da2`。DesignDoc逐行及词级差异为空。任务`DS27-07A`为IMPLEMENTED，仅指审计基础设施，不代表卡牌效果/渲染已通过；本批未改玩法、素材或DesignDoc，未部署。

## 修正入口

- `scripts/AuditCardLocalization.py`：以章节和类型行定位，不再用614～1842固定行号。识别括号衍生牌、编号圣言、事件标题、同名路线/内嵌卡牌；效果保留多行，不串入下一张卡；费用只取开头费用，不把“获得1费”当成卡费，也不把效果中的“技能牌”当成自身类型。
- C#读取排除注释/字符串大括号，分号空类不再串读下一类；注册属性中间插入其他属性不会漏注册；泛型与非泛型同名基类分别索引。解析常量/枚举构造参数转发及默认参数，尊重升级覆盖和显式base调用。无法静态解析的字段列为unknown，不猜测通过。
- 文本相似度只辅助排序，保留标点/分行。所有非技术模型仍标记`PENDING_EXACT_RENDERED_CONTRACT`，数值仍要求独立运行时契约。模板相似不能证明动态数值、分支或实际渲染。
- `--no-write`完全只读；`--output`指定独立报告，兼容旧默认路径但本轮不调用默认写出；`--strict-review`在未验文本、未知元数据或反向覆盖待核对时返回失败。原有并行报告保持未改。
- 增加有类型设计条目→模型的反向覆盖；不声称已自动理解无类型条目、全部自然语言规则或章节成熟度。

## 全量扫描结果（未通过）

224个注册模型，8个技术选项/代理模型，216个非技术模型仍需完整文本/数值/渲染验收。本数字不否定前四批独立测试，只说明此工具本身没有导入或认证那些运行时证据。

8项确定静态差异：

| 模型 | 检查结果 |
|---|---|
| DarkThrust | 普通设计，当前罕见 |
| GagCurse | 设计1费，当前2费 |
| LastStand | 普通设计，当前罕见 |
| LightWings | 稀有设计，当前普通；多重附魔实现仍另列待做 |
| MagicResonance | 当前注册但未找到当前DesignDoc同名条目，需退役/替换兼容核对 |
| MiasmaAbsorption | 普通设计，当前罕见 |
| PleasureDrowning | 普通设计，当前罕见 |
| SemenAppetite | 当前注册但未找到当前DesignDoc同名条目，需范围/兼容核对 |

反向覆盖3项：梦色的颜料、乳汁、欲望爆发。最后一项明确为未完成草稿，不能因为扫描发现它就自动实施；其他项需对照相关完整条目与源模型决定新增/替换。未擅自修改规则。

4个状态模型的关键词由非字面表达式产生，静态读取明确保留未知：BarbedHookStatus、BitingPaperStatus、ClothingBurnStatus、DissolvingFluidStatus；需要运行时元数据测试，不能算作无关键词。

独立本地报告：`.review/design_sync_20260927_audit.json`（生成物，不混入提交）。本轮SHA256：`1580177314726E016972BDE7ED8EDA07B7103512697B31C587712C1F4DDFF13D`。共享`.review/card_localization_audit.json`保持原有702增/507删；无覆盖。

## 验证

以下命令在Mod目录执行：

| 命令 | 结果 |
|---|---|
| `python scripts/TestCardLocalizationAudit.py` | 19项通过；含真实文档、224注册模型、只读保护、严格模式拒绝虚假完成 |
| `python scripts/TestDesignSync20260927.py` | 14项通过 |
| `python scripts/TestDesignSyncNeutral20260927.py` | 8项通过 |
| `python scripts/TestDesignSyncHoly20260927.py` | 6项通过 |
| `python scripts/AuditCardLocalization.py --output .review/design_sync_20260927_audit.json` | 返回1；8差异、4未知、216文本待验、3反向覆盖 |
| `python scripts/AuditCardLocalization.py --no-write --strict-review` | 已由真实调用测试执行；返回1且不写文件，额外报告未完成审阅 |
| `dotnet build MaidenSuccubus.csproj -c Debug --no-restore -p:DeployMod=false -p:ValidateMod=false` | 0警告0错误 |
| `./scripts/ValidateCardEffectTests.ps1 -ProjectDir .` | 224注册、222可执行、2设计待定；这是测试注册检查，不是运行时通过 |
| `./scripts/ValidateMvpContent.ps1 -ProjectDir .` | 通过 |
| `./scripts/ValidateLocalizationStyle.ps1 -ProjectDir .` | 通过 |
| `./scripts/ValidateStructuralContracts.ps1 -ProjectDir .` | 通过 |
| `./scripts/ValidateVisualAssets.ps1 -ProjectDir .` | 仍失败于317行诱惑度口红/独立数字旧契约；未改此UI |
| 范围内`git diff --check` | 通过 |

## 后续任务与限制

先以该清单继续逐牌核对，包括梦色的颜料等新增/替换、各卡费用/稀有度、光之翼多重附魔；用基础/升级真实元数据和效果验证，逐项补精确文案/渲染契约，不靠相似度消除待验状态。其余耐久、遗物、路线、事件和怪物范围仍按总审阅及既定边界推进。本批没有运行游戏场景，前四批运行时场景仍未实机执行，原总目标未完成。
