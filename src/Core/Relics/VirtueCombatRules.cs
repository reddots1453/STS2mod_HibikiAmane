namespace MaidenSuccubus.Core.Relics;

internal static class VirtueCombatRules
{
    // 0: no pickup; 1: Signet; 2: Circlet (including legacy awakened stage 3).
    public static int TemperanceReward(int stage) => stage switch
    {
        1 or 2 => 1, 3 or 4 => 2, _ => 0
    };

    public static bool PatienceAtCombatStart(int stage) => stage is 1 or 2;
    public static bool PatienceAtTurnStart(int stage, bool sameOwner) =>
        stage is 3 or 4 && sameOwner;
    public static bool PatienceDiscount(int stage) => stage is 2 or 3 or 4;
}
