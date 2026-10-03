

## 2026-10-03 战斗RPG浮动提示（UI-COMBAT-FEEDBACK-001）

前置快照`a49b1a277e1cca361b9a5151990db5b645654466`，设计提交`420976fb89a21ffb726b3a149d86067b707bb968`。用户主动要求实现并补充九类空文本接口；已读取协议及DesignDoc UI/欲望/拘束/魔装相关规则，DesignDoc相对上一接受版本无漂移；相对HEAD的既有修改已由前批同步，不覆盖其他用户编辑。本批先单独记录用户确认需求，再同步Plan/追踪后实现。

技术：CombatTextFeedback.Notify统一呈现入口，Hook.AfterDamageGiven读取每段最终DamageResult，独立于每个Power监听器、亦覆盖致死伤害；DesireEvents只监听实际上升与跨阈值；意图执行、魔装伤害分支、挣脱投影及最后拘束移除处接入空文案事件。全部UI入口Safe.Run，仅本地天音，独立CanvasLayer附于当前战斗房间，四行轮廓字体，2.4秒浮动渐隐。文本配置读一次每场战斗、空值不生成节点、解析失败仅日志回退，不阻断玩法。战斗结束/赢得战斗/切房间/切场景清除。Config默认伤害中文，其余九项严格为空，不更改CG/音频开关。

验收ID：CF-01普通伤害正式敌人名/准确生命伤害；CF-02多段每段独立、四行上限、没有文字重叠；CF-03全格挡不报告生命伤害；CF-04战斗结束和继续读档无残留；CF-05九空文案无输出、填入后下一战生效；CF-06欲望跨8/满值仅边沿触发；CF-07多来源拘束只在全部解除后通知，剩余拘束正确报告；CF-08魔装伤害实际耐久下降才提示；CF-09多人仅本地天音，不影响结算。状态IMPLEMENTED待游戏内验收，不运行静态测试，仅build。依据持续授权仅本地游戏部署，安装manifest JSON、沙箱和ModUploader不动，新combat_feedback.json是用户可编辑文案文件，不是安装manifest。


### 用户填写文案

编辑安装DLL旁的`combat_feedback.json`（UTF-8）；保留键名和JSON引号，将`texts`对应的空字符串替换成自己的文本。`enabled:false`关闭全部提示。文件不含运行存档；不会因为填写文字而改变机制。每场战斗读取一次，配置编辑在下一战生效。没有该项数据时不猜测来源：例如一般欲望上升没有enemy，魔装伤害耐久提示也无可靠敌人名。不要在这些项填写enemy。请填写纯文本，可用\n换行（建议单行短句），不是BBCode。

| 键名 | 交互 | 可填写占位符 |
|---|---|---|
| damage_received | 受到敌人实际生命伤害，每段一次 | {enemy}、{player}、{amount} |
| desire_attack_received | 欲望攻击开始执行 | {enemy}、{player}、{amount}（意图基础欲望量，实际增长另由下一项提供） |
| desire_increased | 欲望实际上升 | {player}、{amount}（实际增加）、{old}、{new} |
| desire_reached_8 | 从低于8上升至至少8 | {player}、{old}、{new} |
| desire_reached_max | 从未满上升至满值 | {player}、{old}、{new} |
| armor_damage_received | 因敌人伤害降低魔装耐久 | {player}、{amount}（实际损失）、{old}、{new} |
| control_intent_received | 拘束意图开始执行（可能被格挡阻止） | {enemy}、{player}、{amount}（挣脱要求）、{control}（攻击/技能/能力） |
| escape_incomplete | 挣脱牌结算后仍受拘束 | {enemy}（本次挣脱来源）、{player}、{amount}（本次挣脱量）、{new}（所有剩余拘束合计）、{control}、{card}（来源原牌名） |
| control_released | 最后一项拘束被解除（挣脱/直接解除/来源死亡等） | {enemy}（最后来源，如存在）、{player}、{control} |
| invasion_intent_received | 合法侵犯意图开始执行（可能被防护阻止） | {enemy}、{player}、{amount}（意图基础伤害） |

九类新增文案仍为空；不会自行填入玩家叙述。安装时只首次创建文案配置，后续部署必须保留用户已填写的文件。源目录和聊天输出均提供空模板，可作为恢复备份；此JSON与安装manifest不同，不改manifest。

构建通过：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`，0警告0错误。静态测试NOT_RUN，游戏内NOT_RUN；等待用户实测，编译不代表视觉验收。

部署状态：编译后发现游戏进程SlayTheSpire2（PID7180）重新启动，本地部署在首次进程检查处停止，未替换任何安装产物，也未创建安装文案文件。已请求用户保存退出后继续；产物和空文案模板保存在聊天outputs/combat-feedback-20261003，游戏验收仍NOT_RUN。

用户最新指令“暂不部署”：撤销本轮安装动作，停止部署并等待以后明确授权。所有源码与Debug产物已保存，安装目录、沙箱、ModUploader和原有JSON均未变更，游戏验收仍NOT_RUN。
