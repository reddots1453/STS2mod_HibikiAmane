# Changelog

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
