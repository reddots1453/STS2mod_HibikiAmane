# DS27 批次32：贪婪定向免费商店

2026-09-28；前置快照`4fa8efe8`；ACT4-001→DS27-05L。
本批IMPLEMENTED待手测；预约事件完整集成未完成。未部署，总目标未完成。

## 审计证据与改造

DesignDoc逐行/词级无漂移，完整复核贪婪及三试炼阶段独立替换。旧实现只在CreateForNormalMerchant后设置FreeShopActive，没有问号替换，普通商店错误地消耗资格；再次生成库存还会清除Active。Stage3描述是旧“下一个商店”，Stage4仍为空阶段说明。

当前DLL核对：RunManager.EnterMapPointInternal在选房前保存，调用RollRoomTypeFor→CreateRoom→记录地图历史→EnterRoom；原版MerchantEntry.Cost只在MerchantRoom通过Hook.ModifyMerchantPrice读取动态价格。因此：

1. GreedUnknownRoomPatch在实际问号选房最后优先级选中Shop，不提前抽取原事件/遭遇；普通商店和其他地图点不变。不会在库存预览、价格查询或卡牌执行中消耗资格。
2. GreedShopService按RunState弱引用保存同步选房的短期决定；只有同一幕/楼层、Unknown、真实MerchantRoom创建成功才绑定仍被本人持有的资格。无需新增网络消息，各端执行相同原生选房入口，资格拥有者之外的玩家不免费。实际多人一致性尚未手测。
3. GreedShopState保存Pending、Active、位置；运行时另外绑定房间实例，避免同一楼层中事件内的另一商店错误免费。仅本角色3/4觉醒阶段有效，离开房间清除Active但不丢未消费Pending，克隆不共享房间引用。
4. 原生价格钩子的最后Postfix在所有模型修价之后归零，覆盖库存全部条目。删除FourthRouteMerchantPatch的旧激活调用，保留其碎片替换功能。
5. 拾起残缺/完整分别100金币，觉醒加入1张原版贪婪并排队一次免费商店；保存PickupEffectGranted在await前防重复，正常读档不调用新的拾起奖励。Stage3/4使用正式原句和标点。

## 保存与预约边界

- 选房查询不消费Pending；CreateRoom前保存的原生存档仍保留Pending，重进会再次选择同一问号商店。新字段也允许恢复带有明确位置的Active并绑定同一位置创建的房间，不把bool直接解释为所有商店免费。
- 旧档仅Active=true、没有位置时，不能证明哪个商店有资格，不重发Pending或给任意新商店免单；旧Pending=true仍正常生效。不伪造历史位置，旧档实机迁移待验。
- Q8规定预约事件优先。当前仓库未找到正式预约调度入口，本批不补写其事件内容，也不宣称集成完成。约定高优先级调度器先接管RollRoomTypeFor并跳过原方法；本批最后优先级不覆盖其结果，资格顺延。
- 每次选房的独立无返回值Prefix先清除旧临时决定，即使其他Prefix跳过原方法也不会复用上次决定；最终结果非Shop也清除临时决定。真正预约生产调度与碰撞仍需后续实现和自然流程验收。

## 已执行验证

| 验证 | 结果 |
|---|---|
| Debug/Release，`-p:DeployMod=false --no-restore` | 零警告零错误，最后保留Debug |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 482新增直接执行生产GreedShopState断言，合计4073通过 |
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 256通过，新增8项文本、接线及测试覆盖检查 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过；静态合计275 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 33既有编码断言通过，不代表商店整局保存验收 |
| ValidateMvpContent / StructuralContracts / LocalizationStyle / CardEffectTests | 四门通过 |
| ValidateVisualAssets | 既有317口红独立数值标签门失败，未削弱门禁 |
| `AuditCardLocalization.py --no-write` | 225项；既有GagCurse费用1/2差异、4元数据未解、215文本待审、2设计独有、2退役兼容，未写共享报告 |

生产测试覆盖阶段、Pending/Active、问号/直接商店、预约与非预约、跨幕/跨层、创建失败、重复恢复、本人与其他玩家、不同房间实例、旧档缺少位置、一次资格的完整顺延/消费序列。

## 游戏内测试脚本（编译未运行）

`ms_test_greed confirm`；日志`[DS27GreedTest]`。仅Debug、响木天音、单人非战斗局。

**破坏性，仅可丢弃测试局：移除全部遗物、改变金币和牌组、消耗商店RNG与候选库存。不恢复这些游戏数据。结束/异常只清理临时Harmony提供者、房间栈、测试楼层、TestMode及运行锁。**

- 真正RelicCmd.Obtain五阶段，检查100/100金币、1次贪婪、重复拾起及SavedProperties恢复不再奖励。
- 检查生产Harmony补丁已安装，通过反射调用被补丁的真实RollRoomTypeFor和CreateRoom，不直接假设补丁生效。
- 直接Shop仍收费且Pending不消耗；Unknown变Shop，选房不消费、创建才消费；实际MerchantInventory所有卡牌、遗物、药水、删牌条目免费，库存重建两次仍免费。
- 临时高优先级事件Prefix接管真实选房，确认事件优先且资格顺延。该测试提供者仅在命令期间存在，绝不作为正式事件替代。
- 同楼层不同房间、其他楼层、其他玩家不免费，其他角色即使持有模型也不触发替换；新保存字段保留消费和位置，恢复对象必须先绑定真实房间才可免单。
- 离开后Active和Pending都不再生效，下一个问号不能再次免费。

仍需：运行命令、自然地图点旅行及显示、实际购买/服务结算、S&Q完整保存恢复、遗物移除、多人两端一致性、正式预约事件碰撞测试。未运行前不标VERIFIED；其余全量变更及Q12～Q16未决项仍保留。
