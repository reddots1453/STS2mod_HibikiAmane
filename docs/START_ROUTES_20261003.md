

## 2026-10-03 三路线开局与通关解锁（START-001/003）

前置快照 `87ce00bb51f5f812aab6c345c4fd5fa096b4cb58`。用户直接要求中立0、堕落+3、圣洁-3开局，天音胜利时最终堕落值≥3/≤-3分别解锁。设计已独立提交，随后同步实现。本批沿用一个注册角色和既有初始遗物选择，不注册额外角色模型，不引入新美术。

技术：原生选角信息VBox中遗物上方增加路线行，沿用原生箭头/字体；未解锁显示条件，箭头仅遍历已解锁路线，已准备、锁定、随机、其他角色和离开屏幕不可操作。per-player大厅选择复用starter_relic_choice向后兼容字段；RitsuLib导入完成后的FinalizeStartingRelics初始化，并保存RouteApplied收据，继续存档不重置。堕落值仍沿用既有共享Run/首位天音规则，多天音不作新的独立堕落设计。解锁使用RitsuLib Profile作用域，OnEnded原生胜利完成后记录，仅本地天音、正常保存、非放弃、首次终局结算；普通战斗胜利、其他角色、失败、放弃与Debug不解锁。旧存档缺字段默认中立与未解锁。

验收ID START-ROUTE-SELECT/VICTORY/PROFILE/LOAD：首次≤-3及≥3通关，各自解锁并跨重启保留；±2不解锁；三种初始值及两种遗物独立切换；读档保留中途数值；档案切换、选其他角色、准备/取消、手柄、不同分辨率需用户实测。代码状态IMPLEMENTED，游戏内NOT_RUN，按用户要求不运行静态测试、不部署、不操作ModUploader及安装JSON。


构建交接：`dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`成功，0警告0错误。未运行静态测试，未进行游戏内验收；不部署至正式游戏、沙箱或ModUploader。独立产物保存到聊天outputs/start-routes-20261003，复用上一批完整PCK（本批无资源文件变更）。
