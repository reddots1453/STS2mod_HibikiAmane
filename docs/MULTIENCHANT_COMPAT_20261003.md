# 多重附魔兼容与终极耀斑文本（2026-10-03）

需求来源：玩家合集的 MultiEnchantmentMod v2.5.4，以及用户本轮明确要求。SYS-ENC-001 / CARD-N-400～499 的已有附魔通用规则用于兼容边界；DesignDoc相对HEAD的差异仍为此前已确认并同步内容，本轮不修改设计。前置快照 `6863ec656b830ac3eff8337121d1d974abc09ca7`。

## 实现

- 可选反射桥接外部 v2 公共 MultiEnchantmentApi.Enchant、EnchantmentScope.UntilCombatEnds、SuppressDeckVersionSync 与 GetEnchantments；无编译/清单强依赖。检测实际 CanEnchant 补丁 owner，避免只加载未启用的程序集就放开选牌。
- 已安装且接口有效时，共用 HasOpenSlot 不再提前排除已有附魔的牌，仍由完整 CanEnchant 判断卡型、封印、重复与容量等条件。覆盖锻成·锋利、伶俐、充能、打击、娅露丝的记忆、愤怒路线拾取等现有调用者。
- CombatEnchantmentCmd 接入外部存储/叠加/克隆/钩子，显式仅战斗有效并抑制 DeckVersion 同步。外部执行失败时不再尝试覆盖式回退。光之翼保留本模组已有容器和独立同名多层规则。
- 理外锻成安装多重附魔时按实际合法目标构建选项，不能再因全手牌已附魔而直接跳过；仍复查当前手牌，合法目标为零时直接结束。未安装外部模组时保留既有全附魔跳过路径。
- 共用 Has<T> 能识别外部附加槽中的灵魂联结、愤怒等；不更改设计明确要求已附魔阻断的寄生和不追加沉眠精华规则。
- 终极耀斑保留全体伤害及回合末在手牌的减费条件，仅将“耗能降低1”改为“费用减少”及暗之惩戒相同 `energyPrefix:maidenEnergyIcons(1)`。同步已有Debug文本期望与能量格式化；未新增或运行静态测试。

## 验收边界

状态 IMPLEMENTED，游戏内 NOT_RUN。需在 enchant 配置中测试已有迅捷/充能的攻击牌接受锋利、技能牌接受伶俐、全附魔手牌理外锻成合法/非法目标、灵魂联结在附加槽的抽牌、同名重复遵从外部规则、临时附魔不改变永久牌组、保存/继续及下一场战斗。无外部模组时复测全附魔跳过、光之翼容器。终极耀斑基础/升级的费用图标及回合末减费由用户实测。

仅生成Debug测试产物，不部署正式游戏或ModUploader，不修改其JSON；沙箱运行期间不替换活动模组。兼容参照来自本机安装包反编译，未改第三方原始DLL。


最终构建：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore` 成功，0警告0错误。按用户要求未运行静态测试。测试产物目录为聊天 outputs/multienchant-compat-debug-20261003，包含累计Debug DLL/PDB与当前源码资源PCK；该构建也包含前一批首次无Neow开局的新版地图兜底。游戏内验收NOT_RUN，正式游戏、沙箱活动模组及ModUploader未部署，上传器JSON未修改。
