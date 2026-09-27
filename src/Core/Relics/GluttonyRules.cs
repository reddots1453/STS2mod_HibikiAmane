namespace MaidenSuccubus.Core.Relics;

internal static class GluttonyRules
{
    public static int PickupMaxHp(int stage) => stage is 1 or 2 ? 4 : 0;
    public static int PickupSlots(int stage) => stage is 1 or 2 ? 1 : 0;
    public static bool FillOnPickup(int stage) => stage is 3 or 4;
    public static int MaxHpOnPotionUsed(int stage, bool sameOwner) =>
        stage is 3 or 4 && sameOwner ? 4 : 0;
    public static int EmptySlots(int capacity, int occupied) =>
        Math.Max(0, Math.Max(0, capacity) - Math.Max(0, occupied));
}
