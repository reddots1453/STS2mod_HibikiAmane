

## 2026-10-04 开局状态行简化（START-003-STATUS-TEXT-001）

快照`62dd34501c126b06a1e311054073ef13cff474f1`，设计提交`d1b29530c24721718b6afae156942a6a32e358d1`。复核START-003选角名称/解锁预览规则与当前状态行，相对HEAD和51390df6逐行/词级差异留档，独立文案工作树保留。本次用户只要求状态行“已解锁”或“未解锁”；唯一代码修改StartRouteSelector.Refresh的_rows[2].Text，颜色/条件行/初始堕落值/开始按钮判定不变。READY，构建后IMPLEMENTED，手测前不VERIFIED。本包累积瘴雷每段伤害并获得1费、天平概率说明、V6正式背景、开局历史恢复及此前试炼UI/商人尺寸修订。仅build，不运行静态测试，不部署，不改JSON。

START-003-STATUS-TEXT-001 IMPLEMENTED：状态行只显示已解锁/未解锁；0警告0错误构建，静态测试NOT_RUN，游戏内NOT_RUN，未部署。
