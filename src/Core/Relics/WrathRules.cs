namespace MaidenSuccubus.Core.Relics;

internal static class WrathRules
{
    public static bool EnchantOnPickup(int stage) => stage is 1 or 2;
    public static int FirstPlayBonus(bool used, bool poweredAttack) => !used && poweredAttack ? 6 : 0;
    public static int AwakenedBonus(int stage, bool poweredAttack, bool ownCard, bool attackCard, bool hasWrath) =>
        stage is 3 or 4 && poweredAttack && ownCard && attackCard && hasWrath ? 6 : 0;
}
