# 卡牌描述欲望图标化（2026-09-24）

## 修改

- 新增 `maidenDesireIcons()` SmartFormat 格式化器，使用欲望次级资源UI相同的 `32x32` 心形图标。
- 固定1～3点显示对应数量的图标，0点或4点以上显示“数字＋图标”，与原版能量文本规则一致。
- 将卡牌正文中代表欲望资源数量或欲望费用的文字替换为图标。
- 保留“欲望不再有惩罚”“防止增加欲望”等机制名称中的文字，不把关键词本身机械替换掉。
- 图标使用无尺寸参数的原版 `[img]...[/img]` 标记，避免再次干扰卡牌正文自动字号。
- 增加结构契约，阻止数字欲望单位和欲望费用文字回退。

## 回退点

- 修改前快照：`maiden-pre-desire-description-icons-20260924`
- 修改前基线：`9f930f5c57cc8999ec6c0182bead925467909760`

## 验证

- 卡牌本地化 JSON 解析通过；数字欲望单位与费用文字防回退契约通过。
- 结构契约、视觉资源和本地化富文本样式验证通过。
- 222 项卡牌注册、220 张可执行卡、68 张第二轮数值测试验证通过。
- 11 项控制意图运行时场景结构验证通过。
- `dotnet build -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore`：0 警告、0 错误。
- 完整 DesignDoc 审计当前被14项卡牌映射/费用/稀有度差异阻断；其中功性魔防壁与功性魔防壁IV两项来自并行修改的 DesignDoc，均不属于本次文本图标化范围。

## 部署

- 功能提交：`31e3a8c`（`feat(maiden): render desire costs as inline icons`）
- 部署命令：`dotnet build -c Debug -p:DeployMod=true -p:ValidateMod=false --no-restore`
- 部署结果：0 警告、0 错误。
- 源与安装目录 DLL SHA-256：`F04142FC805D448A79A1AF754FF16BDC89DD11BD61476DA3471D63FDBDFB219B`
- 源与安装目录 PDB SHA-256：`1A779B9C14A3B6BA16CF7402D87AD0F263C056177F1F196BB059C3F7DF2BBAFF`
- 源与安装目录卡牌本地化 SHA-256：`BB45B84E5055441BBABC6163C5816D97E3E7C82A2A973C8BA421DABFD134E854`
- 源与安装目录欲望文本图标 SHA-256：`16B22162585DDDA7CDAD65D707318EFF87B78B6569FEE29A96341F7EB692DF2C`
