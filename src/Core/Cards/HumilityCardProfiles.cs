using MaidenSuccubus.Cards;
using MaidenSuccubus.Cards.Scriptures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace MaidenSuccubus.Core.Cards;

internal static class HumilityCardProfiles
{
    // Exact Type identity avoids accepting unrelated mods with a matching short name.
    // Explicit typeof references also make renamed/removed classes a build error.
    private static readonly IReadOnlyDictionary<Type, string> Bindings = BuildBindings();
    internal static IEnumerable<Type> SupportedTypes => Bindings.Keys;

    private static IReadOnlyDictionary<Type, string> BuildBindings()
    {
        Type[] maiden = [typeof(MaidenStrike), typeof(MaidenDefend), typeof(LightningRecoil),
            typeof(IceBreakingSlash), typeof(FlashStab), typeof(ObstructingShot), typeof(MindsEye),
            typeof(BorrowedForceStrike), typeof(MagicSword), typeof(BurningBracelet), typeof(ForgeStrike),
            typeof(LightningKick), typeof(SoulImpact), typeof(CycloneRupture), typeof(IceShard),
            typeof(RepairAlyssa), typeof(FrozenBracelet), typeof(StudyPlan), typeof(DoubleDefense),
            typeof(ExplosiveImpact), typeof(UltimateFlare), typeof(Takemikazuchi), typeof(AllHopeLost),
            typeof(MaidenSuccubus.Cards.Transform), typeof(IceShield), typeof(AcceleratedMotion), typeof(HealingArt),
            typeof(MaidenSuccubus.Cards.Fusion), typeof(MagiciansSecret), typeof(Procrastinate), typeof(Bath),
            typeof(CurseInfection), typeof(ForgeCharge), typeof(BeyondReasonForge), typeof(CalmingMist), typeof(DreamMist),
            typeof(PenanceSlash), typeof(HolyPunishment), typeof(FocusedSlash), typeof(DesireWard),
            typeof(Judgment), typeof(HolyCurse), typeof(WindRumor), typeof(DevoutBulwark), typeof(AutoReactionArmor),
            typeof(ForgeNimble), typeof(MomentaryGrace), typeof(RetainedGuard), typeof(TacticalCore), typeof(BurningRack),
            typeof(ExternalPowerSkeleton), typeof(DragonflyTouch), typeof(TacticalAnalyzer), typeof(Tranquilizer),
            typeof(Stigma), typeof(Rest), typeof(MultipleReproduction), typeof(SunDance), typeof(DivineEcho),
            typeof(OriginalSinBrand), typeof(Chant), typeof(Gospel), typeof(SneakSnack), typeof(HolyFlame),
            typeof(ExorcismPerfume), typeof(PurificationOrb), typeof(GuardianScripture), typeof(NimbleScripture),
            typeof(PunishmentScripture), typeof(WisdomScripture), typeof(VitalityScripture), typeof(BlissScripture)];
        Type[] vanilla = [typeof(StrikeIronclad), typeof(PommelStrike), typeof(Uppercut), typeof(KinglyKick),
            typeof(ShiningStrike), typeof(DefendIronclad), typeof(ShrugItOff), typeof(Whirlwind)];
        var bindings = maiden.ToDictionary(type => type, type => "maiden:" + type.Name);
        foreach (Type type in vanilla) bindings.Add(type, "vanilla:" + type.Name);
        if (bindings.Count != HumilityProfileDefinitions.All.Count
            || bindings.Values.Any(key => !HumilityProfileDefinitions.All.ContainsKey(key)))
            throw new InvalidOperationException("Humility profile/type bindings are incomplete.");
        return bindings;
    }

    internal static bool TryGet(CardModel card, out HumilityEffectProgram program)
    {
        if (Bindings.TryGetValue(card.GetType(), out string? key))
        {
            program = HumilityProfileDefinitions.All[key];
            return true;
        }
        program = null!;
        return false;
    }

    // Not yet wired into HumilityLesson: its unrestricted selector must not become a
    // supported-only whitelist while the remaining conditional/temporal profiles are pending.
    internal static HumilityRewriteCapability ApplyKnown(CardModel card)
    {
        if (!TryGet(card, out HumilityEffectProgram program))
            throw new NotSupportedException($"No reviewed humility profile for {card.GetType().FullName}.");
        return HumilityRewriteCapability.Apply(card, program);
    }
}
