# DesignDoc → Plan → 实现更新协议

> 协议代号：DPP（Design–Plan–Production Protocol）
>
> 适用范围：`sts2_Maiden&Succubus`的全部设计、计划、实现和验收工作
>
> 核心地位：本协议是开发迭代的强制入口，不是可选的文档整理步骤

## 1. 权威关系

1. `DesignDoc.md`是玩法需求、玩家交互、结算规则、边界行为和设计意图的唯一权威来源。
2. `PLAN_FRAMEWORK.md`负责技术架构、功能拆解、实施顺序和验收标准，不得擅自补完DesignDoc中的待定设计。
3. 代码不得成为未记录需求的唯一载体。
4. `DRAFT`内容不进入正式实现；`OPEN`内容只允许实现无争议的公共框架；`READY`内容才可生成完整实现任务。
5. 每项完成的实现必须能够反向追踪：

```text
DesignDoc需求ID → Plan技术任务/里程碑 → 代码模块 → 验收ID → 测试结论
```

## 2. 稳定需求ID

需求引用使用稳定ID，不使用行号。章节移动、插入或重排时ID保持不变。

| 前缀 | 范围 |
|---|---|
| `DOC-*` | 文档定位、范围和治理规则 |
| `DBG-*` | 类尖塔设计原则与平衡基准 |
| `START-*` | 三种开局、外观和解锁 |
| `SYS-COR-*` | 堕落值 |
| `SYS-DES-*` | 欲望 |
| `SYS-BLS-*` | Boss恩赐 |
| `SYS-SEA-*` | 封印区 |
| `SYS-CTL-*` | 控制与挣脱 |
| `SYS-INV-*` | 侵犯 |
| `SYS-ENC-*` | 临时附魔 |
| `ENCH-*` | 具体附魔内容 |
| `SYS-SCR-*` | 圣言 |
| `KW-*` | 随身、断罪、净化等关键字 |
| `CARD-N-*` | 中立卡牌 |
| `CARD-C-*` | 堕落卡牌 |
| `CARD-H-*` | 圣洁卡牌 |
| `STATUS-*` / `CURSE-*` | 通用状态牌与诅咒牌 |
| `RELIC-*` | 初始、角色、事件及其他遗物 |
| `EVENT-*` | 事件 |
| `MON-*` | 普通、精英和Boss敌人 |
| `ACT4-*` | 第四幕 |
| `MP-*` | 多人规则 |

规则发生修改时沿用原ID。仅当规则被正式废弃并由语义不同的新规则替代时，旧ID标记`DEPRECATED`，新规则获得新ID。

未命名或描述不完整的内容先由章节ID范围承载；达到`READY`后再分配独立内容ID。禁止用临时名称造成“设计已经完成”的假象。

## 3. 需求成熟度

| 状态 | 含义 | 允许的技术动作 |
|---|---|---|
| `DRAFT` | 方向草案，行为或边界明显不完整 | 只研究，不进入正式Plan任务 |
| `OPEN` | 核心方向已确认，但仍有待定字段或内容 | 可搭公共接口和安全占位，不固化待定值 |
| `READY` | 玩家行为、结算、边界和必要数值无关键歧义 | 可生成完整Plan任务并实现 |
| `IMPLEMENTED` | 代码、资源和调试入口已完成 | 等待运行时验收 |
| `VERIFIED` | 已通过约定的自动检查和游戏内验收 | 作为回归基线 |
| `DEPRECATED` | 设计明确废弃 | 不实现；已有实现进入清理或迁移任务 |

成熟度描述设计状态与交付状态，因此`IMPLEMENTED`和`VERIFIED`优先于`READY`。设计发生实质变化时，已实现需求回退到`READY`或`OPEN`，并生成回修任务。

## 4. Git基线与逐行差异

### 4.1 强制规则

每次由用户修改DesignDoc之前，上一份已经接受并同步的DesignDoc必须存在于Git提交中。DesignDoc不得长期处于未跟踪状态。

当前初始基线：

```text
86d749d docs(maiden-succubus): baseline design document
```

### 4.2 更新前检查

```powershell
git status --short -- "mods/sts2_Maiden&Succubus/DesignDoc.md"
git log -1 --oneline -- "mods/sts2_Maiden&Succubus/DesignDoc.md"
```

预期DesignDoc在用户开始编辑前为干净状态。若它尚未被跟踪，必须先建立只包含DesignDoc的基线提交。

### 4.3 用户更新后的差异读取

无论修改是否被暂存，统一以最后一次已接受提交为基线：

```powershell
git diff HEAD -- "mods/sts2_Maiden&Succubus/DesignDoc.md"
git diff --word-diff=plain HEAD -- "mods/sts2_Maiden&Succubus/DesignDoc.md"
```

必要时以明确提交比较：

```powershell
git diff <baseline_commit> -- "mods/sts2_Maiden&Succubus/DesignDoc.md"
```

Git逐行差异用于保证没有漏读修改位置；需求ID语义差异用于判断实际影响。两者必须同时执行，不能互相替代。

### 4.4 同步完成后的提交

一次DesignDoc同步完成后，将以下内容放入同一个文档同步提交：

- 更新后的`DesignDoc.md`；
- 更新后的`PLAN_FRAMEWORK.md`；
- 更新后的`docs/DESIGN_TRACEABILITY.md`；
- 必要的验收清单或迁移说明。

推荐提交信息：

```text
docs(maiden-succubus): sync design requirements <ids>
```

不得把无关项目文件混入该提交。不得为了获得干净diff而重置、覆盖或丢弃用户修改。

## 5. 固定同步流程

当用户说“同步DesignDoc”时，执行以下步骤：

1. 用Git读取相对最后基线的逐行与词级差异。
2. 重新完整阅读DesignDoc，不能只阅读diff。
3. 以需求ID形成语义变更集：
   - `ADD`：新增需求；
   - `CHANGE`：已有规则改变；
   - `REMOVE`：规则删除；
   - `CLARIFY`：消除歧义但不改变行为；
   - `FILL`：填充原待定内容；
   - `CONFLICT`：新旧章节相互矛盾。
4. 对每项变化检查：
   - 数据和存档；
   - 战斗生命周期；
   - 奖励、商店、事件和地图；
   - UI和交互；
   - 卡牌、遗物、敌人及资源；
   - 本地化；
   - RNG确定性；
   - 调试入口；
   - 单人、多人与旧存档边界；
   - 已完成里程碑回归。
5. 更新Plan。每项变更必须落入以下一种结论：
   - 新增或修改技术任务；
   - 废弃或迁移技术任务；
   - 增加回归测试；
   - 只预留接口；
   - 等待设计完成；
   - 无需代码变更，并记录原因。
6. 更新追踪矩阵和验收ID。
7. 用户只要求同步时，到此停止，不编码。
8. 用户要求“同步并实施”时，只实现已达到`READY`的内容。

## 6. 开始里程碑前的漂移门禁

收到“继续M6”等实施指令时，必须先检查：

1. DesignDoc相对最后同步提交是否有未同步修改；
2. 修改是否已经进入Plan和追踪矩阵；
3. 当前任务依赖的需求是否达到允许实现的成熟度；
4. 新需求是否使既有实现、存档或验收标准失效。

存在未同步设计漂移时，先同步再实施。只有会实质改变玩家行为或技术架构的歧义才阻塞询问；原版语境下显然成立的规则不重复追问。

## 7. 完成定义

### 7.1 DesignDoc同步完成

- Git逐行diff已完整审阅；
- DesignDoc已全文重读；
- 所有语义变化均关联稳定ID；
- 每项变化都有成熟度与影响结论；
- Plan和追踪矩阵已同步；
- 已实现功能的回归范围已列出；
- 待定设计没有被擅自固化。

### 7.2 实现完成

- 代码和必要本地化完成；
- 构建通过；
- 调试入口可以复现；
- 验收项可以执行；
- Plan和追踪矩阵状态更新为`IMPLEMENTED`；
- 用户完成运行时测试后再更新为`VERIFIED`。

## 8. 固定口令

- `同步DesignDoc`：Git差异审阅、全文重读、语义分析、Plan和追踪矩阵同步；不编码。
- `同步并实施`：先完成上述同步，再实现所有`READY`变更。
- `继续Mx`：先执行设计漂移门禁，再推进该里程碑。
