namespace MaidenSuccubus.Core.Relics;

internal static class VirtueCombatRules
{
    public static int TemperanceMaximum(int stage) => stage switch
    {
        1 => 2, 2 or 3 or 4 => 3, _ => 0
    };

    public static bool PatienceAtCombatStart(int stage) => stage is 1 or 2;
    public static bool PatienceAtTurnStart(int stage, bool sameOwner) =>
        stage is 3 or 4 && sameOwner;
    public static bool PatienceDiscount(int stage) => stage is 2 or 3 or 4;
}
