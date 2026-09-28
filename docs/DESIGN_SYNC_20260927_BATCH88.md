# DS27第88批：按变更范围执行自动验证

前置`43cfda68`，先行计划`1c6635a2`，DS27-07R。DesignDoc行/词级无漂移。响应用户对逐牌适配与断言扩张的质疑，不再扩大谦逊逐牌表，本批不改玩法。

## 已修复

- 统一入口漏登后续新增的HumilityEffectContracts及GenerosityOfferingContracts；现在登记14套。
- 新增可重复`--suite ID`，去重、保持计划顺序；未知ID在生成输出目录/启动子进程前报错。`--list`可与选择组合，仍不执行。
- JSON schema升级2。完整报告及中途progress包含执行范围；选定通过为`passed_selected`，完整计划为`not_run`并列出未选套件。全目标/游戏未验收标记和退出2保持，不把局部通过变为全目标成功。

## 本批验证

```powershell
python scripts/TestDesignSyncValidationRunner20260927.py
python scripts/ValidateDesignSync20260927.py --suite humility_effects --suite generosity_offering
```

- 12个入口单元测试通过，其中3个新增场景检查过滤/去重/拒绝未知、局部报告边界、CLI只列不执行；它们使用模拟子结果，不是游戏测试。
- 第二条真实运行2套：谦逊936条、慷慨107条既有生产断言通过。本批没有新增卡牌档案断言。
- 报告：`obj/design-sync-validation/20260928T143617Z-0c7f59563e78/report.json`。测试HEAD为计划提交加本批未提交脚本；运行前后源码/HEAD无漂移。报告内exitCode=2表示选定离线通过但完整目标/实机未完成，不是代码测试失败。
- 其余12套未运行；不重复全套构建，没有修改C#或部署。范围diff检查通过。

## 待办边界

此批只补自动验证交付的缺口，不解决谦逊正式选牌入口/觉醒或尚待明确的依赖次数语义，也不声称通用改写方案已完成。全目标其他待办继续保留；实机脚本仍需可丢弃测试局的明确授权，不在正常存档运行。
