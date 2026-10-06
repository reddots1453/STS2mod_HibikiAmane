# HibikiAmane — 响木天音

Slay the Spire 2 可玩角色模组。当前支持游戏 v0.111.0，依赖 STS2.RitsuLib 0.4.64 与 .NET 9 SDK。

## 仓库内容

包含本模组源码、设计与开发记录、测试源码，以及运行时使用的 PNG/音频/场景/本地化资源。编译得到的 DLL、PDB、PCK、Godot 导入缓存、其他模组、原版反编译源码与美术候选不提交。

历史从原多模组仓库中按本模组路径提取，移除了父目录前缀；提交哈希因此变化。来源最新部署记录：`aff6d1e8718058a73c428330ac81cdb34de12475`，功能提交：`83d8189d2499d41de5d2d805ef22668fde12ceb5`。`local-deployed-20261006` 标记提取后的对应版本，随后提交补齐当前工作区的源资源。

## 构建

项目仍使用原目录关系。将本仓库放在游戏目录下的 `Sts2-mod-decompiled/mods/`；本地须有游戏 v0.111.0 程序集，以及 `Sts2-mod-decompiled/_decompiled/sts2-v0.111.0/MegaCrit.Sts2.Core.Models.Cards/`。后者用于生成谦逊效果的调用目录，不随本仓库上传。

```powershell
dotnet restore MaidenSuccubus.csproj
dotnet build MaidenSuccubus.csproj -c Debug -p:DeployMod=false -p:ValidateMod=false --no-restore
```

资源导入使用官方 Godot 4.5.1 的命令行程序，并保持原图尺寸：

```powershell
python scripts/ImportTextureAssets.py --project . --godot '<Godot程序路径>' --cache '<本地导入缓存目录>'
python scripts/PackImportedResources.py --project .
```

资源包只有项目当前 `MaidenSuccubus.pck` 一份；版本恢复使用源码提交重新构建。部署文件哈希与构建记录单独保存，不备份资源包。
