

## 2026-10-03 战斗提示按堕落区间/拘束类型分组（UI-COMBAT-FEEDBACK-002）

前置快照`9593103183e8d76e6bd955e7f448213b6599e554`，设计提交`173f6e023d0791a3ef26fc6f814639917f1713f4`。复核UI-COMBAT-FEEDBACK-001全文、欲望/拘束/堕落区间及既有文案接口；相对最新接受版本25c7438c逐行与词级无漂移。用户明确新规则已同步DesignDoc。沿用当前实际交互时点，不修改战斗机制、欲望支付、保存数据、音频或CG。

实现：新增独立CombatFeedbackTemplates解析与轮换类。texts每项含holy/neutral/corrupt三数组，每数组3槽；十交互通用共90槽，三类拘束交互额外by_control.attack/skill/power各9槽，共81个专属槽。按当前CorruptionQuery.GetBand选择；各(交互,实际组,专属类型)独立游标按1→2→3循环，跳过空槽。typed当前组为空则回退当前路线通用组；不跨路线。战斗切换重新加载文件并重置游标，事件只在显示非空文案时推进游标，未显示的空项不生成UI，不使用游戏RNG或System.Random。

通知接口增添ControlType? bindingType，拘束意图传本次意图类型、未完全挣脱传本次已结算挣脱对应类型、最终解除传被移除的最后类型；均由真实对象提供，不依据中文文本反推，不从多来源拘束中猜类型。旧control占位仍表示攻击/技能/能力，新增control_kind为attack/skill/power、control_name为乳虐/口虐/穴虐、corruption为当前数值。

配置兼容：旧字符串在内存中映射三路线首槽，其余为空；也允许通用三条数组。安装配置只读，绝不自动迁移覆盖。源模板升级保留已有填写值；其余九交互仍为空，默认伤害句保留在每路线首槽。有效文件缺失交互保持静默；单项错误只跳过该项，专属类型错误只跳过该分支；整文件语法错误延续原版安全回退并日志，不阻塞伤害。下一战重读配置。源combat_feedback.json属于本轮用户要求的文案配置，并非ModUploader/安装manifest JSON。

验收CF-GROUP-01区间边界-3/-2/2/3，战斗中跨区间立即使用对应组；02每组3条顺序循环、空槽跳过、各交互和分组独立、下战重置；03三种拘束正确专属/通用回退、没有类型时用通用；04旧文件正常、部分配置静默、enabled=false、配置异常不阻塞；05占位数值/类型正确，多段伤害保留，四行/浮动/场景清理不退化。IMPLEMENTED待手测，不运行静态测试，构建后暂不部署。继承上一欲望读档和瘴雷动态计数包，不改本地安装、沙箱、ModUploader或用户存档。


### 填写新版combat_feedback.json

编辑texts下面的交互键。holy（-5到-3）、neutral（-2到2）、corrupt（3到5）每个数组的三个字符串就是三个文案，按1→2→3循环；空字符串跳过。其他占位符和各交互已有数据含义见COMBAT_FEEDBACK_20261003.md。无需改schema_version或键名。enabled:false仍关闭全部提示。每场战斗读取一次，修改在下一战生效。

例如desire_increased：
```json
"desire_increased": {
  "holy": ["", "", ""],
  "neutral": ["", "", ""],
  "corrupt": ["", "", ""]
}
```

拘束相关三交互还有by_control：attack=乳虐（影响攻击牌），skill=口虐（影响技能牌），power=穴虐（影响能力牌）。每个类型内部也是holy/neutral/corrupt三个数组各3槽。填写专属当前路线组时优先显示；该专属组全空则使用交互上方通用同路线组。若只想某种类型有提示，将通用组留空，只填写对应专属组即可。其他交互当前使用通用路线组，没有可靠类型时不会猜测。

新增占位符：{corruption}当前堕落值；拘束三交互可用{control_kind}英文类型键、{control_name}乳虐/口虐/穴虐。旧{control}保持攻击/技能/能力。最终解除采用最后解除类型；未完全挣脱采用本次挣脱处理的类型，不表示剩余所有拘束均为该类型。多源场景请据此写文案。

旧版"交互键": "单条文案"仍可使用，运行时映射三路线第一条，不写回文件；用户已有文案不会被自动改写。模板留空供用户填写，不为九类交互自行生成新叙述。


最终构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`通过，0警告0错误。未运行静态测试，游戏内NOT_RUN。独立包outputs/combat-feedback-groups-20261003包含新版DLL/PDB、保持原样的最新PCK和分组空文案配置；源与包有新版配置，安装配置未改。继续暂不部署。
