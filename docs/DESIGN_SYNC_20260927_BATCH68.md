# DS27第68批：全注册卡牌文本证据清单

2026-09-28；前置`54779591`，计划`d67c40a5`。DesignDoc逐行/词级无漂移。仅新增只读审查工具和测试，不改任何游戏规则、素材或本地化，不部署。

## 全量清单而非笼统计数

现有审计的pendingText=215没有汇入所有独立原生文本测试，不能把它解释为215张均无测试或均有错误。新工具逐一枚举225个实际注册模型，包含模型/标题/源文件、已审阅文本提供者、断言锚点、源文件SHA256、入口完整性错误和明确的runtime=not_run。

排除8个明确技术选择器及2个明确退役兼容身份后，215张现行卡牌/衍生牌的本次清单为：

| 已审阅源码证据类别 | 数量 | 证明范围 |
|---|---:|---|
| full_run_and_combat_declared | 119 | 存在独立全文预期，真实Run与战斗实例检查接线；未执行 |
| full_combat_only_declared | 5 | 有战斗实例全文，但不能据此证明真实Run显示；未执行 |
| partial_text_declared | 2 | 只核对某段文字/包含关系，不是整张卡全文；未执行 |
| not_identified | 89 | 已审阅入口尚未识别全文，属于后续人工审查队列，不证明仓库其他位置绝无测试 |

仅战斗全文的5张：FlameSword、WindGodCloak、BeyondReasonForge、SuperRegeneration、GagCurse。片段2张：DarkStorm、CurseInfection。炎之剑原测试名称写“outside combat”，实际最初全文断言仍使用同一个战斗实例并传PileType.None；另有永久实例后缀片段检查，但不足以将其提升为真实Run全文。因此本清单按实际对象与断言范围分级，不相信测试名字。

NeutralContract/HolyContract等元数据Entries不会被当作全文。七个已审阅全局Validate提供者、专用Catalog Run接线、六圣言泛型接线和GagCurse基础版助手分别核查；文件/声明数量/未知模型/断言锚点/Runner或Catalog接线缺失会报错，不静默生成“通过”清单。此工具不是C#控制流分析器，锚点守卫也不证明任意源码变更后的语义；提供者变更仍须人工复核。

89项中直接继承中立/圣洁/衍生基类的后续核对项包括Bath、BindingInsight、DrowsyStatus、HumilityLesson、MaidenDefend、MaidenStrike、Transform等；HumilityLesson仍受Q14约束，不以补测试固化未明确规则。注册清单也不解决原审计的2个仅DesignDoc标题或未实现事件/怪物/路线，因此没有缩减全目标。

## 使用与安全边界

```powershell
python scripts/ReportCardTextCoverage20260927.py
python scripts/ReportCardTextCoverage20260927.py --output obj/card-text-evidence-new.json
```

默认仅打印摘要，不写任何文件。显式输出仅允许本Mod obj目录中的新文件，拒绝覆盖和越界，不触碰并行.review/card_localization_audit.json。退出0只代表清单生成且已登记接线无异常；goalCompleted恒为false，不导入旧游戏报告，不宣称游戏已验收。

## 验证

- 本批清单实际输出`obj/card-text-evidence-b68.json`；225唯一行、完整性错误0、全部runtime未运行。
- 新增9项自测：全注册模型唯一、无执行声明、元数据不充当全文、片段/同一战斗实例None正确降级、技术/退役隔离、入口断开、真实Run或锚点缺失、未知模型/文件丢失、默认只读与共享报告路径拒绝。
- `python scripts/ValidateDesignSync20260927.py`：12/12离线通过，日期静态524＋审计29＝553；13760生产纯规则、33编码，双构建DeployMod=false零警告零错误，MVP/结构/本地化/卡牌登记/视觉门通过。
- 统一报告`obj/design-sync-validation/20260928T072943Z-1dcae0600954/report.json`，测试HEAD`d67c40a5`加未提交的本批脚本；前后SHA256均`12e8535ee4507a1f504a755093825de55b6fe573659266e358eebd1af9a15af0`。随后写入的验收文档不在当次指纹内。
- 原全卡审计仍为225、明确失败0、未解析0、pendingText215、designOnly2、retiredCompat2。没有修改严格模式、提高运行时验收状态或减少未完成范围。
- 游戏六组均未运行，无部署、无游戏操作、未覆盖并行修改或暂存。
