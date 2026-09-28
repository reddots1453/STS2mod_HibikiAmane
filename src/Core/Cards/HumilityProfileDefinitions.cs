using System.Collections.ObjectModel;

namespace MaidenSuccubus.Core.Cards;

/// <summary>
/// Source-reviewed effect bodies, not inference from a Damage/Block variable or prose.
/// Empty programs mean the reviewed body has no immediate damage/block to retain.
/// Missing entries mean unsupported, never an empty or single-hit fallback.
/// Shared definitions are immutable; applying humility creates an instance multiplier.
/// </summary>
internal static class HumilityProfileDefinitions
{
    internal static IReadOnlyDictionary<string, HumilityEffectProgram> All { get; } = Build();

    private static IReadOnlyDictionary<string, HumilityEffectProgram> Build()
    {
        var profiles = new Dictionary<string, HumilityEffectProgram>(StringComparer.Ordinal);
        void Group(string origin, HumilityEffectProgram program, params string[] names)
        {
            foreach (string name in names) profiles.Add(origin + ":" + name, program);
        }
        static HumilityValue V(string name) => HumilityValue.Named(name);
        static HumilityValue N(decimal number) => HumilityValue.Number(number);
        static HumilityEffect D(HumilityTarget target = HumilityTarget.Selected, HumilityValue? hits = null) =>
            new(HumilityEffectKind.Damage, target, V("Damage"), hits ?? N(1));
        static HumilityEffect B(HumilityValue? repeats = null) =>
            new(HumilityEffectKind.Block, HumilityTarget.Self, V("Block"), repeats ?? N(1));

        // One native attack; draw, card generation, powers and other side effects deleted.
        Group("maiden", new([D()]),
            "MaidenStrike", "LightningRecoil", "IceBreakingSlash", "FlashStab",
            "ObstructingShot", "MindsEye", "BorrowedForceStrike", "MagicSword",
            "BurningBracelet", "ForgeStrike", "LightningKick", "SoulImpact", "CycloneRupture");
        Group("vanilla", new([D()]),
            "StrikeIronclad", "PommelStrike", "Uppercut", "KinglyKick", "ShiningStrike");
        Group("maiden", new([B()]), "MaidenDefend", "IceShard", "RepairAlyssa", "FrozenBracelet", "StudyPlan");
        Group("vanilla", new([B()]), "DefendIronclad", "ShrugItOff");

        // These boundaries match native OnPlay: two block commands, one two-hit random
        // attack, one all-enemy attack, and one attack with a calculated hit count.
        Group("maiden", new([B(N(2))]), "DoubleDefense");
        Group("maiden", new([D(HumilityTarget.RandomEnemy, N(2))]), "ExplosiveImpact");
        Group("maiden", new([D(HumilityTarget.AllEnemies)]), "UltimateFlare");
        Group("maiden", new([D(hits: V("Hits"))]), "Takemikazuchi");
        Group("vanilla", new([D(HumilityTarget.AllEnemies, HumilityValue.X(HumilityValueKind.EnergyX))]), "Whirlwind");

        // BaseValue of Hits is the upgrade bonus, not its native text's preview X.
        Group("maiden", new([new(HumilityEffectKind.Damage, HumilityTarget.Selected,
            HumilityValue.Binary(HumilityValueKind.Multiply, V("Damage"), HumilityValue.X(HumilityValueKind.SecondaryX)),
            HumilityValue.Binary(HumilityValueKind.Add, HumilityValue.X(HumilityValueKind.EnergyX), V("Hits")))]), "AllHopeLost");

        // Explicit removal, including pickup/exhaust/overdraft triggers. No rule-body
        // effects are retained; an existing enchantment still executes via native hooks.
        Group("maiden", new([]), "Transform", "IceShield", "AcceleratedMotion", "HealingArt", "Fusion",
            "MagiciansSecret", "Procrastinate", "Bath", "CurseInfection", "ForgeCharge", "BeyondReasonForge",
            "CalmingMist", "DreamMist");
        return new ReadOnlyDictionary<string, HumilityEffectProgram>(profiles);
    }
}
