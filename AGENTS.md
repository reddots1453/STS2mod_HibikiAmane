# Maiden & Succubus 持久开发约束

## 核心协议

本目录的所有设计、Plan、实现和验收工作必须遵守：

- `docs/DESIGN_CHANGE_PROTOCOL.md`
- `DesignDoc.md`
- `docs/DESIGN_TRACEABILITY.md`
- `PLAN_FRAMEWORK.md`

`DESIGN_CHANGE_PROTOCOL.md`是开发迭代的核心治理协议。它优先约束本Mod的文档同步和需求落地流程；不得跳过它直接依据零散对话或局部DesignDoc片段实施。

## 每次实施前

1. 用Git检查`DesignDoc.md`相对最后提交是否发生变化。
2. 若变化尚未同步，先完整执行DesignDoc → Plan同步。
3. 完整阅读与当前任务有关的DesignDoc规则、追踪矩阵和Plan任务。
4. 只实现成熟度允许进入开发的需求；`DRAFT`不得实现，`OPEN`不得固化待定值。

## DesignDoc变更检查

必须同时执行：

```powershell
git diff HEAD -- "mods/sts2_Maiden&Succubus/DesignDoc.md"
git diff --word-diff=plain HEAD -- "mods/sts2_Maiden&Succubus/DesignDoc.md"
```

逐行diff用于发现修改位置，需求ID语义分析用于判断实际影响。只读diff、不全文复核不算完成同步。

## 状态更新

- 代码完成但未游戏内验收：`IMPLEMENTED`。
- 用户完成统一手测：`VERIFIED`。
- 需求实质改变：从`IMPLEMENTED/VERIFIED`回退到`READY`或`OPEN`并生成回修任务。
- 每次同步必须更新`docs/DESIGN_TRACEABILITY.md`和`PLAN_FRAMEWORK.md`。

## Git边界

- 用户修改DesignDoc前，上一接受版本必须已经提交。
- 文档同步提交只包含本Mod相关的DesignDoc、Plan、追踪与验收文档。
- 不得重置、覆盖或丢弃用户尚未提交的DesignDoc修改。
- 不得把仓库中其他项目的修改混入本Mod提交。

## 每轮写操作的版本管理

任何会修改本目录文件的开发、修复、重构、本地化或文档任务，都必须形成可恢复、可追踪的独立变更批次：

1. 写入前核对当前分支、HEAD、目标目录状态和最近提交，并以标签或明确的前置提交保存变更前快照。
2. 本轮必须更新`CHANGELOG.md`，记录日期、需求/任务边界、变更前快照、实际修改、验证结果和是否部署。
3. 完成后只暂存本轮明确修改的文件，创建一个范围清晰的Git提交；严禁使用仓库级`git add -A`、`git clean`、重置或回滚其他模组和研究资产。
4. 未通过构建或约定验证时不得提交为完成；未经用户明确要求不得部署。
5. 最终交接必须给出变更前快照、完成提交、验证命令及结果。若因权限或外部阻塞无法创建标签/提交，必须明确报告，不能把未提交修改当作已完成交付。
