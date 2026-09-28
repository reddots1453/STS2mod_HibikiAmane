using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MaidenSuccubus.Characters;
using MaidenSuccubus.Core.Corruption;

namespace MaidenSuccubus.Core.Relics;

internal static class ForgottenSoulVariation
{
    internal static bool TryGetDamage(RelicModel relic, out int damage)
    {
        damage = 1;
        if (relic is not ForgottenSoul || !relic.IsMutable
            || relic.Owner?.Character is not MaidenSuccubusCharacter
            || relic.Owner.RunState is not RunState run)
            return false;
        damage = CorruptionQuery.Get(run) >= 4 ? 2 : 1;
        return true;
    }
}
