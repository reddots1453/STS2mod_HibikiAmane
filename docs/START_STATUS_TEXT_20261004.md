

## 2026-10-04 开局状态行简化（START-003-STATUS-TEXT-001）

快照`62dd34501c126b06a1e311054073ef13cff474f1`，设计提交`d1b29530c24721718b6afae156942a6a32e358d1`。复核START-003选角名称/解锁预览规则与当前状态行，相对HEAD和51390df6逐行/词级差异留档，独立文案工作树保留。本次用户只要求状态行“已解锁”或“未解锁”；唯一代码修改StartRouteSelector.Refresh的_rows[2].Text，颜色/条件行/初始堕落值/开始按钮判定不变。READY，构建后IMPLEMENTED，手测前不VERIFIED。本包累积瘴雷每段伤害并获得1费、天平概率说明、V6正式背景、开局历史恢复及此前试炼UI/商人尺寸修订。仅build，不运行静态测试，不部署，不改JSON。

START-003-STATUS-TEXT-001 IMPLEMENTED：状态行只显示已解锁/未解锁；0警告0错误构建，静态测试NOT_RUN，游戏内NOT_RUN，未部署。


## 2026-10-04 用户授权仅部署本地游戏（LOCAL-DEPLOY-20261004-205215）

用户明确要求“仅部署到本地游戏目录”，覆盖此前暂不部署指令，仅此安装目录授权。最新源码完成提交`ad3a63d3c0193a628f128a596427ac63fb859297`，部署前文档快照`c0489f3acd91afc5d858ed4e9779b68bbffa9772`；复用本轮已通过的Debug构建，0警告0错误，命令`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`。累计部署最新已提交修订：开局状态行仅保留已解锁／未解锁；瘴雷每段造成3/4点伤害并获得1费，按本场欲望消耗次数重复；天平悬停显示实际圣洁／堕落卡池概率及浮动文本；正式V6选角背景；独立通关凭据及旧解锁标记／备份恢复；女神试炼事件式开场UI；假商人事件角色立绘与普通商店匹配。历史记录缺少最终堕落值时不推断旧通关路线。所有已存在JSON配置均保留。

仅替换`D:\game_backup\steam\steamapps\common\Slay the Spire 2\mods\MaidenSuccubus`下MaidenSuccubus.dll/.pdb/.pck，替换前备份`C:\Users\wilson\Documents\Codex\2026-09-30\amane-recovered-context\backups\local-start-status-text-deployment-20261004-205215`，安装后逐项SHA-256一致，原有JSON哈希不变。未改沙箱、ModUploader、上传包或其他游戏/模组目录。未运行静态测试与游戏内验收，用户实测后再标VERIFIED。部署明细：聊天outputs/start-status-text-20261004/local-deployment-20261004-205215.json。
