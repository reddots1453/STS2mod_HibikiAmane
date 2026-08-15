using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using MaidenSuccubus.Pools;
using MaidenSuccubus.Powers.Scriptures;

namespace MaidenSuccubus.Cards.Scriptures;

[RegisterCard(typeof(MSScriptureCardPool))]
public sealed class GuardianScripture
    : ScriptureCardTemplate<GuardianScripturePower>
{
    public GuardianScripture() : base(CardRarity.Uncommon) { }
}

[RegisterCard(typeof(MSScriptureCardPool))]
public sealed class NimbleScripture
    : ScriptureCardTemplate<NimbleScripturePower>
{
    public NimbleScripture() : base(CardRarity.Uncommon) { }
}

[RegisterCard(typeof(MSScriptureCardPool))]
public sealed class PunishmentScripture
    : ScriptureCardTemplate<PunishmentScripturePower>
{
    public PunishmentScripture() : base(CardRarity.Common) { }
}

[RegisterCard(typeof(MSScriptureCardPool))]
public sealed class WisdomScripture
    : ScriptureCardTemplate<WisdomScripturePower>
{
    public WisdomScripture() : base(CardRarity.Rare) { }
}

[RegisterCard(typeof(MSScriptureCardPool))]
public sealed class VitalityScripture
    : ScriptureCardTemplate<VitalityScripturePower>
{
    public VitalityScripture() : base(CardRarity.Common) { }
}

[RegisterCard(typeof(MSScriptureCardPool))]
public sealed class BlissScripture
    : ScriptureCardTemplate<BlissScripturePower>
{
    public BlissScripture() : base(CardRarity.Rare) { }
}
