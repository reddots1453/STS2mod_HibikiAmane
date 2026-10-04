

## 2026-10-04 暂停战斗RPG文本（UI-COMBAT-FEEDBACK-005）

快照`10fffc9d581027df0c465b2f5c8c3f9f820d0c8c`，设计提交`919742e0291c32827b5a1e5a2ac01fc378f97af9`。已完整读取CombatTextFeedback/FeedbackTemplates和UI-COMBAT-FEEDBACK-001～004设计及现有触发边界；对HEAD及最后接受版本的DesignDoc逐行/词级漂移留档，其他事件叙事和反馈新通知OPEN设计保留，不混入当前范围。技术任务READY：在统一入口增加只读PresentationEnabled=false，Initialize在订阅事件前返回，Notify在读取配置/轮换/创建浮动层前返回。所有现有Notify接口、清理方法、文案JSON和解析器保持；已有enabled=true不能绕过暂停。独立CorruptionChangeFeedback和原版浮动伤害数字不受影响，不改战斗Hook及机制。验收入口为所有受伤/欲望/拘束等事件均无RPG浮动文案，原生数字及堕落变化仍正常；仅构建，不运行静态测试、不部署，源/安装JSON均不改。
