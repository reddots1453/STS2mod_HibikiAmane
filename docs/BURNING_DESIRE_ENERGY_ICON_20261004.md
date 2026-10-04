

## 2026-10-04 瘴雷能量图标修复（CARD-C-BURNING-DESIRE-ENERGY-001）

快照`e72ae7d288ab07a7c2e8e6d9a2fa203fcd952127`，设计提交`440b2921209c17f76a2c914e00d8de48eecc8a28`。复核瘴雷当前正式描述、CARD-C-BURNING-DESIRE-COUNT-001、EnergyVar(1)与MaidenEnergyIconsFormatter及同模组魔力图标文本。DesignDoc相对HEAD/最后接受提交逐行与词级漂移留档；其他叙事及反馈扩充工作树保留。原因：描述误用原版energyIcons，未走本mod已注册的maidenEnergyIcons。任务READY：仅将cards.json瘴雷描述的{Energy:energyIcons()}替换为{Energy:maidenEnergyIcons()}；复用magic_energy_cost_icon_32资源，保留EnergyVar和实际效果。PCK仅替换最后接受版本的cards.json这一处文本；其他资源与文案配置继承上一包，不混入未提交文本。构建后IMPLEMENTED、游戏内待用户手测；不运行静态测试，仅build，不部署、不改安装/上传器JSON。

CARD-C-BURNING-DESIRE-ENERGY-001 IMPLEMENTED：瘴雷获得能量文本改用本mod魔力能量图标；构建0警告0错误，静态测试NOT_RUN、游戏内NOT_RUN，未部署。
