# HibikiAmane 本轮累计修复清单 — 2026-10-02

以下列出近期玩家反馈、本次上传包所包含的累计修订。代码已修改且Release编译通过；未统一完成游戏内验收，因此“已修改”不等于“已实测确认”。谦逊上一版网格选择已由用户确认正常，本次改成手牌选择仍待复验。本包是原版内容版本，不是SafeForWork版本。

| 序号 | 问题 | 本包修改内容 |
|---|---|---|
| 1 | 燃烧在怪物攻击后才结算 | 移到敌方攻击前，先结算燃烧伤害。 |
| 2 | 节制之戒/环递归互相打出形成循环 | 先将所选批次的实际牌实例全部移入出牌区，再逐张原生自动打出；同链嵌套共享实例预留记录，已处理的实例不能再次入选，下一独立链解除限制。 |
| 3 | 节制选牌堆名称全显示消耗堆 | 抽牌堆、弃牌堆、消耗堆名称按选项实例绑定。 |
| 4 | 处女跨层减少堕落值不生效 | 用独立跑局跨幕订阅执行，解除对未触发角色钩子的依赖。 |
| 5 | 谦逊打出后悬停、未进入选择 | 先改为原生网格，用户反馈已正常；随后依要求改回原生手牌选择。过滤合法牌、无候选跳过、选后复核；加选择进入/返回日志。新手牌界面待复验。 |
| 6 | 嫉妒路线删牌无法推进 | 接入永久删牌钩子计算试炼进度。 |
| 7 | 试炼遗物奖励重复领取/被RelicRewardChoices替换 | 使用固定Reward领取回调，内部复用原生遗物预览和入栏动画，目标阶段遗物/奖励收据防重；清理重复或旧阶段遗物时不重放既有拾取效果。原版与兼容环境均待实测。 |
| 8 | 不领取试炼奖励便不能走地图 | 奖励可跳过/关闭并保留待领取状态，取消领奖地图锁，同一房间限制自动提示一次。 |
| 9 | 预览卡图全是痛击 | 补齐模型纹理getter及NCard.UpdatePortrait/UpdateVisuals刷新入口，覆盖原版痛击兼容路径的显示兜底。 |
| 10 | 百科预览产生canonical模型Owner警告 | LibraryAuraOverlay访问Owner前排除只读预览模型。 |
| 11 | 血条下出现内部红剑状态与未翻译键 | 在Power可见性、原生Power容器和HoverTips三处过滤既有四类内部载体。保留存档数据、意图次数和冷却。截图仅证实内部载体误显示，不据此宣称所有头顶意图图标都已验证。 |
| 12 | 衍生牌/敌人意图生成牌无洗入动画、计数异常 | 补齐加入抽牌/弃牌堆的原生预览及动画等待，按实际牌实例显示。 |
| 13 | 拘束被玩家格挡回避后没有冷却 | 修正格挡回避后的下一回合拘束跳过状态。 |
| 14 | 噬尸蛞蝓等原生击晕与临时意图重合异常 | 原生STUNNED可覆盖未执行的本mod临时意图，防止被临时状态静默拒绝；保留低频诊断。 |
| 15 | 魔法之剑奖励界面大图打开失败/卡住 | 修正附魔图标纹理类型兼容；本包同时包含上述卡面刷新修复。不同环境仍待复验。 |
| 16 | 多重再现本回合/下回合状态无法叠加 | 分别保存两种授予数，以总层数显示，额外回合逐层消费并保留旧字段迁移；升级费用2→1。 |
| 17 | 理外锻成在手牌均已附魔时卡死 | 无合法空槽跳过选择，第一段后目标失效也跳过；保留光之翼有空槽时的合法多重附魔。 |
| 18 | 变化得到拾起附魔卡却不触发 | 修正None→Deck与原生Deck→Deck变化两种拾起回调识别，覆盖魔法之剑/疾风之剑/闪耀之剑/娅露斯的记忆；一次性标记与既有附魔保留。 |

另包含本轮名称与文案修改：模组清单、RitsuLib设置/授权标题、上传器标题统一HibikiAmane；设置中的“成人”替换为“瑟瑟”。角色中文名仍为响木天音。为兼容既有存档，ModId、程序集/DLL名及资源路径保留MaidenSuccubus。

## 尚未确认修复

重启后点击继续概率黑屏：日志只确认停在预加载附近，没有确定根因。已加入本地ContinueDiag阶段与资源等待日志，不跳过加载、不强制超时成功。本包包含这些定位日志；不可标为黑屏已修复。玩家多mod环境所有头顶色情意图映射也未完成逐怪物实测。

## 上传准备结果

Release构建：`dotnet build MaidenSuccubus.csproj -c Release -p:DeployMod=false -p:ValidateMod=false --no-restore`，0警告0错误。未执行静态玩法测试或启动实际跑局。PCK重新封装648个资源，挂载后逐项路径/大小/SHA-256核对通过；资源相对上次打包仅characters.json变化。打包引擎报告本机证书/Sentry初始化失败，但PCK_COMPLETE=648、PCK_VERIFIED_ALL=648及退出码0均确认打包与资源校验完成，这不是玩法运行结果。

上传器content根内容直接落入游戏mods，因此目录为：

```text
content/
  MaidenSuccubus/
    MaidenSuccubus.dll
    MaidenSuccubus.json   (name=HibikiAmane, has_pck=true)
    MaidenSuccubus.pck
```

目录中不放旧Debug PDB，不附带RitsuLib依赖DLL。workshop.json只更新title与changeNote，其他属性和既有mod_id.txt/image.png保持。只准备上传文件，未执行Steam上传，未替换游戏mods安装目录。

前置快照：`61d03721f38e62e514bf9a8b6f5c365374ed4df9`；累计代码提交：`76fd7482e67f37cb73c498cf69addf3b67e220d5`。

上传目录：`D:\game_backup\steam\steamapps\common\Slay the Spire 2\ModUploader-win-x64\MaidenSuccubus\content`。

旧上传内容备份：`C:\Users\wilson\Documents\Codex\2026-09-30\amane-recovered-context\work\uploader-release-20261002\uploader-before`。

- `MaidenSuccubus.dll`：1846784字节，SHA-256 `dd73410892a66118369ad4fa2073a9277a4e63cd293578aad3ff0d3f29301871`。
- `MaidenSuccubus.json`：350字节，SHA-256 `50dc9d1d67a9ca1d0ce8fceff1cf543f1d1fe1f3bc24d4a5409831dca2266b4e`。
- `MaidenSuccubus.pck`：328530996字节，SHA-256 `1823aaede9dfd3189a812dca4c3406620dae91e6455b3a3194484b04e7129ee1`。
- `workshop.json`：822字节，SHA-256 `a980dfe79e735265be22a753070d15a9465913b4869a70d74f1d24897d508a5a`。
