# DS27 批次48：遗忘之魂路线变奏

前置快照`4538afc9`，Plan同步`743a97fe`，完成提交为本文件所在独立范围提交。`RELIC-VANILLA-002`完整条目→DS27-04I IMPLEMENTED，游戏内待验，不标VERIFIED。DesignDoc逐行/词级无漂移；不部署。

## 实体确认与实现

知识库`FORGOTTEN_SOUL`、v0.111.0反编译源码和当前安装`sts2.dll`的ILSpy只读结果共同确认：遗忘之魂是`ForgottenSoul`，基础伤害1、随机单敌、事件遗物。`CharonsAshes`是另一件遗物，基础3且全体伤害，本批不修改。

此前没有遗忘之魂路线变奏代码。新增`ForgottenSoulVariation.TryGetDamage`只接受可变、已有本Mod角色拥有者及实际RunState的ForgottenSoul。堕落值≥4为2，其余1；没有LocalContext筛选，远端本角色与本地采用相同规则。

两个Safe包裹的Harmony Postfix：

- `RelicModel.get_DynamicVars`：仅更新符合条件实例的Damage.BaseValue。每次读取按当前Run重新计算，变化往返和读档不依赖拾取订阅。保留原DamageVar及Unpowered属性，BaseValue原生setter也同步显示值。
- `RelicModel.get_Description`：仅为符合条件的实例选本Mod命名空间的正反变奏文本，原DynamicDescription继续注入Damage。精确保留句号和≥4/＜4；原版全局文本键不覆盖。

不替换`AfterCardExhausted`，不拦截或再次调用消耗，不改变原生目标列表/随机流/伤害命令/动画，不新增保存字段、不换遗物ID或移除再获取。其他角色和百科原型的变量与描述原样保留。原版Owner不允许将同一遗物实例转交另一玩家，本批不改变此约束。

## 游戏内测试入口

`ms_test_forgotten_soul confirm`：仅Debug、单人本角色、未结束的至少双敌战斗。**破坏性专用测试局脚本，会清理手牌/遗物/状态、修改生命和堕落等；本轮没有执行。**

- 同一遗物遍历-5至5，再3→4→3→5→-5；每次反复读取值和精确描述，读取不消耗目标RNG，遗物ID/位置/获得楼层不变。
- 百科原型、无拥有者副本、铁甲战士持有副本、远端本角色副本、CharonsAshes区别检查。其他玩家卡牌回调不得伤害或抽随机数。
- 原生序列化重建（不重播拾取）与可变克隆即时得到当前数值；不是完整引擎存读档认证。
- 每个阈值使用两次真实CardCmd.Exhaust，经原生Hook分发到取得的遗物；先复制CombatTargets随机状态，在副本上按原版NextItem选择预期目标，逐敌比对伤害及完整RNG状态。不通过手动调用己方回调假装真实分发。
- 给玩家20力量、敌人2易伤，检查伤害仍保持1/2的Unpowered行为；确认卡进入消耗堆。
- 移除遗物后再次真实消耗，不再造成伤害。

7项Python仅为设计全文/实体隔离/patch接线/无额外RNG/测试脚本边界的静态检查，不声称执行原生战斗。

## 本轮验证

| 命令（Mod目录） | 结果 |
|---|---|
| `python -m unittest discover -s scripts -p 'Test*20260927.py'` | 387通过，新增7；初次注释误命中禁止async检查，改为仅查代码后通过 |
| `python scripts/TestCardLocalizationAudit.py` | 19通过，累计406静态 |
| `dotnet build MaidenSuccubus.csproj -c Release -p:DeployMod=false --no-restore` | 0警告0错误 |
| 同命令`-c Debug` | 0警告0错误，最终保留Debug |
| `dotnet run --project tests/DesignSyncContracts --no-restore` | 既有13540生产纯规则断言通过，本批无新增 |
| `dotnet run --project tests/LayeredSaveContracts --no-restore` | 既有33编码断言通过，非引擎执行 |
| MvpContent/StructuralContracts/LocalizationStyle/CardEffectTests（各`-ProjectDir .`） | 四门通过；本地化门初次发现独立键缺title，补齐后通过 |

现存失败：VisualAssets第317行侧栏诱惑度图标/数字检查；全卡审计`--no-write`的GagCurse费用1/2、4未知元数据、215文本待审、2 designOnly、2 retiredCompat。未覆盖并行审计JSON，未改变这些检查。

仍待验：专用局执行脚本、自然消耗/虚无离手/多张同时消耗、无存活敌人沿用原生空目标路径、多人不同角色真实Hook分发、完整存档加载、战斗外阈值变化后的实际遗物悬停。当前安装DLL的原生控制流已核对，但不代替运行时验证。总目标未完成，未部署或启动游戏。
