using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MaidenSuccubus.Pools;

namespace MaidenSuccubus.Relics;

public abstract class BlessingPlaceholderBase : ModRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override RelicAssetProfile AssetProfile => RelicIconAssets.For(this is LightBlessingPlaceholder ? "legacy_light_blessing_placeholder_v1" : "legacy_dark_blessing_placeholder_v1");
}

[RegisterRelic(typeof(MSRelicPool))]
public sealed class LightBlessingPlaceholder : BlessingPlaceholderBase;

[RegisterRelic(typeof(MSRelicPool))]
public sealed class DarkBlessingPlaceholder : BlessingPlaceholderBase;
