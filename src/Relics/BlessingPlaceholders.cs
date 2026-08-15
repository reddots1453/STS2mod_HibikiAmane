using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Relics;

public abstract class BlessingPlaceholderBase : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => new(
        IconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres",
        IconOutlinePath: "res://images/atlases/relic_outline_atlas.sprites/circlet.tres",
        BigIconPath: "res://images/atlases/relic_atlas.sprites/circlet.tres");
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class LightBlessingPlaceholder : BlessingPlaceholderBase;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class DarkBlessingPlaceholder : BlessingPlaceholderBase;
