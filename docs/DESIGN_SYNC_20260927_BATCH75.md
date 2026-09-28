# DS27 第75批：万念俱灰双X

日期：2026-09-28。变更前`7ddbe565`，技术计划`4ff4f20b`。DesignDoc逐行/词级无未同步变化。

## 本批实现

- 当前设计为6Y点伤害、X/X+1次；修正战斗外旧6X/Y公式及升级公式。
- 去除首次OnPlay后清零的`_desireSpent`缓存。改从RitsuLib 0.4.64本次CardPlay台账读取Value；框架为一次OnPlayWrapper的每个重放绑定同一台账，免费自动打出也捕获Value，不要求实际Spend回调。
- 预览使用支付解析器的Value，保留框架X修正与生命替代；不改变Q17免费至打出待定设计。
- 原测试直接注入AfterSpent，未覆盖真实付款。改为七组基础/升级场景：正常付款、付款重放、免费自动、自动重放、零能量、零欲望、生命替代。每组还再次打出同一实例，验证重新捕获资源；检查全文、预览、实际伤害/段数、资源余额与生命付款。

## 定向验证

- `python -m unittest discover -s scripts -p 'TestDesignSyncAllHopeLost20260927.py'`：3/3通过。
- `dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false --no-restore`：成功，0警告、0错误。
- 游戏内脚本已编译，未运行。可由既有卡牌效果测试入口选择AllHopeLost基础/升级场景，在获准的可丢弃测试局执行。
- 按用户最新要求，仅检查本轮变动范围及直接依赖；没有重跑全卡文本审计、全量12门或未修改卡牌。此前全量结果不能冒充本批重测。
- 未部署、未启动游戏；未改素材、其他Agent文件或暂存内容。全部目标仍未完成。
