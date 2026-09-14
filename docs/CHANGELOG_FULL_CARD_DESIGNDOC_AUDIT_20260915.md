# 2026-09-15 全卡牌 DesignDoc 对照与悬停整改

## 版本边界

- 开始提交：`708d2671dbb8843d62746a0e303e66e76df338b6`
- 开始快照：`maiden-pre-full-card-designdoc-audit-20260914`
- 唯一需求来源：本模组当前 `DesignDoc.md`
- 本轮没有修改或提交用户正在维护的 `DesignDoc.md` 与 `CHANGELOG.md`，也没有处理仓库内其他模组、图片及反编译研究资产。

## 全量范围

本轮不是按截图抽查，而是对内容契约中的全部215张注册卡牌执行一致性审计：

- `MSNeutralCardPool`：44张；
- `MSCorruptCardPool`：62张；
- `MSHolyCardPool`：55张；
- `MSInvasionCursePool`：17张；
- `MSGeneratedCardPool`：37张。

其中199张可直接映射到 DesignDoc 卡牌条目；7张是机制选择代理牌（`EnchantmentChoiceCard`、`FourthRouteQuestChoice`、两张透支选择牌和三张圣痕选择牌）；另有9张由基础牌、衍生牌、事件牌或待设计条目的所属章节定义（`MaidenStrike`、`MaidenDefend`、`CalmMind`、`DrowsyStatus`、`IceMist`、`CounterBarrierII`、`InsatiableGreed`、`HypnosisCurse`、`ClimaxBanCurse`）。所有215张仍必须具备注册、本地化、卡池和测试登记，不能因没有独立目录标题而漏审。

构建期审计现在逐张检查：注册与本地化一一对应、DesignDoc 映射、类型、稀有度、基础/升级费用、SmartFormat 变量、Power 文本、术语颜色、公共悬停入口和命名衍生牌悬停。审计报告生成在 `.review/card_localization_audit.json`，该目录继续作为本地审阅资产，不纳入提交。

## DesignDoc 差异修复

- `MagicBurst`（魔力爆发）费用由0改为 DesignDoc 的1费。
- `SacrificialFrenzy`（献祭狂热）稀有度由罕见改为 DesignDoc 的普通。
- `MindsEye`（心眼）移除旧版的消耗关键字。
- `SummonThunder`（唤雷）和 `Bath`（泡澡）不再把“存在魔力增幅状态”误当作魔力解放；二者都通过统一支付流程实际消耗魔力增幅或魔装耐久后才触发追加效果。
- `ResistanceGloves`（抵抗手套）改用可升级动态变量，卡面与效果现在分别精确显示并结算挣脱2/3。
- `ControlPower` 按攻击、技能、能力三种拘束分别使用 DesignDoc 新增的正式游戏描述。

## 卡面文本、颜色与悬停

- 修正唤雷、泡澡的“魔力解放”术语；补回火焰绽放缺失的魔力解放效果行。
- 魔法之剑、锻成・充能、娅露斯的记忆统一采用“附魔：[具体附魔]”格式，并为附魔名使用紫色。
- 修正抵抗手套2/3、精液食粮命名、战术核心“效果翻倍”、魔力共鸣语序及玩火换行。
- 为冰雾、冰晶碎片、发情、赤裸欲、困了、粘液、孢子心灵和功性魔防壁后续牌等命名衍生牌统一着色；对应来源牌仿照原版“小刀”提供实际衍生牌悬停。
- 圣言来源牌只提供“圣言”文字悬停，明确禁止展开六张圣言卡牌，避免悬停牌列越出屏幕。
- 公共悬停解析覆盖断罪、净化、魔力解放、魔力增幅、圣言、欲望、堕落值、挣脱、拘束、诱惑度、魔装耐久、燃烧、破碎、圣域、虚弱、易伤、脆弱、力量、敏捷、荆棘、覆甲、滑溜、残影、变身、格挡、击晕、消耗、保留、虚无和固有。
- 拘束、挣脱和圣言新增正式静态悬停文本；三种拘束 Power 描述中的拘束、挣脱和侵犯均按 STS2 富文本规范着色。
- 所有路线卡、共享衍生牌、圣言、侵犯诅咒和事件诅咒基类均接入同一悬停解析管线；构建门检查该入口与命名衍生牌预览不得回退。

## 自动测试与验证

- 卡牌效果目录与215张注册卡完全一致：213张具备可执行数值断言；只有 DesignDoc 明确写为“效果待后续设计”的 `HypnosisCurse`、`ClimaxBanCurse` 保持 `DESIGN_PENDING`。
- 更新唤雷、泡澡、心眼、魔力爆发、抵抗手套和献祭狂热的自动断言，覆盖实际支付、额外伤害/能量、费用、稀有度、关键字和基础/升级显示值。
- 第二轮新增/变更专项套件保持59张卡牌。
- 修复测试热键冲突：`F10`运行全部215张，`Shift+F10`运行59张第二轮卡牌，`Ctrl+F10`运行9项拘束场景。
- 非部署构建和显式部署构建均通过：0 warning、0 error；内容契约、结构契约、本地化、215张卡牌审计、卡牌效果测试目录和拘束测试目录全部通过。
- 已部署到 `D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`。游戏内数值测试仍需由玩家进入一次可放弃的响木天音战斗后按相应热键执行，构建期结果不冒充运行时通过。
